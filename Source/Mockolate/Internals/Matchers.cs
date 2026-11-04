using System.ComponentModel;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Parameter matchers emitted by the source generator.
/// </summary>
/// <remarks>
///     Public only because generated code in the user's assembly calls them; not intended for direct use
///     from user code. Use <see cref="It" /> instead.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Matchers
{
	/// <summary>
	///     Matches arguments equal to <paramref name="value" /> and defers formatting the value until the
	///     matcher's <see cref="object.ToString" /> is called.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="value">The expected value.</param>
	/// <returns>A parameter matcher that only accepts arguments equal to <paramref name="value" />.</returns>
	public static It.IIsParameter<T> IsValue<T>(T value)
		=> new It.ParameterEqualsMatch<T>(value, null);

	/// <summary>
	///     Matches <see langword="null" /> arguments and renders as <c>null</c> in failure messages.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <returns>A parameter matcher that only accepts <see langword="null" />.</returns>
	public static IParameterWithCallback<T> IsNull<T>()
		=> new It.NullParameterMatch<T>("null");
}
