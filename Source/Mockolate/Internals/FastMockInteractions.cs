using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Per-mock interaction storage: one lock-free list of all calls, newest first. Each call is one
///     <see cref="RecordNode" /> that carries the id of its member, so that the first call of a member needs no
///     storage of its own and the position in the list is the order of the calls.
/// </summary>
/// <remarks>
///     A node is fully written before a single compare-and-swap publishes it as the new head, so readers walk the
///     list without a lock and never see a partially written record.
///     <para />
///     The position of a call is taken from the head it is published on, so a call that is published while
///     <see cref="Clear" /> runs is either removed by it or becomes the first call after it.
/// </remarks>
[DebuggerDisplay("{Count} interactions")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public class FastMockInteractions : IMockInteractions
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private RecordNode? _head;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private IFastMemberBuffer?[]? _buffers;

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
	///     Creates a new <see cref="FastMockInteractions" /> for a mock with <paramref name="memberCount" /> members.
	/// </summary>
	/// <param name="memberCount">The number of distinct mockable members.</param>
	/// <param name="skipInteractionRecording">Mirrors <see cref="MockBehavior.SkipInteractionRecording" />.</param>
	public FastMockInteractions(int memberCount, bool skipInteractionRecording = false)
	{
		MemberCount = memberCount;
		SkipInteractionRecording = skipInteractionRecording;
	}

	/// <summary>
	///     The number of distinct mockable members of the mock.
	/// </summary>
	public int MemberCount { get; }

	/// <summary>
	///     The views on the calls of each member that were requested through
	///     <see cref="GetOrCreateBuffer{TBuffer}(int, Func{FastMockInteractions, TBuffer})" />, by member id. Recording
	///     a call does not need a view, so a slot can be empty although the member was called.
	/// </summary>
	public IFastMemberBuffer?[] Buffers
	{
		get
		{
			if (_buffers is { } existing)
			{
				return existing;
			}

			Interlocked.CompareExchange(ref _buffers, new IFastMemberBuffer?[MemberCount], null);
			return _buffers!;
		}
	}

	/// <inheritdoc cref="IMockInteractions.SkipInteractionRecording" />
	public bool SkipInteractionRecording { get; }

	/// <inheritdoc cref="IMockInteractions.InteractionAdded" />
	public event EventHandler? InteractionAdded;

	/// <inheritdoc cref="IMockInteractions.OnClearing" />
	public event EventHandler? OnClearing;

	/// <summary>
	///     <see langword="true" /> when at least one subscriber has registered with
	///     <see cref="InteractionAdded" />.
	/// </summary>
	public bool HasInteractionAddedSubscribers => InteractionAdded is not null;

	/// <summary>
	///     The number of interactions recorded for the mock.
	/// </summary>
	public int Count => First is { } newest ? (int)newest.Seq + 1 : 0;

	/// <summary>
	///     The newest recorded call of the mock, or <see langword="null" /> if there is none.
	/// </summary>
	public RecordNode? First => Volatile.Read(ref _head);

	/// <summary>
	///     Publishes the fully written <paramref name="node" /> as the newest call of the mock and raises
	///     <see cref="InteractionAdded" />.
	/// </summary>
	/// <remarks>
	///     Public because the source generator emits arity-N buffers in the consumer assembly that record their
	///     calls here. Not intended for end-user code.
	/// </remarks>
	public void Push(RecordNode node)
	{
		RecordNode? head = Volatile.Read(ref _head);
		while (true)
		{
			node.Next = head;
			node.Seq = head is null ? 0 : head.Seq + 1;
			RecordNode? previous = Interlocked.CompareExchange(ref _head, node, head);
			if (ReferenceEquals(previous, head))
			{
				break;
			}

			head = previous;
		}

		InteractionAdded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	///     Returns the view on the calls of the member <paramref name="memberId" />, creating it via
	///     <paramref name="factory" /> on first use. The factory must be a static lambda so the runtime never
	///     allocates a closure.
	/// </summary>
	/// <typeparam name="TBuffer">The concrete buffer type of the member.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the buffer's target member.</param>
	/// <param name="factory">
	///     Builds a new buffer when none was requested yet. Under contention the loser's allocation is discarded
	///     and the winner is returned.
	/// </param>
	public TBuffer GetOrCreateBuffer<TBuffer>(int memberId, Func<FastMockInteractions, TBuffer> factory)
		where TBuffer : class, IFastMemberBuffer
	{
		IFastMemberBuffer?[] buffers = Buffers;
		if (Volatile.Read(ref buffers[memberId]) is TBuffer typed)
		{
			return typed;
		}

		return Publish(buffers, memberId, factory(this));
	}

	/// <summary>
	///     <typeparamref name="TState" />-passing variant of
	///     <see cref="GetOrCreateBuffer{TBuffer}(int, Func{FastMockInteractions, TBuffer})" /> that lets
	///     callers supply the buffer's construction parameter without allocating a closure.
	/// </summary>
	/// <typeparam name="TBuffer">The concrete buffer type of the member.</typeparam>
	/// <typeparam name="TState">Type of the construction parameter forwarded to <paramref name="factory" />.</typeparam>
	/// <param name="memberId">The generator-emitted member id for the buffer's target member.</param>
	/// <param name="factory">
	///     Builds a new buffer from <paramref name="state" /> when none was requested yet. Must be a
	///     static lambda; <paramref name="state" /> is the only flow path so closures are unnecessary.
	/// </param>
	/// <param name="state">Forwarded to <paramref name="factory" /> on construction.</param>
	public TBuffer GetOrCreateBuffer<TBuffer, TState>(int memberId,
		Func<FastMockInteractions, TState, TBuffer> factory, TState state)
		where TBuffer : class, IFastMemberBuffer
	{
		IFastMemberBuffer?[] buffers = Buffers;
		if (Volatile.Read(ref buffers[memberId]) is TBuffer typed)
		{
			return typed;
		}

		return Publish(buffers, memberId, factory(this, state));
	}

	private static TBuffer Publish<TBuffer>(IFastMemberBuffer?[] buffers, int memberId, TBuffer created)
		where TBuffer : class, IFastMemberBuffer
	{
		if (created is RecordList view)
		{
			view.MemberId = memberId;
		}

		IFastMemberBuffer? previous = Interlocked.CompareExchange(ref buffers[memberId], created, null);
		return previous is TBuffer winner ? winner : created;
	}

	/// <summary>
	///     Returns a view on the typed records of the member <paramref name="memberId" />, or
	///     <see langword="null" /> when the member has none.
	/// </summary>
	internal IFastMemberBuffer? FindBuffer(int memberId)
	{
		if ((uint)memberId >= (uint)MemberCount)
		{
			return null;
		}

		IFastMemberBuffer buffer = _buffers?[memberId] ?? new MemberRecords(this) { MemberId = memberId, };
		return buffer is RecordList { HasRecords: false, } ? null : buffer;
	}

	/// <summary>
	///     Returns a typed view on the records of the member <paramref name="memberId" />, or
	///     <see langword="null" /> when the member has none.
	/// </summary>
	internal TBuffer? FindBuffer<TBuffer>(int memberId, Func<FastMockInteractions, TBuffer> factory)
		where TBuffer : RecordList
	{
		if ((uint)memberId >= (uint)MemberCount)
		{
			return null;
		}

		if (_buffers?[memberId] is not TBuffer buffer)
		{
			buffer = factory(this);
			buffer.MemberId = memberId;
		}

		return buffer.HasRecords ? buffer : null;
	}

	/// <inheritdoc cref="IMockInteractions.RegisterInteraction{TInteraction}(TInteraction)" />
	/// <remarks>
	///     Fallback for code paths that produce a heap-allocated <see cref="IInteraction" /> instance. The
	///     generated code records typed calls instead.
	/// </remarks>
	TInteraction IMockInteractions.RegisterInteraction<TInteraction>(TInteraction interaction)
	{
		if (!SkipInteractionRecording)
		{
			Push(new InteractionRecord(interaction));
		}

		return interaction;
	}

	/// <inheritdoc cref="IMockInteractions.GetUnverifiedInteractions" />
	/// <remarks>
	///     A typed verification marks the calls it matched; <see cref="IMockInteractions.Verified(IEnumerable{IInteraction})" />
	///     collects the interactions of the other verifications. The result contains the calls that neither of
	///     them has seen.
	/// </remarks>
	public IReadOnlyCollection<IInteraction> GetUnverifiedInteractions()
	{
		List<IInteraction> unverified = new();
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (!node.Verified)
			{
				unverified.Add(node.Interaction);
			}
		}

		if (unverified.Count == 0)
		{
			return Array.Empty<IInteraction>();
		}

		unverified.Reverse();
		lock (VerifiedLock)
		{
			if (_verified is { Count: > 0, } verified)
			{
				unverified.RemoveAll(verified.Contains);
			}
		}

		return unverified;
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
		Volatile.Write(ref _head, null);

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
		RecordNode? newest = First;
		if (newest is null)
		{
			return [];
		}

		IInteraction[] result = new IInteraction[newest.Seq + 1];
		for (RecordNode? node = newest; node is not null; node = node.Next)
		{
			result[node.Seq] = node.Interaction;
		}

		return result;
	}
}