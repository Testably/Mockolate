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

	/// <summary>
	///     Formats the recorded <paramref name="name" /> of a method without its containing type. The type arguments of a
	///     generic method are recorded with their full runtime names, so that setups of types with the same name in
	///     different namespaces stay apart, and are shown with their C# names, e.g. <c>List&lt;string&gt;</c>.
	/// </summary>
	/// <param name="name">The name of the method, prefixed with its containing type.</param>
	/// <returns>The name of the method, followed by its type arguments for a generic method.</returns>
	public static string FormatMethodName(string name)
	{
		int end = name.Length;
		if (name.EndsWith(">", StringComparison.Ordinal))
		{
			int depth = 0;
			for (int i = name.Length - 1; i >= 0; i--)
			{
				if (name[i] == '>')
				{
					depth++;
				}
				else if (name[i] == '<' && --depth == 0)
				{
					end = i;
					break;
				}
			}
		}

		int index = name.AsSpan(0, end).LastIndexOf('.');
		string methodName = name.Substring(index + 1, end - index - 1);
		return end == name.Length
			? methodName
			: $"{methodName}<{TypeFormatter.FormatTypeNames(name.Substring(end + 1, name.Length - end - 2))}>";
	}
}
