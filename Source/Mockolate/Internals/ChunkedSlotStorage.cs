using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Mockolate.Internals;

/// <summary>
///     Lock-free, append-only slot storage for the <see cref="IFastMemberBuffer" /> implementations of the library
///     and of the generated code.
///     Records live in chunks that never move; growing only adds a chunk, never copies record
///     data. This eliminates the orphaned-write race that arose when an array-doubling buffer
///     snapshot the current array while a concurrent writer was still mid-write to a slot inside it.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
///     <para />
///     A writer reserves a unique slot in the current <see cref="Generation" /> via <see cref="Reserve" />, obtains its
///     destination via <see cref="Generation.SlotForWrite" />, writes its fields, then calls
///     <see cref="Generation.Publish" /> exactly once. The published count only advances over a contiguous run of
///     written slots, so a reader under the lock only sees fully written slots in
///     <c>[0, <see cref="PublishedUnderLock" />)</c>. A writer never spins on an earlier writer: a slot written
///     before an earlier one is parked under the lock and published by the writer that closes the gap. Because
///     readers hold the same lock while they run parameter matchers, a matcher that synchronously waits for another
///     thread to record a call to the same member can deadlock.
///     A slot that is reserved but never published, for example because the writer was aborted, interrupted or ran
///     out of memory between <see cref="Reserve" /> and <see cref="Generation.Publish" />, hides every later record
///     of the generation until the next <see cref="Clear" />.
///     <see cref="Clear" /> discards the current generation, so a writer that reserved its slot before the clear
///     completes in the discarded generation and neither delays nor overwrites the records written after it.
///     <para />
///     Slot 0 lives inline in the generation and every later chunk holds <see cref="ChunkSize" /> slots, so a member
///     called once (the common case in a unit test) only pays for a single slot. The array referencing the later
///     chunks is allocated lazily and doubles under the lock when needed. That grow only copies chunk references
///     (which are stable once installed), so a concurrent writer to an existing chunk cannot lose its data.
///     The verified flags live in a separate flat array indexed by slot that is created by the first
///     <see cref="VerifiedUnderLock" /> call, so a member that is never verified does not pay for them.
///     They are only accessed under the lock, so growing that array by copying is safe.
///     <para />
///     Generated code takes the lock via <see cref="EnterLock" />, whose signature is the same on every target
///     framework, so it works against any build of this library and excludes the library's own lock users.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ChunkedSlotStorage<TRecord> where TRecord : struct
{
	internal const int ChunkShift = 4;
	internal const int ChunkSize = 1 << ChunkShift;
	internal const int ChunkMask = ChunkSize - 1;

	private Generation? _generation;
	private bool[]? _verified;

	internal MockolateLock Lock { get; } = new();

	/// <summary>The number of slots that have been fully written and published.</summary>
	public int Count => Volatile.Read(ref _generation)?.Published ?? 0;

	/// <summary>The number of slots fully written. Caller must hold the lock for a stable read.</summary>
	public int PublishedUnderLock => Count;

	/// <summary>
	///     Takes the lock that readers hold while they enumerate or verify the published slots. Dispose the returned
	///     scope to release it.
	/// </summary>
	public LockScope EnterLock() => new(Lock);

	/// <summary>
	///     Atomically reserves the next unique <paramref name="slot" /> index and returns the generation it belongs to.
	///     The caller writes the slot via <see cref="Generation.SlotForWrite" /> and then calls
	///     <see cref="Generation.Publish" /> on the returned generation exactly once. Skipping or repeating the publish
	///     corrupts the published count.
	/// </summary>
	public Generation Reserve(out int slot)
	{
		Generation generation = Volatile.Read(ref _generation) ?? CreateGeneration();
		slot = generation.Reserve();
		return generation;
	}

	/// <summary>
	///     Returns a reference to the cell for <paramref name="slot" />. Callers must hold
	///     the lock and only enumerate slots in <c>[0, <see cref="PublishedUnderLock" />)</c>.
	/// </summary>
	public ref TRecord SlotUnderLock(int slot) => ref _generation!.SlotUnderLock(slot);

	/// <summary>
	///     Returns a writable reference to the per-slot verified flag, creating the verified storage on first use.
	///     Callers must hold the lock and only pass slots in <c>[0, <see cref="PublishedUnderLock" />)</c>.
	/// </summary>
	public ref bool VerifiedUnderLock(int slot)
	{
		bool[]? verified = _verified;
		if (verified is null || slot >= verified.Length)
		{
			bool[] bigger = new bool[Math.Max(PublishedUnderLock, 2 * (verified?.Length ?? 0))];
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
	///     Callers must hold the lock.
	/// </summary>
	public bool IsVerifiedUnderLock(int slot) => _verified is { } verified && slot < verified.Length && verified[slot];

	/// <summary>
	///     Resets the buffer to an empty state. Caller must not hold the lock on entry.
	/// </summary>
	public void Clear()
	{
		lock (Lock)
		{
			_verified = null;
			Volatile.Write(ref _generation, null);
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

	private Generation CreateGeneration()
	{
		Generation created = new(Lock);
		return Interlocked.CompareExchange(ref _generation, created, null) ?? created;
	}

	/// <summary>
	///     The held lock of a <see cref="ChunkedSlotStorage{TRecord}" />, released by <see cref="Dispose" />.
	/// </summary>
	/// <remarks>
	///     This type is public only so that generated code can use it; it is not intended to be used directly.
	/// </remarks>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	[EditorBrowsable(EditorBrowsableState.Never)]
	public ref struct LockScope
	{
#if NET10_0_OR_GREATER
		private System.Threading.Lock.Scope _scope;

		internal LockScope(System.Threading.Lock @lock)
		{
			_scope = @lock.EnterScope();
		}

		/// <summary>Releases the lock.</summary>
		public void Dispose() => _scope.Dispose();
#else
		private readonly object _lock;

		internal LockScope(object @lock)
		{
			_lock = @lock;
			System.Threading.Monitor.Enter(@lock);
		}

		/// <summary>Releases the lock.</summary>
		public void Dispose() => System.Threading.Monitor.Exit(_lock);
#endif
	}

	/// <summary>
	///     The slots recorded since the last <see cref="ChunkedSlotStorage{TRecord}.Clear" />.
	/// </summary>
	/// <remarks>
	///     This type is public only so that generated code can use it; it is not intended to be used directly.
	/// </remarks>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class Generation
	{
		private readonly MockolateLock _lock;
		private TRecord _firstSlot;
		private TRecord[]?[]? _laterChunks;
		private HashSet<int>? _pending;
		private int _reserved;
		private int _published;
		private int _pendingCount;

		internal Generation(MockolateLock @lock)
		{
			_lock = @lock;
		}

		internal int Published => Volatile.Read(ref _published);

		internal int Reserve() => Interlocked.Increment(ref _reserved) - 1;

		/// <summary>
		///     Returns a writable reference to the storage cell for the reserved <paramref name="slot" />, allocating
		///     the backing chunk if it has not been installed yet.
		/// </summary>
		public ref TRecord SlotForWrite(int slot)
		{
			if (slot == 0)
			{
				return ref _firstSlot;
			}

			int chunkIdx = ChunkIndex(slot, out int offset);
			TRecord[]?[]? chunks = Volatile.Read(ref _laterChunks);
			TRecord[]? chunk = chunks is not null && chunkIdx <= chunks.Length
				? Volatile.Read(ref chunks[chunkIdx - 1])
				: null;
			chunk ??= EnsureLaterChunk(chunkIdx);
			return ref chunk[offset];
		}

		/// <summary>
		///     Marks the reserved <paramref name="slot" /> as fully written. The published count only advances over
		///     a contiguous run of written slots, so a reader never sees a slot that is still being written.
		///     A slot written before an earlier one is parked under the lock and published by the writer that closes
		///     the gap, so a writer never spins on an earlier writer, but it may wait for the lock.
		///     Call it exactly once per slot reserved via <see cref="ChunkedSlotStorage{TRecord}.Reserve" />; a slot
		///     that is never published hides every later record of this generation.
		/// </summary>
		public void Publish(int slot)
		{
			if (Interlocked.CompareExchange(ref _published, slot + 1, slot) == slot)
			{
				if (Volatile.Read(ref _pendingCount) != 0)
				{
					lock (_lock)
					{
						PublishPendingUnderLock();
					}
				}

				return;
			}

			lock (_lock)
			{
				Interlocked.Increment(ref _pendingCount);
				_pending ??= [];
				_pending.Add(slot);
				PublishPendingUnderLock();
			}
		}

		internal ref TRecord SlotUnderLock(int slot)
		{
			int chunkIdx = ChunkIndex(slot, out int offset);
			if (chunkIdx == 0)
			{
				return ref _firstSlot;
			}

			return ref _laterChunks![chunkIdx - 1]![offset];
		}

		private void PublishPendingUnderLock()
		{
			int published = Volatile.Read(ref _published);
			while (_pending!.Remove(published))
			{
				published++;
				Volatile.Write(ref _published, published);
				Interlocked.Decrement(ref _pendingCount);
			}
		}

		private TRecord[] EnsureLaterChunk(int chunkIdx)
		{
			int index = chunkIdx - 1;
			lock (_lock)
			{
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
}
