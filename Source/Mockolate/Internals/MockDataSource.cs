using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mockolate.Exceptions;

namespace Mockolate.Internals;

/// <summary>
///     Creates mocks and classes with mocked dependencies for test framework integrations, without reflection.
/// </summary>
/// <remarks>
///     Every assembly in which the Mockolate source generator discovered a member with an attribute that implements
///     <see cref="IMockDataSource" /> registers the code to create its parameter types from a module initializer.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public static class MockDataSource
{
	/// <summary>
	///     Creates a value of <paramref name="type" /> with the code generated into the <paramref name="assembly" />.
	/// </summary>
	/// <param name="assembly">The assembly that declares the member whose parameter is created.</param>
	/// <param name="type">The type to create.</param>
	/// <param name="behavior">
	///     The behavior of the created mocks and of the default values, or <see langword="null" /> for
	///     <c>MockBehavior.Default</c>.
	/// </param>
	/// <param name="resolve">Supplies the constructor arguments that must not be new mocks.</param>
	/// <param name="value">The created value.</param>
	/// <returns>
	///     <see langword="true" /> when the generated code of the <paramref name="assembly" /> can create a value of
	///     <paramref name="type" />: a mock, a class with mocked dependencies, or for the other types of parameters a value
	///     from the default values of the <paramref name="behavior" />, like an un-configured mock member returns.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	///     The <paramref name="assembly" />, the <paramref name="type" /> or the <paramref name="resolve" /> is
	///     <see langword="null" />.
	/// </exception>
	/// <exception cref="MockException">
	///     Neither the <paramref name="resolve" /> nor the default values of the <paramref name="behavior" /> supply an
	///     argument for a constructor parameter that is neither mockable, creatable, nullable nor optional, or the
	///     <paramref name="resolve" /> supplies an argument that does not have the type of the parameter.
	/// </exception>
	public static bool TryCreate(Assembly assembly, Type type, MockBehavior? behavior, MockArgumentResolver resolve,
		out object? value)
	{
		if (assembly is null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		if (type is null)
		{
			throw new ArgumentNullException(nameof(type));
		}

		if (resolve is null)
		{
			throw new ArgumentNullException(nameof(resolve));
		}

		if (!MockDataSourceRegistry.TryGet(assembly, out MockDataSourceFactory? factory))
		{
			// Test frameworks read the attributes from metadata, which does not run the module initializer.
			RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
			factory = MockDataSourceRegistry.GetOrRegisterNone(assembly);
		}

		return factory(type, behavior, resolve, out value);
	}
}
