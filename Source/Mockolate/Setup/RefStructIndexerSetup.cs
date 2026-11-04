#if NET9_0_OR_GREATER
using System;
using Mockolate.Parameters;

namespace Mockolate.Setup;

/// <summary>
///     Concrete combined getter + setter setup for a ref-struct-keyed indexer with a single key
///     of type <typeparamref name="T" /> and value type <typeparamref name="TValue" />.
/// </summary>
/// <remarks>
///     <para>
///         Composes a <see cref="RefStructIndexerGetterSetup{TValue, T}" /> and a
///         <see cref="RefStructIndexerSetterSetup{TValue, T}" />. The two inner setups share the
///         same key matcher; <see cref="Getter" /> is registered under the CLR <c>get_Item</c>
///         name, <see cref="Setter" /> under <c>set_Item</c>. Both names are supplied at
///         construction so the facade can use fully-qualified accessor names consistent with
///         method setup naming.
///     </para>
///     <para>
///         When the supplied <c>matcher</c> is a projection matcher
///         (<see cref="Parameters.IRefStructProjectionMatch{T}" />, produced by
///         <see cref="It.IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" />),
///         the combined setup wires the setter's <c>BoundGetter</c> slot to point at the getter
///         so that writes performed through the setter are stored under the projected key and
///         subsequently returned by reads of any key with the same projection.
///     </para>
///     <para>
///         Without a projection matcher (e.g. <see cref="It.IsAnyRefStruct{T}" /> or
///         <see cref="It.IsRefStruct{T}" />), storage is not activated — the getter always
///         returns its configured <c>Returns(...)</c> value or the framework default, and the
///         setter's write is not surfaced on subsequent reads. Returning the framework default
///         under these circumstances is intentional: returning a stale or unprojected
///         last-write-wins value would be misleading.
///     </para>
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetup<TValue, T> : MethodSetup, IRefStructIndexerSetup<TValue, T>
	where T : allows ref struct
{
	/// <summary>
	///     The underlying getter setup. Registered with the mock registry under the getter name.
	/// </summary>
	public RefStructIndexerGetterSetup<TValue, T> Getter { get; }

	/// <summary>
	///     The underlying setter setup. Registered with the mock registry under the setter name.
	/// </summary>
	public RefStructIndexerSetterSetup<TValue, T> Setter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}" />
	public RefStructIndexerSetup(string getterName, string setterName, IParameterMatch<T>? matcher = null)
		: base(getterName)
	{
		Getter = new RefStructIndexerGetterSetup<TValue, T>(getterName, matcher);
		Setter = new RefStructIndexerSetterSetup<TValue, T>(setterName, matcher);
		Setter.BoundGetter = Getter;
	}

	/// <inheritdoc cref="MethodSetup.MatchesInteraction(Mockolate.Interactions.IMethodInteraction)" />
	/// <remarks>
	///     The combined setup itself is not registered directly with the registry; only
	///     <see cref="Getter" /> and <see cref="Setter" /> are. This override exists to satisfy the
	///     abstract contract and returns <see langword="false" /> so the combined instance never
	///     participates in <see cref="Internals.MockRegistry.GetMethodSetups{T}" /> lookups.
	/// </remarks>
	protected override bool MatchesInteraction(Interactions.IMethodInteraction interaction) => false;

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.SkippingBaseClass(bool skipBaseClass)
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).SkippingBaseClass(skipBaseClass);
		((IRefStructIndexerSetterSetup<TValue, T>)Setter).SkippingBaseClass(skipBaseClass);
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.Returns(TValue returnValue)
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).Returns(returnValue);
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.Returns(Func<TValue> returnFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).Returns(returnFactory);
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.OnSet(Action<TValue> callback)
	{
		((IRefStructIndexerSetterSetup<TValue, T>)Setter).OnSet(callback);
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.Throws<TException>()
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).Throws<TException>();
		((IRefStructIndexerSetterSetup<TValue, T>)Setter).Throws<TException>();
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.Throws(Exception exception)
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).Throws(exception);
		((IRefStructIndexerSetterSetup<TValue, T>)Setter).Throws(exception);
		return this;
	}

	IRefStructIndexerSetup<TValue, T> IRefStructIndexerSetup<TValue, T>.Throws(Func<Exception> exceptionFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T>)Getter).Throws(exceptionFactory);
		((IRefStructIndexerSetterSetup<TValue, T>)Setter).Throws(exceptionFactory);
		return this;
	}
}

/// <summary>
///     Concrete combined getter + setter setup for a ref-struct-keyed indexer with two keys.
///     See <see cref="RefStructIndexerSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerSetup{TValue, T}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetup<TValue, T1, T2> : MethodSetup, IRefStructIndexerSetup<TValue, T1, T2>
	where T1 : allows ref struct
	where T2 : allows ref struct
{
	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Getter" />
	public RefStructIndexerGetterSetup<TValue, T1, T2> Getter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Setter" />
	public RefStructIndexerSetterSetup<TValue, T1, T2> Setter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T1, T2}" />
	public RefStructIndexerSetup(string getterName, string setterName,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null)
		: base(getterName)
	{
		Getter = new RefStructIndexerGetterSetup<TValue, T1, T2>(getterName, matcher1, matcher2);
		Setter = new RefStructIndexerSetterSetup<TValue, T1, T2>(setterName, matcher1, matcher2);
		Setter.BoundGetter = Getter;
	}

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.MatchesInteraction(Mockolate.Interactions.IMethodInteraction)" />
	protected override bool MatchesInteraction(Interactions.IMethodInteraction interaction) => false;

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.SkippingBaseClass(bool skipBaseClass)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).SkippingBaseClass(skipBaseClass);
		((IRefStructIndexerSetterSetup<TValue, T1, T2>)Setter).SkippingBaseClass(skipBaseClass);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.Returns(TValue returnValue)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).Returns(returnValue);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.Returns(Func<TValue> returnFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).Returns(returnFactory);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.OnSet(Action<TValue> callback)
	{
		((IRefStructIndexerSetterSetup<TValue, T1, T2>)Setter).OnSet(callback);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.Throws<TException>()
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).Throws<TException>();
		((IRefStructIndexerSetterSetup<TValue, T1, T2>)Setter).Throws<TException>();
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.Throws(Exception exception)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).Throws(exception);
		((IRefStructIndexerSetterSetup<TValue, T1, T2>)Setter).Throws(exception);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2> IRefStructIndexerSetup<TValue, T1, T2>.Throws(Func<Exception> exceptionFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2>)Getter).Throws(exceptionFactory);
		((IRefStructIndexerSetterSetup<TValue, T1, T2>)Setter).Throws(exceptionFactory);
		return this;
	}
}

