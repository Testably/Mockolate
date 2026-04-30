using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Mockolate.Tests;

public sealed partial class MockTests
{
	public sealed class ThreadSafetyTests
	{
		[Test]
		public async Task Event_ShouldBeThreadSafe()
		{
			IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
			ManualResetEventSlim barrier = new(false);
			int taskCount = 50;
			int iterationsPerTask = 20;
			Task[] tasks = new Task[taskCount];
			for (int i = 0; i < taskCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					EventHandler handler = (_, _) => { };
					barrier.Wait();
					for (int j = 0; j < iterationsPerTask; j++)
					{
						sut.MyEvent += handler;
						sut.MyEvent -= handler;
					}
				}, CancellationToken.None);
			}

			barrier.Set();
			await Task.WhenAll(tasks);

			await That(sut.Mock.Verify.MyEvent.Subscribed()).Exactly(taskCount * iterationsPerTask);
			await That(sut.Mock.Verify.MyEvent.Unsubscribed()).Exactly(taskCount * iterationsPerTask);
		}

		[Test]
		public async Task Event_SubscribeRaiseUnsubscribe_ShouldBeThreadSafe()
		{
			for (int round = 0; round < 10; round++)
			{
				IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
				int handlerCallCount = 0;
				ManualResetEventSlim barrier = new(false);
				int subscriberCount = 20;
				int eventsPerSubscriber = 10;
				Task[] tasks = new Task[subscriberCount * 3];
				EventHandler[] handlers = new EventHandler[subscriberCount];
				for (int i = 0; i < subscriberCount; i++)
				{
					handlers[i] = (_, _) => Interlocked.Increment(ref handlerCallCount);
				}

				for (int i = 0; i < subscriberCount; i++)
				{
					int handlerIdx = i;
					tasks[i] = Task.Run(() =>
					{
						barrier.Wait();
						sut.MyEvent += handlers[handlerIdx];
					}, CancellationToken.None);
				}

				for (int i = 0; i < subscriberCount; i++)
				{
					int idx = subscriberCount + i;
					tasks[idx] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < eventsPerSubscriber; j++)
						{
							sut.Mock.Raise.MyEvent(this, EventArgs.Empty);
						}
					}, CancellationToken.None);
				}

				for (int i = 0; i < subscriberCount; i++)
				{
					int idx = (subscriberCount * 2) + i;
					int handlerIdx = i;
					tasks[idx] = Task.Run(() =>
					{
						barrier.Wait();
						sut.MyEvent -= handlers[handlerIdx];
					}, CancellationToken.None);
				}

				barrier.Set();
				await Task.WhenAll(tasks);

				await That(sut.Mock.Verify.MyEvent.Subscribed()).Exactly(subscriberCount);
				await That(sut.Mock.Verify.MyEvent.Unsubscribed()).Exactly(subscriberCount);
				await That(handlerCallCount).IsLessThanOrEqualTo(subscriberCount * eventsPerSubscriber * subscriberCount);
			}
		}

		[Test]
		public async Task Indexer_ManyKeys_ShouldBeThreadSafe()
		{
			for (int round = 0; round < 5; round++)
			{
				IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
				int keyCount = 20;
				for (int k = 0; k < keyCount; k++)
				{
					sut.Mock.Setup[k].InitializeWith($"key-{k}");
				}

				ConcurrentDictionary<int, ConcurrentQueue<string>> valuesByKey = [];
				ManualResetEventSlim barrier = new(false);
				int readerCount = 30;
				int iterationsPerReader = 10;
				int writerCount = 8;
				int iterationsPerWriter = 10;
				Task[] tasks = new Task[readerCount + writerCount];
				Random random = new(42 + round);

				int[][] readerKeys = new int[readerCount][];
				for (int i = 0; i < readerCount; i++)
				{
					readerKeys[i] = new int[iterationsPerReader];
					for (int j = 0; j < iterationsPerReader; j++)
					{
						readerKeys[i][j] = random.Next(keyCount);
					}
				}

				int[][] writerKeys = new int[writerCount][];
				for (int i = 0; i < writerCount; i++)
				{
					writerKeys[i] = new int[iterationsPerWriter];
					for (int j = 0; j < iterationsPerWriter; j++)
					{
						writerKeys[i][j] = random.Next(keyCount);
					}
				}

				for (int i = 0; i < readerCount; i++)
				{
					int[] keys = readerKeys[i];
					tasks[i] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < iterationsPerReader; j++)
						{
							int key = keys[j];
							string value = sut[key];
							valuesByKey.GetOrAdd(key, _ => new ConcurrentQueue<string>()).Enqueue(value);
						}
					}, CancellationToken.None);
				}

				for (int i = 0; i < writerCount; i++)
				{
					int[] keys = writerKeys[i];
					tasks[readerCount + i] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < iterationsPerWriter; j++)
						{
							int key = keys[j];
							sut[key] = $"key-{key}";
						}
					}, CancellationToken.None);
				}

				barrier.Set();
				await Task.WhenAll(tasks);

				await That(valuesByKey).All().Satisfy(kvp =>
				{
					string expectedValue = $"key-{kvp.Key}";
					return kvp.Value.All(v => v == expectedValue);
				});
			}
		}

		[Test]
		public async Task Indexer_ShouldBeThreadSafe()
		{
			IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
			sut.Mock.Setup[0].InitializeWith("hello");
			sut.Mock.Setup[1].InitializeWith("hello");
			ConcurrentQueue<string> values = [];
			ManualResetEventSlim barrier = new(false);
			int readerCount = 50;
			int iterationsPerReader = 20;
			int writerCount = 10;
			int iterationsPerWriter = 20;
			Task[] tasks = new Task[readerCount + writerCount];
			for (int i = 0; i < readerCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					barrier.Wait();
					for (int j = 0; j < iterationsPerReader; j++)
					{
						values.Enqueue(sut[0]);
						values.Enqueue(sut[1]);
					}
				}, CancellationToken.None);
			}

			for (int i = 0; i < writerCount; i++)
			{
				tasks[readerCount + i] = Task.Run(() =>
				{
					barrier.Wait();
					for (int j = 0; j < iterationsPerWriter; j++)
					{
						sut[0] = "hello";
						sut[1] = "hello";
					}
				}, CancellationToken.None);
			}

			barrier.Set();
			await Task.WhenAll(tasks);

			await That(sut.Mock.Verify[0].Got()).Exactly(readerCount * iterationsPerReader);
			await That(sut.Mock.Verify[1].Got()).Exactly(readerCount * iterationsPerReader);
			await That(sut.Mock.Verify[0].Set(It.IsAny<string>())).Exactly(writerCount * iterationsPerWriter);
			await That(sut.Mock.Verify[1].Set(It.IsAny<string>())).Exactly(writerCount * iterationsPerWriter);
			await That(values).Contains("hello").Exactly(readerCount * iterationsPerReader * 2);
		}

		[Test]
		public async Task Method_HighContention_ShouldBeThreadSafe()
		{
			for (int round = 0; round < 10; round++)
			{
				IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
				sut.Mock.Setup.MyMethod(It.IsAny<int>()).Returns(42);
				ConcurrentQueue<int> values = [];
				ManualResetEventSlim barrier = new(false);
				int taskCount = 60;
				int iterationsPerTask = 20;
				Task[] tasks = new Task[taskCount];
				for (int i = 0; i < taskCount; i++)
				{
					tasks[i] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < iterationsPerTask; j++)
						{
							values.Enqueue(sut.MyMethod(j));
						}
					}, CancellationToken.None);
				}

				barrier.Set();
				await Task.WhenAll(tasks);

				int expectedCalls = taskCount * iterationsPerTask;
				await That(values).All().Satisfy(v => v == 42);
				await That(sut.Mock.Verify.MyMethod(It.IsAny<int>())).Exactly(expectedCalls);
			}
		}

		[Test]
		public async Task Method_ShouldBeThreadSafe()
		{
			IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsAny<int>()).Returns(42);
			ConcurrentQueue<int> values = [];
			ManualResetEventSlim barrier = new(false);
			int taskCount = 50;
			int iterationsPerTask = 20;
			Task[] tasks = new Task[taskCount];
			for (int i = 0; i < taskCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					barrier.Wait();
					for (int j = 0; j < iterationsPerTask; j++)
					{
						values.Enqueue(sut.MyMethod(j));
					}
				}, CancellationToken.None);
			}

			barrier.Set();
			await Task.WhenAll(tasks);

			await That(sut.Mock.Verify.MyMethod(It.IsAny<int>())).Exactly(taskCount * iterationsPerTask);
			await That(values).Contains(42).Exactly(taskCount * iterationsPerTask);
		}

		[Test]
		public async Task Property_ConcurrentSetupAndRead_ShouldBeThreadSafe()
		{
			for (int round = 0; round < 10; round++)
			{
				IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
				Guid expectedGuid = Guid.NewGuid();
				ManualResetEventSlim barrier = new(false);
				int readerCount = 30;
				int iterationsPerReader = 50;
				Task[] tasks = new Task[readerCount + 2];
				ConcurrentQueue<string?> stringValues = [];
				ConcurrentQueue<Guid> guidValues = [];

				tasks[0] = Task.Run(() =>
				{
					barrier.Wait();
					sut.Mock.Setup.MyStringProperty.InitializeWith("hello");
				}, CancellationToken.None);

				tasks[1] = Task.Run(() =>
				{
					barrier.Wait();
					sut.Mock.Setup.MyGuidProperty.InitializeWith(expectedGuid);
				}, CancellationToken.None);

				for (int i = 0; i < readerCount; i++)
				{
					tasks[2 + i] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < iterationsPerReader; j++)
						{
							stringValues.Enqueue(sut.MyStringProperty);
							guidValues.Enqueue(sut.MyGuidProperty);
						}
					}, CancellationToken.None);
				}

				barrier.Set();
				await Task.WhenAll(tasks);

				await That(stringValues).All().Satisfy(v => v is "" or "hello")
					.Because("each read sees either the pre-setup default or the configured value");
				await That(guidValues).All().Satisfy(v => v == Guid.Empty || v == expectedGuid)
					.Because("each read sees either the pre-setup default or the configured value");
				await That(sut.MyStringProperty).IsEqualTo("hello")
					.Because(
						"both setup tasks have finished by now, so a racing default insert must not have overwritten the configured value");
				await That(sut.MyGuidProperty).IsEqualTo(expectedGuid)
					.Because(
						"both setup tasks have finished by now, so a racing default insert must not have overwritten the configured value");
				await That(sut.Mock.Verify.MyStringProperty.Got()).Exactly((readerCount * iterationsPerReader) + 1);
				await That(sut.Mock.Verify.MyGuidProperty.Got()).Exactly((readerCount * iterationsPerReader) + 1);
			}
		}

		[Test]
		public async Task Property_HighContention_ShouldBeThreadSafe()
		{
			for (int round = 0; round < 10; round++)
			{
				IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
				sut.Mock.Setup.MyStringProperty.InitializeWith("test-value");
				ConcurrentQueue<string> values = [];
				ManualResetEventSlim barrier = new(false);
				int taskCount = 60;
				int iterationsPerTask = 20;
				Task[] tasks = new Task[taskCount];
				for (int i = 0; i < taskCount; i++)
				{
					tasks[i] = Task.Run(() =>
					{
						barrier.Wait();
						for (int j = 0; j < iterationsPerTask; j++)
						{
							values.Enqueue(sut.MyStringProperty);
						}
					}, CancellationToken.None);
				}

				barrier.Set();
				await Task.WhenAll(tasks);

				int expectedReads = taskCount * iterationsPerTask;
				await That(values).All().Satisfy(v => v == "test-value");
				await That(sut.Mock.Verify.MyStringProperty.Got()).Exactly(expectedReads);
			}
		}

		[Test]
		public async Task Property_ShouldBeThreadSafe()
		{
			IMyThreadSafetyService sut = IMyThreadSafetyService.CreateMock();
			Guid expectedGuid = Guid.NewGuid();
			sut.Mock.Setup.MyStringProperty.InitializeWith("hello");
			sut.Mock.Setup.MyGuidProperty.InitializeWith(expectedGuid);
			ConcurrentQueue<string> stringValues = [];
			ConcurrentQueue<Guid> guidValues = [];
			ManualResetEventSlim barrier = new(false);
			int readerCount = 50;
			int iterationsPerReader = 20;
			int writerCount = 10;
			int iterationsPerWriter = 20;
			Task[] tasks = new Task[readerCount + writerCount];
			for (int i = 0; i < readerCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					barrier.Wait();
					for (int j = 0; j < iterationsPerReader; j++)
					{
						stringValues.Enqueue(sut.MyStringProperty);
						guidValues.Enqueue(sut.MyGuidProperty);
					}
				}, CancellationToken.None);
			}

			for (int i = 0; i < writerCount; i++)
			{
				tasks[readerCount + i] = Task.Run(() =>
				{
					barrier.Wait();
					for (int j = 0; j < iterationsPerWriter; j++)
					{
						sut.MyStringProperty = "hello";
						sut.MyGuidProperty = expectedGuid;
					}
				}, CancellationToken.None);
			}

			barrier.Set();
			await Task.WhenAll(tasks);

			await That(sut.Mock.Verify.MyStringProperty.Got())
				.Exactly(readerCount * iterationsPerReader);
			await That(sut.Mock.Verify.MyGuidProperty.Got())
				.Exactly(readerCount * iterationsPerReader);
			await That(sut.Mock.Verify.MyStringProperty.Set(It.IsAny<string>()))
				.Exactly(writerCount * iterationsPerWriter);
			await That(sut.Mock.Verify.MyGuidProperty.Set(It.IsAny<Guid>()))
				.Exactly(writerCount * iterationsPerWriter);
			await That(stringValues).All().Satisfy(v => v == "hello");
			await That(guidValues).All().Satisfy(v => v == expectedGuid);
		}

		internal interface IMyThreadSafetyService
		{
			string MyStringProperty { get; set; }
			Guid MyGuidProperty { get; set; }
			string this[int index] { get; set; }
			int MyMethod(int value);
			event EventHandler? MyEvent;
		}
	}
}
