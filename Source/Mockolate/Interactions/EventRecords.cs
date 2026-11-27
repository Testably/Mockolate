using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Mockolate.Internals;

namespace Mockolate.Interactions;

/// <summary>
///     Distinguishes whether a <see cref="EventRecords" /> records subscribe or unsubscribe calls.
/// </summary>
internal enum EventRecordKind
{
	/// <summary>The record is an event subscription.</summary>
	Subscribe,

	/// <summary>The record is an event unsubscription.</summary>
	Unsubscribe,
}

/// <summary>
///     Records and typed view for event subscribe or unsubscribe access. Each record carries its kind, so that it
///     produces an <see cref="EventSubscription" /> or an <see cref="EventUnsubscription" /> as appropriate.
/// </summary>
[DebuggerDisplay("{Count} event accesses")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class EventRecords : MemberRecords
{
	/// <summary>
	///     Creates a view that reads the recorded subscriptions or unsubscriptions of an event.
	/// </summary>
	public EventRecords(MockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />.
	/// </summary>
	public static void Append(MockInteractions owner, int memberId, EventRecordKind kind, string name,
		object? target, MethodInfo method)
	{
		Record r = new();
		r.MemberId = memberId;
		r.Kind = kind;
		r.Name = name;
		r.Target = target;
		r.Method = method;
		owner.Push(r);
	}

	private sealed class Record : RecordNode
	{
		public string Name = null!;
		public object? Target;
		public MethodInfo Method = null!;
		public EventRecordKind Kind;

		protected override IInteraction CreateInteraction()
			=> Kind == EventRecordKind.Subscribe
				? new EventSubscription(Name, Target, Method)
				: new EventUnsubscription(Name, Target, Method);
	}
}
