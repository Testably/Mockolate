using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Registry;

public sealed class MockRegistrySnapshotRetrievalTests
{
	private static MethodInfo GetMethodInfo()
		=> typeof(MockRegistrySnapshotRetrievalTests).GetMethod(nameof(GetMethodInfo),
			BindingFlags.Static | BindingFlags.NonPublic)!;

	public sealed class GetMethodSetupsTests
	{
		[Test]
		public async Task WithActiveScenarioAndScopedSetup_YieldsScopedThenSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup snapshotSetup = new();
			FakeMethodSetup scopedSetup = new();
			registry.SetupMethod(2, snapshotSetup);
			registry.Setup.GetOrCreateScenario("a").Methods.Add(scopedSetup);

			registry.TransitionTo("a");

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

			await That(setups).HasCount(2);
			await That(setups[0]).IsSameAs(scopedSetup);
			await That(setups[1]).IsSameAs(snapshotSetup);
		}

		[Test]
		public async Task WithActiveScenarioButNoScopedBucket_FallsBackToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup snapshotSetup = new();

			registry.SetupMethod(2, snapshotSetup);
			registry.TransitionTo("never-registered");

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

			await That(setups).HasCount(1);
			await That(setups[0]).IsSameAs(snapshotSetup);
		}

		[Test]
		public async Task WithSnapshotAndGlobalDictSetups_YieldsSnapshotBeforeGlobalDict()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup snapshotSetup = new();
			FakeMethodSetup globalDictSetup = new();

			registry.SetupMethod(2, snapshotSetup);
			registry.Setup.Methods.Add(globalDictSetup);

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

			await That(setups).HasCount(2);
			await That(setups[0]).IsSameAs(snapshotSetup);
			await That(setups[1]).IsSameAs(globalDictSetup);
		}

		[Test]
		public async Task WithSnapshotSetupOnly_YieldsSnapshotSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(2, setup);

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

			await That(setups).HasCount(1);
			await That(setups[0]).IsSameAs(setup);
		}
	}

	public sealed class GetIndexerSetupTests
	{
		[Test]
		public async Task ByAccess_WithActiveScenarioAndScopedDoesNotMatch_FallsBackToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup snapshotSetup = new(true);
			FakeIndexerSetup scopedNonMatching = new(false);
			registry.SetupIndexer(0, snapshotSetup);
			registry.Setup.GetOrCreateScenario("a").Indexers.Add(scopedNonMatching);

			registry.TransitionTo("a");

			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(new IndexerGetterAccess<int>(1));

			await That(result).IsSameAs(snapshotSetup);
		}

		[Test]
		public async Task ByAccess_WithSnapshotSetupOnly_ReturnsSnapshotSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(0, setup);

			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(new IndexerGetterAccess<int>(1));

			await That(result).IsSameAs(setup);
		}

		[Test]
		public async Task ByPredicate_WithActiveScenarioAndPredicateRejectsScoped_FallsBackToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup snapshotSetup = new(true);
			FakeIndexerSetup scopedSetup = new(true);
			registry.SetupIndexer(0, snapshotSetup);
			registry.Setup.GetOrCreateScenario("a").Indexers.Add(scopedSetup);

			registry.TransitionTo("a");

			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(s => ReferenceEquals(s, snapshotSetup));

			await That(result).IsSameAs(snapshotSetup);
		}

		[Test]
		public async Task ByPredicate_WithSnapshotSetupOnly_ReturnsSnapshotSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(0, setup);

			IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(_ => true);

			await That(result).IsSameAs(setup);
		}
	}

	public sealed class RemoveEventSnapshotByNameTests
	{
		[Test]
		public async Task RemoveEvent_WhenUnsubscribeMemberIdDiffersFromSubscribe_FiresUnsubscribedCallbackViaNameScan()
		{
			// The source generator mints separate subscribe and unsubscribe member ids and registers the
			// event setup under the subscribe id only. The unsubscribe path therefore always misses the
			// snapshot lookup-by-id and must fall through to the name-based snapshot scan inside
			// EnumerateDefaultScopeEventSetupsByName for unsubscribed callbacks to fire.
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			int unsubscribedCount = 0;
			EventSetup setup = new(registry, "OnFoo");
			setup.OnUnsubscribed.Do(() => unsubscribedCount++);

			const int subscribeMemberId = 5;
			const int unsubscribeMemberId = 6;
			registry.SetupEvent(subscribeMemberId, setup);

			registry.RemoveEvent(unsubscribeMemberId, "OnFoo", this, GetMethodInfo());

			await That(unsubscribedCount).IsEqualTo(1);
		}
	}

	public sealed class GetUnusedSetupsTests
	{
		[Test]
		public async Task IncludesIndexerSnapshotSetup_WhenNoInteractionsMatch()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(false);

			registry.SetupIndexer(2, setup);

			IReadOnlyCollection<ISetup> unused = registry.GetUnusedSetups(registry.Interactions);

			await That(unused).Contains(setup);
		}

		[Test]
		public async Task IncludesMethodSnapshotSetup_WhenNoInteractionsMatch()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(2, setup);

			IReadOnlyCollection<ISetup> unused = registry.GetUnusedSetups(registry.Interactions);

			await That(unused).Contains(setup);
		}
	}
}
