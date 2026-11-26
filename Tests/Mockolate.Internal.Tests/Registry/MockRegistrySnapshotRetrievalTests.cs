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
		public async Task WithAlternativeName_ShouldYieldSetupsOfBothNames()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup fooSetup = new("foo");
			FakeMethodSetup barSetup = new("bar");
			registry.SetupMethod(1, fooSetup);
			registry.SetupMethod(2, barSetup);
			registry.SetupMethod(3, new FakeMethodSetup("baz"));

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo", "bar").ToList();

			await That(setups).IsEqualTo([fooSetup, barSetup,]).InAnyOrder();
		}

		[Test]
		public async Task WithAlternativeNameAndOnlyGlobalDictSetups_ShouldYieldSetupsOfBothNames()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup fooSetup = new("foo");
			FakeMethodSetup barSetup = new("bar");
			registry.Setup.Methods.Add(fooSetup);
			registry.Setup.Methods.Add(barSetup);
			registry.Setup.Methods.Add(new FakeMethodSetup("baz"));

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo", "bar").ToList();

			await That(setups).IsEqualTo([fooSetup, barSetup,]).InAnyOrder();
		}

		[Test]
		public async Task WithAlternativeNameAndActiveScenario_ShouldYieldScopedSetupsOfBothNamesFirst()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup globalSetup = new("foo");
			FakeMethodSetup scopedSetup = new("bar");
			registry.SetupMethod(1, globalSetup);
			registry.Setup.GetOrCreateScenario("a").Methods.Add(scopedSetup);
			registry.Setup.GetOrCreateScenario("a").Methods.Add(new FakeMethodSetup("baz"));
			registry.TransitionTo("a");

			List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo", "bar").ToList();

			await That(setups).IsEqualTo([scopedSetup, globalSetup,]);
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

	public sealed class GetFallbackMethodSetupsTests
	{
		[Test]
		public async Task WithActiveScenario_ShouldIgnoreScenarioSetups()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.Setup.GetOrCreateScenario("a").Methods.Add(new FakeMethodSetup());
			registry.TransitionTo("a");

			IEnumerable<MethodSetup>? setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo");

			await That(setups).IsNull()
				.Because("generated dispatch calls GetMethodSetups instead while a scenario is active");
		}

		[Test]
		public async Task WithGlobalDictSetup_ShouldYieldOnlyGlobalDictSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup snapshotSetup = new();
			FakeMethodSetup globalDictSetup = new();
			registry.SetupMethod(2, snapshotSetup);
			registry.Setup.Methods.Add(globalDictSetup);

			List<MethodSetup> setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo")!.ToList();

			await That(setups).HasCount(1);
			await That(setups[0]).IsSameAs(globalDictSetup);
		}

		[Test]
		public async Task WithoutSetups_ShouldBeNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

			IEnumerable<MethodSetup>? setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo");

			await That(setups).IsNull();
		}

		[Test]
		public async Task WithSameNameInOtherBuckets_ShouldYieldOtherBucketsThenGlobalDict()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup firstBucketSetup = new();
			FakeMethodSetup thirdBucketSetup = new();
			FakeMethodSetup globalDictSetup = new();
			registry.SetupMethod(1, firstBucketSetup);
			registry.SetupMethod(2, new FakeMethodSetup());
			registry.SetupMethod(3, thirdBucketSetup);
			registry.Setup.Methods.Add(globalDictSetup);

			List<MethodSetup> setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo")!.ToList();

			await That(setups).HasCount(3);
			await That(setups[0]).IsSameAs(firstBucketSetup);
			await That(setups[1]).IsSameAs(thirdBucketSetup);
			await That(setups[2]).IsSameAs(globalDictSetup);
		}

		[Test]
		public async Task WithAlternativeNameInOtherBucket_ShouldYieldIt()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup alternativeSetup = new("bar");
			registry.SetupMethod(1, alternativeSetup);
			registry.SetupMethod(2, new FakeMethodSetup());

			List<MethodSetup> setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo", "bar")!.ToList();

			await That(setups).IsEqualTo([alternativeSetup,]);
		}

		[Test]
		public async Task WithOtherNameInOtherBucket_ShouldBeNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetupMethod(1, new FakeMethodSetup("bar"));
			registry.SetupMethod(2, new FakeMethodSetup());

			IEnumerable<MethodSetup>? setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo");

			await That(setups).IsNull();
		}

		[Test]
		public async Task WithOtherSetupTypeInOtherBucket_ShouldBeNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetupMethod(1, new FakeMethodSetup());

			IEnumerable<VoidMethodSetup>? setups = registry.GetFallbackMethodSetups<VoidMethodSetup>(2, "foo");

			await That(setups).IsNull();
		}

		[Test]
		public async Task WithOnlyOwnBucket_ShouldBeNull()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			registry.SetupMethod(2, new FakeMethodSetup());

			IEnumerable<MethodSetup>? setups = registry.GetFallbackMethodSetups<MethodSetup>(2, "foo");

			await That(setups).IsNull();
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

		[Test]
		public async Task IncludesMethodSnapshotSetupOnce_WhenRegisteredForSeveralMembers()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(1, setup);
			registry.SetupMethod(2, setup);

			IReadOnlyCollection<ISetup> unused = registry.GetUnusedSetups(registry.Interactions);

			await That(unused).IsEqualTo([setup,])
				.Because("a setup for any type argument is registered for every overload whose calls it matches");
		}
	}
}
