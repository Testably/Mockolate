using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Sets up a method returning <see langword="void" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class VoidMethodSetup : MethodSetup,
	IVoidMethodSetupCallbackBuilder, IVoidMethodSetupReturnBuilder
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int>>? _callbacks;
	private Callbacks<Action<int>>? _returnCallbacks;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="VoidMethodSetup{TReturn, T1}" />
	protected VoidMethodSetup(MockRegistry mockRegistry, string name) : base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IVoidMethodSetup.SkippingBaseClass(bool)" />
	IVoidMethodSetup IVoidMethodSetup.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup.Do(Action)" />
	IVoidMethodSetupCallbackBuilder IVoidMethodSetup.Do(Action callback)
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

	/// <inheritdoc cref="IVoidMethodSetup.Do(Action{int})" />
	IVoidMethodSetupCallbackBuilder IVoidMethodSetup.Do(Action<int> callback)
	{
		Callback<Action<int>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup.TransitionTo(string)" />
	IVoidMethodSetupParallelCallbackBuilder IVoidMethodSetup.TransitionTo(string scenario)
	{
		Callback<Action<int>> currentCallback = new(_ => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup.DoesNotThrow()" />
	IVoidMethodSetup IVoidMethodSetup.DoesNotThrow()
	{
		Callback<Action<int>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _)
		{
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup.Throws{TException}()" />
	IVoidMethodSetupReturnBuilder IVoidMethodSetup.Throws<TException>()
	{
		Callback<Action<int>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup.Throws(Exception)" />
	IVoidMethodSetupReturnBuilder IVoidMethodSetup.Throws(Exception exception)
	{
		Callback<Action<int>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup.Throws(Func{Exception})" />
	IVoidMethodSetupReturnBuilder IVoidMethodSetup.Throws(Func<Exception> callback)
	{
		Callback<Action<int>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackBuilder.InParallel()" />
	IVoidMethodSetupParallelCallbackBuilder IVoidMethodSetupCallbackBuilder.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" />
	IVoidMethodSetupCallbackWhenBuilder IVoidMethodSetupParallelCallbackBuilder.When(Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder.For(int)" />
	IVoidMethodSetupCallbackWhenBuilder IVoidMethodSetupCallbackWhenBuilder.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder.Only(int)" />
	IVoidMethodSetup IVoidMethodSetupCallbackWhenBuilder.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnBuilder.When(Func{int, bool})" />
	IVoidMethodSetupReturnWhenBuilder IVoidMethodSetupReturnBuilder.When(Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder.For(int)" />
	IVoidMethodSetupReturnWhenBuilder IVoidMethodSetupReturnWhenBuilder.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder.Only(int)" />
	IVoidMethodSetup IVoidMethodSetupReturnWhenBuilder.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is MethodInvocation;

	/// <summary>
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameters.
	/// </summary>
	public abstract bool Matches();

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup without parameters.
	/// </summary>
	public void TriggerCallbacks()
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

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Action<int>> _ in _returnCallbacks)
			{
				Callback<Action<int>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke<object?>(ref _returnCallbacks.CurrentIndex, null,
					    static (count, @delegate, _) => @delegate(count)))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with three parameters matching against individual typed parameters.
	/// </summary>
	public class WithParameterCollection : VoidMethodSetup
	{
		/// <inheritdoc cref="VoidMethodSetup" />
		public WithParameterCollection(
			MockRegistry mockRegistry,
			string name)
			: base(mockRegistry, name)
		{
		}

		/// <inheritdoc cref="VoidMethodSetup.Matches()" />
		public override bool Matches()
			=> true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}()";
	}

	/// <summary>
	///     Setup for a generic method without parameters, for any type argument.
	/// </summary>
	public class WithParameterCollectionForAnyTypeArguments : WithParameterCollection, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup" />
		public WithParameterCollectionForAnyTypeArguments(MockRegistry mockRegistry, string name)
			: base(mockRegistry, name)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 0, out _);
	}
}

/// <summary>
///     Setup for a method with one parameter <typeparamref name="T1" /> returning <see langword="void" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class VoidMethodSetup<T1> : MethodSetup,
	IVoidMethodSetupWithCallback<T1>,
	IVoidMethodSetupCallbackBuilder<T1>, IVoidMethodSetupReturnBuilder<T1>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1>>? _callbacks;
	private Callbacks<Action<int, T1>>? _returnCallbacks;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="VoidMethodSetup{T1}" />
	public VoidMethodSetup(MockRegistry mockRegistry, string name)
		: base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.SkippingBaseClass(bool)" />
	IVoidMethodSetup<T1> IVoidMethodSetup<T1>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.Do(Action)" />
	IVoidMethodSetupCallbackBuilder<T1> IVoidMethodSetup<T1>.Do(Action callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1}.Do(Action{T1})" />
	IVoidMethodSetupCallbackBuilder<T1> IVoidMethodSetupWithCallback<T1>.Do(Action<T1> callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1}.Do(Action{int, T1})" />
	IVoidMethodSetupCallbackBuilder<T1> IVoidMethodSetupWithCallback<T1>.Do(Action<int, T1> callback)
	{
		Callback<Action<int, T1>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.TransitionTo(string)" />
	IVoidMethodSetupParallelCallbackBuilder<T1> IVoidMethodSetup<T1>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1>> currentCallback = new((_, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.DoesNotThrow()" />
	IVoidMethodSetup<T1> IVoidMethodSetup<T1>.DoesNotThrow()
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.Throws{TException}()" />
	IVoidMethodSetupReturnBuilder<T1> IVoidMethodSetup<T1>.Throws<TException>()
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.Throws(Exception)" />
	IVoidMethodSetupReturnBuilder<T1> IVoidMethodSetup<T1>.Throws(Exception exception)
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1}.Throws(Func{Exception})" />
	IVoidMethodSetupReturnBuilder<T1> IVoidMethodSetup<T1>.Throws(Func<Exception> callback)
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1}.Throws(Func{T1, Exception})" />
	IVoidMethodSetupReturnBuilder<T1> IVoidMethodSetupWithCallback<T1>.Throws(Func<T1, Exception> callback)
	{
		Callback<Action<int, T1>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1)
		{
			throw callback(p1);
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackBuilder{T1}.InParallel()" />
	IVoidMethodSetupParallelCallbackBuilder<T1> IVoidMethodSetupCallbackBuilder<T1>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupParallelCallbackBuilder{T1}.When(Func{int, bool})" />
	IVoidMethodSetupCallbackWhenBuilder<T1> IVoidMethodSetupParallelCallbackBuilder<T1>.When(Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1}.For(int)" />
	IVoidMethodSetupCallbackWhenBuilder<T1> IVoidMethodSetupCallbackWhenBuilder<T1>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1}.Only(int)" />
	IVoidMethodSetup<T1> IVoidMethodSetupCallbackWhenBuilder<T1>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnBuilder{T1}.When(Func{int, bool})" />
	IVoidMethodSetupReturnWhenBuilder<T1> IVoidMethodSetupReturnBuilder<T1>.When(Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1}.For(int)" />
	IVoidMethodSetupReturnWhenBuilder<T1> IVoidMethodSetupReturnWhenBuilder<T1>.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1}.Only(int)" />
	IVoidMethodSetup<T1> IVoidMethodSetupReturnWhenBuilder<T1>.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
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
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

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

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Action<int, T1>> _ in _returnCallbacks)
			{
				Callback<Action<int, T1>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, parameter1,
					    static (count, @delegate, state) => @delegate(count, state)))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : VoidMethodSetup<T1>
	{
		private readonly string _parameterName1;

		/// <inheritdoc cref="VoidMethodSetup{T1}" />
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

		/// <inheritdoc cref="VoidMethodSetup{T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : VoidMethodSetup<T1>,
		IVoidMethodSetupParameterIgnorer<T1>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name, IParameterMatch<T1> parameter1)
			: base(mockRegistry, name)
		{
			Parameter1 = parameter1;
		}

		/// <summary>
		///     The single parameter of the method.
		/// </summary>
		public IParameterMatch<T1> Parameter1 { get; }

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1}.AnyParameters()" />
		IVoidMethodSetup<T1> IVoidMethodSetupParameterIgnorer<T1>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> _matchAnyParameters || Parameter1.Matches(p1Value);

		/// <inheritdoc cref="VoidMethodSetup{T1}.TriggerCallbacks(T1)" />
		public override void TriggerCallbacks(T1 parameter1)
		{
			Parameter1.InvokeCallbacks(parameter1);
			base.TriggerCallbacks(parameter1);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameter1})";
	}

	/// <summary>
	///     Setup for a method with one parameter where the expected value is stored directly on the
	///     setup, avoiding the per-call <see cref="IParameterMatch{T1}" /> allocation that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : VoidMethodSetup<T1>,
		IVoidMethodSetupParameterIgnorer<T1>
	{
		private readonly T1 _value1;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name, T1 value1)
			: base(mockRegistry, name)
		{
			_value1 = value1;
		}

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1}.AnyParameters()" />
		IVoidMethodSetup<T1> IVoidMethodSetupParameterIgnorer<T1>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1}.Matches(T1)" />
		public override bool Matches(T1 p1Value)
			=> _matchAnyParameters || EqualityComparer<T1>.Default.Equals(_value1, p1Value);

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({FormatLiteralValue(_value1)})";
	}

	/// <summary>
	///     Setup for a generic method with one parameter, for any type argument, matching against
	///     <see cref="IParameters" />.
	/// </summary>
	public class WithParametersForAnyTypeArguments : WithParameters, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1}" />
		public WithParametersForAnyTypeArguments(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1)
			: base(mockRegistry, name, parameters, parameterName1)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 1, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   Matches(p1Value);
	}

	/// <summary>
	///     Setup for a generic method with one parameter, for any type argument, matching against an individual
	///     <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollectionForAnyTypeArguments : WithParameterCollection, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1}" />
		public WithParameterCollectionForAnyTypeArguments(MockRegistry mockRegistry, string name,
			IParameterMatch<T1> parameter1)
			: base(mockRegistry, name, parameter1)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 1, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   Matches(p1Value);
	}
}

