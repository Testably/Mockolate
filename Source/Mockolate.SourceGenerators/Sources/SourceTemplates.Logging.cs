using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	private const string LoggerFullName = "global::Microsoft.Extensions.Logging.ILogger";

	private static readonly string[] LogLevelNames = ["Trace", "Debug", "Information", "Warning", "Error", "Critical",];

	/// <summary>
	///     The category type <c>T</c> of a mocked <c>ILogger&lt;T&gt;</c>, or <see langword="null" /> for any other type.
	/// </summary>
	internal static string? GetLoggerCategoryType(string classFullName)
	{
		const string genericLoggerPrefix = LoggerFullName + "<";
		return classFullName.StartsWith(genericLoggerPrefix, StringComparison.Ordinal) &&
		       classFullName.EndsWith(">", StringComparison.Ordinal)
			? classFullName.Substring(genericLoggerPrefix.Length, classFullName.Length - genericLoggerPrefix.Length - 1)
			: null;
	}

	private static void AppendLoggerMockExtensions(StringBuilder sb, Class @class, string name)
	{
		if (!HasAdapter(@class, MockAdapter.LoggerFactory))
		{
			return;
		}

		sb.AppendLine();
		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(global::Mockolate.Mock.IMockFor").Append(name).Append(" mock)").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Gets the logger mock that <c>CreateLogger</c> returns for the <paramref name=\"categoryName\" /> when it has no setup.");
		sb.AppendXmlRemarks("Use it to set up or verify the logger before the code under test creates it.");
		sb.AppendXmlParam("categoryName", "The category name of the logger.");
		sb.Append("\t\tpublic global::Microsoft.Extensions.Logging.ILogger GetLogger(string categoryName)").AppendLine();
		sb.Append("\t\t\t=> global::Mockolate.MockolateLogging.CreateLogger(((global::Mockolate.IMock)mock).MockRegistry, categoryName);").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Gets the logger mock that <c>CreateLogger</c> returns for the category of <typeparamref name=\"TCategoryName\" /> when it has no setup.");
		sb.AppendXmlRemarks("The category name is the one that <c>ILogger&lt;TCategoryName&gt;</c> uses, so this is the logger that a <c>Logger&lt;TCategoryName&gt;</c> created by the factory writes to.");
		sb.AppendXmlTypeParam("TCategoryName", "The type whose name is the category name of the logger.");
		sb.Append("\t\tpublic global::Microsoft.Extensions.Logging.ILogger<TCategoryName> GetLogger<TCategoryName>()").AppendLine();
		sb.Append("\t\t\t=> global::Mockolate.MockolateLogging.GetLogger<TCategoryName>(((global::Mockolate.IMock)mock).MockRegistry);").AppendLine();
		sb.Append("\t}").AppendLine();
	}

	public static string Logging(bool hasLoggerFactory, LoggerMockNames loggerMockNames)
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

		          		/// <summary>
		          		///     The category of the logger, or <see langword="null" /> when the logger has none.
		          		/// </summary>
		          		string? Category { get; }

		          		/// <summary>
		          		///     The structured state of the log entry with the message template and its properties, or
		          		///     <see langword="null" /> when the state is not structured.
		          		/// </summary>
		          		global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>>? State { get; }
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
		          		public LogInvocation(string name, string? category, global::Microsoft.Extensions.Logging.LogLevel logLevel, global::Microsoft.Extensions.Logging.EventId eventId, TState state, global::System.Exception? exception, global::System.Func<TState, global::System.Exception?, string> formatter)
		          			: base(name, logLevel, eventId, state, exception, formatter)
		          		{
		          			Category = category;
		          		}

		          		/// <inheritdoc />
		          		global::Microsoft.Extensions.Logging.LogLevel ILogInvocation.LogLevel => Parameter1;

		          		/// <inheritdoc />
		          		global::Microsoft.Extensions.Logging.EventId ILogInvocation.EventId => Parameter2;

		          		/// <inheritdoc />
		          		public string Message => _message ??= Parameter5(Parameter3, Parameter4);

		          		/// <inheritdoc />
		          		global::System.Exception? ILogInvocation.Exception => Parameter4;

		          		/// <inheritdoc />
		          		public string? Category { get; }

		          		/// <inheritdoc />
		          		global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>>? ILogInvocation.State
		          			=> (object?)Parameter3 as global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>>;

		          		/// <inheritdoc cref="object.ToString()" />
		          		public override string ToString()
		          			=> $"invoke method Log({Parameter1}, {Parameter2}, {Message}, {Parameter4?.ToString() ?? "null"})";
		          	}
		          }

		          namespace Mockolate.Parameters
		          {
		          	/// <summary>
		          	///     Matches the message template and the properties of a call of <c>ILogger.Log</c>.
		          	/// </summary>
		          	/// <remarks>
		          	///     Created by <c>It.MatchesTemplate(…)</c> and passed to <c>WithMessage(…)</c> of a logging setup or verification.
		          	/// </remarks>
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal sealed class LogTemplateMatch
		          	{
		          		private const string OriginalFormat = "{OriginalFormat}";
		          		private readonly string _template;
		          		private readonly global::System.Text.StringBuilder _description = new();
		          		private (string Name, global::System.Func<object?, bool> Matches)[] _properties = [];

		          		/// <inheritdoc cref="LogTemplateMatch" />
		          		public LogTemplateMatch(string template)
		          		{
		          			_template = template;
		          			_description.Append("It.MatchesTemplate(\"").Append(template).Append("\")");
		          		}

		          		/// <summary>
		          		///     Additionally requires the property <paramref name="name" /> to equal the <paramref name="value" />.
		          		/// </summary>
		          		public LogTemplateMatch WithProperty(string name, object? value)
		          			=> Add(name, actual => Equals(actual, value), value switch
		          			{
		          				null => "null",
		          				string text => $"\"{text}\"",
		          				_ => value.ToString(),
		          			});

		          		/// <summary>
		          		///     Additionally requires the property <paramref name="name" /> to match the <paramref name="value" />.
		          		/// </summary>
		          		public LogTemplateMatch WithProperty<T>(string name, global::Mockolate.Parameters.IParameter<T> value)
		          		{
		          			global::Mockolate.Parameters.IParameterMatch<T> match = global::Mockolate.CovariantParameterAdapter<T>.Wrap(value);
		          			return Add(name, actual => actual is T typed ? match.Matches(typed) : actual is null && default(T) is null && match.Matches(default!), value.ToString());
		          		}

		          		/// <summary>
		          		///     Whether the structured <paramref name="state" /> of a log call has the template and the properties.
		          		/// </summary>
		          		public bool Matches(global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>>? state)
		          		{
		          			if (state is null || !TryGetValue(state, OriginalFormat, out object? template) || !string.Equals(template as string, _template, global::System.StringComparison.Ordinal))
		          			{
		          				return false;
		          			}

		          			foreach ((string name, global::System.Func<object?, bool> matches) in global::System.Threading.Volatile.Read(ref _properties))
		          			{
		          				if (!TryGetValue(state, name, out object? actual) || !matches(actual))
		          				{
		          					return false;
		          				}
		          			}

		          			return true;
		          		}

		          		/// <inheritdoc cref="object.ToString()" />
		          		public override string ToString()
		          		{
		          			lock (_description)
		          			{
		          				return _description.ToString();
		          			}
		          		}

		          		private LogTemplateMatch Add(string name, global::System.Func<object?, bool> matches, string? description)
		          		{
		          			lock (_description)
		          			{
		          				global::System.Threading.Volatile.Write(ref _properties, [.. _properties, (name, matches),]);
		          				_description.Append(".WithProperty(\"").Append(name).Append("\", ").Append(description).Append(')');
		          			}

		          			return this;
		          		}

		          		private static bool TryGetValue(global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.KeyValuePair<string, object?>> state, string name, out object? value)
		          		{
		          			for (int i = 0; i < state.Count; i++)
		          			{
		          				if (state[i].Key == name)
		          				{
		          					value = state[i].Value;
		          					return true;
		          				}
		          			}

		          			value = null;
		          			return false;
		          		}
		          	}
		          }

		          namespace Mockolate.Setup
		          {
		          	/// <summary>
		          	///     A setup of the calls of <c>ILogger.Log</c>, whatever the state type of the call is.
		          	/// </summary>
		          	/// <remarks>
		          	///     The callbacks receive the level, the event id, the formatted message and the exception.
		          	/// </remarks>
		          	internal interface ILogSetup : global::Mockolate.Setup.IVoidMethodSetupWithCallback<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, string, global::System.Exception?>
		          	{
		          		/// <summary>
		          		///     Also matches the calls on a higher level.
		          		/// </summary>
		          		ILogSetup OrHigher();

		          		/// <summary>
		          		///     Only matches the calls whose formatted message is exactly the <paramref name="message" />.
		          		/// </summary>
		          		ILogSetup WithMessage(string message);

		          		/// <summary>
		          		///     Only matches the calls whose formatted message matches the <paramref name="message" />, e.g. <c>It.Matches("*failed*")</c>.
		          		/// </summary>
		          		ILogSetup WithMessage(global::Mockolate.Parameters.IParameter<string> message);

		          		/// <summary>
		          		///     Only matches the calls with the message template and properties of <paramref name="template" />, created by <c>It.MatchesTemplate(…)</c>.
		          		/// </summary>
		          		ILogSetup WithMessage(global::Mockolate.Parameters.LogTemplateMatch template);

		          		/// <summary>
		          		///     Only matches the calls whose exception matches the <paramref name="exception" />.
		          		/// </summary>
		          		ILogSetup WithException(global::Mockolate.Parameters.IParameter<global::System.Exception?> exception);

		          		/// <summary>
		          		///     Only matches the calls with an exception of type <typeparamref name="TException" />.
		          		/// </summary>
		          		ILogSetup WithException<TException>() where TException : global::System.Exception;

		          		/// <summary>
		          		///     Only matches the calls with the <paramref name="eventId" />.
		          		/// </summary>
		          		ILogSetup WithEventId(global::Microsoft.Extensions.Logging.EventId eventId);

		          		/// <summary>
		          		///     Only matches the calls whose event id matches the <paramref name="eventId" />.
		          		/// </summary>
		          		ILogSetup WithEventId(global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.EventId> eventId);

		          		/// <summary>
		          		///     Only matches the calls of a logger with the <paramref name="categoryName" />.
		          		/// </summary>
		          		ILogSetup WithCategory(string categoryName);

		          		/// <summary>
		          		///     Only matches the calls of a logger with the category of <typeparamref name="TCategoryName" />.
		          		/// </summary>
		          		ILogSetup WithCategory<TCategoryName>();
		          	}
		          }

		          namespace Mockolate.Verify
		          {
		          	/// <summary>
		          	///     A verification of the calls of <c>ILogger.Log</c>, whatever the state type of the call is.
		          	/// </summary>
		          	/// <remarks>
		          	///     Narrow it down with the <c>With…</c> members and terminate it with a count assertion.
		          	/// </remarks>
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal sealed class LogVerificationResult<TVerify> : global::Mockolate.Internals.FilteredVerificationResult<TVerify>
		          	{
		          		private readonly global::Mockolate.MockolateLogging.LogFilter _filter;

		          		/// <inheritdoc cref="LogVerificationResult{TVerify}" />
		          		public LogVerificationResult(TVerify verify, global::Mockolate.Internals.MockRegistry mockRegistry, global::Mockolate.MockolateLogging.LogFilter filter)
		          			: base(verify, mockRegistry)
		          		{
		          			_filter = filter;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.OrHigher()" />
		          		public LogVerificationResult<TVerify> OrHigher()
		          		{
		          			_filter.OrHigher();
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithMessage(string)" />
		          		public LogVerificationResult<TVerify> WithMessage(string message)
		          		{
		          			_filter.WithMessage(message);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithMessage(global::Mockolate.Parameters.IParameter{string})" />
		          		public LogVerificationResult<TVerify> WithMessage(global::Mockolate.Parameters.IParameter<string> message)
		          		{
		          			_filter.WithMessage(message);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithMessage(global::Mockolate.Parameters.LogTemplateMatch)" />
		          		public LogVerificationResult<TVerify> WithMessage(global::Mockolate.Parameters.LogTemplateMatch template)
		          		{
		          			_filter.WithMessage(template);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithException(global::Mockolate.Parameters.IParameter{global::System.Exception})" />
		          		public LogVerificationResult<TVerify> WithException(global::Mockolate.Parameters.IParameter<global::System.Exception?> exception)
		          		{
		          			_filter.WithException(exception);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithException{TException}()" />
		          		public LogVerificationResult<TVerify> WithException<TException>() where TException : global::System.Exception
		          		{
		          			_filter.WithException<TException>();
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithEventId(global::Microsoft.Extensions.Logging.EventId)" />
		          		public LogVerificationResult<TVerify> WithEventId(global::Microsoft.Extensions.Logging.EventId eventId)
		          		{
		          			_filter.WithEventId(eventId);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithEventId(global::Mockolate.Parameters.IParameter{global::Microsoft.Extensions.Logging.EventId})" />
		          		public LogVerificationResult<TVerify> WithEventId(global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.EventId> eventId)
		          		{
		          			_filter.WithEventId(eventId);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithCategory(string)" />
		          		public LogVerificationResult<TVerify> WithCategory(string categoryName)
		          		{
		          			_filter.WithCategory(categoryName);
		          			return this;
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Setup.ILogSetup.WithCategory{TCategoryName}()" />
		          		public LogVerificationResult<TVerify> WithCategory<TCategoryName>()
		          		{
		          			_filter.WithCategory<TCategoryName>();
		          			return this;
		          		}

		          		/// <inheritdoc />
		          		protected override bool Matches(global::Mockolate.Interactions.IInteraction interaction)
		          			=> interaction is global::Mockolate.Interactions.ILogInvocation log && _filter.Matches(log);

		          		/// <inheritdoc />
		          		protected override string Describe()
		          			=> _filter.ToString();
		          	}
		          }

		          namespace Mockolate
		          {
		          	/// <summary>
		          	///     Setups and verifications of <c>ILogger.Log</c>, whatever the state type of the call is.
		          	/// </summary>
		          	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal static class MockolateLogging
		          	{
		          		private const string LogName = "global::Microsoft.Extensions.Logging.ILogger.Log";
		          		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Mockolate.Internals.MockRegistry, global::Mockolate.Internals.MockRegistry> s_factoryRegistries = new();
		          		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Mockolate.Internals.MockRegistry, string> s_categories = new();

		          		/// <inheritdoc cref="MockolateLogging" />
		          		extension(global::Mockolate.It _)
		          		{
		          			/// <summary>
		          			///     Matches a call of <c>ILogger.Log</c> with the message <paramref name="template" />, e.g. <c>"Order {OrderId} failed"</c>.
		          			/// </summary>
		          			/// <remarks>
		          			///     Pass it to <c>WithMessage(…)</c> of a logging setup or verification and check the properties with <c>.WithProperty(…)</c>.
		          			/// </remarks>
		          			/// <param name="template">The message template, compared exactly.</param>
		          			public static global::Mockolate.Parameters.LogTemplateMatch MatchesTemplate(string template)
		          				=> new(template);
		          		}


		          """);
		AppendLogEntryExtensions(sb, "(global::Mockolate.Setup.IMockSetup<global::Microsoft.Extensions.Logging.ILogger> setup)",
			"global::Mockolate.Setup.ILogSetup", "SetupLog(setup, ", "Sets up the calls of <c>ILogger.Log</c>");
		AppendLogEntryExtensions(sb,
			"<TVerify>(TVerify verify) where TVerify : class, global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILogger>",
			"global::Mockolate.Verify.LogVerificationResult<TVerify>",
			"VerifyLog(verify, ", "Verifies the calls of <c>ILogger.Log</c>");
		sb.Append("""
		          		/// <summary>
		          		///     Dispatches a call of <c>ILogger.Log</c>: setups of the state type <typeparamref name="TState" /> and logging
		          		///     setups both apply, the latest matching one wins.
		          		/// </summary>
		          		/// <remarks>
		          		///     The <paramref name="callBase" /> calls the implementation of a mocked class, unless the setup or the behavior
		          		///     skips the base class.
		          		/// </remarks>
		          		/// <param name="registry">The registry of the mock.</param>
		          		/// <param name="memberId">The member id of <c>Log</c> in the mock.</param>
		          		/// <param name="logName">The name of <c>Log</c> in the mock without its type arguments, under which the typed setups and verifications of the mock address it.</param>
		          		/// <param name="category">The category of the logger, or <see langword="null" /> to use the one of a logger of a factory.</param>
		          		/// <param name="logLevel">The level of the call.</param>
		          		/// <param name="eventId">The event id of the call.</param>
		          		/// <param name="state">The state of the call.</param>
		          		/// <param name="exception">The exception of the call.</param>
		          		/// <param name="formatter">The formatter of the call.</param>
		          		/// <param name="callBase">Calls the base implementation, or <see langword="null" /> when there is none.</param>
		          		public static void DispatchLog<TState>(global::Mockolate.Internals.MockRegistry registry, int memberId, string logName, string? category, global::Microsoft.Extensions.Logging.LogLevel logLevel, global::Microsoft.Extensions.Logging.EventId eventId, TState state, global::System.Exception? exception, global::System.Func<TState, global::System.Exception?, string> formatter, global::System.Action? callBase = null)
		          		{
		          			string name = $"{logName}<{typeof(TState)}>";
		          			if (category is null && s_categories.TryGetValue(registry, out string? registeredCategory))
		          			{
		          				category = registeredCategory;
		          			}

		          			global::Mockolate.Interactions.LogInvocation<TState> invocation = new(name, category, logLevel, eventId, state, exception, formatter);
		          			global::Mockolate.Setup.MethodSetup? setup = null;
		          			if (string.IsNullOrEmpty(registry.Scenario))
		          			{
		          				global::Mockolate.Setup.MethodSetup[]? snapshot = registry.GetMethodSetupSnapshot(memberId);
		          				if (snapshot is not null)
		          				{
		          					for (int i = snapshot.Length - 1; i >= 0; i--)
		          					{
		          						if (IsMatchingSetup(snapshot[i]))
		          						{
		          							setup = snapshot[i];
		          							break;
		          						}
		          					}
		          				}

		          				setup ??= FindSetup(registry.GetFallbackMethodSetups<global::Mockolate.Setup.MethodSetup>(memberId, name, LogName));
		          			}
		          			else
		          			{
		          				setup = FindSetup(registry.GetMethodSetups<global::Mockolate.Setup.MethodSetup>(name, LogName));
		          			}

		          			global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>? methodSetup = setup as global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>>;
		          			LogSetup? logSetup = setup as LogSetup;
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

		          				if (callBase is not null && !hasWrappedResult &&
		          				    !(methodSetup?.SkipBaseClass(registry.Behavior) ?? logSetup?.SkipBaseClass(registry.Behavior) ?? registry.Behavior.SkipBaseClass))
		          				{
		          					callBase();
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
		          				throw new global::Mockolate.Exceptions.MockNotSetupException($"The method '{logName}<TState>(LogLevel, EventId, TState, Exception?, Func<TState, Exception?, string>)' was invoked without prior setup.");
		          			}

		          			// Typed setups of the state type and logging setups are searched together, so the latest matching one wins.
		          			bool IsMatchingSetup(global::Mockolate.Setup.MethodSetup candidate)
		          				=> candidate switch
		          				{
		          					global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, TState, global::System.Exception?, global::System.Func<TState, global::System.Exception?, string>> typedSetup => typedSetup.Name == name && typedSetup.Matches(logLevel, eventId, state, exception, formatter),
		          					LogSetup messageSetup => messageSetup.MatchesLog(invocation),
		          					_ => false,
		          				};

		          			global::Mockolate.Setup.MethodSetup? FindSetup(global::System.Collections.Generic.IEnumerable<global::Mockolate.Setup.MethodSetup>? setups)
		          			{
		          				if (setups is not null)
		          				{
		          					foreach (global::Mockolate.Setup.MethodSetup candidate in setups)
		          					{
		          						if (IsMatchingSetup(candidate))
		          						{
		          							return candidate;
		          						}
		          					}
		          				}

		          				return null;
		          			}
		          		}

		          		/// <summary>
		          		///     Answers <c>ILogger.IsEnabled</c> without a setup: every level is enabled, so that logging code that checks
		          		///     <c>IsEnabled</c> first still reaches <c>Log</c>.
		          		/// </summary>
		          		/// <param name="registry">The registry of the mock.</param>
		          		/// <param name="logLevel">The level of the call.</param>
		          		public static bool IsEnabled(global::Mockolate.Internals.MockRegistry registry, global::Microsoft.Extensions.Logging.LogLevel logLevel)
		          			=> true;

		          		private static global::Mockolate.Internals.IMockAdapterAccess GetAccess(object mock)
		          			=> mock as global::Mockolate.Internals.IMockAdapterAccess ?? throw new global::Mockolate.Exceptions.MockException($"The logging setups and verifications do not support '{mock}'.");

		          		private static global::Mockolate.Setup.ILogSetup SetupLog(object setup, global::Microsoft.Extensions.Logging.LogLevel? logLevel, string entry)
		          		{
		          			global::Mockolate.Internals.IMockAdapterAccess access = GetAccess(setup);
		          			LogSetup methodSetup = new(access.MockRegistry, new LogFilter(logLevel, entry));
		          			access.MockRegistry.SetupMethod(access.GetMemberId("Log"), access.Scenario, methodSetup);
		          			return methodSetup;
		          		}

		          		internal static global::Mockolate.Verify.LogVerificationResult<TVerify> VerifyLog<TVerify>(TVerify verify, global::Microsoft.Extensions.Logging.LogLevel? logLevel, string entry)
		          			where TVerify : class
		          			=> new(verify, GetAccess(verify).MockRegistry, new LogFilter(logLevel, entry));

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
			          					global::Mockolate.Internals.MockRegistry loggerRegistry = ((global::Mockolate.IMock)logger).MockRegistry;
			          					s_factoryRegistries.Add(loggerRegistry, factoryRegistry);
			          					s_categories.Add(loggerRegistry, categoryName);
			          					factoryRegistry.ShareVerifiedInteractions(loggerRegistry);
			          					state.Loggers.Add(categoryName, logger);
			          				}

			          				return logger;
			          			}
			          		}

			          		/// <summary>
			          		///     Returns the logger mock of the factory with the <paramref name="factoryRegistry" /> for the category of <typeparamref name="TCategoryName" />.
			          		/// </summary>
			          		/// <remarks>
			          		///     The returned logger shares the registry of the logger that <see cref="CreateLogger" /> returns for the category.
			          		/// </remarks>
			          		public static global::Microsoft.Extensions.Logging.ILogger<TCategoryName> GetLogger<TCategoryName>(global::Mockolate.Internals.MockRegistry factoryRegistry)
			          		{
			          			global::Mockolate.Internals.MockRegistry registry = ((global::Mockolate.IMock)CreateLogger(factoryRegistry, CategoryName<TCategoryName>.Value)).MockRegistry;

			          """);
			foreach ((string categoryType, string name) in loggerMockNames.CategoryLoggers)
			{
				sb.Append("\t\t\tif (IsSameType<TCategoryName, ").Append(categoryType).Append(">())").AppendLine();
				sb.Append("\t\t\t{").AppendLine();
				sb.Append("\t\t\t\treturn (global::Microsoft.Extensions.Logging.ILogger<TCategoryName>)(object)new global::Mockolate.Mock.")
					.Append(name).Append("(registry);").AppendLine();
				sb.Append("\t\t\t}").AppendLine();
				sb.AppendLine();
			}

			sb.Append("""
			          			return new CategoryLogger<TCategoryName>(registry);
			          		}

			          		/// <summary>
			          		///     Clears the interactions of the factory with the <paramref name="factoryRegistry" /> and of all loggers it created.
			          		/// </summary>
			          		public static void ClearFactoryInteractions(global::Mockolate.Internals.MockRegistry factoryRegistry)
			          		{
			          			factoryRegistry.ClearAllInteractions();
			          			if (s_loggerFactories.TryGetValue(factoryRegistry, out LoggerFactoryState? state))
			          			{
			          				global::Microsoft.Extensions.Logging.ILogger[] loggers;
			          				lock (state.Loggers)
			          				{
			          					loggers = new global::Microsoft.Extensions.Logging.ILogger[state.Loggers.Count];
			          					state.Loggers.Values.CopyTo(loggers, 0);
			          				}

			          				foreach (global::Microsoft.Extensions.Logging.ILogger logger in loggers)
			          				{
			          					((global::Mockolate.IMock)logger).MockRegistry.ClearAllInteractions();
			          				}
			          			}
			          		}

			          		private static bool IsSameType<T1, T2>()
			          			=> typeof(T1) == typeof(T2);

			          		private sealed class CategoryLogger<TCategoryName> : global::Mockolate.Mock.
			          """).Append(loggerMockNames.LoggerName).Append("""
			          , global::Microsoft.Extensions.Logging.ILogger<TCategoryName>
			          		{
			          			public CategoryLogger(global::Mockolate.Internals.MockRegistry mockRegistry)
			          				: base(mockRegistry)
			          			{
			          			}
			          		}

			          		private sealed class LoggerFactoryState
			          		{
			          			public global::System.Collections.Generic.Dictionary<string, global::Microsoft.Extensions.Logging.ILogger> Loggers { get; } = new();
			          		}

			          """);
		}

		sb.Append("""
		          		/// <summary>
		          		///     The category name that <c>Logger&lt;TCategoryName&gt;</c> requests from its factory.
		          		/// </summary>
		          		internal static class CategoryName<TCategoryName>
		          		{
		          			/// <summary>
		          			///     The category name of <typeparamref name="TCategoryName" />.
		          			/// </summary>
		          			public static readonly string Value = GetValue();

		          			private static string GetValue()
		          			{
		          				CategoryNameCapture capture = new();
		          				_ = new global::Microsoft.Extensions.Logging.Logger<TCategoryName>(capture);
		          				return capture.CategoryName;
		          			}
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

		          		/// <summary>
		          		///     The conditions of a logging setup or verification, shared by both.
		          		/// </summary>
		          		internal sealed class LogFilter
		          		{
		          			private readonly global::Microsoft.Extensions.Logging.LogLevel? _logLevel;
		          			private readonly global::System.Text.StringBuilder _description = new();
		          			private global::System.Func<global::Mockolate.Interactions.ILogInvocation, bool>[] _conditions = [];
		          			private volatile bool _orHigher;

		          			public LogFilter(global::Microsoft.Extensions.Logging.LogLevel? logLevel, string entry)
		          			{
		          				_logLevel = logLevel;
		          				_description.Append(entry);
		          			}

		          			public void OrHigher()
		          			{
		          				lock (_description)
		          				{
		          					_orHigher = true;
		          					_description.Append(".OrHigher()");
		          				}
		          			}

		          			public void WithMessage(string message)
		          				=> Add(log => string.Equals(log.Message, message, global::System.StringComparison.Ordinal), $".WithMessage(\"{message}\")");

		          			public void WithMessage(global::Mockolate.Parameters.IParameter<string> message)
		          			{
		          				global::Mockolate.Parameters.IParameterMatch<string> match = global::Mockolate.CovariantParameterAdapter<string>.Wrap(message);
		          				Add(log => match.Matches(log.Message), $".WithMessage({message})");
		          			}

		          			public void WithMessage(global::Mockolate.Parameters.LogTemplateMatch template)
		          				=> Add(log => template.Matches(log.State), $".WithMessage({template})");

		          			public void WithException(global::Mockolate.Parameters.IParameter<global::System.Exception?> exception)
		          			{
		          				global::Mockolate.Parameters.IParameterMatch<global::System.Exception?> match = global::Mockolate.CovariantParameterAdapter<global::System.Exception?>.Wrap(exception);
		          				Add(log => match.Matches(log.Exception), $".WithException({exception})");
		          			}

		          			public void WithException<TException>() where TException : global::System.Exception
		          				=> Add(log => log.Exception is TException, $".WithException<{typeof(TException).Name}>()");

		          			public void WithEventId(global::Microsoft.Extensions.Logging.EventId eventId)
		          				=> Add(log => log.EventId.Id == eventId.Id, $".WithEventId({eventId.Id})");

		          			public void WithEventId(global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.EventId> eventId)
		          			{
		          				global::Mockolate.Parameters.IParameterMatch<global::Microsoft.Extensions.Logging.EventId> match = global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.EventId>.Wrap(eventId);
		          				Add(log => match.Matches(log.EventId), $".WithEventId({eventId})");
		          			}

		          			public void WithCategory(string categoryName)
		          				=> Add(log => string.Equals(log.Category, categoryName, global::System.StringComparison.Ordinal), $".WithCategory(\"{categoryName}\")");

		          			public void WithCategory<TCategoryName>()
		          				=> Add(log => string.Equals(log.Category, CategoryName<TCategoryName>.Value, global::System.StringComparison.Ordinal), $".WithCategory<{typeof(TCategoryName).Name}>()");

		          			public bool Matches(global::Mockolate.Interactions.ILogInvocation log)
		          			{
		          				if (_logLevel is { } logLevel && (_orHigher ? log.LogLevel < logLevel : log.LogLevel != logLevel))
		          				{
		          					return false;
		          				}

		          				foreach (global::System.Func<global::Mockolate.Interactions.ILogInvocation, bool> condition in global::System.Threading.Volatile.Read(ref _conditions))
		          				{
		          					if (!condition(log))
		          					{
		          						return false;
		          					}
		          				}

		          				return true;
		          			}

		          			public override string ToString()
		          			{
		          				lock (_description)
		          				{
		          					return _description.ToString();
		          				}
		          			}

		          			private void Add(global::System.Func<global::Mockolate.Interactions.ILogInvocation, bool> condition, string description)
		          			{
		          				lock (_description)
		          				{
		          					global::System.Threading.Volatile.Write(ref _conditions, [.. _conditions, condition,]);
		          					_description.Append(description);
		          				}
		          			}
		          		}

		          		private sealed class LogSetup : global::Mockolate.Setup.VoidMethodSetup<global::Microsoft.Extensions.Logging.LogLevel, global::Microsoft.Extensions.Logging.EventId, string, global::System.Exception?>.WithParameterCollection, global::Mockolate.Internals.IGenericMethodSetup, global::Mockolate.Setup.ILogSetup
		          		{
		          			private readonly LogFilter _filter;

		          			public LogSetup(global::Mockolate.Internals.MockRegistry mockRegistry, LogFilter filter)
		          				: base(mockRegistry, LogName,
		          					global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.LogLevel>.Wrap(global::Mockolate.It.IsAny<global::Microsoft.Extensions.Logging.LogLevel>()),
		          					global::Mockolate.CovariantParameterAdapter<global::Microsoft.Extensions.Logging.EventId>.Wrap(global::Mockolate.It.IsAny<global::Microsoft.Extensions.Logging.EventId>()),
		          					global::Mockolate.CovariantParameterAdapter<string>.Wrap(global::Mockolate.It.IsAny<string>()),
		          					global::Mockolate.CovariantParameterAdapter<global::System.Exception?>.Wrap(global::Mockolate.It.IsAny<global::System.Exception?>()))
		          			{
		          				_filter = filter;
		          			}

		          			public bool MatchesLog(global::Mockolate.Interactions.ILogInvocation log)
		          				=> _filter.Matches(log);

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.OrHigher()
		          			{
		          				_filter.OrHigher();
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithMessage(string message)
		          			{
		          				_filter.WithMessage(message);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithMessage(global::Mockolate.Parameters.IParameter<string> message)
		          			{
		          				_filter.WithMessage(message);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithMessage(global::Mockolate.Parameters.LogTemplateMatch template)
		          			{
		          				_filter.WithMessage(template);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithException(global::Mockolate.Parameters.IParameter<global::System.Exception?> exception)
		          			{
		          				_filter.WithException(exception);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithException<TException>()
		          			{
		          				_filter.WithException<TException>();
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithEventId(global::Microsoft.Extensions.Logging.EventId eventId)
		          			{
		          				_filter.WithEventId(eventId);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithEventId(global::Mockolate.Parameters.IParameter<global::Microsoft.Extensions.Logging.EventId> eventId)
		          			{
		          				_filter.WithEventId(eventId);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithCategory(string categoryName)
		          			{
		          				_filter.WithCategory(categoryName);
		          				return this;
		          			}

		          			global::Mockolate.Setup.ILogSetup global::Mockolate.Setup.ILogSetup.WithCategory<TCategoryName>()
		          			{
		          				_filter.WithCategory<TCategoryName>();
		          				return this;
		          			}

		          			/// <inheritdoc cref="object.ToString()" />
		          			public override string ToString()
		          				=> _filter.ToString();

		          			protected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)
		          				=> interaction is global::Mockolate.Interactions.ILogInvocation log && _filter.Matches(log);
		          		}
		          	}

		          """);
		if (hasLoggerFactory)
		{
			// The verify extensions of loggers and logger factories differ only in the constraint of their receiver,
			// so they need separate classes.
			sb.Append("""

			          	/// <summary>
			          	///     Verifications of <c>ILogger.Log</c> on all loggers that a logger factory mock created.
			          	/// </summary>
			          	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
			          	internal static class MockolateLoggerFactoryLogging
			          	{

			          """);
			AppendLogEntryExtensions(sb,
				"<TVerify>(TVerify verify) where TVerify : class, global::Mockolate.Verify.IMockVerify<global::Microsoft.Extensions.Logging.ILoggerFactory>",
				"global::Mockolate.Verify.LogVerificationResult<TVerify>",
				"global::Mockolate.MockolateLogging.VerifyLog(verify, ",
				"Verifies the calls of <c>ILogger.Log</c> on all loggers that the factory created");
			sb.Append("\t}").AppendLine();
		}

		sb.Append("""
		          }

		          #nullable disable annotations

		          """);
		return sb.ToString();
	}

	private static void AppendLogEntryExtensions(StringBuilder sb, string receiver, string returnType, string call,
		string summary)
	{
		sb.Append("\t\t/// <inheritdoc cref=\"MockolateLogging\" />").AppendLine();
		sb.Append("\t\textension").Append(receiver).AppendLine();
		sb.Append("\t\t{").AppendLine();
		AppendLogEntry(sb, returnType, "Log", "", $"{call}null, \"Log()\")", $"{summary} on any level.", false);
		AppendLogEntry(sb, returnType, "Log", "global::Microsoft.Extensions.Logging.LogLevel logLevel",
			$"{call}logLevel, \"Log(\" + logLevel + \")\")", $"{summary} on the <paramref name=\"logLevel\" />.", true);
		foreach (string level in LogLevelNames)
		{
			AppendLogEntry(sb, returnType, "Log" + level, "",
				$"{call}global::Microsoft.Extensions.Logging.LogLevel.{level}, \"Log{level}()\")",
				$"{summary} on the level <c>{level}</c>.", false);
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void AppendLogEntry(StringBuilder sb, string returnType, string name, string parameters, string body,
		string summary, bool hasLogLevel)
	{
		sb.AppendXmlSummary(summary, "\t\t\t");
		sb.AppendXmlRemarks("Narrow it down with <c>OrHigher()</c>, <c>WithMessage(…)</c>, <c>WithException(…)</c>, <c>WithEventId(…)</c> and <c>WithCategory(…)</c>.", "\t\t\t");
		if (hasLogLevel)
		{
			sb.AppendXmlParam("logLevel", "The level of the calls.", "\t\t\t");
		}

		sb.Append("\t\t\tpublic ").Append(returnType).Append(' ').Append(name).Append('(').Append(parameters).Append(')')
			.AppendLine();
		sb.Append("\t\t\t\t=> ").Append(body).Append(';').AppendLine();
		sb.AppendLine();
	}
}
