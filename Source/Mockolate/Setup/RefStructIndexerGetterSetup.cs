#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Concrete ref-struct-compatible indexer getter setup for a single ref struct key
///     of type <typeparamref name="T" />.
/// </summary>
/// <remarks>
///     <para>
///         Semantically equivalent to a method <c>TValue get_Item(T key)</c>. Uses
///         <see cref="RefStructMethodInvocation" /> with the CLR getter name convention
///         (<c>get_Item</c>) for interaction matching rather than introducing a separate indexer
///         interaction type. See <see cref="RefStructReturnMethodSetup{TReturn, T}" /> for the
///         underlying design rationale on why the full callback/builder chain is omitted.
///     </para>
///     <para>
///         When the configured key matcher is a projection matcher
///         (<see cref="IRefStructProjectionMatch{T}" />, produced by
///         <see cref="It.IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" />
///         and its predicated overload), the setup activates an internal storage dictionary
///         keyed by the boxed projection. The companion
///         <see cref="RefStructIndexerSetterSetup{TValue, T}" /> writes into this storage and
///         <see cref="Invoke(T, Func{TValue})" /> reads from it, providing write-then-read
///         correlation under the projected key.
///     </para>
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerGetterSetup<TValue, T> : MethodSetup, IRefStructIndexerGetterSetup<TValue, T>
	where T : allows ref struct
{
	private readonly IParameterMatch<T>? _matcher;
	private readonly IRefStructProjectionMatch<T>? _projectionMatcher;
	private readonly Dictionary<object, TValue>? _storage;
	private Func<TValue>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}" />
	/// <param name="name">
	///     The fully qualified indexer getter name. Use <c>"get_Item"</c> to match the CLR
	///     convention unless the interface declares a custom IndexerName.
	/// </param>
	/// <param name="matcher">
	///     The key matcher. <see langword="null" /> matches any key. If the matcher additionally
	///     implements <see cref="IRefStructProjectionMatch{T}" />, a projection-keyed storage
	///     dictionary is activated for setter→getter correlation.
	/// </param>
	public RefStructIndexerGetterSetup(string name, IParameterMatch<T>? matcher = null)
		: base(name)
	{
		_matcher = matcher;
		if (matcher is IRefStructProjectionMatch<T> projectionMatcher)
		{
			_projectionMatcher = projectionMatcher;
			_storage = [];
		}
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Matches(T)" />
	public bool Matches(T value)
		=> _matcher is null || _matcher.Matches(value);

	/// <summary>
	///     Attempts to read a previously-stored value for the projected form of
	///     <paramref name="key" />. Returns <see langword="false" /> if storage is inactive
	///     (no projection matcher was supplied) or no value has been stored for the projected
	///     key.
	/// </summary>
	internal bool TryGetStoredValue(T key, out TValue value)
	{
		if (_storage is null || _projectionMatcher is null)
		{
			value = default!;
			return false;
		}

		object projectedKey = _projectionMatcher.Project(key);
		return _storage.TryGetValue(projectedKey, out value!);
	}

	/// <summary>
	///     Stores <paramref name="value" /> under the projected form of <paramref name="key" />.
	///     No-op when storage is inactive, or when the configured matcher rejects the key.
	/// </summary>
	internal void StoreValue(T key, TValue value)
	{
		if (_storage is null || _projectionMatcher is null)
		{
			return;
		}

		if (_matcher is not null && !_matcher.Matches(key))
		{
			return;
		}

		object projectedKey = _projectionMatcher.Project(key);
		_storage[projectedKey] = value;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.Invoke(T, Func{TReturn})" />
	public TValue Invoke(T value, Func<TValue>? defaultFactory = null)
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

		if (_storage is not null && _projectionMatcher is not null)
		{
			object projectedKey = _projectionMatcher.Project(value);
			if (_storage.TryGetValue(projectedKey, out TValue? stored))
			{
				return stored;
			}
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.HasReturnValue" />
	/// <remarks>
	///     Also reports <see langword="true" /> when the setup has a projection matcher and
	///     therefore owns a storage dictionary — so that <see cref="Invoke(T, Func{TValue})" />
	///     is invoked on the return path and can serve stored values (or fall through to the
	///     default factory if the bucket is empty).
	/// </remarks>
	public bool HasReturnValue => _returnFactory is not null || _storage is not null;

	/// <inheritdoc cref="RefStructReturnMethodSetup{TReturn, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.SkippingBaseClass(bool)" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.Returns(TValue)" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.Returns(TValue returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.Returns(Func{TValue})" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.Returns(Func<TValue> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.Throws{TException}()" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.Throws(Exception)" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	/// <inheritdoc cref="IRefStructIndexerGetterSetup{TValue, T}.Throws(Func{Exception})" />
	IRefStructIndexerGetterSetup<TValue, T> IRefStructIndexerGetterSetup<TValue, T>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && invocation.Name == Name;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> _matcher is null
			? $"{typeof(TValue).Name} this[<{typeof(T).Name} ref struct>]"
			: $"{typeof(TValue).Name} this[{_matcher}]";
}

/// <summary>
///     Ref-struct-compatible indexer getter setup for arity 2. See <see cref="RefStructIndexerGetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <para>
///         Projection-based write-then-read storage is supported at arity 2+ when every
///         ref-struct slot carries a projection matcher
///         (<see cref="It.IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" />).
///         Non-ref-struct slots use their raw value (boxed by the caller and passed as
///         <c>rawKey_i</c>) as the dispatch key. If any ref-struct slot lacks a projection,
///         storage is not activated — setter writes will not feed back into getter reads.
///     </para>
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerGetterSetup<TValue, T1, T2> : MethodSetup, IRefStructIndexerGetterSetup<TValue, T1, T2>
	where T1 : allows ref struct
	where T2 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IRefStructProjectionMatch<T1>? _projection1;
	private readonly IRefStructProjectionMatch<T2>? _projection2;
	private readonly IndexerValueStorage<TValue>? _storage;
	private Func<TValue>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T1, T2}" />
	public RefStructIndexerGetterSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_projection1 = matcher1 as IRefStructProjectionMatch<T1>;
		_projection2 = matcher2 as IRefStructProjectionMatch<T2>;

		if (RefStructStorageHelper.IsSlotStorageReady(_projection1) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection2))
		{
			_storage = new IndexerValueStorage<TValue>();
		}
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2));

	/// <summary>
	///     Attempts to read a previously-stored value for the keys derived from the ref-struct
	///     projections and the caller-supplied <paramref name="rawKey1" />/<paramref name="rawKey2" />.
	///     Returns <see langword="false" /> if storage is inactive or the key path has not been
	///     written.
	/// </summary>
	internal bool TryGetStoredValue(T1 value1, T2 value2, object? rawKey1, object? rawKey2, out TValue value)
	{
		if (_storage is null ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2))
		{
			value = default!;
			return false;
		}

		IndexerValueStorage<TValue>? node = _storage.GetChild(key1)?.GetChild(key2);
		if (node is not null && node.HasValue)
		{
			value = node.Value;
			return true;
		}

		value = default!;
		return false;
	}

	/// <summary>
	///     Stores <paramref name="value" /> under the key path derived from the ref-struct
	///     projections and the caller-supplied <paramref name="rawKey1" />/<paramref name="rawKey2" />.
	///     No-op when storage is inactive or when the configured matchers reject the keys.
	/// </summary>
	internal void StoreValue(T1 value1, T2 value2, TValue value, object? rawKey1, object? rawKey2)
	{
		if (_storage is null || !Matches(value1, value2) ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2))
		{
			return;
		}

		IndexerValueStorage<TValue> leaf = _storage.GetOrAddChild(key1).GetOrAddChild(key2);
		leaf.Value = value;
		leaf.HasValue = true;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Invoke(T, Func{TValue})" />
	public TValue Invoke(T1 value1, T2 value2,
		object? rawKey1 = null, object? rawKey2 = null,
		Func<TValue>? defaultFactory = null)
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

		if (_storage is not null &&
		    TryGetStoredValue(value1, value2, rawKey1, rawKey2, out TValue stored))
		{
			return stored;
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null || _storage is not null;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.Returns(TValue returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.Returns(Func<TValue> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2> IRefStructIndexerGetterSetup<TValue, T1, T2>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && invocation.Name == Name;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TValue).Name} this[{_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}]";
}

