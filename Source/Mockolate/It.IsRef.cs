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
	///     Matches any <see langword="ref" /> parameter of type <typeparamref name="T" /> and replaces its value with
	///     the result of <paramref name="setter" /> when the method is invoked.
	/// </summary>
	/// <remarks>
	///     <paramref name="setter" /> receives the caller's current value and returns the new one; this is how
	///     Mockolate mocks a method that mutates a ref argument. Use <see cref="IsRef{T}(Func{T, bool}, string)" />
	///     when you only want to match (without mutating), or <see cref="IsRef{T}()" /> for verification.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's type.</typeparam>
	/// <param name="setter">Factory that takes the caller's current value and returns the replacement value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> that mutates the caller's ref-variable via <paramref name="setter" />.</returns>
	public static IRefParameter<T> IsRef<T>(Func<T, T> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new RefParameterMatch<T>(_ => true, setter, null, doNotPopulateThisValue);

	/// <summary>
	///     Matches a <see langword="ref" /> parameter of type <typeparamref name="T" /> whose current value satisfies
	///     <paramref name="predicate" />, and replaces its value with the result of <paramref name="setter" />.
	/// </summary>
	/// <remarks>
	///     Combine a predicate gate with a value mutation. Both source expressions are captured by the compiler and
	///     appear in failure messages.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's type.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's current value.</param>
	/// <param name="setter">Factory that takes the caller's current value and returns the replacement value.</param>
	/// <param name="doNotPopulateThisValue1">Do not populate - captured automatically by the compiler.</param>
	/// <param name="doNotPopulateThisValue2">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> that matches when <paramref name="predicate" /> is satisfied and mutates via <paramref name="setter" />.</returns>
	public static IRefParameter<T> IsRef<T>(Func<T, bool> predicate, Func<T, T> setter,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue1 = "",
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue2 = "")
		=> new RefParameterMatch<T>(predicate, setter, doNotPopulateThisValue1, doNotPopulateThisValue2);

	/// <summary>
	///     Matches a <see langword="ref" /> parameter of type <typeparamref name="T" /> whose current value satisfies
	///     <paramref name="predicate" />, without replacing it.
	/// </summary>
	/// <remarks>
	///     Useful when you want to assert on the in-value of a ref argument (via <c>Verify</c>) without mutating it.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's type.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's current value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> that matches when <paramref name="predicate" /> is satisfied and does not mutate the ref-variable.</returns>
	public static IRefParameter<T> IsRef<T>(Func<T, bool> predicate,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue = "")
		=> new RefParameterMatch<T>(predicate, null, doNotPopulateThisValue, null);

	/// <summary>
	///     Matches any <see langword="ref" /> argument of type <typeparamref name="T" /> &#8212; for <c>Verify</c> only;
	///     use <see cref="IsAnyRef{T}" /> in <c>Setup</c>.
	/// </summary>
	/// <remarks>
	///     Accepts any ref-argument without constraint. For <c>Setup</c>, use one of the overloads that accept a
	///     <c>setter</c> to mutate the caller's value, or <see cref="IsAnyRef{T}" /> when you don't care about the value.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's type.</typeparam>
	/// <returns>An <see cref="IVerifyRefParameter{T}" /> that matches any ref-argument.</returns>
	public static IVerifyRefParameter<T> IsRef<T>()
		=> new InvokedRefParameterMatch<T>();

	/// <summary>
	///     Matches any <see langword="ref" /> parameter of type <typeparamref name="T" /> without replacing its value.
	/// </summary>
	/// <remarks>
	///     Unlike <see cref="IsRef{T}()" /> (which is only for verification), <see cref="IsAnyRef{T}" /> returns an
	///     <see cref="IRefParameter{T}" /> usable in <c>Setup</c>. Use it when the method has a <see langword="ref" />
	///     argument you don't care to inspect or mutate.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's type.</typeparam>
	/// <returns>An <see cref="IRefParameter{T}" /> that matches any ref-argument and leaves it unchanged.</returns>
	public static IRefParameter<T> IsAnyRef<T>()
		=> new AnyRefParameterMatch<T>();

#if NET8_0_OR_GREATER
	/// <summary>
	///     Matches any <see langword="ref" /> <see cref="Span{T}" /> parameter and replaces its value
	///     with the result of <paramref name="setter" /> when the method is invoked.
	/// </summary>
	/// <remarks>
	///     <see cref="Span{T}" /> is a ref struct, so the setup-side payload is the non-ref-struct
	///     <see cref="global::Mockolate.Setup.SpanWrapper{T}" />. The wrapper's implicit conversion
	///     operators carry the value across the ref boundary in both directions.
	/// </remarks>
	/// <typeparam name="T">The element type of the ref-<see cref="Span{T}" /> parameter.</typeparam>
	/// <param name="setter">Factory that takes the caller's wrapped current value and returns the replacement wrapper.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.SpanWrapper<T>> IsRefSpan<T>(
		Func<Setup.SpanWrapper<T>, Setup.SpanWrapper<T>> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new RefParameterMatch<Setup.SpanWrapper<T>>(_ => true, setter, null, doNotPopulateThisValue);

	/// <summary>
	///     Matches a <see langword="ref" /> <see cref="Span{T}" /> parameter whose wrapped current value
	///     satisfies <paramref name="predicate" />, and replaces its value with the result of
	///     <paramref name="setter" />.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="Span{T}" /> parameter.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's wrapped current value.</param>
	/// <param name="setter">Factory that takes the caller's wrapped current value and returns the replacement wrapper.</param>
	/// <param name="doNotPopulateThisValue1">Do not populate - captured automatically by the compiler.</param>
	/// <param name="doNotPopulateThisValue2">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.SpanWrapper<T>> IsRefSpan<T>(
		Func<Setup.SpanWrapper<T>, bool> predicate,
		Func<Setup.SpanWrapper<T>, Setup.SpanWrapper<T>> setter,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue1 = "",
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue2 = "")
		=> new RefParameterMatch<Setup.SpanWrapper<T>>(predicate, setter, doNotPopulateThisValue1, doNotPopulateThisValue2);

	/// <summary>
	///     Matches a <see langword="ref" /> <see cref="Span{T}" /> parameter whose wrapped current value
	///     satisfies <paramref name="predicate" />, without replacing it.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="Span{T}" /> parameter.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's wrapped current value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.SpanWrapper<T>> IsRefSpan<T>(
		Func<Setup.SpanWrapper<T>, bool> predicate,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue = "")
		=> new RefParameterMatch<Setup.SpanWrapper<T>>(predicate, null, doNotPopulateThisValue, null);

	/// <summary>
	///     Matches any <see langword="ref" /> <see cref="Span{T}" /> parameter without replacing its value.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="Span{T}" /> parameter.</typeparam>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.SpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.SpanWrapper<T>> IsAnyRefSpan<T>()
		=> new AnyRefParameterMatch<Setup.SpanWrapper<T>>();

	/// <summary>
	///     Matches any <see langword="ref" /> <see cref="ReadOnlySpan{T}" /> parameter and replaces its
	///     value with the result of <paramref name="setter" /> when the method is invoked.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <param name="setter">Factory that takes the caller's wrapped current value and returns the replacement wrapper.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.ReadOnlySpanWrapper<T>> IsRefReadOnlySpan<T>(
		Func<Setup.ReadOnlySpanWrapper<T>, Setup.ReadOnlySpanWrapper<T>> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		=> new RefParameterMatch<Setup.ReadOnlySpanWrapper<T>>(_ => true, setter, null, doNotPopulateThisValue);

	/// <summary>
	///     Matches a <see langword="ref" /> <see cref="ReadOnlySpan{T}" /> parameter whose wrapped current
	///     value satisfies <paramref name="predicate" />, and replaces its value with the result of
	///     <paramref name="setter" />.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's wrapped current value.</param>
	/// <param name="setter">Factory that takes the caller's wrapped current value and returns the replacement wrapper.</param>
	/// <param name="doNotPopulateThisValue1">Do not populate - captured automatically by the compiler.</param>
	/// <param name="doNotPopulateThisValue2">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.ReadOnlySpanWrapper<T>> IsRefReadOnlySpan<T>(
		Func<Setup.ReadOnlySpanWrapper<T>, bool> predicate,
		Func<Setup.ReadOnlySpanWrapper<T>, Setup.ReadOnlySpanWrapper<T>> setter,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue1 = "",
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue2 = "")
		=> new RefParameterMatch<Setup.ReadOnlySpanWrapper<T>>(predicate, setter, doNotPopulateThisValue1, doNotPopulateThisValue2);

	/// <summary>
	///     Matches a <see langword="ref" /> <see cref="ReadOnlySpan{T}" /> parameter whose wrapped current
	///     value satisfies <paramref name="predicate" />, without replacing it.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's wrapped current value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.ReadOnlySpanWrapper<T>> IsRefReadOnlySpan<T>(
		Func<Setup.ReadOnlySpanWrapper<T>, bool> predicate,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue = "")
		=> new RefParameterMatch<Setup.ReadOnlySpanWrapper<T>>(predicate, null, doNotPopulateThisValue, null);

	/// <summary>
	///     Matches any <see langword="ref" /> <see cref="ReadOnlySpan{T}" /> parameter without replacing
	///     its value.
	/// </summary>
	/// <typeparam name="T">The element type of the ref-<see cref="ReadOnlySpan{T}" /> parameter.</typeparam>
	/// <returns>An <see cref="IRefParameter{T}" /> over <see cref="global::Mockolate.Setup.ReadOnlySpanWrapper{T}" />.</returns>
	public static IRefParameter<Setup.ReadOnlySpanWrapper<T>> IsAnyRefReadOnlySpan<T>()
		=> new AnyRefParameterMatch<Setup.ReadOnlySpanWrapper<T>>();
