using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators;

/// <summary>
///     The <see cref="IIncrementalGenerator" /> for the registration of mocks.
/// </summary>
[Generator]
public class MockGenerator : IIncrementalGenerator
{
	void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			"Mock.g.cs",
			ToSource(Sources.Sources.MockClass())));

		// Per-mock stream. Equality on MockClass is now keyed only on its canonical identity
		// (ClassFullName + AdditionalImplementations.ClassFullName), so Roslyn can correctly skip
		// per-mock outputs when the same mock identity is rediscovered across edits.
		IncrementalValuesProvider<MockClass> mocksProvider = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (s, _) => s.IsCreateMethodInvocation(),
				static (ctx, _) =>
				{
					// Set the per-compilation EntityCache as ambient state for this transform
					// invocation so Type / MethodParameter constructors deduplicate identical
					// ITypeSymbol references against an already-built record. Materialize the
					// IEnumerable inside the using scope; the cache scope unwinds on return.
					EntityCache cache = EntityCache.GetOrCreate(ctx.SemanticModel.Compilation);
					using EntityCache.Scope scope = EntityCache.EnterScope(cache);
					return ctx.Node
						.ExtractMockOrMockFactoryCreateSyntaxOrDefault(ctx.SemanticModel)
						.ToArray();
				})
			.SelectMany(static (mocks, _) => mocks);

		// Stable, deduplicated, sorted view across all mocks. Used by every cross-mock stage
		// (naming, aggregate setup files). Sorting by ClassFullName makes the array's content
		// deterministic so downstream Select stages cache cleanly.
		IncrementalValueProvider<EquatableArray<MockClass>> collectedMocks = mocksProvider
			.Collect()
			.Select(static (mocks, _) => Distinct(mocks));

		IncrementalValueProvider<bool> hasOverloadResolutionPriority = context.CompilationProvider
			.Select(static (compilation, _) => HasAttribute(compilation,
				"System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute"));

		// Union-typed setup/verify arguments need C# 15 union conversions in the consuming compilation.
		// Reduced to a bool so the union-dependent outputs only re-run when the answer flips.
		IncrementalValueProvider<bool> hasUnionSupport = context.ParseOptionsProvider
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (source, _) => HasUnionSupport(source.Left, source.Right));

		// The union-mode surface needs three compiler-recognised attributes that older frameworks lack
		// (UnionAttribute: .NET 11, OverloadResolutionPriorityAttribute: .NET 9, CallerArgumentExpressionAttribute:
		// .NET 6). Whatever the referenced framework or the consuming assembly does not declare is polyfilled next
		// to the generated union type, so that union mode works on every target a C# 15 compiler can build. The
		// compilation cannot show the output of other generators (PolySharp), so MockolateUnionAttributePolyfills
		// names the attributes the project already gets elsewhere: "false" stands for the two that PolySharp ships.
		IncrementalValueProvider<(bool Union, bool Priority, bool CallerArgumentExpression)> hasUnionAttributes =
			context.CompilationProvider
				.Combine(context.AnalyzerConfigOptionsProvider)
				.Select(static (source, _) =>
				{
					HashSet<string> provided = ProvidedUnionAttributes(source.Right);
					return (
						provided.Contains("UnionAttribute") ||
						HasAttribute(source.Left, "System.Runtime.CompilerServices.UnionAttribute"),
						provided.Contains("OverloadResolutionPriorityAttribute") ||
						HasAttribute(source.Left, "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute"),
						provided.Contains("CallerArgumentExpressionAttribute") ||
						HasAttribute(source.Left, "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute"));
				});

		context.RegisterSourceOutput(hasUnionSupport.Combine(hasUnionAttributes), static (spc, source) =>
		{
			if (source.Left)
			{
				spc.AddSource("ParameterArg.g.cs",
					ToSource(Sources.Sources.ParameterArg(
						emitUnionAttributePolyfill: !source.Right.Union,
						emitOverloadResolutionPriorityPolyfill: !source.Right.Priority,
						emitCallerArgumentExpressionPolyfill: !source.Right.CallerArgumentExpression)));
			}
		});

		// In union mode ParameterArg.g.cs guarantees OverloadResolutionPriorityAttribute, so every overload set of
		// the compilation (union-typed or classic) can carry priorities; without union mode nothing changes.
		IncrementalValueProvider<bool> canUseOverloadResolutionPriority = hasOverloadResolutionPriority
			.Combine(hasUnionSupport)
			.Select(static (source, _) => source.Left || source.Right);

		// Naming step: cross-mock disambiguation. Cached as a unit; one NamedMock per emission.
		IncrementalValueProvider<EquatableArray<NamedMock>> namedMocksAggregate = collectedMocks
			.Select(static (arr, _) => CreateNamedMocks(arr));

		IncrementalValuesProvider<NamedMock> perMock = namedMocksAggregate
			.SelectMany(static (arr, _) => arr);

		// Per-mock source output: cache hit when the NamedMock is identity-equal to last run.
		context.RegisterSourceOutput(
			perMock.Combine(canUseOverloadResolutionPriority).Combine(hasUnionSupport),
			static (spc, source) => EmitMockFile(spc, source.Left.Left, source.Left.Right, source.Right));

		// As<T> bridge extensions across (a, b) pairs. Cross-mock dedup; one file.
		IncrementalValueProvider<EquatableArray<MockAsExtensionPair>> asPairs = namedMocksAggregate
			.Select(static (arr, _) => CollectAsExtensionPairs(arr));

		context.RegisterSourceOutput(asPairs, static (spc, pairs) =>
		{
			if (pairs.Count > 0)
			{
				spc.AddSource("Mock.AsExtensions.g.cs",
					ToSource(Sources.Sources.MockAsExtensions(pairs)));
			}
		});

		// Aggregate inputs derived from per-mock projections — each aggregate caches independently.
		IncrementalValueProvider<EquatableArray<IndexerSetupKey>> indexerSetupKeys = collectedMocks
			.Select(static (arr, _) => CollectIndexerSetupKeys(arr));

		context.RegisterSourceOutput(
			indexerSetupKeys.Combine(canUseOverloadResolutionPriority),
			static (spc, source) =>
			{
				if (source.Left.Count == 0)
				{
					return;
				}

				spc.AddSource("IndexerSetups.g.cs",
					ToSource(Sources.Sources.IndexerSetups(source.Left, source.Right)));
			});

		IncrementalValueProvider<EquatableArray<MethodSetupKey>> methodSetupKeys = collectedMocks
			.Select(static (arr, _) => CollectMethodSetupKeys(arr));

		context.RegisterSourceOutput(methodSetupKeys, static (spc, keys) =>
		{
			if (keys.Count == 0)
			{
				return;
			}

			HashSet<(int, bool)> set = ToMethodSetupHashSet(keys);
			spc.AddSource("MethodSetups.g.cs", ToSource(Sources.Sources.MethodSetups(set)));

			const int dotNetFuncActionParameterLimit = 16;
			if (set.Any(x => x.Item1 >= dotNetFuncActionParameterLimit))
			{
				spc.AddSource("ActionFunc.g.cs",
					ToSource(Sources.Sources.ActionFunc(set
						.Where(x => x.Item1 >= dotNetFuncActionParameterLimit)
						.SelectMany<(int, bool), int>(x => [x.Item1, x.Item1 + 1,])
						.Where(x => x > dotNetFuncActionParameterLimit)
						.Distinct())));
			}

			if (set.Any(x => !x.Item2))
			{
				spc.AddSource("ReturnsThrowsAsyncExtensions.g.cs",
					ToSource(Sources.Sources.ReturnsThrowsAsyncExtensions(set
						.Where(x => !x.Item2).Select(x => x.Item1).ToArray())));
			}
		});

		IncrementalValueProvider<RefStructAggregate> refStructAggregate = collectedMocks
			.Select(static (arr, _) => CollectRefStructAggregate(arr));

		context.RegisterSourceOutput(refStructAggregate, static (spc, agg) =>
		{
			if (agg.MethodSetups.Count == 0 && agg.IndexerSetups.Count == 0)
			{
				return;
			}

			HashSet<(int, bool)> methodSet = ToMethodSetupHashSet(agg.MethodSetups);
			Dictionary<int, (bool HasGetter, bool HasSetter)> indexerArities = new();
			foreach (RefStructIndexerSetup item in agg.IndexerSetups)
			{
				indexerArities[item.Arity] = (item.HasGetter, item.HasSetter);
			}

			spc.AddSource("RefStructMethodSetups.g.cs",
				ToSource(Sources.Sources.RefStructMethodSetups(methodSet, indexerArities)));
		});

		// MockBehaviorExtensions: only depends on whether HttpClient appears as a mock target.
		// Reduce to a bool so the aggregate cache holds across mock-set churn that doesn't toggle it.
		IncrementalValueProvider<bool> includeHttpClient = collectedMocks
			.Select(static (arr, _) => arr.AsArray().Any(m => m.ClassFullName == "global::System.Net.Http.HttpClient"));

		context.RegisterSourceOutput(includeHttpClient, static (spc, hasHttp) =>
			spc.AddSource("MockBehaviorExtensions.g.cs",
				ToSource(Sources.Sources.MockBehaviorExtensions(hasHttp))));

		static bool HasAttribute(Compilation c, string attributeName)
		{
			INamedTypeSymbol? attributeSymbol = c.GetTypeByMetadataName(attributeName);
			return attributeSymbol != null &&
			       (attributeSymbol.DeclaredAccessibility == Accessibility.Public ||
			        (attributeSymbol.DeclaredAccessibility == Accessibility.Internal &&
			         SymbolEqualityComparer.Default.Equals(attributeSymbol.ContainingAssembly, c.Assembly)));
		}

		// MockolateUnionAttributePolyfills: "false" means the project gets OverloadResolutionPriorityAttribute and
		// CallerArgumentExpressionAttribute from another generator (PolySharp); any other value is a ';'-separated
		// list of attribute names (with or without namespace) that must not be polyfilled.
		static HashSet<string> ProvidedUnionAttributes(AnalyzerConfigOptionsProvider analyzerConfigOptions)
		{
			HashSet<string> provided = new(StringComparer.OrdinalIgnoreCase);
			if (!analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MockolateUnionAttributePolyfills",
				    out string? configured) ||
			    string.IsNullOrWhiteSpace(configured))
			{
				return provided;
			}

			if (string.Equals(configured.Trim(), "false", StringComparison.OrdinalIgnoreCase))
			{
				provided.Add("OverloadResolutionPriorityAttribute");
				provided.Add("CallerArgumentExpressionAttribute");
				return provided;
			}

			foreach (string name in configured.Split(';'))
			{
				string trimmed = name.Trim();
				int lastDot = trimmed.LastIndexOf('.');
				if (trimmed.Length > 0 && !string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
				{
					provided.Add(lastDot < 0 ? trimmed : trimmed.Substring(lastDot + 1));
				}
			}

			return provided;
		}

		// The MockolateUnionParameters build property (made compiler-visible by build/Mockolate.props) wins when
		// set: "true" opts in on a preview compiler, any other value is the kill switch. Otherwise unions are used once the
		// host compiler has shipped C# 15 (the generator is compiled against an older Roslyn and cannot name
		// LanguageVersion.CSharp15, hence the numeric check) and the compilation's effective language version
		// includes it. LanguageVersion.Preview passes the numeric test, but only counts once the compiler is capable.
		static bool HasUnionSupport(ParseOptions parseOptions, AnalyzerConfigOptionsProvider analyzerConfigOptions)
		{
			if (analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MockolateUnionParameters",
				    out string? configured) &&
			    !string.IsNullOrWhiteSpace(configured))
			{
				return string.Equals(configured.Trim(), "true", StringComparison.OrdinalIgnoreCase);
			}

			const int csharp15 = 1500;
			return parseOptions is CSharpParseOptions csharpParseOptions &&
			       Enum.IsDefined(typeof(LanguageVersion), csharp15) &&
			       (int)csharpParseOptions.LanguageVersion >= csharp15;
		}
	}

	private static SourceText ToSource(string source)
		=> SourceText.From(Sources.Sources.ExpandCrefs(source), Encoding.UTF8);

	private static EquatableArray<MockClass> Distinct(ImmutableArray<MockClass> mocks)
	{
		if (mocks.IsDefaultOrEmpty)
		{
			return new EquatableArray<MockClass>([]);
		}

		HashSet<MockClass> seen = new();
		List<MockClass> distinct = new(mocks.Length);
		foreach (MockClass mc in mocks)
		{
			if (seen.Add(mc))
			{
				distinct.Add(mc);
			}
		}

		distinct.Sort(static (a, b) =>
		{
			int cmp = StringComparer.Ordinal.Compare(a.ClassFullName, b.ClassFullName);
			if (cmp != 0)
			{
				return cmp;
			}

			Class[] aAdds = a.AdditionalImplementations.AsArray();
			Class[] bAdds = b.AdditionalImplementations.AsArray();
			cmp = aAdds.Length.CompareTo(bAdds.Length);
			if (cmp != 0)
			{
				return cmp;
			}

			for (int i = 0; i < aAdds.Length; i++)
			{
				cmp = StringComparer.Ordinal.Compare(aAdds[i].ClassFullName, bAdds[i].ClassFullName);
				if (cmp != 0)
				{
					return cmp;
				}
			}

			return 0;
		});

		return new EquatableArray<MockClass>(distinct.ToArray());
	}

	private static HashSet<(int, bool)> ToMethodSetupHashSet(EquatableArray<MethodSetupKey> keys)
	{
		MethodSetupKey[] arr = keys.AsArray();
		HashSet<(int, bool)> set = new();
		foreach (MethodSetupKey item in arr)
		{
			set.Add((item.Arity, item.IsVoid));
		}

		return set;
	}

	private static EquatableArray<IndexerSetupKey> CollectIndexerSetupKeys(EquatableArray<MockClass> mocks)
	{
		MockClass[] arr = mocks.AsArray();
		Dictionary<int, (bool NeedsGetterOnly, bool NeedsSetterOnly)> map = new();
		foreach (MockClass mc in arr)
		{
			foreach (Property property in mc.AllProperties())
			{
				if (property.IndexerParameters?.Count > 4)
				{
					int arity = property.IndexerParameters.Value.Count;
					bool needsGetterOnly = property is { Getter: not null, Setter: null, };
					bool needsSetterOnly = property is { Getter: null, Setter: not null, };
					map.TryGetValue(arity, out (bool NeedsGetterOnly, bool NeedsSetterOnly) existing);
					map[arity] = (existing.NeedsGetterOnly || needsGetterOnly,
						existing.NeedsSetterOnly || needsSetterOnly);
				}
			}
		}

		IndexerSetupKey[] sorted = map
			.OrderBy(kvp => kvp.Key)
			.Select(kvp => new IndexerSetupKey(kvp.Key, kvp.Value.NeedsGetterOnly, kvp.Value.NeedsSetterOnly))
			.ToArray();
		return new EquatableArray<IndexerSetupKey>(sorted);
	}

	private static EquatableArray<MethodSetupKey> CollectMethodSetupKeys(EquatableArray<MockClass> mocks)
	{
		MockClass[] arr = mocks.AsArray();
		HashSet<MethodSetupKey> set = new();
		foreach (MockClass mc in arr)
		{
			foreach (Method m in mc.AllMethods())
			{
				if (m.Parameters.Count > 4)
				{
					set.Add(new MethodSetupKey(m.Parameters.Count, m.ReturnType == Type.Void));
				}
			}
		}

		MethodSetupKey[] sorted = set.ToArray();
		Array.Sort(sorted, static (a, b) =>
		{
			int cmp = a.Arity.CompareTo(b.Arity);
			if (cmp != 0)
			{
				return cmp;
			}

			return a.IsVoid.CompareTo(b.IsVoid);
		});
		return new EquatableArray<MethodSetupKey>(sorted);
	}

	private static RefStructAggregate CollectRefStructAggregate(EquatableArray<MockClass> mocks)
	{
		MockClass[] arr = mocks.AsArray();
		HashSet<MethodSetupKey> methods = new();
		Dictionary<int, (bool HasGetter, bool HasSetter)> indexerMap = new();
		foreach (MockClass mc in arr)
		{
			foreach (Method m in mc.AllMethods())
			{
				if (m.Parameters.Count > 4 && m.Parameters.Any(p => p.NeedsRefStructPipeline()))
				{
					methods.Add(new MethodSetupKey(m.Parameters.Count, m.ReturnType == Type.Void));
				}
			}

			foreach (Property indexer in mc.AllProperties())
			{
				if (indexer is { IsIndexer: true, IndexerParameters: not null, } &&
				    indexer.IndexerParameters.Value.Count > 4 &&
				    indexer.IndexerParameters.Value.Any(kp => kp.NeedsRefStructPipeline()))
				{
					int arity = indexer.IndexerParameters.Value.Count;
					bool hasGetter = indexer.Getter is not null;
					bool hasSetter = indexer.Setter is not null;
					if (indexerMap.TryGetValue(arity, out (bool, bool) existing))
					{
						indexerMap[arity] = (existing.Item1 || hasGetter, existing.Item2 || hasSetter);
					}
					else
					{
						indexerMap[arity] = (hasGetter, hasSetter);
					}
				}
			}
		}

		MethodSetupKey[] methodArr = methods.ToArray();
		Array.Sort(methodArr, static (a, b) =>
		{
			int cmp = a.Arity.CompareTo(b.Arity);
			if (cmp != 0)
			{
				return cmp;
			}

			return a.IsVoid.CompareTo(b.IsVoid);
		});

		RefStructIndexerSetup[] indexerArr = indexerMap
			.OrderBy(kvp => kvp.Key)
			.Select(kvp => new RefStructIndexerSetup(kvp.Key, kvp.Value.HasGetter, kvp.Value.HasSetter))
			.ToArray();

		return new RefStructAggregate(
			new EquatableArray<MethodSetupKey>(methodArr),
			new EquatableArray<RefStructIndexerSetup>(indexerArr));
	}

	private static EquatableArray<NamedMock> CreateNamedMocks(EquatableArray<MockClass> mocks)
	{
		MockClass[] arr = mocks.AsArray();
		if (arr.Length == 0)
		{
			return new EquatableArray<NamedMock>([]);
		}

		HashSet<string> classNames = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> baseClassNames = new(StringComparer.Ordinal);
		List<NamedMock> result = new(arr.Length);
		HashSet<string> seenBaseClasses = new(StringComparer.Ordinal);

		Dictionary<string, MockClass> primaryMocks = new(StringComparer.Ordinal);
		foreach (MockClass mc in arr)
		{
			if (IsValidMockDeclaration(mc))
			{
				primaryMocks[mc.ClassFullName] = mc;
			}
		}

		// Pass 1a: assign disambiguated names to every distinct base/additional/hidden-base class. The
		// order here must be deterministic so the same input set always yields the same names.
		List<Class> orderedClasses = new();
		foreach (MockClass mc in arr)
		{
			if (!IsValidMockDeclaration(mc))
			{
				continue;
			}

			foreach (Class @class in mc.AllImplementations())
			{
				if (!seenBaseClasses.Add(@class.ClassFullName))
				{
					continue;
				}

				string baseName = @class.GetClassNameWithoutDots();
				int suffix = 1;
				string actualName = baseName;
				while (!classNames.Add(actualName))
				{
					actualName = $"{baseName}_{suffix++}";
				}

				baseClassNames[@class.ClassFullName] = actualName;
				orderedClasses.Add(@class);
			}
		}

		// Pass 1b: create one standalone NamedMock per distinct class. Names are now fully assigned, so
		// hidden-base interfaces can be resolved to their disambiguated names.
		foreach (Class @class in orderedClasses)
		{
			string actualName = baseClassNames[@class.ClassFullName];
			EquatableArray<NamedClass>? hiddenBases = null;
			if (primaryMocks.TryGetValue(@class.ClassFullName, out MockClass? primaryMock) &&
			    primaryMock.HiddenBaseInterfaces.Count > 0)
			{
				hiddenBases = new EquatableArray<NamedClass>(primaryMock.HiddenBaseInterfaces
					.Select(hiddenBase => new NamedClass(baseClassNames[hiddenBase.ClassFullName], hiddenBase))
					.ToArray());
			}

			result.Add(new NamedMock(actualName, actualName, @class, null, hiddenBases));
		}

		// Pass 2: combination mocks (additional implementations).
		foreach (MockClass mc in arr)
		{
			if (!IsValidMockDeclaration(mc) || mc.AdditionalImplementations.Count == 0)
			{
				continue;
			}

			string parentBaseName = mc.GetClassNameWithoutDots();
			string combinedName = parentBaseName + "__" +
			                      string.Join("__", mc.AdditionalImplementations.Select(t => t.GetClassNameWithoutDots()));
			int suffix = 1;
			string actualName = combinedName;
			while (!classNames.Add(actualName))
			{
				actualName = $"{combinedName}_{suffix++}";
			}

			NamedClass[] additionalNamed = mc.AdditionalImplementations
				.Select(additional => new NamedClass(baseClassNames[additional.ClassFullName], additional))
				.ToArray();

			result.Add(new NamedMock(actualName, baseClassNames[mc.ClassFullName], mc, new EquatableArray<NamedClass>(additionalNamed)));
		}

		return new EquatableArray<NamedMock>(result.ToArray());
	}

	private static EquatableArray<MockAsExtensionPair> CollectAsExtensionPairs(EquatableArray<NamedMock> mocks)
	{
		NamedMock[] arr = mocks.AsArray();
		HashSet<MockAsExtensionPair> seen = new();
		List<MockAsExtensionPair> ordered = new();
		foreach (NamedMock nm in arr)
		{
			if (nm.HiddenBases is { } hiddenBases)
			{
				foreach (NamedClass hiddenBase in hiddenBases)
				{
					AddIfNew(seen, ordered, MockAsExtensionPair.Create(
						nm.ParentName, nm.Mock.ClassFullName, hiddenBase.Name, hiddenBase.Class.ClassFullName));
				}
			}

			if (nm.AdditionalClasses is not { } additional || additional.Count == 0)
			{
				continue;
			}

			NamedClass[] additionalArr = additional.AsArray();
			NamedClass last = additionalArr[additionalArr.Length - 1];

			AddIfNew(seen, ordered, MockAsExtensionPair.Create(nm.ParentName, nm.Mock.ClassFullName, last.Name, last.Class.ClassFullName));
			for (int i = 0; i < additionalArr.Length - 1; i++)
			{
				AddIfNew(seen, ordered, MockAsExtensionPair.Create(additionalArr[i].Name, additionalArr[i].Class.ClassFullName, last.Name, last.Class.ClassFullName));
			}
		}

		MockAsExtensionPair[] sorted = ordered.ToArray();
		Array.Sort(sorted, static (a, b) =>
		{
			int cmp = StringComparer.Ordinal.Compare(a.SourceName, b.SourceName);
			if (cmp != 0)
			{
				return cmp;
			}

			return StringComparer.Ordinal.Compare(a.OtherName, b.OtherName);
		});
		return new EquatableArray<MockAsExtensionPair>(sorted);

		static void AddIfNew(HashSet<MockAsExtensionPair> set, List<MockAsExtensionPair> list, MockAsExtensionPair pair)
		{
			if (set.Add(pair))
			{
				list.Add(pair);
			}
		}
	}

	private static void EmitMockFile(SourceProductionContext context, NamedMock named, bool hasOverloadResolutionPriority,
		bool hasUnionSupport)
	{
		string fileName = named.FileName;
		Class @class = named.Mock;
		// hasOverloadResolutionPriority already accounts for the polyfill that ParameterArg.g.cs emits in union mode.
		bool useUnionOverloads = hasUnionSupport;

		if (@class is MockClass { Delegate: not null, } mockClass)
		{
			context.AddSource($"Mock.{fileName}.g.cs",
				ToSource(Sources.Sources.MockDelegate(named.ParentName, mockClass, mockClass.Delegate, useUnionOverloads)));
			return;
		}

		if (named.AdditionalClasses is not { } additional || additional.Count == 0)
		{
			(string Name, Class Class)[] hiddenBaseArr = [];
			if (named.HiddenBases is { } hiddenBases && hiddenBases.Count > 0)
			{
				NamedClass[] hiddenNamed = hiddenBases.AsArray();
				hiddenBaseArr = new (string Name, Class Class)[hiddenNamed.Length];
				for (int i = 0; i < hiddenNamed.Length; i++)
				{
					hiddenBaseArr[i] = (hiddenNamed[i].Name, hiddenNamed[i].Class);
				}
			}

			context.AddSource($"Mock.{fileName}.g.cs",
				ToSource(Sources.Sources.MockClass(named.ParentName, @class, hasOverloadResolutionPriority, hiddenBaseArr,
					useUnionOverloads)));
			return;
		}

		NamedClass[] additionalNamed = additional.AsArray();
		(string Name, Class Class)[] additionalArr = new (string Name, Class Class)[additionalNamed.Length];
		for (int i = 0; i < additionalNamed.Length; i++)
		{
			additionalArr[i] = (additionalNamed[i].Name, additionalNamed[i].Class);
		}

		context.AddSource($"Mock.{fileName}.g.cs",
			ToSource(Sources.Sources.MockCombinationClass(fileName, named.ParentName, @class, additionalArr,
				useUnionOverloads)));
	}

	private static bool IsValidMockDeclaration(MockClass mockClass)
		=> (mockClass.IsInterface || mockClass.Constructors is { Count: > 0, }) &&
		   mockClass.AdditionalImplementations.All(x => x.IsInterface) &&
		   mockClass.AllImplementations().All(x => !x.HasInaccessibleRequiredMember);
}

