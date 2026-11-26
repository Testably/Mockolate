using System.Runtime.CompilerServices;
using Mockolate.Integrations.Xunit;
using Mockolate.Frameworks.TestHelpers.Domain;
using Xunit;

namespace Mockolate.Xunit.Tests;

public sealed class AutoMockArgumentsTests
{
	[ModuleInitializer]
	internal static void Configure()
		=> AutoMockArgumentsAttribute.DefaultBehavior =
			() => MockBehavior.Default.WithDefaultValueFor(() => new ConfiguredValue(42));

	[Theory]
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

	[Theory]
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

	[Theory]
	[AutoMockArguments]
	public async Task SealedClassParameter_ShouldBeARealInstanceWithTheParameterMocks(SealedOrderService sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(SealedOrderService));
		await That(sut.Repository).IsSameAs(repository);
	}

	[Theory]
	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[Theory]
	[AutoMockArguments(null)]
	public async Task AutoMockArguments_WithSingleNullArgument_ShouldPassNull(string? name,
		OptionalArgumentsService sut)
	{
		await That(name).IsNull();
		await That(sut.Name).IsNull();
	}

	[Theory]
	[AutoMockArguments]
	public async Task AsDefault_ShouldBeReturnedByMembersWithoutSetup(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository)
		=> await That(warehouse.Repository).IsSameAs(repository);

	[Theory]
	[AutoMockArguments]
	public async Task AsDefault_WhenNotUsedInTheTest_ShouldNotReportItAsUnused(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository)
		=> await That(warehouse.GetRepository()).IsSameAs(warehouse.Repository);

	[Theory]
	[AutoMockArguments]
	public async Task AsDefault_WithType_ShouldBePassedToTheClassUnderTestAndReturnedForTheType(OrderService sut,
		IWarehouse warehouse, [AsDefault(typeof(IOrderRepository))] InMemoryOrderStore store)
	{
		store.Orders.Add(new Order(42, 10m));

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(warehouse.Repository).IsSameAs(store);
	}

	[Theory]
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

	[Theory]
	[AutoMockArguments]
	public async Task DefaultBehavior_AssignedInModuleInitializer_ShouldApplyToTheArguments(ConfiguredValue value)
		=> await That(value.Id).IsEqualTo(42);

	[Theory]
	[AutoMockArguments("name", 5)]
	public async Task AutoMockArguments_WithSeveralArguments_ShouldNotPassThemToTheClassUnderTest(string name,
		int retries, OptionalArgumentsService sut)
	{
		await That(name).IsEqualTo("name");
		await That(retries).IsEqualTo(5);
		await That(sut.Name).IsNull();
	}

	[Theory]
	[AutoMockArguments]
	public async Task DisplayName_ShouldNotReadTheProperties(IChocolateStock stock)
		=> await That(stock.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue();

	[Theory]
	[AutoMockArguments("Dark\n", 4)]
	public async Task DisplayName_ShouldDescribeTheMocks(string type, int amount, IChocolateStock stock)
	{
		await That(type).IsEqualTo("Dark\n");
		await That(amount).IsEqualTo(4);
		await That(stock.ToString()).IsEqualTo("Mockolate.Frameworks.TestHelpers.Domain.IChocolateStock mock");
		// Runners choose whether the name starts with the class, as the default does, or only with the method, as Rider's does.
		await That(TestContext.Current.Test?.TestDisplayName).EndsWith(
			"DisplayName_ShouldDescribeTheMocks(type: \"Dark\\n\", amount: 4, stock: Mockolate.Frameworks.TestHelpers.Domain.IChocolateStock mock)");
	}

	[Theory]
	[AutoMockArguments(Traits = ["Category", "Chocolate",])]
	public async Task Traits_ShouldApplyToTheTestCase(IChocolateStock stock)
	{
		await That(stock).IsNotNull();
		await That(TestContext.Current.Test?.Traits["Category"]).IsEqualTo(["Chocolate",]);
	}

	[Theory]
	[AutoMockArguments(Explicit = true)]
	public void Explicit_ShouldNotRunByDefault(IChocolateStock stock)
		=> throw new InvalidOperationException($"An explicit test case ran with {stock}.");

	[Theory]
	[AutoMockArguments(Skip = "The attribute skips it.")]
	public void Skip_ShouldSkipTheTestCase(IChocolateStock stock)
		=> throw new InvalidOperationException($"A skipped test case ran with {stock}.");

	[Theory]
	[InlineData(3)]
	[AutoMockArguments(4)]
	public async Task AutoMockArguments_WithInlineData_ShouldRunBoth(int id)
		=> await That(id).IsOneOf(3, 4);

	public readonly struct ConfiguredValue(int id)
	{
		public int Id { get; } = id;
	}
}
