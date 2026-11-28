using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Mockolate.Integrations.NUnit;
using Mockolate.Frameworks.TestHelpers.Domain;
using Mockolate.Verify;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Internal.ExecutionHooks;

namespace Mockolate.NUnit.Tests;

public sealed class AutoMockArgumentsTests
{
	private static int _retries;

	[ModuleInitializer]
	internal static void Configure()
		=> AutoMockArgumentsAttribute.DefaultBehavior =
			() => MockBehavior.Default.WithDefaultValueFor(() => new ConfiguredValue(42));

	[AutoMockArguments]
	public async Task ShouldPassTheParameterMocksToTheClassUnderTest(OrderService sut,
		IOrderRepository repository, IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Once();
	}

	[AutoMockArguments]
	public async Task WithClassParameter_ShouldForwardToTheRealImplementationUntilItIsSetUp(DiscountService sut,
		ITaxCalculator taxCalculator)
	{
		taxCalculator.Mock.Setup.GetTax(100m).Returns(20m);

		decimal forwarded = sut.GetTotal(100m);
		sut.Mock.Setup.GetDiscount().Returns(50m);

		await That(forwarded).IsEqualTo(110m);
		await That(sut.GetTotal(100m)).IsEqualTo(70m);
	}

	[AutoMockArguments]
	public async Task WithSealedClassParameter_ShouldPassARealInstanceWithTheParameterMocks(SealedOrderService sut,
		IOrderRepository repository)
	{
		await That(sut.GetType()).IsEqualTo(typeof(SealedOrderService));
		await That(sut.Repository).IsSameAs(repository);
	}

	[AutoMockArguments(1)]
	[AutoMockArguments(2)]
	public async Task WithSeveralAttributes_ShouldRunOneTestCaseForEach(int id, OrderService sut,
		IOrderRepository repository)
	{
		sut.Ship(id);

		await That(repository.Mock.Verify.Find(id)).Once();
	}

	[AutoMockArguments(null)]
	public async Task WithSingleNullArgument_ShouldPassNull(string? name,
		OptionalArgumentsService sut)
	{
		await That(name).IsNull();
		await That(sut.Name).IsNull();
	}

	[AutoMockArguments]
	public async Task WithNotSharedParameter_ShouldNotShareItWithTheClassUnderTest(
		OrderService sut, IOrderRepository repository, [NotShared] IShippingClient shippingClient)
	{
		Order order = new(42, 10m);
		repository.Mock.Setup.Find(42).Returns(order);

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(shippingClient.Mock.Verify.Ship(order)).Never();
	}

	[AutoMockArguments]
	public async Task Deferred_ShouldCreateTheClassUnderTestWithTheSetupsOfTheTest(
		[Deferred] Func<StockIndicator> createSut, IChocolateStock stock)
	{
		stock.Mock.Setup.Count.Returns(5);

		StockIndicator sut = createSut();

		await That(sut.IsAvailable).IsTrue();
		await That(sut.Stock).IsSameAs(stock);
	}

	[AutoMockArguments]
	public async Task ValueFrom_ShouldPassTheValueToTheClassUnderTest(PriceCalculator sut,
		[ValueFrom(nameof(TenPercentOff))] DiscountRule rule)
	{
		await That(sut.Rule).IsSameAs(rule);
		await That(rule.Percentage).IsEqualTo(10m);
	}

	[AutoMockArguments]
	public async Task ValueProvider_ShouldPassTheValueToTheClassUnderTest(PriceCalculator sut,
		[Discount(25)] DiscountRule rule)
	{
		await That(sut.Rule).IsSameAs(rule);
		await That(rule.Percentage).IsEqualTo(25m);
	}

	private static DiscountRule TenPercentOff => new()
	{
		Percentage = 10m,
	};

	[AutoMockArguments]
	public async Task Deferred_WhenTheConstructorThrows_ShouldThrowWhenCalled(
		[Deferred] Func<StockIndicator> createSut, IChocolateStock stock)
	{
		stock.Mock.Setup.Count.Throws<InvalidOperationException>();

		void Act()
			=> createSut();

		await That(Act).Throws<InvalidOperationException>();
	}

	[AutoMockArguments]
	public async Task DefaultBehavior_WhenAssignedInModuleInitializer_ShouldApplyToTheArguments(ConfiguredValue value)
		=> await That(value.Id).IsEqualTo(42);

	[AutoMockArguments]
	public async Task AsDefault_ShouldBeReturnedByMembersWithoutSetup(IWarehouse warehouse,
		[AsDefault] IOrderRepository repository)
		=> await That(warehouse.Repository).IsSameAs(repository);

	[AutoMockArguments]
	public async Task AsDefault_WithType_ShouldBePassedToTheClassUnderTestAndReturnedForTheType(OrderService sut,
		IWarehouse warehouse, [AsDefault(typeof(IOrderRepository))] InMemoryOrderStore store)
	{
		store.Orders.Add(new Order(42, 10m));

		bool result = sut.Ship(42);

		await That(result).IsTrue();
		await That(warehouse.Repository).IsSameAs(store);
	}

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
	public async Task WithRepeat_ShouldGetNewMocksForEveryRun(IOrderRepository repository)
	{
		repository.Find(1);

		await That(repository.Mock.Verify.Find(1)).Once();
	}

