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
		context.RegisterPostInitializationOutput(ctx =>
		{
			ctx.AddSource("Mock.g.cs", ToSource(Sources.Sources.MockClass(), default));
			ctx.AddSource("AutoMockArguments.g.cs", ToSource(Sources.Sources.AutoMockArgumentsShared(), default));
			ctx.AddSource("AutoMockArguments.TUnit.g.cs", ToSource(Sources.Sources.AutoMockArgumentsForTUnit(), default));
			ctx.AddSource("AutoMockArguments.Xunit2.g.cs", ToSource(Sources.Sources.AutoMockArgumentsForXunit2(), default));
			ctx.AddSource("AutoMockArguments.Xunit3.g.cs", ToSource(Sources.Sources.AutoMockArgumentsForXunit3(), default));
		});

		// Per-mock stream. Equality on MockClass is now keyed only on its canonical identity
		// (ClassFullName + AdditionalImplementations.ClassFullName), so Roslyn can correctly skip
		// per-mock outputs when the same mock identity is rediscovered across edits.
		IncrementalValuesProvider<MockCreation> creations = context.SyntaxProvider
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
					return new MockCreation(
						new EquatableArray<MockClass>(ctx.Node
							.ExtractMockOrMockFactoryCreateSyntaxOrDefault(ctx.SemanticModel)
							.ToArray()),
						new CreateMockLocation(ctx.Node.SyntaxTree.FilePath, ctx.Node.Span.Start, ctx.Node.Span.Length));
				});
		IncrementalValuesProvider<MockClass> mocksProvider = creations
			.SelectMany(static (creation, _) => creation.Mocks);

		IncrementalValuesProvider<SealedCreation> sealedCreations = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (s, _) => s.IsSealedCreateMockInvocation(),
				static (ctx, cancellationToken) =>
				{
					EntityCache cache = EntityCache.GetOrCreate(ctx.SemanticModel.Compilation);
					using EntityCache.Scope scope = EntityCache.EnterScope(cache);
					return ctx.Node.ExtractSealedCreation(ctx.SemanticModel, cancellationToken);
				})
			.Where(static creation => creation.Class is not null);

		IncrementalValueProvider<EquatableArray<string>> dataSourceAttributeNames = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (s, _) => s.IsAttributeClassCandidate(),
				static (ctx, cancellationToken) => ctx.Node.GetDataSourceAttributeName(ctx.SemanticModel, cancellationToken))
			.Where(static name => name is not null)
			.Collect()
			.Combine(context.CompilationProvider
				.Select(static (compilation, _) => MockGeneratorHelpers.GetReferencedDataSourceAttributeNames(compilation)))
			.Combine(context.SyntaxProvider
				.CreateSyntaxProvider(
					static (s, _) => s.IsUsingAliasCandidate(),
					static (ctx, _) => ctx.Node.GetUsingAlias())
				.Collect())
			.Select(static (source, _) => MockGeneratorHelpers.GetAttributeUsageNames(
				source.Left.Left.Concat(source.Left.Right)!, source.Right));

		// The transform of a syntax provider runs for every matching node on every edit, and attributed members are
		// everywhere in a test project, so only the members with a matching attribute name are bound.
		IncrementalValuesProvider<DataSourceCreation> dataSourceCreations = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (s, _) => s.IsDataSourceMemberCandidate(),
				static (ctx, _) => ctx.Node.GetDataSourceMemberCandidate())
			.Combine(dataSourceAttributeNames)
			.Where(static source => source.Left.AttributeNames.AsArray().Any(name => source.Right.AsArray().Contains(name)))
			.Select(static (source, _) => source.Left.Location)
			.Combine(context.CompilationProvider)
			.SelectMany(static (source, cancellationToken) =>
			{
				EntityCache cache = EntityCache.GetOrCreate(source.Right);
				using EntityCache.Scope scope = EntityCache.EnterScope(cache);
				return MockGeneratorHelpers.ExtractDataSourceCreations(source.Left, source.Right, cancellationToken).ToArray();
			})
			.Where(static creation => !creation.IsEmpty);

		IncrementalValueProvider<AutoMockingOptions?> autoMockingOptions = context.AnalyzerConfigOptionsProvider
			.Select(static (options, _) =>
				options.GlobalOptions.TryGetValue("build_property.MockolateAutoMocking", out string? value) &&
				string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase)
					? new AutoMockingOptions(
						ReadNamespaces(options, "build_property.MockolateAutoMockingExcludedNamespaces"),
						ReadNamespaces(options, "build_property.MockolateAutoMockingIncludedNamespaces"))
					: null);

		IncrementalValueProvider<bool> autoMocking = autoMockingOptions
			.Select(static (options, _) => options is not null);

		// The excluded namespaces only affect the discovery, so changing them does not generate the mocks again.
		IncrementalValueProvider<AutoMockingOptions?> emissionOptions = autoMockingOptions
			.Select(static (options, _) => options is null
				? null
				: options with { ExcludedNamespaces = new EquatableArray<string>([]), });

		// Mocks for auto-mocking are only discovered with the MockolateAutoMocking build property, from the
		// CreateMock calls bound again in the current compilation, so that every other compilation does no work
		// for them and its output stays unchanged.
		IncrementalValueProvider<EquatableArray<MockClass>> discoveredMocks = creations
			.Collect()
			.Combine(dataSourceCreations
				.Select(static (creation, _) => new MockCreation(creation.Mocks, creation.Location))
				.Collect())
			.Combine(sealedCreations
				.Select(static (creation, _) => new MockCreation(creation.Mocks, creation.Location))
				.Collect())
			.Combine(autoMockingOptions)
			.Combine(context.CompilationProvider)
			.Select(static (source, cancellationToken) => source.Left.Right is { } options
				? DiscoverAutoMocks(source.Left.Left.Left.Left.AddRange(source.Left.Left.Left.Right)
					.AddRange(source.Left.Left.Right), source.Right, options, cancellationToken)
				: new EquatableArray<MockClass>([]));

		// Stable, deduplicated, sorted view across all mocks. Used by every cross-mock stage
		// (naming, aggregate setup files). Sorting by ClassFullName makes the array's content
		// deterministic so downstream Select stages cache cleanly.
		IncrementalValueProvider<EquatableArray<MockClass>> collectedMocks = mocksProvider
			.Collect()
			.Combine(dataSourceCreations.SelectMany(static (creation, _) => creation.Mocks).Collect())
			.Combine(sealedCreations.SelectMany(static (creation, _) => creation.Mocks).Collect())
			.Combine(discoveredMocks)
			.Select(static (source, _) => Distinct(source.Left.Left.Left,
				source.Left.Left.Right.Concat(source.Left.Right).Concat(source.Right).ToArray()));

		IncrementalValueProvider<EquatableArray<DataSourceClass>> dataSourceClasses = dataSourceCreations
			.SelectMany(static (creation, _) => creation.Classes)
			.Collect()
			.Select(static (source, _) => new EquatableArray<DataSourceClass>(source
				.GroupBy(dataSourceClass => dataSourceClass.ClassFullName, StringComparer.Ordinal)
				.Select(group => group.First())
				.OrderBy(dataSourceClass => dataSourceClass.ClassFullName, StringComparer.Ordinal)
				.ToArray()));

		IncrementalValueProvider<(EquatableArray<string> Mocked, EquatableArray<string> ValueOrMock, EquatableArray<string> Value)>
			dataSourceTypes = dataSourceCreations
				.Collect()
				.Select(static (source, _) => (
					SortedDistinct(source.SelectMany(creation => creation.MockedTypes)),
					SortedDistinct(source.SelectMany(creation => creation.ValueOrMockTypes)),
					SortedDistinct(source.SelectMany(creation => creation.ValueTypes))));

		// Like the union attributes, the attribute may come from another generator, which Mockolate cannot see: one that
		// MockolateUnionAttributePolyfills names, PolySharp or the generator of TUnit.
		IncrementalValueProvider<bool> hasModuleInitializer = context.CompilationProvider
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (source, _) =>
				ProvidedUnionAttributes(source.Right).Contains("ModuleInitializerAttribute") ||
				TUnitProvidesModuleInitializer(source.Left, source.Right) ||
				PolySharpProvidesModuleInitializer(source.Left, source.Right.GlobalOptions) ||
				HasAttribute(source.Left, "System.Runtime.CompilerServices.ModuleInitializerAttribute"));

		IncrementalValueProvider<bool> hasOverloadResolutionPriority = context.CompilationProvider
			.Select(static (compilation, _) => HasAttribute(compilation,
				"System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute"));

		IncrementalValueProvider<EquatableArray<string>> externAliases = context.CompilationProvider
			.Select(static (compilation, _) => GetExternAliases(compilation));

		context.RegisterSourceOutput(
			sealedCreations
				.Select(static (creation, _) => creation.Class!)
				.Collect()
				.Select(static (source, _) => new EquatableArray<DataSourceClass>(source
					.GroupBy(sealedClass => sealedClass.ClassFullName, StringComparer.Ordinal)
					.Select(group => group.First())
					.OrderBy(sealedClass => sealedClass.ClassFullName, StringComparer.Ordinal)
					.ToArray()))
				.Combine(externAliases),
			static (spc, source) =>
			{
				if (source.Left.Count > 0)
				{
					// Outside the Mock.<Name>.g.cs pattern of the mock files, so that no mocked type shares the hint name.
					spc.AddSource("SealedClasses.g.cs", ToSource(Sources.Sources.SealedClasses(source.Left), source.Right));
				}
			});

		context.RegisterSourceOutput(
			dataSourceClasses.Combine(dataSourceTypes).Combine(hasModuleInitializer).Combine(externAliases),
			static (spc, source) =>
			{
				((EquatableArray<DataSourceClass> classes,
						(EquatableArray<string> Mocked, EquatableArray<string> ValueOrMock, EquatableArray<string> Value) types),
					bool moduleInitializer) = source.Left;
				if (classes.Count > 0 || types.Mocked.Count > 0 || types.ValueOrMock.Count > 0 || types.Value.Count > 0)
				{
					spc.AddSource("MockDataSources.g.cs",
						ToSource(Sources.Sources.MockDataSources(classes, types.Mocked, types.ValueOrMock, types.Value,
							!moduleInitializer), source.Right));
				}
			});

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
		// names the attributes the project already gets elsewhere: "false" stands for the two of them that PolySharp ships
		// (OverloadResolutionPriorityAttribute and CallerArgumentExpressionAttribute), plus ModuleInitializerAttribute for the
		// mock data sources.
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
						emitCallerArgumentExpressionPolyfill: !source.Right.CallerArgumentExpression), default));
			}
		});

		// In union mode ParameterArg.g.cs guarantees OverloadResolutionPriorityAttribute, so every overload set of
		// the compilation (union-typed or classic) can carry priorities; without union mode nothing changes.
		IncrementalValueProvider<bool> canUseOverloadResolutionPriority = hasOverloadResolutionPriority
			.Combine(hasUnionSupport)
			.Select(static (source, _) => source.Left || source.Right);

		// Naming step: cross-mock disambiguation. Cached as a unit; one NamedMock per emission.
		IncrementalValueProvider<EquatableArray<NamedMock>> namedMocksAggregate = collectedMocks
			.Combine(autoMocking)
			.Select(static (source, _) => CreateNamedMocks(source.Left, source.Right));

		IncrementalValuesProvider<NamedMock> perMock = namedMocksAggregate
			.SelectMany(static (arr, _) => arr);

		// Per-mock source output: cache hit when the NamedMock is identity-equal to last run.
		context.RegisterSourceOutput(
			perMock.Combine(canUseOverloadResolutionPriority).Combine(hasUnionSupport).Combine(externAliases)
				.Combine(emissionOptions),
			static (spc, source) => EmitMockFile(spc, source.Left.Left.Left.Left, source.Left.Left.Left.Right,
				source.Left.Left.Right, source.Left.Right, source.Right));

		IncrementalValueProvider<(bool HasLogger, bool HasLoggerFactory)> loggingMocks = collectedMocks
			.Select(static (arr, _) => (
				arr.AsArray().Any(m => m.AllImplementations().Any(c => Sources.Sources.HasAdapter(c, MockAdapter.Logger))),
				arr.AsArray().Any(m => m.AllImplementations().Any(c => Sources.Sources.HasAdapter(c, MockAdapter.LoggerFactory)))));

		IncrementalValueProvider<LoggerMockNames> loggerMockNames = namedMocksAggregate
			.Select(static (arr, _) => CollectLoggerMockNames(arr));

		context.RegisterSourceOutput(loggingMocks.Combine(loggerMockNames).Combine(externAliases),
			static (spc, source) =>
			{
				(bool hasLogger, bool hasLoggerFactory) = source.Left.Left;
				if (hasLogger || hasLoggerFactory)
				{
					spc.AddSource("Logging.g.cs",
						ToSource(Sources.Sources.Logging(hasLoggerFactory, source.Left.Right), source.Right));
				}
			});

		// As<T> bridge extensions across (a, b) pairs. Cross-mock dedup; one file.
		IncrementalValueProvider<EquatableArray<MockAsExtensionPair>> asPairs = namedMocksAggregate
			.Select(static (arr, _) => CollectAsExtensionPairs(arr));

		context.RegisterSourceOutput(asPairs.Combine(externAliases), static (spc, source) =>
		{
			if (source.Left.Count > 0)
			{
				spc.AddSource("Mock.AsExtensions.g.cs",
					ToSource(Sources.Sources.MockAsExtensions(source.Left), source.Right));
			}
		});

		// Aggregate inputs derived from per-mock projections — each aggregate caches independently.
		IncrementalValueProvider<EquatableArray<IndexerSetupKey>> indexerSetupKeys = collectedMocks
			.Select(static (arr, _) => CollectIndexerSetupKeys(arr));

		context.RegisterSourceOutput(
			indexerSetupKeys.Combine(canUseOverloadResolutionPriority).Combine(externAliases).Combine(autoMocking),
			static (spc, source) =>
			{
				((EquatableArray<IndexerSetupKey> keys, bool priority), EquatableArray<string> aliases) = source.Left;
				if (keys.Count == 0)
				{
					return;
				}

				spc.AddSource("IndexerSetups.g.cs",
					ToSource(Sources.Sources.IndexerSetups(keys, priority, source.Right), aliases));
			});

		IncrementalValueProvider<EquatableArray<MethodSetupKey>> methodSetupKeys = collectedMocks
			.Select(static (arr, _) => CollectMethodSetupKeys(arr));

		context.RegisterSourceOutput(methodSetupKeys.Combine(externAliases), static (spc, source) =>
		{
			(EquatableArray<MethodSetupKey> keys, EquatableArray<string> aliases) = source;
			if (keys.Count == 0)
			{
				return;
			}

			HashSet<(int, bool)> set = ToMethodSetupHashSet(keys);
			spc.AddSource("MethodSetups.g.cs", ToSource(Sources.Sources.MethodSetups(set), aliases));

			const int dotNetFuncActionParameterLimit = 16;
			if (set.Any(x => x.Item1 >= dotNetFuncActionParameterLimit))
			{
				spc.AddSource("ActionFunc.g.cs",
					ToSource(Sources.Sources.ActionFunc(set
						.Where(x => x.Item1 >= dotNetFuncActionParameterLimit)
						.SelectMany<(int, bool), int>(x => [x.Item1, x.Item1 + 1,])
						.Where(x => x > dotNetFuncActionParameterLimit)
						.Distinct()), aliases));
			}

			if (set.Any(x => !x.Item2))
			{
				spc.AddSource("ReturnsThrowsAsyncExtensions.g.cs",
					ToSource(Sources.Sources.ReturnsThrowsAsyncExtensions(set
						.Where(x => !x.Item2).Select(x => x.Item1).ToArray()), aliases));
			}
		});

		IncrementalValueProvider<RefStructAggregate> refStructAggregate = collectedMocks
			.Select(static (arr, _) => CollectRefStructAggregate(arr));

		context.RegisterSourceOutput(refStructAggregate.Combine(externAliases), static (spc, source) =>
		{
			(RefStructAggregate agg, EquatableArray<string> aliases) = source;
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
				ToSource(Sources.Sources.RefStructMethodSetups(methodSet, indexerArities), aliases));
		});

		// MockBehaviorExtensions: depends on whether HttpClient appears as a mock target.
		// Reduce to a bool so the aggregate cache holds across mock-set churn that doesn't toggle it.
		IncrementalValueProvider<bool> includeHttpClient = collectedMocks
			.Select(static (arr, _) => arr.AsArray().Any(m => m.ClassFullName == "global::System.Net.Http.HttpClient"));

		IncrementalValueProvider<EquatableArray<string>?> autoMockableTypes = collectedMocks
			.Combine(emissionOptions)
			.Select(static (source, _) => source.Right is { } options
				? new EquatableArray<string>(source.Left.AsArray()
					.Where(m => options.CanReturnAutoMock(m) && IsValidMockDeclaration(m))
					.Select(m => m.ClassFullName)
					.ToArray())
				: (EquatableArray<string>?)null);

		// IAsyncEnumerable<T> needs Microsoft.Bcl.AsyncInterfaces on .NET Framework and .NET Standard 2.0.
		IncrementalValueProvider<bool> includeAsyncEnumerable = context.CompilationProvider
			.Select(static (compilation, _) => HasAttribute(compilation, "System.Collections.Generic.IAsyncEnumerable`1"));

		context.RegisterSourceOutput(includeHttpClient.Combine(includeAsyncEnumerable).Combine(autoMockableTypes).Combine(externAliases), static (spc, source) =>
			spc.AddSource("MockBehaviorExtensions.g.cs",
				ToSource(Sources.Sources.MockBehaviorExtensions(source.Left.Left.Left, source.Left.Left.Right, source.Left.Right), source.Right)));

		// Since 1.71, the source generator of TUnit declares ModuleInitializerAttribute when it is missing, unless the
		// source generation or the polyfills of TUnit are turned off or PolySharp declares the attribute. Mockolate
		// cannot see that output, so it mirrors these conditions (AttributePolyfillGenerator in TUnit). The props of
		// TUnit always make EnableTUnitSourceGeneration visible, so without it the generator of TUnit is not set
		// up. A project that only excludes the analyzer assets of TUnit sets EnableTUnitPolyfills to false instead.
		static bool TUnitProvidesModuleInitializer(Compilation compilation,
			AnalyzerConfigOptionsProvider analyzerConfigOptions)
		{
			AnalyzerConfigOptions options = analyzerConfigOptions.GlobalOptions;
			if (!compilation.ReferencedAssemblyNames.Any(assembly =>
				    assembly.Name == "TUnit.Core" && assembly.Version >= new Version(1, 71)) ||
			    !options.TryGetValue("build_property.EnableTUnitSourceGeneration", out string? sourceGeneration) ||
			    IsFalse(sourceGeneration) ||
			    (options.TryGetValue("build_property.EnableTUnitPolyfills", out string? polyfills) && IsFalse(polyfills)))
			{
				return false;
			}

			return !PolySharpSelectsModuleInitializer(options);

			static bool IsFalse(string value)
				=> string.Equals(value.Trim(), "false", StringComparison.OrdinalIgnoreCase);
		}

		// MockolateUnionAttributePolyfills: "false" means the project gets OverloadResolutionPriorityAttribute,
		// CallerArgumentExpressionAttribute and ModuleInitializerAttribute from another generator (PolySharp); any other
		// value is a ';'-separated list of attribute names (with or without namespace) that must not be polyfilled.
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
				provided.Add("ModuleInitializerAttribute");
				return provided;
			}

			foreach (string name in configured.Split(';'))
			{
				string trimmed = name.Trim();
				int lastDot = trimmed.LastIndexOf('.');
				if (trimmed.Length > 0 && !string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
				{
					// The lookups check the 'Attribute'-suffixed name; accept the suffix-less spelling too.
					string simpleName = lastDot < 0 ? trimmed : trimmed.Substring(lastDot + 1);
					provided.Add(simpleName);
					if (!simpleName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
					{
						provided.Add(simpleName + "Attribute");
					}
				}
			}

			return provided;
		}

		// Unions are used once the host compiler has shipped C# 15 (the generator is compiled against an older Roslyn
		// and cannot reference LanguageVersion.CSharp15 directly, hence the name-based lookup) and the compilation's
		// effective language version includes it, unless the MockolateDisableUnionParameters build property (made
		// compiler-visible by build/Mockolate.props) is "true".
		static bool HasUnionSupport(ParseOptions parseOptions, AnalyzerConfigOptionsProvider analyzerConfigOptions)
		{
			if (parseOptions is not CSharpParseOptions csharpParseOptions ||
			    !Enum.TryParse("CSharp15", out LanguageVersion csharp15) ||
			    csharpParseOptions.LanguageVersion < csharp15)
			{
				return false;
			}

			return !analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MockolateDisableUnionParameters",
				       out string? disabled) ||
			       !string.Equals(disabled.Trim(), "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	/// <summary>
	///     Whether PolySharp declares <c>ModuleInitializerAttribute</c> in the compilation. Its settings are visible to the
	///     compiler whenever it is installed. PolySharp declares the polyfill unless its two lists filter it out or the
	///     compilation has an accessible declaration; this method uses the same lookup as PolySharp.
	/// </summary>
	internal static bool PolySharpProvidesModuleInitializer(Compilation compilation, AnalyzerConfigOptions options)
	{
		const string metadataName = "System.Runtime.CompilerServices.ModuleInitializerAttribute";
		if (!PolySharpSelectsModuleInitializer(options))
		{
			return false;
		}

		// Like HasAccessibleTypeWithMetadataName in PolySharp: the single lookup, which skips embedded types of references,
		// decides when it finds a type; only otherwise does any accessible declaration count.
		return compilation.GetTypeByMetadataName(metadataName) is { } type
			? !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)
			: !compilation.GetTypesByMetadataName(metadataName)
				.Any(candidate => compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly));
	}

	private static bool PolySharpSelectsModuleInitializer(AnalyzerConfigOptions options)
	{
		const string metadataName = "System.Runtime.CompilerServices.ModuleInitializerAttribute";
		if (!options.TryGetValue("build_property.PolySharpIncludeGeneratedTypes", out string? included))
		{
			return false;
		}

		string[] includedTypes = Split(included);
		if (includedTypes.Length > 0 && !includedTypes.Contains(metadataName, StringComparer.Ordinal))
		{
			return false;
		}

		return !options.TryGetValue("build_property.PolySharpExcludeGeneratedTypes", out string? excluded) ||
		       !Split(excluded).Contains(metadataName, StringComparer.Ordinal);

		static string[] Split(string list)
			=> list.Split(';', ',')
				.Select(entry => entry.Trim())
				.Where(entry => entry.Length > 0)
				.ToArray();
	}

	/// <summary>
	///     Whether generated code can reference <paramref name="attributeName" /> without a conflict: exactly one
	///     declaration is usable (accessible, including an <see langword="internal" /> one exposed through
	///     <c>InternalsVisibleTo</c>, and not an embedded type of a reference), or the compilation declares it itself,
	///     which wins over imported copies. Several usable imported copies are ambiguous, so they count as missing.
	/// </summary>
	internal static bool HasAttribute(Compilation compilation, string attributeName)
	{
		INamedTypeSymbol[] accessible = GetUsableTypes(compilation, attributeName);
		return accessible.Length == 1 ||
		       accessible.Any(type => SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly));
	}

	/// <summary>
	///     The declarations of the type that the compilation can use: accessible, and not an embedded type of a reference,
	///     which the compiler ignores.
	/// </summary>
	private static INamedTypeSymbol[] GetUsableTypes(Compilation compilation, string metadataName)
		=> compilation.GetTypesByMetadataName(metadataName)
			.Where(type => compilation.IsSymbolAccessibleWithin(type, compilation.Assembly) &&
			               (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ||
			                !type.GetAttributes().Any(attribute =>
				                attribute.AttributeClass?.ToDisplayString() == "Microsoft.CodeAnalysis.EmbeddedAttribute")))
			.ToArray();

	private static SourceText ToSource(string source, EquatableArray<string> externAliases)
		=> SourceText.From(Sources.Sources.ExpandCrefs(WithExternAliases(source, externAliases)), Encoding.UTF8);

	private static EquatableArray<string> GetExternAliases(Compilation compilation)
		=> new(compilation.References
			.SelectMany(reference => reference.Properties.Aliases)
			.Where(alias => alias != "global")
			.Distinct()
			.OrderBy(alias => alias, StringComparer.Ordinal)
			.ToArray());

	private static string WithExternAliases(string source, EquatableArray<string> externAliases)
	{
		StringBuilder? directives = null;
		foreach (string alias in externAliases)
		{
			if (source.IndexOf(alias + "::", StringComparison.Ordinal) >= 0)
			{
				directives ??= new StringBuilder();
				directives.AppendLine().Append("extern alias ").Append(alias).Append(';');
			}
		}

		return directives is null
			? source
			: source.Insert(Sources.Sources.AutoGeneratedHeader.Length, directives.ToString());
	}

	private static EquatableArray<string> ReadNamespaces(AnalyzerConfigOptionsProvider options, string property)
		=> new(options.GlobalOptions.TryGetValue(property, out string? value)
			? value.Split([';',], StringSplitOptions.RemoveEmptyEntries)
				.Select(@namespace => @namespace.Trim().TrimEnd('.'))
				.Where(@namespace => @namespace.Length > 0)
				.Distinct(StringComparer.Ordinal)
				.ToArray()
			: []);

	private static EquatableArray<string> SortedDistinct(IEnumerable<string> types)
		=> new(types.Distinct(StringComparer.Ordinal).OrderBy(type => type, StringComparer.Ordinal).ToArray());

	/// <summary>
	///     Binds the <c>CreateMock</c> calls and the <c>IMockDataSource</c> members of the <paramref name="creations" />
	///     again and discovers the mocks for auto-mocking of the types they mock, also of the constructor dependencies.
	/// </summary>
	/// <remarks>
	///     Calls that mock the same types add the same types to the walk, so only the first of them is bound again.
	///     The <see cref="AutoMockDiscoveryCache" /> reuses the result of the previous compilation when the edits
	///     cannot change it.
	/// </remarks>
	private static EquatableArray<MockClass> DiscoverAutoMocks(ImmutableArray<MockCreation> creations,
		Compilation compilation, AutoMockingOptions options, CancellationToken cancellationToken)
	{
		HashSet<string> identities = new(StringComparer.Ordinal);
		List<string> orderedIdentities = [];
		List<MockCreation> distinctCreations = [];
		foreach (MockCreation creation in creations)
		{
			string identity = string.Join("\n", creation.Mocks.AsArray().Select(mock => mock.IdentityKey));
			if (creation.Mocks.Count > 0 && identities.Add(identity))
			{
				orderedIdentities.Add(identity);
				distinctCreations.Add(creation);
			}
		}

		// The empty entries separate the excluded and included namespaces and the identities, as none of them is empty.
		string[] settingParts =
			[..options.ExcludedNamespaces, "", ..options.IncludedNamespaces, "", ..orderedIdentities,];
		string settings = string.Join("\n", settingParts);
		return AutoMockDiscoveryCache.GetOrDiscover(compilation, settings,
			dependencies => DiscoverAutoMocks(distinctCreations, compilation, options, dependencies,
				cancellationToken),
			cancellationToken);
	}

	private static EquatableArray<MockClass> DiscoverAutoMocks(List<MockCreation> creations, Compilation compilation,
		AutoMockingOptions options, AutoMockDependencies dependencies,
		CancellationToken cancellationToken)
	{
		EntityCache cache = EntityCache.GetOrCreate(compilation);
		using EntityCache.Scope scope = EntityCache.EnterScope(cache);
		ILookup<string, SyntaxTree> trees = compilation.SyntaxTrees.ToLookup(tree => tree.FilePath, StringComparer.Ordinal);
		Dictionary<SyntaxTree, SemanticModel> semanticModels = new();
		List<ITypeSymbol[]> mockedTypes = [];
		foreach (MockCreation creation in creations)
		{
			CreateMockLocation location = creation.Location;
			TextSpan span = new(location.Start, location.Length);
			// Trees without a file path share the empty one, so every tree with the path is a candidate.
			foreach (SyntaxTree tree in trees[location.FilePath])
			{
				cancellationToken.ThrowIfCancellationRequested();
				SyntaxNode root = tree.GetRoot(cancellationToken);
				if (!root.FullSpan.Contains(span) ||
				    root.FindNode(span, getInnermostNodeForTie: true) is not { } node ||
				    node.Span != span ||
				    !(node.IsCreateMethodInvocation() || node.IsDataSourceMemberCandidate()))
				{
					continue;
				}

				if (!semanticModels.TryGetValue(tree, out SemanticModel? semanticModel))
				{
					semanticModel = compilation.GetSemanticModel(tree);
					semanticModels[tree] = semanticModel;
				}

				if (node.IsSealedCreateMockInvocation())
				{
					mockedTypes.AddRange(node.ExtractSealedMockedTypes(semanticModel, cancellationToken));
				}
				else if (node.IsCreateMethodInvocation())
				{
					mockedTypes.AddRange(node.ExtractMockedTypes(semanticModel).SelectMany(types =>
						MockGeneratorHelpers.GetConstructorDependencyMockTypes(types[0], compilation)
							.Select(type => new[] { type, })
							.Prepend(types)));
				}
				else
				{
					mockedTypes.AddRange(node.ExtractDataSourceMockedTypes(semanticModel, cancellationToken));
				}
			}
		}

		return new EquatableArray<MockClass>(MockGeneratorHelpers.DiscoverAutoMocks(mockedTypes, compilation,
			options, dependencies, cancellationToken));
	}

	/// <remarks>
	///     A mock that a <c>CreateMock</c> call names wins over one that only a constructor dependency or auto-mocking
	///     needs, and two spellings of the same runtime type (nullable annotations, tuple element names) share one mock,
	///     because a second one would make <c>CreateMock</c> ambiguous.
	/// </remarks>
	private static EquatableArray<MockClass> Distinct(ImmutableArray<MockClass> mocks,
		MockClass[] discoveredMocks)
	{
		if (mocks.IsDefaultOrEmpty && discoveredMocks.Length == 0)
		{
			return new EquatableArray<MockClass>([]);
		}

		HashSet<MockClass> seen = new();
		HashSet<string> seenIdentities = new(StringComparer.Ordinal);
		List<MockClass> distinct = new(mocks.Length + discoveredMocks.Length);
		foreach (MockClass mc in mocks.Where(mock => !mock.IsDiscovered))
		{
			if (seen.Add(mc))
			{
				distinct.Add(mc);
				seenIdentities.Add(mc.IdentityKey);
			}
		}

		foreach (MockClass mc in mocks.Where(mock => mock.IsDiscovered).Concat(discoveredMocks))
		{
			if (seenIdentities.Add(mc.IdentityKey))
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

			if (mc.Delegate is { Parameters.Count: > 4, } @delegate)
			{
				set.Add(new MethodSetupKey(@delegate.Parameters.Count, @delegate.ReturnType == Type.Void));
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

	private static EquatableArray<NamedMock> CreateNamedMocks(EquatableArray<MockClass> mocks, bool autoMocking)
	{
		MockClass[] arr = mocks.AsArray();
		if (arr.Length == 0)
		{
			return new EquatableArray<NamedMock>([]);
		}

		HashSet<string> classNames = new(StringComparer.OrdinalIgnoreCase);
		if (autoMocking)
		{
			// Reserved for the auto-mock factory that MockBehaviorExtensions.g.cs declares in the same class.
			classNames.Add("CreateAutoMock");
		}

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
		// order here must be deterministic so the same input set always yields the same names. Mocks named in a
		// CreateMock call come first, so that a mock discovered for auto-mocking never renames them.
		List<Class> orderedClasses = new();
		foreach (MockClass mc in arr.OrderBy(mc => mc.IsDiscovered))
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

	private static LoggerMockNames CollectLoggerMockNames(EquatableArray<NamedMock> mocks)
	{
		const string loggerFullName = "global::Microsoft.Extensions.Logging.ILogger";
		string? loggerName = null;
		List<(string CategoryType, string Name)> categoryLoggers = new();
		foreach (NamedMock named in mocks)
		{
			if (named.AdditionalClasses is not null)
			{
				continue;
			}

			string fullName = named.Mock.ClassFullName;
			if (fullName == loggerFullName)
			{
				loggerName = named.ParentName;
			}
			else if (Sources.Sources.GetLoggerCategoryType(fullName) is { } categoryType)
			{
				categoryLoggers.Add((categoryType, named.ParentName));
			}
		}

		return new LoggerMockNames(loggerName,
			new EquatableArray<(string CategoryType, string Name)>(categoryLoggers.ToArray()));
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
						nm.ParentName, nm.Mock, hiddenBase.Name, hiddenBase.Class));
				}
			}

			if (nm.AdditionalClasses is not { } additional || additional.Count == 0)
			{
				continue;
			}

			NamedClass[] additionalArr = additional.AsArray();
			NamedClass last = additionalArr[additionalArr.Length - 1];

			AddIfNew(seen, ordered, MockAsExtensionPair.Create(nm.ParentName, nm.Mock, last.Name, last.Class));
			for (int i = 0; i < additionalArr.Length - 1; i++)
			{
				AddIfNew(seen, ordered, MockAsExtensionPair.Create(additionalArr[i].Name, additionalArr[i].Class, last.Name, last.Class));
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
		bool hasUnionSupport, EquatableArray<string> externAliases, AutoMockingOptions? autoMocking)
	{
		string fileName = named.FileName;
		Class @class = named.Mock;
		// hasOverloadResolutionPriority already accounts for the polyfill that ParameterArg.g.cs emits in union mode.
		bool useUnionOverloads = hasUnionSupport;

		if (@class is MockClass { Delegate: not null, } mockClass)
		{
			context.AddSource($"Mock.{fileName}.g.cs",
				ToSource(Sources.Sources.MockDelegate(named.ParentName, mockClass, mockClass.Delegate, autoMocking,
					hasOverloadResolutionPriority, useUnionOverloads), externAliases));
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
				ToSource(Sources.Sources.MockClass(named.ParentName, @class, autoMocking, hasOverloadResolutionPriority, hiddenBaseArr,
					useUnionOverloads), externAliases));
			return;
		}

		NamedClass[] additionalNamed = additional.AsArray();
		(string Name, Class Class)[] additionalArr = new (string Name, Class Class)[additionalNamed.Length];
		for (int i = 0; i < additionalNamed.Length; i++)
		{
			additionalArr[i] = (additionalNamed[i].Name, additionalNamed[i].Class);
		}

		context.AddSource($"Mock.{fileName}.g.cs",
			ToSource(Sources.Sources.MockCombinationClass(fileName, named.ParentName, @class, additionalArr, autoMocking, hasOverloadResolutionPriority,
				useUnionOverloads), externAliases));
	}

	internal static bool IsValidMockDeclaration(MockClass mockClass)
		=> (mockClass.IsInterface || mockClass.Constructors is { Count: > 0, }) &&
		   mockClass.AdditionalImplementations.All(x => x.IsInterface) &&
		   mockClass.AllImplementations().All(x => !x.HasInaccessibleRequiredMember);
}

