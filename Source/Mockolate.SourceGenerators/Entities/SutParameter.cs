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
/// <param name="IsNullForgiving">
///     Whether the parameter has a non-nullable reference type and a <c>null!</c> default, so the generated parameter is
///     nullable and passes its value on with <c>!</c>: a non-nullable one would warn at every call site that omits it
///     (CS8625), and a nullable one passed on as is would warn in the generated code (CS8604).
/// </param>
/// <param name="CallerInfoAttribute">The fully qualified caller info attribute of the parameter, if any.</param>
internal sealed record SutParameter(
	string TypeFullName,
	string TypeOfName,
	string Name,
	bool IsMockable,
	bool IsNullable,
	string? DefaultValue,
	bool IsParams,
	bool IsNullForgiving,
	string? CallerInfoAttribute)
{
	/// <summary>
	///     Whether the <c>CreateWithMocks</c> method lets the caller omit the argument.
	/// </summary>
	public bool IsOptional => IsMockable || IsParams || DefaultValue is not null;
}
