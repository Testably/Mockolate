using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Mockolate.Internals;

/// <summary>
///     The parameters of a constructor whose arguments Mockolate records in <see cref="ConstructorArguments" />.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly. A generated
///     mock keeps one instance per constructor, so that recording the arguments only allocates their values.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ConstructorSignature
{
	/// <summary>
	///     The parameters <paramref name="names" /> with the declared <paramref name="types" /> of the
	///     <paramref name="constructor" />.
	/// </summary>
	public ConstructorSignature(string constructor, string[] names, Type[] types)
	{
		Constructor = constructor;
		Names = names;
		Types = types;
	}

	internal string Constructor { get; }
	internal string[] Names { get; }
	internal Type[] Types { get; }

	/// <summary>
	///     The arguments that the constructor received, in the order of its parameters.
	/// </summary>
	public ConstructorArguments Record(object?[] values)
		=> new(this, values);
}
