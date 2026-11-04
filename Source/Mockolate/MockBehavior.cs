using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Mockolate;

/// <summary>
///     Configures the runtime behavior of a Mockolate mock: default return values for un-configured members,
///     base-class delegation, interaction recording, pre-registered constructor parameters, and whether
///     un-configured invocations throw.
/// </summary>
/// <remarks>
///     An instance is an immutable <see langword="record" /> - every configuration method returns a modified copy,
///     leaving the original untouched so behaviors can be shared safely across tests. Pass a <see cref="MockBehavior" />
///     to <c>CreateMock(...)</c> to customize how un-configured members are handled:
///     <list type="bullet">
///         <item>
///             <description><see cref="ThrowWhenNotSetup" /> (see <see cref="ThrowingWhenNotSetup" />) - throw <see cref="Mockolate.Exceptions.MockNotSetupException" /> instead of returning a default value.</description>
///         </item>
///         <item>
///             <description><see cref="SkipBaseClass" /> (see <see cref="SkippingBaseClass" />) - for class mocks, bypass the base-class implementation entirely.</description>
///         </item>
///         <item>
///             <description><see cref="SkipInteractionRecording" /> (see <see cref="SkippingInteractionRecording" />) - trade verifiability for performance by not recording invocations.</description>
///         </item>
///         <item>
///             <description><see cref="DefaultValue" /> (tune via <see cref="WithDefaultValueFor{T}(Func{T})" />, <see cref="WithDefaultValueFor(DefaultValueFactory[])" /> or <see cref="WithDefaultValueGenerator" />) - supplies the default return values for un-configured members.</description>
///         </item>
///         <item>
///             <description><c>UseConstructorParametersFor&lt;T&gt;(...)</c> - pre-register constructor parameters applied whenever a mock of <c>T</c> is created without explicit ones.</description>
///         </item>
///     </list>
///     Start from <c>MockBehavior.Default</c> (generator-emitted) and chain the fluent methods. The flags can also be
///     set with a <see langword="with" /> expression, e.g. <c>MockBehavior.Default with { SkipBaseClass = true }</c>.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public record MockBehavior : IMockBehaviorAccess
{
	private ConcurrentStack<IConstructorParameters>? _constructorParameters;
	private DefaultValueFactory[]? _defaultValueFactories;
	private IDefaultValueGenerator _defaultValueGenerator;
	private ConcurrentStack<object?>? _values;

	/// <summary>
	///     Initializes a new <see cref="MockBehavior" /> with the given <paramref name="defaultValue" /> generator and
	///     all other flags at their defaults.
	/// </summary>
	/// <param name="defaultValue">The generator used to produce default return values for un-configured members.</param>
	public MockBehavior(IDefaultValueGenerator defaultValue)
	{
		_defaultValueGenerator = defaultValue;
		DefaultValue = defaultValue;
	}

	/// <summary>
	///     Specifies whether an exception is thrown when an operation is attempted without prior setup.
	/// </summary>
	/// <remarks>
	///     If set to <see langword="false" />, the value from the <see cref="DefaultValue" /> is used for return
	///     values of methods or properties.
	/// </remarks>
	public bool ThrowWhenNotSetup { get; init; }

	/// <summary>
	///     Flag indicating if the base class implementation should be skipped.
	/// </summary>
	/// <remarks>
	///     If set to <see langword="false" /> (default value), the base class implementation gets called and
	///     its return values are used as default values.
	/// </remarks>
	public bool SkipBaseClass { get; init; }

	/// <summary>
	///     Flag indicating whether interaction recording is skipped for performance.
	/// </summary>
	/// <remarks>
	///     If set to <see langword="false" /> (default value), every interaction with the mock is recorded and
	///     can be verified later.
	///     <para />
	///     When set to <see langword="true" />, the mock skips allocating interaction records, avoids the
	///     interaction-list lock, and does not raise the
	///     <see cref="Mockolate.Interactions.IMockInteractions.InteractionAdded" /> event. Setups, returns, callbacks, and
	///     base-class delegation continue to work normally - only verification is disabled. Any attempt to
	///     verify throws a <see cref="Mockolate.Exceptions.MockException" />.
	/// </remarks>
	public bool SkipInteractionRecording { get; init; }

	/// <summary>
	///     The generator for default values when not specified by a setup.
	/// </summary>
	/// <remarks>
	///     When <see cref="ThrowWhenNotSetup" /> is <see langword="true" />, a
	///     <see cref="Mockolate.Exceptions.MockNotSetupException" /> is thrown instead of consulting this generator.
	///     <para />
	///     The factories registered with <see cref="WithDefaultValueFor{T}(Func{T})" /> or
	///     <see cref="WithDefaultValueFor(DefaultValueFactory[])" /> are consulted first. Replace the underlying
	///     generator with <see cref="WithDefaultValueGenerator" />, which keeps them.
	/// </remarks>
	public IDefaultValueGenerator DefaultValue { get; private init; }

	/// <summary>
	///     Specifies if calling the base class implementation should be skipped.
	/// </summary>
	/// <remarks>
	///     If set to <see langword="false" /> (default value), the base class implementation gets called and
	///     its return values are used as default values.
	///     <para />
	///     Sets the <see cref="SkipBaseClass" /> to <paramref name="skipBaseClass" />.
	/// </remarks>
	public MockBehavior SkippingBaseClass(bool skipBaseClass = true)
		=> this with
		{
			SkipBaseClass = skipBaseClass,
		};

	/// <summary>
	///     Specifies whether an exception is thrown when an operation is attempted without prior setup.
	/// </summary>
	/// <remarks>
	///     Sets the <see cref="ThrowWhenNotSetup" /> to <paramref name="throwWhenNotSetup" />.
	/// </remarks>
	public MockBehavior ThrowingWhenNotSetup(bool throwWhenNotSetup = true)
		=> this with
		{
			ThrowWhenNotSetup = throwWhenNotSetup,
		};

	/// <summary>
	///     Specifies whether interaction recording should be skipped.
	/// </summary>
	/// <remarks>
	///     If set to <see langword="false" /> (default value), every interaction with the mock is recorded
	///     and can be verified later.
	///     <para />
	///     Skipping interaction recording avoids the per-call allocation of an interaction record, the
	///     interaction-list lock, and the <see cref="Mockolate.Interactions.IMockInteractions.InteractionAdded" />
	///     event. Setups, returns, callbacks, and base-class delegation continue to work normally -
	///     only verification is disabled. Any attempt to verify throws a
	///     <see cref="Mockolate.Exceptions.MockException" />.
	///     <para />
	///     Sets the <see cref="SkipInteractionRecording" /> to <paramref name="skipInteractionRecording" />.
	/// </remarks>
	public MockBehavior SkippingInteractionRecording(bool skipInteractionRecording = true)
		=> this with
		{
			SkipInteractionRecording = skipInteractionRecording,
		};

	/// <summary>
	///     Returns a <see cref="MockBehavior" /> that uses <paramref name="factory" /> to produce the default value
	///     whenever a mocked member of type <typeparamref name="T" /> falls back to a default.
	/// </summary>
	/// <typeparam name="T">The target type the factory supplies defaults for. Matched by exact type equality &#8212; subtypes are not covered.</typeparam>
	/// <param name="factory">Lazy producer of the default value; invoked once per fallback.</param>
	/// <returns>A new <see cref="MockBehavior" /> with the factory registered; the original behavior is unchanged.</returns>
	public MockBehavior WithDefaultValueFor<T>(Func<T> factory)
		=> WithDefaultValueFor(new DefaultValueFactory(
			t => t == typeof(T),
			(_, _) => factory()));

	/// <summary>
	///     Replaces the generator that produces default values for un-configured members.
	/// </summary>
	/// <remarks>
	///     Factories registered with <see cref="WithDefaultValueFor{T}(Func{T})" /> or
	///     <see cref="WithDefaultValueFor(DefaultValueFactory[])" /> are kept and still take precedence, regardless of
	///     whether they were registered before or after this call.
	/// </remarks>
	/// <param name="defaultValueGenerator">The generator consulted when no registered factory supports the type.</param>
	/// <returns>A new <see cref="MockBehavior" /> with the replaced generator; the original behavior is unchanged.</returns>
	public MockBehavior WithDefaultValueGenerator(IDefaultValueGenerator defaultValueGenerator)
		=> this with
		{
			_defaultValueGenerator = defaultValueGenerator,
			DefaultValue = _defaultValueFactories is null
				? defaultValueGenerator
				: new DefaultValueGeneratorWithFactories(defaultValueGenerator, _defaultValueFactories),
		};

	MockBehavior IMockBehaviorAccess.Set<T>(T value)
	{
		MockBehavior behavior = this with
		{
			_values = new ConcurrentStack<object?>(_values ?? []),
		};
		behavior._values.Push(value);
		return behavior;
	}

	bool IMockBehaviorAccess.TryGet<T>([NotNullWhen(true)] out T value)
	{
		if (_values?.FirstOrDefault(i => i is T)
		    is not T typedValue)
		{
			value = default!;
			return false;
		}

		value = typedValue;
		return true;
	}

	/// <inheritdoc cref="IMockBehaviorAccess.TryGetConstructorParameters{T}(out object?[])" />
	bool IMockBehaviorAccess.TryGetConstructorParameters<T>([NotNullWhen(true)] out object?[]? parameters)
	{
		if (_constructorParameters?.FirstOrDefault(i => i is ConstructorParameters<T>)
		    is not ConstructorParameters<T> constructorParameters)
		{
			parameters = null;
			return false;
		}

		parameters = constructorParameters.GetParameters();
		return true;
	}

	/// <summary>
	///     Initializes all mocks of type <typeparamref name="T" /> to use the given constructor <paramref name="parameters" />.
	/// </summary>
	/// <remarks>
	///     These parameters are only used when no explicit constructor parameters are provided when creating the mock.
	/// </remarks>
	/// <typeparam name="T">The mocked type whose constructor parameters are being pre-registered.</typeparam>
	/// <param name="parameters">A factory that produces a fresh parameter array on each mock creation.</param>
	/// <returns>A new <see cref="MockBehavior" /> with the pre-registered parameters.</returns>
	public MockBehavior UseConstructorParametersFor<T>(Func<object?[]> parameters)
	{
		MockBehavior behavior = this with
		{
			_constructorParameters = new ConcurrentStack<IConstructorParameters>(_constructorParameters ?? []),
		};
		behavior._constructorParameters.Push(new ConstructorParameters<T>(parameters));
		return behavior;
	}

	/// <summary>
	///     Initializes all mocks of type <typeparamref name="T" /> to use the given constructor <paramref name="parameters" />.
	/// </summary>
	/// <remarks>
	///     These parameters are only used when no explicit constructor parameters are provided when creating the mock.
	/// </remarks>
	/// <typeparam name="T">The mocked type whose constructor parameters are being pre-registered.</typeparam>
	/// <param name="parameters">The parameter array shared across every mock creation.</param>
	/// <returns>A new <see cref="MockBehavior" /> with the pre-registered parameters.</returns>
	public MockBehavior UseConstructorParametersFor<T>(params object?[] parameters)
	{
		MockBehavior behavior = this with
		{
			_constructorParameters = new ConcurrentStack<IConstructorParameters>(_constructorParameters ?? []),
		};
		behavior._constructorParameters.Push(new ConstructorParameters<T>(() => parameters));
		return behavior;
	}

	/// <summary>
	///     Uses the given <paramref name="defaultValueFactories" /> to create default values for supported types.
	/// </summary>
	/// <param name="defaultValueFactories">Factories consulted in order, before the factories registered earlier; the first one whose <see cref="DefaultValueFactory.CanGenerateValue" /> returns <see langword="true" /> wins, otherwise the default value generator is used.</param>
	/// <returns>A new <see cref="MockBehavior" /> that layers <paramref name="defaultValueFactories" /> on top of the existing default-value generator.</returns>
	public MockBehavior WithDefaultValueFor(params DefaultValueFactory[] defaultValueFactories)
	{
		DefaultValueFactory[] factories = [..defaultValueFactories, ..(_defaultValueFactories ?? [])];
		return this with
		{
			_defaultValueFactories = factories,
			DefaultValue = new DefaultValueGeneratorWithFactories(_defaultValueGenerator, factories),
		};
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
	{
		List<string>? parts = null;
		if (ThrowWhenNotSetup)
		{
			(parts ??= []).Add(nameof(ThrowingWhenNotSetup));
		}

		if (SkipBaseClass)
		{
			(parts ??= []).Add(nameof(SkippingBaseClass));
		}

		if (SkipInteractionRecording)
		{
			(parts ??= []).Add(nameof(SkippingInteractionRecording));
		}

		string baseString = parts is null
			? "Default"
			: string.Join(" and ", parts);

		if (_constructorParameters is not null)
		{
			baseString += $" with {_constructorParameters.Count} constructor parameter registrations";
		}

		if (_values is not null)
		{
			baseString += $" with {_values.Count} setup registrations";
		}

		return baseString;
	}

	private interface IConstructorParameters;

#pragma warning disable S2326
	// ReSharper disable once UnusedTypeParameter
	private sealed class ConstructorParameters<T>(Func<object?[]> parameters) : IConstructorParameters
	{
		public object?[] GetParameters() => parameters();
	}
#pragma warning restore S2326

	private sealed class DefaultValueGeneratorWithFactories(
		IDefaultValueGenerator inner,
		DefaultValueFactory[] factories)
		: IDefaultValueGenerator
	{
		public object? GenerateValue(Type type, params object?[] parameters)
		{
			DefaultValueFactory? factory = factories.FirstOrDefault(f => f.CanGenerateValue(type));
			if (factory is not null)
			{
				return factory.GenerateValue(type, parameters);
			}

			return inner.GenerateValue(type, parameters);
		}
	}
}
