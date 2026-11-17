using System.Collections.Generic;
using System.Diagnostics;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Interactions;

/// <summary>
///     Per-member buffer for property getters. The buffer is bound to a single property; every
///     recorded record stores only a sequence number and shares a single
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
internal sealed class FastPropertyGetterBuffer : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly PropertyGetterAccess _access;

	/// <summary>
	///     Creates a new property-getter buffer pre-seeded with the shared
	///     <see cref="PropertyGetterAccess" /> <paramref name="access" /> singleton.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	/// <param name="access">The shared <see cref="PropertyGetterAccess" /> singleton emitted by the source generator.</param>
	public FastPropertyGetterBuffer(FastMockInteractions owner, PropertyGetterAccess access)
	{
		_owner = owner;
		_access = access;
	}

	/// <summary>
	///     Records a property getter access using the buffer's pre-seeded
	///     <see cref="PropertyGetterAccess" /> singleton.
	/// </summary>
	public void Append()
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		Push(r, epoch);

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		PropertyGetterAccess access = _access;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			Record r = (Record)node;
			dest.Add((r.Seq, access));
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		PropertyGetterAccess access = _access;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			if (node.Verified)
			{
				continue;
			}

			Record r = (Record)node;
			dest.Add((r.Seq, access));
		}
	}

	/// <summary>
	///     Returns the number of recorded property getter accesses and marks every currently-published
	///     slot as verified so a later <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk
	///     skips them. The name reflects the side effect: this is a <c>Count</c> + <c>MarkVerified</c>
	///     step, exposed for symmetry with
	///     <see cref="FastPropertySetterBuffer{T}.ConsumeMatching(IParameterMatch{T})" />.
	/// </summary>
	public int ConsumeMatching()
	{
		int n = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
		{
			node.Verified = true;
			n++;
		}

		return n;
	}

	private sealed class Record : RecordNode
	{
	}
}

/// <summary>
///     Per-member buffer for property setters. Records the property name and the assigned value.
/// </summary>
[DebuggerDisplay("{Count} property sets")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class FastPropertySetterBuffer<T> : RecordList, IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;

	/// <summary>
	///     Creates a new property-setter buffer attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	public FastPropertySetterBuffer(FastMockInteractions owner)
	{
		_owner = owner;
	}

	/// <summary>
	///     Records a property setter access.
	/// </summary>
	public void Append(string name, T value)
	{
		int epoch = Epoch;
		Record r = new();
		r.Seq = _owner.NextSequence();
		r.Name = name;
		r.Value = value;
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
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new PropertySetterAccess<T>(r.Name, r.Value))));
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
			dest.Add((r.Seq, r.Boxed ?? SetBoxed(r, new PropertySetterAccess<T>(r.Name, r.Value))));
		}
	}

	/// <summary>
	///     Counts recorded setter accesses whose assigned value satisfies <paramref name="match" />,
	///     marking each matched slot as verified so a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk skips them. The name
	///     reflects the side effect: this is a <c>Count</c> + <c>MarkVerified</c> step, not a pure read.
	/// </summary>
	public int ConsumeMatching(IParameterMatch<T> match)
	{
		int matches = 0;
		for (RecordNode? node = First; node is not null; node = node.Next)
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
	}
}
