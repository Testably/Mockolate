using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Mockolate.Internals;

#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal readonly struct Wildcard
{
	public Regex Regex { get; }

	private static readonly ConcurrentDictionary<(string RegexPattern, bool IgnoreCase), Wildcard> RegexCache = new();

	public static Wildcard Pattern(string pattern, bool ignoreCase, bool fullMatch = true)
	{
		Wildcard wildcard = RegexCache.GetOrAdd((ToRegularExpression(pattern, fullMatch), ignoreCase),
			item => new Wildcard(
				new Regex(
					item.RegexPattern,
					item.IgnoreCase
						? RegexOptions.IgnoreCase | RegexOptions.Compiled
						: RegexOptions.Compiled,
					Regex.InfiniteMatchTimeout)));
		return wildcard;
	}

	private Wildcard(Regex regex)
	{
		Regex = regex;
	}

	public bool Matches(string value)
		=> Regex.IsMatch(value);

	public static string ToRegularExpression(string pattern, bool fullMatch)
	{
		string regex = Regex.Escape(pattern)
#if NETSTANDARD2_0
			.Replace("\\?", ".")
			.Replace("\\*", "(?:.|\\n)*");
#else
			.Replace("\\?", ".", System.StringComparison.Ordinal)
			.Replace("\\*", "(?:.|\\n)*", System.StringComparison.Ordinal);
#endif
		if (fullMatch)
		{
			return $"^{regex}$";
		}

		return regex;
	}
}
