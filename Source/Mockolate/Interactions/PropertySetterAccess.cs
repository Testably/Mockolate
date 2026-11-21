using System.Diagnostics;
using Mockolate.Internals;

namespace Mockolate.Interactions;

/// <summary>
///     An access of a property setter.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class PropertySetterAccess<T>(string propertyName, T value) : PropertyAccess(propertyName)
{
	/// <summary>
	///     The value the property was being set to.
	/// </summary>
	public T Value { get; } = value;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString() => $"set property {Name.SubstringAfterLast('.')} to {ValueFormatter.Format(Value)}";
}
