using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mockolate.Exceptions;
using Mockolate.Frameworks.TestHelpers.Domain;
using Mockolate.Integrations.MSTest;

[assembly: TestDataSourceOptions(TestDataSourceUnfoldingStrategy.Fold)]

namespace Mockolate.MSTest.Tests;

[TestClass]
public sealed class AutoMockArgumentsTests
{
	private static int _retries;
	private static IOrderRepository? _firstRepository;

	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[AutoMockArguments]
	public async Task AutoMockArguments_ShouldPassTheParameterMocksToTheClassUnderTest(OrderService sut,
		IOrderRepository repository, IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Once();
	}

	[TestMethod]
	[AutoMockArguments]
	public async Task ClassParameter_ShouldForwardToTheRealImplementationUntilItIsSetUp(DiscountService sut,
		ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		decimal forwarded = sut.GetTotal(100m);
		sut.Mock.Setup.GetDiscount().Returns(50m);

		await That(forwarded).IsEqualTo(110m);
		await That(sut.GetTotal(100m)).IsEqualTo(70m);
	}

	[TestMethod]
	[AutoMockArguments]
	public async Task SealedClassParameter_ShouldBeARealInstanceWithTheParameterMocks(SealedOrderService sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(SealedOrderService));
		await That(sut.Repository).IsSameAs(repository);
	}

	[TestMethod]
	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[TestMethod]
	[AutoMockArguments("name", 5)]
	public async Task AutoMockArguments_WithSeveralArguments_ShouldNotPassThemToTheClassUnderTest(string name,
		int retries, OptionalArgumentsService sut)
	{
		await That(name).IsEqualTo("name");
		await That(retries).IsEqualTo(5);
		await That(sut.Name).IsNull();
	}

	[TestMethod]
	[AutoMockArguments(null)]
	public async Task AutoMockArguments_WithSingleNullArgument_ShouldPassNull(string? name,
		OptionalArgumentsService sut)
	{
		await That(name).IsNull();
		await That(sut.Name).IsNull();
	}

	[TestMethod]
	[AutoMockArguments]
	public async Task AsDefault_ShouldBeReturnedByMembersWithoutSetup(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository)
		=> await That(warehouse.Repository).IsSameAs(repository);

	[TestMethod]
	[AutoMockArguments]
	public async Task AsDefault_WithType_ShouldBePassedToTheClassUnderTestAndReturnedForTheType(OrderService sut,
		IWarehouse warehouse, [AsDefault(typeof(IOrderRepository))] InMemoryOrderStore store)
	{
		store.Orders.Add(new Order(42, 10m));

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(warehouse.Repository).IsSameAs(store);
	}

	[TestMethod]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithTransientParameter_ShouldNotShareItWithTheClassUnderTest(
		OrderService sut, IOrderRepository repository, [Transient] IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Never();
	}

	[TestMethod]
	[AutoMockArguments]
	public async Task DefaultBehavior_AssignedInModuleInitializer_ShouldApplyToTheArguments(ConfiguredValue value)
		=> await That(value.Id).IsEqualTo(42);

	[TestMethod]
	[AutoMockArguments("Hello {0}", 4L)]
	public async Task DisplayName_ShouldContainTheGivenArguments(string text, long amount, IOrderRepository repository)
		=> await That(TestContext.TestDisplayName)
			.IsEqualTo("DisplayName_ShouldContainTheGivenArguments(\"Hello {0}\", 4L)");

	[TestMethod]
	[AutoMockArguments]
	public async Task DisplayName_WithoutArguments_ShouldBeTheMethodName(IOrderRepository repository)
		=> await That(TestContext.TestDisplayName).IsEqualTo(nameof(DisplayName_WithoutArguments_ShouldBeTheMethodName));

	[TestMethod(DisplayName = "Custom name")]
	[AutoMockArguments(4)]
	public async Task DisplayName_WithCustomNameOnTheTestMethod_ShouldKeepIt(int amount, IOrderRepository repository)
		=> await That(TestContext.TestDisplayName).IsEqualTo("Custom name(4)");

	[TestMethod]
	public async Task GetData_WithMoreArgumentsThanParameters_ShouldThrowMockException()
	{
		AutoMockArgumentsAttribute attribute = new(1, 2);
		MethodInfo method = typeof(DataTargets).GetMethod(nameof(DataTargets.OneParameter))!;

		List<object?[]> Act() => attribute.GetData(method).ToList();

		await That(Act).Throws<MockException>()
			.WithMessage("AutoMockArguments has 2 arguments, but the test method has only 1 parameters.");
	}

	// The test project folds its data sources, like the documentation recommends, so every attempt gets new mocks.
	[TestMethod]
	[Retry(2)]
	[AutoMockArguments]
	public async Task Retry_ShouldGetNewMocksForEveryAttempt(IOrderRepository repository)
	{
		if (Interlocked.Increment(ref _retries) == 1)
		{
			_firstRepository = repository;
			Assert.Fail("The first attempt fails.");
		}

		await That(repository).IsNotSameAs(_firstRepository);
	}

	public readonly struct ConfiguredValue(int id)
	{
		public int Id { get; } = id;
	}
}

/// <summary>
///     Methods without a data source, so that MSTest does not run them, for which the tests create the arguments.
/// </summary>
public static class DataTargets
{
	public static void OneParameter(int id)
	{
	}
}

/// <summary>
///     Configures the arguments like the documentation recommends, in a module initializer of its own.
/// </summary>
internal static class MockolateConfiguration
{
	[ModuleInitializer]
	internal static void Configure()
		=> AutoMockArgumentsAttribute.DefaultBehavior =
			() => MockBehavior.Default.WithDefaultValueFor(() => new AutoMockArgumentsTests.ConfiguredValue(42));
}
