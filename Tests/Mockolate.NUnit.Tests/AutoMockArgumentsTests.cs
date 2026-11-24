using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Mockolate.Integrations.NUnit;
using Mockolate.TUnit.Tests.Domain;
using Mockolate.Verify;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Mockolate.NUnit.Tests;

public sealed class AutoMockArgumentsTests
{
	private static int _retries;

	[ModuleInitializer]
	internal static void Configure()
		=> AutoMockArgumentsAttribute.DefaultBehavior =
			() => MockBehavior.Default.WithDefaultValueFor(() => new ConfiguredValue(42));

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

	[AutoMockArguments]
	public async Task SealedClassParameter_ShouldBeARealInstanceWithTheParameterMocks(SealedOrderService sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(SealedOrderService));
		await That(sut.Repository).IsSameAs(repository);
	}

	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[AutoMockArguments(null)]
	public async Task AutoMockArguments_WithSingleNullArgument_ShouldPassNull(string? name,
		OptionalArgumentsService sut)
	{
		await That(name).IsNull();
		await That(sut.Name).IsNull();
	}

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

	[AutoMockArguments]
	public async Task DefaultBehavior_AssignedInModuleInitializer_ShouldApplyToTheArguments(ConfiguredValue value)
		=> await That(value.Id).IsEqualTo(42);

	[AutoMockArguments("Hello {0}", 4L)]
	public async Task TestName_ShouldContainTheGivenArgumentsAsTheyAre(string text, long amount,
		IOrderRepository repository)
		=> await That(TestContext.CurrentContext.Test.Name)
			.IsEqualTo("TestName_ShouldContainTheGivenArgumentsAsTheyAre(\"Hello {0}\", 4L)");

	[AutoMockArguments]
	public async Task TestName_WithoutArguments_ShouldBeTheMethodName(IOrderRepository repository)
		=> await That(TestContext.CurrentContext.Test.Name).IsEqualTo(nameof(TestName_WithoutArguments_ShouldBeTheMethodName));

	[AutoMockArguments]
	[Repeat(2)]
	public async Task Repeat_ShouldGetNewMocksForEveryRun(IOrderRepository repository)
	{
		repository.Find(1);

		await That(repository.Mock.Verify.Find(1)).Once();
	}

	[AutoMockArguments]
	[Retry(2)]
	public void Retry_ShouldGetNewMocksForEveryAttempt(IOrderRepository repository)
	{
		repository.Find(1);
		repository.Mock.Verify.Find(1).Once();
		if (Interlocked.Increment(ref _retries) == 1)
		{
			Assert.Fail("The first attempt fails.");
		}
	}

	[AutoMockArguments]
	[CancelAfter(60_000)]
	public async Task CancellationToken_ShouldComeFromNUnit(IOrderRepository repository, CancellationToken token)
		=> await That(token.CanBeCanceled).IsTrue();

	[AutoMockArguments]
	public static async Task StaticMethod_ShouldGetTheArguments(OrderService sut, IOrderRepository repository)
	{
		sut.Ship(3);

		await That(repository.Mock.Verify.Find(3)).Once();
	}

	[AutoMockArguments]
	public async Task TestMethod_WithoutParameters_ShouldRun()
		=> await That(TestContext.CurrentContext.Test.Name).IsEqualTo(nameof(TestMethod_WithoutParameters_ShouldRun));

	[TestCase(5, null)]
	[AutoMockArguments(6)]
	public async Task AutoMockArguments_NextToTestCase_ShouldOnlyFillItsOwnTestCase(int id, IOrderRepository? repository)
		=> await That(repository is null).IsEqualTo(id == 5);

	[AutoMockArguments(double.NaN, float.PositiveInfinity, Flavor.Milk)]
	public async Task TestName_ShouldFormatSpecialValuesAndEnumsLikeCSharp(double ratio, float limit, Flavor flavor)
		=> await That(TestContext.CurrentContext.Test.Name).IsEqualTo(
			"TestName_ShouldFormatSpecialValuesAndEnumsLikeCSharp(double.NaN, float.PositiveInfinity, Flavor.Milk)");

	[AutoMockArguments]
	public async Task SealedClassParameter_WhoseConstructorCanFail_ShouldBeCreated(FailingService sut)
		=> await That(sut).IsNotNull();

	[Test]
	public async Task Wrap_WhenTheCommandAlreadyCreatesTheArguments_ShouldKeepIt()
	{
		AutoMockArgumentsAttribute attribute = new();
		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithFailingService)), null)
			.Single();
		TestCommand command = attribute.Wrap(new ResultCommand(test));

		TestCommand wrappedTwice = new AutoMockArgumentsAttribute().Wrap(command);

		await That(wrappedTwice).IsSameAs(command);
	}

	[Test]
	public async Task CreateArguments_WhenCreationThrows_ShouldCreateThemAgainInTheNextRun()
	{
		AutoMockArgumentsAttribute attribute = new();
		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithFailingService)), null)
			.Single();
		TestCommand command = attribute.Wrap(new ResultCommand(test));
		FailingService.FailOnThisThread = true;

		void Act() => command.Execute(new TestExecutionContext());

		await That(Act).Throws<InvalidOperationException>().WithMessage("The first creation fails.");
		command.Execute(new TestExecutionContext());
		await That(test.Arguments[0]).Is<FailingService>();
	}

	[Test]
	public async Task BuildFrom_WithMoreArgumentsThanParameters_ShouldNotBeRunnable()
	{
		AutoMockArgumentsAttribute attribute = new(1, 2);

		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.OneParameter)), null)
			.Single();

		await That(test.RunState).IsEqualTo(RunState.NotRunnable);
		await That((string?)test.Properties.Get(PropertyNames.SkipReason))
			.Contains("AutoMockArguments has 2 arguments, but the test method has only 1 parameters.");
	}

	[Test]
	public async Task BuildFrom_ShouldNotCreateTheClassesUnderTest()
	{
		AutoMockArgumentsAttribute attribute = new(5);

		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithClassUnderTest)), null)
			.Single();

		await That(test.Arguments).IsEqualTo([5, null,]);
	}

	public readonly struct ConfiguredValue(int id)
	{
		public int Id { get; } = id;
	}
}

