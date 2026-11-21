using System.Diagnostics;
using Mockolate.Internals;

namespace Mockolate.Interactions;

/// <summary>
///     An invocation of a method without parameters.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class MethodInvocation(string name) : IMethodInteraction
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; } = name;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"invoke method {Name.SubstringAfterLast('.')}()";
}

/// <summary>
///     An invocation of a method with a single <paramref name="parameter1" />.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class MethodInvocation<T1>(string name, T1 parameter1) : IMethodInteraction
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	///     The single parameter value of the method.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"invoke method {Name.SubstringAfterLast('.')}({ValueFormatter.Format(Parameter1)})";
}

/// <summary>
///     An invocation of a method with two parameters <paramref name="parameter1" /> and <paramref name="parameter2" />.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class MethodInvocation<T1, T2>(string name, T1 parameter1, T2 parameter2) : IMethodInteraction
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	///     The first parameter value of the method.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the method.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"invoke method {Name.SubstringAfterLast('.')}({ValueFormatter.Format(Parameter1)}, {ValueFormatter.Format(Parameter2)})";
}

/// <summary>
///     An invocation of a method with three parameters <paramref name="parameter1" />, <paramref name="parameter2" /> and
///     <paramref name="parameter3" />.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class MethodInvocation<T1, T2, T3>(string name, T1 parameter1, T2 parameter2, T3 parameter3)
	: IMethodInteraction
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	///     The first parameter value of the method.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the method.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <summary>
	///     The third parameter value of the method.
	/// </summary>
	public T3 Parameter3 { get; } = parameter3;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"invoke method {Name.SubstringAfterLast('.')}({ValueFormatter.Format(Parameter1)}, {ValueFormatter.Format(Parameter2)}, {ValueFormatter.Format(Parameter3)})";
}

/// <summary>
///     An invocation of a method with four parameters <paramref name="parameter1" />, <paramref name="parameter2" />,
///     <paramref name="parameter3" /> and <paramref name="parameter4" />.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class MethodInvocation<T1, T2, T3, T4>(string name, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
	: IMethodInteraction
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	///     The first parameter value of the method.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the method.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <summary>
	///     The third parameter value of the method.
	/// </summary>
	public T3 Parameter3 { get; } = parameter3;

	/// <summary>
	///     The fourth parameter value of the method.
	/// </summary>
	public T4 Parameter4 { get; } = parameter4;

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"invoke method {Name.SubstringAfterLast('.')}({ValueFormatter.Format(Parameter1)}, {ValueFormatter.Format(Parameter2)}, {ValueFormatter.Format(Parameter3)}, {ValueFormatter.Format(Parameter4)})";
}
