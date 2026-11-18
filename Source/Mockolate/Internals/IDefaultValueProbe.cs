using System;
using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     Lets an <see cref="IDefaultValueGenerator" /> tell upfront how it produces the default value of a type, so
///     that generated code only collects the invocation arguments for
///     <see cref="IDefaultValueGenerator.GenerateValue(Type, object?[])" /> when they are needed.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDefaultValueProbe
{
	/// <summary>
	///     Tells how <see cref="IDefaultValueGenerator.GenerateValue(Type, object?[])" /> answers for
	///     <paramref name="type" />. Must not run user code; when in doubt it answers
	///     <see cref="DefaultValueSource.WithArguments" />.
	/// </summary>
	DefaultValueSource Probe(Type type);
}
