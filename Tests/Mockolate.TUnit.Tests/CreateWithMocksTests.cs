using Mockolate.Exceptions;
using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class CreateWithMocksTests
{
	[Test]
	public async Task CreateWithMocks_ShouldPassMocksToTheConstructor()
	{
		Order order = new(42, 10m);
		OrderService sut = OrderService.CreateWithMocks(out OrderServiceMocks mocks);
		mocks.Repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(mocks.ShippingClient.Mock.Verify.Ship(order)).Once();
	}

	[Test]
	public async Task CreateWithMocks_WithArgument_ShouldUseItInsteadOfAMock()
	{
		IOrderRepository repository = IOrderRepository.CreateMock();

		OrderService.CreateWithMocks(out OrderServiceMocks mocks, repository: repository);

		await That(mocks.Repository).IsSameAs(repository);
	}

	[Test]
	public async Task CreateWithMocks_WithUnmockableParameter_ShouldRequireTheArgument()
	{
		PricingService sut = PricingService.CreateWithMocks(out PricingServiceMocks mocks, "EUR");
		mocks.TaxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		string result = sut.GetPrice(new Order(1, 100m));

		await That(result).IsEqualTo("120 EUR");
	}

	[Test]
	public async Task CreateWithMocks_WithMockBehavior_ShouldApplyItToEveryMock()
	{
		OrderService sut = OrderService.CreateWithMocks(out OrderServiceMocks mocks,
			MockBehavior.Default.ThrowingWhenNotSetup());

		void ActOnRepository()
			=> sut.Ship(1);

		void ActOnShippingClient()
			=> mocks.ShippingClient.Ship(new Order(1, 1m));

		await That(ActOnRepository).Throws<MockNotSetupException>();
		await That(ActOnShippingClient).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task CreateWithMocks_WithPascalCaseParameter_ShouldExposeTheArgument()
	{
		PascalCaseService sut = PascalCaseService.CreateWithMocks(out PascalCaseServiceMocks mocks);

		await That(mocks.Repository).IsSameAs(sut.Repository);
	}

	[Test]
	public async Task CreateWithMocks_WithParameterNamesOfTheGeneratedMethod_ShouldRenameTheGeneratedParameters()
	{
		IShippingClient shippingClient = IShippingClient.CreateMock();

		ClashingNamesService sut = ClashingNamesService.CreateWithMocks(out ClashingNamesServiceMocks mocks_1,
			mockBehavior: shippingClient);

		await That(mocks_1.Mocks).IsSameAs(sut.Repository);
		await That(mocks_1.MockBehavior).IsSameAs(shippingClient);
		await That(mocks_1.EqualsArgument).IsSameAs(sut.TaxCalculator);
	}

	[Test]
	public async Task CreateWithMocks_WithOptionalParameters_ShouldKeepTheirDefaults()
	{
		OptionalArgumentsService sut = OptionalArgumentsService.CreateWithMocks(out OptionalArgumentsServiceMocks mocks,
			null);

		await That(sut.Name).IsNull();
		await That(sut.Retries).IsEqualTo(3);
		await That(sut.Factor).IsEqualTo(1.5m);
		await That(sut.Tags).IsEmpty();
		await That(mocks.Tags).IsSameAs(sut.Tags);
	}

	[Test]
	public async Task CreateWithMocks_WithParamsArguments_ShouldPassThem()
	{
		OptionalArgumentsService sut = OptionalArgumentsService.CreateWithMocks(out _, "name", tags: ["a", "b",]);

		await That(sut.Tags).IsEqualTo(["a", "b",]);
	}

	[Test]
	public async Task CreateWithMocks_WithNestedClass_ShouldJoinTheContainingTypeNames()
	{
		Outer.NestedService sut = Outer.NestedService.CreateWithMocks(out Outer_0NestedServiceMocks mocks);

		await That(mocks.Repository).IsSameAs(sut.Repository);
	}
}
