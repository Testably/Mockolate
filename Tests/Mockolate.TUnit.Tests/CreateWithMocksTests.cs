using Mockolate.Exceptions;
using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class CreateWithMocksTests
{
	[Test]
	public async Task CreateWithMocks_ShouldPassMocksToTheConstructor()
	{
		Order order = new(42, 10m);
		OrderService sut = OrderService.CreateWithMocks(out Mock.OrderServiceMocks mocks);
		mocks.Repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(mocks.ShippingClient.Mock.Verify.Ship(order)).Once();
	}

	[Test]
	public async Task CreateWithMocks_WithArgument_ShouldUseItInsteadOfAMock()
	{
		IOrderRepository repository = IOrderRepository.CreateMock();

		OrderService.CreateWithMocks(out Mock.OrderServiceMocks mocks, repository: repository);

		await That(mocks.Repository).IsSameAs(repository);
	}

	[Test]
	public async Task CreateWithMocks_WithUnmockableParameter_ShouldRequireTheArgument()
	{
		PricingService sut = PricingService.CreateWithMocks(out Mock.PricingServiceMocks mocks, "EUR");
		mocks.TaxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		string result = sut.GetPrice(new Order(1, 100m));

		await That(result).IsEqualTo("120 EUR");
	}

	[Test]
	public async Task CreateWithMocks_WithMockBehavior_ShouldApplyItToEveryMock()
	{
		OrderService sut = OrderService.CreateWithMocks(out _, MockBehavior.Default.ThrowingWhenNotSetup());

		void Act()
			=> sut.Ship(1);

		await That(Act).Throws<MockNotSetupException>();
	}
}
