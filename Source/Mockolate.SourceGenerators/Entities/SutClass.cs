namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A class that <c>CreateWithMocks</c> or a <c>IMockDataSource</c> creates with mocked constructor dependencies.
/// </summary>
/// <param name="ClassFullName">The fully qualified name of the class.</param>
/// <param name="Namespace">
///     The namespace of the class, which also declares the container of the constructor arguments, or
///     <see langword="null" /> for the global namespace.
/// </param>
/// <param name="ContainerName">
///     The name of the container of the constructor arguments, which depends only on the class and the types the user
///     declared, so that it stays stable when other classes under test are added.
/// </param>
/// <param name="Parameters">The parameters of the constructor that creates the class.</param>
/// <param name="HasCreateWithMocks">
///     Whether a <c>CreateWithMocks</c> call needs the extension and the container; a class that only a
///     <c>IMockDataSource</c> creates gets neither, so that the parameter types of test methods do not add members to the
///     domain types.
/// </param>
internal sealed record SutClass(
	string ClassFullName,
	string? Namespace,
	string ContainerName,
	EquatableArray<SutParameter> Parameters,
	bool HasCreateWithMocks);
