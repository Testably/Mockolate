using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Mockolate.Internals;

namespace Mockolate.Interactions;

/// <summary>
///     Distinguishes whether a <see cref="FastEventBuffer" /> records subscribe or unsubscribe calls.
/// </summary>
internal enum FastEventBufferKind
{
	/// <summary>The buffer records event subscriptions.</summary>
	Subscribe,

	/// <summary>The buffer records event unsubscriptions.</summary>
	Unsubscribe,
}

/// <summary>
///     Per-member buffer for event subscribe or unsubscribe access. Each record carries its kind, so that it
///     produces an <see cref="EventSubscription" /> or an <see cref="EventUnsubscription" /> as appropriate.
/// </summary>
[DebuggerDisplay("{Count} event accesses")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class FastEventBuffer : RecordList
{
	/// <summary>
	///     Creates a view that reads the recorded subscriptions or unsubscriptions of an event.
	/// </summary>
	public FastEventBuffer(FastMockInteractions owner, int memberId) : base(owner, memberId)
	{
	}

	/// <summary>
	///     Records a call of the member <paramref name="memberId" /> in the <paramref name="owner" />, without
	///     the need for a buffer instance.
	/// </summary>
	public static void Append(FastMockInteractions owner, int memberId, FastEventBufferKind kind, string name,
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
		public FastEventBufferKind Kind;

		protected override IInteraction CreateInteraction()
			=> Kind == FastEventBufferKind.Subscribe
				? new EventSubscription(Name, Target, Method)
				: new EventUnsubscription(Name, Target, Method);
	}
}
