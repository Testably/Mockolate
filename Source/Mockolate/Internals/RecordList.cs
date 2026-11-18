using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Typed view on the calls that a <see cref="FastMockInteractions" /> recorded for one mocked member.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
///     <para />
///     All calls of a mock are recorded in one list, so that the first call of a member needs no storage of its own
///     and the calls need no sequence counter to keep their order. A view has no state besides the mock and the
///     member id: it walks the list of the mock and skips the calls of other members.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class RecordList
{
	/// <inheritdoc cref="RecordList" />
	protected RecordList(FastMockInteractions owner, int memberId)
	{
		Owner = owner;
		MemberId = memberId;
	}

	/// <summary>The interactions of the mock that the calls are recorded in.</summary>
	internal FastMockInteractions Owner { get; }

	/// <summary>The member id of the calls.</summary>
	public int MemberId { get; }

	/// <summary>The number of recorded calls of the member. It walks the list of the mock.</summary>
	public int Count
	{
		get
		{
			int count = 0;
			for (RecordNode? node = First; node is not null; node = Following(node))
			{
				count++;
			}

			return count;
		}
	}

	/// <summary>Whether a call of the member was recorded.</summary>
	public bool HasRecords => First is not null;

	/// <summary>The newest recorded call of the member, or <see langword="null" /> if there is none.</summary>
	protected RecordNode? First => Skip(Owner.First);

	/// <summary>The next older call of the member after <paramref name="node" />, or <see langword="null" />.</summary>
	protected RecordNode? Following(RecordNode node) => Skip(node.Next);

	/// <summary>
	///     Marks every recorded call of the member as verified and returns their number, so that a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> skips them.
	/// </summary>
	internal int ConsumeAll()
	{
		int count = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			node.Verified = true;
			count++;
		}

		return count;
	}
	/// <summary>
	///     Appends the interaction of each recorded call of the member to <paramref name="dest" />, newest first.
	/// </summary>
	internal void AppendBoxed(List<IInteraction> dest)
	{
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			dest.Add(node.Interaction);
		}
	}

	private RecordNode? Skip(RecordNode? node)
	{
		int memberId = MemberId;
		while (node is not null && node.MemberId != memberId)
		{
			node = node.Next;
		}

		return node;
	}
}

/// <summary>
///     A call recorded in a <see cref="FastMockInteractions" />.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class RecordNode
{
	internal const int InteractionMemberId = int.MaxValue;
	private const int VerifiedFlag = int.MinValue;

	private IInteraction? _boxed;
	private int _seq;
	private int _memberIdAndVerified;

	/// <summary>
	///     The position of the call in the mock, counted from zero. It is stored in 32 bits, like the interaction
	///     count of the mock.
	/// </summary>
	public long Seq
	{
		get => _seq;
		internal set => _seq = (int)value;
	}

	/// <summary>
	///     The id of the member that was called. It is not negative, because it shares its storage with
	///     <see cref="Verified" />, so that the record needs no space of its own for either.
	/// </summary>
	public int MemberId
	{
		get => _memberIdAndVerified & ~VerifiedFlag;
		set => _memberIdAndVerified = value;
	}

	/// <summary>The next older call of the mock, or <see langword="null" />.</summary>
	public RecordNode? Next { get; internal set; }

	/// <summary>Whether a verification has matched the call.</summary>
	public bool Verified
	{
		get => _memberIdAndVerified < 0;
		set => _memberIdAndVerified = value
			? _memberIdAndVerified | VerifiedFlag
			: _memberIdAndVerified & ~VerifiedFlag;
	}

	/// <summary>
	///     The interaction of the call, if it was already created or the call was recorded with one.
	/// </summary>
	public IInteraction? Boxed
	{
		get => _boxed;
		set => _boxed = value;
	}

	/// <summary>
	///     The interaction of the call. It is created on first use and stored, so that concurrent readers agree on a
	///     single instance.
	/// </summary>
	public IInteraction Interaction
		=> _boxed ?? Interlocked.CompareExchange(ref _boxed, CreateInteraction(), null) ?? _boxed!;

	/// <summary>Creates the interaction of the call.</summary>
	protected abstract IInteraction CreateInteraction();
}

/// <summary>
///     A call that was recorded as an interaction, without a typed record.
/// </summary>
internal sealed class InteractionRecord : RecordNode
{
	public InteractionRecord(IInteraction interaction)
	{
		Boxed = interaction;
		MemberId = InteractionMemberId;
	}

	protected override IInteraction CreateInteraction()
		=> Boxed!;
}

/// <summary>
///     View on the calls of a member whose record type is not known.
/// </summary>
internal sealed class MemberRecords(FastMockInteractions owner, int memberId) : RecordList(owner, memberId);
