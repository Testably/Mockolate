namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The classes under test and mocked types that a <c>CreateWithMocks</c> call or a method with a
///     <c>IMockDataSource</c> attribute needs, together with the <paramref name="Mocks" /> to generate for them.
/// </summary>
/// <remarks>
///     The <paramref name="Location" /> lets auto-mocking bind the node again to discover the mocks that the
///     <paramref name="Mocks" /> return.
/// </remarks>
internal sealed record DataSourceCreation(
	EquatableArray<SutClass> Suts,
	EquatableArray<string> MockedTypes,
	EquatableArray<MockClass> Mocks,
	CreateMockLocation Location);
