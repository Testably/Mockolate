#if NET9_0_OR_GREATER
using System;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Concrete ref-struct-compatible setup for a <see langword="void" /> method with a single
///     ref struct parameter of type <typeparamref name="T" />.
/// </summary>
/// <remarks>
///     <para>
///         Stores no field of type <typeparamref name="T" /> and no <c>Action&lt;T&gt;</c> —
///         both are illegal for a ref struct <typeparamref name="T" />. The setup matches calls via
///         an <see cref="IParameterMatch{T}" /> supplied at construction. Throw configuration is
///         stored in a single <see cref="Func{Exception}" /> slot; last <c>Throws</c>/<c>DoesNotThrow</c>
///         call wins. The full <c>Callbacks&lt;T&gt;</c> sequencing machinery used by the existing
///         <see cref="VoidMethodSetup{T1}" /> hierarchy is out of scope — it fundamentally cannot
///         carry a ref struct <typeparamref name="T" /> and would require its own ref-struct-safe
///         redesign.
///     </para>
///     <para>
///         <see cref="MatchesInteraction" /> on the verify side matches by method name only,
///         because the parameter value is not retained in a <see cref="RefStructMethodInvocation" />.
///         See that type's remarks for design rationale.
///     </para>
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructVoidMethodSetup<T> : MethodSetup, IRefStructVoidMethodSetup<T>
	where T : allows ref struct
{
	private readonly IParameterMatch<T>? _matcher;
	private Func<Exception?>? _returnAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructVoidMethodSetup{T}" />
	/// <param name="name">The method name.</param>
	/// <param name="matcher">
	///     The parameter matcher. When <see langword="null" />, the setup matches any call to
	///     <paramref name="name" />.
	/// </param>
	public RefStructVoidMethodSetup(string name, IParameterMatch<T>? matcher = null)
		: base(name)
	{
		_matcher = matcher;
	}

	/// <summary>
	///     Checks whether this setup's matcher accepts the given ref struct <paramref name="value" />.
	/// </summary>
	/// <remarks>
	///     Must be called synchronously from the caller's stack frame — <paramref name="value" />
	///     is a ref struct and cannot be captured in a closure.
	/// </remarks>
	public bool Matches(T value)
		=> _matcher is null || _matcher.Matches(value);

	/// <summary>
	///     Returns the matcher for the first (and only) parameter slot, or <see langword="null" />
	///     when the slot has no matcher configured. Used by generated mock bodies to access ref-struct
	///     <c>out</c>/<c>ref</c> matcher payloads (e.g. <see cref="IOutRefStructParameter{T}" />).
	/// </summary>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public IParameterMatch<T>? GetMatcher1()
		=> _matcher;

	/// <summary>
	///     Invokes any matcher-level callbacks against the live <paramref name="value" /> and then
	///     applies the currently configured throw (if any).
	/// </summary>
	public void Invoke(T value)
	{
		_matcher?.InvokeCallbacks(value);
		if (_returnAction is null)
		{
			return;
		}

		Exception? exception = _returnAction();
		if (exception is not null)
		{
			throw exception;
		}
	}

	/// <summary>
	///     The flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T}.SkippingBaseClass(bool)" />
	IRefStructVoidMethodSetup<T> IRefStructVoidMethodSetup<T>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T}.DoesNotThrow()" />
	IRefStructVoidMethodSetup<T> IRefStructVoidMethodSetup<T>.DoesNotThrow()
	{
		_returnAction = static () => null;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T}.Throws{TException}()" />
	IRefStructVoidMethodSetup<T> IRefStructVoidMethodSetup<T>.Throws<TException>()
	{
		_returnAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T}.Throws(Exception)" />
	IRefStructVoidMethodSetup<T> IRefStructVoidMethodSetup<T>.Throws(Exception exception)
	{
		_returnAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T}.Throws(Func{Exception})" />
	IRefStructVoidMethodSetup<T> IRefStructVoidMethodSetup<T>.Throws(Func<Exception> exceptionFactory)
	{
		_returnAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> _matcher is null
			? $"void {Name}(<{typeof(T).Name} ref struct>)"
			: $"void {Name}({_matcher})";
}

/// <summary>
///     Concrete ref-struct-compatible setup for a <see langword="void" /> method with two
///     parameters of which at least one is a ref struct. See <see cref="RefStructVoidMethodSetup{T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructVoidMethodSetup<T1, T2> : MethodSetup, IRefStructVoidMethodSetup<T1, T2>
	where T1 : allows ref struct
	where T2 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private Func<Exception?>? _returnAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructVoidMethodSetup{T1, T2}" />
	public RefStructVoidMethodSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
	}

	/// <summary>
	///     Checks whether this setup's matchers accept the given values.
	/// </summary>
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

	/// <summary>
	///     Invokes any matcher-level callbacks and applies the currently configured throw (if any).
	/// </summary>
	public void Invoke(T1 value1, T2 value2)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		if (_returnAction is null)
		{
			return;
		}

		Exception? exception = _returnAction();
		if (exception is not null)
		{
			throw exception;
		}
	}

	/// <summary>
	///     The flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2}.SkippingBaseClass(bool)" />
	IRefStructVoidMethodSetup<T1, T2> IRefStructVoidMethodSetup<T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2}.DoesNotThrow()" />
	IRefStructVoidMethodSetup<T1, T2> IRefStructVoidMethodSetup<T1, T2>.DoesNotThrow()
	{
		_returnAction = static () => null;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2}.Throws{TException}()" />
	IRefStructVoidMethodSetup<T1, T2> IRefStructVoidMethodSetup<T1, T2>.Throws<TException>()
	{
		_returnAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2}.Throws(Exception)" />
	IRefStructVoidMethodSetup<T1, T2> IRefStructVoidMethodSetup<T1, T2>.Throws(Exception exception)
	{
		_returnAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2}.Throws(Func{Exception})" />
	IRefStructVoidMethodSetup<T1, T2> IRefStructVoidMethodSetup<T1, T2>.Throws(Func<Exception> exceptionFactory)
	{
		_returnAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"void {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"})";
}

