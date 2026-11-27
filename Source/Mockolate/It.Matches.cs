using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mockolate.Internals;
using Mockolate.Parameters;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches a <see langword="string" /> parameter against the given wildcard <paramref name="pattern" />
	///     (<c>*</c> for any sequence, <c>?</c> for a single character).
	/// </summary>
	/// <remarks>
	///     Default comparison is case-insensitive; use <see cref="IParameterMatches.CaseSensitive(bool)" /> to change
	///     that, or <see cref="IParameterMatches.AsRegex" /> to treat <paramref name="pattern" /> as a
	///     <see cref="Regex" /> instead of a wildcard expression.
	/// </remarks>
	/// <param name="pattern">The wildcard (or, with <c>.AsRegex()</c>, regular-expression) pattern to match against.</param>
	/// <returns>A string parameter matcher, fluently configurable with <c>.CaseSensitive()</c> / <c>.AsRegex()</c>.</returns>
	public static IParameterMatches Matches(string pattern)
		=> new MatchesAsWildcardMatch(pattern);

	/// <summary>
	///     A <see langword="string" /> parameter matcher driven by a wildcard or regular-expression pattern.
	/// </summary>
	public interface IParameterMatches : IParameterWithCallback<string>
	{
		/// <summary>
		///     Switches the pattern comparison to be case-sensitive (default: case-insensitive).
		/// </summary>
		IParameterMatches CaseSensitive(bool caseSensitive = true);

		/// <summary>
		///     Interprets the pattern as a <see cref="Regex" /> rather than a wildcard expression.
		/// </summary>
		/// <param name="options">Regex options forwarded to <see cref="Regex" />.</param>
		/// <param name="timeout">Optional match timeout; defaults to <see cref="Regex.InfiniteMatchTimeout" />.</param>
		/// <param name="doNotPopulateThisValue1">Do not populate - captured automatically by the compiler.</param>
		/// <param name="doNotPopulateThisValue2">Do not populate - captured automatically by the compiler.</param>
		IParameterMatches AsRegex(
			RegexOptions options = RegexOptions.None,
			TimeSpan? timeout = null,
			[CallerArgumentExpression("options")] string doNotPopulateThisValue1 = "",
			[CallerArgumentExpression("timeout")] string doNotPopulateThisValue2 = "");
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class MatchesAsWildcardMatch(string pattern) : TypedMatch<string>, IParameterMatches
	{
		private bool _caseSensitive;
		private Regex? _regex;
		private RegexOptions _regexOptions = RegexOptions.None;
		private string? _regexOptionsExpression;
		private TimeSpan _timeout = Regex.InfiniteMatchTimeout;
		private string? _timeoutExpression;

		/// <inheritdoc cref="IParameterMatches.CaseSensitive" />
		public IParameterMatches CaseSensitive(bool caseSensitive = true)
		{
			_caseSensitive = caseSensitive;
			return this;
		}

		/// <inheritdoc cref="IParameterMatches.AsRegex(RegexOptions, TimeSpan?, string, string)" />
		public IParameterMatches AsRegex(RegexOptions options = RegexOptions.None,
			TimeSpan? timeout = null,
			[CallerArgumentExpression("options")] string doNotPopulateThisValue1 = "",
			[CallerArgumentExpression("timeout")] string doNotPopulateThisValue2 = "")
		{
			_regexOptions = options;
			_regexOptionsExpression = doNotPopulateThisValue1;
			if (timeout is not null)
			{
				_timeout = timeout.Value;
				_timeoutExpression = doNotPopulateThisValue2;
			}

			return this;
		}

#pragma warning disable S3218
		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(string value)
		{
			_regex ??= (_regexOptionsExpression is not null, _caseSensitive) switch
			{
				(false, _) => Wildcard.Pattern(pattern, !_caseSensitive).Regex,
				(true, false) => new Regex(pattern,
					_regexOptions | RegexOptions.IgnoreCase, _timeout),
				(true, true) => new Regex(pattern,
					_regexOptions, _timeout),
			};
			return _regex.IsMatch(value);
		}
#pragma warning restore S3218

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> (_regexOptionsExpression is not null, _caseSensitive) switch
			{
				(true, false) =>
					$"It.Matches(\"{pattern.Replace("\"", "\\\"", StringComparison.Ordinal)}\").AsRegex({RegexParameterToString(_timeout, _timeoutExpression, _regexOptionsExpression!)})",
				(true, true) =>
					$"It.Matches(\"{pattern.Replace("\"", "\\\"", StringComparison.Ordinal)}\").AsRegex({RegexParameterToString(_timeout, _timeoutExpression, _regexOptionsExpression!)}).CaseSensitive()",
				(false, false) => $"It.Matches(\"{pattern.Replace("\"", "\\\"", StringComparison.Ordinal)}\")",
				(false, true) => $"It.Matches(\"{pattern.Replace("\"", "\\\"", StringComparison.Ordinal)}\").CaseSensitive()",
			};

		private static string RegexParameterToString(TimeSpan timeout, string? timeoutExpression,
			string regexOptionsExpression)
		{
			if (timeout == Regex.InfiniteMatchTimeout)
			{
				return regexOptionsExpression;
			}

			if (string.IsNullOrEmpty(regexOptionsExpression))
			{
				return $"timeout: {timeoutExpression}";
			}

			return $"{regexOptionsExpression}, {timeoutExpression}";
		}
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
