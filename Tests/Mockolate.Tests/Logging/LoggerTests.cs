#pragma warning disable CA1873 // Avoid potentially expensive logging
using System.Collections.Generic;
using System.Threading;
using aweXpect.Chronology;
using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Setup;
using Mockolate.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Logging;

public sealed class LoggerTests
{
	[Test]
	public async Task CombinationMock_ShouldSupportLoggingSetupsAndVerifications()
	{
		int calls = 0;
		ILogger logger = ILogger.CreateMock().Implementing<IDisposable>();
		logger.Mock.Setup.LogError().Do(() => calls++);

		logger.LogError("failed");

		await That(calls).IsEqualTo(1);
		await That(logger.Mock.Verify.LogError().WithMessage("failed")).Once();
	}

	[Test]
	public async Task CombinationMock_WithAddedLogger_ShouldSupportLoggingSetupsAndVerifications()
	{
		int calls = 0;
		IChocolateDispenser mock = IChocolateDispenser.CreateMock().Implementing<ILogger>();
		mock.Mock.As<ILogger>().Setup.LogError().Do(() => calls++);

		((ILogger)mock).LogError("failed");

		await That(calls).IsEqualTo(1);
		await That(mock.Mock.As<ILogger>().Verify.LogError().WithMessage("failed")).Once();
	}

	[Test]
	public async Task CombinationMock_WithClassRootAndAddedLogger_ShouldSupportLoggingVerifications()
	{
		LoggerClassTests.RecordingService mock = LoggerClassTests.RecordingService.CreateMock().Implementing<ILogger>();

		((ILogger)mock).LogError("failed");

		await That(mock.Mock.As<ILogger>().Verify.LogError().WithMessage("failed")).Once();
	}

	[Test]
	public async Task CombinationMock_WithAddedLogger_ThrowingWhenNotSetup_ShouldAnswerIsEnabled()
	{
		IChocolateDispenser mock = IChocolateDispenser.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup())
			.Implementing<ILogger>();

		bool result = ((ILogger)mock).IsEnabled(LogLevel.Error);

