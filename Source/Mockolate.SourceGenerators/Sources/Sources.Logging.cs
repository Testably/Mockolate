using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	private const string LoggerFullName = "global::Microsoft.Extensions.Logging.ILogger";
	private const string LoggerFactoryFullName = "global::Microsoft.Extensions.Logging.ILoggerFactory";

	internal static bool IsLoggerLog(Method method)
		=> method is { ContainingType: LoggerFullName, Name: "Log<TState>", Parameters.Count: 5, };

	internal static bool IsLoggerIsEnabled(Method method)
		=> method is { ContainingType: LoggerFullName, Name: "IsEnabled", Parameters.Count: 1, };

	internal static bool IsLoggerFactoryCreateLogger(Method method)
		=> method is { ContainingType: LoggerFactoryFullName, Name: "CreateLogger", Parameters.Count: 1, };

	internal static bool IsLogger(Class @class)
		=> @class.IsInterface && @class.AllMethods().Any(IsLoggerLog);

	internal static bool IsLoggerFactory(Class @class)
		=> @class.IsInterface && @class.AllMethods().Any(IsLoggerFactoryCreateLogger);

	/// <summary>
	///     The <c>FakeLogger</c> that a logger mock forwards to by default, or <see langword="null" /> when the compilation
	///     has no <c>FakeLogger</c> or the mock is no logger.
	/// </summary>
	private static string? GetDefaultFakeLogger(Class @class, bool hasFakeLogger)
	{
		if (!hasFakeLogger || !IsLogger(@class))
		{
			return null;
		}

		const string genericLoggerPrefix = LoggerFullName + "<";
		if (@class.ClassFullName.StartsWith(genericLoggerPrefix, StringComparison.Ordinal) &&
		    @class.ClassFullName.EndsWith(">", StringComparison.Ordinal))
		{
			string categoryType = @class.ClassFullName.Substring(genericLoggerPrefix.Length,
				@class.ClassFullName.Length - genericLoggerPrefix.Length - 1);
			return $"new global::Microsoft.Extensions.Logging.Testing.FakeLogger<{categoryType}>()";
		}

		return "new global::Microsoft.Extensions.Logging.Testing.FakeLogger()";
	}

	private static void AppendLoggerAccessDeclaration(StringBuilder sb, Class @class)
	{
		if (IsLogger(@class) || IsLoggerFactory(@class))
		{
			sb.Append(", global::Mockolate.IMockolateLoggerAccess");
		}
	}

	private static void AppendLoggerAccessImplementation(StringBuilder sb, Class @class, string mockRegistryName,
		string scenario, MemberIdTable memberIds, string memberIdPrefix)
	{
		bool isLogger = IsLogger(@class);
		if (!isLogger && !IsLoggerFactory(@class))
		{
			return;
		}

		string logMemberId = isLogger
			? memberIdPrefix + memberIds.GetMethodIdentifier(@class.AllMethods().First(IsLoggerLog))
			: "-1";
		sb.AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Internals.MockRegistry global::Mockolate.IMockolateLoggerAccess.MockolateRegistry => this.")
			.Append(mockRegistryName).Append(';').AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.IMockolateLoggerAccess.MockolateScenario => ").Append(scenario).Append(';')
			.AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tint global::Mockolate.IMockolateLoggerAccess.MockolateLogMemberId => ").Append(logMemberId)
			.Append(';').AppendLine();
	}

	private static void AppendLoggerMockExtensions(StringBuilder sb, Class @class, string name, bool hasFakeLogger)
	{
		bool isLogger = IsLogger(@class);
		bool isLoggerFactory = IsLoggerFactory(@class);
		if (!isLoggerFactory && !(isLogger && hasFakeLogger))
		{
			return;
		}

		sb.AppendLine();
		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(global::Mockolate.Mock.IMockFor").Append(name).Append(" mock)").AppendLine();
		sb.Append("\t{").AppendLine();
		if (isLoggerFactory)
		{
			sb.AppendXmlSummary("Gets the logger mock that <c>CreateLogger</c> returns for the <paramref name=\"categoryName\" /> when it has no setup.");
			sb.AppendXmlRemarks("Use it to set up or verify the logger before the code under test creates it.");
			sb.AppendXmlParam("categoryName", "The category name of the logger.");
			sb.Append("\t\tpublic global::Microsoft.Extensions.Logging.ILogger GetLogger(string categoryName)").AppendLine();
			sb.Append("\t\t\t=> global::Mockolate.MockolateLogging.CreateLogger(((global::Mockolate.IMock)mock).MockRegistry, categoryName);").AppendLine();
			sb.AppendLine();
			sb.AppendXmlSummary("Gets the logger mock that <c>CreateLogger</c> returns for the category of <typeparamref name=\"TCategoryName\" /> when it has no setup.");
			sb.AppendXmlRemarks("The category name is the one that <c>ILogger&lt;TCategoryName&gt;</c> uses, so this is the logger that a <c>Logger&lt;TCategoryName&gt;</c> created by the factory writes to.");
			sb.AppendXmlTypeParam("TCategoryName", "The type whose name is the category name of the logger.");
			sb.Append("\t\tpublic global::Microsoft.Extensions.Logging.ILogger GetLogger<TCategoryName>()").AppendLine();
			sb.Append("\t\t\t=> global::Mockolate.MockolateLogging.CreateLogger(((global::Mockolate.IMock)mock).MockRegistry, global::Mockolate.MockolateLogging.GetCategoryName<TCategoryName>());").AppendLine();
		}

		if (hasFakeLogger)
		{
			if (isLoggerFactory)
			{
				sb.AppendLine();
			}

			sb.AppendXmlSummary(isLoggerFactory
				? "Gets the <c>FakeLogCollector</c> that collects the log records of all loggers the factory created."
				: "Gets the <c>FakeLogCollector</c> of the <c>FakeLogger</c> that the logger mock forwards to.");
			sb.AppendXmlRemarks(isLoggerFactory
				? "The loggers do not forward to the collector when the mock was created with <c>ThrowingWhenNotSetup()</c>."
				: "Throws a <see cref=\"global::Mockolate.Exceptions.MockException\" /> when the mock does not forward to a <c>FakeLogger</c>, because it was created with <c>ThrowingWhenNotSetup()</c> or wraps another logger.");
			sb.Append("\t\tpublic global::Microsoft.Extensions.Logging.Testing.FakeLogCollector LogCollector").AppendLine();
			sb.Append("\t\t\t=> global::Mockolate.MockolateLogging.")
				.Append(isLoggerFactory ? "GetFactoryLogCollector" : "GetLogCollector")
				.Append("(((global::Mockolate.IMock)mock).MockRegistry);").AppendLine();
		}

		sb.Append("\t}").AppendLine();
	}

	public static string Logging(bool hasLoggerFactory, bool hasFakeLogger)
	{
		StringBuilder sb = InitializeBuilder();
		sb.Append("""
		          #nullable enable annotations

		          namespace Mockolate.Interactions
		          {
		          	/// <summary>
		          	///     A recorded call of <c>ILogger.Log</c> with its formatted message.
		          	/// </summary>
		          	internal interface ILogInvocation : global::Mockolate.Interactions.IMethodInteraction
		          	{
		          		/// <summary>
		          		///     The level of the log entry.
		          		/// </summary>
		          		global::Microsoft.Extensions.Logging.LogLevel LogLevel { get; }

		          		/// <summary>
		          		///     The event id of the log entry.
		          		/// </summary>
		          		global::Microsoft.Extensions.Logging.EventId EventId { get; }

		          		/// <summary>
		          		///     The message of the log entry, formatted with the formatter of the call.
		          		/// </summary>
		          		string Message { get; }

		          		/// <summary>
		          		///     The exception of the log entry.
		          		/// </summary>
		          		global::System.Exception? Exception { get; }
		          	}

		          	/// <summary>
		          	///     A recorded call of <c>ILogger.Log</c> with the state type <typeparamref name="TState" />.
		          	/// </summary>
		          	/// <remarks>
		          	///     The message is formatted on first access, so a call that no setup or verification inspects is never formatted.
		          	/// </remarks>
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal sealed class LogInvocation<TState> : global::Mockolate.Interactions.MethodInvocation<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>, ILogInvocation
		          	{
		          		private string? _message;

		          		/// <inheritdoc cref="LogInvocation{TState}" />
		          		public LogInvocation(string name, global::Microsoft.Extensions.Logging.LogLevel logLevel, global::Microsoft.Extensions.Logging.EventId eventId, TState state, global::System.Exception? exception, global::System.Func<TState, global::System.Exception?, string> formatter)
		          			: base(name, logLevel, eventId, state, exception, formatter)
		          		{
		          		}

		          		/// <inheritdoc />
		          		global::Microsoft.Extensions.Logging.LogLevel ILogInvocation.LogLevel => Parameter1;

		          		/// <inheritdoc />
		          		global::Microsoft.Extensions.Logging.EventId ILogInvocation.EventId => Parameter2;

		          		/// <inheritdoc />
		          		public string Message => _message ??= Parameter5(Parameter3, Parameter4);

		          		/// <inheritdoc />
		          		global::System.Exception? ILogInvocation.Exception => Parameter4;

		          		/// <inheritdoc cref="object.ToString()" />
		          		public override string ToString()
		          			=> $"invoke method Log({Parameter1}, {Parameter2}, {Message}, {Parameter4?.ToString() ?? "null"})";
		          	}
		          }

		          namespace Mockolate
		          {
		          	/// <summary>
		          	///     Gives the logging setups and verifications access to the registry of a logger or logger factory mock.
		          	/// </summary>
		          	internal interface IMockolateLoggerAccess
		          	{
		          		/// <summary>
		          		///     The registry of the mock.
		          		/// </summary>
		          		global::Mockolate.Internals.MockRegistry MockolateRegistry { get; }

		          		/// <summary>
		          		///     The scenario that setups are registered for.
		          		/// </summary>
		          		string MockolateScenario { get; }

		          		/// <summary>
		          		///     The member id of <c>ILogger.Log</c>, or <c>-1</c> for a logger factory.
		          		/// </summary>
		          		int MockolateLogMemberId { get; }
		          	}

		          	/// <summary>
		          	///     Setups and verifications of <c>ILogger.Log</c> by the formatted message, whatever the state type of the call is.
		          	/// </summary>
		          	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal static class MockolateLogging
		          	{
		          		private const string LogName = "global::Microsoft.Extensions.Logging.ILogger.Log";
		          		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Mockolate.Internals.MockRegistry, global::Mockolate.Internals.MockRegistry> s_factoryRegistries = new();

		          """);
		AppendLogLevelExtensions(sb, "global::Mockolate.Setup.IMockSetup<global::Microsoft.Extensions.Logging.ILogger> setup", "SetupLog(setup, ",
			"global::Mockolate.Setup.IVoidMethodSetupWithCallback<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, string, global::System.Exception?>",
			"Sets up calls of <c>ILogger.Log</c>", "A setup handle; the callbacks receive the level, the event id, the formatted message and the exception.");
		AppendLogLevelExtensions(sb, "global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILogger> verify", "VerifyLog(verify, ",
			"global::Mockolate.Verify.VerificationResult<global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILogger>>",
			"Verifies calls of <c>ILogger.Log</c>", "A verification result; terminate it with a count assertion.");
		if (hasLoggerFactory)
		{
			AppendLogLevelExtensions(sb, "global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILoggerFactory> verify", "VerifyLog(verify, ",
				"global::Mockolate.Verify.VerificationResult<global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILoggerFactory>>",
				"Verifies calls of <c>ILogger.Log</c> on all loggers that the factory created", "A verification result; terminate it with a count assertion.");
		}

		sb.Append("""
		          		/// <summary>
		          		///     Dispatches a call of <c>ILogger.Log</c>: setups of the state type <typeparamref name="TState" /> and setups of the
		          		///     formatted message both apply, the latest matching one wins.
		          		/// </summary>
		          		public static void DispatchLog<TState>(global::Mockolate.Internals.MockRegistry registry, int memberId, global::Microsoft.Extensions.Logging.LogLevel logLevel, global::Microsoft.Extensions.Logging.EventId eventId, TState state, global::System.Exception? exception, global::System.Func<TState, global::System.Exception?, string> formatter)
		          		{
		          			string name = $"global::Microsoft.Extensions.Logging.ILogger.Log<{typeof(TState)}>";
		          			global::Mockolate.Interactions.LogInvocation<TState> invocation = new(name, logLevel, eventId, state, exception, formatter);
		          			global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>? methodSetup = null;
		          			LogSetup? logSetup = null;
		          			if (string.IsNullOrEmpty(registry.Scenario))
		          			{
		          				global::Mockolate.Setup.MethodSetup[]? snapshot = registry.GetMethodSetupSnapshot(memberId);
		          				if (snapshot is not null)
		          				{
		          					for (int i = snapshot.Length - 1; i >= 0; i--)
		          					{
		          						if (snapshot[i] is global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>> typedSetup && typedSetup.Name == name && typedSetup.Matches(logLevel, eventId, state, exception, formatter))
		          						{
		          							methodSetup = typedSetup;
		          							break;
		          						}

		          						if (snapshot[i] is LogSetup messageSetup && messageSetup.Matches(logLevel, eventId, invocation.Message, exception))
		          						{
		          							logSetup = messageSetup;
		          							break;
		          						}
		          					}
		          				}

		          				if (methodSetup is null && logSetup is null)
		          				{
		          					methodSetup = FindTypedSetup(registry.GetFallbackMethodSetups<global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>>(memberId, name));
		          					logSetup = methodSetup is null ? FindMessageSetup(registry.GetFallbackMethodSetups<LogSetup>(memberId, LogName)) : null;
		          				}
		          			}
		          			else
		          			{
		          				methodSetup = FindTypedSetup(registry.GetMethodSetups<global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>>(name));
		          				logSetup = methodSetup is null ? FindMessageSetup(registry.GetMethodSetups<LogSetup>(LogName)) : null;
		          			}

		          			registry.RegisterInteraction(invocation);
		          			if (s_factoryRegistries.TryGetValue(registry, out global::Mockolate.Internals.MockRegistry? factoryRegistry))
		          			{
		          				factoryRegistry.RegisterInteraction(invocation);
		          			}

		          			bool hasWrappedResult = false;
		          			try
		          			{
		          				if (registry.Wraps is global::Microsoft.Extensions.Logging.ILogger wraps)
		          				{
		          					wraps.Log<TState>(logLevel, eventId, state, exception, formatter);
		          					hasWrappedResult = true;
		          				}
		          			}
		          			finally
		          			{
		          				methodSetup?.TriggerCallbacks(logLevel, eventId, state, exception, formatter);
		          				logSetup?.TriggerCallbacks(logLevel, eventId, invocation.Message, exception);
		          			}

		          			if (methodSetup is null && logSetup is null && !hasWrappedResult && registry.Behavior.ThrowWhenNotSetup)
		          			{
		          				throw new global::Mockolate.Exceptions.MockNotSetupException("The method 'global::Microsoft.Extensions.Logging.ILogger.Log<TState>(LogLevel, EventId, TState, Exception?, Func<TState, Exception?, string>)' was invoked without prior setup.");
		          			}

		          			global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>? FindTypedSetup(global::System.Collections.Generic.IEnumerable<global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>>? setups)
		          			{
		          				if (setups is not null)
		          				{
		          					foreach (global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>> setup in setups)
		          					{
		          						if (setup.Matches(logLevel, eventId, state, exception, formatter))
		          						{
		          							return setup;
		          						}
		          					}
		          				}

		          				return null;
		          			}

		          			LogSetup? FindMessageSetup(global::System.Collections.Generic.IEnumerable<LogSetup>? setups)
		          			{
		          				if (setups is not null)
		          				{
		          					foreach (LogSetup setup in setups)
		          					{
		          						if (setup.Matches(logLevel, eventId, invocation.Message, exception))
		          						{
		          							return setup;
		          						}
		          					}
		          				}

		          				return null;
		          			}
		          		}

		          		private static global::Mockolate.IMockolateLoggerAccess GetAccess(object mock)
		          			=> mock as global::Mockolate.IMockolateLoggerAccess ?? throw new global::Mockolate.Exceptions.MockException($"The logging setups and verifications do not support '{mock}'.");

		          		private static global::Mockolate.Setup.IVoidMethodSetupWithCallback<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, string, global::System.Exception?> SetupLog(object setup, global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.LogLevel> logLevel, global::Mockolate.Parameters.IParameter<string> message, global::Mockolate.Parameters.IParameter<global::System.Exception?> exception)
		          		{
		          			global::Mockolate.IMockolateLoggerAccess access = GetAccess(setup);
		          			LogSetup methodSetup = new(access.MockolateRegistry,
		          				global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.LogLevel>.Wrap(logLevel),
		          				global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.EventId>.Wrap(global::Mockolate.It.IsAny<global::Microsoft.Extensions.Logging.EventId>()),
		          				global::Mockolate.CovariantParameterAdapter<string>.Wrap(message),
		          				global::Mockolate.CovariantParameterAdapter<global::System.Exception?>.Wrap(exception));
		          			access.MockolateRegistry.SetupMethod(access.MockolateLogMemberId, access.MockolateScenario, methodSetup);
		          			return methodSetup;
		          		}

		          		private static global::Mockolate.Verify.VerificationResult<TVerify> VerifyLog<TVerify>(TVerify verify, global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.LogLevel> logLevel, global::Mockolate.Parameters.IParameter<string> message, global::Mockolate.Parameters.IParameter<global::System.Exception?> exception)
		          			where TVerify : class
		          		{
		          			global::Mockolate.Parameters.IParameterMatch<global::Microsoft.Extensions.Logging.LogLevel> logLevelMatch = global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.LogLevel>.Wrap(logLevel);
		          			global::Mockolate.Parameters.IParameterMatch<string> messageMatch = global::Mockolate.CovariantParameterAdapter<string>.Wrap(message);
		          			global::Mockolate.Parameters.IParameterMatch<global::System.Exception?> exceptionMatch = global::Mockolate.CovariantParameterAdapter<global::System.Exception?>.Wrap(exception);
		          			return GetAccess(verify).MockolateRegistry.VerifyMethodInteractions<TVerify, global::Mockolate.Interactions.ILogInvocation>(verify,
		          				invocation => logLevelMatch.Matches(invocation.LogLevel) && messageMatch.Matches(invocation.Message) && exceptionMatch.Matches(invocation.Exception),
		          				() => $"Log({logLevel}, {message}, {exception})");
		          		}

		          """);
		if (hasLoggerFactory)
		{
			sb.Append("""
			          		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Mockolate.Internals.MockRegistry, LoggerFactoryState> s_loggerFactories = new();

			          		/// <summary>
			          		///     Returns the logger mock of the factory with the <paramref name="factoryRegistry" /> for the <paramref name="categoryName" />.
			          		/// </summary>
			          		/// <remarks>
			          		///     The logger records its calls of <c>ILogger.Log</c> on the factory as well, so that the factory can verify them.
			          		/// </remarks>
			          		public static global::Microsoft.Extensions.Logging.ILogger CreateLogger(global::Mockolate.Internals.MockRegistry factoryRegistry, string categoryName)
			          		{
			          			LoggerFactoryState state = s_loggerFactories.GetValue(factoryRegistry, static _ => new LoggerFactoryState());
			          			lock (state.Loggers)
			          			{
			          				if (!state.Loggers.TryGetValue(categoryName, out global::Microsoft.Extensions.Logging.ILogger? logger))
			          				{
			          					logger = global::Microsoft.Extensions.Logging.ILogger.CreateMock(factoryRegistry.Behavior);

			          """);
			if (hasFakeLogger)
			{
				sb.Append("""
				          					if (!factoryRegistry.Behavior.ThrowWhenNotSetup)
				          					{
				          						logger = logger.Wrapping(new global::Microsoft.Extensions.Logging.Testing.FakeLogger(state.Collector, categoryName));
				          					}

				          """);
			}

			sb.Append("""
			          					s_factoryRegistries.Add(((global::Mockolate.IMock)logger).MockRegistry, factoryRegistry);
			          					state.Loggers.Add(categoryName, logger);
			          				}

			          				return logger;
			          			}
			          		}

			          		/// <summary>
			          		///     Returns the category name that <c>Logger&lt;TCategoryName&gt;</c> requests from its factory.
			          		/// </summary>
			          		public static string GetCategoryName<TCategoryName>()
			          		{
			          			CategoryNameCapture capture = new();
			          			_ = new global::Microsoft.Extensions.Logging.Logger<TCategoryName>(capture);
			          			return capture.CategoryName;
			          		}

			          """);
			if (hasFakeLogger)
			{
				sb.Append("""
				          		/// <summary>
				          		///     Returns the collector of all loggers of the factory with the <paramref name="factoryRegistry" />.
				          		/// </summary>
				          		public static global::Microsoft.Extensions.Logging.Testing.FakeLogCollector GetFactoryLogCollector(global::Mockolate.Internals.MockRegistry factoryRegistry)
				          			=> s_loggerFactories.GetValue(factoryRegistry, static _ => new LoggerFactoryState()).Collector;

				          """);
			}

			sb.Append("""
			          		private sealed class LoggerFactoryState
			          		{
			          			public global::System.Collections.Generic.Dictionary<string, global::Microsoft.Extensions.Logging.ILogger> Loggers { get; } = new();

			          """);
			if (hasFakeLogger)
			{
				sb.Append("""
				          			public global::Microsoft.Extensions.Logging.Testing.FakeLogCollector Collector { get; } = new();

				          """);
			}

			sb.Append("""
			          		}

			          		private sealed class CategoryNameCapture : global::Microsoft.Extensions.Logging.ILoggerFactory
			          		{
			          			public string CategoryName { get; private set; } = "";

			          			public global::Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
			          			{
			          				CategoryName = categoryName;
			          				return global::Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
			          			}

			          			public void AddProvider(global::Microsoft.Extensions.Logging.ILoggerProvider provider)
			          			{
			          			}

			          			public void Dispose()
			          			{
			          			}
			          		}

			          """);
		}

		if (hasFakeLogger)
		{
			sb.Append("""
			          		/// <summary>
			          		///     Returns the collector of the <c>FakeLogger</c> that the logger mock with the <paramref name="registry" /> forwards to.
			          		/// </summary>
			          		public static global::Microsoft.Extensions.Logging.Testing.FakeLogCollector GetLogCollector(global::Mockolate.Internals.MockRegistry registry)
			          			=> registry.Wraps is global::Microsoft.Extensions.Logging.Testing.FakeLogger fakeLogger
			          				? fakeLogger.Collector
			          				: throw new global::Mockolate.Exceptions.MockException("The logger mock does not forward to a FakeLogger, because it was created with ThrowingWhenNotSetup() or wraps another logger.");

			          """);
		}

		sb.Append("""
		          		private sealed class LogSetup : global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, string, global::System.Exception?>.WithParameterCollection, global::Mockolate.Internals.IGenericMethodSetup
		          		{
		          			public LogSetup(global::Mockolate.Internals.MockRegistry mockRegistry,
		          				global::Mockolate.Parameters.IParameterMatch<global::Microsoft.Extensions.Logging.LogLevel> logLevel,
		          				global::Mockolate.Parameters.IParameterMatch<global::Microsoft.Extensions.Logging.EventId> eventId,
		          				global::Mockolate.Parameters.IParameterMatch<string> message,
		          				global::Mockolate.Parameters.IParameterMatch<global::System.Exception?> exception)
		          				: base(mockRegistry, LogName, logLevel, eventId, message, exception)
		          			{
		          			}

		          			protected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)
		          				=> interaction is global::Mockolate.Interactions.ILogInvocation log && Matches(log.LogLevel, log.EventId, log.Message, log.Exception);
		          		}
		          	}
		          }

		          #nullable disable annotations

		          """);
		return sb.ToString();
	}

	private static void AppendLogLevelExtensions(StringBuilder sb, string receiver, string call, string returnType,
		string summary, string returns)
	{
		sb.Append("\t\t/// <inheritdoc cref=\"MockolateLogging\" />").AppendLine();
		sb.Append("\t\textension(").Append(receiver).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		AppendLogMember(sb, "Log", returnType, summary + " with a matching level and formatted message.", returns,
			"global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.LogLevel> logLevel, global::Mockolate.Parameters.IParameter<string> message",
			call + "logLevel, message, global::Mockolate.It.IsAny<global::System.Exception?>())", true, false);
		AppendLogMember(sb, "Log", returnType, summary + " with a matching level, formatted message and exception.", returns,
			"global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.LogLevel> logLevel, global::Mockolate.Parameters.IParameter<string> message, global::Mockolate.Parameters.IParameter<global::System.Exception?> exception",
			call + "logLevel, message, exception)", true, true);
		foreach (string level in new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", })
		{
			AppendLogMember(sb, "Log" + level, returnType,
				$"{summary} on the level <c>{level}</c> with a matching formatted message.", returns,
				"global::Mockolate.Parameters.IParameter<string> message",
				$"{call}global::Mockolate.It.Is(global::Microsoft.Extensions.Logging.LogLevel.{level}), message, global::Mockolate.It.IsAny<global::System.Exception?>())",
				false, false);
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void AppendLogMember(StringBuilder sb, string name, string returnType, string summary, string returns,
		string parameters, string body, bool hasLogLevel, bool hasException)
	{
		sb.AppendXmlSummary(summary, "\t\t\t");
		if (hasLogLevel)
		{
			sb.AppendXmlParam("logLevel", "A matcher for the level.", "\t\t\t");
		}

		sb.AppendXmlParam("message", "A matcher for the formatted message, e.g. <c>It.Matches(\"*failed*\")</c>.", "\t\t\t");
		if (hasException)
		{
			sb.AppendXmlParam("exception", "A matcher for the exception.", "\t\t\t");
		}

		sb.AppendXmlReturns(returns, "\t\t\t");
		sb.Append("\t\t\tpublic ").Append(returnType).Append(' ').Append(name).Append('(').Append(parameters).Append(')')
			.AppendLine();
		sb.Append("\t\t\t\t=> ").Append(body).Append(';').AppendLine();
		sb.AppendLine();
	}
}
