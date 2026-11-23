namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The sealed class or record of a <c>T.CreateMock(out ...)</c> call, together with the <paramref name="Mocks" /> of
///     its constructor dependencies.
/// </summary>
internal sealed record SealedCreation(
	DataSourceClass? Class,
	EquatableArray<MockClass> Mocks,
	CreateMockLocation Location);
