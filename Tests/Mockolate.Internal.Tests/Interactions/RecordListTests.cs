using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class RecordListTests
{
	[Test]
	public async Task Clear_AfterManyRecords_ResetsCountAndAllowsRefill()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = CreateBuffer(store);
		for (int i = 0; i < 50; i++)
		{
			buffer.Append("Foo", i);
		}

		store.Clear();
		await That(buffer.Count).IsEqualTo(0);

		buffer.Append("Foo", 99);

		await That(buffer.Count).IsEqualTo(1);
		await That(((MethodInvocation<int>)store.Single()).Parameter1).IsEqualTo(99);
	}

	[Test]
	public async Task Clear_ShouldNotCarryOverVerifiedFlags()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = CreateBuffer(store);
		buffer.Append("Foo", 1);
		buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>());

		store.Clear();
		buffer.Append("Foo", 2);

		await That(store.GetUnverifiedInteractions()).HasCount(1)
			.Because("records written after Clear must not inherit the verified flags of the cleared records");
	}

	[Test]
	[Arguments(1)]
	[Arguments(2)]
	[Arguments(100)]
	public async Task Push_ShouldKeepEveryRecordInOrder(int count)
	{
		IntList list = CreateList(count);

		await That(list.Count).IsEqualTo(count);
		await That(list.Read()).IsEqualTo(Enumerable.Range(0, count));
	}

	[Test]
	public async Task Push_WhenAppendingConcurrently_ShouldKeepEveryRecord()
	{
		const int threads = 8;
		const int perThread = 500;
		for (int run = 0; run < 20; run++)
		{
			IntList list = new();
			RunConcurrently(threads, t =>
			{
				for (int i = 0; i < perThread; i++)
				{
					list.Append((t * perThread) + i + 1);
				}
			});

			await That(list.Read().OrderBy(x => x)).IsEqualTo(Enumerable.Range(1, threads * perThread))
				.Because("racing writers must not lose a record");
			await That(list.Count).IsEqualTo(threads * perThread);
		}
	}

	[Test]
	[Arguments(0)]
	[Arguments(3)]
	public async Task Push_AfterClear_ShouldStartANewSequence(int recordsBeforeClear)
	{
		IntList list = CreateList(recordsBeforeClear);

		list.Clear();
		list.Append(1);

		await That(list.Read()).IsEqualTo([1,]);
		await That(list.Head!.Seq).IsEqualTo(0)
			.Because("the position of a call is counted from the last Clear");
		await That(list.Count).IsEqualTo(1);
	}

	[Test]
	public async Task Push_WhenReadAndClearedConcurrently_ShouldOnlyExposeFullyWrittenRecords()
	{
		const int threads = 8;
		const int perThread = 2_000;
		IntList list = new();
		using CancellationTokenSource readersDone = new();
		List<string> unexpected = [];
		Thread reader = new(() =>
		{
			while (!readersDone.IsCancellationRequested)
			{
				List<(int Value, int Check)> records = list.ReadWithChecks();
				for (int i = 0; i < records.Count; i++)
				{
					if (records[i].Value != records[i].Check)
					{
						unexpected.Add($"{records[i]} at {i} of {records.Count}");
					}
				}
			}
		});
		reader.Start();

		RunConcurrently(threads, t =>
		{
			for (int i = 0; i < perThread; i++)
			{
				list.Append((t * perThread) + i);
				if (i % 500 == 250)
				{
					list.Clear();
				}
			}
		});
		readersDone.Cancel();
		reader.Join();

		await That(unexpected).IsEmpty()
			.Because("readers must only see fully written records, even across Clear");
		await That(list.Count).IsEqualTo(list.Read().Count);
	}

	[Test]
	public async Task Interaction_WhenReadConcurrently_ShouldReturnASingleInstance()
	{
		IntList list = CreateList(1);
		RecordNode node = list.Head!;
		IInteraction[] results = new IInteraction[8];

		RunConcurrently(results.Length, t => results[t] = node.Interaction);

		await That(results.Distinct()).HasCount(1)
			.Because("the unverified-interactions filter compares boxed interactions by reference");
	}

	[Test]
	public async Task Verified_ShouldNotChangeTheMemberId()
	{
		IntNode node = new(1)
		{
			MemberId = 42,
		};

		node.Verified = true;
		int memberIdWhenVerified = node.MemberId;
		bool verified = node.Verified;
		node.Verified = false;

		await That(memberIdWhenVerified).IsEqualTo(42).Because("both values share one field");
		await That(verified).IsTrue();
		await That(node.MemberId).IsEqualTo(42);
		await That(node.Verified).IsFalse();
	}

	[Test]
	public async Task Append_WithoutBuffer_ShouldBeVisibleInTheViewOfItsMemberOnly()
	{
		FastMockInteractions store = new(2);

		FastMethod1Buffer<int>.Append(store, 0, "Foo", 1);
		FastMethod1Buffer<int>.Append(store, 1, "Bar", 2);
		FastMethod1Buffer<int>.Append(store, 0, "Foo", 3);

		FastMethod1Buffer<int> view = CreateBuffer(store);
		await That(view.Count).IsEqualTo(2);
		await That(view.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(2);
		await That(store.Count).IsEqualTo(3);
		await That(store.GetUnverifiedInteractions().Select(x => ((MethodInvocation<int>)x).Name)).IsEqualTo(["Bar",]);
	}

	[Test]
	public async Task ConsumeMatching_ShouldMarkOnlyMatchingRecordsAsVerified()
	{
		const int total = 100;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = CreateBuffer(store);
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
	public async Task ConsumeMatching_WhenMatcherWaitsForAnotherThreadToRecordACall_ShouldNotDeadlock()
	{
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = CreateBuffer(store);
		buffer.Append("Foo", 1);

		int matches = buffer.ConsumeMatching((IParameterMatch<int>)It.Satisfies<int>(_ =>
		{
			Thread writer = new(() => buffer.Append("Foo", 2));
			writer.Start();
			return writer.Join(TimeSpan.FromSeconds(30));
		}));

		await That(matches).IsEqualTo(1);
		await That(buffer.Count).IsEqualTo(2)
			.Because("readers no longer hold a lock that a writer of the same member would wait for");
	}

	[Test]
	public async Task Enumeration_ShouldReturnRecordsInCallOrder()
	{
		const int total = 100;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = CreateBuffer(store);
		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		await That(store.Select(x => ((MethodInvocation<int>)x).Parameter1)).IsEqualTo(Enumerable.Range(0, total));
	}

	private static FastMethod1Buffer<int> CreateBuffer(FastMockInteractions store)
		=> new FastMethod1Buffer<int>(store, 0);

	private static IntList CreateList(int count)
	{
		IntList list = new();
		for (int i = 0; i < count; i++)
		{
			list.Append(i);
		}

		return list;
	}

	private static void RunConcurrently(int threads, Action<int> action)
	{
		using Barrier barrier = new(threads);
		Thread[] workers = Enumerable.Range(0, threads).Select(t => new Thread(() =>
		{
			barrier.SignalAndWait();
			action(t);
		})).ToArray();
		foreach (Thread worker in workers)
		{
			worker.Start();
		}

		foreach (Thread worker in workers)
		{
			worker.Join();
		}
	}

	private sealed class IntList
	{
		private readonly FastMockInteractions _store = new(1);

		public RecordNode? Head => _store.First;

		public int Count => _store.Count;

		public void Append(int value) => _store.Push(new IntNode(value));

		public void Clear() => _store.Clear();

		public List<int> Read() => ReadWithChecks().Select(x => x.Value).Reverse().ToList();

		public List<(int Value, int Check)> ReadWithChecks()
		{
			List<(int, int)> records = [];
			for (RecordNode? node = Head; node is not null; node = node.Next)
			{
				IntNode intNode = (IntNode)node;
				records.Add((intNode.Value, intNode.Check));
			}

			return records;
		}
	}

	private sealed class IntNode : RecordNode
	{
		public IntNode(int value)
		{
			Value = value;
			Check = value;
		}

		public int Value { get; }
		public int Check { get; }

		protected override IInteraction CreateInteraction() => new MethodInvocation("Foo");
	}
}
