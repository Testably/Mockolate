using System.Reflection;
using System.Threading;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public partial class MockSetupsTests
{
	public class IndexersTests
	{
		[Test]
		public async Task AddAndGetLatestOrDefault_ShouldReturnLatestMatching()
		{
			MockSetups.IndexerSetups setups = new();
			FakeIndexerSetup setup1 = new(true);
			FakeIndexerSetup setup2 = new(false);
			FakeIndexerAccess access = new();
			setups.Add(setup1);
			setups.Add(setup2);

			FakeIndexerSetup? result =
				setups.GetMatching<FakeIndexerSetup>(s => ((IInteractiveIndexerSetup)s).Matches(access));

			await That(result).IsEqualTo(setup1);
		}

		[Test]
		public async Task GetMatching_OnEmptyStorage_ShouldReturnNullWithoutThrowing()
		{
			MockSetups.IndexerSetups setups = new();

			IndexerSetup? result = setups.GetMatching<IndexerSetup>(_ => true);

			await That(result).IsNull();
		}

		[Test]
		public async Task GetMatching_WithAccess_ShouldNotBlockOtherThreadsWhileMatching()
		{
			MockSetups.IndexerSetups setups = new();
			bool addedWhileMatching = false;
			setups.Add(new FakeIndexerSetup(true, () => addedWhileMatching = AddFromOtherThread(setups)));

			_ = setups.GetMatching<FakeIndexerSetup>(new FakeIndexerAccess());

			await That(addedWhileMatching).IsTrue()
				.Because("a matcher runs user code, so it must not hold the lock that other threads need to access the setups");
		}

		[Test]
		public async Task GetMatching_WithPredicate_ShouldNotBlockOtherThreadsWhileMatching()
		{
			MockSetups.IndexerSetups setups = new();
			setups.Add(new FakeIndexerSetup(true));
			bool addedWhileMatching = false;

			_ = setups.GetMatching<FakeIndexerSetup>(_ =>
			{
				addedWhileMatching = AddFromOtherThread(setups);
				return true;
			});

			await That(addedWhileMatching).IsTrue()
				.Because("the predicate runs user code, so it must not hold the lock that other threads need to access the setups");
		}

		[Test]
		public async Task InitializeStorageCount_OnFreshInstance_ShouldAllocateValueStoragesArrayWithRequestedLength()
		{
			MockSetups.IndexerSetups setups = new();

			setups.InitializeStorageCount(5);

			Array? storages = GetValueStoragesField(setups);
			await That(storages).IsNotNull();
			await That(storages!.Length).IsEqualTo(5);
		}

		[Test]
		public async Task InitializeStorageCount_WhenAlreadyAllocated_ShouldKeepFirstAllocation()
		{
			MockSetups.IndexerSetups setups = new();
			setups.InitializeStorageCount(3);
			Array? firstAllocation = GetValueStoragesField(setups);

			setups.InitializeStorageCount(7);

			Array? secondAllocation = GetValueStoragesField(setups);
			await That(secondAllocation).IsNotNull();
			await That(secondAllocation!.Length).IsEqualTo(3);
			await That(secondAllocation).IsSameAs(firstAllocation);
		}

		[Test]
		public async Task Stress_ShouldMaintainCountAfterManyAdds()
		{
			MockSetups.IndexerSetups setups = new();

			for (int r = 0; r < 10; r++)
			{
				Parallel.For(0, 100, _ => setups.Add(new FakeIndexerSetup(false)));
			}

			await That(setups.Count).IsEqualTo(1000);
		}

		[Test]
		public async Task ThreadSafety_ConcurrentAddsAndQueries_ShouldReturnConsistentMatches()
		{
			MockSetups.IndexerSetups setups = new();
			FakeIndexerSetup initialMatch = new(true);
			setups.Add(initialMatch);
			Parallel.For(0, 200, i =>
			{
				bool shouldMatch = i % 2 == 0;
				FakeIndexerSetup setup = new(shouldMatch);
				setups.Add(setup);
				FakeIndexerAccess access = new();
				_ = setups.GetMatching<FakeIndexerSetup>(s => ((IInteractiveIndexerSetup)s).Matches(access));
			});
			FakeIndexerSetup finalMatch = new(true);
			setups.Add(finalMatch);
			FakeIndexerAccess finalAccess = new();

			FakeIndexerSetup? result =
				setups.GetMatching<FakeIndexerSetup>(s => ((IInteractiveIndexerSetup)s).Matches(finalAccess));

			await That(result).IsEqualTo(finalMatch);
		}

		[Test]
		public async Task ThreadSafety_ShouldHandleParallelAdds()
		{
			MockSetups.IndexerSetups setups = new();

			Parallel.For(0, 200, _ => setups.Add(new FakeIndexerSetup(false)));

			await That(setups.Count).IsEqualTo(200);
		}

		private static bool AddFromOtherThread(MockSetups.IndexerSetups setups)
		{
			Thread thread = new(() => setups.Add(new FakeIndexerSetup(false)))
			{
				IsBackground = true,
			};
			thread.Start();
			return thread.Join(TimeSpan.FromSeconds(5));
		}

		private static Array? GetValueStoragesField(MockSetups.IndexerSetups setups)
		{
			FieldInfo field = typeof(MockSetups.IndexerSetups).GetField(
				"_valueStorages", BindingFlags.Instance | BindingFlags.NonPublic)!;
			return (Array?)field.GetValue(setups);
		}
	}
}
