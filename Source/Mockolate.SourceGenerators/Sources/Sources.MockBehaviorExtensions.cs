using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	public static string MockBehaviorExtensions(bool includeHttpClient)
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
		          		_default = new global::Mockolate.MockBehavior(new DefaultValueGenerator());
		          	}
		          	
		          	extension(global::Mockolate.MockBehavior)
		          	{
		          		/// <summary>
		          		///     The default <see cref="global::Mockolate.MockBehavior" /> - the starting point for configuring a mock.
		          		/// </summary>
		          		/// <remarks>
		          		///     Un-configured members return the generator-provided default value (empty strings/collections, completed
		          		///     <see cref="global::System.Threading.Tasks.Task" />s, <see langword="null" /> otherwise), base-class
		          		///     implementations run for class mocks, and every invocation is recorded for later verification.
		          		///     <para />
		          		///     Chain <c>SkippingBaseClass()</c>, <c>ThrowingWhenNotSetup()</c>, <c>SkippingInteractionRecording()</c>,
		          		///     <c>WithDefaultValueFor&lt;T&gt;(...)</c> or <c>UseConstructorParametersFor&lt;T&gt;(...)</c> to derive
		          		///     a customized <see cref="global::Mockolate.MockBehavior" />; because it is a <see langword="record" />,
		          		///     each call returns a new instance and this shared default stays unchanged.
		          		/// </remarks>
		          		public static global::Mockolate.MockBehavior Default => _default;
		          	}
		          	
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
		          		private static IDefaultValueFactory[] _factories = [
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

		          			foreach (IDefaultValueFactory factory in global::System.Threading.Volatile.Read(ref _factories))
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
		          			global::System.Threading.CancellationToken parameter = global::System.Linq.Enumerable.FirstOrDefault(global::System.Linq.Enumerable.OfType<global::System.Threading.CancellationToken>(parameters));
		          			if (parameter.IsCancellationRequested)
		          			{
		          				cancellationToken = parameter;
		          				return true;
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
		          			global::System.Threading.CancellationToken cancellationToken = global::System.Linq.Enumerable.FirstOrDefault(
		          				global::System.Linq.Enumerable.OfType<global::System.Threading.CancellationToken?>(parameters)) ?? global::System.Threading.CancellationToken.None;
		          			if (cancellationToken.IsCancellationRequested)
		          			{
		          				return global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
		          			}

		          			return global::System.Threading.Tasks.Task.FromResult(value);
		          		}

		          #if NET8_0_OR_GREATER
		          		/// <summary>
		          		///     Generates a <see cref="global::System.Threading.Tasks.ValueTask" /> of <typeparamref name="T" />, with
		          		///     the <paramref name="parameters" /> for context.
		          		/// </summary>
		          		public global::System.Threading.Tasks.ValueTask<T> Generate<T>(global::System.Threading.Tasks.ValueTask<T> nullValue, T value, params object?[] parameters)
		          		{
		          			global::System.Threading.CancellationToken cancellationToken = global::System.Linq.Enumerable.FirstOrDefault(
		          				global::System.Linq.Enumerable.OfType<global::System.Threading.CancellationToken?>(parameters)) ?? global::System.Threading.CancellationToken.None;
		          			if (cancellationToken.IsCancellationRequested)
		          			{
		          				return global::System.Threading.Tasks.ValueTask.FromCanceled<T>(cancellationToken);
		          			}

		          			return global::System.Threading.Tasks.ValueTask.FromResult(value);
		          		}
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
		          		///     Tells how the generator produces the default value of type <typeparamref name="T" />, so that the
		          		///     invocation parameters are only collected when they are needed.
		          		/// </summary>
		          		public global::Mockolate.Internals.DefaultValueSource Probe<T>()
		          			=> generator is global::Mockolate.Internals.IDefaultValueProbe probe
		          				? probe.Probe(typeof(T))
		          				: global::Mockolate.Internals.DefaultValueSource.WithArguments;
		          	}
		          }

		          #nullable disable
		          """);
		return sb.ToString();
	}
}
