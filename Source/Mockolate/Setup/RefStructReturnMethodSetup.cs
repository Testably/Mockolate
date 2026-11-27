#if NET9_0_OR_GREATER
using System;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Concrete ref-struct-compatible setup for a method returning <typeparamref name="TReturn" />
///     with a single ref struct parameter of type <typeparamref name="T" />.
/// </summary>
/// <remarks>
///     See <see cref="RefStructVoidMethodSetup{T}" /> for the overall design rationale. Return
///     semantics: when the setup matches, <see cref="Invoke" /> runs matcher-level callbacks,
///     applies the configured throw (if any), then returns the configured value. Configuration
///     order: a later <c>Returns</c> / <c>Throws</c> overwrites an earlier one in the same slot.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructReturnMethodSetup<TReturn, T> : MethodSetup, IRefStructReturnMethodSetup<TReturn, T>
	where T : allows ref struct
{
	private readonly IParameterMatch<T>? _matcher;
	private Func<TReturn>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}" />
	public RefStructReturnMethodSetup(string name, IParameterMatch<T>? matcher = null)
		: base(name)
	{
		_matcher = matcher;
	}

	/// <inheritdoc cref="RefStructVoidMethodSetup{T}.Matches(T)" />
	public bool Matches(T value)
		=> _matcher is null || _matcher.Matches(value);

	/// <summary>
	///     Returns the matcher for the first (and only) parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T>? GetMatcher1()
		=> _matcher;

	/// <summary>
	///     Invokes matcher callbacks, applies any configured throw, and returns the configured
	///     value. If no return value is configured, falls back to <paramref name="defaultFactory" />
	///     or <see langword="default" />.
	/// </summary>
	public TReturn Invoke(T value, Func<TReturn>? defaultFactory = null)
	{
		_matcher?.InvokeCallbacks(value);
		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <summary>
	///     Whether a return value has been explicitly configured on this setup.
	/// </summary>
	public bool HasReturnValue => _returnFactory is not null;

	/// <inheritdoc cref="RefStructVoidMethodSetup{T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.SkippingBaseClass(bool)" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.Returns(TReturn)" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.Returns(TReturn returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.Returns(Func{TReturn})" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.Returns(Func<TReturn> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.Throws{TException}()" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.Throws(Exception)" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructReturnMethodSetup{TReturn, T}.Throws(Func{Exception})" />
	IRefStructReturnMethodSetup<TReturn, T> IRefStructReturnMethodSetup<TReturn, T>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> _matcher is null
			? $"{typeof(TReturn).Name} {Name}(<{typeof(T).Name} ref struct>)"
			: $"{typeof(TReturn).Name} {Name}({_matcher})";
}

/// <summary>
///     Concrete ref-struct-compatible return setup for arity 2.
///     See <see cref="RefStructReturnMethodSetup{TReturn, T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructReturnMethodSetup<TReturn, T1, T2> : MethodSetup, IRefStructReturnMethodSetup<TReturn, T1, T2>
	where T1 : allows ref struct
	where T2 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private Func<TReturn>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T1, T2}" />
	public RefStructReturnMethodSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2));

	/// <summary>
	///     Returns the matcher for the first parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T1>? GetMatcher1()
		=> _matcher1;

	/// <summary>
	///     Returns the matcher for the second parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T2>? GetMatcher2()
		=> _matcher2;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Invoke(T, Func{TReturn})" />
	public TReturn Invoke(T1 value1, T2 value2, Func<TReturn>? defaultFactory = null)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.Returns(TReturn returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.Returns(Func<TReturn> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2> IRefStructReturnMethodSetup<TReturn, T1, T2>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TReturn).Name} {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"})";
}

/// <summary>
///     Concrete ref-struct-compatible return setup for arity 3.
///     See <see cref="RefStructReturnMethodSetup{TReturn, T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructReturnMethodSetup<TReturn, T1, T2, T3> : MethodSetup, IRefStructReturnMethodSetup<TReturn, T1, T2, T3>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private Func<TReturn>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T1, T2, T3}" />
	public RefStructReturnMethodSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_matcher3 = matcher3;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2, T3 value3)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3));

	/// <summary>
	///     Returns the matcher for the first parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T1>? GetMatcher1()
		=> _matcher1;

	/// <summary>
	///     Returns the matcher for the second parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T2>? GetMatcher2()
		=> _matcher2;

	/// <summary>
	///     Returns the matcher for the third parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T3>? GetMatcher3()
		=> _matcher3;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Invoke(T, Func{TReturn})" />
	public TReturn Invoke(T1 value1, T2 value2, T3 value3, Func<TReturn>? defaultFactory = null)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		_matcher3?.InvokeCallbacks(value3);
		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.Returns(TReturn returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.Returns(Func<TReturn> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3> IRefStructReturnMethodSetup<TReturn, T1, T2, T3>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TReturn).Name} {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"})";
}

/// <summary>
///     Concrete ref-struct-compatible return setup for arity 4.
///     See <see cref="RefStructReturnMethodSetup{TReturn, T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> : MethodSetup, IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private readonly IParameterMatch<T4>? _matcher4;
	private Func<TReturn>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T1, T2, T3, T4}" />
	public RefStructReturnMethodSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null,
		IParameterMatch<T4>? matcher4 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_matcher3 = matcher3;
		_matcher4 = matcher4;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2, T3 value3, T4 value4)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3))
		   && (_matcher4 is null || _matcher4.Matches(value4));

	/// <summary>
	///     Returns the matcher for the first parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T1>? GetMatcher1()
		=> _matcher1;

	/// <summary>
	///     Returns the matcher for the second parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T2>? GetMatcher2()
		=> _matcher2;

	/// <summary>
	///     Returns the matcher for the third parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T3>? GetMatcher3()
		=> _matcher3;

	/// <summary>
	///     Returns the matcher for the fourth parameter slot.
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T4>? GetMatcher4()
		=> _matcher4;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Invoke(T, Func{TReturn})" />
	public TReturn Invoke(T1 value1, T2 value2, T3 value3, T4 value4, Func<TReturn>? defaultFactory = null)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		_matcher3?.InvokeCallbacks(value3);
		_matcher4?.InvokeCallbacks(value4);
		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.Returns(TReturn returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.Returns(Func<TReturn> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4> IRefStructReturnMethodSetup<TReturn, T1, T2, T3, T4>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TReturn).Name} {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}, {_matcher4?.ToString() ?? $"<{typeof(T4).Name} ref struct>"})";
}
#endif
