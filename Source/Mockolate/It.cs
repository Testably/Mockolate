using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
/// <summary>
///     Specifies a matching condition for a single method or indexer parameter. Complement to
///     <see cref="Match" />, which targets the full parameter list at once.
/// </summary>
/// <remarks>
///     Use <see cref="It" /> matchers to describe expectations parameter-by-parameter inside a <c>Setup</c> or
///     <c>Verify</c> call. Commonly used matchers:
///     <list type="bullet">
///         <item>
///             <description><c>It.IsAny&lt;T&gt;()</c> / <c>It.Is(value)</c> - accept any value / accept a specific value (equality check, overridable comparer via <c>.Using(...)</c>).</description>
///         </item>
///         <item>
///             <description><c>It.IsNot(value)</c> / <c>It.IsNull&lt;T&gt;()</c> / <c>It.IsNotNull&lt;T&gt;()</c> / <c>It.IsTrue()</c> / <c>It.IsFalse()</c> - negative, null and boolean matchers.</description>
///         </item>
///         <item>
///             <description><c>It.IsOneOf(values)</c> / <c>It.IsNotOneOf(values)</c> / <c>It.IsInRange(min, max)</c> - set and range matchers.</description>
///         </item>
///         <item>
///             <description><c>It.Satisfies&lt;T&gt;(predicate)</c> / <c>It.Matches(pattern)</c> / <c>It.Contains(item)</c> / <c>It.SequenceEquals(values)</c> - predicate, wildcard and collection matchers.</description>
///         </item>
///         <item>
///             <description><c>It.IsOut&lt;T&gt;(...)</c> / <c>It.IsRef&lt;T&gt;(...)</c> / <c>It.IsSpan&lt;T&gt;(...)</c> / <c>It.IsReadOnlySpan&lt;T&gt;(...)</c> / <c>It.IsRefStruct&lt;T&gt;(...)</c> - <see langword="out" /> / <see langword="ref" /> / span / <see langword="ref struct" /> parameters.</description>
///         </item>
///     </list>
///     <para />
///     For methods and indexers with up to four parameters, a raw value can be passed directly in place of
///     <c>It.Is(value)</c>. Reach for <see cref="Match" /> when you want to match the full argument tuple with a
///     single predicate or to express "any arguments" via <c>Match.AnyParameters()</c>.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public partial class It
{
	/// <summary>
	///     This class is intentionally not static to allow adding static extension methods on <see cref="It" />.
	/// </summary>
	[ExcludeFromCodeCoverage]
	private It()
	{
		// Prevent instantiation.
	}

	/// <summary>
	///     Matches a method parameter of type <typeparamref name="T" /> against an expectation.
	/// </summary>
#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	internal abstract class TypedMatch<T> : IParameterWithCallback<T>, IParameterMatch<T>
	{
		private List<Action<T>>? _callbacks;

		/// <inheritdoc cref="IParameterWithCallback{T}.Do(Action{T})" />
		IParameterWithCallback<T> IParameterWithCallback<T>.Do(Action<T> callback)
			=> AddCallback(callback);

		/// <summary>
		///     Attaches a <paramref name="callback" /> to this matcher and returns the matcher to continue the fluent chain.
		/// </summary>
		/// <remarks>
		///     Default: mutates this instance's callback list. Override in cached/shared matcher instances to allocate a fresh
		///     mutable copy so the singleton never accumulates per-call callbacks.
		/// </remarks>
		protected virtual IParameterWithCallback<T> AddCallback(Action<T> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		void IParameterMatch<T>.InvokeCallbacks(T value)
			=> InvokeCallbacksCore(value);

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		bool IParameterMatch<T>.Matches(T value)
			=> Matches(value);

		/// <inheritdoc cref="IParameter.Matches(object?)" />
		bool IParameter.Matches(object? value)
		{
			if (value is T typedValue)
			{
				return Matches(typedValue);
			}

			if (value is null && default(T) is null)
			{
				return Matches(default!);
			}

			return MatchesOfDifferentType(value);
		}

		/// <summary>
		///     Called from <see cref="IParameter.Matches(object?)" /> when the incoming value is not of type
		///     <typeparamref name="T" /> and is not the default-null case. Returned when the matcher reached via covariant
		///     widening sees a value whose runtime type diverges from <typeparamref name="T" />.
		/// </summary>
		/// <remarks>
		///     Default: <see langword="false" /> (positive matchers — <c>Is</c>, <c>IsAny</c>, <c>IsOneOf</c>, etc. — cannot
		///     match a value that isn't even of their matched type). Override to <see langword="true" /> in negative
		///     matchers (<c>IsNot</c>, <c>IsNotOneOf</c>, <c>IsNotNull</c>) where a type mismatch also satisfies the
		///     "not equal / not null" semantic.
		/// </remarks>
		protected virtual bool MatchesOfDifferentType(object? value) => false;

		/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
		void IParameter.InvokeCallbacks(object? value)
		{
			if (value is T typedValue)
			{
				InvokeCallbacksCore(typedValue);
			}
			else if (value is null && default(T) is null)
			{
				InvokeCallbacksCore(default!);
			}
		}

		private void InvokeCallbacksCore(T value)
		{
			if (_callbacks is not null)
			{
				_callbacks.ForEach(a => a.Invoke(value));
			}
		}

		/// <summary>
		///     Verifies the expectation for the <paramref name="value" />.
		/// </summary>
		protected abstract bool Matches(T value);
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
