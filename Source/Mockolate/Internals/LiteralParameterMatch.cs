using System.Collections.Generic;
using System.ComponentModel;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Matches arguments equal to a literal value passed to a generated <c>Verify</c> overload that mixes literal
///     values and matchers, so the verification can use the typed fast path.
/// </summary>
/// <remarks>
///     Public only because generated code in the user's assembly calls it; not intended for direct use from user
///     code. It compares with <see cref="EqualityComparer{T}.Default" /> (expected value first) and renders the value
///     as string interpolation does, so failure messages read like the literal value itself.
/// </remarks>
/// <typeparam name="T">The declared type of the parameter.</typeparam>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LiteralParameterMatch<T> : IParameter<T>, IParameterMatch<T>
{
	private readonly T _value;

	/// <summary>
	///     Initializes a new <see cref="LiteralParameterMatch{T}" /> for the expected <paramref name="value" />.
	/// </summary>
	public LiteralParameterMatch(T value)
		=> _value = value;

	/// <inheritdoc cref="IParameter.Matches(object?)" />
	bool IParameter.Matches(object? value)
		=> value is null ? _value is null : value is T typedValue && Matches(typedValue);

	/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
	void IParameter.InvokeCallbacks(object? value)
	{
		// A literal value has no callbacks.
	}

	/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
	public bool Matches(T value)
		=> EqualityComparer<T>.Default.Equals(_value, value);

	/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
	public void InvokeCallbacks(T value)
	{
		// A literal value has no callbacks.
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"{_value}";
}
