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
	///     Matches like <see cref="IsValue{T}" />, unless <paramref name="value" /> is a matcher: a parameter typed with a
	///     type parameter that the call closes over <see langword="object" /> takes every matcher as a value, so the
	///     matcher decides instead.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="value">The expected value or a matcher.</param>
	/// <returns>A parameter matcher for <paramref name="value" />.</returns>
	public static IParameterMatch<T> IsValueOrMatcher<T>(T value)
		=> AsMatcher(value) ?? new It.ParameterEqualsMatch<T>(value, null);

	/// <summary>
	///     The matcher that <paramref name="value" /> is, or <see langword="null" /> when it is a plain value.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="value">The expected value or a matcher.</param>
	/// <returns>The matcher for <paramref name="value" />, or <see langword="null" /> when it is no matcher.</returns>
	public static IParameterMatch<T>? AsMatcher<T>(T value)
		=> value is IParameter<T> parameter ? parameter.AsParameterMatch() : null;

	/// <summary>
	///     Matches the argument that a <see langword="null" /> literal stands for and renders as <c>null</c> in failure
	///     messages: <see langword="null" />, or the default value of a non-nullable value type.
	/// </summary>
	/// <remarks>
	///     A <see langword="null" /> literal for a non-nullable value type binds to the matcher overload in classic mode,
	///     where it stands for <c>default(T)</c>, as in union mode.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <returns>A parameter matcher that only accepts <c>default(T)</c>.</returns>
	public static IParameterWithCallback<T> IsNull<T>()
		=> new It.ParameterEqualsMatch<T>(default!, "null");
}
