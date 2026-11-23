using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.SourceGenerators;

internal static partial class MockGeneratorHelpers
{
	/// <summary>
	///     The constructors of the class <paramref name="type" /> that a mock can call.
	/// </summary>
	/// <remarks>
	///     Parameter types are named verbatim in <c>MockExtensionsForXXX</c>, which does not derive from the mocked type:
	///     a type reachable only through inheritance would cause CS0122 there and CS0051 on the generated
	///     <see langword="public" /> constructor.
	/// </remarks>
	internal static IMethodSymbol[] GetMockConstructors(INamedTypeSymbol type, IAssemblySymbol sourceAssembly)
		=> type.Constructors
			.Where(constructor => constructor.DeclaredAccessibility is Accessibility.Protected
				                      or Accessibility.ProtectedOrInternal or Accessibility.Public &&
			                      constructor.Parameters.All(parameter =>
				                      Helpers.IsAccessibleFrom(parameter.Type, sourceAssembly)))
			.ToArray();

	/// <summary>
	///     The index in <paramref name="constructors" /> of the constructor that <c>CreateMock()</c> fills with
	///     dependencies, when no constructor can be called without arguments: the one with the most parameters, or
	///     <see langword="null" /> when several share that count.
	/// </summary>
	/// <remarks>
	///     A parameter passed by reference or of a ref struct type cannot be filled from the object array of
	///     constructor parameters, which the mock also stores.
	/// </remarks>
	internal static int? GetDependencyConstructor(IMethodSymbol[] constructors)
	{
		int? result = null;
		int count = 0;
		bool isTied = false;
		for (int index = 0; index < constructors.Length; index++)
		{
			IMethodSymbol constructor = constructors[index];
			if (constructor.Parameters.Length == 0 ||
			    HasUsageDiagnostic(constructor) ||
			    constructor.Parameters.Any(parameter => parameter.RefKind is not (RefKind.None or RefKind.In) ||
			                                            parameter.Type.IsRefLikeType ||
			                                            parameter.Type is IPointerTypeSymbol or IFunctionPointerTypeSymbol))
			{
				continue;
			}

			if (constructor.Parameters.Length > count)
			{
				result = index;
				count = constructor.Parameters.Length;
				isTied = false;
			}
			else if (constructor.Parameters.Length == count)
			{
				isTied = true;
			}
		}

		return isTied ? null : result;
	}

	/// <summary>
	///     How <c>CreateMock()</c> fills each parameter of the <paramref name="constructor" />.
	/// </summary>
	internal static ConstructorDependency[] GetConstructorDependencies(IMethodSymbol constructor,
		Compilation compilation)
		=> constructor.Parameters
			.Select(parameter =>
			{
				ITypeSymbol type = parameter.Type.WithDynamicAsObject(compilation);
				string? mockType = !parameter.IsParams &&
				                   IsDependencyMockable(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
					                   compilation)
					? ToFullName(Normalize(type, compilation))
					: null;
				return new ConstructorDependency(mockType,
					parameter.Type.NullableAnnotation == NullableAnnotation.Annotated ||
					parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
			})
			.ToArray();

	/// <summary>
	///     The types that <c>CreateMock()</c> mocks for the constructor dependencies of the class <paramref name="type" />.
	/// </summary>
	internal static IEnumerable<ITypeSymbol> GetConstructorDependencyMockTypes(ITypeSymbol type,
		Compilation compilation)
	{
		if (type is not INamedTypeSymbol { TypeKind: TypeKind.Class, } namedType)
		{
			return [];
		}

		IMethodSymbol[] constructors = GetMockConstructors(namedType, compilation.Assembly);
		if (constructors.Any(constructor => constructor.Parameters.All(parameter =>
			    parameter.HasExplicitDefaultValue || parameter.IsParams)) ||
		    GetDependencyConstructor(constructors) is not { } index)
		{
			return [];
		}

		return constructors[index].Parameters
			.Where(parameter => !parameter.IsParams)
			.Select(parameter => parameter.Type.WithDynamicAsObject(compilation)
				.WithNullableAnnotation(NullableAnnotation.NotAnnotated))
			.Where(parameterType => IsDependencyMockable(parameterType, compilation))
			.ToArray();
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
	///     a ref struct type can neither be stored nor be optional.
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
}
