using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

#pragma warning disable S2436 // Types and methods should not have too many generic parameters

namespace Mockolate.Setup;

/// <summary>
///     Base class for indexer setups.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class IndexerSetup : IInteractiveIndexerSetup
{
	/// <summary>
	///     The mock registry this setup belongs to.
	/// </summary>
	private readonly MockRegistry _mockRegistry;

	/// <summary>
	///     Initializes a new instance of the <see cref="IndexerSetup" /> class bound to the given
	///     <paramref name="mockRegistry" />.
	/// </summary>
	protected IndexerSetup(MockRegistry mockRegistry)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IInteractiveIndexerSetup.Matches(IndexerAccess)" />
	bool IInteractiveIndexerSetup.Matches(IndexerAccess indexerAccess)
		=> MatchesAccess(indexerAccess);

	/// <inheritdoc cref="IInteractiveIndexerSetup.SkipBaseClass()" />
	public abstract bool? SkipBaseClass();

	/// <summary>
	///     Transitions the associated mock registry to the given <paramref name="scenario" />.
	/// </summary>
	/// <param name="scenario">The scenario name to switch to.</param>
	protected void TransitionScenario(string scenario)
		=> _mockRegistry.TransitionTo(scenario);

	/// <summary>
	///     Returns whether this setup applies to the given <paramref name="access" />.
	/// </summary>
	/// <param name="access">The recorded access being matched.</param>
	/// <returns><see langword="true" /> when this setup should handle the access.</returns>
	protected abstract bool MatchesAccess(IndexerAccess access);

	/// <summary>
	///     Runs the getter flow for <paramref name="access" /> using <paramref name="baseValue" /> as the seed.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The recorded access whose arguments identify the slot.</param>
	/// <param name="behavior">The mock's active behavior.</param>
	/// <param name="baseValue">The value produced by the base-class indexer (or a caller-supplied default).</param>
	/// <returns>The resolved getter value.</returns>
	public abstract TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult baseValue);

	/// <summary>
	///     Runs the getter flow for <paramref name="access" />, materializing the default value from
	///     <paramref name="behavior" /> inline when no stored value or initialization is present.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The recorded access whose arguments identify the slot.</param>
	/// <param name="behavior">The mock's active behavior (also provides the default value factory).</param>
	/// <returns>The resolved getter value.</returns>
	public abstract TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior);

	/// <summary>
	///     Runs the getter flow for <paramref name="access" /> using <paramref name="defaultValueGenerator" /> as the
	///     fallback when no value has been stored or initialized.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The recorded access whose arguments identify the slot.</param>
	/// <param name="behavior">The mock's active behavior.</param>
	/// <param name="defaultValueGenerator">Lazy producer of the default value; only invoked when a default is actually needed.</param>
	/// <returns>The resolved getter value.</returns>
	public abstract TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator);

	/// <summary>
	///     Runs the setter flow for <paramref name="access" /> with the given <paramref name="value" />.
	/// </summary>
	/// <typeparam name="TResult">The indexer's value type.</typeparam>
	/// <param name="access">The recorded access whose arguments identify the slot.</param>
	/// <param name="behavior">The mock's active behavior.</param>
	/// <param name="value">The value being assigned.</param>
	public abstract void SetResult<TResult>(IndexerAccess access, MockBehavior behavior, TResult value);

	/// <summary>
	///     Attempts to cast the specified value to the type parameter <typeparamref name="T" />,
	///     returning a value that indicates whether the cast was successful.
	/// </summary>
	/// <remarks>
	///     If value is not of type <typeparamref name="T" /> and is not <see langword="null" />,
	///     result is set to the default value for type <typeparamref name="T" /> as provided
	///     by the <paramref name="behavior" />.
	/// </remarks>
	protected static bool TryCast<T>([NotNullWhen(false)] object? value, out T result, MockBehavior behavior)
	{
		if (value is T typedValue)
		{
			result = typedValue;
			return true;
		}

		result = default!;
		return value is null;
	}

	/// <summary>
	///     Returns a formatted string representation of the given <paramref name="type" />.
	/// </summary>
	protected static string FormatType(Type type)
		=> type.FormatType();
}

