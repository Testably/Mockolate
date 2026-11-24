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
	///     The types are embedded, so that test projects which see each other's internals do not get conflicting types.
	/// </remarks>
	public static string AutoMockArgumentsShared()
		=> InitializeBuilder().Append("""
		   #if MOCKOLATE_TEST_FRAMEWORK
		   #nullable enable
		   namespace Mockolate.Integrations
		   {
		   	/// <summary>
		   	///     Excludes a test parameter from the arguments that are shared with the classes under test.
		   	/// </summary>
		   	[global::System.AttributeUsage(global::System.AttributeTargets.Parameter)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal abstract class TransientParameterAttribute : global::System.Attribute
		   	{
		   	}

		   	/// <summary>
		   	///     Creates the arguments of one test invocation on demand, so that a class under test receives the arguments of
		   	///     the other parameters whatever their order.
		   	/// </summary>
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
		   		///     constructor parameter only decides between several of them. A parameter that is being created is no
		   		///     candidate, so that two classes can depend on each other's type.
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
		=> InitializeBuilder().Append("""
		   #if MOCKOLATE_TUNIT
		   #nullable enable
		   namespace Mockolate.Integrations.TUnit
		   {
		   	/// <summary>
		   	///     Passes the <see cref="Arguments" /> to the first parameters of the test method, like TUnit's <c>Arguments</c>,
		   	///     and fills every remaining parameter: an interface, delegate or abstract class gets a mock, a class gets a mock
		   	///     that forwards to its real implementation, and a sealed class or record a real instance. Their constructor
		   	///     dependencies are mocks, and other values come from the default values of the behavior.
		   	/// </summary>
		   	/// <remarks>
		   	///     A constructor dependency of a class under test gets the argument of the test parameter of the same type, so the
		   	///     test can set up and verify the mock that the class under test uses. When several test parameters have the type,
		   	///     the one with the name of the constructor parameter (ignoring case) is used. The given arguments and a
		   	///     <see cref="TransientAttribute" /> parameter are not shared. Each attribute on a method is one test case.
		   	/// </remarks>
		   	[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
		   	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		   	[global::Microsoft.CodeAnalysis.Embedded]
		   	internal sealed class AutoMockArgumentsAttribute
		   		: global::TUnit.Core.UntypedDataSourceGeneratorAttribute, global::Mockolate.Internals.IMockDataSource
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
		   		///     Assign it in a module initializer: TUnit creates the arguments before it runs any hook, and on another thread,
		   		///     so neither a hook nor a thread-static or async-local value set there takes effect.
		   		/// </remarks>
		   		public static global::System.Func<global::Mockolate.MockBehavior?> DefaultBehavior { get; set; } = static () => null;

		   		/// <inheritdoc />
		   		protected override global::System.Collections.Generic.IEnumerable<global::System.Func<object?[]?>> GenerateDataSources(
		   			global::TUnit.Core.DataGeneratorMetadata dataGeneratorMetadata)
		   		{
		   			// The element type differs between TUnit versions, and the attribute only applies to method parameters.
		   			object[] members = dataGeneratorMetadata.MembersToGenerate;
		   			global::TUnit.Core.ParameterMetadata[] parameters = global::System.Array.ConvertAll(members,
		   				member => (global::TUnit.Core.ParameterMetadata)member);
		   			global::System.Type[] types = global::System.Array.ConvertAll(parameters, parameter => parameter.Type);
		   			string[] names = global::System.Array.ConvertAll(parameters, parameter => parameter.Name);
		   			// The attribute is generated into every test assembly, next to the code that creates the parameters.
		   			global::System.Reflection.Assembly assembly = typeof(AutoMockArgumentsAttribute).Assembly;
		   			// Only a parameter that could be shared needs its reflection info.
		   			yield return () => new global::Mockolate.Integrations.AutoMockRow(assembly, types, names,
		   				index => global::System.Reflection.CustomAttributeExtensions.IsDefined(parameters[index].ReflectionInfo,
		   					typeof(global::Mockolate.Integrations.TransientParameterAttribute), false),
		   				Arguments, DefaultBehavior?.Invoke()).Create();
		   		}
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
