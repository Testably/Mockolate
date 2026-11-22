using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mockolate;

/// <summary>
///     The registry through which test framework integrations create mocks and classes with mocked dependencies
///     without reflection.
/// </summary>
/// <remarks>
///     Every assembly in which the Mockolate source generator discovered a <see cref="IMockDataSource" /> or a
///     <c>CreateWithMocks</c> call registers a <see cref="MockDataSourceFactory" /> from a module initializer.
/// </remarks>
public static class MockDataSources
{
	private static readonly ConcurrentDictionary<Assembly, MockDataSourceFactory> Factories = new();

	/// <summary>
	///     Registers the <paramref name="factory" /> of the <paramref name="assembly" />.
	/// </summary>
	/// <remarks>
	///     Called by generated code; a second registration for the same assembly replaces the first.
	/// </remarks>
	public static void Register(Assembly assembly, MockDataSourceFactory factory)
		=> Factories[assembly] = factory;

	/// <summary>
	///     Creates a value of <paramref name="type" /> with the code generated into the <paramref name="assembly" />.
	/// </summary>
	/// <param name="assembly">The assembly that declares the test method whose parameter is created.</param>
	/// <param name="type">The type to create.</param>
	/// <param name="behavior">
	///     The behavior of the created mocks, or <see langword="null" /> for the default behavior of the
	///     <paramref name="assembly" />.
	/// </param>
	/// <param name="resolve">Supplies the constructor dependencies that must not be new mocks.</param>
	/// <param name="value">The created value.</param>
	/// <returns>
	///     <see langword="true" /> when the generated code of the <paramref name="assembly" /> can create a value of
	///     <paramref name="type" />.
	/// </returns>
	public static bool TryCreate(Assembly assembly, Type type, MockBehavior? behavior, DependencyResolver resolve,
		out object? value)
	{
		if (!Factories.TryGetValue(assembly, out MockDataSourceFactory? factory))
		{
			// Test frameworks read the attributes from metadata, which does not run the module initializer.
			RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
			if (!Factories.TryGetValue(assembly, out factory))
			{
				value = null;
				return false;
			}
		}

		return factory(type, behavior, resolve, out value);
	}
}
