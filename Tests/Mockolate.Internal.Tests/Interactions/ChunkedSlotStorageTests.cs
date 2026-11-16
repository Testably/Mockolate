using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class ChunkedSlotStorageTests
{
	[Test]
	public async Task Clear_AfterMultipleChunks_ResetsCountAndAllowsRefill()
	{
		const int total = (ChunkedSlotStorage<int>.ChunkSize * 2) + 5;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		buffer.Clear();
		await That(buffer.Count).IsEqualTo(0);

		buffer.Append("Foo", 99);

		await That(buffer.Count).IsEqualTo(1);
		await That(((MethodInvocation<int>)store.Single()).Parameter1).IsEqualTo(99);
	}

	[Test]
	public async Task Clear_ShouldResetVerifiedFlags()
	{
		ChunkedSlotStorage<int> storage = CreateStorage(ChunkedSlotStorage<int>.ChunkSize + 2);
		lock (storage.Lock)
		{
			storage.VerifiedUnderLock(0) = true;
			storage.VerifiedUnderLock(ChunkedSlotStorage<int>.ChunkSize + 1) = true;
		}

		storage.Clear();
		Append(storage, ChunkedSlotStorage<int>.ChunkSize + 2);

		await That(ReadVerified(storage)).All().AreEqualTo(false)
			.Because("slots reused after Clear must not inherit the verified flags of the cleared records");
	}

	[Test]
	[Arguments(1)]
	[Arguments(2)]
	[Arguments(ChunkedSlotStorage<int>.ChunkSize)]
	[Arguments(ChunkedSlotStorage<int>.ChunkSize + 1)]
	[Arguments(ChunkedSlotStorage<int>.ChunkSize + 2)]
	[Arguments((ChunkedSlotStorage<int>.ChunkSize * 4) + 2)]
	public async Task SlotForWrite_ShouldPreserveRecordsAcrossChunkBoundaries(int count)
	{
		ChunkedSlotStorage<int> storage = CreateStorage(count);

		await That(storage.Count).IsEqualTo(count);
		await That(ReadRecords(storage)).IsEqualTo(Enumerable.Range(0, count));
	}

	[Test]
	public async Task SlotForWrite_WhenAppendingConcurrently_ShouldKeepEveryRecord()
	{
		const int threads = 8;
		const int perThread = 500;
		for (int run = 0; run < 20; run++)
		{
			ChunkedSlotStorage<int> storage = new();
			using Barrier barrier = new(threads);
			Thread[] writers = Enumerable.Range(0, threads).Select(t => new Thread(() =>
			{
				barrier.SignalAndWait();
				for (int i = 0; i < perThread; i++)
				{
					ChunkedSlotStorage<int>.Generation generation = storage.Reserve(out int slot);
					generation.SlotForWrite(slot) = (t * perThread) + i + 1;
					generation.Publish(slot);
				}
			})).ToArray();

			foreach (Thread writer in writers)
			{
				writer.Start();
			}

			foreach (Thread writer in writers)
			{
				writer.Join();
			}

			await That(ReadRecords(storage).OrderBy(x => x)).IsEqualTo(Enumerable.Range(1, threads * perThread))
				.Because("racing writers that install the first chunk or a later chunk must not lose a record");
		}
	}

	[Test]
	public async Task Publish_WhenSlot0IsStillBeingWritten_ShouldNotExposeSlot1()
	{
		ChunkedSlotStorage<int> storage = new();
		ChunkedSlotStorage<int>.Generation first = storage.Reserve(out int firstSlot);
		ChunkedSlotStorage<int>.Generation second = storage.Reserve(out int secondSlot);
		second.SlotForWrite(secondSlot) = 2;
		second.Publish(secondSlot);

		List<int> recordsWhileSlot0IsWritten = ReadRecords(storage);
		first.SlotForWrite(firstSlot) = 1;
		first.Publish(firstSlot);

		await That(recordsWhileSlot0IsWritten).IsEmpty()
			.Because("a reader must not see slot 0 before it is fully written");
		await That(ReadRecords(storage)).IsEqualTo([1, 2,])
			.Because("publishing slot 0 must also publish the already written slot 1");
	}

	[Test]
	[Arguments(ChunkedSlotStorage<int>.ChunkSize + 1)]
	[Arguments((ChunkedSlotStorage<int>.ChunkSize * 3) + 2)]
	public async Task Publish_InReverseOrder_ShouldOnlyExposeRecordsOncePrecedingSlotsAreWritten(int count)
	{
		ChunkedSlotStorage<int> storage = new();
		ChunkedSlotStorage<int>.Generation[] generations = new ChunkedSlotStorage<int>.Generation[count];
		for (int i = 0; i < count; i++)
		{
			generations[i] = storage.Reserve(out _);
		}

		List<int> visibleCounts = [];
		for (int slot = count - 1; slot >= 0; slot--)
		{
			generations[slot].SlotForWrite(slot) = slot;
			generations[slot].Publish(slot);
			visibleCounts.Add(ReadRecords(storage).Count);
		}

		await That(visibleCounts.Take(count - 1)).All().AreEqualTo(0)
			.Because("no slot may become visible while slot 0 is still being written");
		await That(ReadRecords(storage)).IsEqualTo(Enumerable.Range(0, count))
			.Because("publishing slot 0 must also publish every slot written before it");
	}

	[Test]
	[Arguments(1)]
	[Arguments(ChunkedSlotStorage<int>.ChunkSize + 1)]
	public async Task Publish_WhenSlotWasReservedBeforeClear_ShouldNotBecomeVisible(int slotsBeforeClear)
	{
		ChunkedSlotStorage<int> storage = new();
		Append(storage, slotsBeforeClear - 1);
		ChunkedSlotStorage<int>.Generation stale = storage.Reserve(out int staleSlot);
		stale.SlotForWrite(staleSlot) = -1;

		storage.Clear();
		stale.Publish(staleSlot);

		await That(storage.Count).IsEqualTo(0)
			.Because("a write that started before Clear completes in the discarded generation");
	}

	[Test]
	public async Task SlotForWrite_WhenSlotWasReservedBeforeClear_ShouldNotOverwriteLaterRecords()
	{
		ChunkedSlotStorage<int> storage = new();
		ChunkedSlotStorage<int>.Generation stale = storage.Reserve(out int staleSlot);
		storage.Clear();
		Append(storage, 2);

		List<int> recordsWhileStaleSlotIsPending = ReadRecords(storage);
		stale.SlotForWrite(staleSlot) = -1;
		stale.Publish(staleSlot);

		await That(recordsWhileStaleSlotIsPending).IsEqualTo([0, 1,])
			.Because("records written after Clear must not depend on a slot reserved before Clear");
		await That(ReadRecords(storage)).IsEqualTo([0, 1,])
			.Because("a writer that reserved its slot before Clear must not overwrite the records written after it");
	}

	[Test]
	public async Task Publish_WhenReadAndClearedConcurrently_ShouldOnlyExposeFullyWrittenRecords()
	{
		const int threads = 8;
		const int perThread = 2_000;
		ChunkedSlotStorage<int> storage = new();
		using Barrier barrier = new(threads + 1);
		Thread[] writers = Enumerable.Range(0, threads).Select(_ => new Thread(() =>
		{
			barrier.SignalAndWait();
			for (int i = 0; i < perThread; i++)
			{
				ChunkedSlotStorage<int>.Generation generation = storage.Reserve(out int slot);
				generation.SlotForWrite(slot) = slot + 1;
				generation.Publish(slot);
				if (i % 500 == 250)
				{
					storage.Clear();
				}
			}
		})).ToArray();
		foreach (Thread writer in writers)
		{
			writer.Start();
		}

		barrier.SignalAndWait();
		List<int> unexpected = [];
		while (writers.Any(w => w.IsAlive))
		{
			lock (storage.Lock)
			{
				for (int slot = 0; slot < storage.PublishedUnderLock; slot++)
				{
					if (storage.SlotUnderLock(slot) != slot + 1)
					{
						unexpected.Add(slot);
					}
				}
			}
		}

		ChunkedSlotStorage<int>.Generation last = storage.Reserve(out int lastSlot);
		last.SlotForWrite(lastSlot) = lastSlot + 1;
		last.Publish(lastSlot);

		await That(unexpected).IsEmpty()
			.Because("readers must only see slots whose writers have finished, even across Clear");
		await That(storage.Count).IsEqualTo(lastSlot + 1)
			.Because("once every writer has finished, no written slot may remain unpublished");
	}

	[Test]
	public async Task Publish_WhenGapIsClosedWhileLaterWriterWaitsForTheLock_ShouldLetTheLaterWriterPublishItself()
	{
		ChunkedSlotStorage<int> storage = new();
		ChunkedSlotStorage<int>.Generation first = storage.Reserve(out int firstSlot);
		ChunkedSlotStorage<int>.Generation second = storage.Reserve(out int secondSlot);
		first.SlotForWrite(firstSlot) = 1;
		second.SlotForWrite(secondSlot) = 2;
		Thread secondWriter = new(() => second.Publish(secondSlot));

		(bool secondWriterWaitedForLock, int countBeforeLockIsReleased) =
			PublishWhileHoldingTheLock(storage, secondWriter, () => first.Publish(firstSlot));
		secondWriter.Join();

		await That(secondWriterWaitedForLock).IsTrue()
			.Because("the later writer must take the slow path while slot 0 is unpublished");
		await That(countBeforeLockIsReleased).IsEqualTo(1)
			.Because("the gap closer sees no pending slot yet, so it only publishes its own slot");
		await That(ReadRecords(storage)).IsEqualTo([1, 2,])
			.Because("the later writer must publish its own slot once it gets the lock");
	}

	[Test]
	public async Task AppendBoxedUnverified_ShouldNotCreateVerifiedFlags()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));
		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);

		List<(long Seq, IInteraction Interaction)> dest = [];
		((IFastMemberBuffer)buffer).AppendBoxedUnverified(dest);

		ChunkedSlotStorage<FastMethod1Buffer<int>.Record> storage =
			(ChunkedSlotStorage<FastMethod1Buffer<int>.Record>)typeof(FastMethod1Buffer<int>)
				.GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(buffer)!;
		object? verified = typeof(ChunkedSlotStorage<FastMethod1Buffer<int>.Record>)
			.GetField("_verified", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(storage);
		await That(dest).HasCount(2);
		await That(verified).IsNull()
			.Because("reading the unverified interactions must not allocate the verified flags");
	}

	[Test]
	public async Task IsVerifiedUnderLock_BeforeFirstVerification_ShouldBeFalseForEverySlot()
	{
		ChunkedSlotStorage<int> storage = CreateStorage((ChunkedSlotStorage<int>.ChunkSize * 2) + 3);

		await That(ReadVerified(storage)).All().AreEqualTo(false);
	}

	[Test]
	public async Task IsVerifiedUnderLock_ForSlotsPublishedAfterFirstVerification_ShouldBeFalse()
	{
		ChunkedSlotStorage<int> storage = CreateStorage(1);
		lock (storage.Lock)
		{
			storage.VerifiedUnderLock(0) = true;
		}

		Append(storage, ChunkedSlotStorage<int>.ChunkSize + 1);

		bool[] expected = [true, ..Enumerable.Repeat(false, ChunkedSlotStorage<int>.ChunkSize + 1),];
		await That(ReadVerified(storage)).IsEqualTo(expected)
			.Because("slots beyond the verified flags created by the first verification are not verified");
	}

	[Test]
	public async Task VerifiedUnderLock_WhenSlotsArePublishedAfterFirstVerification_ShouldKeepEarlierFlags()
	{
		ChunkedSlotStorage<int> storage = CreateStorage(1);
		lock (storage.Lock)
		{
			storage.VerifiedUnderLock(0) = true;
		}

		Append(storage, ChunkedSlotStorage<int>.ChunkSize * 3);
		lock (storage.Lock)
		{
			storage.VerifiedUnderLock(1) = true;
			storage.VerifiedUnderLock(ChunkedSlotStorage<int>.ChunkSize + 1) = true;
			storage.VerifiedUnderLock(storage.PublishedUnderLock - 1) = true;
		}

		int[] expected = [0, 1, ChunkedSlotStorage<int>.ChunkSize + 1, ChunkedSlotStorage<int>.ChunkSize * 3,];
		await That(ReadVerified(storage).Select((v, i) => (v, i)).Where(x => x.v).Select(x => x.i))
			.IsEqualTo(expected)
			.Because("growing the verified storage must keep the flags set before the growth");
	}

	[Test]
	public async Task ChunkIndex_ShouldMapSlotsContiguouslyIntoASingleSlotChunkFollowedByFullChunks()
	{
		int expectedChunk = 0;
		int expectedOffset = 0;
		for (int slot = 0; slot < 10_000; slot++)
		{
			if (expectedOffset == (expectedChunk == 0 ? 1 : ChunkedSlotStorage<int>.ChunkSize))
			{
				expectedChunk++;
				expectedOffset = 0;
			}

			int chunk = ChunkedSlotStorage<int>.ChunkIndex(slot, out int offset);

			await That((chunk, offset)).IsEqualTo((expectedChunk, expectedOffset))
				.Because($"slot {slot} must follow the previous slot without gaps or overlaps");
			expectedOffset++;
		}
	}

	[Test]
	public async Task ConsumeMatching_AcrossMultipleChunks_ShouldMarkOnlyMatchingSlotsAsVerified()
	{
		const int total = 100;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));
		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		int matches = buffer.ConsumeMatching((IParameterMatch<int>)It.Satisfies<int>(x => x % 2 == 0));

		await That(matches).IsEqualTo(total / 2);
		await That(store.GetUnverifiedInteractions().Select(x => ((MethodInvocation<int>)x).Parameter1))
			.IsEqualTo(Enumerable.Range(0, total).Where(x => x % 2 != 0));
	}

	[Test]
	public async Task SlotForWrite_AcrossMultipleChunks_PreservesAllRecords()
	{
		const int total = ChunkedSlotStorage<int>.ChunkSize * 3;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		List<IInteraction> ordered = [..store,];

		await That(ordered).HasCount(total);
		for (int i = 0; i < total; i++)
		{
			await That(((MethodInvocation<int>)ordered[i]).Parameter1).IsEqualTo(i);
		}
	}

	[Test]
	public async Task SlotForWrite_BeyondInitialChunksArrayLength_ExpandsArrayUnderLock()
	{
		const int total = (ChunkedSlotStorage<int>.ChunkSize * 4) + 1;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		List<IInteraction> ordered = [..store,];

		await That(ordered).HasCount(total);
		for (int i = 0; i < total; i++)
		{
			await That(((MethodInvocation<int>)ordered[i]).Parameter1).IsEqualTo(i);
		}
	}

	private static void Append(ChunkedSlotStorage<int> storage, int count)
	{
		for (int i = 0; i < count; i++)
		{
			ChunkedSlotStorage<int>.Generation generation = storage.Reserve(out int slot);
			generation.SlotForWrite(slot) = slot;
			generation.Publish(slot);
		}
	}

	private static (bool, int) PublishWhileHoldingTheLock(ChunkedSlotStorage<int> storage, Thread waitingWriter,
		Action publish)
	{
		using (storage.EnterLock())
		{
			waitingWriter.Start();
			bool waited = SpinWait.SpinUntil(() => (waitingWriter.ThreadState & ThreadState.WaitSleepJoin) != 0,
				TimeSpan.FromSeconds(30));
			publish();
			return (waited, storage.Count);
		}
	}

	private static ChunkedSlotStorage<int> CreateStorage(int count)
	{
		ChunkedSlotStorage<int> storage = new();
		Append(storage, count);
		return storage;
	}

	private static List<int> ReadRecords(ChunkedSlotStorage<int> storage)
	{
		lock (storage.Lock)
		{
			List<int> records = [];
			for (int slot = 0; slot < storage.PublishedUnderLock; slot++)
			{
				records.Add(storage.SlotUnderLock(slot));
			}

			return records;
		}
	}

	private static List<bool> ReadVerified(ChunkedSlotStorage<int> storage)
	{
		lock (storage.Lock)
		{
			List<bool> verified = [];
			for (int slot = 0; slot < storage.PublishedUnderLock; slot++)
			{
				verified.Add(storage.IsVerifiedUnderLock(slot));
			}

			return verified;
		}
	}
}
