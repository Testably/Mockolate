using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Registry;

public sealed class MockRegistrySetupSnapshotTests
{
	private static PropertySetup?[]? GetPropertySnapshotTable(MockRegistry registry)
	{
		FieldInfo field = typeof(MockRegistry).GetField(
			"_propertySetupsByMemberId", BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (PropertySetup?[]?)field.GetValue(registry);
	}

	public sealed class MethodTests
	{
		[Test]
		public async Task SetupMethod_WithIncreasingMemberIds_GrowsTableLazily()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup0 = new();
			FakeMethodSetup setup7 = new();
			FakeMethodSetup setup3 = new();

			registry.SetupMethod(0, setup0);
			registry.SetupMethod(7, setup7);
			registry.SetupMethod(3, setup3);

			MethodSetup[]? snapshot0 = registry.GetMethodSetupSnapshot(0);
			MethodSetup[]? snapshot7 = registry.GetMethodSetupSnapshot(7);
			MethodSetup[]? snapshot3 = registry.GetMethodSetupSnapshot(3);
			await That(snapshot0).IsNotNull();
			await That(snapshot0![0]).IsSameAs(setup0);
			await That(snapshot7).IsNotNull();
			await That(snapshot7![0]).IsSameAs(setup7);
			await That(snapshot3).IsNotNull();
			await That(snapshot3![0]).IsSameAs(setup3);
		}

		[Test]
		public async Task SetupMethod_WithMemberId_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(5, setup);

			MethodSetup[]? snapshot = registry.GetMethodSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupMethod_WithMemberIdAndDefaultScenario_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(5, "", setup);

			MethodSetup[]? snapshot = registry.GetMethodSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupMethod_WithMemberIdAndNamedScenario_DoesNotPublishToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(5, "s1", setup);

			await That(registry.GetMethodSetupSnapshot(5)).IsNull();
		}

		[Test]
		public async Task SetupMethod_WithMemberIdAndNamedScenario_PreservesScenarioBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setup = new();

			registry.SetupMethod(4, "s1", setup);

