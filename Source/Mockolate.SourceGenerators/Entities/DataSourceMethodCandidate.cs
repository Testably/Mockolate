namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A method with attributes and parameters, before it is bound: only methods whose
///     <paramref name="AttributeNames" /> match a data source attribute are bound.
/// </summary>
internal sealed record DataSourceMethodCandidate(
	EquatableArray<string> AttributeNames,
	CreateMockLocation Location);
