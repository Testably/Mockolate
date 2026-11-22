using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
{
	private const string CreateWithMocksName = "CreateWithMocks";

	internal static bool IsCreateWithMocksInvocation(this SyntaxNode node)
		=> node is InvocationExpressionSyntax
		{
			Expression: MemberAccessExpressionSyntax
			{
				Name: IdentifierNameSyntax { Identifier.ValueText: CreateWithMocksName, },
			},
		};

	/// <summary>
	///     The class under test of a <c>T.CreateWithMocks(...)</c> call.
	/// </summary>
	internal static SutCreation ExtractSutCreation(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		(SutClass? sut, List<ITypeSymbol> typesToMock) = GetSut(node, semanticModel, cancellationToken);
		return new SutCreation(
			sut,
			new EquatableArray<MockClass>(typesToMock
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
		foreach (INamedTypeSymbol adapterReturnType in GetAdapterMockedReturnTypes(normalized).OfType<INamedTypeSymbol>())
		{
			yield return GetOrCreateDiscoveredMockClass(adapterReturnType, compilation);
		}
	}

	private static MockClass GetOrCreateDiscoveredMockClass(INamedTypeSymbol type, Compilation compilation)
		=> EntityCache.Current is { } cache
			? cache.GetOrAddDiscoveredMock(type, discovered => CreateDiscoveredMockClass(discovered, compilation))
			: CreateDiscoveredMockClass(type, compilation);

	/// <summary>
	///     The dependencies that the <c>CreateWithMocks</c> call <paramref name="node" /> mocks, as roots for the discovery
	///     of auto-mocks.
	/// </summary>
	internal static IEnumerable<ITypeSymbol[]> ExtractSutMockedTypes(this SyntaxNode node,
		SemanticModel semanticModel, CancellationToken cancellationToken)
		=> GetSut(node, semanticModel, cancellationToken).TypesToMock.Select(type => new[] { type, });

	/// <remarks>
	///     The call is only a candidate while it binds to nothing, because the generator does not see its own output;
	///     a call that binds belongs to a member of the user.
	/// </remarks>
	private static (SutClass? Sut, List<ITypeSymbol> TypesToMock) GetSut(SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		List<ITypeSymbol> typesToMock = [];
		if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation)
		{
			return (null, typesToMock);
		}

		SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
		if (symbolInfo.Symbol is not null || !symbolInfo.CandidateSymbols.IsEmpty ||
		    semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is not INamedTypeSymbol sut ||
		    !IsConstructibleSut(sut, compilation) ||
		    HasMemberNamedCreateWithMocks(sut) ||
		    IsFrameworkNamespace(sut.ContainingNamespace) ||
		    GetContainerName(sut, compilation) is not { } containerName ||
		    GetSutConstructor(sut, compilation) is not { } constructor)
		{
			return (null, typesToMock);
		}

		List<SutParameter> parameters = [];
		foreach (IParameterSymbol parameter in constructor.Parameters)
		{
			string? defaultValue = parameter.HasExplicitDefaultValue
				? MethodParameter.From(parameter).ExplicitDefaultValue
				: null;
			ITypeSymbol parameterType = parameter.Type.WithDynamicAsObject(compilation);
			if (parameterType.IsReferenceType && parameterType.NullableAnnotation == NullableAnnotation.None)
			{
				// A nullable-oblivious parameter accepts null, which the generated signature in a nullable-enabled
				// file would otherwise reject (CS8625).
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
				parameter.Name,
				isMockable,
				defaultValue,
				isParamsArray,
				!isMockable && defaultValue == "null" &&
				parameterType is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated, }));
		}

		return (new SutClass(
				ToFullName(sut),
				sut.ContainingNamespace.IsGlobalNamespace ? null : sut.ContainingNamespace.ToDisplayString(),
				containerName,
				new EquatableArray<SutParameter>(parameters.ToArray())),
			typesToMock.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToList());
	}

	private static string ToFullName(ITypeSymbol type)
		=> type.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);

	/// <summary>
	///     Whether the generated code can call a constructor of <paramref name="type" />.
	/// </summary>
	private static bool IsConstructibleSut(INamedTypeSymbol type, Compilation compilation)
		=> type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false, IsGenericType: false, } &&
		   !ContainsTypeParameter(type) &&
		   !UsesTypeNestedInGenericType(type) &&
		   !IsFileLocal(type) &&
		   !HasUsageDiagnostic(type) &&
		   Helpers.IsAccessibleFrom(type, compilation.Assembly);

	/// <summary>
	///     A file-local type is not visible to the generated file.
	/// </summary>
	private static bool IsFileLocal(INamedTypeSymbol type)
	{
		for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
		{
			if (current.IsFileLocal)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     A member of that name would hide the generated <c>CreateWithMocks</c> extension.
	/// </summary>
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
	///     <c>IEnumerable&lt;T&gt;</c> would be mocks that return <see langword="null" />, so only <c>ILogger</c>,
	///     <c>ILogger&lt;T&gt;</c> and <c>ILoggerFactory</c>, for which Mockolate has dedicated mocks, are mocked from the
	///     framework namespaces.
	/// </summary>
	private static bool IsDependencyMockable(ITypeSymbol type, Compilation compilation)
		=> type is INamedTypeSymbol namedType &&
		   (!IsFrameworkNamespace(namedType.ContainingNamespace) || IsDedicatedLoggingType(namedType)) &&
		   CanBeMockedImplicitly(namedType, compilation.Assembly);

	private static bool IsDedicatedLoggingType(INamedTypeSymbol type)
		=> type.Name is "ILogger" or "ILoggerFactory" &&
		   type.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Logging";

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
	///     read in pairs from the left, <c>__</c> and <c>_0</c> cannot be confused, so two classes never share a name.
	///     The name never gets a suffix, so that call sites do not break when a type is added; while an accessible type or
	///     namespace has the name, the class gets no <c>CreateWithMocks</c>. An internal container that a referenced
	///     assembly generated is not accessible and does not count.
	/// </remarks>
	private static string? GetContainerName(INamedTypeSymbol sut, Compilation compilation)
	{
		List<string> names = [];
		for (INamedTypeSymbol? current = sut; current is not null; current = current.ContainingType)
		{
			names.Insert(0, current.Name.Replace("_", "__"));
		}

		string name = string.Join("_0", names) + "Mocks";
		INamespaceSymbol? @namespace = compilation.GetCompilationNamespace(sut.ContainingNamespace);
		return @namespace?.GetMembers(name).Any(member =>
			compilation.IsSymbolAccessibleWithin(member, compilation.Assembly)) == true
			? null
			: name;
	}
}
