using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mockolate.Exceptions;

namespace Mockolate.Internals;

/// <summary>
///     Holds the <see cref="MockDataSourceFactory" /> of every assembly for <see cref="MockDataSource" />.
/// </summary>
/// <remarks>
///     Public only because the generated code calls it. The assemblies are weak keys, so that a collectible
///     <c>AssemblyLoadContext</c> can still be unloaded.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockDataSourceRegistry
{
	private static readonly ConditionalWeakTable<Assembly, MockDataSourceFactory> Factories = new();
	private static readonly ConditionalWeakTable<Assembly, MockDataSourceFactory> DeferredFactories = new();
	private static readonly MockolateLock Lock = new();
	private static readonly MockDataSourceFactory None = NoValue;

	/// <summary>
	///     Registers the <paramref name="factory" /> of the <paramref name="assembly" />; a second registration for the
	///     same assembly replaces the first.
	/// </summary>
	public static void Register(Assembly assembly, MockDataSourceFactory factory)
		=> Register(assembly, factory, None);

	/// <summary>
	///     Registers the <paramref name="factory" /> and the <paramref name="deferredFactory" />, which creates the
	///     delegates of <c>[Deferred]</c> parameters, of the <paramref name="assembly" />; a second registration for the
	///     same assembly replaces the first.
	/// </summary>
	public static void Register(Assembly assembly, MockDataSourceFactory factory, MockDataSourceFactory deferredFactory)
	{
		lock (Lock)
		{
			Factories.Remove(assembly);
			Factories.Add(assembly, factory);
			DeferredFactories.Remove(assembly);
			DeferredFactories.Add(assembly, deferredFactory);
		}
	}

	/// <summary>
	///     The argument that a <see cref="MockArgumentResolver" /> supplied for the constructor parameter
	///     <paramref name="parameterName" /> of the <paramref name="className" />, as a <typeparamref name="T" />.
	/// </summary>
	/// <exception cref="MockException">The <paramref name="value" /> is not a <typeparamref name="T" />.</exception>
	public static T Cast<T>(object? value, string parameterName, string className)
	{
		if (value is T typedValue)
		{
			return typedValue;
		}

		if (value is null && default(T) is null)
		{
			return default!;
		}

		throw new MockException(
			$"The argument for the constructor parameter '{parameterName}' of {className} is {(value is null ? "null" : $"of type {value.GetType()}")}, but must be of type {typeof(T)}.");
	}

	internal static bool TryGet(Assembly assembly, [NotNullWhen(true)] out MockDataSourceFactory? factory)
		=> Factories.TryGetValue(assembly, out factory);

	internal static MockDataSourceFactory GetDeferred(Assembly assembly)
		=> DeferredFactories.TryGetValue(assembly, out MockDataSourceFactory? factory) ? factory : None;

	/// <summary>
	///     The factory of the <paramref name="assembly" />, or one that creates nothing, which is remembered so that the
	///     module constructor of an assembly without generated code is not run again.
	/// </summary>
	internal static MockDataSourceFactory GetOrRegisterNone(Assembly assembly)
	{
		lock (Lock)
		{
			if (!Factories.TryGetValue(assembly, out MockDataSourceFactory? factory))
			{
				factory = None;
				Factories.Add(assembly, factory);
			}

			return factory;
		}
	}

	private static bool NoValue(Type type, MockBehavior? behavior, MockArgumentResolver resolve, out object? value)
	{
		value = null;
		return false;
	}
}
