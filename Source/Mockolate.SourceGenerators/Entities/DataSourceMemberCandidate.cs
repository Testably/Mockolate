namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A method, constructor or type with attributes, before it is bound: only members whose
///     <paramref name="AttributeNames" /> match a data source attribute are bound.
/// </summary>
internal sealed record DataSourceMemberCandidate(
	EquatableArray<string> AttributeNames,
	CreateMockLocation Location);
