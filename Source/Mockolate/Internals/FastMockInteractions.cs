using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Per-mock interaction storage backed by an array of typed per-member buffers. Each mocked
///     member writes to its own buffer (no shared list, no shared lock); a global monotonic
///     sequence number tags every record so ordered enumeration across buffers preserves
///     registration order.
/// </summary>
[DebuggerDisplay("{Count} interactions")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public class FastMockInteractions : IMockInteractions
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private long _globalSequence;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private MockolateLock VerifiedLock
	{
		get
		{
			if (field is { } existing)
			{
				return existing;
			}

			Interlocked.CompareExchange(ref field, new MockolateLock(), null);
			return field!;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private HashSet<IInteraction>? _verified;

	/// <summary>
	///     Creates a new <see cref="FastMockInteractions" /> sized to <paramref name="memberCount" />.
	///     Each mockable member's buffer slot starts empty and is materialized lazily on first access via
	///     <see cref="GetOrCreateBuffer{TBuffer}(int, System.Func{Mockolate.Internals.FastMockInteractions,TBuffer})" />.
	/// </summary>
	/// <param name="memberCount">The number of distinct mockable members the buffer array should hold.</param>
	/// <param name="skipInteractionRecording">Mirrors <see cref="MockBehavior.SkipInteractionRecording" />.</param>
	public FastMockInteractions(int memberCount, bool skipInteractionRecording = false)
	{
		Buffers = new IFastMemberBuffer?[memberCount];
		SkipInteractionRecording = skipInteractionRecording;
	}

	/// <summary>
	///     The per-member buffers exposed by index. The source generator emits typed casts against
	///     this array to record interactions without going through the legacy
	///     <see cref="IMockInteractions.RegisterInteraction{TInteraction}(TInteraction)" /> path.
	/// </summary>
	public IFastMemberBuffer?[] Buffers { get; }

	/// <inheritdoc cref="IMockInteractions.SkipInteractionRecording" />
	public bool SkipInteractionRecording { get; }

	/// <inheritdoc cref="IMockInteractions.InteractionAdded" />
	public event EventHandler? InteractionAdded;

	/// <inheritdoc cref="IMockInteractions.OnClearing" />
	public event EventHandler? OnClearing;

	/// <summary>
	///     <see langword="true" /> when at least one subscriber has registered with
	///     <see cref="InteractionAdded" />. Buffer Append paths read this flag to skip
	///     <see cref="RaiseAdded" /> when nothing is listening, which is the common case (the event is
	///     used today only by <c>Within(TimeSpan)</c> waiting).
	/// </summary>
	public bool HasInteractionAddedSubscribers => InteractionAdded is not null;

	/// <summary>
	///     The number of interactions contained in the collection across all per-member buffers.
	/// </summary>
	public int Count => (int)Interlocked.Read(ref _globalSequence);

	/// <summary>
	///     Returns the buffer at <paramref name="memberId" />, materializing it via <paramref name="factory" />
	///     under a lock-free CAS on first touch. The factory must be a static lambda so the runtime never
	///     allocates a closure on the lazy path.
	/// </summary>
	/// <typeparam name="TBuffer">The concrete buffer type at the slot.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the buffer's target member.</param>
	/// <param name="factory">
	///     Builds a new buffer when the slot is still empty. Invoked at most once per slot in the absence
	///     of a race; under contention the loser's allocation is discarded and the winner is returned.
	/// </param>
	public TBuffer GetOrCreateBuffer<TBuffer>(int memberId, Func<FastMockInteractions, TBuffer> factory)
		where TBuffer : class, IFastMemberBuffer
	{
		IFastMemberBuffer?[] buffers = Buffers;
		IFastMemberBuffer? existing = Volatile.Read(ref buffers[memberId]);
		if (existing is TBuffer typed)
		{
			return typed;
		}

		TBuffer created = factory(this);
		IFastMemberBuffer? prev = Interlocked.CompareExchange(ref buffers[memberId], created, null);
		return prev is TBuffer winner ? winner : created;
	}

	/// <summary>
	///     <typeparamref name="TState" />-passing variant of
	///     <see cref="GetOrCreateBuffer{TBuffer}(int, Func{FastMockInteractions, TBuffer})" /> that lets
	///     callers supply the buffer's construction parameter without allocating a closure.
	/// </summary>
	/// <typeparam name="TBuffer">The concrete buffer type at the slot.</typeparam>
	/// <typeparam name="TState">Type of the construction parameter forwarded to <paramref name="factory" />.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the buffer's target member.</param>
	/// <param name="factory">
	///     Builds a new buffer from <paramref name="state" /> when the slot is still empty. Must be a
	///     static lambda; <paramref name="state" /> is the only flow path so closures are unnecessary.
	/// </param>
	/// <param name="state">Forwarded to <paramref name="factory" /> on construction.</param>
	public TBuffer GetOrCreateBuffer<TBuffer, TState>(int memberId,
		Func<FastMockInteractions, TState, TBuffer> factory, TState state)
		where TBuffer : class, IFastMemberBuffer
	{
		IFastMemberBuffer?[] buffers = Buffers;
		IFastMemberBuffer? existing = Volatile.Read(ref buffers[memberId]);
		if (existing is TBuffer typed)
		{
			return typed;
		}

		TBuffer created = factory(this, state);
		IFastMemberBuffer? prev = Interlocked.CompareExchange(ref buffers[memberId], created, null);
		return prev is TBuffer winner ? winner : created;
	}

	/// <summary>
	///     Reserves the next sequence number for a recording. Buffer implementations call this once
	///     per <see cref="IFastMemberBuffer" /> append so cross-buffer enumeration preserves
	///     registration order.
	/// </summary>
	/// <remarks>
	///     Public because the source generator emits arity-N buffers in the consumer assembly that
	///     need to participate in the same sequence stream. Not intended for end-user code.
	/// </remarks>
	public long NextSequence()
		=> Interlocked.Increment(ref _globalSequence) - 1;

	/// <summary>
	///     Fires the <see cref="InteractionAdded" /> event. Buffer implementations call this after
	///     publishing a new record, but only when <see cref="HasInteractionAddedSubscribers" /> is
	///     <see langword="true" /> — the field check is a single read; the call site is a method call.
	/// </summary>
	/// <remarks>
	///     Public because the source generator emits arity-N buffers in the consumer assembly that
	///     need to drive the event. Not intended for end-user code.
	/// </remarks>
	public void RaiseAdded()
		=> InteractionAdded?.Invoke(this, EventArgs.Empty);

	/// <inheritdoc cref="IMockInteractions.RegisterInteraction{TInteraction}(TInteraction)" />
	/// <remarks>
	///     Transitional fallback for code paths that still produce a heap-allocated
	///     <see cref="IInteraction" /> instance. The hot-path generator emission bypasses this entirely
	///     and writes straight into the typed per-member buffer. <c>RegisterInteraction</c> here
	///     always appends to the global "unattributed" fallback buffer, which still participates
	///     in ordered enumeration and verification.
	/// </remarks>
	TInteraction IMockInteractions.RegisterInteraction<TInteraction>(TInteraction interaction)
	{
		if (SkipInteractionRecording)
		{
			return interaction;
		}

		IFastMemberBuffer fallback = GetOrCreateFallbackBuffer();
		((FallbackBuffer)fallback).Append(interaction);
		return interaction;
	}

	/// <inheritdoc cref="IMockInteractions.GetUnverifiedInteractions" />
	/// <remarks>
	///     Each typed buffer tracks fast-path verifications internally (per-slot bitmap or
	///     monotonic cursor) and only emits records still considered unverified.
	///     <see cref="IMockInteractions.Verified(IEnumerable{IInteraction})" /> still feeds
	///     <c>_verified</c> for slow-path callers, so the result is the intersection of "not marked by
	///     a fast-count walk" and "not seen by a slow-path Verify."
	/// </remarks>
	public IReadOnlyCollection<IInteraction> GetUnverifiedInteractions()
	{
		List<(long Seq, IInteraction Interaction)> unverified = new();
		foreach (IFastMemberBuffer? buffer in Buffers)
		{
			buffer?.AppendBoxedUnverified(unverified);
		}

		Volatile.Read(ref _fallback)?.AppendBoxedUnverified(unverified);

		if (unverified.Count > 1)
		{
			unverified.Sort(static (left, right) => left.Seq.CompareTo(right.Seq));
		}

		lock (VerifiedLock)
		{
			if (_verified is null || _verified.Count == 0)
			{
				if (unverified.Count == 0)
				{
					return Array.Empty<IInteraction>();
				}

				IInteraction[] all = new IInteraction[unverified.Count];
				for (int i = 0; i < unverified.Count; i++)
				{
					all[i] = unverified[i].Interaction;
				}

				return all;
			}

			List<IInteraction> result = new(unverified.Count);
			foreach ((long _, IInteraction interaction) in unverified)
			{
				if (!_verified.Contains(interaction))
				{
					result.Add(interaction);
				}
			}

			return result;
		}
	}

	void IMockInteractions.Verified(IEnumerable<IInteraction> interactions)
	{
		lock (VerifiedLock)
		{
			_verified ??= [];
			foreach (IInteraction interaction in interactions)
			{
				_verified.Add(interaction);
			}
		}
	}

	/// <inheritdoc cref="IMockInteractions.Clear" />
	public void Clear()
	{
		Interlocked.Exchange(ref _globalSequence, 0);
		foreach (IFastMemberBuffer? buffer in Buffers)
		{
			buffer?.Clear();
		}

		Volatile.Read(ref _fallback)?.Clear();

		lock (VerifiedLock)
		{
			_verified = null;
		}

		OnClearing?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc cref="IEnumerable{IInteraction}.GetEnumerator()" />
	public IEnumerator<IInteraction> GetEnumerator()
		=> ((IEnumerable<IInteraction>)SnapshotOrdered()).GetEnumerator();

	/// <inheritdoc cref="IEnumerable.GetEnumerator()" />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	private IInteraction[] SnapshotOrdered()
	{
		List<(long Seq, IInteraction Interaction)> all = new();
		foreach (IFastMemberBuffer? buffer in Buffers)
		{
			buffer?.AppendBoxed(all);
		}

		Volatile.Read(ref _fallback)?.AppendBoxed(all);

		all.Sort(static (left, right) => left.Seq.CompareTo(right.Seq));
		IInteraction[] result = new IInteraction[all.Count];
		for (int i = 0; i < all.Count; i++)
		{
			result[i] = all[i].Interaction;
		}

		return result;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private FallbackBuffer? _fallback;

	private IFastMemberBuffer GetOrCreateFallbackBuffer()
	{
		FallbackBuffer? buffer = Volatile.Read(ref _fallback);
		if (buffer is not null)
		{
			return buffer;
		}

		FallbackBuffer created = new(this);
		FallbackBuffer? existing = Interlocked.CompareExchange(ref _fallback, created, null);
		return existing ?? created;
	}

	private sealed class FallbackBuffer(FastMockInteractions owner) : IFastMemberBuffer
	{
		private readonly MockolateLock _lock = new();
		private readonly FastMockInteractions _owner = owner;
		private int _count;
		private (long Seq, IInteraction Interaction)[] _records = new (long, IInteraction)[4];

		public int Count => Volatile.Read(ref _count);

		public void Clear()
		{
			lock (_lock)
			{
				Array.Clear(_records, 0, _count);
				_count = 0;
			}
		}

		public void AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
		{
			(long Seq, IInteraction Interaction)[] snapshot;
			int n;
			lock (_lock)
			{
				n = _count;
				snapshot = new (long Seq, IInteraction Interaction)[n];
				Array.Copy(_records, 0, snapshot, 0, n);
			}

			for (int i = 0; i < n; i++)
			{
				dest.Add(snapshot[i]);
			}
		}

		public void AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
			=> AppendBoxed(dest);

		public void Append(IInteraction interaction)
		{
			long seq = _owner.NextSequence();
			lock (_lock)
			{
				int n = _count;
				if (n == _records.Length)
				{
					Array.Resize(ref _records, n * 2);
				}

				_records[n] = (seq, interaction);
				Volatile.Write(ref _count, n + 1);
			}

			if (_owner.HasInteractionAddedSubscribers)
			{
				_owner.RaiseAdded();
			}
		}
	}
}
