using System.Collections.Generic;
using System.Diagnostics;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Interactions;

/// <summary>
///     Records and typed view for property getters. A record carries no data of its own; all records of a
///     property share a single
///     <see cref="PropertyGetterAccess" /> when boxed for verification. Sharing one reference
///     across records is safe because property getters carry no parameters — every recorded
///     access is semantically identical — and the two reference-keyed verification paths in
///     this codebase tolerate it: <c>FastMockInteractions._verified</c> filters all-or-nothing
///     per matched property, and <c>VerificationResultExtensions.Then</c> walks the snapshot
///     positionally so repeated occurrences of the same reference still resolve to distinct
///     positions.
/// </summary>
[DebuggerDisplay("{Count} property gets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class PropertyGetterRecords : MemberRecords
{
	/// <summary>
	///     Creates a view that reads the recorded accesses of a property getter.
	/// </summary>
	internal PropertyGetterRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, PropertyGetterAccess access)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Boxed = access;
		owner.Push(r);
	}

	private sealed class Record : RecordNode
	{
		protected override IInteraction CreateInteraction()
			=> Boxed!;
	}
}

/// <summary>
///     Records and typed view for property setters. Records the property name and the assigned value.
/// </summary>
[DebuggerDisplay("{Count} property sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class PropertySetterRecords<T> : MemberRecords
{
	/// <summary>
	///     Creates a view on the calls of the member <paramref name="memberId" /> that <paramref name="owner" /> recorded.
	/// </summary>
	/// <param name="owner">The interactions of the mock that the calls are recorded in.</param>
	/// <param name="memberId">The id of the member whose calls the view reads.</param>
	public PropertySetterRecords(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, string name, T value)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Name = name;
		r.Value = value;
		owner.Push(r);
	}

	/// <summary>
	///     Counts recorded setter accesses whose assigned value satisfies <paramref name="match" />,
	///     marking each matched record as verified so a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk skips them. The name
	///     reflects the side effect: this is a <c>Count</c> + <c>MarkVerified</c> step, not a pure read.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T> match)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			Record r = (Record)node;
			if (match.Matches(r.Value))
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
		public T Value = default!;

		protected override IInteraction CreateInteraction()
			=> new PropertySetterAccess<T>(Name, Value);
	}
}
