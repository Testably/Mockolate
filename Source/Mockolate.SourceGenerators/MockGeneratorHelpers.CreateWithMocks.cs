using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
{
	private const string CreateWithMocksName = "CreateWithMocks";

	private static readonly ConditionalWeakTable<IAssemblySymbol, string[]> s_referencedDataSourceAttributeNames = new();

	internal static bool IsDataSourceCandidate(this SyntaxNode node)
		=> node.IsCreateWithMocksInvocation() || node.IsDataSourceMethodCandidate();

	internal static bool IsCreateWithMocksInvocation(this SyntaxNode node)
		=> node is InvocationExpressionSyntax
		{
			Expression: MemberAccessExpressionSyntax
			{
				Name: IdentifierNameSyntax { Identifier.ValueText: CreateWithMocksName, },
			},
		};

	internal static bool IsDataSourceMethodCandidate(this SyntaxNode node)
		=> node is MethodDeclarationSyntax
		{
			AttributeLists.Count: > 0,
			ParameterList.Parameters.Count: > 0,
		};

	/// <summary>
	///     The attribute names of the method <paramref name="node" />, as the syntax spells them, without binding it.
	/// </summary>
	internal static DataSourceMethodCandidate GetDataSourceMethodCandidate(this SyntaxNode node)
		=> new(new EquatableArray<string>(((MethodDeclarationSyntax)node).AttributeLists
				.SelectMany(list => list.Attributes)
				.Select(attribute => GetSimpleName(attribute.Name))
				.Distinct(StringComparer.Ordinal)
				.ToArray()),
			new CreateMockLocation(node.SyntaxTree.FilePath, node.Span.Start, node.Span.Length));

	private static string GetSimpleName(NameSyntax name)
		=> name switch
		{
			QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
			AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
			SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
			_ => name.ToString(),
		};

	/// <summary>
	///     A class without a base list cannot be an attribute.
	/// </summary>
	internal static bool IsAttributeClassCandidate(this SyntaxNode node)
		=> node is ClassDeclarationSyntax { BaseList: not null, };

	/// <summary>
	///     The name of the class <paramref name="node" />, if it implements <c>Mockolate.IMockDataSource</c>.
	/// </summary>
	internal static string? GetDataSourceAttributeName(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
		=> semanticModel.GetDeclaredSymbol(node, cancellationToken) is INamedTypeSymbol type && IsMockDataSourceType(type)
			? type.Name
			: null;

	/// <summary>
	///     The names of the classes in the referenced assemblies that implement <c>Mockolate.IMockDataSource</c>.
	/// </summary>
	/// <remarks>
	///     Only assemblies that reference Mockolate can declare such a class; their names are cached per assembly,
	///     which later compilations reuse while the references stay the same.
	/// </remarks>
	internal static EquatableArray<string> GetReferencedDataSourceAttributeNames(Compilation compilation)
		=> new(compilation.SourceModule.ReferencedAssemblySymbols
			.Where(assembly => assembly.Modules.Any(module =>
				module.ReferencedAssemblies.Any(identity => identity.Name == "Mockolate")))
			.SelectMany(assembly => s_referencedDataSourceAttributeNames.GetValue(assembly, static referenced =>
			{
				List<string> names = [];
				AddDataSourceAttributeNames(referenced.GlobalNamespace, names);
				return names.ToArray();
			}))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray());

	/// <summary>
	///     The names by which the attribute classes named <paramref name="classNames" /> can be applied, with and
	///     without the <c>Attribute</c> suffix.
	/// </summary>
	internal static EquatableArray<string> GetAttributeUsageNames(IEnumerable<string> classNames)
		=> new(classNames
			.SelectMany(name => name.EndsWith("Attribute", StringComparison.Ordinal)
				? [name, name.Substring(0, name.Length - "Attribute".Length),]
				: new[] { name, })
			.Distinct(StringComparer.Ordinal)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray());

	private static void AddDataSourceAttributeNames(INamespaceOrTypeSymbol container, List<string> names)
	{
		foreach (ISymbol member in container.GetMembers())
		{
			if (member is INamespaceSymbol @namespace)
			{
				AddDataSourceAttributeNames(@namespace, names);
			}
			else if (member is INamedTypeSymbol { TypeKind: TypeKind.Class, } type)
			{
				if (IsMockDataSourceType(type))
				{
					names.Add(type.Name);
				}

				AddDataSourceAttributeNames(type, names);
			}
		}
	}

	/// <summary>
	///     Binds the method at the <paramref name="location" /> of a <see cref="DataSourceMethodCandidate" /> whose
	///     attribute names match a data source attribute.
	/// </summary>
	/// <remarks>
	///     Trees without a file path share the empty one, so every tree with the path is a candidate.
	/// </remarks>
	internal static IEnumerable<DataSourceCreation> ExtractDataSourceCreations(CreateMockLocation location,
		Compilation compilation, CancellationToken cancellationToken)
	{
		TextSpan span = new(location.Start, location.Length);
		foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(tree => tree.FilePath == location.FilePath))
		{
			SyntaxNode root = tree.GetRoot(cancellationToken);
			if (root.FullSpan.Contains(span) &&
			    root.FindNode(span, getInnermostNodeForTie: true) is MethodDeclarationSyntax method &&
			    method.Span == span)
			{
				yield return method.ExtractDataSourceCreation(compilation.GetSemanticModel(tree), cancellationToken);
			}
		}
	}

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
			new EquatableArray<string>(targets.ValueTypes
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
		List<ITypeSymbol> valueTypes = [];
		bool hasCreateWithMocks = false;
		if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation)
		{
			SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
			if (symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.IsEmpty &&
			    GetReceiverType(memberAccess.Expression, semanticModel, cancellationToken) is { } type &&
			    IsConstructibleSut(type, compilation) &&
			    !HasMemberNamedCreateWithMocks(type) &&
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
				         !IsFrameworkNamespace(namedType.ContainingNamespace) &&
				         GetSutConstructor(namedType, compilation) is not null)
				{
					suts.Add(namedType);
				}
				else if (CanBeTypeArgument(parameterType, compilation))
				{
					valueTypes.Add(parameterType);
				}
			}
		}

		List<SutClass> sutClasses = [];
		List<ITypeSymbol> typesToMock = [..mockedTypes,];
		foreach (INamedTypeSymbol sut in suts.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
		{
			// A class that only a data source creates gets no container, so its name cannot clash.
			string? containerName = hasCreateWithMocks ? GetContainerName(sut, compilation) : null;
			if ((hasCreateWithMocks && containerName is null) ||
			    GetSutConstructor(sut, compilation) is not { } constructor)
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
					ToFullName(notAnnotatedType),
					parameter.Name,
					isMockable,
					parameterType.NullableAnnotation == NullableAnnotation.Annotated ||
					parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
					defaultValue,
					isParamsArray,
					!isMockable && defaultValue == "null" &&
					parameterType is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated, },
					GetCallerInfoAttribute(parameter)));
			}

			sutClasses.Add(new SutClass(
				ToFullName(sut),
				sut.ContainingNamespace.IsGlobalNamespace ? null : sut.ContainingNamespace.ToDisplayString(),
				containerName,
				new EquatableArray<SutParameter>(parameters.ToArray()),
				hasCreateWithMocks));
		}

		return new DataSourceTargets(sutClasses, mockedTypes, valueTypes,
			typesToMock.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToList());
	}

	/// <summary>
	///     Whether the generated factory can name the <paramref name="type" /> as a type argument, to create a value of it
	///     from the default values of the behavior.
	/// </summary>
	private static bool CanBeTypeArgument(ITypeSymbol type, Compilation compilation)
		=> type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol or IDynamicTypeSymbol or IErrorTypeSymbol) &&
		   !type.IsRefLikeType &&
		   type.SpecialType != SpecialType.System_Void &&
		   !ContainsTypeParameter(type) &&
		   !UsesTypeNestedInGenericType(type) &&
		   !UsesUnusableType(type) &&
		   Helpers.IsAccessibleFrom(type, compilation.Assembly);

	/// <summary>
	///     The type that the receiver <paramref name="expression" /> names, also when a member of the same name and type
	///     hides it (<c>Svc Svc { get; }</c>), where C# binds the static call to the type.
	/// </summary>
	private static INamedTypeSymbol? GetReceiverType(ExpressionSyntax expression, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		ISymbol? symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
		if (symbol is INamedTypeSymbol type)
		{
			return type;
		}

		ITypeSymbol? memberType = symbol switch
		{
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			ILocalSymbol local => local.Type,
			IParameterSymbol parameter => parameter.Type,
			_ => null,
		};
		return expression is IdentifierNameSyntax identifier &&
		       memberType is INamedTypeSymbol namedType &&
		       namedType.Name == identifier.Identifier.ValueText &&
		       semanticModel.LookupNamespacesAndTypes(expression.SpanStart, name: namedType.Name)
			       .Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, namedType))
			? (INamedTypeSymbol)namedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
			: null;
	}

	/// <summary>
	///     A caller info attribute is copied to the generated parameter, so that it receives the information of the
	///     <c>CreateWithMocks</c> call instead of its default value.
	/// </summary>
	/// <remarks>
	///     The generated method keeps the names of the constructor parameters, so the parameter that a
	///     <c>CallerArgumentExpression</c> refers to exists there as well.
	/// </remarks>
	private static string? GetCallerInfoAttribute(IParameterSymbol parameter)
	{
		foreach (AttributeData attribute in parameter.GetAttributes())
		{
			if (attribute.AttributeClass is not
			    {
				    ContainingNamespace:
				    {
					    Name: "CompilerServices",
					    ContainingNamespace:
					    {
						    Name: "Runtime",
						    ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
					    },
				    },
			    } attributeClass)
			{
				continue;
			}

			switch (attributeClass.Name)
			{
				case "CallerMemberNameAttribute" or "CallerFilePathAttribute" or "CallerLineNumberAttribute":
					return $"global::System.Runtime.CompilerServices.{attributeClass.Name}";
				case "CallerArgumentExpressionAttribute"
					when attribute.ConstructorArguments.Length == 1 &&
					     attribute.ConstructorArguments[0].Value is string parameterName &&
					     parameter.ContainingSymbol is IMethodSymbol constructor &&
					     constructor.Parameters.Any(other => other.Name == parameterName):
					return
						$"global::System.Runtime.CompilerServices.CallerArgumentExpressionAttribute({SymbolDisplay.FormatLiteral(parameterName, true)})";
			}
		}

		return null;
	}

	private static string ToFullName(ITypeSymbol type)
		=> type.ToQualifiedDisplayString(Helpers.TypeDisplayFormat);

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass is { } attributeClass && IsMockDataSourceType(attributeClass);

	private static bool IsMockDataSourceType(INamedTypeSymbol type)
		=> type.AllInterfaces.Any(@interface =>
			@interface.Name == "IMockDataSource" && IsInGlobalMockolateNamespace(@interface));

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
	///     A member of that name would hide the generated <c>CreateWithMocks</c> extension; the factory of a class that
	///     only a test method parameter needs does not use the extension.
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
		// A generic type of the same name does not clash with the non-generic container.
		return @namespace?.GetMembers(name).Any(member =>
			member is INamespaceSymbol or INamedTypeSymbol { Arity: 0, } &&
			compilation.IsSymbolAccessibleWithin(member, compilation.Assembly)) == true
			? null
			: name;
	}

	private sealed record DataSourceTargets(
		List<SutClass> Suts,
		List<ITypeSymbol> MockedTypes,
		List<ITypeSymbol> ValueTypes,
		List<ITypeSymbol> TypesToMock);
}