/// <summary>
///     Setup for a method with two parameters <typeparamref name="T1" /> and <typeparamref name="T2" /> returning
///     <see langword="void" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class VoidMethodSetup<T1, T2> : MethodSetup,
	IVoidMethodSetupWithCallback<T1, T2>,
	IVoidMethodSetupCallbackBuilder<T1, T2>, IVoidMethodSetupReturnBuilder<T1, T2>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2>>? _callbacks;
	private Callbacks<Action<int, T1, T2>>? _returnCallbacks;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
	public VoidMethodSetup(MockRegistry mockRegistry, string name)
		: base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.SkippingBaseClass(bool)" />
	IVoidMethodSetup<T1, T2> IVoidMethodSetup<T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.Do(Action)" />
	IVoidMethodSetupCallbackBuilder<T1, T2> IVoidMethodSetup<T1, T2>.Do(Action callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2}.Do(Action{T1, T2})" />
	IVoidMethodSetupCallbackBuilder<T1, T2> IVoidMethodSetupWithCallback<T1, T2>.Do(Action<T1, T2> callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2}.Do(Action{int, T1, T2})" />
	IVoidMethodSetupCallbackBuilder<T1, T2> IVoidMethodSetupWithCallback<T1, T2>.Do(Action<int, T1, T2> callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.TransitionTo(string)" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2> IVoidMethodSetup<T1, T2>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, T2>> currentCallback = new((_, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.DoesNotThrow()" />
	IVoidMethodSetup<T1, T2> IVoidMethodSetup<T1, T2>.DoesNotThrow()
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.Throws{TException}()" />
	IVoidMethodSetupReturnBuilder<T1, T2> IVoidMethodSetup<T1, T2>.Throws<TException>()
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.Throws(Exception)" />
	IVoidMethodSetupReturnBuilder<T1, T2> IVoidMethodSetup<T1, T2>.Throws(Exception exception)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2}.Throws(Func{Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2> IVoidMethodSetup<T1, T2>.Throws(Func<Exception> callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2}.Throws(Func{T1, T2, Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2> IVoidMethodSetupWithCallback<T1, T2>.Throws(Func<T1, T2, Exception> callback)
	{
		Callback<Action<int, T1, T2>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2)
		{
			throw callback(p1, p2);
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackBuilder{T1, T2}.InParallel()" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2> IVoidMethodSetupCallbackBuilder<T1, T2>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupParallelCallbackBuilder{T1, T2}.When(Func{int, bool})" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2> IVoidMethodSetupParallelCallbackBuilder<T1, T2>.When(Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2}.For(int)" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2> IVoidMethodSetupCallbackWhenBuilder<T1, T2>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2}.Only(int)" />
	IVoidMethodSetup<T1, T2> IVoidMethodSetupCallbackWhenBuilder<T1, T2>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnBuilder{T1, T2}.When(Func{int, bool})" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2> IVoidMethodSetupReturnBuilder<T1, T2>.When(Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2}.For(int)" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2> IVoidMethodSetupReturnWhenBuilder<T1, T2>.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2}.Only(int)" />
	IVoidMethodSetup<T1, T2> IVoidMethodSetupReturnWhenBuilder<T1, T2>.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
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
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" /> and
	///     <paramref name="p2Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified <paramref name="parameter1" />.
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

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Action<int, T1, T2>> _ in _returnCallbacks)
			{
				Callback<Action<int, T1, T2>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (parameter1, parameter2),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2)))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : VoidMethodSetup<T1, T2>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
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

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : VoidMethodSetup<T1, T2>,
		IVoidMethodSetupParameterIgnorer<T1, T2>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name,
			IParameterMatch<T1> parameter1,
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

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2}.AnyParameters()" />
		IVoidMethodSetup<T1, T2> IVoidMethodSetupParameterIgnorer<T1, T2>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> _matchAnyParameters || (
				Parameter1.Matches(p1Value) &&
				Parameter2.Matches(p2Value));

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}.TriggerCallbacks(T1, T2)" />
		public override void TriggerCallbacks(T1 parameter1, T2 parameter2)
		{
			Parameter1.InvokeCallbacks(parameter1);
			Parameter2.InvokeCallbacks(parameter2);
			base.TriggerCallbacks(parameter1, parameter2);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameter1}, {Parameter2})";
	}

	/// <summary>
	///     Setup for a method with two parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T}" /> allocations that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : VoidMethodSetup<T1, T2>,
		IVoidMethodSetupParameterIgnorer<T1, T2>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name, T1 value1, T2 value2)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
		}

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2}.AnyParameters()" />
		IVoidMethodSetup<T1, T2> IVoidMethodSetupParameterIgnorer<T1, T2>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}.Matches(T1, T2)" />
		public override bool Matches(T1 p1Value, T2 p2Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)})";
	}

	/// <summary>
	///     Setup for a generic method with two parameters, for any type argument, matching against
	///     <see cref="IParameters" />.
	/// </summary>
	public class WithParametersForAnyTypeArguments : WithParameters, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
		public WithParametersForAnyTypeArguments(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2)
			: base(mockRegistry, name, parameters, parameterName1, parameterName2)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 2, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   Matches(p1Value, p2Value);
	}

	/// <summary>
	///     Setup for a generic method with two parameters, for any type argument, matching against individual
	///     <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollectionForAnyTypeArguments : WithParameterCollection, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2}" />
		public WithParameterCollectionForAnyTypeArguments(MockRegistry mockRegistry, string name,
			IParameterMatch<T1> parameter1, IParameterMatch<T2> parameter2)
			: base(mockRegistry, name, parameter1, parameter2)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 2, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   Matches(p1Value, p2Value);
	}
}

