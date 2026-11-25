namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
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
	                                              				index => global::System.Reflection.CustomAttributeExtensions.IsDefined(parameters[index],
	                                              					typeof(global::Mockolate.Integrations.AsDefaultParameterAttribute), false),
	                                              				arguments, DefaultBehavior?.Invoke()).Create();


	                                              """;

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
		                              	///     Makes a test parameter the default value of its type for all mocks of the test case.
		                              	/// </summary>
		                              	[global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
		                              	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		                              	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		                              	[global::Microsoft.CodeAnalysis.Embedded]
		                              	internal abstract class AsDefaultParameterAttribute : global::System.Attribute
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
		                              		private readonly global::System.Func<int, bool> _isAsDefaultParameter;
		                              		private readonly object?[] _arguments;
		                              		private global::Mockolate.MockBehavior? _behavior;
		                              		private readonly int[] _states;
		                              		private readonly object?[] _values;
		                              		private readonly bool?[] _isTransient;

		                              		/// <param name="assembly">The test assembly, whose generated code creates the parameters.</param>
		                              		/// <param name="types">The types of the parameters of the test method.</param>
		                              		/// <param name="names">The names of the parameters of the test method.</param>
		                              		/// <param name="isTransientParameter">Whether the parameter at an index is marked as transient.</param>
		                              		/// <param name="isAsDefaultParameter">Whether the parameter at an index is marked as the default value of its type.</param>
		                              		/// <param name="arguments">The given arguments of the first parameters, which are not shared.</param>
		                              		/// <param name="behavior">The behavior of the created mocks and values.</param>
		                              		public AutoMockRow(global::System.Reflection.Assembly assembly, global::System.Type[] types, string[] names,
		                              			global::System.Func<int, bool> isTransientParameter, global::System.Func<int, bool> isAsDefaultParameter,
		                              			object?[] arguments, global::Mockolate.MockBehavior? behavior)
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
		                              			_isAsDefaultParameter = isAsDefaultParameter;
		                              			_arguments = arguments;
		                              			_behavior = behavior;
		                              			_states = new int[types.Length];
		                              			_values = new object?[types.Length];
		                              			_isTransient = new bool?[types.Length];
		                              		}

		                              #if MOCKOLATE_NUNIT || MOCKOLATE_MSTEST
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
		                              				case float number when float.IsNaN(number) || float.IsInfinity(number):
		                              					return float.IsNaN(number) ? "float.NaN" : number > 0 ? "float.PositiveInfinity" : "float.NegativeInfinity";
		                              				case float number:
		                              					return number.ToString("R", culture) + "F";
		                              				case double number when double.IsNaN(number) || double.IsInfinity(number):
		                              					return double.IsNaN(number) ? "double.NaN" : number > 0 ? "double.PositiveInfinity" : "double.NegativeInfinity";
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
		                              				case global::System.Enum enumValue:
		                              					return $"{enumValue.GetType().Name}.{enumValue}";
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
		                              #endif

		                              		public object?[] Create()
		                              		{
		                              			for (int index = 0; index < _arguments.Length; index++)
		                              			{
		                              				_values[index] = _arguments[index];
		                              				_states[index] = 2;
		                              			}

		                              			for (int index = 0; index < _types.Length; index++)
		                              			{
		                              				if (_isAsDefaultParameter(index))
		                              				{
		                              					UseAsDefault(index);
		                              				}
		                              			}

		                              			for (int index = 0; index < _types.Length; index++)
		                              			{
		                              				GetOrCreate(index);
		                              			}

		                              			return _values;
		                              		}

		                              		/// <summary>
		                              		///     The parameter is created on the first request. While it is being created, the previous default values
		                              		///     apply, so that its own creation does not recurse.
		                              		/// </summary>
		                              		private void UseAsDefault(int index)
		                              		{
		                              			global::System.Type type = _types[index];
		                              			global::Mockolate.MockBehavior previous = _behavior ?? global::Mockolate.MockBehavior.Default;
		                              			_behavior = previous.WithDefaultValueFor(new global::Mockolate.DefaultValueFactory(
		                              				candidate => candidate == type,
		                              				(_, parameters) => _states[index] == 1
		                              					? previous.DefaultValue.GenerateValue(type, parameters)
		                              					: GetOrCreate(index)));
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
		                                	///     parameter are not shared. An <see cref="AsDefaultAttribute" /> parameter is the default value of its type for
		                                	///     all mocks. Each attribute on a method is one test case.
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

		                                	/// <summary>
		                                	///     Makes the test parameter the default value of its exact type for all mocks that
		                                	///     <see cref="AutoMockArgumentsAttribute" /> creates for the test case, so that their members without a setup
		                                	///     return it, also wrapped in a task.
		                                	/// </summary>
		                                	[global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
		                                	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		                                	[global::Microsoft.CodeAnalysis.Embedded]
		                                	internal sealed class AsDefaultAttribute : global::Mockolate.Integrations.AsDefaultParameterAttribute
		                                	{
		                                	}
		                                }
		                                #nullable disable
		                                #endif

		                                """).ToString();
}
