using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators;

internal static class MockGeneratorHelpers
{
	internal static bool IsCreateMethodInvocation(this SyntaxNode node)
	{
		if (node is not InvocationExpressionSyntax
		    {
			    Expression: MemberAccessExpressionSyntax memberAccess,
		    })
		{
			return false;
		}

		switch (memberAccess.Name)
		{
			case IdentifierNameSyntax { Identifier.ValueText: "CreateMock", }:
				return true;
			case GenericNameSyntax { Identifier.ValueText: "Implementing", }:
				return true;
			default:
				return false;
		}
	}

	private static bool IsInGlobalMockolateNamespace(ISymbol symbol)
		=> symbol.ContainingNamespace is { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, };

	private static bool IsCreateMockMethod(IMethodSymbol methodSymbol)
		=> methodSymbol.Name == "CreateMock" && IsInGlobalMockolateNamespace(methodSymbol);

	private static bool IsImplementingMethod(IMethodSymbol methodSymbol)
		=> methodSymbol.Name == "Implementing" && IsInGlobalMockolateNamespace(methodSymbol);

	private static bool IsCreateMockInvocation(this SymbolInfo symbolInfo)
	{
		if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
		{
			return IsCreateMockMethod(methodSymbol);
		}

		return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(IsCreateMockMethod);
	}

	private static bool IsImplementingInvocation(this SymbolInfo symbolInfo)
	{
		if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
		{
			return IsImplementingMethod(methodSymbol);
		}

		return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(IsImplementingMethod);
	}

	internal static IEnumerable<MockClass> ExtractMockOrMockFactoryCreateSyntaxOrDefault(
		this SyntaxNode syntaxNode, SemanticModel semanticModel)
	{
		IAssemblySymbol sourceAssembly = semanticModel.Compilation.Assembly;
		// Static extension methods
		if (syntaxNode is InvocationExpressionSyntax invocation)
		{
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
			IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
			if (methodSymbol is not null && symbolInfo.IsCreateMockInvocation())
			{
				ITypeSymbol? type = methodSymbol.ContainingType.ExtensionParameter?.Type;
				if (type is not null && IsMockable(type))
				{
					yield return CreateMockClass(semanticModel.Compilation, type);
					foreach (MockClass? additionalMockClass in DiscoverMockableTypes([type,], sourceAssembly))
					{
						yield return additionalMockClass;
					}
				}
				else if (IsMockable(methodSymbol.ReturnType))
				{
					yield return CreateMockClass(semanticModel.Compilation, methodSymbol.ReturnType);

					foreach (MockClass? additionalMockClass in DiscoverMockableTypes([methodSymbol.ReturnType,], sourceAssembly))
					{
						yield return additionalMockClass;
					}
				}
			}
			else if (syntaxNode is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax, }, } invocationSyntax &&
			         methodSymbol is { IsGenericMethod: true, } &&
			         symbolInfo.IsImplementingInvocation())
			{
				List<ITypeSymbol> collectedTypes = [];
				ExpressionSyntax? chainRootExpression = null;
				InvocationExpressionSyntax currentInvocation = invocationSyntax;

				while (true)
				{
					symbolInfo = semanticModel.GetSymbolInfo(currentInvocation);
					methodSymbol = symbolInfo.Symbol as IMethodSymbol
					               ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

					if (methodSymbol is { Name: "Implementing", } && symbolInfo.IsImplementingInvocation())
					{
						// Collect concrete generic args from each chained call, e.g. Implementing<IMyInterface>()
						foreach (ITypeSymbol typeArgument in methodSymbol.TypeArguments)
						{
							collectedTypes.Add(typeArgument);
						}
					}

					if (currentInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
					{
						// Continue walking left: foo.Bar().Baz<T>() -> inspect foo.Bar()
						if (memberAccess.Expression is InvocationExpressionSyntax innerInvocation)
						{
							currentInvocation = innerInvocation;
							continue;
						}

						// Left-most expression, e.g. "MyService" in "MyService.CreateMock()"
						chainRootExpression = memberAccess.Expression;
					}

					break;
				}

				ITypeSymbol? rootType = null;
				if (chainRootExpression != null)
				{
					// For instance receiver: variable/field/property -> type via TypeInfo
					rootType = semanticModel.GetTypeInfo(chainRootExpression).Type;

					// For static receiver: type name like "MyService" -> symbol is INamedTypeSymbol
					if (rootType == null)
					{
						ISymbol? rootSymbol = semanticModel.GetSymbolInfo(chainRootExpression).Symbol;
						rootType = rootSymbol as ITypeSymbol;
					}
				}

				// We traversed from outermost to innermost, so reverse to get natural order
				collectedTypes.Reverse();

				if (IsMockable(rootType))
				{
					yield return CreateMockClass(semanticModel.Compilation, [rootType!, ..collectedTypes,]);
				}
			}
		}
	}

	private static MockClass CreateMockClass(Compilation compilation, params ITypeSymbol[] types)
		=> new(Array.ConvertAll(types, type => type.WithDynamicAsObject(compilation)), compilation.Assembly);

	private static IEnumerable<MockClass> DiscoverMockableTypes(IEnumerable<ITypeSymbol> initialTypes,
		IAssemblySymbol sourceAssembly)
	{
		Queue<ITypeSymbol> typesToProcess = new(initialTypes);
		HashSet<ITypeSymbol> processedTypes = new(typesToProcess, SymbolEqualityComparer.Default);

		while (typesToProcess.Count > 0)
		{
			ITypeSymbol currentType = typesToProcess.Dequeue();

			// When using HttpClient as a mock, we also have to create a mock of the HttpMessageHandler, that can be used as constructor parameter.
			if (currentType.Name == "HttpClient" &&
			    currentType.ContainingNamespace is { ContainingNamespace: { ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true, ContainingNamespace.Name: "System", Name: "Net", }, Name: "Http", })
			{
				ITypeSymbol httpMessageHandlerType = currentType.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(m => m.MethodKind == MethodKind.Constructor)
					.SelectMany(c => c.Parameters)
					.Select(p => p.Type)
					.First(t => t.Name == "HttpMessageHandler");
				if (processedTypes.Add(httpMessageHandlerType))
				{
					yield return new MockClass([httpMessageHandlerType,], sourceAssembly);
					typesToProcess.Enqueue(httpMessageHandlerType);
				}
			}
		}
	}

	private static bool IsMockable(ITypeSymbol? typeSymbol)
		=> typeSymbol is
		   {
			   DeclaredAccessibility: not Accessibility.Private and not Accessibility.ProtectedAndInternal and not Accessibility.ProtectedAndFriend,
			   IsRecord: false,
			   IsReadOnly: false,
			   ContainingNamespace.IsGlobalNamespace: false,
			   TypeKind: TypeKind.Interface or TypeKind.Class or TypeKind.Delegate,
		   } and INamedTypeSymbol namedTypeSymbol &&
		   // Ignore open generic types
		   (!namedTypeSymbol.IsGenericType ||
		    namedTypeSymbol.TypeArguments.All(a => a.TypeKind != TypeKind.TypeParameter)) &&
		   (!typeSymbol.IsSealed || typeSymbol.TypeKind == TypeKind.Delegate) &&
		   !(typeSymbol.Name == "IMock" &&
		     typeSymbol.ContainingNamespace.Name == "Mockolate" &&
		     typeSymbol.ContainingNamespace.ContainingNamespace.IsGlobalNamespace);
}
