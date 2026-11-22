using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockDataTests
{
	[Test]
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

	[Test]
	[AutoMockData]
	public async Task AutoMockData_WhenTheClassUnderTestComesLast_ShouldPassTheParameterMocks(
		IOrderRepository repository, OrderService sut)
	{
		bool result = sut.Ship(7);

		await That(result).IsFalse();
		await That(repository.Mock.Verify.Find(7)).Once();
	}

	[Test]
	[AutoMockData]
	public async Task AutoMockData_WithMockedTypeOnly_ShouldCreateAMock(ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		decimal result = taxCalculator.GetTax(100m);

		await That(result).IsEqualTo(20m);
	}
}
