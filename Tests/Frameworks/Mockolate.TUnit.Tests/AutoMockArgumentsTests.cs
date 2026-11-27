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
		await That(warehouse.GetStock().Repository).IsSameAs(repository);
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
	public async Task AsDefault_WithType_ShouldBePassedToTheClassUnderTestAndReturnedForTheType(OrderService sut,
		IWarehouse warehouse, [AsDefault(typeof(IOrderRepository))] InMemoryOrderStore store,
		IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		store.Orders.Add(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Once();
		await That(warehouse.Repository).IsSameAs(store);
		await That(await warehouse.GetRepositoryAsync()).IsSameAs(store);
	}

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WithSeveralTypes_ShouldBePassedForEachOfThem(OrderService sut,
		[AsDefault(typeof(IOrderRepository), typeof(IShippingClient))] InMemoryOrderStore store)
	{
		Order order = new(42, 10m);
		store.Orders.Add(order);

		sut.Ship(42);

		await That(store.Shipped).IsEqualTo([order,]);
	}

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WithTypeOfAnotherParameter_ShouldMatchThemByName(OrderService sut,
		IOrderRepository repository, [AsDefault(typeof(IOrderRepository))] InMemoryOrderStore store)
	{
		sut.Ship(42);

		await That(repository.Mock.Verify.Find(42)).Once();
	}
#if NET8_0_OR_GREATER

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WithGenericType_ShouldBePassedToTheClassUnderTestAndReturnedForTheType(
		OrderService sut, IWarehouse warehouse, [AsDefault<IOrderRepository>] InMemoryOrderStore store)
	{
		store.Orders.Add(new Order(42, 10m));

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(warehouse.Repository).IsSameAs(store);
	}

	[Test]
	[AutoMockArguments]
	public async Task AsDefault_WithSeveralGenericTypes_ShouldBePassedForEachOfThem(OrderService sut,
		[AsDefault<IOrderRepository, IShippingClient>] InMemoryOrderStore store)
	{
		Order order = new(42, 10m);
		store.Orders.Add(order);

		sut.Ship(42);

		await That(store.Shipped).IsEqualTo([order,]);
	}
#endif

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
	public async Task Deferred_ShouldCreateTheClassUnderTestWithTheSetupsOfTheTest(
		[Deferred] Func<StockIndicator> createSut, IChocolateStock stock)
	{
		stock.Mock.Setup.Count.Returns(5);

		StockIndicator sut = createSut();

		await That(sut.IsAvailable).IsTrue();
		await That(sut.Stock).IsSameAs(stock);
	}

	[Test]
	[AutoMockArguments]
	public async Task Deferred_WhenTheConstructorThrows_ShouldThrowWhenCalled(
		[Deferred] Func<StockIndicator> createSut, IChocolateStock stock)
	{
		stock.Mock.Setup.Count.Throws<InvalidOperationException>();

		void Act()
			=> createSut();

		await That(Act).Throws<InvalidOperationException>();
	}

	[Test]
	[AutoMockArguments]
	public async Task Deferred_ShouldCreateANewInstanceOnEachCall([Deferred] Func<StockIndicator> createSut,
		IChocolateStock stock)
	{
		StockIndicator first = createSut();
		StockIndicator second = createSut();

		await That(first).IsNotSameAs(second);
		await That(second.Stock).IsSameAs(stock);
	}

	[Test]
	[AutoMockArguments(3)]
	public async Task Deferred_WithInterface_ShouldCreateANewMockWithTheBehaviorOnEachCall([AsDefault] int count,
		[Deferred] Func<IChocolateStock> createStock)
	{
		IChocolateStock first = createStock();
		IChocolateStock second = createStock();

		await That(first).IsNotSameAs(second);
		await That(first.Count).IsEqualTo(3);
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueFrom_ShouldPassTheValueOfThePropertyToTheClassUnderTest(PriceCalculator sut,
		ITaxCalculator taxCalculator, [ValueFrom(nameof(TenPercentOff))] DiscountRule rule)
	{
		taxCalculator.Mock.Setup.GetTax(It.IsAny<decimal>()).Returns(1m);

		decimal price = sut.GetPrice(20m);

		await That(sut.Rule).IsSameAs(rule);
		await That(rule.Percentage).IsEqualTo(10m);
		await That(price).IsEqualTo(19m);
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueFrom_WithMethod_ShouldPassItsResultBeforeTheClassUnderTestIsCreated(StockIndicator sut,
		[ValueFrom(nameof(CreateStockOfFive))] IChocolateStock stock)
	{
		await That(sut.Stock).IsSameAs(stock);
		await That(sut.IsAvailable).IsTrue().Because("the constructor already sees the setup of the member");
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueFrom_WithTransient_ShouldNotPassItToTheClassUnderTest(PriceCalculator sut,
		[Transient] [ValueFrom(nameof(TenPercentOff))] DiscountRule rule)
	{
		await That(sut.Rule).IsNotSameAs(rule);
		await That(sut.Rule.Percentage).IsEqualTo(0m).Because("the class under test gets a created rule");
		await That(rule.Percentage).IsEqualTo(10m);
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueFrom_WithAsDefault_ShouldBeReturnedByMembersWithoutSetup(IWarehouse warehouse,
		[AsDefault] [ValueFrom(nameof(CreateRepository))] IOrderRepository repository)
	{
		await That(warehouse.Repository).IsSameAs(repository);
		await That(repository.Mock.Verify.Find(1)).Once()
			.Because("the repository is the instance that the member created");
	}

	[Test]
	[AutoMockArguments(5)]
	public async Task ValueFrom_WithGivenArguments_ShouldFillTheParameterAfterThem(int amount, PriceCalculator sut,
		[ValueFrom(nameof(TenPercentOff))] DiscountRule rule)
	{
		await That(amount).IsEqualTo(5);
		await That(sut.Rule).IsSameAs(rule);
	}

	private static DiscountRule TenPercentOff => new()
	{
		ProductCode = "Dark",
		Percentage = 10m,
	};

	private static IChocolateStock CreateStockOfFive()
	{
		IChocolateStock stock = IChocolateStock.CreateMock();
		stock.Mock.Setup.Count.Returns(5);
		return stock;
	}

	private static IOrderRepository CreateRepository()
	{
		IOrderRepository repository = IOrderRepository.CreateMock();
		repository.Find(1);
		return repository;
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
