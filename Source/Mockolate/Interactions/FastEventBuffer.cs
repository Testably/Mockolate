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
internal sealed class FastEventBuffer : IFastMemberBuffer
{
	private readonly FastMockInteractions _owner;
	private readonly FastEventBufferKind _kind;
	private readonly ChunkedSlotStorage<Record> _storage = new();

	/// <summary>
	///     Creates a new event buffer of the given <paramref name="kind" /> attached to <paramref name="owner" />.
	/// </summary>
	/// <param name="owner">The mock-wide <see cref="FastMockInteractions" /> the buffer publishes records into.</param>
	/// <param name="kind">Distinguishes a subscribe-recording buffer from an unsubscribe-recording buffer.</param>
	public FastEventBuffer(FastMockInteractions owner, FastEventBufferKind kind)
	{
		_owner = owner;
		_kind = kind;
	}

	/// <inheritdoc cref="IFastMemberBuffer.Count" />
	public int Count => _storage.Count;

	/// <summary>
	///     Records an event subscription or unsubscription.
	/// </summary>
	public void Append(string name, object? target, MethodInfo method)
	{
		long seq = _owner.NextSequence();
		int slot = _storage.Reserve();
		ref Record r = ref _storage.SlotForWrite(slot);
		r.Seq = seq;
		r.Name = name;
		r.Target = target;
		r.Method = method;
		r.Boxed = null;
		_storage.Publish();

		if (_owner.HasInteractionAddedSubscribers)
		{
			_owner.RaiseAdded();
		}
	}

	/// <inheritdoc cref="IFastMemberBuffer.Clear" />
	public void Clear() => _storage.Clear();

	void IFastMemberBuffer.AppendBoxed(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= _kind == FastEventBufferKind.Subscribe
					? new EventSubscription(r.Name, r.Target, r.Method)
					: new EventUnsubscription(r.Name, r.Target, r.Method);
				dest.Add((r.Seq, r.Boxed));
			}
		}
	}

	void IFastMemberBuffer.AppendBoxedUnverified(List<(long Seq, IInteraction Interaction)> dest)
	{
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				if (_storage.VerifiedUnderLock(slot))
				{
					continue;
				}

				ref Record r = ref _storage.SlotUnderLock(slot);
				r.Boxed ??= _kind == FastEventBufferKind.Subscribe
					? new EventSubscription(r.Name, r.Target, r.Method)
					: new EventUnsubscription(r.Name, r.Target, r.Method);
				dest.Add((r.Seq, r.Boxed));
			}
		}
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
		lock (_storage.Lock)
		{
			int n = _storage.PublishedUnderLock;
			for (int slot = 0; slot < n; slot++)
			{
				_storage.VerifiedUnderLock(slot) = true;
			}

			return n;
		}
	}

	internal struct Record
	{
		public long Seq;
		public string Name;
		public object? Target;
		public MethodInfo Method;
		public IInteraction? Boxed;
	}
}
