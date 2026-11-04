using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches a parameter that equals <paramref name="value" />.
	/// </summary>
	/// <remarks>
	///     Equality uses <see cref="EqualityComparer{T}.Default" /> unless overridden via
	///     <see cref="IIsParameter{T}.Using" />. For methods and indexers with up to four parameters you can also pass
	///     the raw value directly &#8212; Mockolate treats it as if it were wrapped in <c>It.Is(value)</c>. Prefer an
	///     explicit <c>It.Is(...)</c> when the call site would otherwise be ambiguous (e.g. when mixing matchers and
	///     raw values), or when you need <see cref="IIsParameter{T}.Using" /> for a custom comparer.
	///     <para />
	///     The expression passed as <paramref name="value" /> is captured by the compiler (via
	///     <see cref="CallerArgumentExpressionAttribute" />) and appears verbatim in failure messages, so keep
	///     expressions short to get readable diagnostics.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="value">The expected value.</param>
	/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
	/// <returns>A parameter matcher that only accepts arguments equal to <paramref name="value" />.</returns>
	public static IIsParameter<T> Is<T>(T value,
		[CallerArgumentExpression(nameof(value))]
		string doNotPopulateThisValue = "")
		=> new ParameterEqualsMatch<T>(value, doNotPopulateThisValue);

	/// <summary>
	///     An <see cref="IParameter{T}" /> used for equality comparison, with an opt-in custom comparer.
	/// </summary>
	public interface IIsParameter<out T> : IParameterWithCallback<T>
	{
		/// <summary>
		///     Switches equality comparison to use <paramref name="comparer" /> instead of
		///     <see cref="EqualityComparer{T}.Default" />.
		/// </summary>
		/// <remarks>
		///     Useful for case-insensitive string comparison or structural comparisons on types that don't override
		///     <see cref="object.Equals(object)" />.
		/// </remarks>
		IIsParameter<T> Using(IEqualityComparer<T> comparer,
			[CallerArgumentExpression(nameof(comparer))]
			string doNotPopulateThisValue = "");
	}

	/// <summary>
	///     Extension point behind <c>It.Is(value).Within(tolerance)</c>, used by <see cref="ParameterExtensions" />.
	/// </summary>
	/// <remarks>
	///     The tolerance overloads are type-specific and can therefore not be declared on the generic
	///     <see cref="IIsParameter{T}" />.
	/// </remarks>
	internal interface IToleranceParameter<T>
	{
		/// <summary>
		///     Switches equality comparison to <paramref name="isWithinTolerance" /> and renders
		///     <paramref name="toleranceExpression" /> in the <c>.Within(...)</c> suffix of <see cref="object.ToString" />.
		/// </summary>
		IParameterWithCallback<T> Within(Func<T, T, bool> isWithinTolerance, string toleranceExpression);
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	internal sealed class ParameterEqualsMatch<T> : TypedMatch<T>, IIsParameter<T>, IToleranceParameter<T>
	{
		private readonly T _value;
		private IEqualityComparer<T>? _comparer;
		private string? _comparerExpression;
		private Func<T, T, bool>? _isWithinTolerance;
		private string? _toleranceExpression;
		private string? _valueExpression;

		/// <summary>
		///     Constructs an equality matcher for <paramref name="value" />. When
		///     <paramref name="valueExpression" /> is non-<see langword="null" /> it is used verbatim in
		///     <see cref="ToString" />; pass <see langword="null" /> to defer formatting until the
		///     diagnostic is actually rendered (the path used by <see cref="Internals.Matchers.IsValue{T}" /> and
		///     generator-emitted by-value matchers, which skips the per-setup string allocation on the
		///     success path).
		/// </summary>
		public ParameterEqualsMatch(T value, string? valueExpression)
		{
			_value = value;
			_valueExpression = valueExpression;
		}

		/// <inheritdoc cref="IIsParameter{T}.Using(IEqualityComparer{T}, string)" />
		IIsParameter<T> IIsParameter<T>.Using(IEqualityComparer<T> comparer, string doNotPopulateThisValue)
		{
			_comparer = comparer;
			_comparerExpression = doNotPopulateThisValue;
			// A custom comparer and a tolerance are mutually exclusive; the last call wins.
			_isWithinTolerance = null;
			_toleranceExpression = null;
			return this;
		}

		/// <inheritdoc cref="IToleranceParameter{T}.Within(Func{T, T, bool}, string)" />
		IParameterWithCallback<T> IToleranceParameter<T>.Within(Func<T, T, bool> isWithinTolerance,
			string toleranceExpression)
		{
			_isWithinTolerance = isWithinTolerance;
			_toleranceExpression = toleranceExpression;
			// A custom comparer and a tolerance are mutually exclusive; the last call wins.
			_comparer = null;
			_comparerExpression = null;
			return this;
		}

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(T value1)
		{
			if (_isWithinTolerance is not null)
			{
				return _isWithinTolerance(value1, _value);
			}

			if (_comparer is not null)
			{
				return _comparer.Equals(value1, _value);
			}

			return EqualityComparer<T>.Default.Equals(value1, _value);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string expression = _valueExpression ??= FormatValueLazily(_value);
			if (_toleranceExpression is not null)
			{
				return $"It.Is({expression}).Within({_toleranceExpression})";
			}

			if (_comparer is not null)
			{
				return $"It.Is({expression}).Using({_comparerExpression})";
			}

			return expression;
		}

		private static string FormatValueLazily(T value)
		{
			if (value is null)
			{
				return "null";
			}

			if (value is string s)
			{
				return $"\"{s}\"";
			}

			if (value is IFormattable formattable)
			{
				return formattable.ToString(null, CultureInfo.InvariantCulture);
			}

			return value.ToString() ?? "null";
		}
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
