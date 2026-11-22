using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mockolate.Exceptions;
using Mockolate.Internals;

namespace Mockolate;

/// <summary>
///     Creates mocks and classes with mocked dependencies for test framework integrations, without reflection.
/// </summary>
/// <remarks>
///     Every assembly in which the Mockolate source generator discovered a member with an attribute that implements
///     <see cref="IMockDataSource" /> registers the code to create its parameter types from a module initializer.
/// </remarks>
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
	///     from the default values of the <paramref name="behavior" /> (see <see cref="TryCreateValue{T}" />).
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

	/// <summary>
	///     Creates a value of <typeparamref name="T" /> from the default values of the <paramref name="behavior" />, for a
	///     value that Mockolate can neither mock nor construct, such as a <see cref="string" /> or an <see cref="int" />.
	/// </summary>
	/// <typeparam name="T">The type to create.</typeparam>
	/// <param name="behavior">The behavior whose <see cref="MockBehavior.DefaultValue" /> creates the value.</param>
	/// <param name="value">The created value.</param>
	/// <returns>
	///     <see langword="true" /> when the default values of the <paramref name="behavior" /> provide a value of
	///     <typeparamref name="T" />, or when <typeparamref name="T" /> is a value type, whose default value is valid.
	/// </returns>
	/// <exception cref="ArgumentNullException">The <paramref name="behavior" /> is <see langword="null" />.</exception>
	public static bool TryCreateValue<T>(MockBehavior behavior, out T value)
	{
		if (behavior is null)
		{
			throw new ArgumentNullException(nameof(behavior));
		}

		if (behavior.DefaultValue.GenerateValue(typeof(T)) is T generated)
		{
			value = generated;
			return true;
		}

		value = default!;
		return typeof(T).IsValueType;
	}
}
