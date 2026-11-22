namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The class under test of a <c>CreateWithMocks</c> call, together with the <paramref name="Mocks" /> to generate for
///     its dependencies.
/// </summary>
/// <remarks>
///     The <paramref name="Location" /> lets auto-mocking bind the call again to discover the mocks that the
///     <paramref name="Mocks" /> return.
/// </remarks>
internal sealed record SutCreation(
	SutClass? Sut,
	EquatableArray<MockClass> Mocks,
	CreateMockLocation Location);
