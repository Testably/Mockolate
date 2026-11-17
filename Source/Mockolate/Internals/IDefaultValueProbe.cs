using System;
using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     Lets an <see cref="IDefaultValueGenerator" /> tell upfront that it has no value for a type, so that
///     generated code can skip collecting the invocation arguments for
///     <see cref="IDefaultValueGenerator.GenerateValue(Type, object?[])" />.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDefaultValueProbe
{
	/// <summary>
	///     Returns <see langword="false" /> only when
	///     <see cref="IDefaultValueGenerator.GenerateValue(Type, object?[])" /> returns <see langword="null" /> for
	///     <paramref name="type" />, whatever the arguments are. Must not run user code.
	/// </summary>
	bool MayGenerateValue(Type type);
}
