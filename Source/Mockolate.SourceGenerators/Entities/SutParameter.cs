namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A constructor parameter of a <see cref="SutClass" />.
/// </summary>
/// <param name="TypeFullName">The fully qualified type, with its nullable annotation.</param>
/// <param name="TypeOfName">The fully qualified type without a nullable reference annotation, valid in <c>typeof</c>.</param>
/// <param name="Name">The parameter name, without the <c>@</c> of a keyword.</param>
/// <param name="IsMockable">Whether the parameter gets a new mock when no argument is given.</param>
/// <param name="IsNullable">Whether <see langword="null" /> is a valid argument.</param>
/// <param name="DefaultValue">The explicit default value of the parameter, if any.</param>
/// <param name="IsParams">Whether the parameter is a <see langword="params" /> array.</param>
internal sealed record SutParameter(
	string TypeFullName,
	string TypeOfName,
	string Name,
	bool IsMockable,
	bool IsNullable,
	string? DefaultValue,
	bool IsParams)
{
	/// <summary>
	///     Whether the <c>CreateWithMocks</c> method lets the caller omit the argument.
	/// </summary>
	public bool IsOptional => IsMockable || IsParams || DefaultValue is not null;
}