			await That(registry.GetMethodSetupSnapshot(4)).IsNull();
			await That(registry.Setup.TryGetScenario("s1", out MockScenarioSetup? scoped)).IsTrue();
			await That(scoped!.Methods.Count).IsEqualTo(1);
		}

		[Test]
		public async Task SetupMethod_WithSameMemberIdTwice_AppendsToBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeMethodSetup setupA = new();
			FakeMethodSetup setupB = new();

			registry.SetupMethod(2, setupA);
			registry.SetupMethod(2, setupB);

			MethodSetup[]? snapshot = registry.GetMethodSetupSnapshot(2);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(2);
			await That(snapshot[0]).IsSameAs(setupA);
			await That(snapshot[1]).IsSameAs(setupB);
		}
	}

	public sealed class PropertyTests
	{
		[Test]
		public async Task PublishPropertyToMemberIdBucket_WhenResized_PreservesEarlierEntries()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setupLow = new(registry, "P5");
			setupLow.InitializeWith(50);
			PropertySetup<int> setupHigh = new(registry, "P10");
			setupHigh.InitializeWith(100);

			registry.SetupProperty(5, setupLow);
			registry.SetupProperty(10, setupHigh);

			await That(registry.GetPropertySetupSnapshot(5)).IsSameAs(setupLow);
			await That(registry.GetPropertySetupSnapshot(10)).IsSameAs(setupHigh);
		}

		[Test]
		public async Task PublishPropertyToMemberIdBucket_WithDefaultThenDefault_OverwritesInSnapshotTable()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup.Default<int> defaultA = new("P1", 0);
			PropertySetup.Default<int> defaultB = new("P1", 0);

			registry.SetupProperty(1, defaultA);
			registry.SetupProperty(1, defaultB);

			PropertySetup? snapshot = registry.GetPropertySetupSnapshot(1);
			await That(snapshot).IsSameAs(defaultB);
		}

		[Test]
		public async Task PublishPropertyToMemberIdBucket_WithDefaultThenUserSetup_RetainsUserSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> defaultSetup = new(registry, "P1");
			defaultSetup.InitializeWith(0);
			PropertySetup<int> userSetup = new(registry, "P1");
			userSetup.InitializeWith(99);

			registry.SetupProperty(1, new PropertySetup.Default<int>("P1", 0));
			registry.SetupProperty(1, userSetup);

			int observed = registry.GetPropertyFast(1, new PropertyGetterAccess("P1"), _ => -1);
			await That(observed).IsEqualTo(99);
		}

		[Test]
		public async Task PublishPropertyToMemberIdBucket_WithUserThenDefault_RetainsUserSetupInSnapshotTable()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> userSetup = new(registry, "P1");
			userSetup.InitializeWith(99);

			registry.SetupProperty(1, userSetup);
			registry.SetupProperty(1, new PropertySetup.Default<int>("P1", 0));

			PropertySetup? snapshot = registry.GetPropertySetupSnapshot(1);
			await That(snapshot).IsSameAs(userSetup);
		}

		[Test]
		public async Task PublishPropertyToMemberIdBucket_WithUserThenDefaultSetup_RetainsUserSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> userSetup = new(registry, "P1");
			userSetup.InitializeWith(99);

			registry.SetupProperty(1, userSetup);
			registry.SetupProperty(1, new PropertySetup.Default<int>("P1", 0));

			int observed = registry.GetPropertyFast(1, new PropertyGetterAccess("P1"), _ => -1);
			await That(observed).IsEqualTo(99);
		}

		[Test]
		public async Task SetupProperty_DefaultDoesNotOverwriteUserSetupAtSameMemberIdInSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> userSetup = new(registry, "Pg");
			userSetup.InitializeWith(7);

			registry.SetupProperty(1, userSetup);
			registry.SetupProperty(1, new PropertySetup.Default<int>("Pg", 0));

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNotNull();
			await That(table![1]).IsSameAs(userSetup);
		}

		[Test]
		public async Task SetupProperty_UserSetupDoesOverwriteDefaultAtSameMemberIdInSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> userSetup = new(registry, "Pg");
			userSetup.InitializeWith(7);
			PropertySetup.Default<int> defaultSetup = new("Pg", 0);

			registry.SetupProperty(1, defaultSetup);
			registry.SetupProperty(1, userSetup);

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNotNull();
			await That(table![1]).IsSameAs(userSetup);
		}

		[Test]
		public async Task SetupProperty_WithGrowingMemberIds_PreservesEarlierEntriesViaArrayCopy()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setupA = new(registry, "Pa");
			setupA.InitializeWith(1);
			PropertySetup<int> setupB = new(registry, "Pb");
			setupB.InitializeWith(2);

			registry.SetupProperty(0, setupA);
			registry.SetupProperty(5, setupB);

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNotNull();
			await That(table!.Length).IsGreaterThanOrEqualTo(6);
			await That(table[0]).IsSameAs(setupA);
			await That(table[5]).IsSameAs(setupB);
		}

		[Test]
		public async Task SetupProperty_WithIncreasingMemberIds_GrowsTableLazily()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup0 = new(registry, "P0");
			setup0.InitializeWith(10);
			PropertySetup<int> setup7 = new(registry, "P7");
			setup7.InitializeWith(70);
			PropertySetup<int> setup3 = new(registry, "P3");
			setup3.InitializeWith(30);

			registry.SetupProperty(0, setup0);
			registry.SetupProperty(7, setup7);
			registry.SetupProperty(3, setup3);

			await That(registry.GetPropertyFast(0, new PropertyGetterAccess("P0"), _ => -1)).IsEqualTo(10);
			await That(registry.GetPropertyFast(7, new PropertyGetterAccess("P7"), _ => -1)).IsEqualTo(70);
			await That(registry.GetPropertyFast(3, new PropertyGetterAccess("P3"), _ => -1)).IsEqualTo(30);
		}

		[Test]
		public async Task SetupProperty_WithMemberId_PopulatesSnapshotSlot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P3");
			setup.InitializeWith(33);

			registry.SetupProperty(3, setup);

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNotNull();
			await That(table!.Length).IsGreaterThanOrEqualTo(4);
			await That(table[3]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupProperty_WithMemberId_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P5");
			setup.InitializeWith(50);

			registry.SetupProperty(5, setup);

			int observed = registry.GetPropertyFast(5, new PropertyGetterAccess("P5"), _ => -1);
			await That(observed).IsEqualTo(50);
		}

		[Test]
		public async Task SetupProperty_WithMemberId_PublishesUserSetupToSnapshotTable()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P5");
			setup.InitializeWith(50);

			registry.SetupProperty(5, setup);

			PropertySetup? snapshot = registry.GetPropertySetupSnapshot(5);
			await That(snapshot).IsSameAs(setup);
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndDefaultScenario_PopulatesSnapshotSlot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P3");
			setup.InitializeWith(33);

			registry.SetupProperty(3, "", setup);

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNotNull();
			await That(table![3]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndDefaultScenario_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P5");
			setup.InitializeWith(50);

			registry.SetupProperty(5, "", setup);

			int observed = registry.GetPropertyFast(5, new PropertyGetterAccess("P5"), _ => -1);
			await That(observed).IsEqualTo(50);
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndDefaultScenario_PublishesUserSetupToSnapshotTable()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P5");
			setup.InitializeWith(50);

			registry.SetupProperty(5, "", setup);

			PropertySetup? snapshot = registry.GetPropertySetupSnapshot(5);
			await That(snapshot).IsSameAs(setup);
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndNamedScenario_DoesNotPublishToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P5");
			setup.InitializeWith(50);

			registry.SetupProperty(5, "s1", setup);

			int observed = registry.GetPropertyFast(5, new PropertyGetterAccess("P5"), _ => -1);
			await That(observed).IsEqualTo(-1);
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndNamedScenario_LeavesSnapshotUnpublished()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setup = new(registry, "P3");
			setup.InitializeWith(33);

			registry.SetupProperty(3, "scope", setup);

			PropertySetup?[]? table = GetPropertySnapshotTable(registry);
			await That(table).IsNull();
		}

		[Test]
		public async Task SetupProperty_WithMemberIdAndNamedScenario_PreservesScenarioBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakePropertySetup setup = new("P4");

			registry.SetupProperty(4, "s1", setup);

			await That(registry.Setup.TryGetScenario("s1", out MockScenarioSetup? scoped)).IsTrue();
			await That(scoped!.Properties.TryGetValue("P4", out PropertySetup? scopedSetup)).IsTrue();
			await That(scopedSetup).IsSameAs(setup);
		}

		[Test]
		public async Task SetupProperty_WithSameMemberIdTwice_RetainsLatestUserSetup()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			PropertySetup<int> setupA = new(registry, "P2");
			setupA.InitializeWith(20);
			PropertySetup<int> setupB = new(registry, "P2");
			setupB.InitializeWith(99);

			registry.SetupProperty(2, setupA);
			registry.SetupProperty(2, setupB);

			int observed = registry.GetPropertyFast(2, new PropertyGetterAccess("P2"), _ => -1);
			await That(observed).IsEqualTo(99);
		}
	}

	public sealed class IndexerTests
	{
		[Test]
		public async Task SetupIndexer_WithIncreasingMemberIds_GrowsTableLazily()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup0 = new(true);
			FakeIndexerSetup setup7 = new(true);
			FakeIndexerSetup setup3 = new(true);

			registry.SetupIndexer(0, setup0);
			registry.SetupIndexer(7, setup7);
			registry.SetupIndexer(3, setup3);

			IndexerSetup[]? snapshot0 = registry.GetIndexerSetupSnapshot(0);
			IndexerSetup[]? snapshot7 = registry.GetIndexerSetupSnapshot(7);
			IndexerSetup[]? snapshot3 = registry.GetIndexerSetupSnapshot(3);
			await That(snapshot0).IsNotNull();
			await That(snapshot0![0]).IsSameAs(setup0);
			await That(snapshot7).IsNotNull();
			await That(snapshot7![0]).IsSameAs(setup7);
			await That(snapshot3).IsNotNull();
			await That(snapshot3![0]).IsSameAs(setup3);
		}

		[Test]
		public async Task SetupIndexer_WithMemberId_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(5, setup);

			IndexerSetup[]? snapshot = registry.GetIndexerSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupIndexer_WithMemberIdAndDefaultScenario_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(5, "", setup);

			IndexerSetup[]? snapshot = registry.GetIndexerSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupIndexer_WithMemberIdAndNamedScenario_DoesNotPublishToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(5, "s1", setup);

			await That(registry.GetIndexerSetupSnapshot(5)).IsNull();
		}

		[Test]
		public async Task SetupIndexer_WithMemberIdAndNamedScenario_PreservesScenarioBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setup = new(true);

			registry.SetupIndexer(4, "s1", setup);

			await That(registry.GetIndexerSetupSnapshot(4)).IsNull();
			await That(registry.Setup.TryGetScenario("s1", out MockScenarioSetup? scoped)).IsTrue();
			await That(scoped!.Indexers.Count).IsEqualTo(1);
		}

		[Test]
		public async Task SetupIndexer_WithSameMemberIdTwice_AppendsToBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			FakeIndexerSetup setupA = new(true);
			FakeIndexerSetup setupB = new(true);

			registry.SetupIndexer(2, setupA);
			registry.SetupIndexer(2, setupB);

			IndexerSetup[]? snapshot = registry.GetIndexerSetupSnapshot(2);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(2);
			await That(snapshot[0]).IsSameAs(setupA);
			await That(snapshot[1]).IsSameAs(setupB);
		}
	}

	public sealed class EventTests
	{
		[Test]
		public async Task SetupEvent_WithIncreasingMemberIds_GrowsTableLazily()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup0 = new(registry, "E0");
			EventSetup setup7 = new(registry, "E7");
			EventSetup setup3 = new(registry, "E3");

			registry.SetupEvent(0, setup0);
			registry.SetupEvent(7, setup7);
			registry.SetupEvent(3, setup3);

			EventSetup[]? snapshot0 = registry.GetEventSetupSnapshot(0);
			EventSetup[]? snapshot7 = registry.GetEventSetupSnapshot(7);
			EventSetup[]? snapshot3 = registry.GetEventSetupSnapshot(3);
			await That(snapshot0).IsNotNull();
			await That(snapshot0![0]).IsSameAs(setup0);
			await That(snapshot7).IsNotNull();
			await That(snapshot7![0]).IsSameAs(setup7);
			await That(snapshot3).IsNotNull();
			await That(snapshot3![0]).IsSameAs(setup3);
		}

		[Test]
		public async Task SetupEvent_WithMemberId_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "Evt");

			registry.SetupEvent(5, setup);

			EventSetup[]? snapshot = registry.GetEventSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupEvent_WithMemberIdAndDefaultScenario_PublishesToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "Evt");

			registry.SetupEvent(5, "", setup);

			EventSetup[]? snapshot = registry.GetEventSetupSnapshot(5);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(1);
			await That(snapshot[0]).IsSameAs(setup);
		}

		[Test]
		public async Task SetupEvent_WithMemberIdAndNamedScenario_DoesNotPublishToSnapshot()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "Evt");

			registry.SetupEvent(5, "s1", setup);

			await That(registry.GetEventSetupSnapshot(5)).IsNull();
		}

		[Test]
		public async Task SetupEvent_WithMemberIdAndNamedScenario_PreservesScenarioBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setup = new(registry, "Evt");

			registry.SetupEvent(4, "s1", setup);

			await That(registry.GetEventSetupSnapshot(4)).IsNull();
			await That(registry.Setup.TryGetScenario("s1", out MockScenarioSetup? scoped)).IsTrue();
			await That(scoped!.Events.Count).IsEqualTo(1);
		}

		[Test]
		public async Task SetupEvent_WithSameMemberIdTwice_AppendsToBucket()
		{
			MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));
			EventSetup setupA = new(registry, "Evt");
			EventSetup setupB = new(registry, "Evt");

			registry.SetupEvent(2, setupA);
			registry.SetupEvent(2, setupB);

			EventSetup[]? snapshot = registry.GetEventSetupSnapshot(2);
			await That(snapshot).IsNotNull();
			await That(snapshot!.Length).IsEqualTo(2);
			await That(snapshot[0]).IsSameAs(setupA);
			await That(snapshot[1]).IsSameAs(setupB);
		}
	}
}
