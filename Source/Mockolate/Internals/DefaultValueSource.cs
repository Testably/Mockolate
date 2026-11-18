using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     How an <see cref="IDefaultValueGenerator" /> produces the default value of a type.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum DefaultValueSource
{
	/// <summary>The generator returns <see langword="null" /> for the type, whatever the arguments are.</summary>
	None,

	/// <summary>The generator returns the same value for the type with and without the arguments.</summary>
	WithoutArguments,

	/// <summary>The generator may use the arguments to produce the value.</summary>
	WithArguments,
}
