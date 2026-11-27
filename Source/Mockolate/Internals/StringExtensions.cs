using System;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Internals;

#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal static class StringExtensions
{
	internal static string SubstringUntilFirst(this string name, char c)
	{
		int index = name.IndexOf(c, StringComparison.Ordinal);
		if (index >= 0)
		{
			return name.Substring(0, index);
		}

		return name;
	}

	internal static string SubstringAfterLast(this string name, char c)
	{
		int index = name.AsSpan().LastIndexOf(c);
		if (index >= 0)
		{
			return name.Substring(index + 1);
		}

		return name;
	}
}
