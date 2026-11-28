using System.Collections.Generic;
using System.Linq;
using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Registry;

public sealed class MockRegistryScenarioTests
{
	[Test]
	public async Task ApplyIndexerGetter_WithLazyDefaultAndSetup_ShouldReturnTheResultOfTheSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeIndexerSetup setup = new(true);
		IndexerGetterAccess<int> access = new(1);

		int callCount = 0;
		int result = registry.ApplyIndexerGetter(access, setup, () => ++callCount, 0);

		await That(result).IsEqualTo(1)
			.Because("FakeIndexerSetup.GetResult hands back the value produced by the default value generator");
		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task GetIndexerSetup_ByAccess_WithActiveScenarioAndMatchingScopedSetup_ShouldReturnTheScopedSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeIndexerSetup global = new(true);
		FakeIndexerSetup scoped = new(true);
		registry.Setup.Indexers.Add(global);
		registry.Setup.GetOrCreateScenario("a").Indexers.Add(scoped);

		registry.TransitionTo("a");

		IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(new IndexerGetterAccess<int>(1));

		await That(result).IsSameAs(scoped);
	}

	[Test]
	public async Task GetIndexerSetup_ByAccess_WithActiveScenario_WhenTheScopedSetupDoesNotMatch_ShouldFallBackToTheGlobalSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeIndexerSetup global = new(true);
		FakeIndexerSetup scopedNonMatching = new(false);
		registry.Setup.Indexers.Add(global);
		registry.Setup.GetOrCreateScenario("a").Indexers.Add(scopedNonMatching);

		registry.TransitionTo("a");

		IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(new IndexerGetterAccess<int>(1));

		await That(result).IsSameAs(global);
	}

	[Test]
	public async Task GetIndexerSetup_ByPredicate_WithActiveScenario_WhenThePredicateRejectsTheScopedSetup_ShouldFallBackToTheGlobalSetup()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeIndexerSetup global = new(true);
		FakeIndexerSetup scoped = new(true);
		registry.Setup.Indexers.Add(global);
		registry.Setup.GetOrCreateScenario("a").Indexers.Add(scoped);

		registry.TransitionTo("a");

		IndexerSetup? result = registry.GetIndexerSetup<IndexerSetup>(s => ReferenceEquals(s, global));

		await That(result).IsSameAs(global);
	}

	[Test]
	public async Task GetMethodSetups_WithActiveScenarioAndScopedSetup_ShouldYieldTheScopedSetupsBeforeTheGlobalSetups()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeMethodSetup global = new();
		FakeMethodSetup scoped = new();
		registry.Setup.Methods.Add(global);
		registry.Setup.GetOrCreateScenario("a").Methods.Add(scoped);

		registry.TransitionTo("a");

		List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

		await That(setups).HasCount(2);
		await That(setups[0]).IsSameAs(scoped);
		await That(setups[1]).IsSameAs(global);
	}

	[Test]
	public async Task GetMethodSetups_WithActiveScenarioWithoutScopedSetups_ShouldFallBackToTheGlobalSetups()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));
		FakeMethodSetup global = new();
		registry.Setup.Methods.Add(global);

		registry.TransitionTo("never-registered");

		List<MethodSetup> setups = registry.GetMethodSetups<MethodSetup>("foo").ToList();

		await That(setups).HasCount(1);
		await That(setups[0]).IsSameAs(global);
	}
}