/// <summary>
///     Setup for a method with three parameters <typeparamref name="T1" />, <typeparamref name="T2" /> and
///     <typeparamref name="T3" /> returning <see langword="void" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class VoidMethodSetup<T1, T2, T3> : MethodSetup,
	IVoidMethodSetupWithCallback<T1, T2, T3>,
	IVoidMethodSetupCallbackBuilder<T1, T2, T3>, IVoidMethodSetupReturnBuilder<T1, T2, T3>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2, T3>>? _callbacks;
	private Callbacks<Action<int, T1, T2, T3>>? _returnCallbacks;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
	public VoidMethodSetup(MockRegistry mockRegistry, string name)
		: base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.SkippingBaseClass(bool)" />
	IVoidMethodSetup<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.Do(Action)" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.Do(Action callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3}.Do(Action{T1, T2, T3})" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> IVoidMethodSetupWithCallback<T1, T2, T3>.Do(Action<T1, T2, T3> callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3}.Do(Action{int, T1, T2, T3})" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> IVoidMethodSetupWithCallback<T1, T2, T3>.Do(Action<int, T1, T2, T3> callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.TransitionTo(string)" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new((_, _, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.DoesNotThrow()" />
	IVoidMethodSetup<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.DoesNotThrow()
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.Throws{TException}()" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.Throws<TException>()
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.Throws(Exception)" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.Throws(Exception exception)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3}.Throws(Func{Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3> IVoidMethodSetup<T1, T2, T3>.Throws(Func<Exception> callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3}.Throws(Func{T1, T2, T3, Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3> IVoidMethodSetupWithCallback<T1, T2, T3>.Throws(Func<T1, T2, T3, Exception> callback)
	{
		Callback<Action<int, T1, T2, T3>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3)
		{
			throw callback(p1, p2, p3);
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackBuilder{T1, T2, T3}.InParallel()" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3> IVoidMethodSetupCallbackBuilder<T1, T2, T3>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupParallelCallbackBuilder{T1, T2, T3}.When(Func{int, bool})" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3> IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2, T3}.For(int)" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3> IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3>.For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2, T3}.Only(int)" />
	IVoidMethodSetup<T1, T2, T3> IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnBuilder{T1, T2, T3}.When(Func{int, bool})" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3> IVoidMethodSetupReturnBuilder<T1, T2, T3>.When(
		Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2, T3}.For(int)" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3> IVoidMethodSetupReturnWhenBuilder<T1, T2, T3>.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2, T3}.Only(int)" />
	IVoidMethodSetup<T1, T2, T3> IVoidMethodSetupReturnWhenBuilder<T1, T2, T3>.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
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
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" />,
	///     <paramref name="p2Value" /> and <paramref name="p3Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value, T3 p3Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified <paramref name="parameter1" />.
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

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Action<int, T1, T2, T3>> _ in _returnCallbacks)
			{
				Callback<Action<int, T1, T2, T3>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (parameter1, parameter2, parameter3),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2, state.parameter3)))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : VoidMethodSetup<T1, T2, T3>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;
		private readonly string _parameterName3;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
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

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value, p3Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value), (_parameterName3, p3Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : VoidMethodSetup<T1, T2, T3>,
		IVoidMethodSetupParameterIgnorer<T1, T2, T3>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name,
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

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2, T3}.AnyParameters()" />
		IVoidMethodSetup<T1, T2, T3> IVoidMethodSetupParameterIgnorer<T1, T2, T3>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> _matchAnyParameters || (
				Parameter1.Matches(p1Value) &&
				Parameter2.Matches(p2Value) &&
				Parameter3.Matches(p3Value));

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}.TriggerCallbacks(T1, T2, T3)" />
		public override void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3)
		{
			Parameter1.InvokeCallbacks(parameter1);
			Parameter2.InvokeCallbacks(parameter2);
			Parameter3.InvokeCallbacks(parameter3);
			base.TriggerCallbacks(parameter1, parameter2, parameter3);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameter1}, {Parameter2}, {Parameter3})";
	}

	/// <summary>
	///     Setup for a method with three parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T}" /> allocations that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : VoidMethodSetup<T1, T2, T3>,
		IVoidMethodSetupParameterIgnorer<T1, T2, T3>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private readonly T3 _value3;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name,
			T1 value1, T2 value2, T3 value3)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
			_value3 = value3;
		}

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2, T3}.AnyParameters()" />
		IVoidMethodSetup<T1, T2, T3> IVoidMethodSetupParameterIgnorer<T1, T2, T3>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}.Matches(T1, T2, T3)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value) &&
			    EqualityComparer<T3>.Default.Equals(_value3, p3Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)}, {FormatLiteralValue(_value3)})";
	}

	/// <summary>
	///     Setup for a generic method with three parameters, for any type argument, matching against
	///     <see cref="IParameters" />.
	/// </summary>
	public class WithParametersForAnyTypeArguments : WithParameters, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
		public WithParametersForAnyTypeArguments(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2, string parameterName3)
			: base(mockRegistry, name, parameters, parameterName1, parameterName2, parameterName3)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 3, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 2, out T3 p3Value) &&
			   Matches(p1Value, p2Value, p3Value);
	}

	/// <summary>
	///     Setup for a generic method with three parameters, for any type argument, matching against individual
	///     <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollectionForAnyTypeArguments : WithParameterCollection, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3}" />
		public WithParameterCollectionForAnyTypeArguments(MockRegistry mockRegistry, string name,
			IParameterMatch<T1> parameter1, IParameterMatch<T2> parameter2, IParameterMatch<T3> parameter3)
			: base(mockRegistry, name, parameter1, parameter2, parameter3)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 3, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 2, out T3 p3Value) &&
			   Matches(p1Value, p2Value, p3Value);
	}
}

