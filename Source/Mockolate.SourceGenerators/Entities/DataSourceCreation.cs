namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     The classes and types that the parameters of a member with an <c>IMockDataSource</c> attribute need, together
///     with the <paramref name="Mocks" /> to generate for them.
/// </summary>
/// <param name="Classes">The classes that are created with mocked dependencies.</param>
/// <param name="MockedTypes">The interfaces, delegates and abstract classes that get a mock.</param>
/// <param name="ValueOrMockTypes">
///     The framework interfaces that get a value from the default values of the behavior, or else a mock.
/// </param>
/// <param name="ValueTypes">The other types, which get a value from the default values of the behavior.</param>
/// <param name="DeferredTypes">The types that the <c>Func&lt;T&gt;</c> of a <c>[Deferred]</c> parameter creates.</param>
/// <param name="Mocks">The mocks to generate.</param>
/// <param name="Location">
///     The location of the member, which lets auto-mocking bind it again to discover the mocks that the
///     <paramref name="Mocks" /> return.
/// </param>
internal sealed record DataSourceCreation(
	EquatableArray<DataSourceClass> Classes,
	EquatableArray<string> MockedTypes,
	EquatableArray<string> ValueOrMockTypes,
	EquatableArray<string> ValueTypes,
	EquatableArray<string> DeferredTypes,
	EquatableArray<MockClass> Mocks,
	CreateMockLocation Location)
{
	/// <summary>
	///     Whether the member needs no generated code.
	/// </summary>
	public bool IsEmpty => Classes.Count == 0 && MockedTypes.Count == 0 && ValueOrMockTypes.Count == 0 &&
	                       ValueTypes.Count == 0;
}
