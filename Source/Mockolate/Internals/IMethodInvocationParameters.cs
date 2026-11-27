using System;
using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     Gives untyped access to the arguments of a recorded method invocation, so that a setup or verification for any
///     type argument of a generic method can inspect the calls of every closed method.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMethodInvocationParameters
{
	/// <summary>
	///     The number of parameters of the method.
	/// </summary>
	int ParameterCount { get; }

	/// <summary>
	///     Gets the argument at the zero-based <paramref name="index" />.
	/// </summary>
	object? GetParameterValueAt(int index);

	/// <summary>
	///     Gets the type that the parameter at the zero-based <paramref name="index" /> has in the called method, which
	///     tells overloads apart that the arguments alone do not.
	/// </summary>
	Type? GetParameterTypeAt(int index);
}