internal readonly record struct MethodSetupKey(int Arity, bool IsVoid);

internal readonly record struct RefStructIndexerSetup(int Arity, bool HasGetter, bool HasSetter);

internal readonly record struct IndexerSetupKey(int Arity, bool NeedsGetterOnly, bool NeedsSetterOnly);

internal readonly record struct MockAsExtensionPair(
	string SourceName,
	string SourceFullName,
	string OtherName,
	string OtherFullName)
{
	public static MockAsExtensionPair Create(string nameA, string fullNameA, string nameB, string fullNameB)
		=> string.CompareOrdinal(nameA, nameB) <= 0
			? new MockAsExtensionPair(nameA, fullNameA, nameB, fullNameB)
			: new MockAsExtensionPair(nameB, fullNameB, nameA, fullNameA);
}

internal sealed class RefStructAggregate : IEquatable<RefStructAggregate>
{
	public RefStructAggregate(
		EquatableArray<MethodSetupKey> methodSetups,
		EquatableArray<RefStructIndexerSetup> indexerSetups)
	{
		MethodSetups = methodSetups;
		IndexerSetups = indexerSetups;
	}

	public EquatableArray<MethodSetupKey> MethodSetups { get; }
	public EquatableArray<RefStructIndexerSetup> IndexerSetups { get; }

