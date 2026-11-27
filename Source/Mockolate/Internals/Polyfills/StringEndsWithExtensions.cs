#if !NET11_0_OR_GREATER
using System;

namespace Mockolate.Internals.Polyfills;

/// <summary>
///     Provides the <see langword="char" /> overload of <see cref="string.EndsWith(string, StringComparison)" /> that was
///     added in .NET 11.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal static class StringEndsWithExtensions
{
	/// <summary>
	///     Determines whether the end of this string instance matches the specified character when compared using the
	///     specified comparison option.
	/// </summary>
	internal static bool EndsWith(
		this string @this,
		char value,
		StringComparison comparisonType)
		=> @this.EndsWith(value.ToString(), comparisonType);
}
#endif
