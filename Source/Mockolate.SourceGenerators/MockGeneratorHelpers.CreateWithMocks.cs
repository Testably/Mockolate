using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
{
	private const string CreateWithMocksName = "CreateWithMocks";

	internal static bool IsDataSourceCandidate(this SyntaxNode node)
		=> node is InvocationExpressionSyntax
		   {
			   Expression: MemberAccessExpressionSyntax
			   {
				   Name: IdentifierNameSyntax { Identifier.ValueText: CreateWithMocksName, },
			   },
		   } or MethodDeclarationSyntax
		   {
			   AttributeLists.Count: > 0,
			   ParameterList.Parameters.Count: > 0,
		   };

	/// <summary>
	///     The class under test of a <c>T.CreateWithMocks(...)</c> call, or the parameter types of a method with an
	///     attribute that implements <c>Mockolate.IMockDataSource</c>.
	/// </summary>
	internal static DataSourceCreation ExtractDataSourceCreation(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		DataSourceTargets targets = GetDataSourceTargets(node, semanticModel, cancellationToken);
		return new DataSourceCreation(
			new EquatableArray<SutClass>(targets.Suts.ToArray()),
			new EquatableArray<string>(targets.MockedTypes
				.Select(type => ToFullName(Normalize(type, compilation)))
				.Distinct()
				.ToArray()),
			new EquatableArray<MockClass>(targets.TypesToMock
				.SelectMany(type => CreateDependencyMockClasses(type, compilation))
				.ToArray()),
			new CreateMockLocation(node.SyntaxTree.FilePath, node.Span.Start, node.Span.Length));
	}

	/// <summary>
	///     Creates the mock of a dependency like a mock discovered for auto-mocking: in one spelling without nullable
	///     annotations and with a nullable-oblivious receiver, so that every spelling of the type uses it without a
	///     warning.
	/// </summary>
	private static IEnumerable<MockClass> CreateDependencyMockClasses(ITypeSymbol type, Compilation compilation)
	{
		if (Normalize(type, compilation) is not INamedTypeSymbol normalized)
		{
			yield break;
		}

		yield return GetOrCreateDiscoveredMockClass(normalized, compilation);
		if (GetLoggerOfLoggerFactory(normalized) is INamedTypeSymbol loggerType)
		{
			yield return GetOrCreateDiscoveredMockClass(loggerType, compilation);
		}
	}

	private static MockClass GetOrCreateDiscoveredMockClass(INamedTypeSymbol type, Compilation compilation)
		=> EntityCache.Current is { } cache
			? cache.GetOrAddDiscoveredMock(type, discovered => CreateDiscoveredMockClass(discovered, compilation))
			: CreateDiscoveredMockClass(type, compilation);

	/// <summary>
	///     The types that the <c>CreateWithMocks</c> call or the <c>IMockDataSource</c> method <paramref name="node" />
	///     mocks, as roots for the discovery of auto-mocks.
	/// </summary>
	internal static IEnumerable<ITypeSymbol[]> ExtractDataSourceMockedTypes(this SyntaxNode node,
		SemanticModel semanticModel, CancellationToken cancellationToken)
		=> GetDataSourceTargets(node, semanticModel, cancellationToken).TypesToMock.Select(type => new[] { type, });

	/// <remarks>
	///     The call is only a candidate while it binds to nothing, because the generator does not see its own output;
	///     a call that binds belongs to a member of the user.
	/// </remarks>
	private static DataSourceTargets GetDataSourceTargets(SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		List<INamedTypeSymbol> suts = [];
		List<ITypeSymbol> mockedTypes = [];
		bool hasCreateWithMocks = false;
		if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation)
		{
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			if (symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.IsEmpty &&
			    semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is INamedTypeSymbol type &&
			    IsConstructibleSut(type, compilation) &&
			    !IsFrameworkNamespace(type.ContainingNamespace))
			{
				suts.Add(type);
				hasCreateWithMocks = true;
			}
		}
		else if (node is MethodDeclarationSyntax method &&
		         semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol methodSymbol &&
		         methodSymbol.GetAttributes().Any(IsMockDataSourceAttribute))
		{
			foreach (IParameterSymbol parameter in methodSymbol.Parameters)
			{
				ITypeSymbol parameterType = parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				if (IsDependencyMockable(parameterType, compilation))
				{
					mockedTypes.Add(parameterType);
				}
				else if (parameterType is INamedTypeSymbol namedType && IsConstructibleSut(namedType, compilation) &&
				         !IsFrameworkNamespace(namedType.ContainingNamespace))
				{
					suts.Add(namedType);
				}
			}
		}

		List<SutClass> sutClasses = [];
		List<ITypeSymbol> typesToMock = [..mockedTypes,];
		foreach (INamedTypeSymbol sut in suts.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
		{
			if (GetSutConstructor(sut, compilation) is not { } constructor)
			{
				continue;
			}

			List<SutParameter> parameters = [];
			foreach (IParameterSymbol parameter in constructor.Parameters)
			{
				string? defaultValue = parameter.HasExplicitDefaultValue
					? MethodParameter.From(parameter).ExplicitDefaultValue
					: null;
				ITypeSymbol parameterType = parameter.Type.WithDynamicAsObject(compilation);
				if (defaultValue == "null" && parameterType.IsReferenceType)
				{
					// A nullable-oblivious parameter with a null default would warn (CS8625) in the generated signature.
					parameterType = parameterType.WithNullableAnnotation(NullableAnnotation.Annotated);
				}

				ITypeSymbol notAnnotatedType = parameterType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				bool isParamsArray = parameter.IsParams && parameterType is IArrayTypeSymbol;
				bool isMockable = !parameter.IsParams && IsDependencyMockable(notAnnotatedType, compilation);
				if (isMockable)
				{
					typesToMock.Add(notAnnotatedType);
				}

				parameters.Add(new SutParameter(
					ToFullName(isMockable ? notAnnotatedType : parameterType),
					ToFullName(notAnnotatedType),
					parameter.Name,
					isMockable,
					parameterType.NullableAnnotation == NullableAnnotation.Annotated ||
					parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
					defaultValue,
					isParamsArray));
			}

			sutClasses.Add(new SutClass(
				ToFullName(sut),
				sut.ContainingNamespace.IsGlobalNamespace ? null : sut.ContainingNamespace.ToDisplayString(),
				GetContainerName(sut, compilation),
				new EquatableArray<SutParameter>(parameters.ToArray()),
				hasCreateWithMocks));
		}

		return new DataSourceTargets(sutClasses, mockedTypes,
			typesToMock.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToList());
	}

	private static string ToFullName(ITypeSymbol type)
		=> type.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass?.AllInterfaces.Any(@interface =>
			@interface.Name == "IMockDataSource" && IsInGlobalMockolateNamespace(@interface)) == true;

	/// <summary>
	///     Whether the generated code can call a constructor of <paramref name="type" /> and a static
	///     <c>CreateWithMocks</c> extension on it without hitting a member of the type.
	/// </summary>
	private static bool IsConstructibleSut(INamedTypeSymbol type, Compilation compilation)
		=> type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false, IsGenericType: false, } &&
		   !ContainsTypeParameter(type) &&
		   !UsesTypeNestedInGenericType(type) &&
		   !HasUsageDiagnostic(type) &&
		   Helpers.IsAccessibleFrom(type, compilation.Assembly) &&
		   !HasMemberNamedCreateWithMocks(type);

	private static bool HasMemberNamedCreateWithMocks(INamedTypeSymbol type)
	{
		for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (!current.GetMembers(CreateWithMocksName).IsEmpty)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Only interfaces that the generator can mock without the checks of the <c>MockabilityAnalyzer</c> are mocked,
	///     because no <c>CreateMock</c> call in the user code names them. Framework interfaces such as
	///     <c>IEnumerable&lt;T&gt;</c> would be mocks that return <see langword="null" />, so only the logging interfaces,
	///     for which Mockolate has dedicated mocks, are mocked from the framework namespaces.
	/// </summary>
	private static bool IsDependencyMockable(ITypeSymbol type, Compilation compilation)
		=> type is INamedTypeSymbol namedType &&
		   (!IsFrameworkNamespace(namedType.ContainingNamespace) ||
		    namedType.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Logging") &&
		   CanBeMockedImplicitly(namedType, compilation.Assembly);

	/// <summary>
	///     The accessible constructor with the most parameters that the generated code can call without warnings, or
	///     <see langword="null" /> when several share that count.
	/// </summary>
	/// <remarks>
	///     A constructor that leaves <see langword="required" /> members unset cannot be called (CS9035), and parameters of
	///     a ref struct type can neither be stored in the container nor be optional.
	/// </remarks>
	private static IMethodSymbol? GetSutConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		bool hasRequiredMembers = HasRequiredMembers(type);
		IMethodSymbol[] candidates = type.InstanceConstructors
			.Where(constructor => !constructor.IsVararg &&
			                      !HasUsageDiagnostic(constructor) &&
			                      (!hasRequiredMembers || HasSetsRequiredMembers(constructor)) &&
			                      compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly) &&
			                      constructor.Parameters.All(parameter =>
				                      parameter.RefKind is RefKind.None or RefKind.In &&
				                      !parameter.Type.IsRefLikeType &&
				                      !UsesUnusableType(parameter.Type) &&
				                      Helpers.IsAccessibleFrom(parameter.Type, compilation.Assembly)))
			.OrderByDescending(constructor => constructor.Parameters.Length)
			.ToArray();
		return candidates.Length switch
		{
			0 => null,
			1 => candidates[0],
			_ => candidates[0].Parameters.Length == candidates[1].Parameters.Length ? null : candidates[0],
		};
	}

	private static bool HasRequiredMembers(INamedTypeSymbol type)
	{
		for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (current.GetMembers().Any(member => member is IPropertySymbol { IsRequired: true, } or
				    IFieldSymbol { IsRequired: true, }))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasSetsRequiredMembers(IMethodSymbol constructor)
		=> constructor.GetAttributes().Any(attribute => attribute.AttributeClass is
		{
			Name: "SetsRequiredMembersAttribute",
			ContainingNamespace:
			{
				Name: "CodeAnalysis",
				ContainingNamespace:
				{
					Name: "Diagnostics", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
				},
			},
		});

	/// <summary>
	///     The name of the container of the constructor arguments, declared next to the class under test.
	/// </summary>
	/// <remarks>
	///     A nested class joins the names of its containing types with <c>_0</c>, after doubling every <c>_</c> in them;
	///     read in pairs from the left, <c>__</c> and <c>_0</c> cannot be confused, so two classes never share a name. A
	///     suffix avoids a type or namespace that the user declared, so the name never depends on the other classes under
	///     test.
	/// </remarks>
	private static string GetContainerName(INamedTypeSymbol sut, Compilation compilation)
	{
		List<string> names = [];
		for (INamedTypeSymbol? current = sut; current is not null; current = current.ContainingType)
		{
			names.Insert(0, current.Name.Replace("_", "__"));
		}

		string baseName = string.Join("_0", names) + "Mocks";
		INamespaceSymbol? @namespace = compilation.GetCompilationNamespace(sut.ContainingNamespace);
		string name = baseName;
		for (int index = 1; @namespace?.GetMembers(name).Any() == true; index++)
		{
			name = $"{baseName}_{index}";
		}

		return name;
	}

	private sealed record DataSourceTargets(
		List<SutClass> Suts,
		List<ITypeSymbol> MockedTypes,
		List<ITypeSymbol> TypesToMock);
}
