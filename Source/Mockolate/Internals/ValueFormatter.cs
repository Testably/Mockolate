using System;
using System.ComponentModel;
using System.Globalization;

namespace Mockolate.Internals;

/// <summary>
///     Formats argument values for failure messages and the descriptions of interactions.
/// </summary>
/// <remarks>
///     Public only because generated code in the user's assembly calls it; not intended for direct use from user
///     code.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ValueFormatter
{
	/// <summary>
	///     Formats <paramref name="value" /> independent of the current culture: <see langword="null" /> as <c>null</c>,
	///     strings in quotes, <see cref="IFormattable" /> values with <see cref="CultureInfo.InvariantCulture" /> and
	///     everything else with <see cref="object.ToString" />.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to format.</param>
	/// <returns>The text that represents <paramref name="value" />.</returns>
	public static string Format<T>(T value)
		=> value switch
		{
			null => "null",
			string s => $"\"{s}\"",
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "null",
			_ => value.ToString() ?? "null",
		};
}
