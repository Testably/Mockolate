using System;

namespace Mockolate;

/// <summary>
///     Supplies the value for the constructor parameter <paramref name="parameterName" /> of type
///     <paramref name="type" />, or <see langword="null" /> to let Mockolate create a mock for it.
/// </summary>
public delegate object? DependencyResolver(Type type, string parameterName);
