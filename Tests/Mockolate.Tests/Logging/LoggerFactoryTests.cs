using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.Logging;

public sealed class LoggerFactoryTests
{
	[Test]
	public async Task CreateLogger_WithoutSetup_ShouldReturnOneLoggerMockPerCategory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();

		ILogger orders1 = factory.CreateLogger("Orders");
		ILogger orders2 = factory.CreateLogger("Orders");
		ILogger payments = factory.CreateLogger("Payments");

		await That(orders1).IsSameAs(orders2);
		await That(orders1).IsNotSameAs(payments);
		await That(factory.Mock.GetLogger("Orders")).IsSameAs(orders1);
	}

	[Test]
	public async Task CombinationMock_ShouldCreateLoggersAndVerifyTheirCalls()
	{
		ILoggerFactory factory = (ILoggerFactory)IChocolateDispenser.CreateMock().Implementing<ILoggerFactory>();

		factory.CreateLogger("Orders").LogError("failed");
		factory.Dispose();

		await That(factory.Mock.Verify.LogError().WithCategory("Orders")).Once();
		await That(factory.Mock.Verify.Dispose()).Once();
	}

	[Test]
	public async Task CreateLogger_WithDefaultValueForLogger_ShouldStillReturnTheLoggerMock()
	{
		ILogger custom = ILogger.CreateMock();
		ILoggerFactory factory = ILoggerFactory.CreateMock(MockBehavior.Default.WithDefaultValueFor(() => custom));

		ILogger result = factory.CreateLogger("Orders");

		await That(result).IsNotSameAs(custom).And.IsSameAs(factory.Mock.GetLogger("Orders"));
	}

	[Test]
	public async Task CreateLogger_WhenWrappingAFactory_ShouldReturnTheLoggerOfTheWrappedFactory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock().Wrapping(NullLoggerFactory.Instance);

		ILogger result = factory.CreateLogger("Orders");

		await That(result).IsSameAs(NullLogger.Instance);
	}

	[Test]
	public async Task CreateLogger_Generic_ShouldNotReturnTheLoggerMock()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerFactoryTests> logger = factory.CreateLogger<LoggerFactoryTests>();

		void Act() => _ = logger.Mock;

		await That(Act).Throws<MockException>()
			.Because("the extension method of Microsoft.Extensions.Logging wraps the logger mock in a new Logger<T>");
	}

	[Test]
	public async Task CreateLogger_WithSetup_ShouldReturnSetupValue()
	{
		ILogger custom = ILogger.CreateMock();
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.Mock.Setup.CreateLogger(It.Is("Orders")).Returns(custom);

		ILogger result = factory.CreateLogger("Orders");

		await That(result).IsSameAs(custom);
	}

	[Test]
	public async Task GetLogger_Generic_ShouldReturnTheLoggerOfTheCategoryOfTheType()
	{
		int calls = 0;
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.Mock.GetLogger<LoggerFactoryTests>().Mock.Setup.LogError().Do(() => calls++);
		ILogger<LoggerFactoryTests> logger = new Logger<LoggerFactoryTests>(factory);

		logger.LogError("failed");

		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task GetLogger_Generic_ShouldShareTheLoggerOfTheCategory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerFactoryTests> logger = factory.Mock.GetLogger<LoggerFactoryTests>();

		logger.LogError("from typed logger");
		factory.CreateLogger(typeof(LoggerFactoryTests).FullName!).LogError("from category logger");

		await That(logger.Mock.Verify.LogError()).Twice();
		await That(factory.Mock.Verify.LogError()).Twice();
	}

	[Test]
	public async Task GetLogger_Generic_ShouldShareTheSetupsOfAllMembers()
	{
		using CancellationTokenSource scope = new();
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerFactoryTests> typedLogger = factory.Mock.GetLogger<LoggerFactoryTests>();
		typedLogger.Mock.Setup.IsEnabled(It.Is(LogLevel.Debug)).Returns(false);
		typedLogger.Mock.Setup.BeginScope<int>(It.IsAny<int>()).Returns(scope);

		ILogger<LoggerFactoryTests> logger = factory.CreateLogger<LoggerFactoryTests>();

		await That(logger.IsEnabled(LogLevel.Debug)).IsFalse();
		await That(logger.BeginScope(42)).IsSameAs(scope);
	}

	[Test]
	public async Task GetLogger_Generic_WhenTheTypedLoggerIsMocked_ShouldShareTheSetupsOfAllMembers()
	{
		using CancellationTokenSource scope = new();
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerTests> typedLogger = factory.Mock.GetLogger<LoggerTests>();
		typedLogger.Mock.Setup.IsEnabled(It.Is(LogLevel.Debug)).Returns(false);
		typedLogger.Mock.Setup.BeginScope<int>(It.IsAny<int>()).Returns(scope);

		ILogger<LoggerTests> logger = factory.CreateLogger<LoggerTests>();

		await That(logger.IsEnabled(LogLevel.Debug)).IsFalse()
			.Because("the generated ILogger<LoggerTests> mock and the logger of the factory must number their members alike");
		await That(logger.BeginScope(42)).IsSameAs(scope);
	}

	[Test]
	public async Task GetLogger_Generic_WhenTheTypedLoggerIsMocked_ShouldShareTheLoggerOfTheCategory()
	{
		int calls = 0;
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerTests> logger = factory.Mock.GetLogger<LoggerTests>();
		logger.Mock.Setup.LogError().Do(() => calls++);

		factory.CreateLogger<LoggerTests>().LogError("from category logger");

		await That(calls).IsEqualTo(1)
			.Because("ILogger<LoggerTests> has its own mock, whose Mock property must also work on the returned logger");
		await That(logger.Mock.Verify.LogError()).Once();
	}

	[Test]
	public async Task Verify_ShouldCountTheCallsOfAllLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();

		factory.CreateLogger("Orders").LogInformation("one");
		factory.CreateLogger("Payments").LogInformation("two");
		factory.CreateLogger<LoggerFactoryTests>().LogWarning("three");

		await That(factory.Mock.Verify.LogInformation()).Twice();
		await That(factory.Mock.Verify.Log()).Exactly(3);
		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogInformation()).Once()
			.Because("each logger still counts only its own calls");
	}

	[Test]
	public async Task Verify_WithCategory_ShouldMatchTheCategoryOfTheLogger()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();

		factory.CreateLogger("Orders").LogInformation("one");
		factory.CreateLogger<LoggerFactoryTests>().LogInformation("two");
		factory.Mock.GetLogger<LoggerFactoryTests>().LogInformation("three");

		await That(factory.Mock.Verify.LogInformation().WithCategory("Orders")).Once();
		await That(factory.Mock.Verify.LogInformation().WithCategory<LoggerFactoryTests>()).Twice();
		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogInformation().WithCategory("Orders")).Once();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WhenVerifiedOnTheFactory_ShouldIncludeTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger logger = factory.CreateLogger("Orders");
		logger.LogError("failed");

		bool loggerBefore = logger.Mock.VerifyThatAllInteractionsAreVerified();
		await That(factory.Mock.Verify.LogError()).Once();
		bool loggerAfter = logger.Mock.VerifyThatAllInteractionsAreVerified();

		await That(loggerBefore).IsFalse();
		await That(loggerAfter).IsTrue();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WhenVerifiedOnTheLogger_ShouldIncludeTheFactory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.CreateLogger("Orders").LogError("failed");

		bool factoryBefore = factory.Mock.VerifyThatAllInteractionsAreVerified();
		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogError()).Once();
		bool factoryAfter = factory.Mock.VerifyThatAllInteractionsAreVerified();

		await That(factoryBefore).IsFalse();
		await That(factoryAfter).IsTrue();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WhenVerifiedOnOneLogger_ShouldNotIncludeOtherLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.CreateLogger("Orders").LogError("failed");
		factory.CreateLogger("Payments").LogError("failed");

		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogError()).Once();

		await That(factory.Mock.GetLogger("Payments").Mock.VerifyThatAllInteractionsAreVerified()).IsFalse();
	}

	[Test]
	public async Task ClearAllInteractions_OnOneLogger_ShouldKeepTheCallsOnTheFactory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger logger = factory.CreateLogger("Orders");
		logger.LogError("failed");

		logger.Mock.ClearAllInteractions();

		await That(logger.Mock.Verify.LogError()).Never();
		await That(factory.Mock.Verify.LogError()).Once();
	}

	[Test]
	public async Task Monitor_ShouldOnlyVerifyTheCallsOfTheLoggersWhileRunning()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger logger = factory.CreateLogger("Orders");
		MockMonitor<Mock.IMockVerifyForILoggerFactory> monitor = factory.Mock.Monitor();

		logger.LogError("before");
		using (monitor.Run())
		{
			logger.LogError("while running");
		}

		logger.LogError("after");

		await That(monitor.Verify.LogError()).Once();
	}

	[Test]
	public async Task Verify_Then_ShouldOrderLogCallsAndOtherCalls()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();

		factory.CreateLogger("Orders").LogError("failed");

		await That(factory.Mock.Verify.CreateLogger(It.IsAny<string>())).Then(m => m.LogError());
	}

	[Test]
	public async Task ThrowingWhenNotSetup_ShouldApplyToTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		ILogger logger = factory.Mock.GetLogger("Orders");

		void Act() => logger.LogError("failed");

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_CreateLogger_ShouldReturnTheLoggerOfTheCategory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		ILogger logger = factory.CreateLogger("Orders");

		await That(logger).IsSameAs(factory.Mock.GetLogger("Orders"))
			.Because("logging code creates its loggers itself, so only an unexpected log call should throw");
	}

	[Test]
	public async Task GetLogger_Generic_WithNullableCategoryType_ShouldShareTheLoggerOfTheCategory()
	{
		ILogger<string?> nullableLogger = ILogger<string?>.CreateMock();
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<string> logger = factory.Mock.GetLogger<string>();

		factory.CreateLogger<string>().LogError("from category logger");

		await That(logger.Mock.Verify.LogError()).Once()
			.Because("a mock of ILogger<string?> must not break the generated code");
		await That(nullableLogger.Mock.Verify.LogError()).Never();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_ShouldIgnoreTheCallsOfCreateLogger()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.CreateLogger<LoggerFactoryTests>().LogWarning("warning");

		bool before = factory.Mock.VerifyThatAllInteractionsAreVerified();
		await That(factory.Mock.Verify.LogWarning()).Once();
		bool after = factory.Mock.VerifyThatAllInteractionsAreVerified();

		await That(before).IsFalse();
		await That(after).IsTrue();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WithOtherUnverifiedCalls_ShouldReturnFalse()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.CreateLogger("Orders");
		factory.Dispose();

		bool result = factory.Mock.VerifyThatAllInteractionsAreVerified();

		await That(result).IsFalse();
	}

	[Test]
	public async Task ClearAllInteractions_ShouldClearTheCallsOfTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger logger = factory.CreateLogger("Orders");
		logger.LogError("failed");

		factory.Mock.ClearAllInteractions();

		await That(factory.Mock.Verify.LogError()).Never();
		await That(logger.Mock.Verify.LogError()).Never();
		await That(factory.Mock.Verify.CreateLogger(It.IsAny<string>())).Never();
	}
}
