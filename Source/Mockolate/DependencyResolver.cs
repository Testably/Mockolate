using System;

namespace Mockolate;

/// <summary>
///     Supplies the argument for the constructor parameter <paramref name="parameterName" /> of type
///     <paramref name="type" />, or <see langword="null" /> when there is none: Mockolate then creates a mock for a
///     mockable parameter and uses the default value for an optional or nullable one.
/// </summary>
public delegate object? DependencyResolver(Type type, string parameterName);
