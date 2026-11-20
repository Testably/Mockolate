using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;

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
		factory.Mock.GetLogger<LoggerFactoryTests>().Mock.Setup.LogError(It.IsAny<string>()).Do(() => calls++);
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

		await That(logger.Mock.Verify.LogError(It.IsAny<string>())).Twice();
		await That(factory.Mock.Verify.LogError(It.IsAny<string>())).Twice();
	}

	[Test]
	public async Task GetLogger_Generic_WhenTheTypedLoggerIsMocked_ShouldShareTheLoggerOfTheCategory()
	{
		int calls = 0;
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger<LoggerTests> logger = factory.Mock.GetLogger<LoggerTests>();
		logger.Mock.Setup.LogError(It.IsAny<string>()).Do(() => calls++);

		factory.CreateLogger<LoggerTests>().LogError("from category logger");

		await That(calls).IsEqualTo(1)
			.Because("ILogger<LoggerTests> has its own mock, whose Mock property must also work on the returned logger");
		await That(logger.Mock.Verify.LogError(It.IsAny<string>())).Once();
	}

	[Test]
	public async Task Verify_ShouldCountTheCallsOfAllLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();

		factory.CreateLogger("Orders").LogInformation("one");
		factory.CreateLogger("Payments").LogInformation("two");
		factory.CreateLogger<LoggerFactoryTests>().LogWarning("three");

		await That(factory.Mock.Verify.LogInformation(It.IsAny<string>())).Twice();
		await That(factory.Mock.Verify.Log(It.IsAny<LogLevel>(), It.IsAny<string>())).Exactly(3);
		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogInformation(It.IsAny<string>())).Once()
			.Because("each logger still counts only its own calls");
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WhenVerifiedOnTheFactory_ShouldIncludeTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		ILogger logger = factory.CreateLogger("Orders");
		logger.LogError("failed");

		bool loggerBefore = logger.Mock.VerifyThatAllInteractionsAreVerified();
		await That(factory.Mock.Verify.LogError(It.IsAny<string>())).Once();
		bool loggerAfter = logger.Mock.VerifyThatAllInteractionsAreVerified();

		await That(loggerBefore).IsFalse();
		await That(loggerAfter).IsTrue();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_WhenVerifiedOnTheLogger_ShouldIncludeTheFactory()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock();
		factory.CreateLogger("Orders").LogError("failed");
		await That(factory.Mock.Verify.CreateLogger(It.Is("Orders"))).Once();

		bool factoryBefore = factory.Mock.VerifyThatAllInteractionsAreVerified();
		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogError(It.IsAny<string>())).Once();
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

		await That(factory.Mock.GetLogger("Orders").Mock.Verify.LogError(It.IsAny<string>())).Once();

		await That(factory.Mock.GetLogger("Payments").Mock.VerifyThatAllInteractionsAreVerified()).IsFalse();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_ShouldApplyToTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		ILogger logger = factory.Mock.GetLogger("Orders");

		void Act() => logger.LogError("failed");

		await That(Act).Throws<MockNotSetupException>();
	}
}
