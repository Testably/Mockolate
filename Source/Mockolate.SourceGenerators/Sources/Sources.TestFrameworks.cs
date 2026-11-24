namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The code that the <c>AutoMockArguments</c> attributes of all test frameworks share, guarded by the constant that
	///     <c>Mockolate.targets</c> defines when the project references a supported test framework.
	/// </summary>
	/// <remarks>
	///     All attributes are emitted as post-initialization output, because TUnit's own source generator discovers data
	///     sources and cannot see the regular output of another generator: the test would compile, but run without arguments.
	///     The types are embedded, so that test projects which see each other's internals do not get conflicting types. The
	///     embedded attribute is declared here instead of through <c>AddEmbeddedAttributeDefinition</c>, so that only
	///     projects with a test framework get it; it is partial like the one that API adds for other generators.
	/// </remarks>
	public static string AutoMockArgumentsShared()
		=> InitializeBuilder().Append("""
		   #if MOCKOLATE_TEST_FRAMEWORK
		   #nullable enable
		   namespace Microsoft.CodeAnalysis
		   {
		   	internal sealed partial class EmbeddedAttribute : global::System.Attribute
		   	{
		   	}
		   }

		   namespace Mockolate.Integrations
		   {
		   	/// <summary>
		   	///     Excludes a test parameter from the arguments that are shared with the classes under test.
		   	/// </summary>
		   	[global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
		   	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal abstract class TransientParameterAttribute : global::System.Attribute
		   	{
		   	}

		   	/// <summary>
		   	///     Creates the arguments of one test invocation on demand, so that a class under test receives the arguments of
		   	///     the other parameters whatever their order.
		   	/// </summary>
		   	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal sealed class AutoMockRow
		   	{
		   		private readonly global::System.Reflection.Assembly _assembly;
		   		private readonly global::System.Type[] _types;
		   		private readonly string[] _names;
		   		private readonly global::System.Func<int, bool> _isTransientParameter;
		   		private readonly object?[] _arguments;
		   		private readonly global::Mockolate.MockBehavior? _behavior;
		   		private readonly int[] _states;
		   		private readonly object?[] _values;
		   		private readonly bool?[] _isTransient;

		   		/// <param name="assembly">The test assembly, whose generated code creates the parameters.</param>
		   		/// <param name="types">The types of the parameters of the test method.</param>
		   		/// <param name="names">The names of the parameters of the test method.</param>
		   		/// <param name="isTransientParameter">Whether the parameter at an index is marked as transient.</param>
		   		/// <param name="arguments">The given arguments of the first parameters, which are not shared.</param>
		   		/// <param name="behavior">The behavior of the created mocks and values.</param>
		   		public AutoMockRow(global::System.Reflection.Assembly assembly, global::System.Type[] types, string[] names,
		   			global::System.Func<int, bool> isTransientParameter, object?[] arguments, global::Mockolate.MockBehavior? behavior)
		   		{
		   			if (arguments.Length > types.Length)
		   			{
		   				throw new global::Mockolate.Exceptions.MockException(
		   					$"AutoMockArguments has {arguments.Length} arguments, but the test method has only {types.Length} parameters.");
		   			}

		   			_assembly = assembly;
		   			_types = types;
		   			_names = names;
		   			_isTransientParameter = isTransientParameter;
		   			_arguments = arguments;
		   			_behavior = behavior;
		   			_states = new int[types.Length];
		   			_values = new object?[types.Length];
		   			_isTransient = new bool?[types.Length];
		   		}

		   		/// <summary>
		   		///     The name of a test case. It only contains the given arguments, because formatting a mock would call its
		   		///     members before the test starts.
		   		/// </summary>
		   		public static string GetDisplayName(string name, object?[] arguments)
		   		{
		   			if (arguments.Length == 0)
		   			{
		   				return name;
		   			}

		   			return $"{name}({string.Join(", ", global::System.Array.ConvertAll(arguments, FormatValue))})";
		   		}

		   		private static string FormatValue(object? value)
		   		{
		   			global::System.Globalization.CultureInfo culture = global::System.Globalization.CultureInfo.InvariantCulture;
		   			switch (value)
		   			{
		   				case null:
		   					return "null";
		   				case string text:
		   					return $"\"{Escape(text, '"')}\"";
		   				case char character:
		   					return $"'{Escape(character.ToString(), '\'')}'";
		   				case bool flag:
		   					return flag ? "true" : "false";
		   				case long number:
		   					return number.ToString(culture) + "L";
		   				case ulong number:
		   					return number.ToString(culture) + "UL";
		   				case uint number:
		   					return number.ToString(culture) + "U";
		   				case float number:
		   					return number.ToString("R", culture) + "F";
		   				case double number:
		   					return number.ToString("R", culture) + "D";
		   				case decimal number:
		   					return number.ToString(culture) + "M";
		   				case global::System.Array array:
		   					string[] items = new string[array.Length];
		   					for (int index = 0; index < items.Length; index++)
		   					{
		   						items[index] = FormatValue(array.GetValue(index));
		   					}

		   					return $"[{string.Join(", ", items)}]";
		   				case global::System.IFormattable formattable:
		   					return formattable.ToString(null, culture);
		   				default:
		   					return value.ToString() ?? "null";
		   			}
		   		}

		   		private static string Escape(string text, char quote)
		   		{
		   			global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(text.Length);
		   			foreach (char character in text)
		   			{
		   				switch (character)
		   				{
		   					case '\\': builder.Append("\\\\"); break;
		   					case '\0': builder.Append("\\0"); break;
		   					case '\n': builder.Append("\\n"); break;
		   					case '\r': builder.Append("\\r"); break;
		   					case '\t': builder.Append("\\t"); break;
		   					default:
		   						if (character == quote)
		   						{
		   							builder.Append('\\').Append(character);
		   						}
		   						else if (char.IsControl(character))
		   						{
		   							builder.Append("\\u").Append(((int)character).ToString("x4", global::System.Globalization.CultureInfo.InvariantCulture));
		   						}
		   						else
		   						{
		   							builder.Append(character);
		   						}

		   						break;
		   				}
		   			}

		   			return builder.ToString();
		   		}

		   		public object?[] Create()
		   		{
		   			for (int index = 0; index < _arguments.Length; index++)
		   			{
		   				_values[index] = _arguments[index];
		   				_states[index] = 2;
		   			}

		   			for (int index = 0; index < _types.Length; index++)
		   			{
		   				GetOrCreate(index);
		   			}

		   			return _values;
		   		}

		   		private object? GetOrCreate(int index)
		   		{
		   			if (_states[index] == 2)
		   			{
		   				return _values[index];
		   			}

		   			_states[index] = 1;
		   			if (!global::Mockolate.Internals.MockDataSource.TryCreate(_assembly, _types[index], _behavior,
		   				    (global::System.Type type, string parameterName, out object? value) =>
		   					    TryResolve(type, parameterName, index, out value), out object? created))
		   			{
		   				throw new global::Mockolate.Exceptions.MockException(
		   					$"Mockolate cannot create the parameter '{_names[index]}' of type '{_types[index]}'. Pass it as an argument, or register a default value for its type through AutoMockArgumentsAttribute.DefaultBehavior.");
		   			}

		   			_values[index] = created;
		   			_states[index] = 2;
		   			return created;
		   		}

		   		// Given arguments are not shared: a value like 4 would otherwise reach every int of every class under test.
		   		private bool IsTransient(int index)
		   			=> index < _arguments.Length || (_isTransient[index] ??= _isTransientParameter(index));

		   		/// <summary>
		   		///     The shared test parameter of the <paramref name="type" />; the <paramref name="parameterName" /> of the
		   		///     constructor parameter decides between several of them, and must match for a value type or a string. A
		   		///     parameter that is being created is no candidate, so that two classes can depend on each other's type.
		   		/// </summary>
		   		private bool TryResolve(global::System.Type type, string parameterName, int requester, out object? value)
		   		{
		   			global::System.Collections.Generic.List<int> candidates = new();
		   			for (int index = 0; index < _types.Length; index++)
		   			{
		   				if (index != requester && _states[index] != 1 && _types[index] == type && !IsTransient(index))
		   				{
		   					candidates.Add(index);
		   				}
		   			}

		   			if (type.IsValueType || type == typeof(string))
		   			{
		   				// A value is only shared by name: a value like 0 would otherwise reach every int of every class under test.
		   				candidates = candidates.FindAll(index =>
		   					string.Equals(_names[index], parameterName, global::System.StringComparison.OrdinalIgnoreCase));
		   			}

		   			if (candidates.Count > 1)
		   			{
		   				global::System.Collections.Generic.List<int> named = candidates.FindAll(index =>
		   					string.Equals(_names[index], parameterName, global::System.StringComparison.OrdinalIgnoreCase));
		   				if (named.Count != 1)
		   				{
		   					string names = string.Join(", ", candidates.ConvertAll(index => $"'{_names[index]}'"));
		   					throw new global::Mockolate.Exceptions.MockException(
		   						$"The constructor parameter '{parameterName}' of type '{type}' matches the test parameters {names}; name one of them '{parameterName}' or mark the others [Transient].");
		   				}

		   				candidates = named;
		   			}

		   			if (candidates.Count == 1)
		   			{
		   				value = GetOrCreate(candidates[0]);
		   				return true;
		   			}

		   			value = null;
		   			return false;
		   		}
		   	}
		   }
		   #nullable disable
		   #endif

		   """).ToString();

	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for TUnit.
	/// </summary>
	public static string AutoMockArgumentsForTUnit()
		=> AutoMockArgumentsAttribute("MOCKOLATE_TUNIT", "TUnit", "TUnit creates the arguments before it runs any hook, and on another thread,",
			"global::TUnit.Core.UntypedDataSourceGeneratorAttribute", """
			   		/// <inheritdoc />
			   		protected override global::System.Collections.Generic.IEnumerable<global::System.Func<object?[]?>> GenerateDataSources(
			   			global::TUnit.Core.DataGeneratorMetadata dataGeneratorMetadata)
			   		{
			   			// The attribute only applies to methods, so every member is a parameter.
			   			global::TUnit.Core.ParameterMetadata[] parameters = global::System.Array.ConvertAll(
			   				dataGeneratorMetadata.MembersToGenerate, member => (global::TUnit.Core.ParameterMetadata)member);
			   			global::System.Type[] types = global::System.Array.ConvertAll(parameters, parameter => parameter.Type);
			   			string[] names = global::System.Array.ConvertAll(parameters, parameter => parameter.Name);
			   			// The attribute is generated into every test assembly, next to the code that creates the parameters.
			   			global::System.Reflection.Assembly assembly = typeof(AutoMockArgumentsAttribute).Assembly;
			   			// Only a parameter that could be shared needs its reflection info, which older versions of TUnit can lack.
			   			yield return () => new global::Mockolate.Integrations.AutoMockRow(assembly, types, names,
			   				index => parameters[index].ReflectionInfo is { } reflectionInfo &&
			   				         global::System.Reflection.CustomAttributeExtensions.IsDefined(reflectionInfo,
			   					         typeof(global::Mockolate.Integrations.TransientParameterAttribute), false),
			   				Arguments, DefaultBehavior?.Invoke()).Create();
			   		}
			   """);

	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for xUnit v2, which a project that also references xUnit v3 does not get.
	/// </summary>
	public static string AutoMockArgumentsForXunit2()
		=> AutoMockArgumentsAttribute("MOCKOLATE_XUNIT2 && !MOCKOLATE_XUNIT3", "Xunit", "xUnit can create the arguments during discovery, on another thread,",
			"global::Xunit.Sdk.DataAttribute", ParameterInfoArguments + """
			   		/// <inheritdoc />
			   		public override global::System.Collections.Generic.IEnumerable<object[]> GetData(
			   			global::System.Reflection.MethodInfo testMethod)
			   		{
			   			yield return CreateArguments(testMethod.GetParameters(), Arguments)!;
			   		}
			   """);

	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for xUnit v3.
	/// </summary>
	public static string AutoMockArgumentsForXunit3()
		=> AutoMockArgumentsAttribute("MOCKOLATE_XUNIT3", "Xunit", "xUnit creates the arguments before it runs the test, possibly on another thread,",
			"global::Xunit.v3.DataAttribute", ParameterInfoArguments + """
			   		/// <inheritdoc />
			   		public override global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IReadOnlyCollection<global::Xunit.ITheoryDataRow>> GetData(
			   			global::System.Reflection.MethodInfo testMethod, global::Xunit.Sdk.DisposalTracker disposalTracker)
			   		{
			   			// Like xUnit's InlineData, the row takes every setting of the attribute that the version of xUnit knows.
			   			return new global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IReadOnlyCollection<global::Xunit.ITheoryDataRow>>(
			   				new global::Xunit.ITheoryDataRow[] { ConvertDataRow(CreateArguments(testMethod.GetParameters(), Arguments)), });
			   		}

			   		/// <summary>
			   		///     Mocks cannot be serialized, so the arguments are created when the test runs.
			   		/// </summary>
			   		public override bool SupportsDiscoveryEnumeration()
			   			=> false;
			   """);

	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for NUnit, which replaces <c>[Test]</c> like <c>[TestCase]</c> does.
	/// </summary>
	/// <remarks>
	///     NUnit builds the test cases during discovery and keeps their arguments, so the attribute only reserves them there
	///     and creates them before every run, also of a repeated or retried test.
	/// </remarks>
	public static string AutoMockArgumentsForNUnit()
		=> AutoMockArgumentsAttribute("MOCKOLATE_NUNIT", "NUnit", "NUnit runs a test on a thread of its own,",
			"global::NUnit.Framework.NUnitAttribute, global::NUnit.Framework.Interfaces.ITestBuilder, global::NUnit.Framework.Interfaces.IImplyFixture, global::NUnit.Framework.Interfaces.IWrapSetUpTearDown",
			ParameterInfoArguments + """
			   		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<object?[], PendingArguments> Pending = new();

			   		/// <inheritdoc />
			   		public global::System.Collections.Generic.IEnumerable<global::NUnit.Framework.Internal.TestMethod> BuildFrom(
			   			global::NUnit.Framework.Interfaces.IMethodInfo method, global::NUnit.Framework.Internal.Test? suite)
			   		{
			   			global::System.Reflection.ParameterInfo[] parameters = method.MethodInfo.GetParameters();
			   #if MOCKOLATE_NUNIT4
			   			// NUnit 4 passes its own token to a last CancellationToken parameter that gets no argument.
			   			if (parameters.Length > Arguments.Length &&
			   			    parameters[parameters.Length - 1].ParameterType == typeof(global::System.Threading.CancellationToken))
			   			{
			   				global::System.Array.Resize(ref parameters, parameters.Length - 1);
			   			}
			   #endif
			   			global::NUnit.Framework.Internal.TestCaseParameters testCase;
			   			if (Arguments.Length > parameters.Length)
			   			{
			   				testCase = new global::NUnit.Framework.Internal.TestCaseParameters(new global::Mockolate.Exceptions.MockException(
			   					$"AutoMockArguments has {Arguments.Length} arguments, but the test method has only {parameters.Length} parameters."));
			   			}
			   			else
			   			{
			   				object?[] reserved = new object?[parameters.Length];
			   				global::System.Array.Copy(Arguments, reserved, Arguments.Length);
			   				testCase = new global::NUnit.Framework.Internal.TestCaseParameters(reserved);
			   			}

			   			// NUnit would read braces in the given arguments as a pattern, so the name is set on the built test.
			   			testCase.TestName = method.Name;
			   			global::NUnit.Framework.Internal.TestMethod test =
			   				new global::NUnit.Framework.Internal.Builders.NUnitTestCaseBuilder().BuildTestMethod(method, suite, testCase);
			   			test.Name = global::Mockolate.Integrations.AutoMockRow.GetDisplayName(method.Name, Arguments);
			   			test.FullName = $"{suite?.FullName ?? method.TypeInfo.FullName}.{test.Name}";
			   			// A test case that is not runnable can share an empty argument array with others.
			   			Pending.Remove(test.Arguments);
			   			Pending.Add(test.Arguments, new PendingArguments(parameters, Arguments));
			   			yield return test;
			   		}

			   		/// <inheritdoc />
			   		public global::NUnit.Framework.Internal.Commands.TestCommand Wrap(
			   			global::NUnit.Framework.Internal.Commands.TestCommand command)
			   			=> new CreateArgumentsCommand(command);

			   		private sealed class PendingArguments(global::System.Reflection.ParameterInfo[] parameters, object?[] arguments)
			   		{
			   			public global::System.Reflection.ParameterInfo[] Parameters { get; } = parameters;
			   			public object?[] Arguments { get; } = arguments;
			   			public int Depth { get; set; }
			   		}

			   		/// <summary>
			   		///     Creates the arguments of the test before it runs. Every <c>AutoMockArguments</c> attribute on the method wraps
			   		///     each of its test cases, so only the outermost command of a run creates them.
			   		/// </summary>
			   		private sealed class CreateArgumentsCommand(global::NUnit.Framework.Internal.Commands.TestCommand innerCommand)
			   			: global::NUnit.Framework.Internal.Commands.DelegatingTestCommand(innerCommand)
			   		{
			   			/// <inheritdoc />
			   			public override global::NUnit.Framework.Internal.TestResult Execute(
			   				global::NUnit.Framework.Internal.TestExecutionContext context)
			   			{
			   				if (Test is not global::NUnit.Framework.Internal.TestMethod test ||
			   				    !Pending.TryGetValue(test.Arguments, out PendingArguments? pending))
			   				{
			   					return innerCommand.Execute(context);
			   				}

			   				if (pending.Depth++ == 0)
			   				{
			   					object?[] values = CreateArguments(pending.Parameters, pending.Arguments);
			   					global::System.Array.Copy(values, test.Arguments, values.Length);
			   				}

			   				try
			   				{
			   					return innerCommand.Execute(context);
			   				}
			   				finally
			   				{
			   					pending.Depth--;
			   				}
			   			}
			   		}
			   """);

	/// <summary>
	///     Creates the arguments from the <see cref="System.Reflection.ParameterInfo" />s of the test method, for the test
	///     frameworks that provide them.
	/// </summary>
	private const string ParameterInfoArguments = """
	                                              		// The attribute is generated into every test assembly, next to the code that creates the parameters.
	                                              		private static object?[] CreateArguments(global::System.Reflection.ParameterInfo[] parameters, object?[] arguments)
	                                              			=> new global::Mockolate.Integrations.AutoMockRow(typeof(AutoMockArgumentsAttribute).Assembly,
	                                              				global::System.Array.ConvertAll(parameters, parameter => parameter.ParameterType),
	                                              				global::System.Array.ConvertAll(parameters, parameter => parameter.Name ?? ""),
	                                              				index => global::System.Reflection.CustomAttributeExtensions.IsDefined(parameters[index],
	                                              					typeof(global::Mockolate.Integrations.TransientParameterAttribute), false),
	                                              				arguments, DefaultBehavior?.Invoke()).Create();


	                                              """;

	private static string AutoMockArgumentsAttribute(string condition, string @namespace, string timing, string baseType,
		string members)
		=> InitializeBuilder().Append($$"""
		   #if {{condition}}
		   #nullable enable
		   namespace Mockolate.Integrations.{{@namespace}}
		   {
		   	/// <summary>
		   	///     Passes the <see cref="Arguments" /> to the first parameters of the test method and fills every remaining
		   	///     parameter: an interface, delegate or abstract class gets a mock, a class gets a mock that forwards to its real
		   	///     implementation, and a sealed class or record a real instance. Their constructor dependencies are mocks, and
		   	///     other values come from the default values of the behavior.
		   	/// </summary>
		   	/// <remarks>
		   	///     A constructor dependency of a class under test gets the argument of the test parameter of the same type, so the
		   	///     test can set up and verify the mock that the class under test uses. When several test parameters have the type,
		   	///     the one with the name of the constructor parameter (ignoring case) is used; a value type or string is only
		   	///     shared with a constructor parameter of its name. The given arguments and a <see cref="TransientAttribute" />
		   	///     parameter are not shared. Each attribute on a method is one test case.
		   	/// </remarks>
		   	[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal sealed class AutoMockArgumentsAttribute
		   		: {{baseType}}, global::Mockolate.Internals.IMockDataSource
		   	{
		   		/// <summary>
		   		///     Fills every parameter of the test method, or the parameters after the given <paramref name="arguments" />.
		   		/// </summary>
		   		/// <param name="arguments">The arguments of the first parameters of the test method.</param>
		   		public AutoMockArgumentsAttribute(params object?[]? arguments)
		   		{
		   			// A single null argument binds to the array itself.
		   			Arguments = arguments ?? new object?[] { null, };
		   		}

		   		/// <summary>
		   		///     The arguments of the first parameters of the test method.
		   		/// </summary>
		   		public object?[] Arguments { get; }

		   		/// <summary>
		   		///     Provides the behavior of the created mocks and of the values that are neither mocked nor given, each time
		   		///     the arguments are created. <see langword="null" /> stands for the default behavior.
		   		/// </summary>
		   		/// <remarks>
		   		///     Assign it in a module initializer: {{timing}}
		   		///     so neither a hook nor a thread-static or async-local value set there reliably takes effect.
		   		/// </remarks>
		   		public static global::System.Func<global::Mockolate.MockBehavior?> DefaultBehavior { get; set; } = static () => null;

		   {{members}}
		   	}

		   	/// <summary>
		   	///     Excludes the test parameter from the arguments that <see cref="AutoMockArgumentsAttribute" /> shares with the
		   	///     classes under test, like a transient service in dependency injection.
		   	/// </summary>
		   	[global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal sealed class TransientAttribute : global::Mockolate.Integrations.TransientParameterAttribute
		   	{
		   	}
		   }
		   #nullable disable
		   #endif

		   """).ToString();
}
