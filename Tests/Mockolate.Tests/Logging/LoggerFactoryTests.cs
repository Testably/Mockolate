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
	public async Task ThrowingWhenNotSetup_ShouldApplyToTheLoggers()
	{
		ILoggerFactory factory = ILoggerFactory.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		ILogger logger = factory.Mock.GetLogger("Orders");

		void Act() => logger.LogError("failed");

		await That(Act).Throws<MockNotSetupException>();
	}
}
