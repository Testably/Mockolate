using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Setup;
using Mockolate.Verify;

namespace Mockolate.Tests.Logging;

public sealed class LoggerTests
{
	[Test]
	public async Task CombinationMock_ShouldSupportLoggingSetupsAndVerifications()
	{
		int calls = 0;
		ILogger logger = ILogger.CreateMock().Implementing<IDisposable>();
		logger.Mock.Setup.LogError(It.IsAny<string>()).Do(() => calls++);

		logger.LogError("failed");

		await That(calls).IsEqualTo(1);
		await That(logger.Mock.Verify.LogError(It.Is("failed"))).Once();
	}

	[Test]
	public async Task IsEnabled_WithoutSetup_ShouldReturnTrue()
	{
		ILogger logger = ILogger.CreateMock();

		bool result = logger.IsEnabled(LogLevel.Trace);

		await That(result).IsTrue().Because("logging code that checks IsEnabled first must still reach Log");
	}

	[Test]
	public async Task IsEnabled_WithSetup_ShouldReturnSetupValue()
	{
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.IsEnabled(It.Is(LogLevel.Debug)).Returns(false);

		await That(logger.IsEnabled(LogLevel.Debug)).IsFalse();
		await That(logger.IsEnabled(LogLevel.Error)).IsTrue();
	}

	[Test]
	public async Task Setup_LogError_ShouldMatchFormattedMessageAndLevel()
	{
		List<string> messages = [];
		ILogger<LoggerTests> logger = ILogger<LoggerTests>.CreateMock();
		logger.Mock.Setup.LogError(It.Matches("*failed*")).Do((_, _, message, _) => messages.Add(message));

		logger.LogError("Order {OrderId} failed", 42);
		logger.LogWarning("Order {OrderId} failed", 43);
		logger.LogError("Order {OrderId} succeeded", 44);

		await That(messages).IsEqualTo(["Order 42 failed",]);
	}

	[Test]
	public async Task Setup_ShouldMatchAnyStateType()
	{
		int calls = 0;
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.LogInformation(It.Is("custom state")).Do(() => calls++);

		logger.Log(LogLevel.Information, new EventId(1), 42, null, (_, _) => "custom state");

		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Setup_Log_WithException_ShouldOnlyMatchMatchingException()
	{
		InvalidOperationException exception = new();
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.Log(It.IsAny<LogLevel>(), It.IsAny<string>(), It.Is<Exception?>(exception))
			.Throws<TimeoutException>();

		void ActWithSameException() => logger.LogError(exception, "with exception");
		void ActWithOtherException() => logger.LogError(new ArgumentException(), "with other exception");

		await That(ActWithSameException).Throws<TimeoutException>();
		await That(ActWithOtherException).DoesNotThrow();
	}

	[Test]
	public async Task Setup_WithSeveralMatchingSetups_ShouldUseLatest()
	{
		List<string> calls = [];
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.LogError(It.IsAny<string>()).Do(() => calls.Add("first"));
		logger.Mock.Setup.LogError(It.IsAny<string>()).Do(() => calls.Add("second"));

		logger.LogError("failed");

		await That(calls).IsEqualTo(["second",]);
	}

	[Test]
	public async Task Setup_InScenario_ShouldOnlyApplyInScenario()
	{
		int calls = 0;
		ILogger logger = ILogger.CreateMock();
		logger.Mock.InScenario("broken").Setup.LogError(It.IsAny<string>()).Do(() => calls++);

		logger.LogError("before transition");
		logger.Mock.TransitionTo("broken");
		logger.LogError("after transition");

		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithoutSetup_ShouldThrow()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act() => logger.LogError("failed");

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithMessageSetup_ShouldNotThrow()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		logger.Mock.Setup.LogError(It.IsAny<string>());

		void Act() => logger.LogError("failed");

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Verify_ShouldCountMatchingFormattedMessages()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning("Retry {Count}", 1);
		logger.LogWarning("Retry {Count}", 2);
		logger.LogError("Retry {Count}", 3);

		await That(logger.Mock.Verify.LogWarning(It.Matches("Retry *"))).Twice();
		await That(logger.Mock.Verify.LogWarning(It.Is("Retry 2"))).Once();
		await That(logger.Mock.Verify.LogCritical(It.IsAny<string>())).Never();
		await That(logger.Mock.Verify.Log(It.IsAny<LogLevel>(), It.Matches("Retry *"))).Exactly(3);
	}

	[Test]
	public async Task Verify_Log_WithException_ShouldMatchException()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogError(new InvalidOperationException("boom"), "Failed");

		await That(logger.Mock.Verify.Log(It.Is(LogLevel.Error), It.Is("Failed"),
			It.Satisfies<Exception?>(e => e is InvalidOperationException))).Once();
		await That(logger.Mock.Verify.Log(It.Is(LogLevel.Error), It.Is("Failed"),
			It.Satisfies<Exception?>(e => e is ArgumentException))).Never();
	}

	[Test]
	public async Task Verify_WhenNotMatching_ShouldDescribeExpectation()
	{
		ILogger logger = ILogger.CreateMock();

		void Act() => logger.Mock.Verify.LogError(It.Is("failed")).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*Log(*Error*failed*)*").AsWildcard();
	}

	[Test]
	public async Task VerifySetup_ShouldCountCallsMatchingTheMessageSetup()
	{
		ILogger logger = ILogger.CreateMock();
		IMethodSetup setup = logger.Mock.Setup.LogError(It.IsAny<string>());

		logger.LogError("one");
		logger.LogWarning("two");
		logger.Log(LogLevel.Error, new EventId(3), 3, null, (_, _) => "three");

		await That(logger.Mock.VerifySetup(setup)).Twice()
			.Because("the setup matches the calls of every state type");
	}

	[Test]
	public async Task VerifyThatAllSetupsAreUsed_ShouldConsiderCallsMatchingTheMessageSetup()
	{
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.LogError(It.Is("used"));
		logger.Mock.Setup.LogError(It.Is("unused"));

		logger.LogError("used");
		bool allUsedBefore = logger.Mock.VerifyThatAllSetupsAreUsed();
		logger.LogError("unused");
		bool allUsedAfter = logger.Mock.VerifyThatAllSetupsAreUsed();

		await That(allUsedBefore).IsFalse();
		await That(allUsedAfter).IsTrue();
	}

	[Test]
	public async Task Monitor_ShouldOnlyVerifyCallsWhileRunning()
	{
		ILogger logger = ILogger.CreateMock();
		MockMonitor<Mock.IMockVerifyForILogger> monitor = logger.Mock.Monitor();

		logger.LogError("before");
		using (monitor.Run())
		{
			logger.LogError("while running");
		}

		logger.LogError("after");

		await That(monitor.Verify.LogError(It.IsAny<string>())).Once();
	}
}
