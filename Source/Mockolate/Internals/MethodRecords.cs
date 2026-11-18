using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Records and typed view for parameterless methods. Records only the method name + sequence number.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodRecords : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public MethodRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		owner.Push(r);
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;

		protected override IInteraction CreateInteraction()
			=> new MethodInvocation(Name);
	}
}

/// <summary>
///     Records and typed view for 1-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodRecords<T1> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public MethodRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name, T1 parameter1)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		r.P1 = parameter1;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded calls whose parameter satisfies <paramref name="match1" />, marking each
	///     matched record as verified so it is skipped by a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk. Walks the typed storage in
	///     place and never allocates an <see cref="IInteraction" />, so count-only verification is
	///     allocation-free in the common case.
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

	/// <summary>
	///     Literal-value variant of <see cref="ConsumeMatching(IParameterMatch{T1})" /> — compares each
	///     recorded parameter against <paramref name="value1" /> using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call <see cref="IParameterMatch{T1}" />
	///     allocation that the matcher overload requires.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1)
	{
		int matches = 0;
		EqualityComparer<T1> cmp = EqualityComparer<T1>.Default;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			Record r = (Record)node;
			if (cmp.Equals(value1, r.P1))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;
		public T1 P1 = default!;

		protected override IInteraction CreateInteraction()
			=> new MethodInvocation<T1>(Name, P1);
	}
}

/// <summary>
///     Records and typed view for 2-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodRecords<T1, T2> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public MethodRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name, T1 parameter1, T2 parameter2)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     record as verified. Allocation-free fast path for count-only verification; mirrors
	///     <see cref="MethodRecords{T1}.ConsumeMatching(IParameterMatch{T1})" />.
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

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2})" /> — compares each
	///     recorded parameter against <paramref name="value1" /> / <paramref name="value2" /> using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			Record r = (Record)node;
			if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;
		public T1 P1 = default!;
		public T2 P2 = default!;

		protected override IInteraction CreateInteraction()
			=> new MethodInvocation<T1, T2>(Name, P1, P2);
	}
}

/// <summary>
///     Records and typed view for 3-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodRecords<T1, T2, T3> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public MethodRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name, T1 parameter1, T2 parameter2, T3 parameter3)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     record as verified. Allocation-free fast path for count-only verification.
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

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2}, IParameterMatch{T3})" />
	///     — compares each recorded parameter against the supplied values using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2, T3 value3)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		EqualityComparer<T3> cmp3 = EqualityComparer<T3>.Default;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			Record r = (Record)node;
			if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2) && cmp3.Equals(value3, r.P3))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;

		protected override IInteraction CreateInteraction()
			=> new MethodInvocation<T1, T2, T3>(Name, P1, P2, P3);
	}
}

/// <summary>
///     Records and typed view for 4-parameter methods.
/// </summary>
[DebuggerDisplay("{Count} method calls")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MethodRecords<T1, T2, T3, T4> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public MethodRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		r.P1 = parameter1;
		r.P2 = parameter2;
		r.P3 = parameter3;
		r.P4 = parameter4;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded calls whose parameters satisfy the supplied matchers, marking each matched
	///     record as verified. Allocation-free fast path for count-only verification.
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

	/// <summary>
	///     Literal-value variant of
	///     <see cref="ConsumeMatching(IParameterMatch{T1}, IParameterMatch{T2}, IParameterMatch{T3}, IParameterMatch{T4})" />
	///     — compares each recorded parameter against the supplied values using
	///     <see cref="EqualityComparer{T}.Default" />, sparing the per-call matcher allocations.
	/// </summary>
	public int ConsumeMatchingLiteral(T1 value1, T2 value2, T3 value3, T4 value4)
	{
		int matches = 0;
		EqualityComparer<T1> cmp1 = EqualityComparer<T1>.Default;
		EqualityComparer<T2> cmp2 = EqualityComparer<T2>.Default;
		EqualityComparer<T3> cmp3 = EqualityComparer<T3>.Default;
		EqualityComparer<T4> cmp4 = EqualityComparer<T4>.Default;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			Record r = (Record)node;
			if (cmp1.Equals(value1, r.P1) && cmp2.Equals(value2, r.P2) &&
			    cmp3.Equals(value3, r.P3) && cmp4.Equals(value4, r.P4))
			{
				matches++;
				node.Verified = true;
			}
		}

		return matches;
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;
		public T1 P1 = default!;
		public T2 P2 = default!;
		public T3 P3 = default!;
		public T4 P4 = default!;

		protected override IInteraction CreateInteraction()
			=> new MethodInvocation<T1, T2, T3, T4>(Name, P1, P2, P3, P4);
	}
}
