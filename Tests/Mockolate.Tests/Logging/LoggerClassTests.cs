#pragma warning disable CA1873 // Avoid potentially expensive logging
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;

namespace Mockolate.Tests.Logging;

public sealed class LoggerClassTests
{
	[Test]
	public async Task AbstractLogger_IsEnabled_WithoutSetup_ShouldReturnTrue()
	{
		AbstractLogger logger = AbstractLogger.CreateMock();

		bool result = logger.IsEnabled(LogLevel.Trace);

		await That(result).IsTrue();
	}

	[Test]
	public async Task AbstractLogger_ShouldSupportLoggingSetupsAndVerifications()
	{
		List<string> messages = [];
		AbstractLogger logger = AbstractLogger.CreateMock();
		logger.Mock.Setup.LogError().Do((_, _, message, _) => messages.Add(message));

		logger.LogError("Order {OrderId} failed", 42);
		logger.LogWarning("ignored");

		await That(messages).IsEqualTo(["Order 42 failed",]);
		await That(logger.Mock.Verify.LogError().WithMessage("Order 42 failed")).Once();
		await That(logger.Mock.Verify.Log()).Twice();
	}

	[Test]
	public async Task AbstractLogger_ThrowingWhenNotSetup_ShouldOnlyThrowForLogCalls()
	{
		AbstractLogger logger = AbstractLogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void ActWithScope()
		{
			using (logger.BeginScope("scope"))
			{
				_ = logger.IsEnabled(LogLevel.Error);
			}
		}

		void ActWithLog() => logger.LogError("failed");

		await That(ActWithScope).DoesNotThrow();
		await That(ActWithLog).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task AbstractLogger_VerifyThatAllInteractionsAreVerified_ShouldIgnoreIsEnabledAndBeginScope()
	{
		AbstractLogger logger = AbstractLogger.CreateMock();

		using (logger.BeginScope("scope"))
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError("failed");
			}
		}

		bool before = logger.Mock.VerifyThatAllInteractionsAreVerified();
		await That(logger.Mock.Verify.LogError()).Once();
		bool after = logger.Mock.VerifyThatAllInteractionsAreVerified();

		await That(before).IsFalse();
		await That(after).IsTrue();
	}

	[Test]
	public async Task GenericLogger_ShouldUseTheCategoryOfTheTypeArgument()
	{
		AbstractCategoryLogger logger = AbstractCategoryLogger.CreateMock();

		logger.LogError("failed");

		await That(logger.Mock.Verify.LogError().WithCategory<LoggerClassTests>()).Once();
	}

	[Test]
	public async Task VirtualLogger_ShouldCallTheBaseImplementation()
	{
		int calls = 0;
		RecordingLogger logger = RecordingLogger.CreateMock();
		logger.Mock.Setup.LogError().Do(() => calls++);

		logger.LogError("failed");

		await That(logger.Messages).IsEqualTo(["failed",]);
		await That(calls).IsEqualTo(1);
		await That(logger.Mock.Verify.LogError()).Once();
	}

	[Test]
	public async Task VirtualLogger_SkippingBaseClass_ShouldNotCallTheBaseImplementation()
	{
		RecordingLogger logger = RecordingLogger.CreateMock(MockBehavior.Default.SkippingBaseClass());

		logger.LogError("failed");

		await That(logger.Messages).IsEmpty();
		await That(logger.Mock.Verify.LogError()).Once();
	}

	[Test]
	public async Task VirtualLogger_SetupSkippingBaseClass_ShouldNotCallTheBaseImplementation()
	{
		RecordingLogger logger = RecordingLogger.CreateMock();
		logger.Mock.Setup.LogError().SkippingBaseClass();

		logger.LogError("failed");
		logger.LogWarning("warning");

		await That(logger.Messages).IsEqualTo(["warning",]);
	}

	[Test]
	public async Task VirtualLogger_ThrowingWhenNotSetup_ShouldNotThrow()
	{
		RecordingLogger logger = RecordingLogger.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act() => logger.LogError("failed");

		await That(Act).DoesNotThrow()
			.Because("the base implementation handles the call");
	}

	[Test]
	public async Task VirtualLogger_TypedSetupAndVerification_ShouldMatchTheLogCalls()
	{
		int calls = 0;
		RecordingLogger logger = RecordingLogger.CreateMock();
		logger.Mock.Setup.Log<string>(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.Is("state"),
				It.IsAny<Exception?>(), It.IsAny<Func<string, Exception?, string>>())
			.Do(() => calls++);

		logger.Log(LogLevel.Error, new EventId(1), "state", null, (state, _) => state);

		await That(calls).IsEqualTo(1);
		await That(logger.Mock.Verify.Log<string>(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.Is("state"),
			It.IsAny<Exception?>(), It.IsAny<Func<string, Exception?, string>>())).Once();
	}

	[Test]
	public async Task LoggerWithMembersFromNonLoggerBase_ShouldSupportLoggingSetupsAndVerifications()
	{
		LoggerWithMembersFromNonLoggerBase logger =
			LoggerWithMembersFromNonLoggerBase.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		logger.Mock.Setup.LogError();

		bool isEnabled = logger.IsEnabled(LogLevel.Error);
		logger.LogError("failed");

		await That(isEnabled).IsTrue()
			.Because("the abstract IsEnabled of the base class implements ILogger.IsEnabled for the mocked class");
		await That(logger.Mock.Verify.LogError().WithMessage("failed")).Once();
	}

	[Test]
	public async Task VirtualLogger_IsEnabled_ShouldReturnTheBaseValue()
	{
		RecordingLogger logger = RecordingLogger.CreateMock();

		bool result = logger.IsEnabled(LogLevel.Trace);

		await That(result).IsFalse();
	}

	public abstract class AbstractLogger : ILogger
	{
		public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;

		public abstract bool IsEnabled(LogLevel logLevel);

		public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter);
	}

	public abstract class AbstractCategoryLogger : ILogger<LoggerClassTests>
	{
		public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;

		public abstract bool IsEnabled(LogLevel logLevel);

		public abstract void Log<T>(LogLevel logLevel, EventId eventId, T state, Exception? exception,
			Func<T, Exception?, string> formatter);
	}

	public abstract class NonLoggerBase
	{
		public abstract bool IsEnabled(LogLevel logLevel);

		public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
		}
	}

	public abstract class LoggerWithMembersFromNonLoggerBase : NonLoggerBase, ILogger
	{
		public abstract IDisposable? BeginScope<TState>(TState state) where TState : notnull;
	}

	public class RecordingService
	{
		public virtual void Run()
		{
		}
	}

	public class RecordingLogger : ILogger
	{
		public List<string> Messages { get; } = [];

		public virtual IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public virtual bool IsEnabled(LogLevel logLevel)
			=> logLevel >= LogLevel.Information;

		public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
			=> Messages.Add(formatter(state, exception));
	}
}