/// <summary>
///     Ref-struct-compatible indexer getter setup for arity 3. See <see cref="RefStructIndexerGetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T1, T2}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerGetterSetup<TValue, T1, T2, T3> : MethodSetup, IRefStructIndexerGetterSetup<TValue, T1, T2, T3>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private readonly IRefStructProjectionMatch<T1>? _projection1;
	private readonly IRefStructProjectionMatch<T2>? _projection2;
	private readonly IRefStructProjectionMatch<T3>? _projection3;
	private readonly IndexerValueStorage<TValue>? _storage;
	private Func<TValue>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T1, T2, T3}" />
	public RefStructIndexerGetterSetup(string name,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null)
		: base(name)
	{
		_matcher1 = matcher1;
		_matcher2 = matcher2;
		_matcher3 = matcher3;
		_projection1 = matcher1 as IRefStructProjectionMatch<T1>;
		_projection2 = matcher2 as IRefStructProjectionMatch<T2>;
		_projection3 = matcher3 as IRefStructProjectionMatch<T3>;

		if (RefStructStorageHelper.IsSlotStorageReady(_projection1) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection2) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection3))
		{
			_storage = new IndexerValueStorage<TValue>();
		}
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2, T3 value3)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3));

	internal bool TryGetStoredValue(T1 value1, T2 value2, T3 value3,
		object? rawKey1, object? rawKey2, object? rawKey3, out TValue value)
	{
		if (_storage is null ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2) ||
		    !RefStructStorageHelper.TryResolveKey(_projection3, value3, rawKey3, out object key3))
		{
			value = default!;
			return false;
		}

		IndexerValueStorage<TValue>? node = _storage.GetChild(key1)?.GetChild(key2)?.GetChild(key3);
		if (node is not null && node.HasValue)
		{
			value = node.Value;
			return true;
		}

		value = default!;
		return false;
	}

	internal void StoreValue(T1 value1, T2 value2, T3 value3, TValue value,
		object? rawKey1, object? rawKey2, object? rawKey3)
	{
		if (_storage is null || !Matches(value1, value2, value3) ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2) ||
		    !RefStructStorageHelper.TryResolveKey(_projection3, value3, rawKey3, out object key3))
		{
			return;
		}

		IndexerValueStorage<TValue> leaf = _storage.GetOrAddChild(key1).GetOrAddChild(key2).GetOrAddChild(key3);
		leaf.Value = value;
		leaf.HasValue = true;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Invoke(T, Func{TValue})" />
	public TValue Invoke(T1 value1, T2 value2, T3 value3,
		object? rawKey1 = null, object? rawKey2 = null, object? rawKey3 = null,
		Func<TValue>? defaultFactory = null)
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

		if (_storage is not null &&
		    TryGetStoredValue(value1, value2, value3, rawKey1, rawKey2, rawKey3, out TValue stored))
		{
			return stored;
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null || _storage is not null;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.Returns(TValue returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.Returns(Func<TValue> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3> IRefStructIndexerGetterSetup<TValue, T1, T2, T3>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && invocation.Name == Name;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TValue).Name} this[{_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}]";
}

