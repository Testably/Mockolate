using Mockolate.Integrations.TUnit;
using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockArgumentsTests
{
	[Test]
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

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WhenTheClassUnderTestComesLast_ShouldPassTheParameterMocks(
		IOrderRepository repository, OrderService sut)
	{
		bool result = sut.Ship(7);

		await That(result).IsFalse();
		await That(repository.Mock.Verify.Find(7)).Once();
	}

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithMockedTypeOnly_ShouldCreateAMock(ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		decimal result = taxCalculator.GetTax(100m);

		await That(result).IsEqualTo(20m);
	}

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithOptionalAndNullableParameters_ShouldUseTheirDefaults(
		OptionalArgumentsService sut, IOrderRepository repository)
	{
		await That(sut.Repository).IsSameAs(repository);
		await That(sut.Name).IsNull();
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Tags).IsEmpty();
	}

	[Test]
	[AutoMockArguments("EUR")]
	public async Task AutoMockArguments_WithArgument_ShouldPassItToTheFirstParameterAndTheClassUnderTest(
		string currency, PricingService sut, ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		string result = sut.GetPrice(new Order(1, 100m));

		await That(currency).IsEqualTo("EUR");
		await That(result).IsEqualTo("120 EUR");
	}

	[Test]
	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[Test]
	[AutoMockArguments("name", 5)]
	public async Task AutoMockArguments_WithSeveralArguments_ShouldPassThemToTheClassUnderTest(string name,
		int retries, OptionalArgumentsService sut)
	{
		await That(sut.Name).IsEqualTo(name);
		await That(sut.Retries).IsEqualTo(retries);
	}

	[Test]
	[AutoMockArguments(null)]
	public async Task AutoMockArguments_WithSingleNullArgument_ShouldPassNull(string? name,
		OptionalArgumentsService sut)
	{
		await That(name).IsNull();
		await That(sut.Name).IsNull();
	}

	[Test]
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

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithSeveralParametersOfTheType_ShouldMatchThemByName(TransferService sut,
		IAccount target, IAccount source)
	{
		sut.Transfer(10m);

		await That(source.Mock.Verify.Withdraw(10m)).Once();
		await That(target.Mock.Verify.Deposit(10m)).Once();
	}

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithTransientParameterOfTheType_ShouldIgnoreItWhenMatching(
		TransferService sut, IAccount source, IAccount target, [Transient] IAccount other)
	{
		sut.Transfer(10m);

		await That(source.Mock.Verify.Withdraw(10m)).Once();
		await That(target.Mock.Verify.Deposit(10m)).Once();
		await That(other.Mock.Verify.Withdraw(10m)).Never();
	}
}
