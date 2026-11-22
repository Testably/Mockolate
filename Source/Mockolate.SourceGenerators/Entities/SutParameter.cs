namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     A constructor parameter of a <see cref="SutClass" />, which gets a new mock unless it is not
///     <paramref name="IsMockable" />.
/// </summary>
internal sealed record SutParameter(string TypeFullName, string Name, bool IsMockable);
