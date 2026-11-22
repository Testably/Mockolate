using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mockolate.Internals;

namespace Mockolate;

/// <summary>
///     Creates mocks and classes with mocked dependencies for test framework integrations, without reflection.
/// </summary>
/// <remarks>
///     Every assembly in which the Mockolate source generator discovered a method with an attribute that implements
///     <see cref="IMockDataSource" />, or a <c>CreateWithMocks</c> call, registers the code to create those types from a
///     module initializer.
/// </remarks>
public static class MockDataSources
{
	/// <summary>
	///     Creates a value of <paramref name="type" /> with the code generated into the <paramref name="assembly" />.
	/// </summary>
	/// <param name="assembly">The assembly that declares the test method whose parameter is created.</param>
	/// <param name="type">The type to create.</param>
	/// <param name="behavior">
	///     The behavior of the created mocks, or <see langword="null" /> for the default behavior of the
	///     <paramref name="assembly" />.
	/// </param>
	/// <param name="resolve">Supplies the constructor arguments that must not be new mocks.</param>
	/// <param name="value">The created value.</param>
	/// <returns>
	///     <see langword="true" /> when the generated code of the <paramref name="assembly" /> can create a value of
	///     <paramref name="type" />.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	///     The <paramref name="resolve" /> supplies no argument for a constructor parameter that is neither mockable,
	///     nullable nor optional.
	/// </exception>
	public static bool TryCreate(Assembly assembly, Type type, MockBehavior? behavior, DependencyResolver resolve,
		out object? value)
	{
		if (!MockDataSourceRegistry.TryGet(assembly, out MockDataSourceFactory? factory))
		{
			// Test frameworks read the attributes from metadata, which does not run the module initializer.
			RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
			if (!MockDataSourceRegistry.TryGet(assembly, out factory))
			{
				value = null;
				return false;
			}
		}

		return factory(type, behavior, resolve, out value);
	}
}
