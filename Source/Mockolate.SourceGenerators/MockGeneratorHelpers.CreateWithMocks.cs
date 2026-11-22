using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

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
	/// <remarks>
	///     The call is only a candidate while it binds to nothing, because the generator does not see its own output;
	///     a call that binds belongs to a member of the user.
	/// </remarks>
	internal static DataSourceCreation ExtractDataSourceCreation(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		List<INamedTypeSymbol> suts = [];
		List<ITypeSymbol> mockedTypes = [];
		if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation)
		{
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			if (symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.IsEmpty &&
			    semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is INamedTypeSymbol type &&
			    IsConstructibleSut(type, compilation))
			{
				suts.Add(type);
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
				ITypeSymbol parameterType = parameter.Type.WithDynamicAsObject(compilation);
				ITypeSymbol notAnnotatedType = parameterType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
				bool isMockable = !parameter.IsParams && IsDependencyMockable(notAnnotatedType, compilation);
				if (isMockable)
				{
					typesToMock.Add(notAnnotatedType);
				}

				string? defaultValue = parameter.HasExplicitDefaultValue
					? MethodParameter.From(parameter).ExplicitDefaultValue
					: null;
				parameters.Add(new SutParameter(
					ToFullName(isMockable ? notAnnotatedType : parameterType),
					ToFullName(notAnnotatedType),
					parameter.Name,
					isMockable,
					parameterType.NullableAnnotation == NullableAnnotation.Annotated ||
					parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
					defaultValue,
					parameter.IsParams));
			}

			sutClasses.Add(new SutClass(
				ToFullName(sut),
				sut.ContainingNamespace.IsGlobalNamespace ? null : sut.ContainingNamespace.ToDisplayString(),
				GetContainerName(sut, compilation),
				new EquatableArray<SutParameter>(parameters.ToArray())));
		}

		return new DataSourceCreation(
			new EquatableArray<SutClass>(sutClasses.ToArray()),
			new EquatableArray<string>(mockedTypes.Select(ToFullName).Distinct().ToArray()),
			new EquatableArray<MockClass>(typesToMock
				.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
				.SelectMany(type => CreateMockClasses(compilation, [type,]))
				.ToArray()));
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
	///     because no <c>CreateMock</c> call in the user code names them.
	/// </summary>
	private static bool IsDependencyMockable(ITypeSymbol type, Compilation compilation)
		=> type is INamedTypeSymbol namedType && CanBeMockedImplicitly(namedType, compilation.Assembly);

	/// <summary>
	///     The accessible constructor with the most parameters that the generated code can call without warnings, or
	///     <see langword="null" /> when several share that count.
	/// </summary>
	private static IMethodSymbol? GetSutConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] candidates = type.InstanceConstructors
			.Where(constructor => !constructor.IsVararg &&
			                      !HasUsageDiagnostic(constructor) &&
			                      compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly) &&
			                      constructor.Parameters.All(parameter =>
				                      parameter.RefKind == RefKind.None &&
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

	/// <summary>
	///     The name of the container of the constructor arguments, declared next to the class under test.
	/// </summary>
	/// <remarks>
	///     A nested class joins the names of its containing types. A suffix avoids a type that the user declared in the
	///     namespace, so the name never depends on the other classes under test.
	/// </remarks>
	private static string GetContainerName(INamedTypeSymbol sut, Compilation compilation)
	{
		List<string> names = [];
		for (INamedTypeSymbol? current = sut; current is not null; current = current.ContainingType)
		{
			names.Insert(0, current.Name);
		}

		string baseName = string.Join("_", names) + "Mocks";
		INamespaceSymbol? @namespace = compilation.GetCompilationNamespace(sut.ContainingNamespace);
		string name = baseName;
		for (int index = 1; @namespace?.GetTypeMembers(name).IsEmpty == false; index++)
		{
			name = $"{baseName}_{index}";
		}

		return name;
	}
}
