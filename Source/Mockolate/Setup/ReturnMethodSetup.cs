using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Sets up a method returning <typeparamref name="TReturn" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class ReturnMethodSetup<TReturn>
	: MethodSetup, IReturnMethodSetupCallbackBuilder<TReturn>, IReturnMethodSetupReturnBuilder<TReturn>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int>>? _callbacks;
	private Callbacks<Func<int, TReturn>>? _returnCallbacks;
	private TReturn _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, TReturn>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TReturn returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, TReturn>>((_) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}" />
	protected ReturnMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.SkippingBaseClass(bool)" />
	IReturnMethodSetup<TReturn> IReturnMethodSetup<TReturn>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Do(Action)" />
	IReturnMethodSetupCallbackBuilder<TReturn> IReturnMethodSetup<TReturn>.Do(Action callback)
	{
		Callback<Action<int>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Do(Action{int})" />
	IReturnMethodSetupCallbackBuilder<TReturn> IReturnMethodSetup<TReturn>.Do(Action<int> callback)
	{
		Callback<Action<int>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.TransitionTo(string)" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn> IReturnMethodSetup<TReturn>.TransitionTo(string scenario)
	{
		Callback<Action<int>> currentCallback = new(_ => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Returns(Func{TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn> IReturnMethodSetup<TReturn>.Returns(Func<TReturn> callback)
	{
		Callback<Func<int, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Returns(TReturn)" />
	IReturnMethodSetupReturnBuilder<TReturn> IReturnMethodSetup<TReturn>.Returns(TReturn returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Throws{TException}()" />
	IReturnMethodSetupReturnBuilder<TReturn> IReturnMethodSetup<TReturn>.Throws<TException>()
	{
		Callback<Func<int, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Throws(Exception)" />
	IReturnMethodSetupReturnBuilder<TReturn> IReturnMethodSetup<TReturn>.Throws(Exception exception)
	{
		Callback<Func<int, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn}.Throws(Func{Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn> IReturnMethodSetup<TReturn>.Throws(Func<Exception> callback)
	{
		Callback<Func<int, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackBuilder{TReturn}.InParallel()" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn> IReturnMethodSetupCallbackBuilder<TReturn>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn> IReturnMethodSetupParallelCallbackBuilder<TReturn>.When(
		Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn}.For(int)" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn> IReturnMethodSetupCallbackWhenBuilder<TReturn>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn}.Only(int)" />
	IReturnMethodSetup<TReturn> IReturnMethodSetupCallbackWhenBuilder<TReturn>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnBuilder{TReturn}.When(Func{int, bool})" />
	IReturnMethodSetupReturnWhenBuilder<TReturn> IReturnMethodSetupReturnBuilder<TReturn>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn}.For(int)" />
	IReturnMethodSetupReturnWhenBuilder<TReturn> IReturnMethodSetupReturnWhenBuilder<TReturn>.For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn}.Only(int)" />
	IReturnMethodSetup<TReturn> IReturnMethodSetupReturnWhenBuilder<TReturn>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is MethodInvocation;

	/// <summary>
	///     Flag indicating, if any return callbacks have been registered on this setup.
	/// </summary>
	public bool HasReturnCallbacks => _hasReturnValue || _returnCallbacks is { Count: > 0, };

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Tries to get the registered return value if any is set up.
	/// </summary>
	public bool TryGetReturnValue(out TReturn returnValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			returnValue = _returnValue;
			return true;
		}

		if (_returnCallbacks != null)
		{
			foreach (Callback<Func<int, TReturn>> _ in _returnCallbacks)
			{
				Callback<Func<int, TReturn>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke<object?, TReturn>(ref _returnCallbacks.CurrentIndex, null,
					    static (count, @delegate, _) => @delegate(count),
					    out TReturn? newValue))
				{
					returnValue = newValue;
					return true;
				}
			}
		}

		returnValue = default!;
		return false;
	}

	/// <summary>
	///     Check if the setup matches the specified parameters.
	/// </summary>
	public abstract bool Matches();

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup without parameters.
	/// </summary>
	public virtual void TriggerCallbacks()
	{
		bool wasInvoked = false;
		if (_callbacks is not null)
		{
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int>> callback =
					_callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke<object?>(wasInvoked, ref _callbacks.CurrentIndex, null,
					    static (count, @delegate, _) => @delegate(count)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : ReturnMethodSetup<TReturn>
	{
		/// <inheritdoc cref="ReturnMethodSetup{TReturn}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name)
			: base(mockRegistry, name)
		{
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn}.Matches()" />
		public override bool Matches()
			=> true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}()";
	}
}

/// <summary>
///     Setup for a method with one parameter <typeparamref name="T1" /> returning <typeparamref name="TReturn" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class ReturnMethodSetup<TReturn, T1> : MethodSetup,
	IReturnMethodSetupWithCallback<TReturn, T1>,
	IReturnMethodSetupCallbackBuilder<TReturn, T1>, IReturnMethodSetupReturnBuilder<TReturn, T1>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1>>? _callbacks;
	private Callbacks<Func<int, T1, TReturn>>? _returnCallbacks;
	private TReturn _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, TReturn>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TReturn returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, TReturn>>((_, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}" />
	protected ReturnMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.SkippingBaseClass(bool)" />
	IReturnMethodSetup<TReturn, T1> IReturnMethodSetup<TReturn, T1>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Do(Action)" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Do(Action callback)
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1}.Do(Action{T1})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1> IReturnMethodSetupWithCallback<TReturn, T1>.Do(Action<T1> callback)
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			callback(p1);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1}.Do(Action{int, T1})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1> IReturnMethodSetupWithCallback<TReturn, T1>.Do(Action<int, T1> callback)
	{
		Callback<Action<int, T1>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.TransitionTo(string)" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1>> currentCallback = new((_, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1}.Returns(Func{T1, TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetupWithCallback<TReturn, T1>.Returns(Func<T1, TReturn> callback)
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			return callback(p1);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Returns(Func{TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Returns(Func<TReturn> callback)
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Returns(TReturn)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Returns(TReturn returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Throws{TException}()" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Throws<TException>()
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Throws(Exception)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Throws(Exception exception)
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1}.Throws(Func{Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetup<TReturn, T1>.Throws(Func<Exception> callback)
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1}.Throws(Func{T1, Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1> IReturnMethodSetupWithCallback<TReturn, T1>.Throws(Func<T1, Exception> callback)
	{
		Callback<Func<int, T1, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1)
		{
			throw callback(p1);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackBuilder{TReturn, T1}.InParallel()" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1> IReturnMethodSetupCallbackBuilder<TReturn, T1>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupParallelCallbackBuilder{TReturn, T1}.When(Func{int, bool})" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1> IReturnMethodSetupParallelCallbackBuilder<TReturn, T1>.When(
		Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1}.For(int)" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1}.Only(int)" />
	IReturnMethodSetup<TReturn, T1> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnBuilder{TReturn, T1}.When(Func{int, bool})" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1> IReturnMethodSetupReturnBuilder<TReturn, T1>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1}.For(int)" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1> IReturnMethodSetupReturnWhenBuilder<TReturn, T1>.For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1}.Only(int)" />
	IReturnMethodSetup<TReturn, T1> IReturnMethodSetupReturnWhenBuilder<TReturn, T1>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
	{
		if (interaction is MethodInvocation<T1> invocation)
		{
			return Matches(invocation.Parameter1);
		}

		return false;
	}

	/// <summary>
	///     Flag indicating, if any return callbacks have been registered on this setup.
	/// </summary>
	public bool HasReturnCallbacks => _hasReturnValue || _returnCallbacks is { Count: > 0, };

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Tries to get the registered return value if any is set up for the specified parameter value <paramref name="p1" />.
	/// </summary>
	public bool TryGetReturnValue(T1 p1, out TReturn returnValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			returnValue = _returnValue;
			return true;
		}

		if (_returnCallbacks != null)
		{
			foreach (Callback<Func<int, T1, TReturn>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, TReturn>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, p1,
					    static (count, @delegate, state) => @delegate(count, state),
					    out TReturn? newValue))
				{
					returnValue = newValue;
					return true;
				}
			}
		}

		returnValue = default!;
		return false;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter value <paramref name="p1Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified <paramref name="parameter1" />.
	/// </summary>
	public virtual void TriggerCallbacks(T1 parameter1)
	{
		if (_callbacks is not null)
		{
			bool wasInvoked = false;
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int, T1>> callback =
					_callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke(wasInvoked, ref _callbacks.CurrentIndex, parameter1,
					    static (count, @delegate, state) => @delegate(count, state)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : ReturnMethodSetup<TReturn, T1>
	{
		private readonly string _parameterName1;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}" />
		public WithParameters(MockRegistry mockRegistry, string name, IParameters parameters, string parameterName1)
			: base(mockRegistry, name)
		{
			Parameters = parameters;
			_parameterName1 = parameterName1;
		}

		/// <summary>
		///     The parameters match for the method.
		/// </summary>
		private IParameters Parameters { get; }

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : ReturnMethodSetup<TReturn, T1>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name, IParameterMatch<T1> parameter1)
			: base(mockRegistry, name)
		{
			Parameter1 = parameter1;
		}

		/// <summary>
		///     The single parameter of the method.
		/// </summary>
		public IParameterMatch<T1> Parameter1 { get; }

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1> IReturnMethodSetupParameterIgnorer<TReturn, T1>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> _matchAnyParameters || Parameter1.Matches(p1Value);

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}.TriggerCallbacks(T1)" />
		public override void TriggerCallbacks(T1 parameter1)
		{
			Parameter1.InvokeCallbacks(parameter1);
			base.TriggerCallbacks(parameter1);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameter1})";
	}

	/// <summary>
	///     Setup for a method with one parameter where the expected value is stored directly on the
	///     setup, avoiding the per-call <see cref="IParameterMatch{T1}" /> allocation that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : ReturnMethodSetup<TReturn, T1>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1>
	{
		private readonly T1 _value1;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name, T1 value1)
			: base(mockRegistry, name)
		{
			_value1 = value1;
		}

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1> IReturnMethodSetupParameterIgnorer<TReturn, T1>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> _matchAnyParameters || EqualityComparer<T1>.Default.Equals(_value1, p1Value);

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({FormatLiteralValue(_value1)})";
	}
}

/// <summary>
///     Setup for a method with two parameters <typeparamref name="T1" /> and <typeparamref name="T2" /> returning
///     <typeparamref name="TReturn" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class ReturnMethodSetup<TReturn, T1, T2> : MethodSetup,
	IReturnMethodSetupWithCallback<TReturn, T1, T2>,
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2>, IReturnMethodSetupReturnBuilder<TReturn, T1, T2>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2>>? _callbacks;
	private Callbacks<Func<int, T1, T2, TReturn>>? _returnCallbacks;
	private TReturn _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, TReturn>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TReturn returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, TReturn>>((_, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}" />
	protected ReturnMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.SkippingBaseClass(bool)" />
	IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Do(Action)" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Do(Action callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2}.Do(Action{T1, T2})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> IReturnMethodSetupWithCallback<TReturn, T1, T2>.Do(Action<T1, T2> callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2}.Do(Action{int, T1, T2})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> IReturnMethodSetupWithCallback<TReturn, T1, T2>.Do(
		Action<int, T1, T2> callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.TransitionTo(string)" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2>> currentCallback = new((_, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2}.Returns(Func{T1, T2, TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetupWithCallback<TReturn, T1, T2>.Returns(
		Func<T1, T2, TReturn> callback)
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			return callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Returns(Func{TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Returns(Func<TReturn> callback)
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Returns(TReturn)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Returns(TReturn returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Throws{TException}()" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Throws<TException>()
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Throws(Exception)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Throws(Exception exception)
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2}.Throws(Func{Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetup<TReturn, T1, T2>.Throws(
		Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2}.Throws(Func{T1, T2, Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> IReturnMethodSetupWithCallback<TReturn, T1, T2>.Throws(
		Func<T1, T2, Exception> callback)
	{
		Callback<Func<int, T1, T2, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2)
		{
			throw callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackBuilder{TReturn, T1, T2}.InParallel()" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2> IReturnMethodSetupCallbackBuilder<TReturn, T1, T2>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupParallelCallbackBuilder{TReturn, T1, T2}.When(Func{int, bool})" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2> IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2>.When(
		Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2}.For(int)" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2>.
		For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2}.When(Func{int, bool})" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2> IReturnMethodSetupReturnBuilder<TReturn, T1, T2>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2}.For(int)" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2> IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2>.
		For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
	{
		if (interaction is MethodInvocation<T1, T2> invocation)
		{
			return Matches(invocation.Parameter1, invocation.Parameter2);
		}

		return false;
	}

	/// <summary>
	///     Flag indicating, if any return callbacks have been registered on this setup.
	/// </summary>
	public bool HasReturnCallbacks => _hasReturnValue || _returnCallbacks is { Count: > 0, };

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Tries to get the registered return value if any is set up for the specified parameter values.
	/// </summary>
	public bool TryGetReturnValue(T1 p1, T2 p2, out TReturn returnValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			returnValue = _returnValue;
			return true;
		}

		if (_returnCallbacks != null)
		{
			foreach (Callback<Func<int, T1, T2, TReturn>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, TReturn>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2),
					    out TReturn? newValue))
				{
					returnValue = newValue;
					return true;
				}
			}
		}

		returnValue = default!;
		return false;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" /> and
	///     <paramref name="p2Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified parameters.
	/// </summary>
	public virtual void TriggerCallbacks(T1 parameter1, T2 parameter2)
	{
		if (_callbacks is not null)
		{
			bool wasInvoked = false;
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int, T1, T2>> callback =
					_callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke(wasInvoked, ref _callbacks.CurrentIndex, (parameter1, parameter2),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with two parameters matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : ReturnMethodSetup<TReturn, T1, T2>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}" />
		public WithParameters(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2)
			: base(mockRegistry, name)
		{
			Parameters = parameters;
			_parameterName1 = parameterName1;
			_parameterName2 = parameterName2;
		}

		/// <summary>
		///     The parameters match for the method.
		/// </summary>
		private IParameters Parameters { get; }

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with two parameters matching against individual typed parameters.
	/// </summary>
	public class WithParameterCollection : ReturnMethodSetup<TReturn, T1, T2>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name, IParameterMatch<T1> parameter1,
			IParameterMatch<T2> parameter2)
			: base(mockRegistry, name)
		{
			Parameter1 = parameter1;
			Parameter2 = parameter2;
		}

		/// <summary>
		///     The first parameter of the method.
		/// </summary>
		public IParameterMatch<T1> Parameter1 { get; }

		/// <summary>
		///     The second parameter of the method.
		/// </summary>
		public IParameterMatch<T2> Parameter2 { get; }

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> _matchAnyParameters ||
			   (Parameter1.Matches(p1Value) &&
			    Parameter2.Matches(p2Value));

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}.TriggerCallbacks(T1, T2)" />
		public override void TriggerCallbacks(T1 parameter1, T2 parameter2)
		{
			Parameter1.InvokeCallbacks(parameter1);
			Parameter2.InvokeCallbacks(parameter2);
			base.TriggerCallbacks(parameter1, parameter2);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameter1}, {Parameter2})";
	}

	/// <summary>
	///     Setup for a method with two parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T1}" /> /
	///     <see cref="IParameterMatch{T2}" /> allocations that <see cref="WithParameterCollection" />
	///     requires.
	/// </summary>
	public class WithLiteralValues : ReturnMethodSetup<TReturn, T1, T2>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name, T1 value1, T2 value2)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
		}

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)})";
	}
}

/// <summary>
///     Setup for a method with three parameters <typeparamref name="T1" />, <typeparamref name="T2" /> and
///     <typeparamref name="T3" /> returning <typeparamref name="TReturn" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class ReturnMethodSetup<TReturn, T1, T2, T3> : MethodSetup,
	IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>,
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3>, IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2, T3>>? _callbacks;
	private Callbacks<Func<int, T1, T2, T3, TReturn>>? _returnCallbacks;
	private TReturn _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, T3, TReturn>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TReturn returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, T3, TReturn>>((_, _, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}" />
	protected ReturnMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.SkippingBaseClass(bool)" />
	IReturnMethodSetup<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.SkippingBaseClass(
		bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Do(Action)" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Do(Action callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3}.Do(Action{T1, T2, T3})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>.Do(
		Action<T1, T2, T3> callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3}.Do(Action{int, T1, T2, T3})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>.Do(
		Action<int, T1, T2, T3> callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.TransitionTo(string)" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new((_, _, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3}.Returns(Func{T1, T2, T3, TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>.Returns(
		Func<T1, T2, T3, TReturn> callback)
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			return callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Returns(Func{TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Returns(
		Func<TReturn> callback)
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Returns(TReturn)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Returns(
		TReturn returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Throws{TException}()" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Throws<TException>()
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Throws(Exception)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Throws(
		Exception exception)
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3}.Throws(Func{Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetup<TReturn, T1, T2, T3>.Throws(
		Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3}.Throws(Func{T1, T2, T3, Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>.Throws(
		Func<T1, T2, T3, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackBuilder{TReturn, T1, T2, T3}.InParallel()" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3> IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3>.
		InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupParallelCallbackBuilder{TReturn, T1, T2, T3}.When(Func{int, bool})" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3> IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3>.
		When(Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2, T3}.For(int)" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3>
		IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2, T3}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2, T3> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3}.When(Func{int, bool})" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3> IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2, T3}.For(int)" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3> IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3>.
		For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2, T3}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2, T3> IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3>.Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
	{
		if (interaction is MethodInvocation<T1, T2, T3> invocation)
		{
			return Matches(invocation.Parameter1, invocation.Parameter2, invocation.Parameter3);
		}

		return false;
	}

	/// <summary>
	///     Flag indicating, if any return callbacks have been registered on this setup.
	/// </summary>
	public bool HasReturnCallbacks => _hasReturnValue || _returnCallbacks is { Count: > 0, };

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Tries to get the registered return value if any is set up for the specified parameter values.
	/// </summary>
	public bool TryGetReturnValue(T1 p1, T2 p2, T3 p3, out TReturn returnValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			returnValue = _returnValue;
			return true;
		}

		if (_returnCallbacks != null)
		{
			foreach (Callback<Func<int, T1, T2, T3, TReturn>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, T3, TReturn>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2, p3),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3),
					    out TReturn? newValue))
				{
					returnValue = newValue;
					return true;
				}
			}
		}

		returnValue = default!;
		return false;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" />,
	///     <paramref name="p2Value" /> and <paramref name="p3Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value, T3 p3Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified parameters.
	/// </summary>
	public virtual void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3)
	{
		if (_callbacks is not null)
		{
			bool wasInvoked = false;
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3>> callback =
					_callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke(wasInvoked, ref _callbacks.CurrentIndex, (parameter1, parameter2, parameter3),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2, state.parameter3)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with three parameters matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : ReturnMethodSetup<TReturn, T1, T2, T3>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;
		private readonly string _parameterName3;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}" />
		public WithParameters(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2, string parameterName3)
			: base(mockRegistry, name)
		{
			Parameters = parameters;
			_parameterName1 = parameterName1;
			_parameterName2 = parameterName2;
			_parameterName3 = parameterName3;
		}

		/// <summary>
		///     The parameters match for the method.
		/// </summary>
		private IParameters Parameters { get; }

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value, p3Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value), (_parameterName3, p3Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with three parameters matching against individual typed parameters.
	/// </summary>
	public class WithParameterCollection : ReturnMethodSetup<TReturn, T1, T2, T3>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}" />
		public WithParameterCollection(
			MockRegistry mockRegistry,
			string name,
			IParameterMatch<T1> parameter1,
			IParameterMatch<T2> parameter2,
			IParameterMatch<T3> parameter3)
			: base(mockRegistry, name)
		{
			Parameter1 = parameter1;
			Parameter2 = parameter2;
			Parameter3 = parameter3;
		}

		/// <summary>
		///     The first parameter of the method.
		/// </summary>
		public IParameterMatch<T1> Parameter1 { get; }

		/// <summary>
		///     The second parameter of the method.
		/// </summary>
		public IParameterMatch<T2> Parameter2 { get; }

		/// <summary>
		///     The third parameter of the method.
		/// </summary>
		public IParameterMatch<T3> Parameter3 { get; }

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2, T3}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2, T3> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> _matchAnyParameters ||
			   (Parameter1.Matches(p1Value) &&
			    Parameter2.Matches(p2Value) &&
			    Parameter3.Matches(p3Value));

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}.TriggerCallbacks(T1, T2, T3)" />
		public override void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3)
		{
			Parameter1.InvokeCallbacks(parameter1);
			Parameter2.InvokeCallbacks(parameter2);
			Parameter3.InvokeCallbacks(parameter3);
			base.TriggerCallbacks(parameter1, parameter2, parameter3);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameter1}, {Parameter2}, {Parameter3})";
	}

	/// <summary>
	///     Setup for a method with three parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T}" /> allocations that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : ReturnMethodSetup<TReturn, T1, T2, T3>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private readonly T3 _value3;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name, T1 value1, T2 value2, T3 value3)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
			_value3 = value3;
		}

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2, T3}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2, T3> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value) &&
			    EqualityComparer<T3>.Default.Equals(_value3, p3Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)}, {FormatLiteralValue(_value3)})";
	}
}

