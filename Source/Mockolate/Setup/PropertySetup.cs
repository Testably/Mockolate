using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Internals;

namespace Mockolate.Setup;

/// <summary>
///     Base class for property setups.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class PropertySetup : IInteractivePropertySetup
{
	/// <summary>
	///     The property name.
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	///     Gets whether the property has already been initialized with a value.
	/// </summary>
	internal abstract bool IsValueInitialized { get; }

	/// <inheritdoc cref="IInteractivePropertySetup.InvokeSetter{T}(IInteraction?, T, MockBehavior)" />
	void IInteractivePropertySetup.InvokeSetter<T>(IInteraction? invocation, T value, MockBehavior behavior)
		=> InvokeSetter(value, behavior);

	/// <inheritdoc cref="IInteractivePropertySetup.InvokeGetter{TResult}(IInteraction?, MockBehavior, Func{TResult}?)" />
	TResult IInteractivePropertySetup.InvokeGetter<TResult>(IInteraction? invocation, MockBehavior behavior,
		Func<TResult> defaultValueGenerator)
		=> InvokeGetter(behavior, defaultValueGenerator);

	/// <inheritdoc cref="IInteractivePropertySetup.Matches(PropertyAccess)" />
	bool IInteractivePropertySetup.Matches(PropertyAccess propertyAccess)
		=> Matches(propertyAccess);

	/// <inheritdoc cref="IInteractivePropertySetup.SkipBaseClass()" />
	bool? IInteractivePropertySetup.SkipBaseClass()
		=> GetSkipBaseClass();

	/// <inheritdoc cref="IInteractivePropertySetup.InitializeWith(object?)" />
	void IInteractivePropertySetup.InitializeWith(object? value)
		=> InitializeValue(value);

	/// <summary>
	///     Seeds the property's backing storage with <paramref name="value" />. Called once per property at
	///     initialization or after a scenario reset.
	/// </summary>
	/// <param name="value">The initial value; boxed to <see cref="object" /> because the property type is hidden behind the abstract.</param>
	protected abstract void InitializeValue(object? value);

	/// <summary>
	///     Returns the per-setup override of <see cref="MockBehavior.SkipBaseClass" />, or <see langword="null" />
	///     when the mock-wide behavior should apply.
	/// </summary>
	/// <returns><see langword="true" /> to always skip, <see langword="false" /> to always call, <see langword="null" /> to defer to <see cref="MockBehavior.SkipBaseClass" />.</returns>
	protected abstract bool? GetSkipBaseClass();

	/// <summary>
	///     Returns whether this setup applies to the given <paramref name="propertyAccess" />.
	/// </summary>
	/// <param name="propertyAccess">The recorded access being matched.</param>
	/// <returns><see langword="true" /> when this setup should handle the access.</returns>
	protected abstract bool Matches(PropertyAccess propertyAccess);

	/// <summary>
	///     Runs the setter flow for <paramref name="value" />.
	/// </summary>
	/// <typeparam name="TValue">The property's value type.</typeparam>
	/// <param name="value">The value being assigned.</param>
	/// <param name="behavior">The mock's active behavior.</param>
	protected abstract void InvokeSetter<TValue>(TValue value, MockBehavior behavior);

	/// <summary>
	///     Runs the getter flow and returns the property value.
	/// </summary>
	/// <typeparam name="TResult">The property's value type.</typeparam>
	/// <param name="behavior">The mock's active behavior.</param>
	/// <param name="defaultValueGenerator">Fallback producer used when the setup cannot supply a value.</param>
	/// <returns>The resolved getter value.</returns>
	protected abstract TResult InvokeGetter<TResult>(MockBehavior behavior, Func<TResult> defaultValueGenerator);

	/// <summary>
	///     Allocation-free variant of <see cref="InvokeGetter{TResult}(MockBehavior, Func{TResult})" /> that
	///     accepts a behavior-keyed default-value generator. The source generator passes a <c>static</c>
	///     lambda (cached by the C# compiler) so the proxy property body does not allocate a closure per call.
	/// </summary>
	internal abstract TResult InvokeGetter<TResult>(MockBehavior behavior, Func<MockBehavior, TResult> defaultValueGenerator);

#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal abstract class Default(string name) : PropertySetup
	{
		/// <inheritdoc cref="PropertySetup.Name" />
		public override string Name => name;

		/// <inheritdoc cref="PropertySetup.IsValueInitialized" />
		// Stryker disable once Boolean : Default.InitializeValue is a no-op, so flipping this to false only triggers a redundant re-init call with the same observable outcome.
		internal override bool IsValueInitialized => true;

		/// <inheritdoc cref="PropertySetup.InitializeValue(object?)" />
		protected override void InitializeValue(object? value)
		{
			// Is always initialized from the constructor
		}

		/// <inheritdoc cref="PropertySetup.GetSkipBaseClass()" />
		protected override bool? GetSkipBaseClass()
			=> null;

		/// <inheritdoc cref="PropertySetup.Matches(PropertyAccess)" />
		protected override bool Matches(PropertyAccess propertyAccess)
			=> name.Equals(propertyAccess.Name, StringComparison.Ordinal);
	}

#if !DEBUG
	[DebuggerNonUserCode]
#endif
	internal sealed class Default<T>(string name, T initialValue)
		: Default(name)
	{
		private T _value = initialValue;

		/// <summary>
		///     The current value, for a getter of exactly <typeparamref name="T" /> that avoids the generic virtual
		///     call of <see cref="InvokeGetter{TResult}(MockBehavior, Func{MockBehavior, TResult})" />.
		/// </summary>
		internal T Value
		{
			get => _value;
			set => _value = value;
		}

		/// <inheritdoc cref="PropertySetup.InvokeSetter{TValue}(TValue, MockBehavior)" />
		protected override void InvokeSetter<TValue>(TValue value, MockBehavior behavior)
		{
			if (typeof(TValue) == typeof(T))
			{
				_value = Unsafe.As<TValue, T>(ref value);
			}
			else if (value is null && default(T) is null)
			{
				_value = default!;
			}
			else
			{
				throw new MockException(
					$"The property value only supports '{typeof(T).FormatType()}', but is '{typeof(TValue).FormatType()}'.");
			}
		}

		/// <inheritdoc cref="PropertySetup.InvokeGetter{TResult}(MockBehavior, Func{TResult})" />
		protected override TResult InvokeGetter<TResult>(MockBehavior behavior, Func<TResult> defaultValueGenerator)
		{
			if (typeof(TResult) == typeof(T))
			{
				// Stryker disable once Block : dropping this body falls through to the pattern-match below which produces the same value (via boxing); the fast path exists purely to avoid that boxing.
				return Unsafe.As<T, TResult>(ref _value);
			}

			if (_value is TResult typedValue)
			{
				return typedValue;
			}

			return defaultValueGenerator.Invoke();
		}

		/// <inheritdoc cref="PropertySetup.InvokeGetter{TResult}(MockBehavior, Func{MockBehavior, TResult})" />
		internal override TResult InvokeGetter<TResult>(MockBehavior behavior,
			Func<MockBehavior, TResult> defaultValueGenerator)
		{
			if (typeof(TResult) == typeof(T))
			{
				return Unsafe.As<T, TResult>(ref _value);
			}

			if (_value is TResult typedValue)
			{
				return typedValue;
			}

			return defaultValueGenerator(behavior);
		}
	}
}