/// <summary>
///     Methods without an attribute, so that NUnit does not discover them, for which the tests build test cases.
/// </summary>
public static class BuildTargets
{
	public static void OneParameter(int id)
	{
	}

	public static void WithClassUnderTest(int id, OrderService sut)
	{
	}

	public static void WithFailingService(FailingService sut)
	{
	}
}

[TestFixture("Dark")]
public sealed class ParameterizedFixtureTests(string type)
{
	[AutoMockArguments]
	public async Task AutoMockArguments_InParameterizedFixture_ShouldGetTheArguments(IOrderRepository repository)
	{
		repository.Find(1);

		await That(type).IsEqualTo("Dark");
		await That(repository.Mock.Verify.Find(1)).Once();
		await That(TestContext.CurrentContext.Test.FullName).IsEqualTo(
			"Mockolate.NUnit.Tests.ParameterizedFixtureTests(\"Dark\").AutoMockArguments_InParameterizedFixture_ShouldGetTheArguments");
	}
}

public enum Flavor
{
	Dark,
	Milk,
}

public sealed class FailingService
{
	[ThreadStatic] public static bool FailOnThisThread;

	public FailingService(IOrderRepository repository)
	{
		Repository = repository;
		if (FailOnThisThread)
		{
			FailOnThisThread = false;
			throw new InvalidOperationException("The first creation fails.");
		}
	}

	public IOrderRepository Repository { get; }
}

/// <summary>
///     The innermost command of a test, which only returns a result.
/// </summary>
internal sealed class ResultCommand(Test test) : TestCommand(test)
{
	public override TestResult Execute(TestExecutionContext context)
		=> Test.MakeTestResult();
}