#endif

	/// <summary>
	///     Matches a method <see langword="ref" /> parameter against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class RefParameterMatch<T>(
		Func<T, bool> predicate,
		Func<T, T>? setter,
		string? predicateExpression,
		string? setterExpression) : TypedRefMatch<T>
	{
		/// <inheritdoc cref="TypedRefMatch{T}.Matches(T)" />
		protected override bool Matches(T value)
			=> predicate(value);

		/// <inheritdoc cref="IRefParameter{T}.GetValue(T)" />
		public override T GetValue(T value)
		{
			if (setter is null)
			{
				return value;
			}

			return setter(value);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> (predicateExpression is not null, setterExpression is not null) switch
			{
				(true, true) => $"It.IsRef<{typeof(T).FormatType()}>({predicateExpression}, {setterExpression})",
				(true, false) => $"It.IsRef<{typeof(T).FormatType()}>({predicateExpression})",
				(false, _) => $"It.IsRef<{typeof(T).FormatType()}>({setterExpression})",
			};
	}

	/// <summary>
	///     Matches any method <see langword="ref" /> parameter.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyRefParameterMatch<T> : TypedRefMatch<T>
	{
		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"It.IsAnyRef<{typeof(T).FormatType()}>()";

		/// <inheritdoc cref="TypedRefMatch{T}.Matches(T)" />
		protected override bool Matches(T value)
			=> true;
	}

	/// <summary>
	///     Matches a method <see langword="ref" /> parameter against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class InvokedRefParameterMatch<T> : IVerifyRefParameter<T>, IParameterMatch<T>
#if NET9_0_OR_GREATER
		where T : allows ref struct
#endif
	{
		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		bool IParameterMatch<T>.Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		void IParameterMatch<T>.InvokeCallbacks(T value)
		{
			// Do nothing
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsRef<{typeof(T).FormatType()}>()";
	}

	/// <summary>
	///     Matches a method parameter of type <typeparamref name="T" /> against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private abstract class TypedRefMatch<T> : IRefParameter<T>, IParameterMatch<T>
	{
		private List<Action<T>>? _callbacks;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		bool IParameterMatch<T>.Matches(T value)
			=> Matches(value);

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		void IParameterMatch<T>.InvokeCallbacks(T value)
		{
			if (_callbacks is not null)
			{
				foreach (Action<T> callback in _callbacks)
				{
					callback.Invoke(value);
				}
			}
		}

		/// <inheritdoc cref="IRefParameter{T}.GetValue(T)" />
		public virtual T GetValue(T value)
			=> value;

		/// <inheritdoc cref="IRefParameter{T}.Do(Action{T})" />
		public IRefParameter<T> Do(Action<T> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		/// <summary>
		///     Verifies the expectation for the <paramref name="value" />.
		/// </summary>
		protected abstract bool Matches(T value);
	}

#if NET9_0_OR_GREATER
	/// <summary>
	///     Matches any <see langword="ref" /> parameter of a ref struct type
	///     <typeparamref name="T" /> and replaces its value with the result of
	///     <paramref name="setter" /> when the method is invoked.
	/// </summary>
	/// <remarks>
	///     The ref-struct-safe counterpart to <see cref="IsRef{T}(System.Func{T, T}, string)" /> does
	///     not support <see cref="IRefParameter{T}.Do(System.Action{T})" /> callbacks because
	///     <see cref="System.Action{T}" /> cannot carry the <c>allows ref struct</c> anti-constraint.
	///     <see cref="System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute" /> defers
	///     to the <see cref="System.Func{T, T}" /> overload when both are viable.
	/// </remarks>
	/// <typeparam name="T">The ref-parameter's ref struct type.</typeparam>
	/// <param name="setter">Factory that takes the caller's current value and returns the replacement value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefRefStructParameter{T}" /> that mutates the caller's ref-variable via <paramref name="setter" />.</returns>
	[OverloadResolutionPriority(-1)]
	public static IRefRefStructParameter<T> IsRef<T>(RefStructTransform<T> setter,
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue = "")
		where T : allows ref struct
		=> new RefRefStructParameterMatch<T>(static _ => true, setter, null, doNotPopulateThisValue);

	/// <summary>
	///     Matches a <see langword="ref" /> parameter of a ref struct type whose current value
	///     satisfies <paramref name="predicate" />, and replaces its value with the result of
	///     <paramref name="setter" />.
	/// </summary>
	/// <typeparam name="T">The ref-parameter's ref struct type.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's current value.</param>
	/// <param name="setter">Factory that takes the caller's current value and returns the replacement value.</param>
	/// <param name="doNotPopulateThisValue1">Do not populate - captured automatically by the compiler.</param>
	/// <param name="doNotPopulateThisValue2">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefRefStructParameter{T}" /> that matches when <paramref name="predicate" /> is satisfied and mutates via <paramref name="setter" />.</returns>
	[OverloadResolutionPriority(-1)]
	public static IRefRefStructParameter<T> IsRef<T>(RefStructPredicate<T> predicate, RefStructTransform<T> setter,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue1 = "",
		[CallerArgumentExpression("setter")] string doNotPopulateThisValue2 = "")
		where T : allows ref struct
		=> new RefRefStructParameterMatch<T>(predicate, setter, doNotPopulateThisValue1, doNotPopulateThisValue2);

	/// <summary>
	///     Matches a <see langword="ref" /> parameter of a ref struct type whose current value
	///     satisfies <paramref name="predicate" />, without replacing it.
	/// </summary>
	/// <typeparam name="T">The ref-parameter's ref struct type.</typeparam>
	/// <param name="predicate">The predicate evaluated against the caller's current value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>An <see cref="IRefRefStructParameter{T}" /> that matches when <paramref name="predicate" /> is satisfied and does not mutate the ref-variable.</returns>
	[OverloadResolutionPriority(-1)]
	public static IRefRefStructParameter<T> IsRef<T>(RefStructPredicate<T> predicate,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue = "")
		where T : allows ref struct
		=> new RefRefStructParameterMatch<T>(predicate, null, doNotPopulateThisValue, null);

	/// <summary>
	///     Matches any <see langword="ref" /> parameter of a ref struct type
	///     <typeparamref name="T" /> without replacing its value.
	/// </summary>
	/// <typeparam name="T">The ref-parameter's ref struct type.</typeparam>
	/// <returns>An <see cref="IRefRefStructParameter{T}" /> that matches any ref-argument and leaves it unchanged.</returns>
	public static IRefRefStructParameter<T> IsAnyRefRefStruct<T>()
		where T : allows ref struct
		=> new AnyRefRefStructParameterMatch<T>();

	/// <summary>
	///     Matches a method <see langword="ref" /> parameter of a ref struct type against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class RefRefStructParameterMatch<T>(
		RefStructPredicate<T> predicate,
		RefStructTransform<T>? setter,
		string? predicateExpression,
		string? setterExpression) : IRefRefStructParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		/// <inheritdoc cref="IRefRefStructParameter{T}.GetValue(T)" />
		public T GetValue(T value)
		{
			if (setter is null)
			{
				return value;
			}

			return setter(value);
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> predicate(value);

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			// No callbacks: Action<T> cannot carry the 'allows ref struct' anti-constraint.
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> (predicateExpression is not null, setterExpression is not null) switch
			{
				(true, true) => $"It.IsRef<{typeof(T).FormatType()}>({predicateExpression}, {setterExpression})",
				(true, false) => $"It.IsRef<{typeof(T).FormatType()}>({predicateExpression})",
				(false, _) => $"It.IsRef<{typeof(T).FormatType()}>({setterExpression})",
			};
	}

	/// <summary>
	///     Matches any method <see langword="ref" /> parameter of a ref struct type without
	///     mutating the value.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyRefRefStructParameterMatch<T> : IRefRefStructParameter<T>, IParameterMatch<T>
		where T : allows ref struct
	{
		/// <inheritdoc cref="IRefRefStructParameter{T}.GetValue(T)" />
		public T GetValue(T value)
			=> value;

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(T value)
			=> true;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(T value)
		{
			// No callbacks: Action<T> cannot carry the 'allows ref struct' anti-constraint.
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsAnyRefRefStruct<{typeof(T).FormatType()}>()";
	}

#endif
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
