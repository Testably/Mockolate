using System;

namespace Mockolate;

/// <summary>
///     Declares the types of the values that an <see cref="IParameterValueProvider" /> attribute provides, so that the analyzer
///     reports a test parameter that cannot hold any of them.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProvidesValuesForAttribute : Attribute
{
	/// <param name="types">The types of the values that the attribute provides.</param>
	public ProvidesValuesForAttribute(params Type[]? types)
	{
		Types = types ?? Type.EmptyTypes;
	}

	/// <summary>
	///     The types of the values that the attribute provides.
	/// </summary>
	public Type[] Types { get; }
}
