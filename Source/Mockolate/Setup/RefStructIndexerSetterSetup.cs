#if NET9_0_OR_GREATER
using System;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Concrete setter setup for a ref-struct-keyed indexer with a single key of type
///     <typeparamref name="T" /> and value type <typeparamref name="TValue" />.
/// </summary>
/// <remarks>
///     <para>
///         Uses <see cref="RefStructMethodInvocation" /> with the CLR setter name convention
///         (<c>set_Item</c>) for interaction matching. <typeparamref name="TValue" /> is not
///         anti-constrained with <c>allows ref struct</c>: the setup stores an
///         <see cref="Action{TValue}" /> for the <c>OnSet</c> callback and a
///         <see cref="Func{Exception}" /> for the throw slot, both of which are illegal for a
///         ref-struct <typeparamref name="TValue" />. Indexers whose value type is itself a ref
///         struct are out of the scope of this setup.
///     </para>
///     <para>
///         When combined with a getter via <see cref="RefStructIndexerSetup{TValue, T}" /> and
///         a projection-bearing key matcher
///         (<see cref="It.IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" />),
///         setter writes are forwarded to the bound getter's storage dictionary so a subsequent
///         read of a key with the same projection returns the written value. A standalone
///         setter-only setup (with no bound getter) never stores — there is nothing to read the
///         value back from.
///     </para>
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetterSetup<TValue, T> : MethodSetup, IRefStructIndexerSetterSetup<TValue, T>
	where T : allows ref struct
{
	private readonly IParameterMatch<T>? _matcher;
	private Action<TValue>? _onSet;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}" />
	/// <param name="name">
	///     The fully qualified indexer setter name. Use <c>"set_Item"</c> to match the CLR
	///     convention unless the interface declares a custom IndexerName.
	/// </param>
	/// <param name="matcher">The key matcher. <see langword="null" /> matches any key.</param>
	public RefStructIndexerSetterSetup(string name, IParameterMatch<T>? matcher = null)
		: base(name)
	{
		_matcher = matcher;
	}

	/// <summary>
	///     The companion getter setup whose storage dictionary should receive writes performed
	///     via this setter. Set by the combined
	///     <see cref="RefStructIndexerSetup{TValue, T}" /> facade after both inner setups are
	///     constructed. <see langword="null" /> for setter-only setups — such setups never store.
	/// </summary>
	internal RefStructIndexerGetterSetup<TValue, T>? BoundGetter { get; set; }

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Matches(T)" />
	public bool Matches(T value)
		=> _matcher is null || _matcher.Matches(value);

	/// <summary>
	///     Invokes matcher callbacks, runs the configured <c>OnSet</c> callback (if any),
	///     applies the configured throw (if any), and finally — if a getter is bound and a
	///     projection matcher is in place — stores <paramref name="value" /> under the
	///     projected form of <paramref name="key" /> on the bound getter's storage.
	/// </summary>
	public void Invoke(T key, TValue value)
	{
		_matcher?.InvokeCallbacks(key);
		_onSet?.Invoke(value);

		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		BoundGetter?.StoreValue(key, value);
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T}.SkippingBaseClass(bool)" />
	IRefStructIndexerSetterSetup<TValue, T> IRefStructIndexerSetterSetup<TValue, T>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T}.OnSet(Action{TValue})" />
	IRefStructIndexerSetterSetup<TValue, T> IRefStructIndexerSetterSetup<TValue, T>.OnSet(Action<TValue> callback)
	{
		_onSet = callback;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T}.Throws{TException}()" />
	IRefStructIndexerSetterSetup<TValue, T> IRefStructIndexerSetterSetup<TValue, T>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T}.Throws(Exception)" />
	IRefStructIndexerSetterSetup<TValue, T> IRefStructIndexerSetterSetup<TValue, T>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T}.Throws(Func{Exception})" />
	IRefStructIndexerSetterSetup<TValue, T> IRefStructIndexerSetterSetup<TValue, T>.Throws(Func<Exception> exceptionFactory)
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
			? $"{Name}(<{typeof(T).Name} ref struct>) = {typeof(TValue).Name}"
			: $"{Name}({_matcher}) = {typeof(TValue).Name}";
}

/// <summary>
///     Concrete setter setup for a ref-struct-keyed indexer with two keys.
///     See <see cref="RefStructIndexerSetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     Projection-based write-then-read correlation is supported at arity 2+ when every
///     ref-struct slot carries an <see cref="IRefStructProjectionMatch{T}" /> matcher and every
///     non-ref-struct slot supplies a boxed raw key on
///     <see cref="Invoke(T1, T2, TValue, object?, object?)" />. The combined
///     <see cref="RefStructIndexerSetup{TValue, T1, T2}" /> facade wires
///     <see cref="BoundGetter" /> to forward setter writes into the companion getter's storage.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetterSetup<TValue, T1, T2> : MethodSetup,
	IRefStructIndexerSetterSetup<TValue, T1, T2>
	where T1 : allows ref struct
	where T2 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private Action<TValue>? _onSet;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T1, T2}" />
	public RefStructIndexerSetterSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
	}

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.BoundGetter" />
	internal RefStructIndexerGetterSetup<TValue, T1, T2>? BoundGetter { get; set; }

	/// <summary>
	///     Checks whether this setup's key matchers accept the given values.
	/// </summary>
	public bool Matches(T1 value1, T2 value2)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2));

	/// <summary>
	///     Invokes matcher callbacks, runs the <c>OnSet</c> callback, applies any throw, and —
	///     if a getter is bound — forwards the write to the getter's storage using the
	///     per-slot projections plus the caller-supplied <paramref name="rawKey1" />/<paramref name="rawKey2" />
	///     for non-ref-struct slots.
	/// </summary>
	public void Invoke(T1 k1, T2 k2, TValue value,
		object? rawKey1 = null, object? rawKey2 = null)
	{
		_matcher1?.InvokeCallbacks(k1);
		_matcher2?.InvokeCallbacks(k2);
		_onSet?.Invoke(value);

		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		BoundGetter?.StoreValue(k1, k2, value, rawKey1, rawKey2);
	}

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2}.SkippingBaseClass(bool)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2> IRefStructIndexerSetterSetup<TValue, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2}.OnSet(Action{TValue})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2> IRefStructIndexerSetterSetup<TValue, T1, T2>.OnSet(Action<TValue> callback)
	{
		_onSet = callback;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2}.Throws{TException}()" />
	IRefStructIndexerSetterSetup<TValue, T1, T2> IRefStructIndexerSetterSetup<TValue, T1, T2>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2}.Throws(Exception)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2> IRefStructIndexerSetterSetup<TValue, T1, T2>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2}.Throws(Func{Exception})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2> IRefStructIndexerSetterSetup<TValue, T1, T2>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}) = {typeof(TValue).Name}";
}

