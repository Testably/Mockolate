using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

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
					foreach (MockClass? additionalMockClass in DiscoverMockableTypes([type,], semanticModel.Compilation))
					{
						yield return additionalMockClass;
					}
				}
				else if (IsMockable(methodSymbol.ReturnType))
				{
					yield return CreateMockClass(semanticModel.Compilation, methodSymbol.ReturnType);

					foreach (MockClass? additionalMockClass in DiscoverMockableTypes([methodSymbol.ReturnType,], semanticModel.Compilation))
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
					foreach (MockClass additionalMockClass in DiscoverMockableTypes([rootType!, ..collectedTypes,], semanticModel.Compilation))
					{
						yield return additionalMockClass;
					}
				}
			}
		}
	}

	private static MockClass CreateMockClass(Compilation compilation, params ITypeSymbol[] types)
		=> new(Array.ConvertAll(types, type => type.WithDynamicAsObject(compilation)), compilation);

	/// <summary>
	///     Whether an un-configured member returning <paramref name="typeSymbol" /> can return an auto-mock.
	/// </summary>
	/// <remarks>
	///     Framework interfaces are excluded, because mocking them transitively would generate mocks for large parts
	///     of the base class library. A type that still contains a type parameter cannot be named in the auto-mock
	///     factory, and an interface with static abstract members cannot be a type argument (CS8920).
	/// </remarks>
	internal static bool IsAutoMockable(ITypeSymbol typeSymbol)
		=> typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Interface, } namedTypeSymbol &&
		   IsMockable(namedTypeSymbol) &&
		   !IsFrameworkNamespace(namedTypeSymbol.ContainingNamespace) &&
		   !ContainsTypeParameter(namedTypeSymbol) &&
		   !IsObsolete(namedTypeSymbol) &&
		   !HasStaticAbstractMembers(namedTypeSymbol);

	/// <summary>
	///     Whether the generator can create a mock of <paramref name="typeSymbol" /> for auto-mocking, which unlike a
	///     type in a <c>CreateMock</c> call is not checked by the <c>MockabilityAnalyzer</c>.
	/// </summary>
	/// <remarks>
	///     The type must be accessible from the compilation, must not expand itself (e.g. <c>INode&lt;T&gt;</c>
	///     returning <c>INode&lt;INode&lt;T&gt;&gt;</c>, which would never end) and no member the mock implements may be
	///     obsolete or use a type that cannot be a type argument, so that the generated mock compiles without
	///     warnings.
	/// </remarks>
	private static bool CanBeDiscovered(INamedTypeSymbol typeSymbol, IAssemblySymbol sourceAssembly)
		=> IsAutoMockable(typeSymbol) &&
		   Helpers.IsAccessibleFrom(typeSymbol, sourceAssembly) &&
		   !RepeatsGenericDefinition(typeSymbol, []) &&
		   GetImplementedMembers(typeSymbol, sourceAssembly).All(member =>
			   !IsObsolete(member) && GetSignatureTypes(member).All(type => !UsesUnusableType(type)));

	private static bool IsFrameworkNamespace(INamespaceSymbol? namespaceSymbol)
	{
		while (namespaceSymbol is { ContainingNamespace.IsGlobalNamespace: false, })
		{
			namespaceSymbol = namespaceSymbol.ContainingNamespace;
		}

		return namespaceSymbol?.Name is "System" or "Microsoft" or "Windows";
	}

	private static bool ContainsTypeParameter(ITypeSymbol typeSymbol)
		=> typeSymbol switch
		{
			ITypeParameterSymbol => true,
			IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
			IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
			INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter) ||
			                          (named.ContainingType is { } containingType && ContainsTypeParameter(containingType)),
			_ => false,
		};

	private static bool IsObsolete(ISymbol symbol)
		=> symbol.GetAttributes().Any(attribute => attribute.AttributeClass is
		   {
			   Name: "ObsoleteAttribute",
			   ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
		   }) ||
		   (symbol.ContainingType is { } containingType && IsObsolete(containingType));

	private static bool HasStaticAbstractMembers(INamedTypeSymbol typeSymbol)
		=> typeSymbol.GetMembers().Concat(typeSymbol.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

	/// <summary>
	///     Whether a member signature uses a type for which the generator emits no compilable mock without the
	///     diagnostics of the <c>MockabilityAnalyzer</c>: pointers, ref structs other than spans, obsolete types and
	///     interfaces with static abstract members.
	/// </summary>
	private static bool UsesUnusableType(ITypeSymbol typeSymbol)
		=> typeSymbol switch
		{
			IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
			IArrayTypeSymbol array => UsesUnusableType(array.ElementType),
			INamedTypeSymbol named => IsObsolete(named) ||
			                          (named.IsRefLikeType && named.GetSpecialType() is not
				                          (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan)) ||
			                          (named.TypeKind == TypeKind.Interface && HasStaticAbstractMembers(named)) ||
			                          named.TypeArguments.Any(UsesUnusableType),
			_ => false,
		};

	private static bool RepeatsGenericDefinition(ITypeSymbol typeSymbol, List<ISymbol> path)
	{
		switch (typeSymbol)
		{
			case IArrayTypeSymbol array:
				return RepeatsGenericDefinition(array.ElementType, path);
			case INamedTypeSymbol { IsGenericType: true, } named:
				if (path.Contains(named.OriginalDefinition, SymbolEqualityComparer.Default))
				{
					return true;
				}

				path.Add(named.OriginalDefinition);
				bool repeats = named.TypeArguments.Any(typeArgument => RepeatsGenericDefinition(typeArgument, path));
				path.RemoveAt(path.Count - 1);
				return repeats;
			default:
				return false;
		}
	}

	private static IEnumerable<ITypeSymbol> GetSignatureTypes(ISymbol member)
		=> member switch
		{
			IMethodSymbol method => [method.ReturnType, ..method.Parameters.Select(parameter => parameter.Type),],
			IPropertySymbol property => [property.Type, ..property.Parameters.Select(parameter => parameter.Type),],
			IEventSymbol @event => [@event.Type,],
			_ => [],
		};

	/// <summary>
	///     The members that a mock of <paramref name="typeSymbol" /> implements or overrides.
	/// </summary>
	private static IEnumerable<ISymbol> GetImplementedMembers(ITypeSymbol typeSymbol, IAssemblySymbol sourceAssembly)
	{
		IEnumerable<ITypeSymbol> types = [typeSymbol, ..typeSymbol.AllInterfaces,];
		for (ITypeSymbol? baseType = typeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			types = types.Append(baseType);
		}

		return types.SelectMany(type => type.GetMembers())
			.Where(member => member is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke, } ||
			                 (member is { IsStatic: false, IsSealed: false, } &&
			                  (member.IsAbstract || member.IsVirtual || member.IsOverride) &&
			                  Helpers.IsOverridableFrom(member, sourceAssembly) &&
			                  Helpers.HasAccessibleSignature(member, sourceAssembly)));
	}

	private static ITypeSymbol UnwrapTask(ITypeSymbol typeSymbol)
		=> typeSymbol is INamedTypeSymbol
		{
			TypeArguments.Length: 1,
			Name: "Task" or "ValueTask",
			ContainingNamespace:
			{
				Name: "Tasks",
				ContainingNamespace:
				{
					Name: "Threading", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
				},
			},
		} task
			? task.TypeArguments[0]
			: typeSymbol;

	private static IEnumerable<INamedTypeSymbol> GetAutoMockableReturnTypes(ITypeSymbol typeSymbol,
		IAssemblySymbol sourceAssembly)
	{
		foreach (ISymbol member in GetImplementedMembers(typeSymbol, sourceAssembly))
		{
			ITypeSymbol? returnType = member switch
			{
				IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.DelegateInvoke, ReturnsVoid: false, } method => method.ReturnType,
				IPropertySymbol { GetMethod: not null, } property => property.Type,
				_ => null,
			};
			if (returnType is not null && UnwrapTask(returnType) is INamedTypeSymbol candidate &&
			    CanBeDiscovered(candidate, sourceAssembly))
			{
				yield return candidate;
			}
		}
	}

	/// <summary>
	///     The mocks of <paramref name="initialTypes" /> needs besides themselves: auto-mocks of the interfaces their
	///     members return (transitively) and the <c>HttpMessageHandler</c> of an <c>HttpClient</c>.
	/// </summary>
	/// <remarks>
	///     The result is cached per type and compilation, because every <c>CreateMock</c> call of the type asks again.
	/// </remarks>
	private static IEnumerable<MockClass> DiscoverMockableTypes(IEnumerable<ITypeSymbol> initialTypes,
		Compilation compilation)
		=> initialTypes.SelectMany(type => EntityCache.Current is { } cache
			? cache.GetOrAddDiscoveredMocks(type, t => DiscoverMockableTypes(t, compilation))
			: DiscoverMockableTypes(type, compilation));

	private static MockClass[] DiscoverMockableTypes(ITypeSymbol initialType, Compilation compilation)
	{
		IAssemblySymbol sourceAssembly = compilation.Assembly;
		List<MockClass> result = [];
		Queue<ITypeSymbol> typesToProcess = new([initialType,]);
		HashSet<ITypeSymbol> processedTypes = new(typesToProcess, SymbolEqualityComparer.Default);

		while (typesToProcess.Count > 0)
		{
			ITypeSymbol currentType = typesToProcess.Dequeue();

			foreach (INamedTypeSymbol returnType in GetAutoMockableReturnTypes(currentType, sourceAssembly))
			{
				if (processedTypes.Add(returnType))
				{
					result.Add(EntityCache.Current is { } cache
						? cache.GetOrAddDiscoveredMock(returnType, type => CreateDiscoveredMockClass(type, compilation))
						: CreateDiscoveredMockClass(returnType, compilation));
					typesToProcess.Enqueue(returnType);
				}
			}

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
					result.Add(new MockClass([httpMessageHandlerType,], compilation));
					typesToProcess.Enqueue(httpMessageHandlerType);
				}
			}
		}

		return result.ToArray();
	}

	private static MockClass CreateDiscoveredMockClass(INamedTypeSymbol type, Compilation compilation)
		=> new([type.WithDynamicAsObject(compilation),], compilation, isDiscovered: true);

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