internal readonly record struct MethodSetupKey(int Arity, bool IsVoid);

internal readonly record struct CreateMockLocation(string FilePath, int Start, int Length);

internal readonly record struct MockCreation(EquatableArray<MockClass> Mocks, CreateMockLocation Location);

internal readonly record struct RefStructIndexerSetup(int Arity, bool HasGetter, bool HasSetter);

internal readonly record struct IndexerSetupKey(int Arity, bool NeedsGetterOnly, bool NeedsSetterOnly);

internal readonly record struct MockAsExtensionPair(
	string SourceName,
	string SourceFullName,
	string SourceXmlDocReference,
	string OtherName,
	string OtherFullName,
	string OtherXmlDocReference)
{
	public static MockAsExtensionPair Create(string nameA, Class a, string nameB, Class b)
		=> string.CompareOrdinal(nameA, nameB) <= 0
			? new MockAsExtensionPair(nameA, a.ClassFullName, a.XmlDocReference, nameB, b.ClassFullName, b.XmlDocReference)
			: new MockAsExtensionPair(nameB, b.ClassFullName, b.XmlDocReference, nameA, a.ClassFullName, a.XmlDocReference);
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

internal readonly record struct LoggerMockNames(
	string? LoggerName,
	EquatableArray<(string CategoryType, string Name)> CategoryLoggers);

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