/// <summary>
///     Concrete combined getter + setter setup for a ref-struct-keyed indexer with three keys.
///     See <see cref="RefStructIndexerSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerSetup{TValue, T}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetup<TValue, T1, T2, T3> : MethodSetup,
	IRefStructIndexerSetup<TValue, T1, T2, T3>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
{
	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Getter" />
	public RefStructIndexerGetterSetup<TValue, T1, T2, T3> Getter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Setter" />
	public RefStructIndexerSetterSetup<TValue, T1, T2, T3> Setter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T1, T2, T3}" />
	public RefStructIndexerSetup(string getterName, string setterName,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null)
		: base(getterName)
	{
		Getter = new RefStructIndexerGetterSetup<TValue, T1, T2, T3>(getterName, matcher1, matcher2, matcher3);
		Setter = new RefStructIndexerSetterSetup<TValue, T1, T2, T3>(setterName, matcher1, matcher2, matcher3);
		Setter.BoundGetter = Getter;
	}

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.MatchesInteraction(Mockolate.Interactions.IMethodInteraction)" />
	protected override bool MatchesInteraction(Interactions.IMethodInteraction interaction) => false;

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.SkippingBaseClass(bool skipBaseClass)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).SkippingBaseClass(skipBaseClass);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3>)Setter).SkippingBaseClass(skipBaseClass);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.Returns(TValue returnValue)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).Returns(returnValue);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.Returns(Func<TValue> returnFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).Returns(returnFactory);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.OnSet(Action<TValue> callback)
	{
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3>)Setter).OnSet(callback);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.Throws<TException>()
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).Throws<TException>();
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3>)Setter).Throws<TException>();
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.Throws(Exception exception)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).Throws(exception);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3>)Setter).Throws(exception);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3> IRefStructIndexerSetup<TValue, T1, T2, T3>.Throws(Func<Exception> exceptionFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3>)Getter).Throws(exceptionFactory);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3>)Setter).Throws(exceptionFactory);
		return this;
	}
}

/// <summary>
///     Concrete combined getter + setter setup for a ref-struct-keyed indexer with four keys.
///     See <see cref="RefStructIndexerSetup{TValue, T}" />.
/// </summary>
/// <remarks>
///     <inheritdoc cref="RefStructIndexerSetup{TValue, T}" path="/remarks" />
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public sealed class RefStructIndexerSetup<TValue, T1, T2, T3, T4> : MethodSetup,
	IRefStructIndexerSetup<TValue, T1, T2, T3, T4>
	where T1 : allows ref struct
	where T2 : allows ref struct
	where T3 : allows ref struct
	where T4 : allows ref struct
{
	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Getter" />
	public RefStructIndexerGetterSetup<TValue, T1, T2, T3, T4> Getter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.Setter" />
	public RefStructIndexerSetterSetup<TValue, T1, T2, T3, T4> Setter { get; }

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T1, T2, T3, T4}" />
	public RefStructIndexerSetup(string getterName, string setterName,
		IParameterMatch<T1>? matcher1 = null,
		IParameterMatch<T2>? matcher2 = null,
		IParameterMatch<T3>? matcher3 = null,
		IParameterMatch<T4>? matcher4 = null)
		: base(getterName)
	{
		Getter = new RefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>(getterName, matcher1, matcher2, matcher3, matcher4);
		Setter = new RefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>(setterName, matcher1, matcher2, matcher3, matcher4);
		Setter.BoundGetter = Getter;
	}

	/// <inheritdoc cref="RefStructIndexerSetup{TValue, T}.MatchesInteraction(Mockolate.Interactions.IMethodInteraction)" />
	protected override bool MatchesInteraction(Interactions.IMethodInteraction interaction) => false;

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.SkippingBaseClass(bool skipBaseClass)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).SkippingBaseClass(skipBaseClass);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>)Setter).SkippingBaseClass(skipBaseClass);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.Returns(TValue returnValue)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).Returns(returnValue);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.Returns(Func<TValue> returnFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).Returns(returnFactory);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.OnSet(Action<TValue> callback)
	{
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>)Setter).OnSet(callback);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.Throws<TException>()
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).Throws<TException>();
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>)Setter).Throws<TException>();
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.Throws(Exception exception)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).Throws(exception);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>)Setter).Throws(exception);
		return this;
	}

	IRefStructIndexerSetup<TValue, T1, T2, T3, T4> IRefStructIndexerSetup<TValue, T1, T2, T3, T4>.Throws(Func<Exception> exceptionFactory)
	{
		((IRefStructIndexerGetterSetup<TValue, T1, T2, T3, T4>)Getter).Throws(exceptionFactory);
		((IRefStructIndexerSetterSetup<TValue, T1, T2, T3, T4>)Setter).Throws(exceptionFactory);
		return this;
	}
}
#endif