/// <summary>
///     Sets up a <typeparamref name="TValue" /> indexer for <typeparamref name="T1" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerSetup<TValue, T1>(MockRegistry mockRegistry, IParameterMatch<T1> parameter1)
	: IndexerSetup(mockRegistry),
		IIndexerSetupWithCallback<TValue, T1>,
		IIndexerGetterSetupCallbackBuilder<TValue, T1>, IIndexerSetterSetupCallbackBuilder<TValue, T1>,
		IIndexerSetupReturnBuilder<TValue, T1>,
		IIndexerGetterSetupWithCallback<TValue, T1>, IIndexerSetterSetupWithCallback<TValue, T1>,
		IIndexerGetterOnlySetup<TValue, T1>, IIndexerSetterOnlySetup<TValue, T1>,
		IIndexerGetterOnlyGetterSetup<TValue, T1>, IIndexerSetterOnlySetterSetup<TValue, T1>,
		IIndexerGetterOnlySetupCallbackBuilder<TValue, T1>, IIndexerSetterOnlySetupCallbackBuilder<TValue, T1>,
		IIndexerGetterOnlySetupReturnBuilder<TValue, T1>
{
	private Callbacks<Action<int, T1, TValue>>? _getterCallbacks;
	private Callbacks<Func<int, T1, TValue, TValue>>? _returnCallbacks;
	private TValue _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private Callbacks<Action<int, T1, TValue>>? _setterCallbacks;
	private Func<T1, TValue>? _initialization;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, TValue, TValue>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TValue returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, TValue, TValue>>((_, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetup{TValue, T1}.Do(Action)" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1> IIndexerGetterSetup<TValue, T1>.Do(Action callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue currentValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1}.Do(Action{T1})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1> IIndexerGetterSetupWithCallback<TValue, T1>.Do(Action<T1> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue currentValue)
		{
			callback(p1);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1}.Do(Action{T1, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1> IIndexerGetterSetupWithCallback<TValue, T1>.Do(Action<T1, TValue> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue v)
		{
			callback(p1, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1}.Do(Action{int, T1, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1> IIndexerGetterSetupWithCallback<TValue, T1>.Do(Action<int, T1, TValue> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(callback);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1> IIndexerGetterSetup<TValue, T1>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new((_, _, _) => TransitionScenario(scenario));
		currentCallback.InParallel();
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1}.Do(Action)" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1> IIndexerSetterSetup<TValue, T1>.Do(Action callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue newValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1}.Do(Action{TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1> IIndexerSetterSetup<TValue, T1>.Do(Action<TValue> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue v)
		{
			callback(v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1}.Do(Action{T1, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1> IIndexerSetterSetupWithCallback<TValue, T1>.Do(Action<T1, TValue> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, TValue v)
		{
			callback(p1, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1}.Do(Action{int, T1, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1> IIndexerSetterSetupWithCallback<TValue, T1>.Do(Action<int, T1, TValue> callback)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new(callback);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1> IIndexerSetterSetup<TValue, T1>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, TValue>> currentCallback = new((_, _, _) => TransitionScenario(scenario));
		currentCallback.InParallel();
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.SkippingBaseClass(bool)" />
	public IIndexerSetup<TValue, T1> SkippingBaseClass(bool skipBaseClass = true)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.InitializeWith(TValue)" />
	public IIndexerSetupWithCallback<TValue, T1> InitializeWith(TValue value)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = _ => value;
		return this;
	}

	IIndexerSetup<TValue, T1> IIndexerSetup<TValue, T1>.InitializeWith(TValue value)
		=> InitializeWith(value);

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1}.InitializeWith(Func{T1, TValue})" />
	public IIndexerSetup<TValue, T1> InitializeWith(Func<T1, TValue> valueGenerator)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = valueGenerator;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.OnGet" />
	public IIndexerGetterSetupWithCallback<TValue, T1> OnGet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.OnSet" />
	public IIndexerSetterSetupWithCallback<TValue, T1> OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.Returns(TValue)" />
	public IIndexerSetupReturnBuilder<TValue, T1> Returns(TValue returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.Returns(Func{TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Returns(Func<TValue> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1}.Returns(Func{T1, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Returns(Func<T1, TValue> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			return callback(p1);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1}.Returns(Func{T1, TValue, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Returns(Func<T1, TValue, TValue> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue v)
		{
			return callback(p1, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.Throws{TException}()" />
	public IIndexerSetupReturnBuilder<TValue, T1> Throws<TException>()
		where TException : Exception, new()
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.Throws(Exception)" />
	public IIndexerSetupReturnBuilder<TValue, T1> Throws(Exception exception)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1}.Throws(Func{Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Throws(Func<Exception> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1}.Throws(Func{T1, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Throws(Func<T1, Exception> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue currentValue)
		{
			throw callback(p1);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1}.Throws(Func{T1, TValue, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1> Throws(Func<T1, TValue, Exception> callback)
	{
		Callback<Func<int, T1, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, TValue v)
		{
			throw callback(p1, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupParallelCallbackBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1> IIndexerGetterSetupParallelCallbackBuilder<TValue, T1>.When(
		Func<int, bool> predicate)
	{
		_getterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackBuilder{TValue, T1}.InParallel()" />
	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1> IIndexerGetterSetupCallbackBuilder<TValue, T1>.InParallel()
	{
		_getterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1}.For(int)" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1>.For(int times)
	{
		_getterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1}.Only(int)" />
	IIndexerSetup<TValue, T1> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1>.Only(int times)
	{
		_getterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupParallelCallbackBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1> IIndexerSetterSetupParallelCallbackBuilder<TValue, T1>.When(
		Func<int, bool> predicate)
	{
		_setterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackBuilder{TValue, T1}.InParallel()" />
	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1> IIndexerSetterSetupCallbackBuilder<TValue, T1>.InParallel()
	{
		_setterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1}.For(int)" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1>.For(int times)
	{
		_setterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1}.Only(int)" />
	IIndexerSetup<TValue, T1> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1>.Only(int times)
	{
		_setterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerSetupReturnWhenBuilder<TValue, T1> IIndexerSetupReturnBuilder<TValue, T1>.When(Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1}.For(int)" />
	IIndexerSetupReturnWhenBuilder<TValue, T1> IIndexerSetupReturnWhenBuilder<TValue, T1>.For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1}.Only(int)" />
	IIndexerSetup<TValue, T1> IIndexerSetupReturnWhenBuilder<TValue, T1>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{FormatType(typeof(TValue))} this[{parameter1}]";

	/// <inheritdoc cref="IndexerSetup.SkipBaseClass()" />
	public override bool? SkipBaseClass()
		=> _skipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter value <paramref name="p1" />.
	/// </summary>
	public virtual bool Matches(T1 p1)
	{
		if (!parameter1.Matches(p1))
		{
			return false;
		}

		parameter1.InvokeCallbacks(p1);
		return true;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, TValue value)
		=> Matches(p1);

	/// <inheritdoc cref="IndexerSetup.MatchesAccess(IndexerAccess)" />
	protected override bool MatchesAccess(IndexerAccess access)
	{
		if (access is IndexerGetterAccess<T1> getter)
		{
			return Matches(getter.Parameter1);
		}

		if (access is IndexerSetterAccess<T1, TValue> setter)
		{
			return Matches(setter.Parameter1, setter.TypedValue);
		}

		return false;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult baseValue)
	{
		if (!TryExtractParameter(access, out T1 p1))
		{
			return baseValue;
		}

		TValue currentValue = TryCast(baseValue, out TValue casted, behavior) ? casted : default!;
		currentValue = ExecuteGetterCallbacks(p1, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : baseValue;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior)
	{
		if (!TryExtractParameter(access, out T1 p1))
		{
			return behavior.DefaultValue.GenerateTyped<TResult>();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1);
		}
		else
		{
			currentValue = TryCast(AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>()), out TValue casted, behavior)
				? casted
				: default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior)
			? result
			: behavior.DefaultValue.GenerateTyped<TResult>();
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, Func{TResult})" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
	{
		if (!TryExtractParameter(access, out T1 p1))
		{
			return defaultValueGenerator();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1);
		}
		else
		{
			currentValue = TryCast(defaultValueGenerator(), out TValue casted, behavior) ? casted : default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : defaultValueGenerator();
	}

	/// <inheritdoc cref="IndexerSetup.SetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult value)
	{
		access.StoreValue(value);
		if (!TryExtractParameter(access, out T1 p1))
		{
			return;
		}

		if (!TryCast(value, out TValue resultValue, behavior))
		{
			return;
		}

		ExecuteSetterCallbacks(p1, resultValue);
	}

	internal TValue GetResult(IndexerValueStorage<TValue> leaf, T1 p1, MockBehavior behavior)
	{
		TValue currentValue;
		if (leaf.HasValue)
		{
			currentValue = leaf.Value;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1);
		}
		else
		{
			currentValue = AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>());
		}

		currentValue = ExecuteGetterCallbacks(p1, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal TValue GetResultWithBase(IndexerValueStorage<TValue> leaf, T1 p1, TValue baseValue)
	{
		TValue currentValue = ExecuteGetterCallbacks(p1, baseValue);
		currentValue = ExecuteReturnCallbacks(p1, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal void SetResult(IndexerValueStorage<TValue> leaf, T1 p1, TValue value)
	{
		leaf.Store(value);
		ExecuteSetterCallbacks(p1, value);
	}

	private void ExecuteSetterCallbacks(T1 p1, TValue resultValue)
	{
		if (_setterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;
			for (int i = 0; i < _setterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, TValue>> setterCallback =
					_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];
				if (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, (p1, resultValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.resultValue)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	private TValue ExecuteGetterCallbacks(T1 p1, TValue currentValue)
	{
		if (_getterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;
			for (int i = 0; i < _getterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, TValue>> getterCallback =
					_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];
				if (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, (p1, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.currentValue)))
				{
					wasInvoked = true;
				}
			}
		}

		return currentValue;
	}

	private TValue ExecuteReturnCallbacks(T1 p1, TValue currentValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			return _returnValue;
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Func<int, T1, TValue, TValue>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, TValue, TValue>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.currentValue),
					    out TValue? newValue))
				{
					return newValue!;
				}
			}
		}

		return currentValue;
	}

	private static bool TryExtractParameter(IndexerAccess access, out T1 p1)
	{
		if (access is IndexerGetterAccess<T1> getter)
		{
			p1 = getter.Parameter1;
			return true;
		}

		if (access is IndexerSetterAccess<T1, TValue> setter)
		{
			p1 = setter.Parameter1;
			return true;
		}

		p1 = default!;
		return false;
	}

	#region Accessor-restricted views

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.SkippingBaseClass(bool)" />
	IIndexerGetterOnlySetup<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.InitializeWith(TValue)" />
	IIndexerGetterOnlySetup<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.InitializeWith(TValue value)
	{
		InitializeWith(value);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.InitializeWith(Func{T1, TValue})" />
	IIndexerGetterOnlySetup<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.InitializeWith(Func<T1, TValue> valueGenerator)
	{
		InitializeWith(valueGenerator);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.OnGet" />
	IIndexerGetterOnlyGetterSetup<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.OnGet
		=> this;

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1}.Do(Action)" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1> IIndexerGetterOnlyGetterSetup<TValue, T1>.Do(Action callback)
	{
		((IIndexerGetterSetup<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1}.Do(Action{T1})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1> IIndexerGetterOnlyGetterSetup<TValue, T1>.Do(Action<T1> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1}.Do(Action{T1, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1> IIndexerGetterOnlyGetterSetup<TValue, T1>.Do(
		Action<T1, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1}.Do(Action{int, T1, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1> IIndexerGetterOnlyGetterSetup<TValue, T1>.Do(
		Action<int, T1, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1}.TransitionTo(string)" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1> IIndexerGetterOnlyGetterSetup<TValue, T1>.TransitionTo(
		string scenario)
	{
		((IIndexerGetterSetup<TValue, T1>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackBuilder{TValue, T1}.InParallel()" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1> IIndexerGetterOnlySetupCallbackBuilder<TValue, T1>.InParallel()
	{
		((IIndexerGetterSetupCallbackBuilder<TValue, T1>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupParallelCallbackBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1> IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1>.When(
		Func<int, bool> predicate)
	{
		((IIndexerGetterSetupParallelCallbackBuilder<TValue, T1>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1}.For(int)" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1>.For(
		int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1>.Only(int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Returns(TValue)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Returns(TValue returnValue)
	{
		Returns(returnValue);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Returns(Func{TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Returns(Func<TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Returns(Func{T1, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Returns(Func<T1, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Returns(Func{T1, TValue, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Returns(
		Func<T1, TValue, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Throws{TException}()" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Throws<TException>()
	{
		Throws<TException>();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Throws(Exception)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Throws(Exception exception)
	{
		Throws(exception);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Throws(Func{Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Throws(Func<Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Throws(Func{T1, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Throws(
		Func<T1, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1}.Throws(Func{T1, TValue, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1> IIndexerGetterOnlySetup<TValue, T1>.Throws(
		Func<T1, TValue, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1> IIndexerGetterOnlySetupReturnBuilder<TValue, T1>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetupReturnBuilder<TValue, T1>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1}.For(int)" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1>.For(
		int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1>.Only(int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1}.SkippingBaseClass(bool)" />
	IIndexerSetterOnlySetup<TValue, T1> IIndexerSetterOnlySetup<TValue, T1>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1}.OnSet" />
	IIndexerSetterOnlySetterSetup<TValue, T1> IIndexerSetterOnlySetup<TValue, T1>.OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1}.Do(Action)" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1> IIndexerSetterOnlySetterSetup<TValue, T1>.Do(Action callback)
	{
		((IIndexerSetterSetup<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1}.Do(Action{TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1> IIndexerSetterOnlySetterSetup<TValue, T1>.Do(
		Action<TValue> callback)
	{
		((IIndexerSetterSetup<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1}.Do(Action{T1, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1> IIndexerSetterOnlySetterSetup<TValue, T1>.Do(
		Action<T1, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1}.Do(Action{int, T1, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1> IIndexerSetterOnlySetterSetup<TValue, T1>.Do(
		Action<int, T1, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1}.TransitionTo(string)" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1> IIndexerSetterOnlySetterSetup<TValue, T1>.TransitionTo(
		string scenario)
	{
		((IIndexerSetterSetup<TValue, T1>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackBuilder{TValue, T1}.InParallel()" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1> IIndexerSetterOnlySetupCallbackBuilder<TValue, T1>.InParallel()
	{
		((IIndexerSetterSetupCallbackBuilder<TValue, T1>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupParallelCallbackBuilder{TValue, T1}.When(Func{int, bool})" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1> IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetterSetupParallelCallbackBuilder<TValue, T1>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1}.For(int)" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1>.For(
		int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1}.Only(int)" />
	IIndexerSetterOnlySetup<TValue, T1> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1>.Only(int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1>)this).Only(times);
		return this;
	}

	#endregion Accessor-restricted views
}

/// <summary>
///     Sets up a <typeparamref name="TValue" /> indexer for <typeparamref name="T1" /> and <typeparamref name="T2" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerSetup<TValue, T1, T2>(
	MockRegistry mockRegistry,
	IParameterMatch<T1> parameter1,
	IParameterMatch<T2> parameter2) : IndexerSetup(mockRegistry),
	IIndexerSetupWithCallback<TValue, T1, T2>,
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2>, IIndexerSetterSetupCallbackBuilder<TValue, T1, T2>,
	IIndexerSetupReturnBuilder<TValue, T1, T2>,
	IIndexerGetterSetupWithCallback<TValue, T1, T2>, IIndexerSetterSetupWithCallback<TValue, T1, T2>,
	IIndexerGetterOnlySetup<TValue, T1, T2>, IIndexerSetterOnlySetup<TValue, T1, T2>,
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2>, IIndexerSetterOnlySetterSetup<TValue, T1, T2>,
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2>, IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2>,
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2>
{
	private Callbacks<Action<int, T1, T2, TValue>>? _getterCallbacks;
	private Callbacks<Func<int, T1, T2, TValue, TValue>>? _returnCallbacks;
	private TValue _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private Callbacks<Action<int, T1, T2, TValue>>? _setterCallbacks;
	private Func<T1, T2, TValue>? _initialization;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, TValue, TValue>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TValue returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, TValue, TValue>>((_, _, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetup{TValue, T1, T2}.Do(Action)" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2> IIndexerGetterSetup<TValue, T1, T2>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2}.Do(Action{T1, T2})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2> IIndexerGetterSetupWithCallback<TValue, T1, T2>.Do(Action<T1, T2> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2}.Do(Action{T1, T2, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2> IIndexerGetterSetupWithCallback<TValue, T1, T2>.Do(Action<T1, T2, TValue> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue v)
		{
			callback(p1, p2, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2}.Do(Action{int, T1, T2, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2> IIndexerGetterSetupWithCallback<TValue, T1, T2>.Do(
		Action<int, T1, T2, TValue> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(callback);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2> IIndexerGetterSetup<TValue, T1, T2>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new((_, _, _, _) => TransitionScenario(scenario));
		currentCallback.InParallel();
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2}.Do(Action)" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2> IIndexerSetterSetup<TValue, T1, T2>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue newValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2}.Do(Action{TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2> IIndexerSetterSetup<TValue, T1, T2>.Do(Action<TValue> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue v)
		{
			callback(v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2}.Do(Action{T1, T2, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2> IIndexerSetterSetupWithCallback<TValue, T1, T2>.Do(Action<T1, T2, TValue> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, TValue v)
		{
			callback(p1, p2, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2}.Do(Action{int, T1, T2, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2> IIndexerSetterSetupWithCallback<TValue, T1, T2>.Do(
		Action<int, T1, T2, TValue> callback)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new(callback);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2> IIndexerSetterSetup<TValue, T1, T2>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, T2, TValue>> currentCallback = new((_, _, _, _) => TransitionScenario(scenario));
		currentCallback.InParallel();
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.SkippingBaseClass(bool)" />
	public IIndexerSetup<TValue, T1, T2> SkippingBaseClass(bool skipBaseClass = true)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.InitializeWith(TValue)" />
	public IIndexerSetupWithCallback<TValue, T1, T2> InitializeWith(TValue value)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = (_, _) => value;
		return this;
	}

	IIndexerSetup<TValue, T1, T2> IIndexerSetup<TValue, T1, T2>.InitializeWith(TValue value)
		=> InitializeWith(value);

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2}.InitializeWith(Func{T1, T2, TValue})" />
	public IIndexerSetup<TValue, T1, T2> InitializeWith(Func<T1, T2, TValue> valueGenerator)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = valueGenerator;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.OnGet" />
	public IIndexerGetterSetupWithCallback<TValue, T1, T2> OnGet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.OnSet" />
	public IIndexerSetterSetupWithCallback<TValue, T1, T2> OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.Returns(TValue)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Returns(TValue returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.Returns(Func{TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Returns(Func<TValue> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2}.Returns(Func{T1, T2, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Returns(Func<T1, T2, TValue> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			return callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2}.Returns(Func{T1, T2, TValue, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Returns(Func<T1, T2, TValue, TValue> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue v)
		{
			return callback(p1, p2, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.Throws{TException}()" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Throws<TException>()
		where TException : Exception, new()
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.Throws(Exception)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Throws(Exception exception)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2}.Throws(Func{Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Throws(Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2}.Throws(Func{T1, T2, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Throws(Func<T1, T2, Exception> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue currentValue)
		{
			throw callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2}.Throws(Func{T1, T2, TValue, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2> Throws(Func<T1, T2, TValue, Exception> callback)
	{
		Callback<Func<int, T1, T2, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, TValue v)
		{
			throw callback(p1, p2, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupParallelCallbackBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2> IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		_getterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackBuilder{TValue, T1, T2}.InParallel()" />
	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2> IIndexerGetterSetupCallbackBuilder<TValue, T1, T2>.InParallel()
	{
		_getterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2}.For(int)" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2>.For(int times)
	{
		_getterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2}.Only(int)" />
	IIndexerSetup<TValue, T1, T2> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		_getterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupParallelCallbackBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2> IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		_setterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackBuilder{TValue, T1, T2}.InParallel()" />
	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2> IIndexerSetterSetupCallbackBuilder<TValue, T1, T2>.InParallel()
	{
		_setterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2}.For(int)" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2>.For(int times)
	{
		_setterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2}.Only(int)" />
	IIndexerSetup<TValue, T1, T2> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		_setterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2> IIndexerSetupReturnBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2}.For(int)" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2> IIndexerSetupReturnWhenBuilder<TValue, T1, T2>.For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2}.Only(int)" />
	IIndexerSetup<TValue, T1, T2> IIndexerSetupReturnWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{FormatType(typeof(TValue))} this[{parameter1}, {parameter2}]";

	/// <inheritdoc cref="IndexerSetup.SkipBaseClass()" />
	public override bool? SkipBaseClass()
		=> _skipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2)
	{
		if (!parameter1.Matches(p1) || !parameter2.Matches(p2))
		{
			return false;
		}

		parameter1.InvokeCallbacks(p1);
		parameter2.InvokeCallbacks(p2);
		return true;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2, TValue value)
		=> Matches(p1, p2);

	/// <inheritdoc cref="IndexerSetup.MatchesAccess(IndexerAccess)" />
	protected override bool MatchesAccess(IndexerAccess access)
	{
		if (access is IndexerGetterAccess<T1, T2> getter)
		{
			return Matches(getter.Parameter1, getter.Parameter2);
		}

		if (access is IndexerSetterAccess<T1, T2, TValue> setter)
		{
			return Matches(setter.Parameter1, setter.Parameter2, setter.TypedValue);
		}

		return false;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult baseValue)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2))
		{
			return baseValue;
		}

		TValue currentValue = TryCast(baseValue, out TValue casted, behavior) ? casted : default!;
		currentValue = ExecuteGetterCallbacks(p1, p2, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : baseValue;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2))
		{
			return behavior.DefaultValue.GenerateTyped<TResult>();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2);
		}
		else
		{
			currentValue = TryCast(AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>()), out TValue casted, behavior)
				? casted
				: default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior)
			? result
			: behavior.DefaultValue.GenerateTyped<TResult>();
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, Func{TResult})" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2))
		{
			return defaultValueGenerator();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2);
		}
		else
		{
			currentValue = TryCast(defaultValueGenerator(), out TValue casted, behavior) ? casted : default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : defaultValueGenerator();
	}

	/// <inheritdoc cref="IndexerSetup.SetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult value)
	{
		access.StoreValue(value);
		if (!TryExtractParameters(access, out T1 p1, out T2 p2))
		{
			return;
		}

		if (!TryCast(value, out TValue resultValue, behavior))
		{
			return;
		}

		ExecuteSetterCallbacks(p1, p2, resultValue);
	}

	internal TValue GetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, MockBehavior behavior)
	{
		TValue currentValue;
		if (leaf.HasValue)
		{
			currentValue = leaf.Value;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2);
		}
		else
		{
			currentValue = AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>());
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal TValue GetResultWithBase(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, TValue baseValue)
	{
		TValue currentValue = ExecuteGetterCallbacks(p1, p2, baseValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal void SetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, TValue value)
	{
		leaf.Store(value);
		ExecuteSetterCallbacks(p1, p2, value);
	}

	private void ExecuteSetterCallbacks(T1 p1, T2 p2, TValue resultValue)
	{
		if (_setterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;
			for (int i = 0; i < _setterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, TValue>> setterCallback =
					_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];
				if (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, (p1, p2, resultValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.resultValue)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	private TValue ExecuteGetterCallbacks(T1 p1, T2 p2, TValue currentValue)
	{
		if (_getterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;
			for (int i = 0; i < _getterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, TValue>> getterCallback =
					_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];
				if (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, (p1, p2, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.currentValue)))
				{
					wasInvoked = true;
				}
			}
		}

		return currentValue;
	}

	private TValue ExecuteReturnCallbacks(T1 p1, T2 p2, TValue currentValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			return _returnValue;
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Func<int, T1, T2, TValue, TValue>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, TValue, TValue>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.currentValue),
					    out TValue? newValue))
				{
					return newValue!;
				}
			}
		}

		return currentValue;
	}

	private static bool TryExtractParameters(IndexerAccess access, out T1 p1, out T2 p2)
	{
		if (access is IndexerGetterAccess<T1, T2> getter)
		{
			p1 = getter.Parameter1;
			p2 = getter.Parameter2;
			return true;
		}

		if (access is IndexerSetterAccess<T1, T2, TValue> setter)
		{
			p1 = setter.Parameter1;
			p2 = setter.Parameter2;
			return true;
		}

		p1 = default!;
		p2 = default!;
		return false;
	}

	#region Accessor-restricted views

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.SkippingBaseClass(bool)" />
	IIndexerGetterOnlySetup<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.InitializeWith(TValue)" />
	IIndexerGetterOnlySetup<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.InitializeWith(TValue value)
	{
		InitializeWith(value);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.InitializeWith(Func{T1, T2, TValue})" />
	IIndexerGetterOnlySetup<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.InitializeWith(Func<T1, T2, TValue> valueGenerator)
	{
		InitializeWith(valueGenerator);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.OnGet" />
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.OnGet
		=> this;

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2}.Do(Action)" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlyGetterSetup<TValue, T1, T2>.Do(Action callback)
	{
		((IIndexerGetterSetup<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2}.Do(Action{T1, T2})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlyGetterSetup<TValue, T1, T2>.Do(Action<T1, T2> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2}.Do(Action{T1, T2, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlyGetterSetup<TValue, T1, T2>.Do(
		Action<T1, T2, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2}.Do(Action{int, T1, T2, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlyGetterSetup<TValue, T1, T2>.Do(
		Action<int, T1, T2, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2}.TransitionTo(string)" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlyGetterSetup<TValue, T1, T2>.TransitionTo(
		string scenario)
	{
		((IIndexerGetterSetup<TValue, T1, T2>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackBuilder{TValue, T1, T2}.InParallel()" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2> IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2>.InParallel()
	{
		((IIndexerGetterSetupCallbackBuilder<TValue, T1, T2>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupParallelCallbackBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		((IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2}.For(int)" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2>.For(
		int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Returns(TValue)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Returns(TValue returnValue)
	{
		Returns(returnValue);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Returns(Func{TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Returns(Func<TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Returns(Func{T1, T2, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Returns(Func<T1, T2, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Returns(Func{T1, T2, TValue, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Returns(
		Func<T1, T2, TValue, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Throws{TException}()" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Throws<TException>()
	{
		Throws<TException>();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Throws(Exception)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Throws(Exception exception)
	{
		Throws(exception);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Throws(Func{Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Throws(Func<Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Throws(Func{T1, T2, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Throws(
		Func<T1, T2, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2}.Throws(Func{T1, T2, TValue, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2> IIndexerGetterOnlySetup<TValue, T1, T2>.Throws(
		Func<T1, T2, TValue, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2> IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetupReturnBuilder<TValue, T1, T2>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2}.For(int)" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2>.For(
		int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2}.SkippingBaseClass(bool)" />
	IIndexerSetterOnlySetup<TValue, T1, T2> IIndexerSetterOnlySetup<TValue, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2}.OnSet" />
	IIndexerSetterOnlySetterSetup<TValue, T1, T2> IIndexerSetterOnlySetup<TValue, T1, T2>.OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2}.Do(Action)" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetterSetup<TValue, T1, T2>.Do(Action callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2}.Do(Action{TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetterSetup<TValue, T1, T2>.Do(
		Action<TValue> callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2}.Do(Action{T1, T2, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetterSetup<TValue, T1, T2>.Do(
		Action<T1, T2, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2}.Do(Action{int, T1, T2, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetterSetup<TValue, T1, T2>.Do(
		Action<int, T1, T2, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2}.TransitionTo(string)" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetterSetup<TValue, T1, T2>.TransitionTo(
		string scenario)
	{
		((IIndexerSetterSetup<TValue, T1, T2>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackBuilder{TValue, T1, T2}.InParallel()" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2> IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2>.InParallel()
	{
		((IIndexerSetterSetupCallbackBuilder<TValue, T1, T2>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupParallelCallbackBuilder{TValue, T1, T2}.When(Func{int, bool})" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2}.For(int)" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2>.For(
		int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2}.Only(int)" />
	IIndexerSetterOnlySetup<TValue, T1, T2> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2>.Only(int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2>)this).Only(times);
		return this;
	}

	#endregion Accessor-restricted views
}

/// <summary>
///     Sets up a <typeparamref name="TValue" /> indexer for <typeparamref name="T1" />, <typeparamref name="T2" /> and
///     <typeparamref name="T3" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerSetup<TValue, T1, T2, T3>(
	MockRegistry mockRegistry,
	IParameterMatch<T1> parameter1,
	IParameterMatch<T2> parameter2,
	IParameterMatch<T3> parameter3) : IndexerSetup(mockRegistry),
	IIndexerSetupWithCallback<TValue, T1, T2, T3>,
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3>, IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3>,
	IIndexerSetupReturnBuilder<TValue, T1, T2, T3>,
	IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>, IIndexerSetterSetupWithCallback<TValue, T1, T2, T3>,
	IIndexerGetterOnlySetup<TValue, T1, T2, T3>, IIndexerSetterOnlySetup<TValue, T1, T2, T3>,
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>, IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>,
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3>, IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3>,
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3>
{
	private Callbacks<Action<int, T1, T2, T3, TValue>>? _getterCallbacks;
	private Callbacks<Func<int, T1, T2, T3, TValue, TValue>>? _returnCallbacks;
	private TValue _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private Callbacks<Action<int, T1, T2, T3, TValue>>? _setterCallbacks;
	private Func<T1, T2, T3, TValue>? _initialization;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, T3, TValue, TValue>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TValue returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, T3, TValue, TValue>>((_, _, _, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetup{TValue, T1, T2, T3}.Do(Action)" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetup<TValue, T1, T2, T3>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3}.Do(Action{T1, T2, T3})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>.Do(
		Action<T1, T2, T3> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3}.Do(Action{T1, T2, T3, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>.Do(
		Action<T1, T2, T3, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue v)
		{
			callback(p1, p2, p3, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3}.Do(Action{int, T1, T2, T3, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>.Do(
		Action<int, T1, T2, T3, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(callback);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetup<TValue, T1, T2, T3>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new((_, _, _, _, _) =>
			TransitionScenario(scenario));
		currentCallback.InParallel();
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2, T3}.Do(Action)" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetup<TValue, T1, T2, T3>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue newValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2, T3}.Do(Action{TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetup<TValue, T1, T2, T3>.Do(
		Action<TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue v)
		{
			callback(v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2, T3}.Do(Action{T1, T2, T3, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetupWithCallback<TValue, T1, T2, T3>.Do(
		Action<T1, T2, T3, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, TValue v)
		{
			callback(p1, p2, p3, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2, T3}.Do(Action{int, T1, T2, T3, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetupWithCallback<TValue, T1, T2, T3>.Do(
		Action<int, T1, T2, T3, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new(callback);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetup<TValue, T1, T2, T3>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3, TValue>> currentCallback = new((_, _, _, _, _) =>
			TransitionScenario(scenario));
		currentCallback.InParallel();
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.SkippingBaseClass(bool)" />
	public IIndexerSetup<TValue, T1, T2, T3> SkippingBaseClass(bool skipBaseClass = true)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.InitializeWith(TValue)" />
	public IIndexerSetupWithCallback<TValue, T1, T2, T3> InitializeWith(TValue value)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = (_, _, _) => value;
		return this;
	}

	IIndexerSetup<TValue, T1, T2, T3> IIndexerSetup<TValue, T1, T2, T3>.InitializeWith(TValue value)
		=> InitializeWith(value);

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3}.InitializeWith(Func{T1, T2, T3, TValue})" />
	public IIndexerSetup<TValue, T1, T2, T3> InitializeWith(Func<T1, T2, T3, TValue> valueGenerator)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = valueGenerator;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.OnGet" />
	public IIndexerGetterSetupWithCallback<TValue, T1, T2, T3> OnGet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.OnSet" />
	public IIndexerSetterSetupWithCallback<TValue, T1, T2, T3> OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.Returns(TValue)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Returns(TValue returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.Returns(Func{TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Returns(Func<TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3}.Returns(Func{T1, T2, T3, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Returns(Func<T1, T2, T3, TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			return callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3}.Returns(Func{T1, T2, T3, TValue, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Returns(Func<T1, T2, T3, TValue, TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue v)
		{
			return callback(p1, p2, p3, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.Throws{TException}()" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Throws<TException>()
		where TException : Exception, new()
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.Throws(Exception)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Throws(Exception exception)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3}.Throws(Func{Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Throws(Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3}.Throws(Func{T1, T2, T3, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Throws(Func<T1, T2, T3, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue currentValue)
		{
			throw callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3}.Throws(Func{T1, T2, T3, TValue, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3> Throws(Func<T1, T2, T3, TValue, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, TValue v)
		{
			throw callback(p1, p2, p3, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupParallelCallbackBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		_getterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackBuilder{TValue, T1, T2, T3}.InParallel()" />
	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3>.InParallel()
	{
		_getterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2,T3}.For(int)" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		_getterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2,T3}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		_getterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupParallelCallbackBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		_setterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackBuilder{TValue, T1, T2, T3}.InParallel()" />
	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3>.InParallel()
	{
		_setterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2,T3}.For(int)" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		_setterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2,T3}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		_setterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3> IIndexerSetupReturnBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2,T3}.For(int)" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3> IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2,T3}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3> IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{FormatType(typeof(TValue))} this[{parameter1}, {parameter2}, {parameter3}]";

	/// <inheritdoc cref="IndexerSetup.SkipBaseClass()" />
	public override bool? SkipBaseClass()
		=> _skipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2, T3 p3)
	{
		if (!parameter1.Matches(p1) || !parameter2.Matches(p2) || !parameter3.Matches(p3))
		{
			return false;
		}

		parameter1.InvokeCallbacks(p1);
		parameter2.InvokeCallbacks(p2);
		parameter3.InvokeCallbacks(p3);
		return true;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2, T3 p3, TValue value)
		=> Matches(p1, p2, p3);

	/// <inheritdoc cref="IndexerSetup.MatchesAccess(IndexerAccess)" />
	protected override bool MatchesAccess(IndexerAccess access)
	{
		if (access is IndexerGetterAccess<T1, T2, T3> getter)
		{
			return Matches(getter.Parameter1, getter.Parameter2, getter.Parameter3);
		}

		if (access is IndexerSetterAccess<T1, T2, T3, TValue> setter)
		{
			return Matches(setter.Parameter1, setter.Parameter2, setter.Parameter3, setter.TypedValue);
		}

		return false;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult baseValue)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3))
		{
			return baseValue;
		}

		TValue currentValue = TryCast(baseValue, out TValue casted, behavior) ? casted : default!;
		currentValue = ExecuteGetterCallbacks(p1, p2, p3, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : baseValue;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3))
		{
			return behavior.DefaultValue.GenerateTyped<TResult>();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3);
		}
		else
		{
			currentValue = TryCast(AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>()), out TValue casted, behavior)
				? casted
				: default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior)
			? result
			: behavior.DefaultValue.GenerateTyped<TResult>();
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, Func{TResult})" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3))
		{
			return defaultValueGenerator();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3);
		}
		else
		{
			currentValue = TryCast(defaultValueGenerator(), out TValue casted, behavior) ? casted : default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : defaultValueGenerator();
	}

	/// <inheritdoc cref="IndexerSetup.SetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult value)
	{
		access.StoreValue(value);
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3))
		{
			return;
		}

		if (!TryCast(value, out TValue resultValue, behavior))
		{
			return;
		}

		ExecuteSetterCallbacks(p1, p2, p3, resultValue);
	}

	internal TValue GetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, MockBehavior behavior)
	{
		TValue currentValue;
		if (leaf.HasValue)
		{
			currentValue = leaf.Value;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3);
		}
		else
		{
			currentValue = AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>());
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal TValue GetResultWithBase(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, TValue baseValue)
	{
		TValue currentValue = ExecuteGetterCallbacks(p1, p2, p3, baseValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal void SetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, TValue value)
	{
		leaf.Store(value);
		ExecuteSetterCallbacks(p1, p2, p3, value);
	}

	private void ExecuteSetterCallbacks(T1 p1, T2 p2, T3 p3, TValue resultValue)
	{
		if (_setterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;
			for (int i = 0; i < _setterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, TValue>> setterCallback =
					_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];
				if (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, (p1, p2, p3, resultValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.resultValue)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	private TValue ExecuteGetterCallbacks(T1 p1, T2 p2, T3 p3, TValue currentValue)
	{
		if (_getterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;
			for (int i = 0; i < _getterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, TValue>> getterCallback =
					_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];
				if (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, (p1, p2, p3, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.currentValue)))
				{
					wasInvoked = true;
				}
			}
		}

		return currentValue;
	}

	private TValue ExecuteReturnCallbacks(T1 p1, T2 p2, T3 p3, TValue currentValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			return _returnValue;
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Func<int, T1, T2, T3, TValue, TValue>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, T3, TValue, TValue>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2, p3, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.currentValue),
					    out TValue? newValue))
				{
					return newValue!;
				}
			}
		}

		return currentValue;
	}

	private static bool TryExtractParameters(IndexerAccess access, out T1 p1, out T2 p2, out T3 p3)
	{
		if (access is IndexerGetterAccess<T1, T2, T3> getter)
		{
			p1 = getter.Parameter1;
			p2 = getter.Parameter2;
			p3 = getter.Parameter3;
			return true;
		}

		if (access is IndexerSetterAccess<T1, T2, T3, TValue> setter)
		{
			p1 = setter.Parameter1;
			p2 = setter.Parameter2;
			p3 = setter.Parameter3;
			return true;
		}

		p1 = default!;
		p2 = default!;
		p3 = default!;
		return false;
	}

	#region Accessor-restricted views

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.SkippingBaseClass(bool)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.InitializeWith(TValue)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.InitializeWith(TValue value)
	{
		InitializeWith(value);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.InitializeWith(Func{T1, T2, T3, TValue})" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.InitializeWith(Func<T1, T2, T3, TValue> valueGenerator)
	{
		InitializeWith(valueGenerator);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.OnGet" />
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.OnGet
		=> this;

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3}.Do(Action)" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>.Do(Action callback)
	{
		((IIndexerGetterSetup<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3}.Do(Action{T1, T2, T3})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>.Do(Action<T1, T2, T3> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3}.Do(Action{T1, T2, T3, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>.Do(
		Action<T1, T2, T3, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3}.Do(Action{int, T1, T2, T3, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>.Do(
		Action<int, T1, T2, T3, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3}.TransitionTo(string)" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3>.TransitionTo(
		string scenario)
	{
		((IIndexerGetterSetup<TValue, T1, T2, T3>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackBuilder{TValue, T1, T2, T3}.InParallel()" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3>.InParallel()
	{
		((IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupParallelCallbackBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		((IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3}.For(int)" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Returns(TValue)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Returns(TValue returnValue)
	{
		Returns(returnValue);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Returns(Func{TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Returns(Func<TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Returns(Func{T1, T2, T3, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Returns(Func<T1, T2, T3, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Returns(Func{T1, T2, T3, TValue, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Returns(
		Func<T1, T2, T3, TValue, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Throws{TException}()" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Throws<TException>()
	{
		Throws<TException>();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Throws(Exception)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Throws(Exception exception)
	{
		Throws(exception);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Throws(Func{Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Throws(Func<Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Throws(Func{T1, T2, T3, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Throws(
		Func<T1, T2, T3, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3}.Throws(Func{T1, T2, T3, TValue, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetup<TValue, T1, T2, T3>.Throws(
		Func<T1, T2, T3, TValue, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetupReturnBuilder<TValue, T1, T2, T3>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2, T3}.For(int)" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2, T3}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2, T3}.SkippingBaseClass(bool)" />
	IIndexerSetterOnlySetup<TValue, T1, T2, T3> IIndexerSetterOnlySetup<TValue, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2, T3}.OnSet" />
	IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3> IIndexerSetterOnlySetup<TValue, T1, T2, T3>.OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3}.Do(Action)" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>.Do(Action callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3}.Do(Action{TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>.Do(
		Action<TValue> callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3}.Do(Action{T1, T2, T3, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>.Do(
		Action<T1, T2, T3, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3}.Do(Action{int, T1, T2, T3, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>.Do(
		Action<int, T1, T2, T3, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2, T3>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3}.TransitionTo(string)" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3>.TransitionTo(
		string scenario)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackBuilder{TValue, T1, T2, T3}.InParallel()" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3>.InParallel()
	{
		((IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupParallelCallbackBuilder{TValue, T1, T2, T3}.When(Func{int, bool})" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3}.For(int)" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3>.For(
		int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3}.Only(int)" />
	IIndexerSetterOnlySetup<TValue, T1, T2, T3> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3>.Only(int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3>)this).Only(times);
		return this;
	}

	#endregion Accessor-restricted views
}

/// <summary>
///     Sets up a <typeparamref name="TValue" /> indexer for <typeparamref name="T1" />, <typeparamref name="T2" />,
///     <typeparamref name="T3" /> and <typeparamref name="T4" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerSetup<TValue, T1, T2, T3, T4>(
	MockRegistry mockRegistry,
	IParameterMatch<T1> parameter1,
	IParameterMatch<T2> parameter2,
	IParameterMatch<T3> parameter3,
	IParameterMatch<T4> parameter4) : IndexerSetup(mockRegistry),
	IIndexerSetupWithCallback<TValue, T1, T2, T3, T4>,
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>, IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>,
	IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4>,
	IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>, IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4>,
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>, IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4>,
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>, IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>,
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4>,
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4>,
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4>
{
	private Callbacks<Action<int, T1, T2, T3, T4, TValue>>? _getterCallbacks;
	private Callbacks<Func<int, T1, T2, T3, T4, TValue, TValue>>? _returnCallbacks;
	private TValue _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private Callbacks<Action<int, T1, T2, T3, T4, TValue>>? _setterCallbacks;
	private Func<T1, T2, T3, T4, TValue>? _initialization;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, T3, T4, TValue, TValue>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TValue returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, T3, T4, TValue, TValue>>((_, _, _, _, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetup{TValue, T1, T2, T3, T4}.Do(Action)" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetup<TValue, T1, T2, T3, T4>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue v)
		{
			callback(p1, p2, p3, p4, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4, TValue})" />
	IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>.Do(
		Action<int, T1, T2, T3, T4, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(callback);
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetup<TValue, T1, T2, T3, T4>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new((_, _, _, _, _, _) =>
			TransitionScenario(scenario));
		currentCallback.InParallel();
		_getterCallbacks = _getterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2, T3, T4}.Do(Action)" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetup<TValue, T1, T2, T3, T4>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue newValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetup{TValue, T1, T2, T3, T4}.Do(Action{TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue v)
		{
			callback(v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue v)
		{
			callback(p1, p2, p3, p4, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4, TValue})" />
	IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4>.Do(
		Action<int, T1, T2, T3, T4, TValue> callback)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new(callback);
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetup<TValue, T1, T2, T3, T4>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3, T4, TValue>> currentCallback = new((_, _, _, _, _, _) =>
			TransitionScenario(scenario));
		currentCallback.InParallel();
		_setterCallbacks = _setterCallbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	public IIndexerSetup<TValue, T1, T2, T3, T4> SkippingBaseClass(bool skipBaseClass = true)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.InitializeWith(TValue)" />
	public IIndexerSetupWithCallback<TValue, T1, T2, T3, T4> InitializeWith(TValue value)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = (_, _, _, _) => value;
		return this;
	}

	IIndexerSetup<TValue, T1, T2, T3, T4> IIndexerSetup<TValue, T1, T2, T3, T4>.InitializeWith(TValue value)
		=> InitializeWith(value);

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3, T4}.InitializeWith(Func{T1, T2, T3, T4, TValue})" />
	public IIndexerSetup<TValue, T1, T2, T3, T4> InitializeWith(Func<T1, T2, T3, T4, TValue> valueGenerator)
	{
		if (_initialization is not null)
		{
			throw new MockException("The indexer is already initialized. You cannot initialize it twice.");
		}

		_initialization = valueGenerator;
		return this;
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.OnGet" />
	public IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4> OnGet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.OnSet" />
	public IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4> OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.Returns(TValue)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Returns(TValue returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.Returns(Func{TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Returns(Func<TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3, T4}.Returns(Func{T1, T2, T3, T4, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Returns(Func<T1, T2, T3, T4, TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			return callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3, T4}.Returns(Func{T1, T2, T3, T4, TValue, TValue})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Returns(Func<T1, T2, T3, T4, TValue, TValue> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue v)
		{
			return callback(p1, p2, p3, p4, v);
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.Throws{TException}()" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Throws<TException>()
		where TException : Exception, new()
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.Throws(Exception)" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Throws(Exception exception)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IIndexerSetup{TValue, T1, T2, T3, T4}.Throws(Func{Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Throws(Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Throws(Func<T1, T2, T3, T4, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
		{
			throw callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, TValue, Exception})" />
	public IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4> Throws(Func<T1, T2, T3, T4, TValue, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TValue Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4, TValue v)
		{
			throw callback(p1, p2, p3, p4, v);
		}
	}

	/// <inheritdoc cref="IIndexerGetterSetupParallelCallbackBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		_getterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackBuilder{TValue, T1, T2, T3, T4}.InParallel()" />
	IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>.
		InParallel()
	{
		_getterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2,T3,T4}.For(int)" />
	IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.
		For(int times)
	{
		_getterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue,T1,T2,T3,T4}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3, T4> IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		_getterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupParallelCallbackBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		_setterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackBuilder{TValue, T1, T2, T3, T4}.InParallel()" />
	IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>.
		InParallel()
	{
		_setterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2,T3,T4}.For(int)" />
	IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.
		For(int times)
	{
		_setterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue,T1,T2,T3,T4}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3, T4> IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		_setterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2,T3,T4}.For(int)" />
	IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4>.For(
		int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue,T1,T2,T3,T4}.Only(int)" />
	IIndexerSetup<TValue, T1, T2, T3, T4> IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{FormatType(typeof(TValue))} this[{parameter1}, {parameter2}, {parameter3}, {parameter4}]";

	/// <inheritdoc cref="IndexerSetup.SkipBaseClass()" />
	public override bool? SkipBaseClass()
		=> _skipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2, T3 p3, T4 p4)
	{
		if (!parameter1.Matches(p1) || !parameter2.Matches(p2) ||
		    !parameter3.Matches(p3) || !parameter4.Matches(p4))
		{
			return false;
		}

		parameter1.InvokeCallbacks(p1);
		parameter2.InvokeCallbacks(p2);
		parameter3.InvokeCallbacks(p3);
		parameter4.InvokeCallbacks(p4);
		return true;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values.
	/// </summary>
	public virtual bool Matches(T1 p1, T2 p2, T3 p3, T4 p4, TValue value)
		=> Matches(p1, p2, p3, p4);

	/// <inheritdoc cref="IndexerSetup.MatchesAccess(IndexerAccess)" />
	protected override bool MatchesAccess(IndexerAccess access)
	{
		if (access is IndexerGetterAccess<T1, T2, T3, T4> getter)
		{
			return Matches(getter.Parameter1, getter.Parameter2, getter.Parameter3, getter.Parameter4);
		}

		if (access is IndexerSetterAccess<T1, T2, T3, T4, TValue> setter)
		{
			return Matches(setter.Parameter1, setter.Parameter2, setter.Parameter3, setter.Parameter4,
				setter.TypedValue);
		}

		return false;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult baseValue)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3, out T4 p4))
		{
			return baseValue;
		}

		TValue currentValue = TryCast(baseValue, out TValue casted, behavior) ? casted : default!;
		currentValue = ExecuteGetterCallbacks(p1, p2, p3, p4, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, p4, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : baseValue;
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior)" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3, out T4 p4))
		{
			return behavior.DefaultValue.GenerateTyped<TResult>();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3, p4);
		}
		else
		{
			currentValue = TryCast(AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>()), out TValue casted, behavior)
				? casted
				: default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, p4, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, p4, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior)
			? result
			: behavior.DefaultValue.GenerateTyped<TResult>();
	}

	/// <inheritdoc cref="IndexerSetup.GetResult{TResult}(IndexerAccess, MockBehavior, Func{TResult})" />
	public override TResult GetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
	{
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3, out T4 p4))
		{
			return defaultValueGenerator();
		}

		TValue currentValue;
		if (access.TryFindStoredValue(out TValue existing))
		{
			currentValue = existing;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3, p4);
		}
		else
		{
			currentValue = TryCast(defaultValueGenerator(), out TValue casted, behavior) ? casted : default!;
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, p4, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, p4, currentValue);
		access.StoreValue(currentValue);
		return TryCast(currentValue, out TResult result, behavior) ? result : defaultValueGenerator();
	}

	/// <inheritdoc cref="IndexerSetup.SetResult{TResult}(IndexerAccess, MockBehavior, TResult)" />
	public override void SetResult<TResult>(IndexerAccess access, MockBehavior behavior,
		TResult value)
	{
		access.StoreValue(value);
		if (!TryExtractParameters(access, out T1 p1, out T2 p2, out T3 p3, out T4 p4))
		{
			return;
		}

		if (!TryCast(value, out TValue resultValue, behavior))
		{
			return;
		}

		ExecuteSetterCallbacks(p1, p2, p3, p4, resultValue);
	}

	internal TValue GetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, T4 p4, MockBehavior behavior)
	{
		TValue currentValue;
		if (leaf.HasValue)
		{
			currentValue = leaf.Value;
		}
		else if (_initialization is not null)
		{
			currentValue = _initialization.Invoke(p1, p2, p3, p4);
		}
		else
		{
			currentValue = AutoMocks.Or(behavior, behavior.DefaultValue.GenerateTyped<TValue>());
		}

		currentValue = ExecuteGetterCallbacks(p1, p2, p3, p4, currentValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, p4, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal TValue GetResultWithBase(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, T4 p4, TValue baseValue)
	{
		TValue currentValue = ExecuteGetterCallbacks(p1, p2, p3, p4, baseValue);
		currentValue = ExecuteReturnCallbacks(p1, p2, p3, p4, currentValue);
		leaf.Store(currentValue);
		return currentValue;
	}

	internal void SetResult(IndexerValueStorage<TValue> leaf, T1 p1, T2 p2, T3 p3, T4 p4, TValue value)
	{
		leaf.Store(value);
		ExecuteSetterCallbacks(p1, p2, p3, p4, value);
	}

	private void ExecuteSetterCallbacks(T1 p1, T2 p2, T3 p3, T4 p4, TValue resultValue)
	{
		if (_setterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;
			for (int i = 0; i < _setterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, T4, TValue>> setterCallback =
					_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];
				if (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, (p1, p2, p3, p4, resultValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.p4, state.resultValue)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	private TValue ExecuteGetterCallbacks(T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
	{
		if (_getterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;
			for (int i = 0; i < _getterCallbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, T4, TValue>> getterCallback =
					_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];
				if (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, (p1, p2, p3, p4, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.p4, state.currentValue)))
				{
					wasInvoked = true;
				}
			}
		}

		return currentValue;
	}

	private TValue ExecuteReturnCallbacks(T1 p1, T2 p2, T3 p3, T4 p4, TValue currentValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			return _returnValue;
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, T3, T4, TValue, TValue>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2, p3, p4, currentValue),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.p4, state.currentValue),
					    out TValue? newValue))
				{
					return newValue!;
				}
			}
		}

		return currentValue;
	}

	private static bool TryExtractParameters(IndexerAccess access,
		out T1 p1, out T2 p2, out T3 p3, out T4 p4)
	{
		if (access is IndexerGetterAccess<T1, T2, T3, T4> getter)
		{
			p1 = getter.Parameter1;
			p2 = getter.Parameter2;
			p3 = getter.Parameter3;
			p4 = getter.Parameter4;
			return true;
		}

		if (access is IndexerSetterAccess<T1, T2, T3, T4, TValue> setter)
		{
			p1 = setter.Parameter1;
			p2 = setter.Parameter2;
			p3 = setter.Parameter3;
			p4 = setter.Parameter4;
			return true;
		}

		p1 = default!;
		p2 = default!;
		p3 = default!;
		p4 = default!;
		return false;
	}

	#region Accessor-restricted views

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.InitializeWith(TValue)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.InitializeWith(TValue value)
	{
		InitializeWith(value);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.InitializeWith(Func{T1, T2, T3, T4, TValue})" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.InitializeWith(Func<T1, T2, T3, T4, TValue> valueGenerator)
	{
		InitializeWith(valueGenerator);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.OnGet" />
	IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.OnGet
		=> this;

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3, T4}.Do(Action)" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>.Do(Action callback)
	{
		((IIndexerGetterSetup<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>.Do(Action<T1, T2, T3, T4> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4, TValue})" />
	IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<int, T1, T2, T3, T4, TValue> callback)
	{
		((IIndexerGetterSetupWithCallback<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, T1, T2, T3, T4}.TransitionTo(string)" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlyGetterSetup<TValue, T1, T2, T3, T4>.TransitionTo(
		string scenario)
	{
		((IIndexerGetterSetup<TValue, T1, T2, T3, T4>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackBuilder{TValue, T1, T2, T3, T4}.InParallel()" />
	IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4>.InParallel()
	{
		((IIndexerGetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupParallelCallbackBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		((IIndexerGetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3, T4}.For(int)" />
	IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.For(
		int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3, T4}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		((IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Returns(TValue)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Returns(TValue returnValue)
	{
		Returns(returnValue);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Returns(Func{TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Returns(Func<TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Returns(Func{T1, T2, T3, T4, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Returns(Func<T1, T2, T3, T4, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Returns(Func{T1, T2, T3, T4, TValue, TValue})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Returns(
		Func<T1, T2, T3, T4, TValue, TValue> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Throws{TException}()" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Throws<TException>()
	{
		Throws<TException>();
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Throws(Exception)" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Throws(Exception exception)
	{
		Throws(exception);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Throws(Func{Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Throws(Func<Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Throws(
		Func<T1, T2, T3, T4, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, TValue, Exception})" />
	IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4>.Throws(
		Func<T1, T2, T3, T4, TValue, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupReturnBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetupReturnBuilder<TValue, T1, T2, T3, T4>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2, T3, T4}.For(int)" />
	IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3, T4>.For(
		int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, T1, T2, T3, T4}.Only(int)" />
	IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		((IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, T1, T2, T3, T4}.OnSet" />
	IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4>.OnSet
		=> this;

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3, T4}.Do(Action)" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>.Do(Action callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3, T4}.Do(Action{TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<TValue> callback)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4, TValue})" />
	IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>.Do(
		Action<int, T1, T2, T3, T4, TValue> callback)
	{
		((IIndexerSetterSetupWithCallback<TValue, T1, T2, T3, T4>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, T1, T2, T3, T4}.TransitionTo(string)" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetterSetup<TValue, T1, T2, T3, T4>.TransitionTo(
		string scenario)
	{
		((IIndexerSetterSetup<TValue, T1, T2, T3, T4>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackBuilder{TValue, T1, T2, T3, T4}.InParallel()" />
	IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetupCallbackBuilder<TValue, T1, T2, T3, T4>.InParallel()
	{
		((IIndexerSetterSetupCallbackBuilder<TValue, T1, T2, T3, T4>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupParallelCallbackBuilder{TValue, T1, T2, T3, T4}.When(Func{int, bool})" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		((IIndexerSetterSetupParallelCallbackBuilder<TValue, T1, T2, T3, T4>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3, T4}.For(int)" />
	IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.For(
		int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, T1, T2, T3, T4}.Only(int)" />
	IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>.Only(int times)
	{
		((IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4>)this).Only(times);
		return this;
	}

	#endregion Accessor-restricted views
}
