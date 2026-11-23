using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mockolate.Analyzers;

/// <summary>
///     Mirrors of the rules by which the source generator decides which classes it can create with their constructor.
/// </summary>
internal static class ConstructorRules
{
	/// <summary>
	///     Why the generator emits no <c>CreateMock(out ...)</c> for the <paramref name="type" />, or
	///     <see langword="null" /> when it does.
	/// </summary>
	internal static string? GetSealedClassReason(INamedTypeSymbol type, Compilation compilation)
	{
		if (type.TypeKind != TypeKind.Class || !(type.IsSealed || type.IsRecord))
		{
			return "CreateMock(out ...) is only for sealed classes and records; use CreateMock() and Mock.ConstructorArgument<T>()";
		}

		if (type.IsStatic || type.IsAbstract)
		{
			return "type is static or abstract";
		}

		if (type.IsGenericType || ContainsTypeParameter(type) || IsNestedInGenericType(type))
		{
			return "type is generic or nested in a generic type";
		}

		if (IsFileLocal(type) || !IsAccessible(type, compilation.Assembly))
		{
			return "type is not accessible from the generated code";
		}

		if (HasUsageDiagnostic(type))
		{
			return "type is obsolete or experimental";
		}

		if (IsFrameworkNamespace(type.ContainingNamespace))
		{
			return "type is declared in the System, Microsoft or Windows namespace";
		}

		for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (!current.GetMembers("CreateMock").IsEmpty)
			{
				return "type has a member named CreateMock, which would hide the generated extension";
			}
		}

		IMethodSymbol[] constructors = GetUsableConstructors(type, compilation);
		return constructors.Length == 0 ||
		       (constructors.Length > 1 && constructors[0].Parameters.Length == constructors[1].Parameters.Length)
			? "type has no single accessible constructor with the most parameters that Mockolate can call"
			: null;
	}

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
