using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
{
	internal static bool IsDataSourceCandidate(this SyntaxNode node)
		=> node is InvocationExpressionSyntax
		   {
			   Expression: MemberAccessExpressionSyntax
			   {
				   Name: IdentifierNameSyntax { Identifier.ValueText: "CreateWithMocks", },
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
		List<INamedTypeSymbol> suts = [];
		List<ITypeSymbol> mockedTypes = [];
		if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, })
		{
			if (semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is INamedTypeSymbol type &&
			    type.GetMembers("CreateWithMocks").IsEmpty)
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
				if (IsSutCandidate(parameter.Type))
				{
					suts.Add((INamedTypeSymbol)parameter.Type);
				}
				else if (IsDependencyMockable(parameter.Type))
				{
					mockedTypes.Add(parameter.Type);
				}
			}
		}

		List<SutClass> sutClasses = [];
		List<ITypeSymbol> typesToMock = [..mockedTypes,];
		foreach (INamedTypeSymbol sut in suts)
		{
			if (GetSutConstructor(sut, compilation) is not { } constructor)
			{
				continue;
			}

			List<SutParameter> parameters = [];
			foreach (IParameterSymbol parameter in constructor.Parameters)
			{
				bool isMockable = IsDependencyMockable(parameter.Type);
				if (isMockable)
				{
					typesToMock.Add(parameter.Type);
				}

				parameters.Add(new SutParameter(ToFullName(parameter.Type), parameter.Name, isMockable));
			}

			sutClasses.Add(new SutClass(ToFullName(sut), sut.Name, new EquatableArray<SutParameter>(parameters.ToArray())));
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
		=> type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass?.AllInterfaces.Any(@interface =>
			@interface.Name == "IMockDataSource" && IsInGlobalMockolateNamespace(@interface)) == true;

	/// <summary>
	///     A parameter of a concrete class outside the framework namespaces is created with mocked dependencies instead
	///     of being mocked itself.
	/// </summary>
	private static bool IsSutCandidate(ITypeSymbol type)
		=> type is INamedTypeSymbol
		   {
			   TypeKind: TypeKind.Class,
			   IsAbstract: false,
			   IsStatic: false,
			   IsGenericType: false,
			   SpecialType: SpecialType.None,
		   } namedType &&
		   !IsFrameworkNamespace(namedType.ContainingNamespace);

	/// <summary>
	///     A class mock is only created when the class has a parameterless constructor, because the generated code has
	///     no constructor arguments for it.
	/// </summary>
	private static bool IsDependencyMockable(ITypeSymbol type)
		=> IsMockable(type) &&
		   (type.TypeKind != TypeKind.Class ||
		    ((INamedTypeSymbol)type).InstanceConstructors.Any(constructor =>
			    constructor.Parameters.IsEmpty && constructor.DeclaredAccessibility != Accessibility.Private));

	/// <summary>
	///     The accessible constructor with the most parameters, or <see langword="null" /> when several share that count.
	/// </summary>
	private static IMethodSymbol? GetSutConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] candidates = type.InstanceConstructors
			.Where(constructor => compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly) &&
			                      constructor.Parameters.All(parameter =>
				                      parameter.RefKind == RefKind.None && !parameter.Type.IsRefLikeType))
			.OrderByDescending(constructor => constructor.Parameters.Length)
			.ToArray();
		return candidates.Length switch
		{
			0 => null,
			1 => candidates[0],
			_ => candidates[0].Parameters.Length == candidates[1].Parameters.Length ? null : candidates[0],
		};
	}
}
