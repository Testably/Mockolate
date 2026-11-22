using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
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
		foreach (ITypeSymbol[] types in syntaxNode.ExtractMockedTypes(semanticModel))
		{
			yield return CreateMockClass(semanticModel.Compilation, types);
			if (types.Length == 1 && GetHttpMessageHandler(types[0]) is { } httpMessageHandlerType)
			{
				// When using HttpClient as a mock, we also have to create a mock of the HttpMessageHandler, that can be used as constructor parameter.
				yield return new MockClass([httpMessageHandlerType,], semanticModel.Compilation);
			}

			foreach (ITypeSymbol adapterReturnType in types.SelectMany(GetAdapterMockedReturnTypes).Distinct<ITypeSymbol>(SymbolEqualityComparer.Default))
			{
				yield return new MockClass([adapterReturnType,], semanticModel.Compilation);
			}
		}
	}

	/// <summary>
	///     The types of the mocks that a <c>CreateMock</c> call or an <c>Implementing&lt;T&gt;</c> chain creates.
	/// </summary>
	internal static IEnumerable<ITypeSymbol[]> ExtractMockedTypes(this SyntaxNode syntaxNode,
		SemanticModel semanticModel)
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
					yield return [type,];
				}
				else if (IsMockable(methodSymbol.ReturnType))
				{
					yield return [methodSymbol.ReturnType,];
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
					yield return [rootType!, ..collectedTypes,];
				}
			}
		}
	}

	private static MockClass CreateMockClass(Compilation compilation, params ITypeSymbol[] types)
		=> new(Array.ConvertAll(types, type => type.WithDynamicAsObject(compilation)), compilation);

	/// <summary>
	///     Whether an un-configured member returning <paramref name="typeSymbol" /> can return an auto-mock, apart from
	///     its namespace (see <see cref="AutoMockingOptions" />).
	/// </summary>
	/// <remarks>
	///     A type that still contains a type parameter cannot be named in the auto-mock factory. Every type the
	///     generator sees runs this check, so it stays cheap; a member that returns an interface with static abstract
	///     members does not compile anyway (CS8920).
	/// </remarks>
	internal static bool IsAutoMockable(ITypeSymbol typeSymbol)
		=> typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Interface, } namedTypeSymbol &&
		   IsMockable(namedTypeSymbol) &&
		   !ContainsTypeParameter(namedTypeSymbol) &&
		   !HasUsageDiagnostic(namedTypeSymbol);

	/// <summary>
	///     The namespace of <paramref name="typeSymbol" /> when it lies in a framework namespace (see
	///     <see cref="IsFrameworkNamespace" />); otherwise <see langword="null" />.
	/// </summary>
	internal static string? GetFrameworkNamespace(ITypeSymbol typeSymbol)
		=> IsFrameworkNamespace(typeSymbol.ContainingNamespace)
			? typeSymbol.ContainingNamespace.ToDisplayString()
			: null;

	/// <summary>
	///     Whether the generator can create a mock of <paramref name="typeSymbol" /> for auto-mocking, which unlike a
	///     type in a <c>CreateMock</c> call is not checked by the <c>MockabilityAnalyzer</c>.
	/// </summary>
	/// <remarks>
	///     The type must be accessible from the compilation, must not expand itself (e.g. <c>INode&lt;T&gt;</c>
	///     returning <c>INode&lt;INode&lt;T&gt;&gt;</c>, which would never end) or nest more than
	///     <see cref="MaxTypeArgumentDepth" /> type arguments, which bounds how many generic combinations the walk
	///     creates, and its namespace must not be excluded by the <paramref name="options" />. The mock must be able to
	///     implement every abstract member, and no member the mock implements may be obsolete or use a type that cannot
	///     be a type argument, so that the generated mock compiles without warnings.
	/// </remarks>
	private static bool CanBeDiscovered(INamedTypeSymbol typeSymbol, IAssemblySymbol sourceAssembly,
		AutoMockingOptions options)
		=> CanDiscoverNamespace(typeSymbol, options) && CanBeMockedImplicitly(typeSymbol, sourceAssembly);

	/// <summary>
	///     Whether the generator can create a compilable mock of <paramref name="typeSymbol" /> without a
	///     <c>CreateMock</c> call that the <c>MockabilityAnalyzer</c> checks.
	/// </summary>
	private static bool CanBeMockedImplicitly(INamedTypeSymbol typeSymbol, IAssemblySymbol sourceAssembly)
		=> IsAutoMockable(typeSymbol) &&
		   !UsesTypeNestedInGenericType(typeSymbol) &&
		   !HasStaticAbstractMembers(typeSymbol) &&
		   !HasNonPublicAbstractMembers(typeSymbol) &&
		   !HasMemberNamedCreateMock(typeSymbol) &&
		   Helpers.IsAccessibleFrom(typeSymbol, sourceAssembly) &&
		   GetTypeArgumentDepth(typeSymbol) <= MaxTypeArgumentDepth &&
		   !RepeatsGenericDefinition(typeSymbol, []) &&
		   GetImplementedMembers(typeSymbol, sourceAssembly).All(member =>
			   !HasUsageDiagnostic(member) && !HasRefReadOnlyRefStructParameter(member) && !HasUnsupportedShape(member) &&
			   GetSignatureTypes(member).All(type => !UsesUnusableType(type)));

	private const int MaxTypeArgumentDepth = 5;

	private static bool CanDiscoverNamespace(INamedTypeSymbol typeSymbol, AutoMockingOptions options)
		=> options.CanDiscover(typeSymbol.ContainingNamespace.ToDisplayString(),
			IsFrameworkNamespace(typeSymbol.ContainingNamespace));

	/// <summary>
	///     The auto-mock factory calls <c>CreateMock</c> on the type, where a member of the interface with that name
	///     would hide the generated extension.
	/// </summary>
	private static bool HasMemberNamedCreateMock(INamedTypeSymbol typeSymbol)
		=> !typeSymbol.GetMembers("CreateMock").IsEmpty ||
		   typeSymbol.AllInterfaces.Any(@interface => !@interface.GetMembers("CreateMock").IsEmpty);

	/// <summary>
	///     Whether the <paramref name="namespaceSymbol" /> lies in <c>System</c>, <c>Microsoft</c> or <c>Windows</c>, whose
	///     interfaces are only auto-mocked when their namespace is included, because mocking them transitively would
	///     generate mocks for large parts of the base class library.
	/// </summary>
	internal static bool IsFrameworkNamespace(INamespaceSymbol? namespaceSymbol)
	{
		while (namespaceSymbol is { ContainingNamespace.IsGlobalNamespace: false, })
		{
			namespaceSymbol = namespaceSymbol.ContainingNamespace;
		}

		return namespaceSymbol?.Name is "System" or "Microsoft" or "Windows";
	}

	/// <summary>
	///     Whether <paramref name="typeSymbol" /> or one of its type arguments is nested in a generic type, e.g.
	///     <c>Outer&lt;T&gt;.IInner</c>, whose type arguments the checks for self-expanding generics and the
	///     normalization of nullable spellings do not see.
	/// </summary>
	private static bool UsesTypeNestedInGenericType(ITypeSymbol typeSymbol)
	{
		switch (typeSymbol)
		{
			case IArrayTypeSymbol array:
				return UsesTypeNestedInGenericType(array.ElementType);
			case INamedTypeSymbol named:
				for (INamedTypeSymbol? containingType = named.ContainingType;
				     containingType is not null;
				     containingType = containingType.ContainingType)
				{
					if (containingType.IsGenericType)
					{
						return true;
					}
				}

				return named.TypeArguments.Any(UsesTypeNestedInGenericType);
			default:
				return false;
		}
	}

	private static int GetTypeArgumentDepth(ITypeSymbol typeSymbol)
		=> typeSymbol switch
		{
			IArrayTypeSymbol array => GetTypeArgumentDepth(array.ElementType),
			INamedTypeSymbol { TypeArguments.Length: > 0, } named => 1 + named.TypeArguments.Max(GetTypeArgumentDepth),
			_ => 0,
		};

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

	/// <summary>
	///     Whether using <paramref name="symbol" /> reports a diagnostic in the consuming project: it is obsolete,
	///     experimental or a preview feature.
	/// </summary>
	private static bool HasUsageDiagnostic(ISymbol symbol)
		=> symbol.GetAttributes().Any(attribute => attribute.AttributeClass is
		   {
			   Name: "ObsoleteAttribute",
			   ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
		   } or
		   {
			   Name: "ExperimentalAttribute",
			   ContainingNamespace:
			   {
				   Name: "CodeAnalysis",
				   ContainingNamespace:
				   {
					   Name: "Diagnostics", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
				   },
			   },
		   } or
		   {
			   Name: "RequiresPreviewFeaturesAttribute",
			   ContainingNamespace:
			   {
				   Name: "Versioning",
				   ContainingNamespace:
				   {
					   Name: "Runtime", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
				   },
			   },
		   }) ||
		   (symbol.ContainingType is { } containingType && HasUsageDiagnostic(containingType));

	private static bool HasStaticAbstractMembers(INamedTypeSymbol typeSymbol)
		=> typeSymbol.GetMembers().Concat(typeSymbol.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

	/// <summary>
	///     Whether the interface <paramref name="typeSymbol" /> has an abstract member that is not public, which the
	///     generated mock cannot implement (CS0737).
	/// </summary>
	private static bool HasNonPublicAbstractMembers(INamedTypeSymbol typeSymbol)
		=> typeSymbol.GetMembers().Concat(typeSymbol.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Any(member => member is { IsStatic: false, IsAbstract: true, } &&
			               member.DeclaredAccessibility != Accessibility.Public);

	/// <summary>
	///     Whether the generator emits no compilable mock for <paramref name="member" />: a property or indexer that
	///     returns by reference (CS8152) or a method with a type parameter that allows ref structs (CS9244).
	/// </summary>
	private static bool HasUnsupportedShape(ISymbol member)
		=> member switch
		{
			IPropertySymbol property => property.ReturnsByRef || property.ReturnsByRefReadonly,
			IMethodSymbol method => method.TypeParameters.Any(typeParameter => typeParameter.AllowsRefLikeType),
			_ => false,
		};

	/// <summary>
	///     Whether a member has a <see langword="ref readonly" /> parameter of a ref struct type, which needs the
	///     ref-struct pipeline even for spans and so does not compile on every target framework.
	/// </summary>
	private static bool HasRefReadOnlyRefStructParameter(ISymbol member)
	{
		ImmutableArray<IParameterSymbol> parameters = member switch
		{
			IMethodSymbol method => method.Parameters,
			IPropertySymbol property => property.Parameters,
			_ => ImmutableArray<IParameterSymbol>.Empty,
		};
		return parameters.Any(parameter => parameter is { RefKind: RefKind.RefReadOnlyParameter, Type.IsRefLikeType: true, });
	}

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
			INamedTypeSymbol named => HasUsageDiagnostic(named) ||
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
			IMethodSymbol method =>
			[
				method.ReturnType,
				..method.Parameters.Select(parameter => parameter.Type),
				..method.TypeParameters.SelectMany(typeParameter => typeParameter.ConstraintTypes),
			],
			IPropertySymbol property => [property.Type, ..property.Parameters.Select(parameter => parameter.Type),],
			IEventSymbol @event => [@event.Type,],
			_ => [],
		};

	/// <summary>
	///     The members that a mock of <paramref name="typeSymbol" /> implements or overrides.
	/// </summary>
	/// <remarks>
	///     A class mock only implements the interface members that the class leaves unimplemented; the others it
	///     overrides through the class members, if at all.
	/// </remarks>
	private static IEnumerable<ISymbol> GetImplementedMembers(ITypeSymbol typeSymbol, IAssemblySymbol sourceAssembly)
	{
		bool isClass = typeSymbol.TypeKind != TypeKind.Interface;
		IEnumerable<ITypeSymbol> types = [typeSymbol, ..typeSymbol.AllInterfaces,];
		for (ITypeSymbol? baseType = typeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			types = types.Append(baseType);
		}

		return types.SelectMany(type => type.GetMembers())
			.Where(member => member is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke, } ||
			                 (member is { IsStatic: false, IsSealed: false, } &&
			                  (member.IsAbstract || member.IsVirtual || member.IsOverride) &&
			                  !(isClass && member.ContainingType.TypeKind == TypeKind.Interface &&
			                    typeSymbol.FindImplementationForInterfaceMember(member) is not null) &&
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
		IAssemblySymbol sourceAssembly, AutoMockingOptions options, AutoMockDependencies? dependencies,
		CancellationToken cancellationToken)
	{
		foreach (ISymbol member in GetImplementedMembers(typeSymbol, sourceAssembly))
		{
			ITypeSymbol? returnType = member switch
			{
				IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.DelegateInvoke, ReturnsVoid: false, } method => method.ReturnType,
				IPropertySymbol { GetMethod: not null, } property => property.Type,
				_ => null,
			};
			if (returnType is null || UnwrapTask(returnType) is not INamedTypeSymbol candidate)
			{
				continue;
			}

			// Whether a candidate is discovered depends on its members, unless its own declaration rejects it.
			if (IsAutoMockable(candidate) && CanDiscoverNamespace(candidate, options))
			{
				dependencies?.Add(candidate, true, cancellationToken);
			}

			if (CanBeDiscovered(candidate, sourceAssembly, options))
			{
				yield return candidate;
			}
		}
	}

	private static ITypeSymbol? GetHttpMessageHandler(ITypeSymbol type)
		=> type.Name == "HttpClient" &&
		   type.ContainingNamespace is { ContainingNamespace: { ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true, ContainingNamespace.Name: "System", Name: "Net", }, Name: "Http", }
			? type.GetMembers()
				.OfType<IMethodSymbol>()
				.Where(m => m.MethodKind == MethodKind.Constructor)
				.SelectMany(c => c.Parameters)
				.Select(p => p.Type)
				.First(t => t.Name == "HttpMessageHandler")
			: null;

	/// <summary>
	///     The return types of the members of the adapted interfaces of the <paramref name="type" /> whose hooks create
	///     mocks of them, e.g. the logger mocks of a logger factory mock, so that these types are mocked as well.
	/// </summary>
	private static IEnumerable<ITypeSymbol> GetAdapterMockedReturnTypes(ITypeSymbol type)
	{
		foreach (MockAdapter adapter in MockAdapter.All)
		{
			if (adapter.FindTarget(type) is not { } target)
			{
				continue;
			}

			foreach (string memberName in adapter.MockedReturnTypesOf)
			{
				if (target.GetMembers(memberName).OfType<IMethodSymbol>().FirstOrDefault() is { } member)
				{
					yield return member.ReturnType;
				}
			}
		}
	}

	/// <summary>
	///     The mocks for auto-mocking: of the interfaces that the members of the <paramref name="mockedTypes" /> return,
	///     transitively.
	/// </summary>
	/// <remarks>
	///     Each interface is mocked in one spelling without nullable annotations and tuple element names, so that the
	///     result does not depend on the order of the files, and the model of each interface is built once per
	///     compilation. Only a type that is mocked on its own has a mock already; a type that a mock only implements in addition
	///     (<c>Implementing&lt;T&gt;</c>) still needs an auto-mock when a member returns it.
	/// </remarks>
	/// <param name="mockedTypes">The types of the mocks that the <c>CreateMock</c> calls create.</param>
	/// <param name="compilation">The current compilation.</param>
	/// <param name="options">The namespaces whose interfaces get a mock.</param>
	/// <param name="dependencies">Collects the names that the result depends on, if not <see langword="null" />.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	internal static MockClass[] DiscoverAutoMocks(IReadOnlyCollection<ITypeSymbol[]> mockedTypes,
		Compilation compilation, AutoMockingOptions options, AutoMockDependencies? dependencies,
		CancellationToken cancellationToken)
	{
		IAssemblySymbol sourceAssembly = compilation.Assembly;
		List<MockClass> result = [];
		Queue<ITypeSymbol> typesToProcess = new(mockedTypes.SelectMany(types => types));
		HashSet<ITypeSymbol> processedTypes = new(mockedTypes.Where(types => types.Length == 1)
			.Select(types => Normalize(types[0], compilation)), SymbolEqualityComparer.Default);

		while (typesToProcess.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ITypeSymbol currentType = typesToProcess.Dequeue();
			dependencies?.Add(currentType, true, cancellationToken);
			foreach (INamedTypeSymbol returnType in GetAutoMockableReturnTypes(currentType, sourceAssembly,
				         options, dependencies, cancellationToken))
			{
				if (Normalize(returnType, compilation) is INamedTypeSymbol normalized && processedTypes.Add(normalized))
				{
					result.Add(EntityCache.Current is { } cache
						? cache.GetOrAddDiscoveredMock(normalized, type => CreateDiscoveredMockClass(type, compilation))
						: CreateDiscoveredMockClass(normalized, compilation));
					typesToProcess.Enqueue(normalized);
					foreach (INamedTypeSymbol adapterReturnType in GetAdapterMockedReturnTypes(normalized).OfType<INamedTypeSymbol>())
					{
						if (processedTypes.Add(adapterReturnType))
						{
							result.Add(CreateDiscoveredMockClass(adapterReturnType, compilation));
						}
					}
				}
			}
		}

		return result.ToArray();
	}

	private static MockClass CreateDiscoveredMockClass(INamedTypeSymbol type, Compilation compilation)
		=> new([type.WithDynamicAsObject(compilation),], compilation, isDiscovered: true);

	/// <summary>
	///     The <paramref name="type" /> without nullable reference annotations and tuple element names, also in its
	///     type arguments.
	/// </summary>
	private static ITypeSymbol Normalize(ITypeSymbol type, Compilation compilation)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				return compilation.CreateArrayTypeSymbol(Normalize(array.ElementType, compilation), array.Rank);
			case INamedTypeSymbol { IsTupleType: true, TupleUnderlyingType: { } underlying, }:
				return Normalize(underlying, compilation);
			case INamedTypeSymbol { IsGenericType: true, } named when named.ContainingType is not { IsGenericType: true, }:
				return named.OriginalDefinition.Construct(
					named.TypeArguments.Select(argument => Normalize(argument, compilation)).ToImmutableArray(),
					named.TypeArguments.Select(_ => NullableAnnotation.NotAnnotated).ToImmutableArray());
			default:
				return type.IsReferenceType ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type;
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
