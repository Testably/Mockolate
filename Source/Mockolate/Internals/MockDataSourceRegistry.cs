using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mockolate.Internals;

/// <summary>
///     Holds the <see cref="MockDataSourceFactory" /> of every assembly for <see cref="MockDataSources" />.
/// </summary>
/// <remarks>
///     Public only because a module initializer of the generated code calls <see cref="Register" />.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockDataSourceRegistry
{
	private static readonly ConcurrentDictionary<Assembly, MockDataSourceFactory> Factories = new();

	/// <summary>
	///     Registers the <paramref name="factory" /> of the <paramref name="assembly" />; a second registration for the
	///     same assembly replaces the first.
	/// </summary>
	public static void Register(Assembly assembly, MockDataSourceFactory factory)
		=> Factories[assembly] = factory;

	internal static bool TryGet(Assembly assembly, [NotNullWhen(true)] out MockDataSourceFactory? factory)
		=> Factories.TryGetValue(assembly, out factory);
}
