using System.ComponentModel;
using System.Threading;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Lock-free, append-only list of the calls recorded for one mocked member. Base type of the
///     <see cref="IFastMemberBuffer" /> implementations of the library and of the generated code.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
///     <para />
///     Each call is one <see cref="RecordNode" />, fully written before a single compare-and-swap publishes it as the
///     new head, so readers walk the list without a lock and never see a partially written record. The list is
///     newest first; readers that need the call order sort by <see cref="RecordNode.Seq" />.
///     <para />
///     <see cref="Clear" /> drops every call that started before it: a writer captures <see cref="Epoch" /> before it
///     takes its sequence number and <see cref="Push" /> discards the node when a clear happened in between. Each clear
///     installs a new empty head, so a writer that read the head before the clear cannot publish over it.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class RecordList
{
	private RecordNode? _head;
	private int _epoch;

	/// <summary>The number of recorded calls. It walks the list, which keeps a counter out of every record.</summary>
	public int Count
	{
		get
		{
			int count = 0;
			for (RecordNode? node = First; node is not null; node = node.Next)
			{
				count++;
			}

			return count;
		}
	}

	/// <summary>
	///     The clear epoch, read by a writer before it takes its sequence number and passed to <see cref="Push" />.
	/// </summary>
	protected int Epoch => Volatile.Read(ref _epoch);

	/// <summary>The newest recorded call, or <see langword="null" /> if there is none.</summary>
	protected RecordNode? First => Volatile.Read(ref _head) is { } head and not EmptyHead ? head : null;

	/// <summary>
	///     Publishes the fully written <paramref name="node" /> as the newest call, unless <see cref="Clear" /> ran since
	///     <paramref name="epoch" /> was read.
	/// </summary>
	protected void Push(RecordNode node, int epoch)
	{
		RecordNode? head = Volatile.Read(ref _head);
		while (Volatile.Read(ref _epoch) == epoch)
		{
			node.Next = head is EmptyHead ? null : head;
			RecordNode? previous = Interlocked.CompareExchange(ref _head, node, head);
			if (ReferenceEquals(previous, head))
			{
				return;
			}

			head = previous;
		}
	}

	/// <summary>
	///     Returns the boxed interaction of <paramref name="node" />, storing <paramref name="boxed" /> if it has none
	///     yet, so concurrent readers agree on a single instance.
	/// </summary>
	protected static IInteraction SetBoxed(RecordNode node, IInteraction boxed)
		=> node.SetBoxed(boxed);

	/// <summary>
	///     Removes every recorded call and drops the calls that are still being recorded.
	/// </summary>
	public void Clear()
	{
		Interlocked.Increment(ref _epoch);
		Volatile.Write(ref _head, new EmptyHead());
	}

	private sealed class EmptyHead : RecordNode;
}

/// <summary>
///     A call recorded in a <see cref="RecordList" />.
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
	private IInteraction? _boxed;
	private int _seq;

	/// <summary>
	///     The mock-wide sequence number of the call. It is stored in 32 bits, like the interaction count of the
	///     mock, so that it shares a slot with <see cref="Verified" />.
	/// </summary>
	public long Seq
	{
		get => _seq;
		set => _seq = (int)value;
	}

	/// <summary>The next older call, or <see langword="null" />.</summary>
	public RecordNode? Next { get; internal set; }

	/// <summary>Whether a verification has matched the call.</summary>
	public bool Verified { get; set; }

	/// <summary>The boxed interaction, created lazily on enumeration.</summary>
	public IInteraction? Boxed
	{
		get => _boxed;
		internal set => _boxed = value;
	}

	internal IInteraction SetBoxed(IInteraction boxed)
		=> Interlocked.CompareExchange(ref _boxed, boxed, null) ?? boxed;
}
