using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Placeholder matcher for an <see langword="out" /> parameter of type <typeparamref name="T" />, intended
	///     for use in <c>Verify</c>.
	/// </summary>
	/// <remarks>
	///     Use this overload when you only want to assert that a method was invoked (with any out-argument).
	///     For <c>Setup</c>, use <see cref="IsOut{T}(Func{T}, string)" /> to actually produce an out-value, or
	///     <see cref="IsAnyOut{T}" /> to assign <see langword="default" /> to the caller's variable.
	/// </remarks>
	/// <typeparam name="T">The out-parameter's type.</typeparam>
	/// <returns>An <see cref="IVerifyOutParameter{T}" /> that matches any out-argument.</returns>
	public static IVerifyOutParameter<T> IsOut<T>()
		=> new InvokedOutParameterMatch<T>();

	/// <summary>
	///     Matches any <see langword="out" /> parameter of type <typeparamref name="T" /> in a <c>Setup</c> and uses
	///     <paramref name="setter" /> to produce the value assigned to the caller's variable when the method is
	///     invoked.
	/// </summary>
	/// <remarks>
	///     <paramref name="setter" /> runs on every matching invocation, so you can return a fresh value per call
	///     (e.g. a new <see cref="System.Guid" /> or an incrementing counter). Pair with
	///     <see cref="IOutParameter{T}.Do" /> to observe each produced value.
	/// </remarks>
	/// <typeparam name="T">The out-parameter's type.</typeparam>
	/// <param name="setter">Factory that produces the value to assign to the caller's out-variable.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IOutParameter{T}" /> that produces a value via <paramref name="setter" />.</returns>
	public static IOutParameter<T> IsOut<T>(Func<T> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new OutParameterMatch<T>(setter, doNotPopulateThisValue);

	/// <summary>
	///     Matches any <see langword="out" /> parameter of type <typeparamref name="T" /> in a <c>Setup</c> and assigns
	///     the value produced by the mock's <see cref="MockBehavior.DefaultValue" /> generator to the caller's variable.
	/// </summary>
	/// <remarks>
	///     Use this when the caller's out value is irrelevant to the test and you just need the method call to return.
	///     The produced value equals <see langword="default" /><c>(T)</c> unless a custom
	///     <see cref="MockBehavior.WithDefaultValueFor{T}(Func{T})" /> factory is registered; in that case the factory's
	///     value is used.
	/// </remarks>
	/// <typeparam name="T">The out-parameter's type.</typeparam>
	/// <returns>An <see cref="IOutParameter{T}" /> that produces a default value for the caller's out-variable.</returns>
	public static IOutParameter<T> IsAnyOut<T>()
		=> new AnyOutParameterMatch<T>();

#if NET8_0_OR_GREATER
	/// <summary>
	///     Matches any <see langword="out" /> <see cref="Span{T}" /> parameter in a <c>Setup</c> and uses
	///     <paramref name="setter" /> to produce the value assigned to the caller's variable when the
	///     method is invoked.
	/// </summary>
	/// <remarks>
	///     <see cref="Span{T}" /> is a ref struct, so the setup-side payload is the non-ref-struct
	///     <see cref="global::Mockolate.Setup.SpanWrapper{T}" />. The generated mock unpacks the wrapper
	///     into a temp local and assigns it to the caller's <see cref="Span{T}" /> via the wrapper's
	///     implicit conversion operator. Pair with
	///     <see cref="IOutParameter{T}.Do(Action{T})" /> to observe each produced wrapper.
	/// </remarks>
	/// <typeparam name="T">The element type of the out-<see cref="Span{T}" /> parameter.</typeparam>
	/// <param name="setter">Factory that produces the wrapper to assign to the caller's out-variable.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IOutParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IOutParameter<Setup.SpanWrapper<T>> IsOutSpan<T>(
		Func<Setup.SpanWrapper<T>> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new OutParameterMatch<Setup.SpanWrapper<T>>(setter, doNotPopulateThisValue);

	/// <summary>
	///     Matches any <see langword="out" /> <see cref="Span{T}" /> parameter in a <c>Setup</c> and
	///     assigns the default <see cref="global::Mockolate.Setup.SpanWrapper{T}" /> produced by the
	///     mock's <see cref="MockBehavior.DefaultValue" /> generator (which yields an empty
	///     <see cref="Span{T}" /> at the call site via the implicit conversion operator).
	/// </summary>
	/// <typeparam name="T">The element type of the out-<see cref="Span{T}" /> parameter.</typeparam>
	/// <returns>An <see cref="IOutParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IOutParameter<Setup.SpanWrapper<T>> IsAnyOutSpan<T>()
		=> new AnyOutParameterMatch<Setup.SpanWrapper<T>>();

	/// <summary>
	///     Matches any <see langword="out" /> <see cref="ReadOnlySpan{T}" /> parameter in a <c>Setup</c>
	///     and uses <paramref name="setter" /> to produce the value assigned to the caller's variable
	///     when the method is invoked.
	/// </summary>
	/// <remarks>
	///     <see cref="ReadOnlySpan{T}" /> is a ref struct, so the setup-side payload is the non-ref-struct
	///     <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />. Pair with
	///     <see cref="IOutParameter{T}.Do(Action{T})" /> to observe each produced wrapper.
	/// </remarks>
	/// <typeparam name="T">The element type of the out-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <param name="setter">Factory that produces the wrapper to assign to the caller's out-variable.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IOutParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IOutParameter<Setup.ReadOnlySpanWrapper<T>> IsOutReadOnlySpan<T>(
		Func<Setup.ReadOnlySpanWrapper<T>> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new OutParameterMatch<Setup.ReadOnlySpanWrapper<T>>(setter, doNotPopulateThisValue);

	/// <summary>
	///     Matches any <see langword="out" /> <see cref="ReadOnlySpan{T}" /> parameter in a <c>Setup</c>
	///     and assigns the default <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" /> produced
	///     by the mock's <see cref="MockBehavior.DefaultValue" /> generator.
	/// </summary>
	/// <typeparam name="T">The element type of the out-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <returns>An <see cref="IOutParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IOutParameter<Setup.ReadOnlySpanWrapper<T>> IsAnyOutReadOnlySpan<T>()
		=> new AnyOutParameterMatch<Setup.ReadOnlySpanWrapper<T>>();
#endif

	/// <summary>
	///     Matches an <see langword="out" /> parameter against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class OutParameterMatch<T>(Func<T> setter, string setterExpression) : TypedOutMatch<T>
	{
		/// <inheritdoc cref="IOutParameter{T}.TryGetValue(out T)" />
		public override bool TryGetValue(out T value)
		{
			value = setter();
			return true;
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsOut<{typeof(T).FormatType()}>({setterExpression})";
	}

	/// <summary>
	///     Matches an <see langword="out" /> parameter against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyOutParameterMatch<T> : TypedOutMatch<T>
	{
		/// <inheritdoc cref="IOutParameter{T}.TryGetValue(out T)" />
		public override bool TryGetValue(out T value)
		{
			value = default!;
			return false;
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsAnyOut<{typeof(T).FormatType()}>()";
	}

	/// <summary>
	///     Matches any <see langword="out" /> parameter.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class InvokedOutParameterMatch<T> : IVerifyOutParameter<T>, IParameterMatch<T>
#if NET9_0_OR_GREATER
		where T : allows ref struct
#endif
	{
		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsOut<{typeof(T).FormatType()}>()";

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			// Do nothing
		}
	}

	/// <summary>
	///     Matches a method parameter of type <typeparamref name="T" /> against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private abstract class TypedOutMatch<T> : IOutParameter<T>, IParameterMatch<T>
	{
		private List<Action<T>>? _callbacks;

		/// <inheritdoc cref="IOutParameter{T}.TryGetValue(out T)" />
		public abstract bool TryGetValue(out T value);

		/// <inheritdoc cref="IOutParameter{T}.Do(Action{T})" />
		public IOutParameter<T> Do(Action<T> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			if (_callbacks is not null)
			{
				_callbacks.ForEach(a => a.Invoke(value));
			}
		}
	}

#if NET9_0_OR_GREATER
	/// <summary>
	///     Matches any <see langword="out" /> parameter of a ref struct type
	///     <typeparamref name="T" /> in a <c>Setup</c> and uses <paramref name="setter" /> to
	///     produce the value assigned to the caller's variable when the method is invoked.
	/// </summary>
	/// <remarks>
	///     <paramref name="setter" /> runs on every matching invocation, so you can return a fresh
	///     value per call. The ref-struct-safe counterpart to
	///     <see cref="IsOut{T}(System.Func{T}, string)" /> does not support
	///     <see cref="IOutParameter{T}.Do(System.Action{T})" /> callbacks because
	///     <see cref="System.Action{T}" /> cannot carry the <c>allows ref struct</c> anti-constraint.
	///     <see cref="System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute" /> defers
	///     to the <see cref="System.Func{T}" /> overload when both are viable.
	/// </remarks>
	/// <typeparam name="T">The out-parameter's ref struct type.</typeparam>
	/// <param name="setter">Factory that produces the value to assign to the caller's out-variable.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IOutRefStructParameter{T}" /> that produces a value via <paramref name="setter" />.</returns>
	[OverloadResolutionPriority(-1)]
	public static IOutRefStructParameter<T> IsOut<T>(RefStructFactory<T> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		where T : allows ref struct
		=> new OutRefStructParameterMatch<T>(setter, doNotPopulateThisValue);

	/// <summary>
	///     Matches any <see langword="out" /> parameter of a ref struct type
	///     <typeparamref name="T" /> in a <c>Setup</c> and assigns
	///     <see langword="default" />(<typeparamref name="T" />) to the caller's variable.
	/// </summary>
	/// <remarks>
	///     Use this when the caller's out value is irrelevant to the test and you just need the
	///     method call to return. Unlike the non-ref-struct <see cref="IsAnyOut{T}" /> overload,
	///     the value is always <see langword="default" />(<typeparamref name="T" />) because
	///     <see cref="MockBehavior.DefaultValue" /> cannot produce ref-struct values.
	/// </remarks>
	/// <typeparam name="T">The out-parameter's ref struct type.</typeparam>
	/// <returns>An <see cref="IOutRefStructParameter{T}" /> that produces <see langword="default" />.</returns>
	public static IOutRefStructParameter<T> IsAnyOutRefStruct<T>()
		where T : allows ref struct
		=> new AnyOutRefStructParameterMatch<T>();

	/// <summary>
	///     Matches an <see langword="out" /> parameter of a ref struct type against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class OutRefStructParameterMatch<T>(RefStructFactory<T> setter, string setterExpression)
		: IOutRefStructParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		/// <inheritdoc cref="IOutRefStructParameter{T}.TryGetValue(out T)" />
		public bool TryGetValue(out T value)
		{
			value = setter();
			return true;
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			// No callbacks: Action<T> cannot carry the 'allows ref struct' anti-constraint.
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsOut<{typeof(T).FormatType()}>({setterExpression})";
	}

	/// <summary>
	///     Matches any <see langword="out" /> parameter of a ref struct type and assigns
	///     <see langword="default" /> to the caller's variable.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyOutRefStructParameterMatch<T> : IOutRefStructParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		/// <inheritdoc cref="IOutRefStructParameter{T}.TryGetValue(out T)" />
		public bool TryGetValue(out T value)
		{
			value = default!;
			return false;
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			// No callbacks: Action<T> cannot carry the 'allows ref struct' anti-constraint.
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsAnyOutRefStruct<{typeof(T).FormatType()}>()";
	}

#endif
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