	public bool Equals(RefStructAggregate? other)
		=> other is not null &&
		   MethodSetups.Equals(other.MethodSetups) &&
		   IndexerSetups.Equals(other.IndexerSetups);

	public override bool Equals(object? obj) => Equals(obj as RefStructAggregate);

	public override int GetHashCode() => unchecked((MethodSetups.GetHashCode() * 17) + IndexerSetups.GetHashCode());
}

internal readonly record struct NamedClass(string Name, Class Class);

internal sealed class NamedMock : IEquatable<NamedMock>
{
	public NamedMock(string fileName, string parentName, Class mock, EquatableArray<NamedClass>? additionalClasses,
		EquatableArray<NamedClass>? hiddenBases = null)
	{
		FileName = fileName;
		ParentName = parentName;
		Mock = mock;
		AdditionalClasses = additionalClasses;
		HiddenBases = hiddenBases;
	}

	public string FileName { get; }
	public string ParentName { get; }
	public Class Mock { get; }
	public EquatableArray<NamedClass>? AdditionalClasses { get; }
	public EquatableArray<NamedClass>? HiddenBases { get; }

	public bool Equals(NamedMock? other)
	{
		if (other is null)
		{
			return false;
		}

		if (FileName != other.FileName || ParentName != other.ParentName)
		{
			return false;
		}

		if (!Mock.Equals(other.Mock))
		{
			return false;
		}

		if (!NullableEquals(HiddenBases, other.HiddenBases))
		{
			return false;
		}

		if (AdditionalClasses is null)
		{
			return other.AdditionalClasses is null;
		}

		if (other.AdditionalClasses is null)
		{
			return false;
		}

		return AdditionalClasses.Value.Equals(other.AdditionalClasses.Value);

		static bool NullableEquals(EquatableArray<NamedClass>? a, EquatableArray<NamedClass>? b)
		{
			return a is null ? b is null : b is not null && a.Value.Equals(b.Value);
		}
	}

	public override bool Equals(object? obj) => Equals(obj as NamedMock);

	public override int GetHashCode()
	{
		int hash = FileName.GetHashCode();
		hash = unchecked((hash * 17) + ParentName.GetHashCode());
		hash = unchecked((hash * 17) + Mock.GetHashCode());
		if (AdditionalClasses is { } additional)
		{
			hash = unchecked((hash * 17) + additional.GetHashCode());
		}

		if (HiddenBases is { } hiddenBases)
		{
			hash = unchecked((hash * 17) + hiddenBases.GetHashCode());
		}

		return hash;
	}
}
