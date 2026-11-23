namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A class that a <c>IMockDataSource</c> creates with mocked constructor dependencies, either for a parameter or as
///     the constructor dependency of another such class.
/// </summary>
/// <param name="ClassFullName">The fully qualified name of the class.</param>
/// <param name="Parameters">The parameters of the constructor that creates the class.</param>
internal sealed record DataSourceClass(
	string ClassFullName,
	EquatableArray<DataSourceParameter> Parameters);
