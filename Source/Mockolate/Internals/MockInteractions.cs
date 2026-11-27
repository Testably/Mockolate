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
public class MockInteractions : IMockInteractions
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private RecordNode? _head;

	/// <summary>
	///     Whether a call was recorded as an interaction instead of a typed record. Only then can a member without
	///     typed records have calls at all.
	/// </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private bool _hasInteractionRecords;
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

			Interlocked.CompareExchange(ref field, new MockolateLock(), comparand: null);
			return field!;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private HashSet<IInteraction>? _verified;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private MockInteractions[]? _sharedVerifications;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private CallOrderTracker.TrackedMock? _trackedCallOrder;

	/// <summary>
	///     Creates a new <see cref="MockInteractions" /> for a mock with <paramref name="memberCount" /> members.
	/// </summary>
	/// <param name="memberCount">The number of distinct mockable members.</param>
	/// <param name="skipInteractionRecording">Mirrors <see cref="MockBehavior.SkipInteractionRecording" />.</param>
	public MockInteractions(int memberCount, bool skipInteractionRecording = false)
	{
		MemberCount = memberCount;
		SkipInteractionRecording = skipInteractionRecording;
	}

	/// <summary>
	///     The number of distinct mockable members of the mock.
	/// </summary>
	public int MemberCount { get; }

	/// <inheritdoc cref="IMockInteractions.SkipInteractionRecording" />
	public bool SkipInteractionRecording { get; }

	/// <inheritdoc cref="IMockInteractions.InteractionAdded" />
	public event EventHandler? InteractionAdded;

	/// <inheritdoc cref="IMockInteractions.OnClearing" />
	public event EventHandler? OnClearing;

	/// <summary>
	///     The number of interactions recorded for the mock.
	/// </summary>
	public int Count => First is { } newest ? newest.Seq + 1 : 0;

	/// <summary>
	///     The newest recorded call of the mock, or <see langword="null" /> if there is none.
	/// </summary>
	public RecordNode? First => Volatile.Read(ref _head);

	/// <summary>
	///     Publishes the fully written <paramref name="node" /> as the newest call of the mock and raises
	///     <see cref="InteractionAdded" />.
	/// </summary>
	/// <remarks>
	///     Public because the source generator emits the recorders of methods with more than four parameters in the consumer assembly that record their
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

		if (Volatile.Read(ref _trackedCallOrder) is { } trackedCallOrder)
		{
			trackedCallOrder.Record(node);
		}

		InteractionAdded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	///     The mock as tracked by the latest tracker of its call order, or <see langword="null" /> if it was never
	///     tracked.
	/// </summary>
	internal CallOrderTracker.TrackedMock? TrackedCallOrder => Volatile.Read(ref _trackedCallOrder);

	/// <summary>
	///     Lets <paramref name="tracked" /> track the order of the following calls, unless another tracker is still
	///     running.
	/// </summary>
	internal bool TryTrackCallOrder(CallOrderTracker.TrackedMock tracked, out CallOrderTracker.TrackedMock? previous)
	{
		do
		{
			previous = Volatile.Read(ref _trackedCallOrder);
			if (previous is { IsRunning: true, })
			{
				return false;
			}
		} while (Interlocked.CompareExchange(ref _trackedCallOrder, tracked, previous) != previous);

		return true;
	}

	/// <summary>
	///     Undoes <see cref="TryTrackCallOrder" /> of <paramref name="tracked" />, so that the <paramref name="previous" />
	///     tracker keeps the call order of the mock.
	/// </summary>
	internal void RestoreCallOrder(CallOrderTracker.TrackedMock tracked, CallOrderTracker.TrackedMock? previous)
		=> Interlocked.CompareExchange(ref _trackedCallOrder, previous, tracked);

	/// <summary>
	///     Returns a view on the typed records of the member <paramref name="memberId" />, or
	///     <see langword="null" /> when its calls can only be found among all interactions of the mock.
	/// </summary>
	internal MemberRecords? FindRecords(int memberId)
		=> FindRecords(memberId, static (owner, id) => new UntypedMemberRecords(owner, id));

	/// <summary>
	///     Returns a typed view on the records of the member <paramref name="memberId" />, or
	///     <see langword="null" /> when its calls can only be found among all interactions of the mock.
	/// </summary>
	/// <remarks>
	///     A member without typed records was either not called or recorded as an interaction. As long as the mock
	///     holds no interaction record it is the former, and the empty view answers the verification.
	/// </remarks>
	internal TRecords? FindRecords<TRecords>(int memberId, Func<MockInteractions, int, TRecords> factory)
		where TRecords : MemberRecords
	{
		if (memberId < 0)
		{
			return null;
		}

		TRecords records = factory(this, memberId);
		return records.HasRecords || !Volatile.Read(ref _hasInteractionRecords) ? records : null;
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
			// Set before and after publishing: a concurrent Clear may reset the flag in between, and it must
			// never be unset while the record is in the list.
			Volatile.Write(ref _hasInteractionRecords, value: true);
			Push(new InteractionRecord(interaction));
			Volatile.Write(ref _hasInteractionRecords, value: true);
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
		MarkVerified(interactions);
		if (Volatile.Read(ref _sharedVerifications) is { } sharedVerifications)
		{
			foreach (MockInteractions shared in sharedVerifications)
			{
				shared.MarkVerified(interactions);
			}
		}
	}

	/// <summary>
	///     Marks the interactions that a verification on this store matched as verified on <paramref name="other" />
	///     as well.
	/// </summary>
	internal void ShareVerificationsWith(MockInteractions other)
	{
		MockInteractions[]? current;
		MockInteractions[] updated;
		do
		{
			current = Volatile.Read(ref _sharedVerifications);
			updated = current is null ? [other,] : [..current, other,];
		} while (Interlocked.CompareExchange(ref _sharedVerifications, updated, current) != current);
	}

	private void MarkVerified(IEnumerable<IInteraction> interactions)
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
		Volatile.Read(ref _trackedCallOrder)?.Clear();
		Volatile.Write(ref _hasInteractionRecords, value: false);
		Volatile.Write(ref _head, value: null);

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