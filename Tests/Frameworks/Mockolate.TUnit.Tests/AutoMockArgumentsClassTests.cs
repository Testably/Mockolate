using Mockolate.Integrations.TUnit;
using Mockolate.Frameworks.TestHelpers.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockArgumentsClassTests
{
	[Test]
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

	[Test]
	[AutoMockArguments]
	public async Task SealedClassParameter_ShouldBeARealInstanceWithTheParameterMocks(SealedOrderService sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(SealedOrderService));
		await That(sut.Repository).IsSameAs(repository);
	}

	[Test]
	[AutoMockArguments]
	public async Task RecordParameter_ShouldBeARealInstanceWithAValueFromTheBehavior(OrderRecord sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(OrderRecord));
		await That(sut.Repository).IsSameAs(repository);
		await That(sut.Name).IsEqualTo("");
	}

	[Test]
	[AutoMockArguments]
	public async Task ConstructorDependency_WithoutTestParameter_ShouldBeReachableThroughTheMock(DiscountService sut)
	{
		ITaxCalculator taxCalculator = sut.Mock.ConstructorArgument<ITaxCalculator>();
		taxCalculator.Mock.Setup.GetTax(10m).Returns(1m);

		await That(sut.GetTotal(10m)).IsEqualTo(1m);
	}

	[Test]
	[AutoMockArguments]
	public async Task ValueParameters_ShouldComeFromTheDefaultValues(string text, int number, int[] values)
	{
		await That(text).IsEqualTo("");
		await That(number).IsEqualTo(0);
		await That(values).IsEmpty();
	}

	[Test]
	[AutoMockArguments]
	[Repeat(2)]
	public async Task EveryInvocation_ShouldGetNewMocks(IOrderRepository repository)
	{
		repository.Find(1);

		await That(repository.Mock.Verify.Find(1)).Once();
	}
}