/// <summary>
///     Concrete setter setup for a ref-struct-keyed indexer with three keys.
///     See <see cref="RefStructIndexerSetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T1, T2}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetterSetup<TValue, T1, T2, T3> : MethodSetup,
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private Action<TValue>? _onSet;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T1, T2, T3}" />
	public RefStructIndexerSetterSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_matcher3 = matcher3;
	}

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.BoundGetter" />
	internal RefStructIndexerGetterSetup<TValue, T1, T2, T3>? BoundGetter { get; set; }

	/// <summary>
	///     Checks whether this setup's key matchers accept the given values.
	/// </summary>
	public bool Matches(T1 value1, T2 value2, T3 value3)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3));

	/// <summary>
	///     Invokes matcher callbacks, runs the <c>OnSet</c> callback, applies any throw, and —
	///     if a getter is bound — forwards the write to the getter's storage.
	/// </summary>
	public void Invoke(T1 k1, T2 k2, T3 k3, TValue value,
		object? rawKey1 = null, object? rawKey2 = null, object? rawKey3 = null)
	{
		_matcher1?.InvokeCallbacks(k1);
		_matcher2?.InvokeCallbacks(k2);
		_matcher3?.InvokeCallbacks(k3);
		_onSet?.Invoke(value);

		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		BoundGetter?.StoreValue(k1, k2, k3, value, rawKey1, rawKey2, rawKey3);
	}

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3}.SkippingBaseClass(bool)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3> IRefStructIndexerSetterSetup<TValue, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3}.OnSet(Action{TValue})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3> IRefStructIndexerSetterSetup<TValue, T1, T2, T3>.OnSet(Action<TValue> callback)
	{
		_onSet = callback;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3}.Throws{TException}()" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3> IRefStructIndexerSetterSetup<TValue, T1, T2, T3>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3}.Throws(Exception)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3> IRefStructIndexerSetterSetup<TValue, T1, T2, T3>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3}.Throws(Func{Exception})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3> IRefStructIndexerSetterSetup<TValue, T1, T2, T3>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}) = {typeof(TValue).Name}";
}

/// <summary>
///     Concrete setter setup for a ref-struct-keyed indexer with four keys.
///     See <see cref="RefStructIndexerSetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T1, T2}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> : MethodSetup,
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private readonly IParameterMatch<T4>? _matcher4;
	private Action<TValue>? _onSet;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}" />
	public RefStructIndexerSetterSetup(string name,
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

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.BoundGetter" />
	internal RefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>? BoundGetter { get; set; }

	/// <summary>
	///     Checks whether this setup's key matchers accept the given values.
	/// </summary>
	public bool Matches(T1 value1, T2 value2, T3 value3, T4 value4)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3))
		   && (_matcher4 is null || _matcher4.Matches(value4));

	/// <summary>
	///     Invokes matcher callbacks, runs the <c>OnSet</c> callback, applies any throw, and —
	///     if a getter is bound — forwards the write to the getter's storage.
	/// </summary>
	public void Invoke(T1 k1, T2 k2, T3 k3, T4 k4, TValue value,
		object? rawKey1 = null, object? rawKey2 = null, object? rawKey3 = null, object? rawKey4 = null)
	{
		_matcher1?.InvokeCallbacks(k1);
		_matcher2?.InvokeCallbacks(k2);
		_matcher3?.InvokeCallbacks(k3);
		_matcher4?.InvokeCallbacks(k4);
		_onSet?.Invoke(value);

		if (_throwAction is not null)
		{
			Exception? exception = _throwAction();
			if (exception is not null)
			{
				throw exception;
			}
		}

		BoundGetter?.StoreValue(k1, k2, k3, k4, value, rawKey1, rawKey2, rawKey3, rawKey4);
	}

	/// <inheritdoc cref="RefStructIndexerSetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}.SkippingBaseClass(bool)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}.OnSet(Action{TValue})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>.OnSet(Action<TValue> callback)
	{
		_onSet = callback;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}.Throws{TException}()" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}.Throws(Exception)" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerSetterSetup{TValue, T1, T2, T3, T4}.Throws(Func{Exception})" />
	IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && string.Equals(invocation.Name, Name, StringComparison.Ordinal);

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{Name}({_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}, {_matcher4?.ToString() ?? $"<{typeof(T4).Name} ref struct>"}) = {typeof(TValue).Name}";
}
#endif
