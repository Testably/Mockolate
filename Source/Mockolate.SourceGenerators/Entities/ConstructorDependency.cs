namespace Mockolate.SourceGenerators.Entities;

/// <summary>
///     How <c>CreateMock()</c> fills a parameter of the constructor with dependencies.
/// </summary>
/// <param name="MockTypeFullName">The type whose new mock is the argument, or <see langword="null" /> when it is not mocked.</param>
/// <param name="IsNullable">Whether the parameter accepts <see langword="null" />.</param>
internal sealed record ConstructorDependency(string? MockTypeFullName, bool IsNullable);