/// <summary>
///     Setup for a method with four parameters <typeparamref name="T1" />, <typeparamref name="T2" />,
///     <typeparamref name="T3" /> and <typeparamref name="T4" /> returning <see langword="void" />.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public abstract class VoidMethodSetup<T1, T2, T3, T4> : MethodSetup,
	IVoidMethodSetupWithCallback<T1, T2, T3, T4>,
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4>, IVoidMethodSetupReturnBuilder<T1, T2, T3, T4>
{
	private readonly MockRegistry _mockRegistry;
	private Callbacks<Action<int, T1, T2, T3, T4>>? _callbacks;
	private Callbacks<Action<int, T1, T2, T3, T4>>? _returnCallbacks;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
	public VoidMethodSetup(MockRegistry mockRegistry, string name)
		: base(name)
	{
		_mockRegistry = mockRegistry;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.Do(Action)" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.Do(Action callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3, T4}.Do(Action{T1, T2, T3, T4})" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> IVoidMethodSetupWithCallback<T1, T2, T3, T4>.Do(Action<T1, T2, T3, T4> callback)
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

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3, T4}.Do(Action{int, T1, T2, T3, T4})" />
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> IVoidMethodSetupWithCallback<T1, T2, T3, T4>.Do(Action<int, T1, T2, T3, T4> callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(callback);
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.TransitionTo(string)" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.TransitionTo(string scenario)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new((_, _, _, _, _) => _mockRegistry.TransitionTo(scenario));
		currentCallback.InParallel();
		_callbacks = _callbacks.Register(currentCallback);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.DoesNotThrow()" />
	IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.DoesNotThrow()
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.Throws{TException}()" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.Throws<TException>()
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw new TException();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.Throws(Exception)" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.Throws(Exception exception)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw exception;
		}
	}

	/// <inheritdoc cref="IVoidMethodSetup{T1, T2, T3, T4}.Throws(Func{Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> IVoidMethodSetup<T1, T2, T3, T4>.Throws(Func<Exception> callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw callback();
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupWithCallback{T1, T2, T3, T4}.Throws(Func{T1, T2, T3, T4, Exception})" />
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> IVoidMethodSetupWithCallback<T1, T2, T3, T4>.Throws(Func<T1, T2, T3, T4, Exception> callback)
	{
		Callback<Action<int, T1, T2, T3, T4>> currentCallback = new(Delegate);
		_returnCallbacks = _returnCallbacks.Register(currentCallback);
		return this;

		[DebuggerNonUserCode]
		void Delegate(int _, T1 p1, T2 p2, T3 p3, T4 p4)
		{
			throw callback(p1, p2, p3, p4);
		}
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackBuilder{T1, T2, T3, T4}.InParallel()" />
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4> IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4>.InParallel()
	{
		_callbacks?.Active?.InParallel();
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupParallelCallbackBuilder{T1, T2, T3, T4}.When(Func{int, bool})" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4> IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		_callbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2, T3, T4}.For(int)" />
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4> IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4>.
		For(int times)
	{
		_callbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupCallbackWhenBuilder{T1, T2, T3, T4}.Only(int)" />
	IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4>.Only(int times)
	{
		_callbacks?.Active?.Only(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnBuilder{T1, T2, T3, T4}.When(Func{int, bool})" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4> IVoidMethodSetupReturnBuilder<T1, T2, T3, T4>.When(
		Func<int, bool> predicate)
	{
		_returnCallbacks?.Active?.When(predicate);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2, T3, T4}.For(int)" />
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4> IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4>.For(int times)
	{
		_returnCallbacks?.Active?.For(times);
		return this;
	}

	/// <inheritdoc cref="IVoidMethodSetupReturnWhenBuilder{T1, T2, T3, T4}.Only(int)" />
	IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4>.Only(int times)
	{
		_returnCallbacks?.Active?.Only(times);
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
	///     Gets the flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <summary>
	///     Check if the setup matches the specified parameter values <paramref name="p1Value" />,
	///     <paramref name="p2Value" />, <paramref name="p3Value" /> and <paramref name="p4Value" />.
	/// </summary>
	public abstract bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value);

	/// <summary>
	///     Triggers any configured parameter callbacks for the method setup with the specified <paramref name="parameter1" />.
	/// </summary>
	public virtual void TriggerCallbacks(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	{
		if (_callbacks is not null)
		{
			bool wasInvoked = false;
			int currentCallbacksIndex = _callbacks.CurrentIndex;
			for (int i = 0; i < _callbacks.Count; i++)
			{
				Callback<Action<int, T1, T2, T3, T4>> callback = _callbacks[(currentCallbacksIndex + i) % _callbacks.Count];
				if (callback.Invoke(wasInvoked, ref _callbacks.CurrentIndex, (parameter1, parameter2, parameter3, parameter4),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2, state.parameter3, state.parameter4)))
				{
					wasInvoked = true;
				}
			}
		}

		if (_returnCallbacks is not null)
		{
			foreach (Callback<Action<int, T1, T2, T3, T4>> _ in _returnCallbacks)
			{
				Callback<Action<int, T1, T2, T3, T4>> returnCallback =
					_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];
				if (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (parameter1, parameter2, parameter3, parameter4),
					    static (count, @delegate, state) => @delegate(count, state.parameter1, state.parameter2, state.parameter3, state.parameter4)))
				{
					return;
				}
			}
		}
	}

	/// <summary>
	///     Setup for a method with one parameter matching against <see cref="IParameters" />.
	/// </summary>
	public class WithParameters : VoidMethodSetup<T1, T2, T3, T4>
	{
		private readonly string _parameterName1;
		private readonly string _parameterName2;
		private readonly string _parameterName3;
		private readonly string _parameterName4;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
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

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> Parameters switch
			{
				IParametersMatch m => m.Matches([p1Value, p2Value, p3Value, p4Value,]),
				INamedParametersMatch m => m.Matches([(_parameterName1, p1Value), (_parameterName2, p2Value), (_parameterName3, p3Value), (_parameterName4, p4Value),]),
				_ => true,
			};

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({Parameters})";
	}

	/// <summary>
	///     Setup for a method with one parameter matching against individual <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollection : VoidMethodSetup<T1, T2, T3, T4>,
		IVoidMethodSetupParameterIgnorer<T1, T2, T3, T4>
	{
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
		public WithParameterCollection(MockRegistry mockRegistry, string name,
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

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2, T3, T4}.AnyParameters()" />
		IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetupParameterIgnorer<T1, T2, T3, T4>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> _matchAnyParameters || (
				Parameter1.Matches(p1Value) &&
				Parameter2.Matches(p2Value) &&
				Parameter3.Matches(p3Value) &&
				Parameter4.Matches(p4Value));

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}.TriggerCallbacks(T1, T2, T3, T4)" />
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
			=> $"void {Name.SubstringAfterLast('.')}({Parameter1}, {Parameter2}, {Parameter3}, {Parameter4})";
	}

	/// <summary>
	///     Setup for a method with four parameters where the expected values are stored directly on
	///     the setup, avoiding the per-call <see cref="IParameterMatch{T}" /> allocations that
	///     <see cref="WithParameterCollection" /> requires.
	/// </summary>
	public class WithLiteralValues : VoidMethodSetup<T1, T2, T3, T4>,
		IVoidMethodSetupParameterIgnorer<T1, T2, T3, T4>
	{
		private readonly T1 _value1;
		private readonly T2 _value2;
		private readonly T3 _value3;
		private readonly T4 _value4;
		private bool _matchAnyParameters;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
		public WithLiteralValues(MockRegistry mockRegistry, string name,
			T1 value1, T2 value2, T3 value3, T4 value4)
			: base(mockRegistry, name)
		{
			_value1 = value1;
			_value2 = value2;
			_value3 = value3;
			_value4 = value4;
		}

		/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2, T3, T4}.AnyParameters()" />
		IVoidMethodSetup<T1, T2, T3, T4> IVoidMethodSetupParameterIgnorer<T1, T2, T3, T4>.AnyParameters()
		{
			_matchAnyParameters = true;
			return this;
		}

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}.Matches(T1, T2, T3, T4)" />
		public override bool Matches(T1 p1Value, T2 p2Value, T3 p3Value, T4 p4Value)
			=> _matchAnyParameters ||
			   (EqualityComparer<T1>.Default.Equals(_value1, p1Value) &&
			    EqualityComparer<T2>.Default.Equals(_value2, p2Value) &&
			    EqualityComparer<T3>.Default.Equals(_value3, p3Value) &&
			    EqualityComparer<T4>.Default.Equals(_value4, p4Value));

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"void {Name.SubstringAfterLast('.')}({FormatLiteralValue(_value1)}, {FormatLiteralValue(_value2)}, {FormatLiteralValue(_value3)}, {FormatLiteralValue(_value4)})";
	}

	/// <summary>
	///     Setup for a generic method with four parameters, for any type argument, matching against
	///     <see cref="IParameters" />.
	/// </summary>
	public class WithParametersForAnyTypeArguments : WithParameters, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
		public WithParametersForAnyTypeArguments(MockRegistry mockRegistry, string name, IParameters parameters,
			string parameterName1, string parameterName2, string parameterName3, string parameterName4)
			: base(mockRegistry, name, parameters, parameterName1, parameterName2, parameterName3, parameterName4)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 4, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 2, out T3 p3Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 3, out T4 p4Value) &&
			   Matches(p1Value, p2Value, p3Value, p4Value);
	}

	/// <summary>
	///     Setup for a generic method with four parameters, for any type argument, matching against individual
	///     <see cref="IParameterMatch{T}" />.
	/// </summary>
	public class WithParameterCollectionForAnyTypeArguments : WithParameterCollection, IGenericMethodSetup
	{
		private readonly string _namePrefix;

		/// <inheritdoc cref="VoidMethodSetup{T1, T2, T3, T4}" />
		public WithParameterCollectionForAnyTypeArguments(MockRegistry mockRegistry, string name,
			IParameterMatch<T1> parameter1, IParameterMatch<T2> parameter2, IParameterMatch<T3> parameter3,
			IParameterMatch<T4> parameter4)
			: base(mockRegistry, name, parameter1, parameter2, parameter3, parameter4)
		{
			_namePrefix = GenericMethodInvocations.GetNamePrefix(name);
		}

		/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
		protected override bool MatchesInteraction(IMethodInteraction interaction)
			=> GenericMethodInvocations.TryGetInvocation(interaction, _namePrefix, 4, out IMethodInvocationParameters invocation) &&
			   GenericMethodInvocations.TryGetValue(invocation, 0, out T1 p1Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 1, out T2 p2Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 2, out T3 p3Value) &&
			   GenericMethodInvocations.TryGetValue(invocation, 3, out T4 p4Value) &&
			   Matches(p1Value, p2Value, p3Value, p4Value);
	}
}