/// <summary>
///     Concrete ref-struct-compatible setup for a <see langword="void" /> method with three
///     parameters of which at least one is a ref struct. See <see cref="RefStructVoidMethodSetup{T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructVoidMethodSetup<T1, T2, T3> : MethodSetup, IRefStructVoidMethodSetup<T1, T2, T3>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private Func<Exception?>? _returnAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructVoidMethodSetup{T1, T2, T3}" />
	public RefStructVoidMethodSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_matcher3 = matcher3;
	}

	/// <summary>
	///     Checks whether this setup's matchers accept the given values.
	/// </summary>
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

	/// <summary>
	///     Invokes any matcher-level callbacks and applies the currently configured throw (if any).
	/// </summary>
	public void Invoke(T1 value1, T2 value2, T3 value3)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		_matcher3?.InvokeCallbacks(value3);
		if (_returnAction is null)
		{
			return;
		}

		Exception? exception = _returnAction();
		if (exception is not null)
		{
			throw exception;
		}
	}

	/// <summary>
	///     The flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3}.SkippingBaseClass(bool)" />
	IRefStructVoidMethodSetup<T1, T2, T3> IRefStructVoidMethodSetup<T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3}.DoesNotThrow()" />
	IRefStructVoidMethodSetup<T1, T2, T3> IRefStructVoidMethodSetup<T1, T2, T3>.DoesNotThrow()
	{
		_returnAction = static () => null;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3}.Throws{TException}()" />
	IRefStructVoidMethodSetup<T1, T2, T3> IRefStructVoidMethodSetup<T1, T2, T3>.Throws<TException>()
	{
		_returnAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3}.Throws(Exception)" />
	IRefStructVoidMethodSetup<T1, T2, T3> IRefStructVoidMethodSetup<T1, T2, T3>.Throws(Exception exception)
	{
		_returnAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3}.Throws(Func{Exception})" />
	IRefStructVoidMethodSetup<T1, T2, T3> IRefStructVoidMethodSetup<T1, T2, T3>.Throws(Func<Exception> exceptionFactory)
	{
		_returnAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"void {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"})";
}

/// <summary>
///     Concrete ref-struct-compatible setup for a <see langword="void" /> method with four
///     parameters of which at least one is a ref struct. See <see cref="RefStructVoidMethodSetup{T}" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructVoidMethodSetup<T1, T2, T3, T4> : MethodSetup, IRefStructVoidMethodSetup<T1, T2, T3, T4>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private readonly IParameterMatch<T4>? _matcher4;
	private Func<Exception?>? _returnAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructVoidMethodSetup{T1, T2, T3, T4}" />
	public RefStructVoidMethodSetup(string name,
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

	/// <summary>
	///     Checks whether this setup's matchers accept the given values.
	/// </summary>
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

	/// <summary>
	///     Invokes any matcher-level callbacks and applies the currently configured throw (if any).
	/// </summary>
	public void Invoke(T1 value1, T2 value2, T3 value3, T4 value4)
	{
		_matcher1?.InvokeCallbacks(value1);
		_matcher2?.InvokeCallbacks(value2);
		_matcher3?.InvokeCallbacks(value3);
		_matcher4?.InvokeCallbacks(value4);
		if (_returnAction is null)
		{
			return;
		}

		Exception? exception = _returnAction();
		if (exception is not null)
		{
			throw exception;
		}
	}

	/// <summary>
	///     The flag indicating if the base class implementation should be skipped.
	/// </summary>
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IRefStructVoidMethodSetup<T1, T2, T3, T4> IRefStructVoidMethodSetup<T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3, T4}.DoesNotThrow()" />
	IRefStructVoidMethodSetup<T1, T2, T3, T4> IRefStructVoidMethodSetup<T1, T2, T3, T4>.DoesNotThrow()
	{
		_returnAction = static () => null;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3, T4}.Throws{TException}()" />
	IRefStructVoidMethodSetup<T1, T2, T3, T4> IRefStructVoidMethodSetup<T1, T2, T3, T4>.Throws<TException>()
	{
		_returnAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3, T4}.Throws(Exception)" />
	IRefStructVoidMethodSetup<T1, T2, T3, T4> IRefStructVoidMethodSetup<T1, T2, T3, T4>.Throws(Exception exception)
	{
		_returnAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructVoidMethodSetup{T1, T2, T3, T4}.Throws(Func{Exception})" />
	IRefStructVoidMethodSetup<T1, T2, T3, T4> IRefStructVoidMethodSetup<T1, T2, T3, T4>.Throws(Func<Exception> exceptionFactory)
	{
		_returnAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"void {Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}, {_matcher4?.ToString() ?? $"<{typeof(T4).Name} ref struct>"})";
}
#endif
