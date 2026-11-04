using System;
using System.Threading;

namespace Mockolate.Interactions;

/// <summary>
///     Lock-free, append-only slot storage for <see cref="Internals.IFastMemberBuffer" /> implementations.
///     Records live in fixed-size chunks; growing only adds a chunk reference, never copies record
///     data. This eliminates the orphaned-write race that arose when an array-doubling buffer
///     snapshot the current array while a concurrent writer was still mid-write to a slot inside it.
/// </summary>
/// <remarks>
///     Slot N is stored at <c>chunks[N &gt;&gt; <see cref="ChunkShift" />][N &amp; <see cref="ChunkMask" />]</c>.
///     A writer reserves a unique slot via <see cref="Reserve" />, obtains its destination via
///     <see cref="SlotForWrite" />, writes its fields, then calls <see cref="Publish" />.
///     The chunks array itself is allocated lazily on the first <see cref="SlotForWrite" /> and
///     doubles when a new chunk index is past its end; that grow only copies chunk references
///     (which are stable once installed), so a concurrent writer to an existing chunk cannot lose
///     its data. A buffer that is never written into stays allocation-free past its own header.
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
	internal TRecord[]?[]? Chunks;
	internal bool[]?[]? VerifiedChunks;
	private int _reserved;
	private int _published;

	/// <summary>The number of slots that have been fully written and published.</summary>
	public int Count => Volatile.Read(ref _published);

	/// <summary>The number of slots fully written. Caller must hold <see cref="Lock" /> for a stable read.</summary>
	public int PublishedUnderLock => _published;

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
		int chunkIdx = slot >> ChunkShift;
		int offset = slot & ChunkMask;
		TRecord[]?[]? chunks = Volatile.Read(ref Chunks);
		TRecord[]? chunk = chunks is not null && chunkIdx < chunks.Length
			? Volatile.Read(ref chunks[chunkIdx])
			: null;
		if (chunk is null)
		{
			chunk = EnsureChunk(chunkIdx);
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
		int chunkIdx = slot >> ChunkShift;
		int offset = slot & ChunkMask;
		return ref Chunks![chunkIdx]![offset];
	}

	/// <summary>
	///     Returns a writable reference to the per-slot verified flag. Callers must hold <see cref="Lock" />.
	/// </summary>
	public ref bool VerifiedUnderLock(int slot)
	{
		int chunkIdx = slot >> ChunkShift;
		int offset = slot & ChunkMask;
		return ref VerifiedChunks![chunkIdx]![offset];
	}

	/// <summary>
	///     Resets the buffer to an empty state. Caller must not hold <see cref="Lock" /> on entry.
	/// </summary>
	public void Clear()
	{
		lock (Lock)
		{
			int n = _published;
			if (n > 0)
			{
				TRecord[]?[] chunks = Chunks!;
				bool[]?[] verified = VerifiedChunks!;
				for (int slot = 0; slot < n; slot++)
				{
					int chunkIdx = slot >> ChunkShift;
					int offset = slot & ChunkMask;
					chunks[chunkIdx]![offset] = default;
					verified[chunkIdx]![offset] = false;
				}
			}

			_reserved = 0;
			Volatile.Write(ref _published, 0);
		}
	}

	private TRecord[] EnsureChunk(int chunkIdx)
	{
		lock (Lock)
		{
			TRecord[]?[]? chunks = Chunks;
			if (chunks is null)
			{
				int initialLen = 1;
				while (chunkIdx >= initialLen)
				{
					initialLen *= 2;
				}

				chunks = new TRecord[initialLen][];
				VerifiedChunks = new bool[initialLen][];
				Volatile.Write(ref Chunks, chunks);
			}
			else if (chunkIdx >= chunks.Length)
			{
				int newLen = chunks.Length;
				while (chunkIdx >= newLen)
				{
					newLen *= 2;
				}

				TRecord[]?[] biggerChunks = new TRecord[newLen][];
				Array.Copy(chunks, biggerChunks, chunks.Length);
				bool[]?[] biggerVerified = new bool[newLen][];
				Array.Copy(VerifiedChunks!, biggerVerified, VerifiedChunks!.Length);
				chunks = biggerChunks;
				VerifiedChunks = biggerVerified;
				Volatile.Write(ref Chunks, chunks);
			}

			TRecord[]? chunk = chunks[chunkIdx];
			if (chunk is null)
			{
				chunk = new TRecord[ChunkSize];
				VerifiedChunks![chunkIdx] = new bool[ChunkSize];
				Volatile.Write(ref chunks[chunkIdx], chunk);
			}

			return chunk;
		}
	}
}
