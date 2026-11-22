using System;
using System.ComponentModel;

namespace Mockolate.Internals;

/// <summary>
///     Creates a value of <paramref name="type" /> with the code the Mockolate source generator emitted into one
///     assembly.
/// </summary>
/// <remarks>
///     Public only because generated code registers it in the <see cref="MockDataSourceRegistry" />.
/// </remarks>
/// <param name="type">The type to create.</param>
/// <param name="behavior">
///     The behavior of the created mocks, or <see langword="null" /> for the default behavior of the assembly.
/// </param>
/// <param name="resolve">Supplies the constructor arguments that must not be new mocks.</param>
/// <param name="value">The created value.</param>
/// <returns><see langword="true" /> when the assembly can create a value of <paramref name="type" />.</returns>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate bool MockDataSourceFactory(Type type, MockBehavior? behavior, DependencyResolver resolve,
	out object? value);