	[AutoMockArguments]
	[Retry(2)]
	public void WithRetry_ShouldGetNewMocksForEveryAttempt(IOrderRepository repository)
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
	public async Task WithCancellationTokenParameter_ShouldPassTheTokenOfNUnit(IOrderRepository repository, CancellationToken token)
		=> await That(token.CanBeCanceled).IsTrue();

	[AutoMockArguments]
	public static async Task WhenTheTestMethodIsStatic_ShouldGetTheArguments(OrderService sut, IOrderRepository repository)
	{
		sut.Ship(3);

		await That(repository.Mock.Verify.Find(3)).Once();
	}

	[AutoMockArguments]
	public async Task WithoutParameters_ShouldRun()
		=> await That(TestContext.CurrentContext.Test.Name).IsEqualTo(nameof(WithoutParameters_ShouldRun));

	[TestCase(5, null)]
	[AutoMockArguments(6)]
	public async Task WithTestCaseAttribute_ShouldOnlyFillItsOwnTestCase(int id, IOrderRepository? repository)
		=> await That(repository is null).IsEqualTo(id == 5);

	[AutoMockArguments(double.NaN, float.PositiveInfinity, Flavor.Milk)]
	public async Task TestName_ShouldFormatSpecialValuesAndEnumsLikeCSharp(double ratio, float limit, Flavor flavor)
		=> await That(TestContext.CurrentContext.Test.Name).IsEqualTo(
			"TestName_ShouldFormatSpecialValuesAndEnumsLikeCSharp(double.NaN, float.PositiveInfinity, Flavor.Milk)");

	[AutoMockArguments]
	public async Task WithSealedClassParameterWhoseConstructorCanFail_ShouldCreateIt(FailingService sut)
		=> await That(sut).IsNotNull();

	[AutoMockArguments]
	[RecordingHook]
	public async Task WithExecutionHookAfterTheAttribute_ShouldRunAndGetTheArguments(IOrderRepository repository)
	{
		await That(repository).IsNotNull();
		await That(RecordingHookAttribute.LastTest).IsEqualTo(TestContext.CurrentContext.Test.Name);
	}

	[RecordingHook]
	[AutoMockArguments]
	public async Task WithExecutionHookBeforeTheAttribute_ShouldRunAndGetTheArguments(IOrderRepository repository)
	{
		await That(repository).IsNotNull();
		await That(RecordingHookAttribute.LastTest).IsEqualTo(TestContext.CurrentContext.Test.Name);
	}

	[Test]
	public async Task Wrap_WithTheCommandOfTheTestMethod_ShouldReplaceItWithOneThatCreatesTheArguments()
	{
		AutoMockArgumentsAttribute attribute = new();
		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithFailingService)), null)
			.Single();

		TestCommand command = attribute.Wrap(new TestMethodCommand(test));

		await That(command).Is<TestMethodCommand>();
		await That(new AutoMockArgumentsAttribute().Wrap(command)).IsSameAs(command);
	}

	[Test]
	public async Task Wrap_WithTheCommandOfTheTestMethod_ShouldCreateTheArgumentsBeforeTheMethodRuns()
	{
		AutoMockArgumentsAttribute attribute = new();
		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithFailingService)), null)
			.Single();
		TestCommand command = attribute.Wrap(new TestMethodCommand(test));

		command.Execute(CreateContext(test));

		await That(test.Arguments[0]).Is<FailingService>();
	}

	[Test]
	public async Task Wrap_WithASubclassOfTheTestMethodCommand_ShouldKeepIt()
	{
		AutoMockArgumentsAttribute attribute = new();
		TestMethod test = attribute.BuildFrom(new MethodWrapper(typeof(BuildTargets), nameof(BuildTargets.WithFailingService)), null)
			.Single();
		CountingTestMethodCommand foreign = new(test);
		TestCommand command = attribute.Wrap(foreign);

		command.Execute(CreateContext(test));

		await That(foreign.Executions).IsEqualTo(1);
		await That(test.Arguments[0]).Is<FailingService>();
	}

	private static TestExecutionContext CreateContext(Test test)
		=> new()
		{
			CurrentTest = test,
			CurrentResult = test.MakeTestResult(),
		};

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
	public async Task AutoMockArguments_ShouldGetTheArguments(IOrderRepository repository)
	{
		repository.Find(1);

		await That(type).IsEqualTo("Dark");
		await That(repository.Mock.Verify.Find(1)).Once();
		await That(TestContext.CurrentContext.Test.FullName).IsEqualTo(
			"Mockolate.NUnit.Tests.ParameterizedFixtureTests(\"Dark\").AutoMockArguments_ShouldGetTheArguments");
	}
}

public enum Flavor
{
	Dark,
	Milk,
}

public sealed class FailingService
{
	[ThreadStatic] internal static bool FailOnThisThread;

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

/// <summary>
///     An execution hook of NUnit 4.5 or later, which only accepts the command of the test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RecordingHookAttribute : ExecutionHookAttribute
{
	[ThreadStatic] internal static string? LastTest;

	public override void BeforeTestHook(HookData hookData)
		=> LastTest = TestContext.CurrentContext.Test.Name;
}

/// <summary>
///     The command of the test method as another attribute could replace it, which counts its executions.
/// </summary>
internal sealed class CountingTestMethodCommand(TestMethod testMethod) : TestMethodCommand(testMethod)
{
	public int Executions { get; private set; }

	public override TestResult Execute(TestExecutionContext context)
	{
		Executions++;
		return base.Execute(context);
	}
}