		await That(result).IsTrue();
	}

	[Test]
	public async Task Setup_WhenRefinedWhileLoggingOnAnotherThread_ShouldNotThrow()
	{
		ILogger logger = ILogger.CreateMock();
		ILogSetup setup = logger.Mock.Setup.LogError();
		using CancellationTokenSource cts = new();
		int logCalls = 0;
		Task logging = Task.Run(() =>
		{
			while (!cts.IsCancellationRequested)
			{
				logger.LogError("failed");
				Interlocked.Increment(ref logCalls);
			}
		});
		while (Volatile.Read(ref logCalls) == 0 && !logging.IsCompleted)
		{
			Thread.Yield();
		}

		int logCallsBeforeRefining = Volatile.Read(ref logCalls);
		int refinements = 0;
		while ((refinements < 1000 || Volatile.Read(ref logCalls) < logCallsBeforeRefining + 100) &&
		       !logging.IsCompleted)
		{
			setup.WithMessage(It.IsAny<string>());
			refinements++;
		}

		cts.Cancel();
		await That(() => logging).DoesNotThrow()
			.Because("the code under test can log while the test still refines the setup");
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
	public async Task IsEnabled_WithSetupWithoutReturnValue_ShouldReturnTrue()
	{
		ILogger logger = ILogger.CreateMock();
		int calls = 0;
		logger.Mock.Setup.IsEnabled(It.IsAny<LogLevel>()).Do(() => calls++);

		bool result = logger.IsEnabled(LogLevel.Debug);

		await That(result).IsTrue().Because("a setup without a return value does not disable logging");
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Setup_LogError_WithMessageMatcher_ShouldMatchFormattedMessageAndLevel()
	{
		List<string> messages = [];
		ILogger<LoggerTests> logger = ILogger<LoggerTests>.CreateMock();
		logger.Mock.Setup.LogError().WithMessage(It.Matches("*failed*"))
			.Do((_, _, message, _) => messages.Add(message));

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
		logger.Mock.Setup.LogInformation().WithMessage("custom state").Do(() => calls++);

		logger.Log(LogLevel.Information, new EventId(1), 42, null, (_, _) => "custom state");

		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Setup_WithException_ShouldOnlyMatchMatchingException()
	{
		InvalidOperationException exception = new();
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.Log().WithException(It.Is<Exception?>(exception)).Throws<TimeoutException>();

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
		logger.Mock.Setup.LogError().Do(() => calls.Add("first"));
		logger.Mock.Setup.LogError().Do(() => calls.Add("second"));

		logger.LogError("failed");

		await That(calls).IsEqualTo(["second",]);
	}

	[Test]
	public async Task Setup_InScenario_ShouldOnlyApplyInScenario()
	{
		int calls = 0;
		ILogger logger = ILogger.CreateMock();
		logger.Mock.InScenario("broken").Setup.LogError().Do(() => calls++);

		logger.LogError("before transition");
		logger.Mock.TransitionTo("broken");
		logger.LogError("after transition");

		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Setup_InScenario_WithTypedAndLoggingSetups_ShouldUseLatest()
	{
		List<string> calls = [];
		ILogger logger = ILogger.CreateMock();
		logger.Mock.InScenario("broken").Setup.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<int>(),
			It.IsAny<Exception?>(), It.IsAny<Func<int, Exception?, string>>()).Do(() => calls.Add("typed"));
		logger.Mock.InScenario("broken").Setup.LogError().Do(() => calls.Add("logging"));
		logger.Mock.TransitionTo("broken");

		logger.Log(LogLevel.Error, new EventId(1), 42, null, (_, _) => "failed");

		await That(calls).IsEqualTo(["logging",]);
	}

	[Test]
	public async Task Setup_InScenario_WithLoggingAndTypedSetups_ShouldUseLatest()
	{
		List<string> calls = [];
		ILogger logger = ILogger.CreateMock();
		logger.Mock.InScenario("broken").Setup.LogError().Do(() => calls.Add("logging"));
		logger.Mock.InScenario("broken").Setup.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<int>(),
			It.IsAny<Exception?>(), It.IsAny<Func<int, Exception?, string>>()).Do(() => calls.Add("typed"));
		logger.Mock.TransitionTo("broken");

		logger.Log(LogLevel.Error, new EventId(1), 42, null, (_, _) => "failed");

		await That(calls).IsEqualTo(["typed",]);
	}

	[Test]
	public async Task Setup_WithMessageTemplate_ShouldMatchTemplateAndProperties()
	{
		List<string> messages = [];
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.LogWarning()
			.WithMessage(It.MatchesTemplate("Retry {Count} of {Name}").WithProperty("Count", 2))
			.Do((_, _, message, _) => messages.Add(message));

		logger.LogWarning("Retry {Count} of {Name}", 1, "order");
		logger.LogWarning("Retry {Count} of {Name}", 2, "order");
		logger.LogWarning("Retry {Count} for {Name}", 2, "order");

		await That(messages).IsEqualTo(["Retry 2 of order",]);
	}

	[Test]
	public async Task ThrowingWhenNotSetup_IsEnabledWithoutSetup_ShouldReturnTrue()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		bool result = logger.IsEnabled(LogLevel.Debug);

		await That(result).IsTrue();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_LoggerMessage_WithLoggingSetup_ShouldNotThrow()
	{
		Action<ILogger, int, Exception?> logOrderFailed =
			LoggerMessage.Define<int>(LogLevel.Error, new EventId(1), "Order {OrderId} failed");
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		logger.Mock.Setup.LogError();

		void Act() => logOrderFailed(logger, 42, null);

		await That(Act).DoesNotThrow().Because("the generated logging code checks IsEnabled before it calls Log");
	}

	[Test]
	public async Task ThrowingWhenNotSetup_LoggerMessage_WithoutSetup_ShouldThrow()
	{
		Action<ILogger, int, Exception?> logOrderFailed =
			LoggerMessage.Define<int>(LogLevel.Error, new EventId(1), "Order {OrderId} failed");
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act() => logOrderFailed(logger, 42, null);

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage("The method 'global::Microsoft.Extensions.Logging.ILogger.Log<TState>(LogLevel, EventId, TState, Exception?, Func<TState, Exception?, string>)' was invoked without prior setup.");
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithoutSetup_ShouldThrow()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act() => logger.LogError("failed");

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_BeginScope_ShouldNotThrow()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		logger.Mock.Setup.LogError();

		void Act()
		{
			using (logger.BeginScope("Order {OrderId}", 42))
			{
				logger.LogError("failed");
			}
		}

		await That(Act).DoesNotThrow()
			.Because("logging code opens scopes around its log calls");
	}

	[Test]
	public async Task Wrapping_ShouldForwardTheLogCallsAndApplyTheSetups()
	{
		RecordingLogger wrapped = new();
		int calls = 0;
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup()).Wrapping(wrapped);
		logger.Mock.Setup.LogError().Do(() => calls++);

		logger.LogError("failed");
		logger.LogWarning("ignored");

		await That(wrapped.Messages).IsEqualTo(["failed", "ignored",]);
		await That(calls).IsEqualTo(1);
		await That(logger.Mock.Verify.Log()).Twice();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithLoggingSetup_ShouldNotThrow()
	{
		ILogger logger = ILogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		logger.Mock.Setup.LogError();

		void Act() => logger.LogError("failed");

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Verify_Level_ShouldCountTheCallsOnTheLevel()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning("Retry {Count}", 1);
		logger.LogWarning("Retry {Count}", 2);
		logger.LogError("Retry {Count}", 3);

		await That(logger.Mock.Verify.LogWarning()).Twice();
		await That(logger.Mock.Verify.Log(LogLevel.Error)).Once();
		await That(logger.Mock.Verify.LogCritical()).Never();
		await That(logger.Mock.Verify.Log()).Exactly(3);
	}

	[Test]
	public async Task Verify_OrHigher_ShouldIncludeHigherLevels()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogInformation("one");
		logger.LogWarning("two");
		logger.LogCritical("three");

		await That(logger.Mock.Verify.LogWarning().OrHigher()).Twice();
		await That(logger.Mock.Verify.Log(LogLevel.Information).OrHigher()).Exactly(3);
	}

	[Test]
	public async Task Verify_WithMessage_ShouldOnlyMatchTheExactMessage()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning("Retry {Count}", 1);
		logger.LogWarning("Retry {Count} again", 1);

		await That(logger.Mock.Verify.LogWarning().WithMessage("Retry 1")).Once();
		await That(logger.Mock.Verify.LogWarning().WithMessage("retry 1")).Never();
		await That(logger.Mock.Verify.LogWarning().WithMessage(It.Matches("Retry 1*"))).Twice();
	}

	[Test]
	public async Task Verify_WithMessageTemplate_ShouldMatchTheTemplate()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogInformation("Ordered {Amount} {Type} chocolates", 3, "Dark");
		logger.LogInformation("Ordered 3 Dark chocolates");

		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates"))).Once();
		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} chocolates"))).Never();
	}

	[Test]
	public async Task Verify_WithMessageTemplate_WithProperty_ShouldMatchTheValue()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogInformation("Ordered {Amount} {Type} chocolates", 3, "Dark");
		logger.LogInformation("Ordered {Amount} {Type} chocolates", 5, "White");

		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates").WithProperty("Type", "Dark"))).Once();
		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates")
				.WithProperty("Amount", It.Satisfies<int>(amount => amount > 2))
				.WithProperty("Type", It.IsAny<string>()))).Twice();
		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates").WithProperty("Amount", "3"))).Never()
			.Because("the property is an int, not a string");
		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates").WithProperty("Size", It.IsAny<int>()))).Never()
			.Because("the template has no property Size");
	}

	[Test]
	public async Task Verify_WithMessageTemplate_WithNullProperty_ShouldMatchNull()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogInformation("Ordered {Type} chocolates", (string?)null);

		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Type} chocolates").WithProperty("Type", null))).Once();
		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Type} chocolates").WithProperty("Type", It.IsNull<string>()))).Once();
	}

	[Test]
	public async Task Verify_WithMessageTemplate_WhenStateIsNotStructured_ShouldNotMatch()
	{
		ILogger logger = ILogger.CreateMock();

		logger.Log(LogLevel.Information, new EventId(1), 42, null, (_, _) => "Ordered 42 chocolates");

		await That(logger.Mock.Verify.LogInformation()
			.WithMessage(It.MatchesTemplate("Ordered {Amount} chocolates"))).Never();
	}

	[Test]
	public async Task Verify_WithException_ShouldMatchException()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogError(new InvalidOperationException("boom"), "Failed");

		await That(logger.Mock.Verify.LogError().WithException<InvalidOperationException>()).Once();
		await That(logger.Mock.Verify.LogError().WithException<Exception>()).Once();
		await That(logger.Mock.Verify.LogError().WithException<ArgumentException>()).Never();
		await That(logger.Mock.Verify.LogError()
			.WithException(It.Satisfies<Exception?>(e => e?.Message == "boom"))).Once();
		await That(logger.Mock.Verify.LogError().WithException(It.IsNull<Exception?>())).Never();
	}

	[Test]
	public async Task Verify_WithEventId_ShouldMatchTheId()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning(new EventId(7, "Retry"), "Retry");
		logger.LogWarning(new EventId(8), "Retry");

		await That(logger.Mock.Verify.LogWarning().WithEventId(7)).Once();
		await That(logger.Mock.Verify.LogWarning().WithEventId(new EventId(8))).Once();
		await That(logger.Mock.Verify.LogWarning()
			.WithEventId(It.Satisfies<EventId>(eventId => eventId.Name == "Retry"))).Once();
	}

	[Test]
	public async Task Verify_WithCategory_OnGenericLogger_ShouldMatchTheCategoryOfTheType()
	{
		ILogger<LoggerTests> logger = ILogger<LoggerTests>.CreateMock();

		logger.LogWarning("Retry");

		await That(logger.Mock.Verify.LogWarning().WithCategory<LoggerTests>()).Once();
		await That(logger.Mock.Verify.LogWarning().WithCategory("Mockolate.Tests.Logging.LoggerTests")).Once();
		await That(logger.Mock.Verify.LogWarning().WithCategory<LoggerFactoryTests>()).Never();
	}

	[Test]
	public async Task Verify_WithCategory_OnLoggerWithoutCategory_ShouldNotMatch()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning("Retry");

		await That(logger.Mock.Verify.LogWarning().WithCategory<LoggerTests>()).Never();
	}

	[Test]
	public async Task Verify_WithSeveralFilters_ShouldRequireAll()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogError(new EventId(1), new TimeoutException(), "Order {OrderId} failed", 42);
		logger.LogError(new EventId(2), new TimeoutException(), "Order {OrderId} failed", 43);

		await That(logger.Mock.Verify.LogError()
			.WithEventId(1)
			.WithException<TimeoutException>()
			.WithMessage(It.MatchesTemplate("Order {OrderId} failed").WithProperty("OrderId", 42))).Once();
	}

	[Test]
	public async Task Verify_Then_ShouldCheckTheOrder()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogInformation("first");
		logger.LogWarning("second");

		await That(logger.Mock.Verify.LogInformation()).Then(m => m.LogWarning());
	}

	[Test]
	public async Task Verify_Then_WhenInWrongOrder_ShouldFail()
	{
		ILogger logger = ILogger.CreateMock();

		logger.LogWarning("first");
		logger.LogInformation("second");

		void Act() => logger.Mock.Verify.LogInformation().Then(m => m.LogWarning());

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*then invoked method LogWarning() in order*").AsWildcard();
	}

	[Test]
	public async Task Verify_Then_ShouldOrderLogCallsAndOtherCalls()
	{
		ILogger logger = ILogger.CreateMock();

		_ = logger.IsEnabled(LogLevel.Error);
		logger.LogError("failed");
		_ = logger.IsEnabled(LogLevel.Warning);

		await That(logger.Mock.Verify.IsEnabled(It.Is(LogLevel.Error))).Then(m => m.LogError());
		await That(logger.Mock.Verify.LogError()).Then(m => m.IsEnabled(It.Is(LogLevel.Warning)));
	}

	[Test]
	public async Task Verify_Within_ShouldWaitForLogCallsOnAnotherThread()
	{
		ILogger logger = ILogger.CreateMock();

		_ = Task.Run(async () =>
		{
			await Task.Delay(50.Milliseconds());
			logger.LogError("failed");
		});

		logger.Mock.Verify.LogError().WithMessage("failed").Within(30.Seconds()).Once();
		await That(logger.Mock.Verify.LogError()).Once();
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_ShouldIgnoreTheCallsOfIsEnabled()
	{
		ILogger logger = ILogger.CreateMock();
		Action<ILogger, int, Exception?> logOrderFailed =
			LoggerMessage.Define<int>(LogLevel.Error, new EventId(1), "Order {OrderId} failed");

		logOrderFailed(logger, 42, null);
		bool before = logger.Mock.VerifyThatAllInteractionsAreVerified();
		await That(logger.Mock.Verify.LogError()).Once();
		bool after = logger.Mock.VerifyThatAllInteractionsAreVerified();

		await That(before).IsFalse();
		await That(after).IsTrue()
			.Because("logging code checks IsEnabled before it logs, so only the log calls need a verification");
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_ShouldIgnoreTheCallsOfBeginScope()
	{
		ILogger logger = ILogger.CreateMock();

		using (logger.BeginScope("Order {OrderId}", 42))
		{
			logger.LogError("failed");
		}

		await That(logger.Mock.Verify.LogError()).Once();
		await That(logger.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue()
			.Because("logging code opens scopes around its log calls, so only the log calls need a verification");
	}

	[Test]
	public async Task Verify_WithCategory_OnInterfaceDerivedFromGenericLogger_ShouldUseTheCategoryOfTheTypeArgument()
	{
		ITypedLogger logger = ITypedLogger.CreateMock();

		logger.LogError("failed");

		await That(logger.Mock.Verify.LogError().WithCategory<LoggerTests>()).Once();
	}

	[Test]
	public async Task Verify_WhenNotMatching_ShouldDescribeTheFilters()
	{
		ILogger logger = ILogger.CreateMock();

		void Act() => logger.Mock.Verify.LogError().OrHigher().WithMessage("failed")
			.WithException<TimeoutException>().WithEventId(3).WithCategory("Orders").Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method LogError().OrHigher().WithMessage(\"failed\").WithException<TimeoutException>().WithEventId(3).WithCategory(\"Orders\") exactly once, but it never did.");
	}

	[Test]
	public async Task Verify_WhenNotMatchingTemplate_ShouldDescribeTheTemplate()
	{
		ILogger logger = ILogger.CreateMock();

		void Act() => logger.Mock.Verify.Log(LogLevel.Warning)
			.WithMessage(It.MatchesTemplate("Retry {Count}").WithProperty("Count", 2).WithProperty("Name", "order"))
			.Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Log(Warning).WithMessage(It.MatchesTemplate(\"Retry {Count}\").WithProperty(\"Count\", 2).WithProperty(\"Name\", \"order\")) exactly once, but it never did.");
	}

	[Test]
	public async Task VerifySetup_ShouldCountCallsMatchingTheLoggingSetup()
	{
		ILogger logger = ILogger.CreateMock();
		IMethodSetup setup = logger.Mock.Setup.LogError();

		logger.LogError("one");
		logger.LogWarning("two");
		logger.Log(LogLevel.Error, new EventId(3), 3, null, (_, _) => "three");

		await That(logger.Mock.VerifySetup(setup)).Twice()
			.Because("the setup matches the calls of every state type");
	}

	[Test]
	public async Task VerifyThatAllSetupsAreUsed_ShouldConsiderCallsMatchingTheLoggingSetup()
	{
		ILogger logger = ILogger.CreateMock();
		logger.Mock.Setup.LogError().WithMessage("used");
		logger.Mock.Setup.LogError().WithMessage("unused");

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

		await That(monitor.Verify.LogError()).Once();
	}

	public interface ITypedLogger : ILogger<LoggerTests>
	{
	}

	private sealed class RecordingLogger : ILogger
	{
		public List<string> Messages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel)
			=> true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
			=> Messages.Add(formatter(state, exception));
	}
}
