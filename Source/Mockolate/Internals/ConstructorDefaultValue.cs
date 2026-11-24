using System.ComponentModel;
using System.Diagnostics;

namespace Mockolate.Internals;

/// <summary>
///     Provides the default values of constructor parameters for which Mockolate creates no argument.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConstructorDefaultValue
{
	/// <summary>
	///     The value that the <paramref name="behavior" /> configures for <typeparamref name="T" />, or else the
	///     <paramref name="typedDefault" />.
	/// </summary>
	/// <remarks>
	///     The typed <c>Generate</c> overloads return an empty collection without asking the generator, so a configured
	///     value is looked up first.
	/// </remarks>
	public static T Get<T>(MockBehavior behavior, T typedDefault)
		=> behavior.DefaultValue.GenerateValue(typeof(T)) is T configured ? configured : typedDefault;
}