/// <summary>
///     Ref-struct-compatible indexer getter setup for arity 4. See <see cref="RefStructIndexerGetterSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T1, T2}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> : MethodSetup, IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	private readonly IParameterMatch<T1>? _matcher1;
	private readonly IParameterMatch<T2>? _matcher2;
	private readonly IParameterMatch<T3>? _matcher3;
	private readonly IParameterMatch<T4>? _matcher4;
	private readonly IRefStructProjectionMatch<T1>? _projection1;
	private readonly IRefStructProjectionMatch<T2>? _projection2;
	private readonly IRefStructProjectionMatch<T3>? _projection3;
	private readonly IRefStructProjectionMatch<T4>? _projection4;
	private readonly IndexerValueStorage<TValue>? _storage;
	private Func<TValue>? _returnFactory;
	private Func<Exception?>? _throwAction;
	private bool? _skipBaseClass;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T1, T2, T3, T4}" />
	public RefStructIndexerGetterSetup(string name,
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
		_projection1 = matcher1 as IRefStructProjectionMatch<T1>;
		_projection2 = matcher2 as IRefStructProjectionMatch<T2>;
		_projection3 = matcher3 as IRefStructProjectionMatch<T3>;
		_projection4 = matcher4 as IRefStructProjectionMatch<T4>;

		if (RefStructStorageHelper.IsSlotStorageReady(_projection1) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection2) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection3) &&
		    RefStructStorageHelper.IsSlotStorageReady(_projection4))
		{
			_storage = new IndexerValueStorage<TValue>();
		}
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Matches(T)" />
	public bool Matches(T1 value1, T2 value2, T3 value3, T4 value4)
		=> (_matcher1 is null || _matcher1.Matches(value1))
		   && (_matcher2 is null || _matcher2.Matches(value2))
		   && (_matcher3 is null || _matcher3.Matches(value3))
		   && (_matcher4 is null || _matcher4.Matches(value4));

	internal bool TryGetStoredValue(T1 value1, T2 value2, T3 value3, T4 value4,
		object? rawKey1, object? rawKey2, object? rawKey3, object? rawKey4, out TValue value)
	{
		if (_storage is null ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2) ||
		    !RefStructStorageHelper.TryResolveKey(_projection3, value3, rawKey3, out object key3) ||
		    !RefStructStorageHelper.TryResolveKey(_projection4, value4, rawKey4, out object key4))
		{
			value = default!;
			return false;
		}

		IndexerValueStorage<TValue>? node = _storage.GetChild(key1)?.GetChild(key2)?.GetChild(key3)?.GetChild(key4);
		if (node is not null && node.HasValue)
		{
			value = node.Value;
			return true;
		}

		value = default!;
		return false;
	}

	internal void StoreValue(T1 value1, T2 value2, T3 value3, T4 value4, TValue value,
		object? rawKey1, object? rawKey2, object? rawKey3, object? rawKey4)
	{
		if (_storage is null || !Matches(value1, value2, value3, value4) ||
		    !RefStructStorageHelper.TryResolveKey(_projection1, value1, rawKey1, out object key1) ||
		    !RefStructStorageHelper.TryResolveKey(_projection2, value2, rawKey2, out object key2) ||
		    !RefStructStorageHelper.TryResolveKey(_projection3, value3, rawKey3, out object key3) ||
		    !RefStructStorageHelper.TryResolveKey(_projection4, value4, rawKey4, out object key4))
		{
			return;
		}

		IndexerValueStorage<TValue> leaf = _storage.GetOrAddChild(key1).GetOrAddChild(key2).GetOrAddChild(key3).GetOrAddChild(key4);
		leaf.Value = value;
		leaf.HasValue = true;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.Invoke(T, Func{TValue})" />
	public TValue Invoke(T1 value1, T2 value2, T3 value3, T4 value4,
		object? rawKey1 = null, object? rawKey2 = null, object? rawKey3 = null, object? rawKey4 = null,
		Func<TValue>? defaultFactory = null)
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

		if (_storage is not null &&
		    TryGetStoredValue(value1, value2, value3, value4, rawKey1, rawKey2, rawKey3, rawKey4, out TValue stored))
		{
			return stored;
		}

		if (_returnFactory is not null)
		{
			return _returnFactory();
		}

		return defaultFactory is not null ? defaultFactory() : default!;
	}

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.HasReturnValue" />
	public bool HasReturnValue => _returnFactory is not null || _storage is not null;

	/// <inheritdoc cref="RefStructIndexerGetterSetup{TValue, T}.SkipBaseClass(MockBehavior)" />
	public bool SkipBaseClass(MockBehavior behavior)
		=> _skipBaseClass ?? behavior.SkipBaseClass;

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		_skipBaseClass = skipBaseClass;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.Returns(TValue returnValue)
	{
		_returnFactory = () => returnValue;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.Returns(Func<TValue> returnFactory)
	{
		_returnFactory = returnFactory;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.Throws<TException>()
	{
		_throwAction = static () => new TException();
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.Throws(Exception exception)
	{
		_throwAction = () => exception;
		return this;
	}

	IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>.Throws(Func<Exception> exceptionFactory)
	{
		_throwAction = exceptionFactory;
		return this;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(IMethodInteraction)" />
	protected override bool MatchesInteraction(IMethodInteraction interaction)
		=> interaction is RefStructMethodInvocation invocation && invocation.Name == Name;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{typeof(TValue).Name} this[{_matcher1?.ToString() ?? $"<{typeof(T1).Name} ref struct>"}, {_matcher2?.ToString() ?? $"<{typeof(T2).Name} ref struct>"}, {_matcher3?.ToString() ?? $"<{typeof(T3).Name} ref struct>"}, {_matcher4?.ToString() ?? $"<{typeof(T4).Name} ref struct>"}]";
}
#endif
