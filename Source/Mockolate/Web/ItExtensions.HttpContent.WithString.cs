using System;
using System.Text;
using System.Text.RegularExpressions;
using Mockolate.Internals;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Web;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="IHttpContentParameter" />
	extension(IHttpContentParameter parameter)
	{
		/// <summary>
		///     Expects the string body to contain <paramref name="expected" />.
		/// </summary>
		/// <param name="expected">The substring that must appear in the decoded body.</param>
		/// <returns>A <see cref="IStringContentBodyParameter" /> for tightening to an exact match or ignoring case.</returns>
		/// <remarks>
		///     Matching is ordinal by default &#8212; chain <see cref="IStringContentBodyParameter.IgnoringCase" /> to
		///     compare case-insensitively, or <see cref="IStringContentBodyParameter.Exactly" /> to require the whole
		///     body to equal <paramref name="expected" /> rather than merely contain it.
		/// </remarks>
		public IStringContentBodyParameter WithString(string expected)
		{
			StringMatcher data = new(expected, isExact: true);
			StringContentParameter contentParameter = new(parameter, data);
			return contentParameter;
		}

		/// <summary>
		///     Expects the string body to match the wildcard <paramref name="pattern" /> (<c>*</c> = any sequence,
		///     <c>?</c> = any single character).
		/// </summary>
		/// <param name="pattern">The wildcard pattern to match against the decoded body.</param>
		/// <returns>
		///     A <see cref="IStringContentBodyMatchingParameter" /> that additionally supports
		///     <see cref="IStringContentBodyMatchingParameter.AsRegex" /> to switch the pattern semantics to
		///     <see cref="Regex" />.
		/// </returns>
		public IStringContentBodyMatchingParameter WithStringMatching(string pattern)
		{
			StringMatcher data = new(pattern, isExact: false);
			StringContentParameter contentParameter = new(parameter, data);
			return contentParameter;
		}
	}

	/// <summary>
	///     Further expectations on a wildcard/regex body match.
	/// </summary>
	public interface IStringContentBodyMatchingParameter : IStringContentBodyParameter
	{
		/// <summary>
		///     Interprets the previously supplied pattern as a <see cref="Regex" /> rather than a wildcard.
		/// </summary>
		/// <param name="options">Regex options to apply.</param>
		/// <param name="timeout">Optional match timeout; <see langword="null" /> disables the timeout.</param>
		/// <returns>The same parameter, for chaining additional body constraints.</returns>
		IStringContentBodyParameter AsRegex(
			RegexOptions options = RegexOptions.None,
			TimeSpan? timeout = null);
	}

	/// <summary>
	///     Further expectations on a string body match.
	/// </summary>
	public interface IStringContentBodyParameter : IHttpContentParameter
	{
		/// <summary>
		///     Performs the match case-insensitively.
		/// </summary>
		/// <returns>The same parameter, for chaining.</returns>
		IStringContentBodyParameter IgnoringCase();

		/// <summary>
		///     Requires the decoded body to equal the previously supplied string exactly, not just contain it.
		/// </summary>
		/// <returns>The same parameter, for chaining.</returns>
		IStringContentBodyParameter Exactly();
	}

	private sealed class StringMatcher(string value, bool isExact)
	{
		private BodyMatchType _bodyMatchType = isExact ? BodyMatchType.Exact : BodyMatchType.Wildcard;
		private bool _exactly;
		private bool _ignoringCase;
		private RegexOptions _regexOptions;
		private TimeSpan? _timeout;

		public bool Matches(string stringContent)
		{
			switch (_bodyMatchType, _exactly)
			{
				case (BodyMatchType.Exact, false) when
					!stringContent.Contains(value, _ignoringCase
						? StringComparison.OrdinalIgnoreCase
						: StringComparison.Ordinal):
					return false;
				case (BodyMatchType.Exact, true) when
					!stringContent.Equals(value, _ignoringCase
						? StringComparison.OrdinalIgnoreCase
						: StringComparison.Ordinal):
					return false;
				case (BodyMatchType.Wildcard, _) when
					!Wildcard.Pattern(value, _ignoringCase, _exactly).Matches(stringContent):
					return false;
				case (BodyMatchType.Regex, _):
					{
						Regex regex = new(value,
							_ignoringCase ? _regexOptions | RegexOptions.IgnoreCase : _regexOptions,
							_timeout ?? Regex.InfiniteMatchTimeout);
						if (!regex.IsMatch(stringContent))
						{
							return false;
						}

						break;
					}
			}

			return true;
		}

		public void AsRegex(
			RegexOptions options = RegexOptions.None,
			TimeSpan? timeout = null)
		{
			_regexOptions = options;
			_timeout = timeout;
			_bodyMatchType = BodyMatchType.Regex;
		}

		public void Exactly()
			=> _exactly = true;

		public void IgnoringCase()
			=> _ignoringCase = true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> (_bodyMatchType, _exactly, _ignoringCase) switch
			{
				(BodyMatchType.Wildcard, _, false) => $"matching pattern \"{value}\"",
				(BodyMatchType.Wildcard, _, true) => $"matching pattern \"{value}\" ignoring case",
				(BodyMatchType.Regex, _, false) when _regexOptions is RegexOptions.None => $"matching regex pattern \"{value}\"",
				(BodyMatchType.Regex, _, false) => $"matching regex pattern \"{value}\" with options {_regexOptions}",
				(BodyMatchType.Regex, _, true) when _regexOptions is RegexOptions.None => $"matching regex pattern \"{value}\" ignoring case",
				(BodyMatchType.Regex, _, true) => $"matching regex pattern \"{value}\" ignoring case with options {_regexOptions}",
				(BodyMatchType.Exact, true, false) => $"equal to \"{value}\"",
				(BodyMatchType.Exact, true, true) => $"equal to \"{value}\" ignoring case",
				(_, _, true) => $"containing \"{value}\" ignoring case",
				(_, _, _) => $"containing \"{value}\"",
			};

		private enum BodyMatchType
		{
			Exact,
			Wildcard,
			Regex,
		}
	}

	private sealed class StringContentParameter : HttpContentParameterWrapper, IStringContentBodyMatchingParameter
	{
		private readonly StringMatcher _data;

		public StringContentParameter(IHttpContentParameter parameter, StringMatcher data) : base(parameter, () =>
		{
			StringBuilder sb = new();
			sb.Append("string content ").Append(data);

			if (parameter is HttpContentParameter httpContentParameter)
			{
				httpContentParameter.AppendAdditionalDescription(sb);
			}

			return sb.ToString();
		})
		{
			_data = data;
			parameter.WithString(data.Matches);
		}

		public IStringContentBodyParameter IgnoringCase()
		{
			_data.IgnoringCase();
			return this;
		}

		public IStringContentBodyParameter Exactly()
		{
			_data.Exactly();
			return this;
		}

		public IStringContentBodyParameter AsRegex(RegexOptions options = RegexOptions.None, TimeSpan? timeout = null)
		{
			_data.AsRegex(options, timeout);
			return this;
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
