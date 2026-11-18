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
	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1)
		=> Append(Owner, MemberId, parameter1);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1}" />.
	/// </summary>
	/// <remarks>
	///     The generator allocates the access value to use as a setup-matching key; this overload reuses
	///     it as the pre-boxed record so enumeration doesn't allocate a second one.
	/// </remarks>
	public void Append(IndexerGetterAccess<T1> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerGetterAccess<T1> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose key satisfies <paramref name="match1" />,
	///     marking each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerGetterAccess<T1>(P1);
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
	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2)
		=> Append(Owner, MemberId, parameter1, parameter2);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerGetterAccess<T1, T2> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerGetterAccess<T1, T2>(P1, P2);
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
	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3)
		=> Append(Owner, MemberId, parameter1, parameter2, parameter3);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2, T3}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2, T3> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerGetterAccess<T1, T2, T3> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerGetterAccess<T1, T2, T3>(P1, P2, P3);
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
	/// <summary>
	///     Creates a new indexer-getter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerGetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer getter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
		=> Append(Owner, MemberId, parameter1, parameter2, parameter3, parameter4);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer getter access using an already-allocated <see cref="IndexerGetterAccess{T1, T2, T3, T4}" />.
	/// </summary>
	public void Append(IndexerGetterAccess<T1, T2, T3, T4> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerGetterAccess<T1, T2, T3, T4> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.P4 = access.Parameter4;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose keys satisfy the supplied matchers, marking
	///     each matched slot as verified. Allocation-free fast path for count-only verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerGetterAccess<T1, T2, T3, T4>(P1, P2, P3, P4);
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
	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, TValue value)
		=> Append(Owner, MemberId, parameter1, value);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, TValue> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerSetterAccess<T1, TValue> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.Value = access.TypedValue;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose key and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerSetterAccess<T1, TValue>(P1, Value);
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
	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, TValue value)
		=> Append(Owner, MemberId, parameter1, parameter2, value);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, TValue> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, TValue> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.Value = access.TypedValue;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerSetterAccess<T1, T2, TValue>(P1, P2, Value);
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
	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
		=> Append(Owner, MemberId, parameter1, parameter2, parameter3, value);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, T3, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, T3, TValue> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, T3, TValue> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.Value = access.TypedValue;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerSetterAccess<T1, T2, T3, TValue>(P1, P2, P3, Value);
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
	/// <summary>
	///     Creates a new indexer-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastIndexerSetterBuffer(FastMockInteractions owner) : base(owner)
	{
	}

	/// <summary>
	///     Records an indexer setter access.
	/// </summary>
	public void Append(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
		=> Append(Owner, MemberId, parameter1, parameter2, parameter3, parameter4, value);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records an indexer setter access using an already-allocated <see cref="IndexerSetterAccess{T1, T2, T3, T4, TValue}" />.
	/// </summary>
	public void Append(IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
		=> Append(Owner, MemberId, access);

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.P2 = access.Parameter2;
		r.P3 = access.Parameter3;
		r.P4 = access.Parameter4;
		r.Value = access.TypedValue;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer setter accesses whose keys and assigned value satisfy the supplied
	///     matchers, marking each matched slot as verified. Allocation-free fast path for count-only
	///     verification.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4, IParameterMatch<TValue> matchValue)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
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

		protected override IInteraction CreateInteraction()
			=> new IndexerSetterAccess<T1, T2, T3, T4, TValue>(P1, P2, P3, P4, Value);
	}
}
