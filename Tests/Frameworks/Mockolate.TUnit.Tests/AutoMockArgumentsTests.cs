using System.Threading;
using Mockolate.Integrations.TUnit;
using Mockolate.Frameworks.TestHelpers.Domain;

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
	public async Task AutoMockArguments_WithArgument_ShouldPassItOnlyToTheFirstParameter(
		string currency, PricingService sut, ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		string result = sut.GetPrice(new Order(1, 100m));

		await That(currency).IsEqualTo("EUR");
		await That(result).IsEqualTo("120 ");
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
	public async Task AutoMockArguments_WithSeveralArguments_ShouldNotPassThemToTheClassUnderTest(string name,
		int retries, OptionalArgumentsService sut)
	{
		await That(name).IsEqualTo("name");
		await That(retries).IsEqualTo(5);
		await That(sut.Name).IsNull();
		await That(sut.Retries).IsEqualTo(3);
	}

	[Test]
	[AutoMockArguments("name")]
	public async Task AutoMockArguments_WithTransientArgument_ShouldPassIt([Transient] string name)
		=> await That(name).IsEqualTo("name");

	[Test]
	[AutoMockArguments]
	public async Task ValueParameter_WithOtherName_ShouldNotReachTheClassUnderTest(int count, string text,
		OptionalArgumentsService sut)
	{
		await That(count).IsEqualTo(0);
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Name).IsNull();
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueParameter_WithTheNameOfTheConstructorParameter_ShouldReachTheClassUnderTest(int retries,
		OptionalArgumentsService sut)
		=> await That(sut.Retries).IsEqualTo(retries);

	[Test]
	[AutoMockArguments]
	public async Task CancellationToken_ShouldComeFromTUnit(IOrderRepository repository, CancellationToken token)
	{
		await That(repository).IsNotNull();
		await That(token.CanBeCanceled).IsTrue();
	}

	[Test]
	[AutoMockArguments]
	public async Task AutoMockArguments_WithClassesOfTheSameSelfReferencingType_ShouldPassOneToTheOther(
		ChocolateNode first, ChocolateNode second)
	{
		await That(first.Parent).IsSameAs(second);
		await That(second.Parent).IsNull();
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
	public async Task AutoMockArguments_WithOptionsDelegates_ShouldUseTheDefaultOptions(ChocolateFactory sut)
		=> await That(sut.Options.Flavor).IsEqualTo("Dark");

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
	public async Task AsDefault_ShouldBeReturnedByMembersWithoutSetup(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository)
	{
		bool found = warehouse.TryGetRepository(out IOrderRepository outRepository);

		await That(warehouse.Repository).IsSameAs(repository);
		await That(warehouse["Dark"]).IsSameAs(repository);
		await That(warehouse.GetRepository()).IsSameAs(repository);
		await That(await warehouse.GetRepositoryAsync()).IsSameAs(repository);
		await That(found).IsFalse();
		await That(outRepository).IsSameAs(repository);
	}

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WithSetupOrWrittenValue_ShouldNotBeReturned(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository, [Transient] IOrderRepository other)
	{
		warehouse.Mock.Setup.GetRepository().Returns(other);
		warehouse["Dark"] = other;

		await That(warehouse.GetRepository()).IsSameAs(other);
		await That(warehouse["Dark"]).IsSameAs(other);
		await That(warehouse["Milk"]).IsSameAs(repository);
	}

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_ShouldStillBePassedToTheClassUnderTest(OrderService sut,
		[AsDefault] IOrderRepository repository)
	{
		sut.Ship(5);

		await That(repository.Mock.Verify.Find(5)).Once();
	}

	[Test]
	[AutoMockArguments]
	public async Task WithoutAsDefault_ShouldNotReturnTheParameter(IWarehouse warehouse, IOrderRepository repository)
		=> await That(warehouse.Repository).IsNotSameAs(repository);

	[Test]
	[AutoMockArguments(42)]
	public async Task AsDefault_WithGivenValue_ShouldBeReturnedForItsType([AsDefault] int count, IChocolateStock stock)
		=> await That(stock.Count).IsEqualTo(42);

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WhenCreatedFromTheDefaultValues_ShouldNotAskForItself([AsDefault] int count,
		IChocolateStock stock)
	{
		await That(count).IsEqualTo(0);
		await That(stock.Count).IsEqualTo(0);
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
