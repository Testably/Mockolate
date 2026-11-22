namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A class that <c>CreateWithMocks</c> or a <c>IMockDataSource</c> creates with mocked constructor dependencies.
/// </summary>
internal sealed record SutClass(string ClassFullName, string Name, EquatableArray<SutParameter> Parameters);
