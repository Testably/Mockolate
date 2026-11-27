using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	/// <remarks>
	///     The <paramref name="autoMockableTypes" /> are <see langword="null" /> unless the compilation sets the
	///     <c>MockolateAutoMocking</c> build property.
	/// </remarks>
	public static string MockBehaviorExtensions(bool includeHttpClient, bool includeAsyncEnumerable, EquatableArray<string>? autoMockableTypes)
	{
		StringBuilder sb = InitializeBuilder();

		sb.Append("""
		          namespace Mockolate;

		          #nullable enable annotations

		          /// <summary>
		          ///     Extensions for <see cref="global::Mockolate.MockBehavior" />.
		          /// </summary>
		          internal static partial class Mock
		          {
		          	private static readonly global::Mockolate.MockBehavior _default;
		          	
		          	static Mock()
		          	{
		          """).AppendLine();
		// With auto-mocking, the default behavior carries the factory, so that CreateMock does not copy it for every mock.
		sb.Append(autoMockableTypes is not null
			? "\t\t_default = global::Mockolate.Internals.AutoMocks.UseFactory(new global::Mockolate.MockBehavior(new DefaultValueGenerator()), CreateAutoMock);"
			: "\t\t_default = new global::Mockolate.MockBehavior(new DefaultValueGenerator());").AppendLine();
		sb.Append("""
		          	}
		          	
		          	extension(global::Mockolate.MockBehavior)
		          	{
		          		/// <summary>
		          		///     The default <see cref="global::Mockolate.MockBehavior" /> - the starting point for configuring a mock.
		          		/// </summary>
		          		/// <remarks>
		          		///     Un-configured members return the generator-provided default value (empty strings/collections, completed
		          		///     <see cref="global::System.Threading.Tasks.Task" />s, an identity <see cref="global::System.Func{T, TResult}" />
		          		///     for <c>Func&lt;T, T&gt;</c>, a no-op <see cref="global::System.Action{T}" />, <see langword="null" /> otherwise), base-class
		          		///     implementations run for class mocks, and every invocation is recorded for later verification.
		          		///     <para />
		          		///     Chain <c>SkippingBaseClass()</c>, <c>ThrowingWhenNotSetup()</c>, <c>SkippingInteractionRecording()</c>,
		          		///     <c>WithDefaultValueFor&lt;T&gt;(...)</c> or <c>UseConstructorParametersFor&lt;T&gt;(...)</c> to derive
		          		///     a customized <see cref="global::Mockolate.MockBehavior" />; because it is a <see langword="record" />,
		          		///     each call returns a new instance and this shared default stays unchanged.
		          		/// </remarks>
		          		public static global::Mockolate.MockBehavior Default => _default;
		          	}
		          """).AppendLine();
		sb.Append('\t').AppendLine();
		if (autoMockableTypes is not null)
		{
			sb.Append("\t/// <summary>").AppendLine();
			sb.Append("\t///     Creates an auto-mock of the <paramref name=\"type\" /> with the <paramref name=\"behavior\" />, or returns").AppendLine();
			sb.Append("\t///     <see langword=\"null\" /> when this compilation generates no mock of the <paramref name=\"type\" />.").AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\tinternal static object? CreateAutoMock(global::System.Type type, global::Mockolate.MockBehavior behavior)").AppendLine();
			sb.Append("\t{").AppendLine();
			foreach (string autoMockableType in autoMockableTypes.Value)
			{
				sb.Append("\t\tif (type == typeof(").Append(autoMockableType).Append("))").AppendLine();
				sb.Append("\t\t{").AppendLine();
				sb.Append("\t\t\treturn ").Append(autoMockableType).Append(".CreateMock(behavior);").AppendLine();
				sb.Append("\t\t}").AppendLine();
				sb.AppendLine();
			}

			sb.Append("\t\treturn null;").AppendLine();
			sb.Append("\t}").AppendLine();
			sb.Append('\t').AppendLine();
		}

		sb.Append("""
		          	/// <summary>
		          	///     Defines a factory for creating default values for a specified type.
		          	/// </summary>
		          	public interface IDefaultValueFactory
		          	{
		          		/// <summary>
		          		///     Determines whether the specified <paramref name="type" /> can be created by this factory.
		          		/// </summary>
		          		bool IsMatch(global::System.Type type);
		          	
		          		/// <summary>
		          		///     Creates a new instance of the specified type.
		          		/// </summary>
		          		object? Create(global::System.Type type, IDefaultValueGenerator defaultValueGenerator, object?[] parameters);
		          	}

		          	/// <summary>
		          	///     Marks a <see cref="IDefaultValueFactory" /> that creates the same value whatever the parameters are.
		          	/// </summary>
		          	internal interface IDefaultValueFactoryWithoutParameters;

		          	/// <summary>
		          	///     A <see cref="IDefaultValueFactory" /> that returns a specified <paramref name="value" /> for the given type
		          	///     parameter <typeparamref name="T" />.
		          	/// </summary>
		          	[global::System.Diagnostics.DebuggerNonUserCode]
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal class TypedDefaultValueFactory<T>(T value) : IDefaultValueFactory, IDefaultValueFactoryWithoutParameters
		          	{
		          		/// <inheritdoc cref="IDefaultValueFactory.IsMatch(global::System.Type)" />
		          		public bool IsMatch(global::System.Type type)
		          			=> type == typeof(T);
		          	
		          		/// <inheritdoc cref="IDefaultValueFactory.Create(global::System.Type, IDefaultValueGenerator, object[])" />
		          		public object? Create(global::System.Type type, IDefaultValueGenerator defaultValueGenerator, params object?[] parameters)
		          			=> value;
		          	}
		          """).AppendLine();
		if (includeHttpClient)
		{
			sb.Append("""
			          	/// <summary>
			          	///     A <see cref="IDefaultValueFactory" /> that returns an empty <see cref="global::System.Net.Http.HttpResponseMessage" /> with the specified
			          	///     <paramref name="statusCode" />.
			          	/// </summary>
			          	[global::System.Diagnostics.DebuggerNonUserCode]
			          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
			          	private sealed class HttpResponseMessageFactory(global::System.Net.HttpStatusCode statusCode) : IDefaultValueFactory, IDefaultValueFactoryWithoutParameters
			          	{
			          		/// <inheritdoc cref="IDefaultValueFactory.IsMatch(global::System.Type)" />
			          		public bool IsMatch(global::System.Type type)
			          			=> type == typeof(global::System.Net.Http.HttpResponseMessage);
			          	
			          		/// <inheritdoc cref="IDefaultValueFactory.Create(global::System.Type, IDefaultValueGenerator, object[])" />
			          		public object? Create(global::System.Type type, IDefaultValueGenerator defaultValueGenerator, params object?[] parameters)
			          			=> new global::System.Net.Http.HttpResponseMessage(statusCode) { Content = new global::System.Net.Http.StringContent(string.Empty) };
			          	}
			          """).AppendLine();
		}

		sb.Append("""
		          	
		          	/// <summary>
		          	///     Provides default values for common types used in mocking scenarios.
		          	/// </summary>
		          	[global::System.Diagnostics.DebuggerNonUserCode]
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	private class DefaultValueGenerator : IDefaultValueGenerator, global::Mockolate.Internals.IDefaultValueProbe
		          	{
		          		private static readonly IDefaultValueFactory[] _builtInFactories = [
		          			new TypedDefaultValueFactory<string>(""),
		          """).AppendLine();
		if (includeHttpClient)
		{
			sb.Append("""
			          			new HttpResponseMessageFactory(global::System.Net.HttpStatusCode.NotImplemented),
			          """).AppendLine();
		}

		sb.Append("""
		          			new CancellableTaskFactory(),
		          	#if NET8_0_OR_GREATER
		          			new CancellableValueTaskFactory(),
		          	#endif
		          			new TypedDefaultValueFactory<global::System.Threading.CancellationToken>(global::System.Threading.CancellationToken.None),
		          			new TypedDefaultValueFactory<System.Collections.IEnumerable>(global::System.Array.Empty<object?>()),
		          		];

		          		private static IDefaultValueFactory[] _factories = _builtInFactories;

		          		/// <inheritdoc cref="IDefaultValueGenerator.GenerateValue(global::System.Type, object?[])" />
		          		public object? GenerateValue(global::System.Type type, params object?[] parameters)
		          		{
		          			if (TryGenerate(type, parameters, out object? value))
		          			{
		          				return value;
		          			}
		          	
		          			return null;
		          		}
		          	
		          		/// <summary>
		          		///     Registers a <paramref name="defaultValueFactory" /> to provide default values for a specific type.
		          		/// </summary>
		          		public static void Register(IDefaultValueFactory defaultValueFactory)
		          		{
		          			IDefaultValueFactory[] current;
		          			IDefaultValueFactory[] updated;
		          			do
		          			{
		          				current = global::System.Threading.Volatile.Read(ref _factories);
		          				updated = new IDefaultValueFactory[current.Length + 1];
		          				global::System.Array.Copy(current, updated, current.Length);
		          				updated[current.Length] = defaultValueFactory;
		          			}
		          			while (global::System.Threading.Interlocked.CompareExchange(ref _factories, updated, current) != current);
		          		}

		          		/// <inheritdoc cref="global::Mockolate.Internals.IDefaultValueProbe.Probe(global::System.Type)" />
		          		global::Mockolate.Internals.DefaultValueSource global::Mockolate.Internals.IDefaultValueProbe.Probe(global::System.Type type)
		          		{
		          			if (GetType() != typeof(DefaultValueGenerator))
		          			{
		          				return global::Mockolate.Internals.DefaultValueSource.WithArguments;
		          			}

		          			IDefaultValueFactory[] factories = global::System.Threading.Volatile.Read(ref _factories);
		          			if (ReferenceEquals(factories, _builtInFactories))
		          			{
		          				return ProbeBuiltIn(type);
		          			}

		          			foreach (IDefaultValueFactory factory in factories)
		          			{
		          				if (factory.IsMatch(type))
		          				{
		          					return factory is IDefaultValueFactoryWithoutParameters
		          						? global::Mockolate.Internals.DefaultValueSource.WithoutArguments
		          						: global::Mockolate.Internals.DefaultValueSource.WithArguments;
		          				}
		          			}

		          			return global::Mockolate.Internals.DefaultValueSource.None;
		          		}

		          		/// <summary>
		          		///     Answers for the built-in factories without calling each of them.
		          		/// </summary>
		          		private static global::Mockolate.Internals.DefaultValueSource ProbeBuiltIn(global::System.Type type)
		          		{
		          			if (type == typeof(string) ||
		          """).AppendLine();
		if (includeHttpClient)
		{
			sb.Append("""
			          			    type == typeof(global::System.Net.Http.HttpResponseMessage) ||
			          """).AppendLine();
		}

		sb.Append("""
		          			    type == typeof(global::System.Threading.CancellationToken) ||
		          			    type == typeof(global::System.Collections.IEnumerable))
		          			{
		          				return global::Mockolate.Internals.DefaultValueSource.WithoutArguments;
		          			}

		          	#if NET8_0_OR_GREATER
		          			if (type == typeof(global::System.Threading.Tasks.ValueTask))
		          			{
		          				return global::Mockolate.Internals.DefaultValueSource.CancellationToken;
		          			}

		          	#endif
		          			return type == typeof(global::System.Threading.Tasks.Task)
		          				? global::Mockolate.Internals.DefaultValueSource.CancellationToken
		          				: global::Mockolate.Internals.DefaultValueSource.None;
		          		}

		          		/// <summary>
		          		///     Tries to generate a default value for the specified type.
		          		/// </summary>
		          		protected virtual bool TryGenerate(global::System.Type type, object?[] parameters, out object? value)
		          		{
		          			foreach (IDefaultValueFactory factory in global::System.Threading.Volatile.Read(ref _factories))
		          			{
		          				if (factory.IsMatch(type))
		          				{
		          					value = factory.Create(type, this, parameters);
		          					return true;
		          				}
		          			}

		          			value = null;
		          			return false;
		          		}
		          	
		          		private static bool HasCanceledCancellationToken(object?[] parameters, out global::System.Threading.CancellationToken cancellationToken)
		          		{
		          			foreach (object? parameter in parameters)
		          			{
		          				if (parameter is global::System.Threading.CancellationToken token)
		          				{
		          					cancellationToken = token;
		          					return token.IsCancellationRequested;
		          				}
		          			}

		          			cancellationToken = global::System.Threading.CancellationToken.None;
		          			return false;
		          		}

		          		[global::System.Diagnostics.DebuggerNonUserCode]
		          		[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          		private sealed class CancellableTaskFactory : IDefaultValueFactory
		          		{
		          			/// <inheritdoc cref="IDefaultValueFactory.IsMatch(global::System.Type)" />
		          			public bool IsMatch(global::System.Type type)
		          				=> type == typeof(global::System.Threading.Tasks.Task);
		          	
		          			/// <inheritdoc cref="IDefaultValueFactory.Create(global::System.Type, IDefaultValueGenerator, object[])" />
		          			public object Create(global::System.Type type, IDefaultValueGenerator defaultValueGenerator, params object?[] parameters)
		          			{
		          				if (HasCanceledCancellationToken(parameters, out global::System.Threading.CancellationToken cancellationToken))
		          				{
		          					return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken);
		          				}
		          	
		          				return global::System.Threading.Tasks.Task.CompletedTask;
		          			}
		          		}
		          	#if NET8_0_OR_GREATER
		          		[global::System.Diagnostics.DebuggerNonUserCode]
		          		[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          		private sealed class CancellableValueTaskFactory : IDefaultValueFactory
		          		{
		          			/// <inheritdoc cref="IDefaultValueFactory.IsMatch(global::System.Type)" />
		          			public bool IsMatch(global::System.Type type)
		          				=> type == typeof(global::System.Threading.Tasks.ValueTask);
		          	
		          			/// <inheritdoc cref="IDefaultValueFactory.Create(global::System.Type, IDefaultValueGenerator, object[])" />
		          			public object Create(global::System.Type type, IDefaultValueGenerator defaultValueGenerator, params object?[] parameters)
		          			{
		          				if (HasCanceledCancellationToken(parameters, out global::System.Threading.CancellationToken cancellationToken))
		          				{
		          					return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken);
		          				}
		          	
		          				return global::System.Threading.Tasks.ValueTask.CompletedTask;
		          			}
		          		}
		          	#endif
		          	}
		          }

		          /// <summary>
		          ///     Extensions on <see cref="IDefaultValueGenerator" />
		          /// </summary>
		          [global::System.Diagnostics.DebuggerNonUserCode]
		          [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          internal static class DefaultValueGeneratorExtensions
		          {
		          	/// <summary>
		          	///     Adds a generic <c>Generate</c> method for specific types.
		          	/// </summary>
		          	extension(IDefaultValueGenerator generator)
		          	{
		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.Task" /> of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value, params object?[] parameters)
		          		{
		          			foreach (object? parameter in parameters)
		          			{
		          				if (parameter is global::System.Threading.CancellationToken cancellationToken)
		          				{
		          					return generator.Generate(nullValue, value, cancellationToken);
		          				}
		          			}

		          			return global::System.Threading.Tasks.Task.FromResult(value);
		          		}

		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.Task" /> of <typeparamref name="T" /> for a
		          		///     method without a parameter that can be a <see cref="global::System.Threading.CancellationToken" />.
		          		/// </summary>
		          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value)
		          			=> global::System.Threading.Tasks.Task.FromResult(value);

		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.Task" /> of <typeparamref name="T" />, which
		          		///     is canceled when the <paramref name="cancellationToken" /> is.
		          		/// </summary>
		          		public global::System.Threading.Tasks.Task<T> Generate<T>(global::System.Threading.Tasks.Task<T> nullValue, T value, global::System.Threading.CancellationToken cancellationToken)
		          			=> cancellationToken.IsCancellationRequested
		          				? global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken)
		          				: global::System.Threading.Tasks.Task.FromResult(value);

		          #if NET8_0_OR_GREATER
		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.ValueTask" /> of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value, params object?[] parameters)
		          		{
		          			foreach (object? parameter in parameters)
		          			{
		          				if (parameter is global::System.Threading.CancellationToken cancellationToken)
		          				{
		          					return generator.Generate(nullValue, value, cancellationToken);
		          				}
		          			}

		          			return global::System.Threading.Tasks.ValueTask.FromResult(value);
		          		}

		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.ValueTask" /> of <typeparamref name="T" /> for a
		          		///     method without a parameter that can be a <see cref="global::System.Threading.CancellationToken" />.
		          		/// </summary>
		          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value)
		          			=> global::System.Threading.Tasks.ValueTask.FromResult(value);

		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.ValueTask" /> of <typeparamref name="T" />, which
		          		///     is canceled when the <paramref name="cancellationToken" /> is.
		          		/// </summary>
		          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value, global::System.Threading.CancellationToken cancellationToken)
		          			=> cancellationToken.IsCancellationRequested
		          				? global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken)
		          				: global::System.Threading.Tasks.ValueTask.FromResult(value);
		          #endif

		          		/// <summary>
		          		///     Generates an empty enumerable of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Collections.Generic.IEnumerable<T> Generate<T>(global::System.Collections.Generic.IEnumerable<T> nullValue, params object?[] parameters)
		          			=> global::System.Array.Empty<T>();
		          		
		          		/// <summary>
		          		///     Generates an empty enumerable of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Collections.Generic.List<T> Generate<T>(global::System.Collections.Generic.List<T> nullValue, params object?[] parameters)
		          			=> new global::System.Collections.Generic.List<T>();
		          		
		          		/// <summary>
		          		///     Generates an empty array of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public T[] Generate<T>(T[] nullValue, params object?[] parameters)
		          			=> global::System.Array.Empty<T>();
		          		
		          		/// <summary>
		          		///     Generates an empty two-dimensional array of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public T[,] Generate<T>(T[,] nullValue, params object?[] parameters)
		          			=> new T[,] { };
		          				
		          		/// <summary>
		          		///     Generates an empty three-dimensional array of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public T[,,] Generate<T>(T[,,] nullValue, params object?[] parameters)
		          			=> new T[,,] { };
		          				
		          		/// <summary>
		          		///     Generates an empty four-dimensional array of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public T[,,,] Generate<T>(T[,,,] nullValue, params object?[] parameters)
		          			=> new T[,,,] { };

		          		/// <summary>
		          		///     Generates a function that returns its argument, such as an options callback, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Func<T, T> Generate<T>(global::System.Func<T, T> nullValue, params object?[] parameters)
		          		{
		          			if (generator.GenerateValue(typeof(global::System.Func<T, T>), parameters) is global::System.Func<T, T> value)
		          			{
		          				return value;
		          			}

		          			return static argument => argument;
		          		}

		          		/// <summary>
		          		///     Generates an action that does nothing, such as a configure callback, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Action<T> Generate<T>(global::System.Action<T> nullValue, params object?[] parameters)
		          		{
		          			if (generator.GenerateValue(typeof(global::System.Action<T>), parameters) is global::System.Action<T> value)
		          			{
		          				return value;
		          			}

		          			return static _ => { };
		          		}

		          """).AppendLine();
		if (includeAsyncEnumerable)
		{
			sb.Append("""
			          		/// <summary>
			          		///     Generates an empty async enumerable of <typeparamref name="T" />, with
			          		///     the <paramref name="parameters" /> for context.
			          		/// </summary>
			          		/// <remarks>
			          		///     Its enumeration is canceled when the first <see cref="global::System.Threading.CancellationToken" />
			          		///     in the <paramref name="parameters" /> is.
			          		/// </remarks>
			          		public global::System.Collections.Generic.IAsyncEnumerable<T> Generate<T>(global::System.Collections.Generic.IAsyncEnumerable<T> nullValue, params object?[] parameters)
			          		{
			          			if (generator.GenerateValue(typeof(global::System.Collections.Generic.IAsyncEnumerable<T>), parameters) is global::System.Collections.Generic.IAsyncEnumerable<T> value)
			          			{
			          				return value;
			          			}

			          			foreach (object? parameter in parameters)
			          			{
			          				if (parameter is global::System.Threading.CancellationToken cancellationToken)
			          				{
			          					return cancellationToken.CanBeCanceled
			          						? new EmptyAsyncEnumerable<T>(cancellationToken)
			          						: EmptyAsyncEnumerable<T>.Instance;
			          				}
			          			}

			          			return EmptyAsyncEnumerable<T>.Instance;
			          		}

			          """).AppendLine();
		}

		sb.Append("""
		          		/// <summary>
		          		///     Generates a default value of type <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public T Generate<T>(T nullValue, params object?[] parameters)
		          		{
		          			if (generator.GenerateValue(typeof(T), parameters) is T value)
		          			{
		          				return value;
		          			}

		          			return nullValue;
		          		}

		          		/// <summary>
		          		///     Generates a default value of type <typeparamref name="T" />, with the <paramref name="parameters" />
		          		///     for context, or returns <see langword="null" /> when the generator has no value for it.
		          		/// </summary>
		          		public T? GenerateOrNull<T>(params object?[] parameters)
		          			where T : struct
		          			=> generator.GenerateValue(typeof(T), parameters) is T value ? value : null;

		          		/// <summary>
		          		///     Tells how the generator produces the default value of type <typeparamref name="T" />, so that the
		          		///     invocation parameters are only collected when they are needed.
		          		/// </summary>
		          		public global::Mockolate.Internals.DefaultValueSource Probe<T>()
		          			=> generator is global::Mockolate.Internals.IDefaultValueProbe probe
		          				? probe.Probe(typeof(T))
		          				: global::Mockolate.Internals.DefaultValueSource.WithArguments;
		          	}
		          """).AppendLine();
		if (includeAsyncEnumerable)
		{
			sb.Append("""

			          	[global::System.Diagnostics.DebuggerNonUserCode]
			          	private sealed class EmptyAsyncEnumerable<T>(
			          		global::System.Threading.CancellationToken cancellationToken,
			          		global::System.Threading.CancellationToken enumeratorToken = default)
			          		: global::System.Collections.Generic.IAsyncEnumerable<T>, global::System.Collections.Generic.IAsyncEnumerator<T>
			          	{
			          		public static readonly EmptyAsyncEnumerable<T> Instance = new(global::System.Threading.CancellationToken.None);

			          		public T Current => default!;

			          		public global::System.Collections.Generic.IAsyncEnumerator<T> GetAsyncEnumerator(global::System.Threading.CancellationToken enumeratorCancellationToken = default)
			          			=> enumeratorCancellationToken.CanBeCanceled && enumeratorCancellationToken != cancellationToken
			          				? new EmptyAsyncEnumerable<T>(cancellationToken, enumeratorCancellationToken)
			          				: this;

			          		public global::System.Threading.Tasks.ValueTask<bool> MoveNextAsync()
			          		{
			          			if (cancellationToken.IsCancellationRequested)
			          			{
			          				return new global::System.Threading.Tasks.ValueTask<bool>(global::System.Threading.Tasks.Task.FromCanceled<bool>(cancellationToken));
			          			}

			          			if (enumeratorToken.IsCancellationRequested)
			          			{
			          				return new global::System.Threading.Tasks.ValueTask<bool>(global::System.Threading.Tasks.Task.FromCanceled<bool>(enumeratorToken));
			          			}

			          			return default;
			          		}

			          		public global::System.Threading.Tasks.ValueTask DisposeAsync()
			          			=> default;
			          	}
			          """).AppendLine();
		}

		sb.Append("""
		          }

		          #nullable disable
		          """);
		return sb.ToString();
	}
}
