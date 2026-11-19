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
	///     matcher decides instead. For the only parameter of a method, a parameters match decides as well.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="value">The expected value or a matcher.</param>
	/// <param name="parameterName">
	///     The name of the parameter when it is the only one of the method, otherwise <see langword="null" />.
	/// </param>
	/// <returns>A parameter matcher for <paramref name="value" />.</returns>
	public static IParameterMatch<T> IsValueOrMatcher<T>(T value, string? parameterName)
		=> value switch
		{
			IParameter<T> parameter => parameter.AsParameterMatch(),
			IParameters parameters when parameterName is not null => new ParametersMatch<T>(parameters, parameterName),
			_ => new It.ParameterEqualsMatch<T>(value, null),
		};

	/// <summary>
	///     Matches <see langword="null" /> arguments and renders as <c>null</c> in failure messages.
	/// </summary>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <returns>A parameter matcher that only accepts <see langword="null" />.</returns>
	public static IParameterWithCallback<T> IsNull<T>()
		=> new It.NullParameterMatch<T>("null");

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class ParametersMatch<T>(IParameters parameters, string parameterName) : IParameterMatch<T>
	{
		public bool Matches(T value)
			=> parameters switch
			{
				IParametersMatch m => m.Matches([value,]),
				INamedParametersMatch m => m.Matches([(parameterName, value),]),
				_ => true,
			};

		public void InvokeCallbacks(T value)
		{
			// A parameters match has no callbacks.
		}

		public override string? ToString() => parameters.ToString();
	}
}
