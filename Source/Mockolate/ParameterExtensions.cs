using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mockolate.Parameters;

namespace Mockolate;

/// <summary>
///     Extension methods for <see cref="IParameter" />s.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public static class ParameterExtensions
{
	/// <summary>
	///     Extensions for parameters with callbacks.
	/// </summary>
	/// <typeparam name="T">The parameter's value type.</typeparam>
	/// <param name="parameter">The parameter matcher to observe.</param>
	extension<T>(IParameterWithCallback<T> parameter)
	{
		/// <summary>
		///     Attaches a <paramref name="monitor" /> that records every argument value matched by this parameter.
		/// </summary>
		/// <param name="monitor">An out-parameter receiving the monitor; its <see cref="IParameterMonitor{T}.Values" /> list grows as the mock is invoked.</param>
		/// <returns>The same <paramref name="parameter" />, allowing further fluent calls.</returns>
		public IParameterWithCallback<T> Monitor(out IParameterMonitor<T> monitor)
		{
			ParameterMonitor<T> parameterMonitor = new();
			monitor = parameterMonitor;
			return parameter.Do(v => parameterMonitor.AddValue(v));
		}
	}

	/// <summary>
	///     Extensions for ref-parameters.
	/// </summary>
	/// <typeparam name="T">The ref-parameter's value type.</typeparam>
	/// <param name="parameter">The ref-parameter matcher to observe.</param>
	extension<T>(IRefParameter<T> parameter)
	{
		/// <summary>
		///     Attaches a <paramref name="monitor" /> that records every argument value matched by this ref-parameter.
		/// </summary>
		/// <param name="monitor">An out-parameter receiving the monitor; its <see cref="IParameterMonitor{T}.Values" /> list grows as the mock is invoked.</param>
		/// <returns>The same <paramref name="parameter" />, allowing further fluent calls.</returns>
		public IRefParameter<T> Monitor(out IParameterMonitor<T> monitor)
		{
			ParameterMonitor<T> parameterMonitor = new();
			parameter.Do(v => parameterMonitor.AddValue(v));
			monitor = parameterMonitor;
			return parameter;
		}
	}

	/// <summary>
	///     Extensions for out-parameters.
	/// </summary>
	/// <typeparam name="T">The out-parameter's value type.</typeparam>
	/// <param name="parameter">The out-parameter matcher to observe.</param>
	extension<T>(IOutParameter<T> parameter)
	{
		/// <summary>
		///     Attaches a <paramref name="monitor" /> that records every argument value matched by this out-parameter.
		/// </summary>
		/// <param name="monitor">An out-parameter receiving the monitor; its <see cref="IParameterMonitor{T}.Values" /> list grows as the mock is invoked.</param>
		/// <returns>The same <paramref name="parameter" />, allowing further fluent calls.</returns>
		public IOutParameter<T> Monitor(out IParameterMonitor<T> monitor)
		{
			ParameterMonitor<T> parameterMonitor = new();
			parameter.Do(v => parameterMonitor.AddValue(v));
			monitor = parameterMonitor;
			return parameter;
		}
	}

	/// <summary>
	///     Extensions for <see cref="double" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<double> parameter)
	{
		/// <summary>
		///     Relaxes the equality check to accept any value whose distance to the expected value is at most
		///     <paramref name="tolerance" />.
		/// </summary>
		/// <remarks>
		///     The bound is inclusive, but because binary floating point cannot represent every decimal fraction exactly,
		///     values that lie exactly on the bound may or may not match.
		///     <para />
		///     The returned matcher does not expose <see cref="It.IIsParameter{T}.Using" />, because a custom comparer
		///     would replace the tolerance again.
		/// </remarks>
		/// <param name="tolerance">The maximum allowed absolute difference (inclusive).</param>
		/// <param name="doNotPopulateThisValue">Do not populate - captured automatically by the compiler.</param>
		/// <returns>The same <paramref name="parameter" />, allowing callbacks to be attached.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///     <paramref name="tolerance" /> is negative or <see cref="double.NaN" />.
		/// </exception>
		public IParameterWithCallback<double> Within(double tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<double>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="double" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<double?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<double?> Within(double tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<double?>)parameter).Within(
				ForNullable<double>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for <see cref="float" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<float> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<float> Within(float tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<float>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="float" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<float?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<float?> Within(float tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<float?>)parameter).Within(
				ForNullable<float>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for <see cref="decimal" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<decimal> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<decimal> Within(decimal tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<decimal>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="decimal" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<decimal?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<decimal?> Within(decimal tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<decimal?>)parameter).Within(
				ForNullable<decimal>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for <see cref="DateTime" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<DateTime> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<DateTime> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<DateTime>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="DateTime" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<DateTime?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<DateTime?> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<DateTime?>)parameter).Within(
				ForNullable<DateTime>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for <see cref="DateTimeOffset" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<DateTimeOffset> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<DateTimeOffset> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<DateTimeOffset>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="DateTimeOffset" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<DateTimeOffset?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<DateTimeOffset?> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<DateTimeOffset?>)parameter).Within(
				ForNullable<DateTimeOffset>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for <see cref="TimeSpan" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<TimeSpan> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<TimeSpan> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<TimeSpan>)parameter).Within(
				(x, y) => IsWithinTolerance(x, y, tolerance), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Extensions for nullable <see cref="TimeSpan" /> equality matchers.
	/// </summary>
	/// <param name="parameter">The equality matcher created by <c>It.Is(value)</c>.</param>
	extension(It.IIsParameter<TimeSpan?> parameter)
	{
		/// <inheritdoc cref="extension(It.IIsParameter{double}).Within(double, string)" />
		public IParameterWithCallback<TimeSpan?> Within(TimeSpan tolerance,
			[CallerArgumentExpression(nameof(tolerance))]
			string doNotPopulateThisValue = "")
		{
			ThrowIfInvalidTolerance(tolerance);
			return ((It.IToleranceParameter<TimeSpan?>)parameter).Within(
				ForNullable<TimeSpan>((x, y) => IsWithinTolerance(x, y, tolerance)), doNotPopulateThisValue);
		}
	}

	/// <summary>
	///     Applies <paramref name="isWithinTolerance" /> when both values are present; otherwise <see langword="null" />
	///     only matches <see langword="null" />, as <see cref="EqualityComparer{T}.Default" /> does.
	/// </summary>
	private static Func<T?, T?, bool> ForNullable<T>(Func<T, T, bool> isWithinTolerance)
		where T : struct
		=> (x, y) => x.HasValue && y.HasValue
			? isWithinTolerance(x.Value, y.Value)
			: x.HasValue == y.HasValue;

	// Equals also accepts NaN and the infinities, for which the difference is NaN and never within the tolerance.
	private static bool IsWithinTolerance(double x, double y, double tolerance)
		=> x.Equals(y) || Math.Abs(x - y) <= tolerance;

	// Equals also accepts NaN and the infinities, for which the difference is NaN and never within the tolerance.
	private static bool IsWithinTolerance(float x, float y, float tolerance)
		=> x.Equals(y) || Math.Abs(x - y) <= tolerance;

	/// <summary>
	///     Whether <paramref name="x" /> and <paramref name="y" /> differ by at most <paramref name="tolerance" />.
	/// </summary>
	/// <remarks>
	///     Subtracting two values of opposite sign can overflow, so for that case the tolerance is subtracted from the
	///     larger value instead; as the tolerance is never negative, that direction always stays in range.
	/// </remarks>
	private static bool IsWithinTolerance(decimal x, decimal y, decimal tolerance)
	{
		decimal larger = x > y ? x : y;
		decimal smaller = x > y ? y : x;
		return larger >= 0m && smaller <= 0m
			? larger - tolerance <= smaller
			: larger - smaller <= tolerance;
	}

	/// <inheritdoc cref="IsWithinTolerance(decimal, decimal, decimal)" />
	private static bool IsWithinTolerance(long x, long y, long tolerance)
	{
		long larger = x > y ? x : y;
		long smaller = x > y ? y : x;
		return larger >= 0 && smaller <= 0
			? larger - tolerance <= smaller
			: larger - smaller <= tolerance;
	}

	// The difference between two DateTime values always fits into a TimeSpan, so it cannot overflow.
	private static bool IsWithinTolerance(DateTime x, DateTime y, TimeSpan tolerance)
		=> (x - y).Duration() <= tolerance;

	// The difference between two DateTimeOffset values always fits into a TimeSpan, so it cannot overflow.
	private static bool IsWithinTolerance(DateTimeOffset x, DateTimeOffset y, TimeSpan tolerance)
		=> (x - y).Duration() <= tolerance;

	private static bool IsWithinTolerance(TimeSpan x, TimeSpan y, TimeSpan tolerance)
		=> IsWithinTolerance(x.Ticks, y.Ticks, tolerance.Ticks);

	private static void ThrowIfInvalidTolerance(double tolerance)
	{
		if (double.IsNaN(tolerance) || tolerance < 0)
		{
			// ReSharper disable once LocalizableElement
			throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance must be a non-negative number.");
		}
	}

	private static void ThrowIfInvalidTolerance(decimal tolerance)
	{
		if (tolerance < 0m)
		{
			// ReSharper disable once LocalizableElement
			throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance must not be negative.");
		}
	}

	private static void ThrowIfInvalidTolerance(TimeSpan tolerance)
	{
		if (tolerance < TimeSpan.Zero)
		{
			// ReSharper disable once LocalizableElement
			throw new ArgumentOutOfRangeException(nameof(tolerance), "The tolerance must not be negative.");
		}
	}

	private sealed class ParameterMonitor<T> : IParameterMonitor<T>
	{
		private readonly List<T> _values = new();

		/// <inheritdoc cref="ParameterMonitor{T}" />
		public ParameterMonitor()
		{
			Values = _values.AsReadOnly();
		}

		/// <inheritdoc cref="IParameterMonitor{T}.Values" />
		public IReadOnlyList<T> Values { get; }

		public void AddValue(T value)
			=> _values.Add(value);
	}
}
