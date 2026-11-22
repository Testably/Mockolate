using Mockolate.XUnit.Tests.Domain;
using Xunit;

namespace Mockolate.XUnit.Tests;

public sealed class AutoMockDataTests
{
	[Theory]
	[AutoMockData]
	public async Task AutoMockData_ShouldPassTheParameterMocksToTheClassUnderTest(OrderService sut,
		IOrderRepository repository, IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Once();
	}

	[Theory]
	[AutoMockData]
	public async Task AutoMockData_WhenTheClassUnderTestComesLast_ShouldPassTheParameterMocks(
		IOrderRepository repository, OrderService sut)
	{
		bool result = sut.Ship(7);

		await That(result).IsFalse();
		await That(repository.Mock.Verify.Find(7)).Once();
	}

	[Theory]
	[AutoMockData("EUR")]
	public async Task AutoMockData_WithArgument_ShouldPassItToTheFirstParameterAndTheClassUnderTest(
		string currency, PricingService sut, ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		string result = sut.GetPrice(new Order(1, 100m));

		await That(currency).IsEqualTo("EUR");
		await That(result).IsEqualTo("120 EUR");
	}

	[Theory]
	[AutoMockData(1)]
	[AutoMockData(2)]
	public async Task AutoMockData_WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[Theory]
	[AutoMockData]
	public async Task AutoMockData_WithTransientParameter_ShouldNotShareItWithTheClassUnderTest(
		OrderService sut, IOrderRepository repository, [Transient] IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Never();
	}

	[Theory]
	[AutoMockData]
	public async Task AutoMockData_WithSeveralParametersOfTheType_ShouldMatchThemByName(TransferService sut,
		IAccount target, IAccount source)
	{
		sut.Transfer(10m);

		await That(source.Mock.Verify.Withdraw(10m)).Once();
		await That(target.Mock.Verify.Deposit(10m)).Once();
	}
}
