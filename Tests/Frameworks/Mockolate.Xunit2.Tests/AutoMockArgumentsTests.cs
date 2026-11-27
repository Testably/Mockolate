using System.Linq;
using System.Reflection;
using Mockolate.Integrations.Xunit;
using Mockolate.Frameworks.TestHelpers.Domain;

namespace Mockolate.Xunit2.Tests;

public sealed class AutoMockArgumentsTests
{
	[Test]
	public async Task GetData_ShouldPassTheParameterMocksToTheClassUnderTest()
	{
		object[] row = GetSingleRow(nameof(SharedMocks));
		OrderService sut = (OrderService)row[0];
		IOrderRepository repository = (IOrderRepository)row[1];
		repository.Mock.Setup.Find(42).Returns(new Order(42, 10m));

		sut.Ship(42);

		await That(repository.Mock.Verify.Find(42)).Once();
	}

	[Test]
	public async Task GetData_WithArgument_ShouldPassItToTheFirstParameter()
	{
		object[] row = GetSingleRow(nameof(WithArgument));

		await That(row[0]).IsEqualTo(7);
		await That(row[1]).Is<IOrderRepository>();
	}

	[Test]
	public async Task GetData_WithNotSharedParameter_ShouldNotShareIt()
	{
		object[] row = GetSingleRow(nameof(NotShared));
		OrderService sut = (OrderService)row[0];
		IOrderRepository repository = (IOrderRepository)row[1];
		IShippingClient shippingClient = (IShippingClient)row[2];
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		sut.Ship(42);

		await That(shippingClient.Mock.Verify.Ship(order)).Never();
	}

	[Test]
	public async Task GetData_WithAsDefaultParameter_ShouldReturnItFromTheOtherMocks()
	{
		object[] row = GetSingleRow(nameof(AsDefault));
		IWarehouse warehouse = (IWarehouse)row[0];

		await That(warehouse.Repository).IsSameAs(row[1]);
	}

	[Test]
	public async Task GetData_WithDeferredParameter_ShouldCreateTheClassUnderTestWithTheSetupsOfTheTest()
	{
		object[] row = GetSingleRow(nameof(Deferred));
		Func<StockIndicator> createSut = (Func<StockIndicator>)row[0];
		IChocolateStock stock = (IChocolateStock)row[1];
		stock.Mock.Setup.Count.Returns(5);

		StockIndicator sut = createSut();

		await That(sut.IsAvailable).IsTrue();
		await That(sut.Stock).IsSameAs(stock);
	}

	[Test]
	public async Task GetData_WithValueFromParameter_ShouldPassTheValueToTheClassUnderTest()
	{
		object[] row = GetSingleRow(nameof(ValueFrom));
		PriceCalculator sut = (PriceCalculator)row[0];

		await That(sut.Rule).IsSameAs(row[1]);
		await That(((DiscountRule)row[1]).Percentage).IsEqualTo(10m);
	}

	private static object[] GetSingleRow(string methodName)
	{
		MethodInfo method = typeof(AutoMockArgumentsTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
		return method.GetCustomAttribute<AutoMockArgumentsAttribute>()!.GetData(method).Single();
	}

	[AutoMockArguments]
	private static void SharedMocks(OrderService sut, IOrderRepository repository, IShippingClient shippingClient)
	{
	}

	[AutoMockArguments(7)]
	private static void WithArgument(int id, IOrderRepository repository)
	{
	}

	[AutoMockArguments]
	private static void NotShared(OrderService sut, IOrderRepository repository,
		[NotShared] IShippingClient shippingClient)
	{
	}

	[AutoMockArguments]
	private static void AsDefault(IWarehouse warehouse, [AsDefault] IOrderRepository repository)
	{
	}

	[AutoMockArguments]
	private static void Deferred([Deferred] Func<StockIndicator> createSut, IChocolateStock stock)
	{
	}

	[AutoMockArguments]
	private static void ValueFrom(PriceCalculator sut, [ValueFrom(nameof(TenPercentOff))] DiscountRule rule)
	{
	}

	private static DiscountRule TenPercentOff => new()
	{
		Percentage = 10m,
	};
}