#pragma warning disable S2436 // Types and methods should not have too many generic parameters
/// <summary>
///     Setup for a method with four parameters <typeparamref name="T1" />, <typeparamref name="T2" />,
///     <typeparamref name="T3" /> and <typeparamref name="T4" /> returning <typeparamref name="TReturn" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class ReturnMethodSetup<TReturn, T1, T2, T3, T4> : MethodSetup,
	IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>,
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4>,
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2, T3, T4>>? _callbacks;
	private Callbacks<Func<int, T1, T2, T3, T4, TReturn>>? _returnCallbacks;
	private TReturn _returnValue = default!;
	private bool _hasReturnValue;
	private int _returnValueCount;
	private bool? _skipBaseClass;

	/// <summary>
	///     The return callbacks, after turning a directly stored return value into a regular callback.
	/// </summary>
	private Callbacks<Func<int, T1, T2, T3, T4, TReturn>>? ReturnCallbacks
	{
		get
		{
			if (_hasReturnValue)
			{
				TReturn returnValue = _returnValue;
				_hasReturnValue = false;
				_returnValue = default!;
				_returnCallbacks = _returnCallbacks.Register(
					new Callback<Func<int, T1, T2, T3, T4, TReturn>>((_, _, _, _, _) => returnValue), _returnValueCount);
			}

			return _returnCallbacks;
		}
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}" />
	protected ReturnMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.SkippingBaseClass(
		bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Do(Action)" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.Do(
		Action callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>.Do(
		Action<T1, T2, T3, T4> callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_callbacks = _callbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4})" />
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>.Do(
		Action<int, T1, T2, T3, T4> callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.TransitionTo(string)" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.TransitionTo(
		string scenario)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new((_, _, _, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3, T4}.Returns(Func{T1, T2, T3, T4, TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>.Returns(
		Func<T1, T2, T3, T4, TReturn> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			return callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Returns(Func{TReturn})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.Returns(
		Func<TReturn> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			return callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Returns(TReturn)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.Returns(
		TReturn returnValue)
	{
		if (!_hasReturnValue && _returnCallbacks is null)
		{
			_returnValue = returnValue;
			_hasReturnValue = true;
			return this;
		}

		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			return returnValue;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Throws{TException}()" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.
		Throws<TException>()
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Throws(Exception)" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.Throws(
		Exception exception)
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}.Throws(Func{Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetup<TReturn, T1, T2, T3, T4>.Throws(
		Func<Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupWithCallback{TReturn, T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, Exception})" />
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>.Throws(
		Func<T1, T2, T3, T4, Exception> callback)
	{
		Callback<Func<int, T1, T2, T3, T4, TReturn>> currentCallback = new(Delegate);
		_returnCallbacks = ReturnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		TReturn Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackBuilder{TReturn, T1, T2, T3, T4}.InParallel()" />
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4>
		IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupParallelCallbackBuilder{TReturn, T1, T2, T3, T4}.When(Func{int, bool})" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4>
		IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4>.When(Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2, T3, T4}.For(int)" />
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4>
		IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4>.
		For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupCallbackWhenBuilder{TReturn, T1, T2, T3, T4}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4>.
		Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3, T4}.When(Func{int, bool})" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4>
		IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4>.When(Func<int, bool> predicate)
	{
		ReturnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2, T3, T4}.For(int)" />
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4>
		IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4>.
		For(int times)
	{
		ReturnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IReturnMethodSetupReturnWhenBuilder{TReturn, T1, T2, T3, T4}.Only(int)" />
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4>.
		Only(int times)
	{
		ReturnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
	{
		if (interaction is MethodInvocation<T1, T2, T3, T4> invocation)
		{
			return Matches(invocation.Parameter1, invocation.Parameter2, invocation.Parameter3, invocation.Parameter4);
		}

		return false;
	}

	/// <summary>
	///     Flag indicating, if any return callbacks have been registered on this setup.
	/// </summary>
	public bool HasReturnCallbacks => _hasReturnValue || _returnCallbacks is { Count: > 0, };

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Tries to get the registered return value if any is set up for the specified parameter values.
	/// </summary>
	public bool TryGetReturnValue(T1 p1, T2 p2, T3 p3, T4 p4, out TReturn returnValue)
	{
		if (_hasReturnValue)
		{
			_returnValueCount++;
			returnValue = _returnValue;
			return true;
		}

		if (_returnCallbacks != null)
		{
			foreach (Callback<Func<int, T1, T2, T3, T4, TReturn>> _ in _returnCallbacks)
			{
				Callback<Func<int, T1, T2, T3, T4, TReturn>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (p1, p2, p3, p4),
					    static (count, @delegate, state) => @delegate(count, state.p1, state.p2, state.p3, state.p4),
					    out TReturn? newValue))
				{
					returnValue = newValue;
					return true;
				}
			}
		}

		returnValue = default!;
		return false;
	}

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" />,
	///     <paramref name="p2Value" />, <paramref name="p3Value" /> and <paramref name="p4Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified parameters.
	/// </summary>
	public virtual void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		if (_callbacks is not null)
		{
			bool wasInvoked = false;
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, T4>> callback =
					_callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke(wasInvoked, ref _callbacks.CurrentIndex, (parameter1, parameter2, parameter3, parameter4),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2, state.parameter3, state.parameter4)))
				{
					wasInvoked = true;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with four parameters matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : ReturnMethodSetup<TReturn, T1, T2, T3, T4>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;
		private readonly string _parameterName3;
		private readonly string _parameterName4;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}" />
		public WithParameters(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2, string parameterName3, string parameterName4)
			: base(mockRegistry, name)
		{
			Parameters = parameters;
			_parameterName1 = parameterName1;
			_parameterName2 = parameterName2;
			_parameterName3 = parameterName3;
			_parameterName4 = parameterName4;
		}

		/// <summary>
		///     The parameters match for the method.
		/// </summary>
		private IParameters Parameters { get; }

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value, p3Value, p4Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value), (_parameterName3, p3Value), (_parameterName4, p4Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with four parameters matching against individual typed parameters.
	/// </summary>
	public class WithParameterCollection : ReturnMethodSetup<TReturn, T1, T2, T3, T4>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3, T4>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}" />
		public WithParameterCollection(
			MockRegistry mockRegistry,
			string name,
			IParameterMatch<T1> parameter1,
			IParameterMatch<T2> parameter2,
			IParameterMatch<T3> parameter3,
			IParameterMatch<T4> parameter4)
			: base(mockRegistry, name)
		{
			Parameter1 = parameter1;
			Parameter2 = parameter2;
			Parameter3 = parameter3;
			Parameter4 = parameter4;
		}

		/// <summary>
		///     The first parameter of the method.
		/// </summary>
		public IParameterMatch<T1> Parameter1 { get; }

		/// <summary>
		///     The second parameter of the method.
		/// </summary>
		public IParameterMatch<T2> Parameter2 { get; }

		/// <summary>
		///     The third parameter of the method.
		/// </summary>
		public IParameterMatch<T3> Parameter3 { get; }

		/// <summary>
		///     The fourth parameter of the method.
		/// </summary>
		public IParameterMatch<T4> Parameter4 { get; }

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2, T3, T4}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2, T3, T4> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3, T4>.
			AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> _matchAnyParameters ||
			   (Parameter1.Matches(p1Value) &&
			    Parameter2.Matches(p2Value) &&
			    Parameter3.Matches(p3Value) &&
			    Parameter4.Matches(p4Value));

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}.TriggerCallbacks(T1, T2, T3, T4)" />
		public override void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
		{
			Parameter1.InvokeCallbacks(parameter1);
			Parameter2.InvokeCallbacks(parameter2);
			Parameter3.InvokeCallbacks(parameter3);
			Parameter4.InvokeCallbacks(parameter4);
			base.TriggerCallbacks(parameter1, parameter2, parameter3, parameter4);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({Parameter1}, {Parameter2}, {Parameter3}, {Parameter4})";
	}

	/// <summary>
	///     Setup for a method with four parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T}" /> allocations that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : ReturnMethodSetup<TReturn, T1, T2, T3, T4>,
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3, T4>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private readonly T3 _value3;
		private readonly T4 _value4;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name,
			T1 value1, T2 value2, T3 value3, T4 value4)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
			_value3 = value3;
			_value4 = value4;
		}

		/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2, T3, T4}.AnyParameters()" />
		IReturnMethodSetup<TReturn, T1, T2, T3, T4> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2, T3, T4>.
			AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value) &&
			    EqualityComparer<T3>.Default.Equals(_value3, p3Value) &&
			    EqualityComparer<T4>.Default.Equals(_value4, p4Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"{FormatType(typeof(TReturn))} {ValueFormatter.FormatMethodName(Name)}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)}, {FormatLiteralValue(_value3)}, {FormatLiteralValue(_value4)})";
	}
}
#pragma warning restore S2436 // Types and methods should not have too many generic parameters
