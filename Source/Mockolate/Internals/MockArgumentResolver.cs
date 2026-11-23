using System;

namespace Mockolate.Internals;

/// <summary>
///     Supplies the <paramref name="value" /> for the constructor parameter <paramref name="parameterName" /> of type
///     <paramref name="type" /> of a class that <see cref="MockDataSource" /> creates.
/// </summary>
/// <remarks>
///     It is asked for every constructor parameter, also of the classes that are created as constructor dependencies.
///     Without a value, Mockolate creates a mock for a mockable parameter, creates a supported class with mocked
///     dependencies, uses the default value for an optional or nullable parameter, and otherwise a value from the
///     default values of the behavior.
/// </remarks>
/// <returns>
///     <see langword="true" /> when the <paramref name="value" />, which may be <see langword="null" />, is the argument.
/// </returns>
public delegate bool MockArgumentResolver(Type type, string parameterName, out object? value);
