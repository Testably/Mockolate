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
///     Per-member buffer for event subscribe or unsubscribe access. The kind is fixed at construction
///     time so the boxed records produce <see cref="EventSubscription" /> or
///     <see cref="EventUnsubscription" /> as appropriate.
/// </summary>
[DebuggerDisplay("{Count} event {_kind}s")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
internal sealed class FastEventBuffer : RecordList, IFastMemberBuffer
{
	private readonly FastEventBufferKind _kind;

	/// <summary>
	///     Creates a new event buffer of the given <paramref name="kind" /> attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	/// <param name="kind">Distinguishes a subscribe-recording buffer from an unsubscribe-recording buffer.</param>
	public FastEventBuffer(FastMockInteractions owner, FastEventBufferKind kind) : base(owner)
	{
		_kind = kind;
	}

	/// <summary>
	///     Records an event subscription or unsubscription.
	/// </summary>
	public void Append(string name, object? target, MethodInfo method)
		=> Append(Owner, MemberId, _kind, name, target, method);

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

	/// <summary>
	///     Returns the number of recorded subscribe/unsubscribe accesses and marks every
	///     currently-published slot as verified so a later
	///     <see cref="IMockInteractions.GetUnverifiedInteractions" /> walk skips them.
	///     The name reflects the side effect: this is a <c>Count</c> + <c>MarkVerified</c> step,
	///     not a pure read.
	/// </summary>
	public int ConsumeMatching()
	{
		int n = 0;
		for (RecordNode? node = First; node is not null; node = Following(node))
		{
			node.Verified = true;
			n++;
		}

		return n;
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
