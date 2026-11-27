using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Records and typed view for indexer getters with a single key.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerGetterRecords<T1> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerGetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		owner.Push(r);
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerGetterAccess<T1> access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = access.Parameter1;
		r.Boxed = access;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded indexer getter accesses whose key satisfies <paramref name="match1" />,
	///     marking each matched record as verified. Allocation-free fast path for count-only verification.
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
///     Records and typed view for indexer getters with two keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerGetterRecords<T1, T2> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerGetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		owner.Push(r);
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerGetterAccess<T1, T2> access)
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
	///     each matched record as verified. Allocation-free fast path for count-only verification.
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
///     Records and typed view for indexer getters with three keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerGetterRecords<T1, T2, T3> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerGetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		owner.Push(r);
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerGetterAccess<T1, T2, T3> access)
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
	///     each matched record as verified. Allocation-free fast path for count-only verification.
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
///     Records and typed view for indexer getters with four keys.
/// </summary>
[DebuggerDisplay("{Count} indexer gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerGetterRecords<T1, T2, T3, T4> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerGetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
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
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerGetterAccess<T1, T2, T3, T4> access)
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
	///     each matched record as verified. Allocation-free fast path for count-only verification.
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
///     Records and typed view for indexer setters with a single key.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerSetterRecords<T1, TValue> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerSetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerSetterAccess<T1, TValue> access)
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
	///     matchers, marking each matched record as verified. Allocation-free fast path for count-only
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
///     Records and typed view for indexer setters with two keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerSetterRecords<T1, T2, TValue> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerSetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2, TValue value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, TValue> access)
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
	///     matchers, marking each matched record as verified. Allocation-free fast path for count-only
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
///     Records and typed view for indexer setters with three keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerSetterRecords<T1, T2, T3, TValue> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerSetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
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
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, T3, TValue> access)
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
	///     matchers, marking each matched record as verified. Allocation-free fast path for count-only
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
///     Records and typed view for indexer setters with four keys.
/// </summary>
[DebuggerDisplay("{Count} indexer sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IndexerSetterRecords<T1, T2, T3, T4, TValue> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public IndexerSetterRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
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
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, IndexerSetterAccess<T1, T2, T3, T4, TValue> access)
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
	///     matchers, marking each matched record as verified. Allocation-free fast path for count-only
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
