using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     Per-compilation cache for symbol-keyed entity records. Roslyn returns canonical
///     <see cref="ITypeSymbol" /> instances for the same type within one compilation, so
///     deduplicating on symbol identity is sound and lets a generator build that touches the
///     same shared types (<c>string</c>, <c>int</c>, <see cref="System.Threading.Tasks.Task" />,
///     etc.) hundreds of times allocate the entity record only once.
/// </summary>
/// <remarks>
///     Caches MUST NOT cross compilation boundaries — the symbols won't survive. The
///     <see cref="ConditionalWeakTable{TKey,TValue}" /> guarantees the cache is collected when
///     the <see cref="Compilation" /> is. Cache lookup is exposed through
///     <see cref="EnterScope" /> so the existing entity constructors can stay parameter-stable.
/// </remarks>
internal sealed class EntityCache
{
	private static readonly ConditionalWeakTable<Compilation, EntityCache> _caches = new();

	private readonly ConcurrentDictionary<IParameterSymbol, MethodParameter> _parameters = new(SymbolEqualityComparer.IncludeNullability);

	// IncludeNullability is essential — Default would treat `string` and `string?` as the same
	// key, but the resulting Type/MethodParameter records carry different `CanBeNullable` /
	// `IsNullableAnnotated` flags and produce different generated code.
	private readonly ConcurrentDictionary<ITypeSymbol, Type> _types = new(SymbolEqualityComparer.IncludeNullability);

	private readonly Dictionary<IAssemblySymbol, string> _externAliases;

	private EntityCache(Compilation compilation)
	{
		_externAliases = GetExternAliases(compilation);
	}

	[field: ThreadStatic] public static EntityCache? Current { get; private set; }

	public static EntityCache GetOrCreate(Compilation compilation)
		=> _caches.GetValue(compilation, static c => new EntityCache(c));

	public static Scope EnterScope(EntityCache cache)
	{
		EntityCache? previous = Current;
		Current = cache;
		return new Scope(previous);
	}

	public Type GetOrAddType(ITypeSymbol symbol, Func<ITypeSymbol, Type> factory)
		=> _types.GetOrAdd(symbol, factory);

	public MethodParameter GetOrAddParameter(IParameterSymbol symbol, Func<IParameterSymbol, MethodParameter> factory)
		=> _parameters.GetOrAdd(symbol, factory);

	public bool HasExternAliases => _externAliases.Count > 0;

	/// <summary>
	///     Returns the extern alias through which types of <paramref name="assembly" /> must be qualified, or
	///     <see langword="null" /> when they are reachable through <c>global::</c>.
	/// </summary>
	public string? GetExternAlias(IAssemblySymbol assembly)
		=> _externAliases.TryGetValue(assembly, out string? alias) ? alias : null;

	private static Dictionary<IAssemblySymbol, string> GetExternAliases(Compilation compilation)
	{
		Dictionary<IAssemblySymbol, string> externAliases = new(SymbolEqualityComparer.Default);
		foreach (MetadataReference reference in compilation.References)
		{
			ImmutableArray<string> aliases = reference.Properties.Aliases;
			if (!aliases.IsEmpty && !aliases.Contains("global") &&
			    compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
			{
				externAliases[assembly] = aliases[0];
			}
		}

		if (externAliases.Count > 0)
		{
			foreach (MetadataReference reference in compilation.References)
			{
				ImmutableArray<string> aliases = reference.Properties.Aliases;
				if ((aliases.IsEmpty || aliases.Contains("global")) &&
				    compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					externAliases.Remove(assembly);
				}
			}
		}

		return externAliases;
	}

	public readonly struct Scope : IDisposable
	{
		private static void ExitScope(EntityCache? previous) => Current = previous;

		private readonly EntityCache? _previous;

		internal Scope(EntityCache? previous)
		{
			_previous = previous;
		}

		public void Dispose() => ExitScope(_previous);
	}
}
