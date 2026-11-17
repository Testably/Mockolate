using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Per-member buffer for indexer getters with a single key.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerGetterBuffer<T1> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1}" />.
	/// </summary>
	/// <remarks>
	///     The generator allocates the access value to use as a setup-matching key; this overload reuses
	///     it as the pre-boxed record so enumeration doesn't allocate a second one.
	/// </remarks>
	public void Append(IndexerGetterAccess<T1> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1>(r.P1))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1>(r.P1))));
		}
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose key satisfies <paramref name="match1" />,
	///     marking each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer getters with two keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerGetterBuffer<T1, T2> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2>(r.P1, r.P2))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2>(r.P1, r.P2))));
		}
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer getters with three keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerGetterBuffer<T1, T2, T3> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2, T3}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2, T3> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2, T3>(r.P1, r.P2, r.P3))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2, T3>(r.P1, r.P2, r.P3))));
		}
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer getters with four keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerGetterBuffer<T1, T2, T3, T4> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2, T3, T4}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2, T3, T4> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.P4 = access.Parameter4;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2, T3, T4>(r.P1, r.P2, r.P3, r.P4))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerGetterAccess<T1, T2, T3, T4>(r.P1, r.P2, r.P3, r.P4))));
		}
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3) && match4.Matches(r.P4))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;
		public T4 P4 = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer setters with a single key.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerSetterBuffer<T1, TValue> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, TValue value)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.Value = value;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, TValue> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.Value = access.TypedValue;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, TValue>(r.P1, r.Value))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, TValue>(r.P1, r.Value))));
		}
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose key and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && matchValue.Matches(r.Value))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public TValue Value = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer setters with two keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerSetterBuffer<T1, T2, TValue> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, TValue value)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.Value = value;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, TValue> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.Value = access.TypedValue;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, TValue>(r.P1, r.P2, r.Value))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, TValue>(r.P1, r.P2, r.Value))));
		}
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2) && matchValue.Matches(r.Value))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
		public TValue Value = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer setters with three keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerSetterBuffer<T1, T2, T3, TValue> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.Value = value;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, T3, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, T3, TValue> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.Value = access.TypedValue;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, T3, TValue>(r.P1, r.P2, r.P3, r.Value))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, T3, TValue>(r.P1, r.P2, r.P3, r.Value))));
		}
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3) && matchValue.Matches(r.Value))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;
		public TValue Value = default!;
	}
}

/// <summary>
///     Per-member buffer for indexer setters with four keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		r.Value = value;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, T3, T4, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.P4 = access.Parameter4;
		r.Value = access.TypedValue;
		r.Boxed = access;
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, T3, T4, TValue>(r.P1, r.P2, r.P3, r.P4, r.Value))));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new IndexerSetterAccess<T1, T2, T3, T4, TValue>(r.P1, r.P2, r.P3, r.P4, r.Value))));
		}
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			if (match1.Matches(r.P1) && match2.Matches(r.P2) && match3.Matches(r.P3) && match4.Matches(r.P4) && matchValue.Matches(r.Value))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;
		public T4 P4 = default!;
		public TValue Value = default!;
	}
}
