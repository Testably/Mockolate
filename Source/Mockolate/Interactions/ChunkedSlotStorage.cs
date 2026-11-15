using System;
using System.Threading;

namespace Mockolate.Interactions;

/// <summary>
///     Lock-free, append-only slot storage for <see cref="Internals.IFastMemberBuffer" /> implementations.
///     Records live in chunks that never move; growing only adds a chunk, never copies record
///     data. This eliminates the orphaned-write race that arose when an array-doubling buffer
///     snapshot the current array while a concurrent writer was still mid-write to a slot inside it.
/// </summary>
/// <remarks>
///     Chunk 0 holds only slot 0 and every later chunk holds <see cref="ChunkSize" /> slots, so a member called
///     once (the common case in a unit test) only pays for a single slot.
///     A writer reserves a unique slot via <see cref="Reserve" />, obtains its destination via
///     <see cref="SlotForWrite" />, writes its fields, then calls <see cref="Publish" />.
///     Chunk 0 is installed without the lock, and also by any write into a later chunk, so a reader never finds
///     it missing below a published slot. The array referencing the later chunks is allocated lazily
///     and doubles under the lock when needed. That grow only copies chunk references (which are stable
///     once installed), so a concurrent writer to an existing chunk cannot lose its data.
///     The verified flags live in a separate flat array indexed by slot that is created by the first
///     <see cref="VerifiedUnderLock" /> call, so a member that is never verified does not pay for them.
///     They are only accessed under <see cref="Lock" />, so growing that array by copying is safe.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class ChunkedSlotStorage<TRecord> where TRecord : struct
{
	internal const int ChunkShift = 4;
	internal const int ChunkSize = 1 << ChunkShift;
	internal const int ChunkMask = ChunkSize - 1;

	internal readonly MockolateLock Lock = new();
	private TRecord[]? _firstChunk;
	private TRecord[]?[]? _laterChunks;
	private bool[]? _verified;
	private int _reserved;
	private int _published;

	/// <summary>The number of slots that have been fully written and published.</summary>
	public int Count => Volatile.Read(ref _published);

	/// <summary>The number of slots fully written. Caller must hold <see cref="Lock" /> for a stable read.</summary>
	public int PublishedUnderLock => Volatile.Read(ref _published);

	/// <summary>Atomically reserves the next unique slot index.</summary>
	public int Reserve() => Interlocked.Increment(ref _reserved) - 1;

	/// <summary>Marks the slot reserved by <see cref="Reserve" /> as fully written.</summary>
	public void Publish() => Interlocked.Increment(ref _published);

	/// <summary>
	///     Returns a writable reference to the storage cell for <paramref name="slot" />, allocating
	///     the backing chunk if it has not been installed yet.
	/// </summary>
	public ref TRecord SlotForWrite(int slot)
	{
		int chunkIdx = ChunkIndex(slot, out int offset);
		TRecord[]? chunk;
		if (chunkIdx == 0)
		{
			chunk = Volatile.Read(ref _firstChunk);
			if (chunk is null)
			{
				TRecord[] created = new TRecord[1];
				chunk = Interlocked.CompareExchange(ref _firstChunk, created, null) ?? created;
			}
		}
		else
		{
			TRecord[]?[]? chunks = Volatile.Read(ref _laterChunks);
			chunk = chunks is not null && chunkIdx <= chunks.Length
				? Volatile.Read(ref chunks[chunkIdx - 1])
				: null;
			chunk ??= EnsureLaterChunk(chunkIdx);
		}

		return ref chunk[offset];
	}

	/// <summary>
	///     Returns a read-only reference to the cell for <paramref name="slot" />. Callers must hold
	///     <see cref="Lock" /> and only enumerate slots in <c>[0, <see cref="PublishedUnderLock" />)</c>;
	///     those slots are guaranteed to have an installed chunk.
	/// </summary>
	public ref TRecord SlotUnderLock(int slot)
	{
		int chunkIdx = ChunkIndex(slot, out int offset);
		TRecord[] chunk = chunkIdx == 0 ? _firstChunk! : _laterChunks![chunkIdx - 1]!;
		return ref chunk[offset];
	}

	/// <summary>
	///     Returns a writable reference to the per-slot verified flag, creating the verified storage on first use.
	///     Callers must hold <see cref="Lock" /> and only pass slots in <c>[0, <see cref="PublishedUnderLock" />)</c>.
	/// </summary>
	public ref bool VerifiedUnderLock(int slot)
	{
		bool[]? verified = _verified;
		if (verified is null || slot >= verified.Length)
		{
			bool[] bigger = new bool[Math.Max(_published, 2 * (verified?.Length ?? 0))];
			if (verified is not null)
			{
				Array.Copy(verified, bigger, verified.Length);
			}

			_verified = verified = bigger;
		}

		return ref verified[slot];
	}

	/// <summary>
	///     Returns whether <paramref name="slot" /> is marked as verified, without creating the verified storage.
	///     Callers must hold <see cref="Lock" />.
	/// </summary>
	public bool IsVerifiedUnderLock(int slot) => _verified is { } verified && slot < verified.Length && verified[slot];

	/// <summary>
	///     Resets the buffer to an empty state. Caller must not hold <see cref="Lock" /> on entry.
	/// </summary>
	public void Clear()
	{
		lock (Lock)
		{
			int n = _published;
			for (int slot = 0; slot < n; slot++)
			{
				SlotUnderLock(slot) = default;
			}

			_verified = null;
			_reserved = 0;
			Volatile.Write(ref _published, 0);
		}
	}

	/// <summary>
	///     Maps <paramref name="slot" /> to its chunk index and the <paramref name="offset" /> inside that chunk.
	/// </summary>
	internal static int ChunkIndex(int slot, out int offset)
	{
		if (slot == 0)
		{
			offset = 0;
			return 0;
		}

		int laterSlot = slot - 1;
		offset = laterSlot & ChunkMask;
		return (laterSlot >> ChunkShift) + 1;
	}

	private TRecord[] EnsureLaterChunk(int chunkIdx)
	{
		int index = chunkIdx - 1;
		lock (Lock)
		{
			if (Volatile.Read(ref _firstChunk) is null)
			{
				Interlocked.CompareExchange(ref _firstChunk, new TRecord[1], null);
			}

			TRecord[]?[]? chunks = _laterChunks;
			if (chunks is null || index >= chunks.Length)
			{
				int newLen = chunks?.Length ?? 1;
				while (index >= newLen)
				{
					newLen *= 2;
				}

				TRecord[]?[] biggerChunks = new TRecord[newLen][];
				if (chunks is not null)
				{
					Array.Copy(chunks, biggerChunks, chunks.Length);
				}

				chunks = biggerChunks;
				Volatile.Write(ref _laterChunks, chunks);
			}

			TRecord[]? chunk = chunks[index];
			if (chunk is null)
			{
				chunk = new TRecord[ChunkSize];
				Volatile.Write(ref chunks[index], chunk);
			}

			return chunk;
		}
	}
}
