using System.Collections.Generic;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class MockRegistryTests
{
	[Test]
	public async Task GetUnusedSetups_IndexerSetup_ShouldHaveCorrectString()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup["Dark"].Returns(5);
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("int this[\"Dark\"]");
	}

	[Test]
	public async Task GetUnusedSetups_MethodSetup_ShouldHaveCorrectString()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.DoSomething(null, 3.5, It.IsAny<string>()).DoesNotThrow();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("void DoSomething(null, 3.5, It.IsAny<string>())");
	}

	[Test]
	public async Task GetUnusedSetups_PropertySetup_ShouldHaveCorrectString()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.TotalDispensed.InitializeWith(4);
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("int TotalDispensed");
	}

	[Test]
	public async Task GetUnusedSetups_WithNullableDouble_ShouldHaveCorrectString()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.DoSomething(new DateTime(2026, 4, 1, 12, 0, 0), 3.5).DoesNotThrow();
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

		ISetup setup = await That(result).HasSingle();
		await That(setup.ToString()).IsEqualTo("void DoSomething(04/01/2026 12:00:00, 3.5)");
	}

	[Test]
	public async Task SetProperty_WithoutMemberId_SkippingBaseClass_ShouldReturnTrue()
	{
		MockBehavior behavior = MockBehavior.Default.SkippingBaseClass();
		MockRegistry registry = new(behavior, new FastMockInteractions(0, behavior.SkipInteractionRecording));

		bool result = registry.SetProperty("Foo", 42);

		await That(result).IsTrue();
	}
}
