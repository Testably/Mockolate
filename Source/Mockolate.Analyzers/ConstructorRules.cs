using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.Analyzers;

/// <summary>
///     Mirrors of the rules by which the source generator decides which classes it can create with their constructor.
/// </summary>
internal static class ConstructorRules
{
	/// <summary>
	///     Mirror of <c>GetSutConstructor</c> in the generator: the constructors that the generated code can call, with
	///     the most parameters first.
	/// </summary>
	internal static IMethodSymbol[] GetUsableConstructors(INamedTypeSymbol type, Compilation compilation)
	{
		bool hasRequiredMembers = HasRequiredMembers(type);
		return type.InstanceConstructors
			.Where(constructor => !constructor.IsVararg &&
			                      !HasUsageDiagnostic(constructor) &&
			                      (!hasRequiredMembers || HasSetsRequiredMembers(constructor)) &&
			                      compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly) &&
			                      constructor.Parameters.All(parameter =>
				                      parameter.RefKind is RefKind.None or RefKind.In &&
				                      !parameter.Type.IsRefLikeType &&
				                      !UsesUnusableType(parameter.Type) &&
				                      IsAccessible(parameter.Type, compilation.Assembly)))
			.OrderByDescending(constructor => constructor.Parameters.Length)
			.ToArray();
	}

	internal static bool IsFrameworkNamespace(INamespaceSymbol? namespaceSymbol)
	{
		while (namespaceSymbol is { ContainingNamespace.IsGlobalNamespace: false, })
		{
			namespaceSymbol = namespaceSymbol.ContainingNamespace;
		}

		return namespaceSymbol?.Name is "System" or "Microsoft" or "Windows";
	}

	internal static bool IsFileLocal(INamedTypeSymbol type)
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

	internal static bool IsNestedInGenericType(INamedTypeSymbol type)
	{
		for (INamedTypeSymbol? current = type.ContainingType; current is not null; current = current.ContainingType)
		{
			if (current.IsGenericType)
			{
				return true;
			}
		}

		return false;
	}

	internal static bool ContainsTypeParameter(ITypeSymbol type)
		=> type switch
		{
			ITypeParameterSymbol => true,
			IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
			IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
			INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter) ||
			                          (named.ContainingType is { } containingType && ContainsTypeParameter(containingType)),
			_ => false,
		};

	internal static bool HasUsageDiagnostic(ISymbol symbol)
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

	internal static bool UsesUnusableType(ITypeSymbol type)
		=> type switch
		{
			IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
			IArrayTypeSymbol array => UsesUnusableType(array.ElementType),
			INamedTypeSymbol named => HasUsageDiagnostic(named) ||
			                          (named.IsRefLikeType && !IsSpan(named)) ||
			                          (named.TypeKind == TypeKind.Interface && HasStaticAbstractMembers(named)) ||
			                          named.TypeArguments.Any(UsesUnusableType),
			_ => false,
		};

	private static bool IsSpan(INamedTypeSymbol type)
		=> type is
		{
			Name: "Span" or "ReadOnlySpan", Arity: 1,
			ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true, },
		};

	private static bool HasStaticAbstractMembers(INamedTypeSymbol type)
		=> type.GetMembers().Concat(type.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

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
	///     Mirror of <c>Helpers.IsAccessibleFrom</c> in the generator.
	/// </summary>
	private static bool IsAccessible(ITypeSymbol type, IAssemblySymbol sourceAssembly)
	{
		switch (type)
		{
			case IArrayTypeSymbol array:
				return IsAccessible(array.ElementType, sourceAssembly);
			case IPointerTypeSymbol pointer:
				return IsAccessible(pointer.PointedAtType, sourceAssembly);
			case INamedTypeSymbol named:
				for (INamedTypeSymbol? t = named; t is not null; t = t.ContainingType)
				{
					if (!IsDeclarationAccessible(t) ||
					    !t.TypeArguments.All(argument => IsAccessible(argument, sourceAssembly)))
					{
						return false;
					}
				}

				return true;
			default:
				return true;
		}

		bool IsDeclarationAccessible(INamedTypeSymbol candidate)
			=> candidate.DeclaredAccessibility switch
			{
				Accessibility.Public => true,
				Accessibility.Internal or Accessibility.ProtectedOrInternal =>
					SymbolEqualityComparer.Default.Equals(candidate.ContainingAssembly, sourceAssembly) ||
					candidate.ContainingAssembly?.GivesAccessTo(sourceAssembly) == true,
				_ => false,
			};
	}
}
