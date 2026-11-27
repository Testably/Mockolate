namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A constructor parameter of a <see cref="DataSourceClass" />.
/// </summary>
/// <param name="TypeFullName">The fully qualified type without a nullable reference annotation, valid in <see langword="typeof" />.</param>
/// <param name="Name">The parameter name, without the <c>@</c> of a keyword.</param>
/// <param name="IsMockable">Whether the parameter gets a new mock when the resolver supplies no argument.</param>
/// <param name="IsMockedLast">
///     Whether the parameter gets a new mock only when the default values of the behavior provide no value.
/// </param>
/// <param name="IsParams">Whether the parameter is a <see langword="params" /> collection.</param>
/// <param name="DefaultValue">The explicit default value of the parameter, if any.</param>
/// <param name="IsCreatable">
///     Whether the parameter is a <see cref="DataSourceClass" /> that is created when the resolver supplies no argument.
/// </param>
/// <param name="IsNullable">Whether <see langword="null" /> is a valid argument.</param>
internal sealed record DataSourceParameter(
	string TypeFullName,
	string Name,
	bool IsMockable,
	bool IsMockedLast,
	bool IsParams,
	string? DefaultValue,
	bool IsCreatable,
	bool IsNullable);
