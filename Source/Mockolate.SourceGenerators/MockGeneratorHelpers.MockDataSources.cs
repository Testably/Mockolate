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
	private static readonly ConditionalWeakTable<IAssemblySymbol, string[]> s_referencedDataSourceAttributeNames = new();

	/// <summary>
	///     A method or constructor with parameters, or a class, record or struct, whose attributes can fill parameters.
	/// </summary>
	internal static bool IsDataSourceMemberCandidate(this SyntaxNode node)
		=> node switch
		{
			MethodDeclarationSyntax or ConstructorDeclarationSyntax => node is BaseMethodDeclarationSyntax
			{
				AttributeLists.Count: > 0, ParameterList.Parameters.Count: > 0,
			},
			ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax => node is TypeDeclarationSyntax
			{
				AttributeLists.Count: > 0,
			},
			_ => false,
		};

	/// <summary>
	///     The attribute names of the member <paramref name="node" />, as the syntax spells them, without binding it.
	/// </summary>
	internal static DataSourceMemberCandidate GetDataSourceMemberCandidate(this SyntaxNode node)
		=> new(new EquatableArray<string>(((MemberDeclarationSyntax)node).AttributeLists
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

	internal static bool IsUsingAliasCandidate(this SyntaxNode node)
		=> node is UsingDirectiveSyntax { Alias: not null, NamespaceOrType: NameSyntax, };

	/// <summary>
	///     The name of the alias that the using directive <paramref name="node" /> declares, and the simple name of the
	///     type it stands for.
	/// </summary>
	internal static (string Alias, string Target) GetUsingAlias(this SyntaxNode node)
	{
		UsingDirectiveSyntax usingDirective = (UsingDirectiveSyntax)node;
		return (usingDirective.Alias!.Name.Identifier.ValueText,
			GetSimpleName((NameSyntax)usingDirective.NamespaceOrType));
	}

	/// <summary>
	///     A class without a base list cannot be an attribute.
	/// </summary>
	internal static bool IsAttributeClassCandidate(this SyntaxNode node)
		=> node is ClassDeclarationSyntax { BaseList: not null, };

	/// <summary>
	///     The name of the class <paramref name="node" />, if it implements <c>Mockolate.Internals.IMockDataSource</c>.
	/// </summary>
	internal static string? GetDataSourceAttributeName(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
		=> semanticModel.GetDeclaredSymbol(node, cancellationToken) is INamedTypeSymbol type && IsMockDataSourceType(type)
			? type.Name
			: null;

	/// <summary>
	///     The names of the classes in the referenced assemblies that implement <c>Mockolate.Internals.IMockDataSource</c>.
	/// </summary>
	/// <remarks>
	///     Only assemblies that reference Mockolate, directly or through another referenced assembly that derives such a
	///     class, can declare one; their names are cached per assembly, which later compilations reuse while the
	///     references stay the same.
	/// </remarks>
	internal static EquatableArray<string> GetReferencedDataSourceAttributeNames(Compilation compilation)
		=> new(GetAssembliesReferencingMockolate(compilation.SourceModule.ReferencedAssemblySymbols)
			.SelectMany(assembly => s_referencedDataSourceAttributeNames.GetValue(assembly, static referenced =>
			{
				List<string> names = [];
				AddDataSourceAttributeNames(referenced.GlobalNamespace, names);
				return names.ToArray();
			}))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray());

	private static List<IAssemblySymbol> GetAssembliesReferencingMockolate(IEnumerable<IAssemblySymbol> assemblies)
	{
		HashSet<string> reachingMockolate = new(StringComparer.Ordinal) { "Mockolate", };
		List<IAssemblySymbol> remaining = assemblies.ToList();
		List<IAssemblySymbol> result = [];
		bool added = true;
		while (added)
		{
			added = false;
			for (int index = remaining.Count - 1; index >= 0; index--)
			{
				IAssemblySymbol assembly = remaining[index];
				if (assembly.Modules.Any(module =>
					    module.ReferencedAssemblies.Any(identity => reachingMockolate.Contains(identity.Name))))
				{
					reachingMockolate.Add(assembly.Identity.Name);
					result.Add(assembly);
					remaining.RemoveAt(index);
					added = true;
				}
			}
		}

		return result;
	}

	/// <summary>
	///     The names by which the attribute classes named <paramref name="classNames" /> can be applied, directly or
	///     through one of the <paramref name="aliases" />, with and without the <c>Attribute</c> suffix.
	/// </summary>
	internal static EquatableArray<string> GetAttributeUsageNames(IEnumerable<string> classNames,
		IEnumerable<(string Alias, string Target)> aliases)
	{
		HashSet<string> names = new(classNames, StringComparer.Ordinal);
		names.UnionWith(aliases.Where(alias => names.Contains(alias.Target)).Select(alias => alias.Alias).ToArray());
		return new EquatableArray<string>(names
			.SelectMany(name => name.EndsWith("Attribute", StringComparison.Ordinal)
				? [name, name.Substring(0, name.Length - "Attribute".Length),]
				: new[] { name, })
			.Distinct(StringComparer.Ordinal)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray());
	}

	private static void AddDataSourceAttributeNames(INamespaceOrTypeSymbol container, List<string> names)
	{
		foreach (ISymbol member in container.GetMembers())
		{
			if (member is INamespaceSymbol @namespace)
			{
				AddDataSourceAttributeNames(@namespace, names);
			}
			else if (member is INamedTypeSymbol type)
			{
				if (type.TypeKind == TypeKind.Class && IsMockDataSourceType(type))
				{
					names.Add(type.Name);
				}

				AddDataSourceAttributeNames(type, names);
			}
		}
	}

	/// <summary>
	///     Binds the member at the <paramref name="location" /> of a <see cref="DataSourceMemberCandidate" /> whose
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
			    root.FindNode(span, getInnermostNodeForTie: true) is { } node &&
			    node.Span == span && node.IsDataSourceMemberCandidate())
			{
				yield return node.ExtractDataSourceCreation(compilation.GetSemanticModel(tree), cancellationToken);
			}
		}
	}

	/// <summary>
	///     The parameter types of a member with an attribute that implements <c>Mockolate.Internals.IMockDataSource</c>.
	/// </summary>
	internal static DataSourceCreation ExtractDataSourceCreation(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		DataSourceTargets targets = GetDataSourceTargets(node, semanticModel, cancellationToken);
		return new DataSourceCreation(
			new EquatableArray<DataSourceClass>(targets.Classes.ToArray()),
			ToFullNames(targets.MockedTypes, compilation),
			ToFullNames(targets.ValueOrMockTypes, compilation),
			ToFullNames(targets.ValueTypes, compilation),
			new EquatableArray<MockClass>(targets.TypesToMock
				.SelectMany(type => CreateDependencyMockClasses(type, compilation))
				.ToArray()),
			new CreateMockLocation(node.SyntaxTree.FilePath, node.Span.Start, node.Span.Length));

		static EquatableArray<string> ToFullNames(List<ITypeSymbol> types, Compilation compilation)
			=> new(types
				.Select(type => ToFullName(Normalize(type, compilation)))
				.Distinct()
				.ToArray());
	}

	/// <summary>
	///     The types that the <c>IMockDataSource</c> member <paramref name="node" /> mocks, as roots for the discovery of
	///     auto-mocks.
	/// </summary>
	internal static IEnumerable<ITypeSymbol[]> ExtractDataSourceMockedTypes(this SyntaxNode node,
		SemanticModel semanticModel, CancellationToken cancellationToken)
		=> GetDataSourceTargets(node, semanticModel, cancellationToken).TypesToMock.Select(type => new[] { type, });

	/// <summary>
	///     The parameters that the <c>IMockDataSource</c> attributes of the member <paramref name="symbol" /> fill: those
	///     of a method or constructor, or those of the declared constructors of a type.
	/// </summary>
	/// <remarks>
	///     A <c>[method: …]</c> attribute on a type declaration belongs to its primary constructor.
	/// </remarks>
	internal static IEnumerable<IParameterSymbol> GetDataSourceParameters(ISymbol? symbol)
		=> symbol switch
		{
			IMethodSymbol method when method.GetAttributes().Any(IsMockDataSourceAttribute) => method.Parameters,
			INamedTypeSymbol type => type.InstanceConstructors
				.Where(constructor => !constructor.IsImplicitlyDeclared &&
				                      (type.GetAttributes().Any(IsMockDataSourceAttribute) ||
				                       constructor.GetAttributes().Any(IsMockDataSourceAttribute)))
				.SelectMany(constructor => constructor.Parameters),
			_ => [],
		};

	private static DataSourceTargets GetDataSourceTargets(SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		DataSourceTargets targets = new([], [], [], [], []);
		List<INamedTypeSymbol> pending = [];
		foreach (IParameterSymbol parameter in
		         GetDataSourceParameters(semanticModel.GetDeclaredSymbol(node, cancellationToken)))
		{
			ITypeSymbol parameterType = parameter.Type.WithDynamicAsObject(compilation)
				.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
			switch (GetDataSourceParameterKind(parameterType, compilation))
			{
				case DataSourceParameterKind.Mock when parameterType is INamedTypeSymbol { TypeKind: TypeKind.Class, } mockedClass:
					pending.Add(mockedClass);
					break;
				case DataSourceParameterKind.Mock:
					targets.MockedTypes.Add(parameterType);
					targets.TypesToMock.Add(parameterType);
					targets.TypesToMock.AddRange(GetConstructorDependencyMockTypes(parameterType, compilation));
					break;
				case DataSourceParameterKind.ValueOrMock:
					targets.ValueOrMockTypes.Add(parameterType);
					targets.TypesToMock.Add(parameterType);
					break;
				case DataSourceParameterKind.Create:
					pending.Add((INamedTypeSymbol)parameterType);
					break;
				case DataSourceParameterKind.Value:
					targets.ValueTypes.Add(parameterType);
					break;
			}
		}

		HashSet<INamedTypeSymbol> visited = new(SymbolEqualityComparer.Default);
		for (int index = 0; index < pending.Count; index++)
		{
			INamedTypeSymbol type = pending[index];
			if (!visited.Add(type))
			{
				continue;
			}

			IMethodSymbol? constructor = GetDataSourceConstructor(type, compilation);
			if (constructor is null && !type.IsAbstract)
			{
				continue;
			}

			DataSourceParameter[] parameters = constructor is null
				? []
				: constructor.Parameters
					.Select(parameter => CreateDataSourceParameter(parameter, compilation, targets.TypesToMock, pending))
					.ToArray();
			bool isMock = type.IsAbstract || IsClassMockable(type, constructor!, compilation);
			if (isMock)
			{
				targets.TypesToMock.Add(type);
				targets.TypesToMock.AddRange(GetConstructorDependencyMockTypes(type, compilation));
			}

			targets.Classes.Add(new DataSourceClass(ToFullName(type),
				new EquatableArray<DataSourceParameter>(parameters), isMock));
		}

		return targets with
		{
			TypesToMock = targets.TypesToMock.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default).ToList(),
		};
	}

	/// <summary>
	///     Whether the factory creates the <paramref name="type" /> as a class mock through the
	///     <paramref name="constructor" />: its mock is emitted and compiles, and the object array of constructor
	///     parameters reaches no other constructor of the mock.
	/// </summary>
	private static bool IsClassMockable(INamedTypeSymbol type, IMethodSymbol constructor, Compilation compilation)
	{
		if (GetCompilableDataSourceMock(type, compilation) is null)
		{
			return false;
		}

		int count = constructor.Parameters.Length;
		IMethodSymbol[] reachable = GetMockConstructors(type, compilation.Assembly)
			.Where(candidate => !candidate.Parameters.Any(parameter => parameter.Type.IsRefLikeType) &&
			                    candidate.Parameters.Count(parameter => !parameter.HasExplicitDefaultValue) <= count &&
			                    count <= candidate.Parameters.Length)
			.ToArray();
		return reachable.Length == 1 && SymbolEqualityComparer.Default.Equals(reachable[0], constructor);
	}

	/// <summary>
	///     The constructor whose parameters the factory fills: the one of a concrete class, or the one that the mock of an
	///     abstract class fills with dependencies. Without one, the factory calls <c>CreateMock</c> without arguments.
	/// </summary>
	private static IMethodSymbol? GetDataSourceConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		if (!type.IsAbstract)
		{
			return GetSutConstructor(type, compilation);
		}

		return GetCompilableDataSourceMock(type, compilation)?.ConstructorWithDependencies is { } index &&
		       GetMockConstructors(type, compilation.Assembly)[index] is { } constructor &&
		       IsClassMockable(type, constructor, compilation)
			? constructor
			: null;
	}

	private static DataSourceParameter CreateDataSourceParameter(IParameterSymbol parameter, Compilation compilation,
		List<ITypeSymbol> typesToMock, List<INamedTypeSymbol> pending)
	{
		ITypeSymbol parameterType = parameter.Type.WithDynamicAsObject(compilation)
			.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
		string? defaultValue = parameter.HasExplicitDefaultValue
			? MethodParameter.From(parameter).ExplicitDefaultValue
			: null;
		bool isMockable = !parameter.IsParams && IsDependencyMockable(parameterType, compilation);
		// A framework interface such as IEnumerable<T> is better served by the default values of the behavior,
		// so its mock is only the last resort.
		bool isMockedLast = !isMockable && !parameter.IsParams &&
		                    IsMockableFrameworkInterface(parameterType, compilation);
		// An explicit default value wins over a created class, as it does over null for a nullable parameter.
		bool isCreatable = !isMockable && !parameter.IsParams && defaultValue is null &&
		                   parameterType is INamedTypeSymbol namedType &&
		                   IsCreatableDependency(namedType, compilation);
		if (isMockable || isMockedLast)
		{
			typesToMock.Add(parameterType);
		}
		else if (isCreatable)
		{
			pending.Add((INamedTypeSymbol)parameterType);
		}

		return new DataSourceParameter(
			ToFullName(parameterType),
			parameter.Name,
			isMockable,
			isMockedLast,
			parameter.IsParams,
			defaultValue,
			isCreatable,
			// A nullable-oblivious parameter does not state that it accepts null, so it is not given null.
			parameter.Type.NullableAnnotation == NullableAnnotation.Annotated ||
			parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
	}

	/// <summary>
	///     A <c>T.CreateMock(out ...)</c> call, the overload for classes that cannot be mocked. It is the only overload
	///     with an <see langword="out" /> parameter, which a named argument can pass at any position.
	/// </summary>
	internal static bool IsSealedCreateMockInvocation(this SyntaxNode node)
		=> node is InvocationExpressionSyntax
		   {
			   Expression: MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "CreateMock", }, },
			   ArgumentList.Arguments: { Count: > 0, } arguments,
		   } &&
		   arguments.Any(argument => argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

	/// <summary>
	///     The sealed class or record of the <c>T.CreateMock(out ...)</c> call <paramref name="node" />.
	/// </summary>
	internal static SealedCreation ExtractSealedCreation(this SyntaxNode node, SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		List<ITypeSymbol> typesToMock = [];
		DataSourceClass? sealedClass = GetSealedClass(node, semanticModel, typesToMock, cancellationToken);
		return new SealedCreation(sealedClass,
			new EquatableArray<MockClass>(typesToMock
				.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
				.SelectMany(type => CreateDependencyMockClasses(type, compilation))
				.ToArray()),
			new CreateMockLocation(node.SyntaxTree.FilePath, node.Span.Start, node.Span.Length));
	}

	/// <summary>
	///     The dependencies that the <c>T.CreateMock(out ...)</c> call <paramref name="node" /> mocks, as roots for the
	///     discovery of auto-mocks.
	/// </summary>
	internal static IEnumerable<ITypeSymbol[]> ExtractSealedMockedTypes(this SyntaxNode node,
		SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		List<ITypeSymbol> typesToMock = [];
		GetSealedClass(node, semanticModel, typesToMock, cancellationToken);
		return typesToMock.Select(type => new[] { type, });
	}

	private static DataSourceClass? GetSealedClass(SyntaxNode node, SemanticModel semanticModel,
		List<ITypeSymbol> typesToMock, CancellationToken cancellationToken)
	{
		Compilation compilation = semanticModel.Compilation;
		if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } ||
		    semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol is not INamedTypeSymbol type ||
		    !(type.IsSealed || type.IsRecord) ||
		    !IsConstructibleSut(type, compilation) ||
		    IsFrameworkNamespace(type.ContainingNamespace) ||
		    HasMemberNamedCreateMock(type) ||
		    GetSutConstructor(type, compilation) is not { } constructor)
		{
			return null;
		}

		// A sealed class only fills its own constructor: a concrete dependency is no mock and gets a default value.
		List<INamedTypeSymbol> ignored = [];
		return new DataSourceClass(ToFullName(type), new EquatableArray<DataSourceParameter>(constructor.Parameters
			.Select(parameter => CreateDataSourceParameter(parameter, compilation, typesToMock, ignored))
			.ToArray()), false);
	}

	private static bool IsCreatableDependency(INamedTypeSymbol type, Compilation compilation)
		=> !IsFrameworkNamespace(type.ContainingNamespace) &&
		   IsConstructibleSut(type, compilation) &&
		   GetSutConstructor(type, compilation) is not null;

	private static bool IsMockableFrameworkInterface(ITypeSymbol type, Compilation compilation)
		=> type is INamedTypeSymbol { TypeKind: TypeKind.Interface, } namedType &&
		   IsFrameworkNamespace(namedType.ContainingNamespace) &&
		   CanBeMockedImplicitly(namedType, compilation.Assembly);

	/// <summary>
	///     How a parameter of an <c>IMockDataSource</c> member is filled.
	/// </summary>
	/// <remarks>
	///     A parameter is an explicit request for its type, so an interface, delegate or abstract class is mocked like by
	///     a <c>CreateMock</c> call, and the <c>MockDataSourceAnalyzer</c> reports what cannot be created. Framework types
	///     are often the arguments that the test framework passes, so they get a value from the default values of the
	///     behavior, and only their interfaces that can be mocked without these checks get a mock when there is none.
	/// </remarks>
	private static DataSourceParameterKind GetDataSourceParameterKind(ITypeSymbol type, Compilation compilation)
	{
		if (!CanBeTypeArgument(type, compilation))
		{
			return DataSourceParameterKind.None;
		}

		if (type is not INamedTypeSymbol
		    {
			    TypeKind: TypeKind.Interface or TypeKind.Delegate or TypeKind.Class, IsStatic: false,
		    } namedType)
		{
			return DataSourceParameterKind.Value;
		}

		if (IsFrameworkNamespace(namedType.ContainingNamespace))
		{
			return IsMockableFrameworkInterface(namedType, compilation)
				? DataSourceParameterKind.ValueOrMock
				: DataSourceParameterKind.Value;
		}

		if (namedType.TypeKind != TypeKind.Class || namedType.IsAbstract)
		{
			return CanMockDataSourceParameter(namedType, compilation)
				? DataSourceParameterKind.Mock
				: DataSourceParameterKind.Value;
		}

		return IsConstructibleSut(namedType, compilation) && GetSutConstructor(namedType, compilation) is not null
			? DataSourceParameterKind.Create
			: DataSourceParameterKind.Value;
	}

	/// <summary>
	///     Whether the factory can create the parameter <paramref name="type" /> with <c>CreateMock()</c>.
	/// </summary>
	private static bool CanMockDataSourceParameter(INamedTypeSymbol type, Compilation compilation)
		=> GetCompilableDataSourceMock(type, compilation) is { } mock &&
		   (type.TypeKind != TypeKind.Class || HasCallableMockConstructor(type, mock, compilation));

	/// <summary>
	///     Whether <c>CreateMock()</c> calls a constructor of the class <paramref name="type" /> that is not obsolete: one
	///     without arguments, or else the one that it fills with dependencies.
	/// </summary>
	private static bool HasCallableMockConstructor(INamedTypeSymbol type, MockClass mock, Compilation compilation)
	{
		int? constructor = mock.ConstructorWithoutArguments ?? mock.ConstructorWithDependencies;
		return constructor is { } index &&
		       !HasUsageDiagnostic(GetMockConstructors(type, compilation.Assembly)[index]);
	}

	/// <summary>
	///     The mock of the <paramref name="type" />, or <see langword="null" /> when it is not emitted or does not compile
	///     without warnings.
	/// </summary>
	/// <remarks>
	///     Without an emitted mock, the factory's <c>CreateMock</c> call binds to the fallback that always throws, so this
	///     asks the same <see cref="MockClass" /> that decides the emission.
	/// </remarks>
	private static MockClass? GetCompilableDataSourceMock(INamedTypeSymbol type, Compilation compilation)
	{
		// The mock of a type named like a keyword, such as @event, would be a class of that name.
		if (!IsMockable(type) ||
		    SyntaxFacts.GetKeywordKind(type.Name) != SyntaxKind.None ||
		    HasMemberNamedCreateMock(type) ||
		    (type.TypeKind == TypeKind.Interface && HasNonPublicAbstractMembers(type)) ||
		    type.DelegateInvokeMethod is { ReturnsByRef: true, } or { ReturnsByRefReadonly: true, } ||
		    // A delegate mock only implements Invoke, not the virtual members of MulticastDelegate.
		    !(type.DelegateInvokeMethod is { } invoke
			    ? IsCompilableMockMember(invoke)
			    : HasCompilableMockMembers(type, compilation.Assembly)) ||
		    Normalize(type, compilation) is not INamedTypeSymbol normalized)
		{
			return null;
		}

		MockClass mock = GetOrCreateDiscoveredMockClass(normalized, compilation);
		return MockGenerator.IsValidMockDeclaration(mock) ? mock : null;
	}

	private static bool UsesFileLocalType(ITypeSymbol type)
		=> type switch
		{
			IArrayTypeSymbol array => UsesFileLocalType(array.ElementType),
			INamedTypeSymbol named => IsFileLocal(named) || named.TypeArguments.Any(UsesFileLocalType),
			_ => false,
		};

	/// <summary>
	///     Whether the generated factory can name the <paramref name="type" />, as a type argument or in <c>typeof</c>.
	/// </summary>
	private static bool CanBeTypeArgument(ITypeSymbol type, Compilation compilation)
		=> type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol or IDynamicTypeSymbol or IErrorTypeSymbol) &&
		   !type.IsRefLikeType &&
		   type.SpecialType != SpecialType.System_Void &&
		   !ContainsTypeParameter(type) &&
		   !UsesTypeNestedInGenericType(type) &&
		   !UsesUnusableType(type) &&
		   !UsesFileLocalType(type) &&
		   Helpers.IsAccessibleFrom(type, compilation.Assembly);

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass is { } attributeClass && IsMockDataSourceType(attributeClass);

	private static bool IsMockDataSourceType(INamedTypeSymbol type)
		=> type.AllInterfaces.Any(@interface =>
			@interface is { Name: "IMockDataSource", ContainingNamespace: { Name: "Internals", ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, }, }, });

	private enum DataSourceParameterKind
	{
		None,
		Mock,
		ValueOrMock,
		Create,
		Value,
	}

	private sealed record DataSourceTargets(
		List<DataSourceClass> Classes,
		List<ITypeSymbol> MockedTypes,
		List<ITypeSymbol> ValueOrMockTypes,
		List<ITypeSymbol> ValueTypes,
		List<ITypeSymbol> TypesToMock);
}