/// <summary>
///     Sets up a property.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class PropertySetup<T> : PropertySetup,
	IPropertyGetterSetupCallbackBuilder<T>, IPropertySetterSetupCallbackBuilder<T>,
	IPropertySetupReturnBuilder<T>,
	IPropertyGetterSetup<T>, IPropertySetterSetup<T>,
	IPropertyGetterOnlySetup<T>, IPropertySetterOnlySetup<T>,
	IPropertyGetterOnlyGetterSetup<T>, IPropertySetterOnlySetterSetup<T>,
	IPropertyGetterOnlySetupCallbackBuilder<T>, IPropertySetterOnlySetupCallbackBuilder<T>,
	IPropertyGetterOnlySetupReturnBuilder<T>
{
	private readonly MockRegistry _mockRegistry;
	private readonly string _name;
	private readonly MockolateLock _initializationLock = new();
	private Callbacks<Action<int, T>>? _getterCallbacks;
	private bool _isInitialized;
	private bool _isUserInitialized;
	private Callbacks<Func<int, T, T>>? _returnCallbacks;
	private Callbacks<Action<int, T>>? _setterCallbacks;
	private bool? _skipBaseClass;
	private T _value = default!;

	/// <summary>
	///     Sets up a property.
	/// </summary>
	public PropertySetup(MockRegistry mockRegistry, string name)
	{
		_mockRegistry = mockRegistry;
		_name = name;
	}

	/// <inheritdoc cref="PropertySetup.Name" />
	public override string Name => _name;

	/// <inheritdoc cref="PropertySetup.IsValueInitialized" />
	internal override bool IsValueInitialized => Volatile.Read(ref _isInitialized);

	/// <inheritdoc cref="IPropertyGetterSetupParallelCallbackBuilder{T}.When(Func{int, bool})" />
	IPropertyGetterSetupCallbackWhenBuilder<T> IPropertyGetterSetupParallelCallbackBuilder<T>.When(Func<int, bool> predicate)
	{
		_getterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterSetupCallbackBuilder{T}.InParallel()" />
	IPropertyGetterSetupParallelCallbackBuilder<T> IPropertyGetterSetupCallbackBuilder<T>.InParallel()
	{
		_getterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterSetupCallbackWhenBuilder{T}.For(int)" />
	IPropertyGetterSetupCallbackWhenBuilder<T> IPropertyGetterSetupCallbackWhenBuilder<T>.For(int times)
	{
		_getterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterSetupCallbackWhenBuilder{T}.Only(int)" />
	IPropertySetup<T> IPropertyGetterSetupCallbackWhenBuilder<T>.Only(int times)
	{
		_getterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterSetupParallelCallbackBuilder{T}.When(Func{int, bool})" />
	IPropertySetterSetupCallbackWhenBuilder<T> IPropertySetterSetupParallelCallbackBuilder<T>.When(Func<int, bool> predicate)
	{
		_setterCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterSetupCallbackBuilder{T}.InParallel()" />
	IPropertySetterSetupParallelCallbackBuilder<T> IPropertySetterSetupCallbackBuilder<T>.InParallel()
	{
		_setterCallbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IPropertySetterSetupCallbackWhenBuilder{T}.For(int)" />
	IPropertySetterSetupCallbackWhenBuilder<T> IPropertySetterSetupCallbackWhenBuilder<T>.For(int times)
	{
		_setterCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterSetupCallbackWhenBuilder{T}.Only(int)" />
	IPropertySetup<T> IPropertySetterSetupCallbackWhenBuilder<T>.Only(int times)
	{
		_setterCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetupReturnBuilder{T}.When(Func{int, bool})" />
	IPropertySetupReturnWhenBuilder<T> IPropertySetupReturnBuilder<T>.When(Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertySetupReturnWhenBuilder{T}.For(int)" />
	IPropertySetupReturnWhenBuilder<T> IPropertySetupReturnWhenBuilder<T>.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetupReturnWhenBuilder{T}.Only(int)" />
	IPropertySetup<T> IPropertySetupReturnWhenBuilder<T>.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="PropertySetup.Matches(PropertyAccess)" />
	protected override bool Matches(PropertyAccess propertyAccess)
		=> _name.Equals(propertyAccess.Name, StringComparison.Ordinal);

	/// <inheritdoc cref="PropertySetup.InvokeGetter{TResult}(MockBehavior, Func{TResult})" />
	protected override TResult InvokeGetter<TResult>(MockBehavior behavior, Func<TResult> defaultValueGenerator)
	{
		RunGetterCallbacks();

		if (typeof(TResult) == typeof(T))
		{
			return Unsafe.As<T, TResult>(ref _value);
		}

		if (_value is TResult typedValue)
		{
			return typedValue;
		}

		return defaultValueGenerator();
	}

	/// <inheritdoc cref="PropertySetup.InvokeGetter{TResult}(MockBehavior, Func{MockBehavior, TResult})" />
	internal override TResult InvokeGetter<TResult>(MockBehavior behavior,
		Func<MockBehavior, TResult> defaultValueGenerator)
	{
		RunGetterCallbacks();

		if (typeof(TResult) == typeof(T))
		{
			return Unsafe.As<T, TResult>(ref _value);
		}

		if (_value is TResult typedValue)
		{
			return typedValue;
		}

		return defaultValueGenerator(behavior);
	}

	/// <summary>
	///     Counterpart of <see cref="InvokeGetter{TResult}(MockBehavior, Func{MockBehavior, TResult})" /> for a
	///     getter of exactly <typeparamref name="T" />. It is not virtual, because a generic virtual call is
	///     resolved at runtime on every property read.
	/// </summary>
	internal T InvokeGetterTyped()
	{
		RunGetterCallbacks();
		return _value;
	}

	private void RunGetterCallbacks()
	{
		if (_getterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;
			for (int i = 0; i < _getterCallbacks.Count; i++)
			{
				Callback<Action<int, T>> getterCallback =
					_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];
				if (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, this,
					    static (count, @delegate, self) => @delegate(count, self._value)))
				{
					wasInvoked = true;
				}
			}
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Func<int, T, T>> _ in _returnCallbacks)
			{
				Callback<Func<int, T, T>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, this,
					    static (count, @delegate, self) => @delegate(count, self._value),
					    out T? newValue))
				{
					_value = newValue;
					_isInitialized = true;
					break;
				}
			}
		}
	}

	/// <inheritdoc cref="PropertySetup.InvokeSetter{TValue}(TValue, MockBehavior)" />
	protected override void InvokeSetter<TValue>(TValue value, MockBehavior behavior)
	{
		T newValue;
		if (value is T v)
		{
			newValue = v;
		}
		else if (value is null && default(T) is null)
		{
			newValue = default!;
		}
		else
		{
			throw new MockException(
				$"The property value only supports '{typeof(T).FormatType()}', but is '{typeof(TValue).FormatType()}'.");
		}

		InvokeSetterTyped(newValue);
	}

	/// <summary>
	///     Counterpart of <see cref="InvokeSetter{TValue}(TValue, MockBehavior)" /> for a setter of exactly
	///     <typeparamref name="T" />. It is not virtual, because a generic virtual call is resolved at runtime on
	///     every property write.
	/// </summary>
	internal void InvokeSetterTyped(T newValue)
	{
		if (_setterCallbacks is not null)
		{
			bool wasInvoked = false;
			int currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;
			for (int i = 0; i < _setterCallbacks.Count; i++)
			{
				Callback<Action<int, T>> setterCallback =
					_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];
				if (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, newValue,
					    static (count, @delegate, state) => @delegate(count, state)))
				{
					wasInvoked = true;
				}
			}
		}

		_value = newValue;
	}

	/// <inheritdoc cref="PropertySetup.InitializeValue(object?)" />
	protected override void InitializeValue(object? value)
	{
		// Auto-init path (used by the reader when a setup is registered but
		// not yet user-initialized). A racing user InitializeWith(T) must still
		// win, so we never clobber a value set through the public API.
		lock (_initializationLock)
		{
			if (_isUserInitialized || _isInitialized)
			{
				return;
			}

			if (value is T typedValue)
			{
				_value = typedValue;
			}

			Volatile.Write(ref _isInitialized, value is T or null);
		}
	}

	/// <inheritdoc cref="PropertySetup.GetSkipBaseClass()" />
	protected override bool? GetSkipBaseClass()
		=> _skipBaseClass;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(T).FormatType()} {_name.SubstringAfterLast('.')}";

	#region IPropertySetup<T>

	/// <inheritdoc cref="IPropertySetup{T}.SkippingBaseClass(bool)" />
	public IPropertySetup<T> SkippingBaseClass(bool skipBaseClass = true)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IPropertySetup{T}.Register" />
	public IPropertySetup<T> Register()
		=> this;

	/// <inheritdoc cref="IPropertySetup{T}.InitializeWith(T)" />
	public IPropertySetup<T> InitializeWith(T value)
	{
		// User-explicit init: must win over any racing auto-init performed by a
		// concurrent reader between `Setup.Prop` (which registers an uninitialized
		// setup) and this call. Preserves write-once semantics for repeat user calls.
		lock (_initializationLock)
		{
			if (_isUserInitialized)
			{
				return this;
			}

			_value = value;
			// Stryker disable once Boolean : flipping this to false only leaves IsValueInitialized wrong for one redundant auto-init cycle that is no-op'd by the _isUserInitialized guard on the next line.
			Volatile.Write(ref _isInitialized, value: true);
			_isUserInitialized = true;
		}

		return this;
	}

	/// <inheritdoc cref="IPropertySetup{T}.OnGet" />
	public IPropertyGetterSetup<T> OnGet
		=> this;

	/// <inheritdoc cref="IPropertyGetterSetup{T}.Do(Action)" />
	IPropertyGetterSetupCallbackBuilder<T> IPropertyGetterSetup<T>.Do(Action callback)
	{
		Callback<Action<int, T>> item = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(item);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T currentValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IPropertyGetterSetup{T}.Do(Action{T})" />
	IPropertyGetterSetupCallbackBuilder<T> IPropertyGetterSetup<T>.Do(Action<T> callback)
	{
		Callback<Action<int, T>> item = new(Delegate);
		_getterCallbacks = _getterCallbacks.Register(item);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T v)
		{
			callback(v);
		}
	}

	/// <inheritdoc cref="IPropertyGetterSetup{T}.Do(Action{int, T})" />
	IPropertyGetterSetupCallbackBuilder<T> IPropertyGetterSetup<T>.Do(Action<int, T> callback)
	{
		Callback<Action<int, T>> item = new(callback);
		_getterCallbacks = _getterCallbacks.Register(item);
		return this;
	}

	IPropertySetterSetupParallelCallbackBuilder<T> IPropertySetterSetup<T>.TransitionTo(string scenario)
	{
		Callback<Action<int, T>> item = new((_, _) => _mockRegistry.TransitionTo(scenario));
		item.InParallel();
		_setterCallbacks = _setterCallbacks.Register(item);
		return this;
	}

	IPropertyGetterSetupParallelCallbackBuilder<T> IPropertyGetterSetup<T>.TransitionTo(string scenario)
	{
		Callback<Action<int, T>> item = new((_, _) => _mockRegistry.TransitionTo(scenario));
		item.InParallel();
		_getterCallbacks = _getterCallbacks.Register(item);
		return this;
	}

	/// <inheritdoc cref="IPropertySetup{T}.OnSet" />
	public IPropertySetterSetup<T> OnSet
		=> this;

	/// <inheritdoc cref="IPropertySetterSetup{T}.Do(Action)" />
	IPropertySetterSetupCallbackBuilder<T> IPropertySetterSetup<T>.Do(Action callback)
	{
		Callback<Action<int, T>> item = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(item);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T newValue)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IPropertySetterSetup{T}.Do(Action{T})" />
	IPropertySetterSetupCallbackBuilder<T> IPropertySetterSetup<T>.Do(Action<T> callback)
	{
		Callback<Action<int, T>> item = new(Delegate);
		_setterCallbacks = _setterCallbacks.Register(item);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T newValue)
		{
			callback(newValue);
		}
	}

	/// <inheritdoc cref="IPropertySetterSetup{T}.Do(Action{int, T})" />
	IPropertySetterSetupCallbackBuilder<T> IPropertySetterSetup<T>.Do(Action<int, T> callback)
	{
		Callback<Action<int, T>> item = new(callback);
		_setterCallbacks = _setterCallbacks.Register(item);
		return this;
	}

	/// <inheritdoc cref="IPropertySetup{T}.Returns(T)" />
	public IPropertySetupReturnBuilder<T> Returns(T returnValue)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T currentValue)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Returns(Func{T})" />
	public IPropertySetupReturnBuilder<T> Returns(Func<T> callback)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T currentValue)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Returns(Func{T, T})" />
	public IPropertySetupReturnBuilder<T> Returns(Func<T, T> callback)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T p)
		{
			return callback(p);
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Throws{TException}()" />
	public IPropertySetupReturnBuilder<T> Throws<TException>()
		where TException : Exception, new()
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T currentValue)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Throws(Exception)" />
	public IPropertySetupReturnBuilder<T> Throws(Exception exception)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T currentValue)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Throws(Func{Exception})" />
	public IPropertySetupReturnBuilder<T> Throws(Func<Exception> callback)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T currentValue)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IPropertySetup{T}.Throws(Func{T, Exception})" />
	public IPropertySetupReturnBuilder<T> Throws(Func<T, Exception> callback)
	{
		Callback<Func<int, T, T>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		T Delegate(int _, T p)
		{
			throw callback(p);
		}
	}

	#endregion IPropertySetup<T>

	#region Accessor-restricted views

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.SkippingBaseClass(bool)" />
	IPropertyGetterOnlySetup<T> IPropertyGetterOnlySetup<T>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Register()" />
	IPropertyGetterOnlySetup<T> IPropertyGetterOnlySetup<T>.Register()
	{
		Register();
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.InitializeWith(T)" />
	IPropertyGetterOnlySetup<T> IPropertyGetterOnlySetup<T>.InitializeWith(T value)
	{
		InitializeWith(value);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.OnGet" />
	IPropertyGetterOnlyGetterSetup<T> IPropertyGetterOnlySetup<T>.OnGet
		=> this;

	/// <inheritdoc cref="IPropertyGetterOnlyGetterSetup{T}.Do(Action)" />
	IPropertyGetterOnlySetupCallbackBuilder<T> IPropertyGetterOnlyGetterSetup<T>.Do(Action callback)
	{
		((IPropertyGetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlyGetterSetup{T}.Do(Action{T})" />
	IPropertyGetterOnlySetupCallbackBuilder<T> IPropertyGetterOnlyGetterSetup<T>.Do(Action<T> callback)
	{
		((IPropertyGetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlyGetterSetup{T}.Do(Action{int, T})" />
	IPropertyGetterOnlySetupCallbackBuilder<T> IPropertyGetterOnlyGetterSetup<T>.Do(Action<int, T> callback)
	{
		((IPropertyGetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlyGetterSetup{T}.TransitionTo(string)" />
	IPropertyGetterOnlySetupParallelCallbackBuilder<T> IPropertyGetterOnlyGetterSetup<T>.TransitionTo(string scenario)
	{
		((IPropertyGetterSetup<T>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupCallbackBuilder{T}.InParallel()" />
	IPropertyGetterOnlySetupParallelCallbackBuilder<T> IPropertyGetterOnlySetupCallbackBuilder<T>.InParallel()
	{
		((IPropertyGetterSetupCallbackBuilder<T>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupParallelCallbackBuilder{T}.When(Func{int, bool})" />
	IPropertyGetterOnlySetupCallbackWhenBuilder<T> IPropertyGetterOnlySetupParallelCallbackBuilder<T>.When(
		Func<int, bool> predicate)
	{
		((IPropertyGetterSetupParallelCallbackBuilder<T>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupCallbackWhenBuilder{T}.For(int)" />
	IPropertyGetterOnlySetupCallbackWhenBuilder<T> IPropertyGetterOnlySetupCallbackWhenBuilder<T>.For(int times)
	{
		((IPropertyGetterSetupCallbackWhenBuilder<T>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupCallbackWhenBuilder{T}.Only(int)" />
	IPropertyGetterOnlySetup<T> IPropertyGetterOnlySetupCallbackWhenBuilder<T>.Only(int times)
	{
		((IPropertyGetterSetupCallbackWhenBuilder<T>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Returns(T)" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Returns(T returnValue)
	{
		Returns(returnValue);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Returns(Func{T})" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Returns(Func<T> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Returns(Func{T, T})" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Returns(Func<T, T> callback)
	{
		Returns(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Throws{TException}()" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Throws<TException>()
	{
		Throws<TException>();
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Throws(Exception)" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Throws(Exception exception)
	{
		Throws(exception);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Throws(Func{Exception})" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Throws(Func<Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetup{T}.Throws(Func{T, Exception})" />
	IPropertyGetterOnlySetupReturnBuilder<T> IPropertyGetterOnlySetup<T>.Throws(Func<T, Exception> callback)
	{
		Throws(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupReturnBuilder{T}.When(Func{int, bool})" />
	IPropertyGetterOnlySetupReturnWhenBuilder<T> IPropertyGetterOnlySetupReturnBuilder<T>.When(Func<int, bool> predicate)
	{
		((IPropertySetupReturnBuilder<T>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupReturnWhenBuilder{T}.For(int)" />
	IPropertyGetterOnlySetupReturnWhenBuilder<T> IPropertyGetterOnlySetupReturnWhenBuilder<T>.For(int times)
	{
		((IPropertySetupReturnWhenBuilder<T>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertyGetterOnlySetupReturnWhenBuilder{T}.Only(int)" />
	IPropertyGetterOnlySetup<T> IPropertyGetterOnlySetupReturnWhenBuilder<T>.Only(int times)
	{
		((IPropertySetupReturnWhenBuilder<T>)this).Only(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetup{T}.SkippingBaseClass(bool)" />
	IPropertySetterOnlySetup<T> IPropertySetterOnlySetup<T>.SkippingBaseClass(bool skipBaseClass)
	{
		SkippingBaseClass(skipBaseClass);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetup{T}.Register()" />
	IPropertySetterOnlySetup<T> IPropertySetterOnlySetup<T>.Register()
	{
		Register();
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetup{T}.OnSet" />
	IPropertySetterOnlySetterSetup<T> IPropertySetterOnlySetup<T>.OnSet
		=> this;

	/// <inheritdoc cref="IPropertySetterOnlySetterSetup{T}.Do(Action)" />
	IPropertySetterOnlySetupCallbackBuilder<T> IPropertySetterOnlySetterSetup<T>.Do(Action callback)
	{
		((IPropertySetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetterSetup{T}.Do(Action{T})" />
	IPropertySetterOnlySetupCallbackBuilder<T> IPropertySetterOnlySetterSetup<T>.Do(Action<T> callback)
	{
		((IPropertySetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetterSetup{T}.Do(Action{int, T})" />
	IPropertySetterOnlySetupCallbackBuilder<T> IPropertySetterOnlySetterSetup<T>.Do(Action<int, T> callback)
	{
		((IPropertySetterSetup<T>)this).Do(callback);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetterSetup{T}.TransitionTo(string)" />
	IPropertySetterOnlySetupParallelCallbackBuilder<T> IPropertySetterOnlySetterSetup<T>.TransitionTo(string scenario)
	{
		((IPropertySetterSetup<T>)this).TransitionTo(scenario);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetupCallbackBuilder{T}.InParallel()" />
	IPropertySetterOnlySetupParallelCallbackBuilder<T> IPropertySetterOnlySetupCallbackBuilder<T>.InParallel()
	{
		((IPropertySetterSetupCallbackBuilder<T>)this).InParallel();
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetupParallelCallbackBuilder{T}.When(Func{int, bool})" />
	IPropertySetterOnlySetupCallbackWhenBuilder<T> IPropertySetterOnlySetupParallelCallbackBuilder<T>.When(
		Func<int, bool> predicate)
	{
		((IPropertySetterSetupParallelCallbackBuilder<T>)this).When(predicate);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetupCallbackWhenBuilder{T}.For(int)" />
	IPropertySetterOnlySetupCallbackWhenBuilder<T> IPropertySetterOnlySetupCallbackWhenBuilder<T>.For(int times)
	{
		((IPropertySetterSetupCallbackWhenBuilder<T>)this).For(times);
		return this;
	}

	/// <inheritdoc cref="IPropertySetterOnlySetupCallbackWhenBuilder{T}.Only(int)" />
	IPropertySetterOnlySetup<T> IPropertySetterOnlySetupCallbackWhenBuilder<T>.Only(int times)
	{
		((IPropertySetterSetupCallbackWhenBuilder<T>)this).Only(times);
		return this;
	}

	#endregion Accessor-restricted views
}
