namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The classes under test and mocked types that a <c>CreateWithMocks</c> call or a method with a
///     <c>IMockDataSource</c> attribute needs, together with the <paramref name="Mocks" /> to generate for them.
/// </summary>
internal sealed record DataSourceCreation(
	EquatableArray<SutClass> Suts,
	EquatableArray<string> MockedTypes,
	EquatableArray<MockClass> Mocks);
