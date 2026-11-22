namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The classes under test and mocked types that a <c>CreateWithMocks</c> call or a method with a
///     <c>IMockDataSource</c> attribute needs, together with the <paramref name="Mocks" /> to generate for them.
/// </summary>
/// <param name="Suts">The classes under test.</param>
/// <param name="MockedTypes">The interfaces of test method parameters, which get a mock.</param>
/// <param name="ValueTypes">
///     The other types of test method parameters, which get a value from the default values of the behavior.
/// </param>
/// <param name="Mocks">The mocks to generate.</param>
/// <param name="Location">
///     The location of the node, which lets auto-mocking bind it again to discover the mocks that the
///     <paramref name="Mocks" /> return.
/// </param>
internal sealed record DataSourceCreation(
	EquatableArray<SutClass> Suts,
	EquatableArray<string> MockedTypes,
	EquatableArray<string> ValueTypes,
	EquatableArray<MockClass> Mocks,
	CreateMockLocation Location)
{
	/// <summary>
	///     Whether the node needs generated code.
	/// </summary>
	public bool IsEmpty => Suts.Count == 0 && MockedTypes.Count == 0 && ValueTypes.Count == 0;
}
