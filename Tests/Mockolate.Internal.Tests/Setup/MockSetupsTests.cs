using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public partial class MockSetupsTests
{
	[Test]
	public async Task EventSetup_ToString_ShouldReturnEventName()
	{
		EventSetup setup = new(new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
			"global::MyCode.IMyService.SomeEvent");

		string result = setup.ToString();

		await That(result).IsEqualTo("SomeEvent");
	}

	[Test]
	[Arguments(0, 0, 0, 0, "no setups")]
	[Arguments(1, 0, 0, 0, "1 method")]
	[Arguments(2, 0, 0, 0, "2 methods")]
	[Arguments(0, 1, 0, 0, "1 property")]
	[Arguments(0, 2, 0, 0, "2 properties")]
	[Arguments(0, 0, 1, 0, "1 indexer")]
	[Arguments(0, 0, 2, 0, "2 indexers")]
	[Arguments(0, 0, 0, 1, "1 event")]
	[Arguments(0, 0, 0, 2, "2 events")]
	[Arguments(3, 5, 2, 1, "3 methods, 5 properties, 2 indexers, 1 event")]
	public async Task ToString_ShouldReturnExpectedValue(
		int methodCount, int propertyCount, int indexerCount, int eventCount, string expected)
	{
		IMyService sut = IMyService.CreateMock();
		IMock mock = (IMock)sut;

		for (int i = 0; i < methodCount; i++)
		{
			mock.MockRegistry.SetupMethod(
				new ReturnMethodSetup<int>.WithParameterCollection(mock.MockRegistry, $"my.method{i}"));
		}

		for (int i = 0; i < propertyCount; i++)
		{
			mock.MockRegistry.SetupProperty(new PropertySetup<int>(mock.MockRegistry, $"my.property{i}"));
		}

		for (int i = 0; i < indexerCount; i++)
		{
			mock.MockRegistry.SetupIndexer(new IndexerSetup<string, int>(
				mock.MockRegistry, (IParameterMatch<int>)It.IsAny<int>()));
		}

		for (int i = 0; i < eventCount; i++)
		{
			mock.MockRegistry.SetupEvent(new EventSetup(mock.MockRegistry, $"my.event{i}"));
		}

		string result = mock.MockRegistry.Setup.ToString();

		await That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task TryGetScenario_WithEmptyName_ShouldReturnTrueAndYieldRootBucket()
	{
		MockSetups setups = new();

		bool result = setups.TryGetScenario("", out MockScenarioSetup? scenario);

		await That(result).IsTrue();
		await That(scenario).IsSameAs(setups);
	}

	[Test]
	public async Task TryGetScenario_WithNullName_ShouldReturnTrueAndYieldRootBucket()
	{
		MockSetups setups = new();

		bool result = setups.TryGetScenario(null!, out MockScenarioSetup? scenario);

		await That(result).IsTrue();
		await That(scenario).IsSameAs(setups);
	}

	internal interface IMyService
	{
	}

	public static MethodInfo GetMethodInfo()
		=> typeof(MockSetupsTests).GetMethod(nameof(GetMethodInfo), BindingFlags.Static | BindingFlags.Public)!;
}
