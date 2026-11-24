using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Mockolate.SourceGenerators.Internals;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that reports a parameter of a member with an attribute that implements
///     <c>Mockolate.Internals.IMockDataSource</c> whose type asks for a mock or an instance, but that Mockolate cannot create.
/// </summary>
/// <remarks>
///     Mirrors the rules of the source generator (<c>MockGeneratorHelpers.MockDataSources.cs</c>), which skips such a
///     parameter without a diagnostic.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MockDataSourceAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	///     A parameter of a mock data source that Mockolate cannot create.
	/// </summary>
	/// <remarks>
	///     Declared here instead of in <see cref="Rules" />, because the release tracking analyzer reports RS2002 for an
	///     unshipped rule declared in another file.
	/// </remarks>
	public static readonly DiagnosticDescriptor UnsupportedParameterRule = new(
		"Mockolate0010",
		new LocalizableResourceString(nameof(Resources.Mockolate0010Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0010MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Warning,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0010Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		UnsupportedParameterRule,
	];

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method, SymbolKind.NamedType);
	}

	private static void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		IEnumerable<IParameterSymbol> parameters = context.Symbol switch
		{
			// A partial method has two symbols with the same attributes; only the definition reports.
			IMethodSymbol { PartialDefinitionPart: not null, } => [],
			// Mockolate does not create the first parameters, for which every attribute supplies a value.
			IMethodSymbol method when method.GetAttributes().Any(IsMockDataSourceAttribute) => method.Parameters
				.Skip(method.GetAttributes().Where(IsMockDataSourceAttribute).Min(GetSuppliedArgumentCount)),
			// A constructor with an attribute of its own, such as [method: …] on a primary constructor, reports itself.
			INamedTypeSymbol type when type.GetAttributes().Any(IsMockDataSourceAttribute) => type.InstanceConstructors
				.Where(constructor => !constructor.IsImplicitlyDeclared &&
				                      !constructor.GetAttributes().Any(IsMockDataSourceAttribute))
				.SelectMany(constructor => constructor.Parameters),
			_ => [],
		};
		foreach (IParameterSymbol parameter in parameters)
		{
			if (parameter.Locations.FirstOrDefault() is { IsInSource: true, } location &&
			    GetReason(parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), context.Compilation)
				    is { } reason)
			{
				context.ReportDiagnostic(Diagnostic.Create(UnsupportedParameterRule, location,
					parameter.Name, parameter.Type.ToDisplayString(), reason));
			}
		}
	}

	/// <summary>
	///     Why Mockolate cannot create a value of the <paramref name="type" />, or <see langword="null" /> when it can or
	///     when the type does not ask for a mock or an instance.
	/// </summary>
	/// <remarks>
	///     Framework types are often the arguments that the test framework passes, so they are never reported.
	/// </remarks>
	private static string? GetReason(ITypeSymbol type, Compilation compilation)
	{
		if (type is ITypeParameterSymbol)
		{
			return Resources.Mockolate0010TypeParameterReason;
		}

		if (type is IArrayTypeSymbol array)
		{
			return GetArrayReason(array, compilation);
		}

		if (type is not INamedTypeSymbol
		    {
			    TypeKind: TypeKind.Interface or TypeKind.Delegate or TypeKind.Class, IsStatic: false,
		    } namedType ||
		    ConstructorRules.IsFrameworkNamespace(namedType.ContainingNamespace))
		{
			return null;
		}

		if (!MockabilityAnalyzer.IsTypeAccessibleFrom(namedType, compilation.Assembly))
		{
			return Resources.Mockolate0010InaccessibleReason;
		}

		if (ConstructorRules.ContainsTypeParameter(namedType))
		{
			return Resources.Mockolate0010ContainsTypeParameterReason;
		}

		if (UsesFileLocalType(namedType))
		{
			return Resources.Mockolate0010FileLocalReason;
		}

		if (ConstructorRules.IsNestedInGenericType(namedType))
		{
			return Resources.Mockolate0010NestedInGenericTypeReason;
		}

		if (ConstructorRules.HasUsageDiagnostic(namedType))
		{
			return Resources.Mockolate0010ObsoleteReason;
		}

		if (namedType.TypeArguments.Any(UsesUnusableTypeArgument))
		{
			return Resources.Mockolate0010TypeArgumentReason;
		}

		if (namedType.TypeKind != TypeKind.Class || namedType.IsAbstract)
		{
			return GetMockReason(namedType, compilation);
		}

		if (namedType.IsGenericType)
		{
			return Resources.Mockolate0010GenericClassReason;
		}

		return HasSingleLongestConstructor(namedType, compilation)
			? null
			: Resources.Mockolate0010ConstructorReason;
	}

	private static string? GetMockReason(INamedTypeSymbol type, Compilation compilation)
	{
		if (!MockabilityAnalyzer.IsMockable(type, compilation.Assembly, out string? reason))
		{
			return string.Format(CultureInfo.InvariantCulture, Resources.Mockolate0010NotMockableReason, reason);
		}

		if (type.TypeKind == TypeKind.Interface && HasStaticAbstractMembers(type))
		{
			return Resources.Mockolate0010StaticAbstractReason;
		}

		if (ConstructorRules.HasMemberNamedCreateMock(type))
		{
			return Resources.Mockolate0010CreateMockMemberReason;
		}

		if (GetUnsupportedMember(type, compilation.Assembly) is { } member)
		{
			return string.Format(CultureInfo.InvariantCulture, Resources.Mockolate0010MemberReason,
				member.ToDisplayString());
		}

		return type.TypeKind == TypeKind.Class && !HasCallableMockConstructor(type, compilation)
			? Resources.Mockolate0010ParameterlessConstructorReason
			: null;
	}

	private static string? GetArrayReason(IArrayTypeSymbol array, Compilation compilation)
	{
		if (ConstructorRules.ContainsTypeParameter(array))
		{
			return Resources.Mockolate0010ContainsTypeParameterReason;
		}

		if (!MockabilityAnalyzer.IsTypeAccessibleFrom(array, compilation.Assembly))
		{
			return Resources.Mockolate0010InaccessibleReason;
		}

		if (UsesFileLocalType(array))
		{
			return Resources.Mockolate0010FileLocalReason;
		}

		return UsesUnusableTypeArgument(array.ElementType) ? Resources.Mockolate0010TypeArgumentReason : null;
	}

	private static bool UsesFileLocalType(ITypeSymbol type)
		=> type switch
		{
			IArrayTypeSymbol array => UsesFileLocalType(array.ElementType),
			INamedTypeSymbol named => ConstructorRules.IsFileLocal(named) || named.TypeArguments.Any(UsesFileLocalType),
			_ => false,
		};

	private static bool UsesUnusableTypeArgument(ITypeSymbol type)
		=> ConstructorRules.UsesUnusableType(type) || UsesTypeNestedInGenericType(type);

	private static bool UsesTypeNestedInGenericType(ITypeSymbol type)
		=> type switch
		{
			IArrayTypeSymbol array => UsesTypeNestedInGenericType(array.ElementType),
			INamedTypeSymbol named => ConstructorRules.IsNestedInGenericType(named) ||
			                          named.TypeArguments.Any(UsesTypeNestedInGenericType),
			_ => false,
		};

	/// <summary>
	///     Mirror of <c>HasCompilableMockMembers</c> of the source generator: a member that the mock of
	///     <paramref name="type" /> cannot implement without an error or a warning.
	/// </summary>
	private static ISymbol? GetUnsupportedMember(INamedTypeSymbol type, IAssemblySymbol sourceAssembly)
	{
		// A delegate mock only implements Invoke, not the virtual members of MulticastDelegate.
		if (type.DelegateInvokeMethod is { } invoke)
		{
			return invoke.ReturnsByRef || invoke.ReturnsByRefReadonly || IsUnsupportedMember(invoke, true) ? invoke : null;
		}

		bool isClass = type.TypeKind != TypeKind.Interface;
		HashSet<ISymbol> overridden = GetOverriddenMembers(type);
		IEnumerable<ITypeSymbol> types = [type, ..type.AllInterfaces, ..GetBaseTypes(type),];
		return types.SelectMany(declaring => declaring.GetMembers())
			.Where(member => !overridden.Contains(member) && IsImplementedByMock(member, type, isClass, sourceAssembly))
			.FirstOrDefault(member => IsUnsupportedMember(member, isClass));
	}

	private static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
	{
		for (ITypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			yield return baseType;
		}
	}

	/// <summary>
	///     The base members with an override in the hierarchy: the mock implements the override, or nothing when it is
	///     sealed.
	/// </summary>
	private static HashSet<ISymbol> GetOverriddenMembers(ITypeSymbol type)
	{
		HashSet<ISymbol> overridden = new(SymbolEqualityComparer.Default);
		foreach (ISymbol member in new[] { type, }.Concat(GetBaseTypes(type)).SelectMany(current => current.GetMembers()))
		{
			ISymbol? baseMember = member switch
			{
				IMethodSymbol method => method.OverriddenMethod,
				IPropertySymbol property => property.OverriddenProperty,
				IEventSymbol @event => @event.OverriddenEvent,
				_ => null,
			};
			if (baseMember is not null)
			{
				overridden.Add(baseMember);
			}
		}

		return overridden;
	}

	private static bool IsImplementedByMock(ISymbol member, INamedTypeSymbol type, bool isClass,
		IAssemblySymbol sourceAssembly)
		=> member is { IsStatic: false, IsSealed: false, } &&
		   (member.IsAbstract || member.IsVirtual || member.IsOverride) &&
		   !(isClass && member.ContainingType.TypeKind == TypeKind.Interface &&
		     type.FindImplementationForInterfaceMember(member) is not null) &&
		   MockabilityAnalyzer.IsAccessibleFrom(member, sourceAssembly) &&
		   GetSignatureTypes(member).All(signatureType =>
			   MockabilityAnalyzer.IsTypeAccessibleFrom(signatureType, sourceAssembly));

	private static bool IsUnsupportedMember(ISymbol member, bool isClass)
		=> (!isClass && member is { IsAbstract: true, } && member.DeclaredAccessibility != Accessibility.Public) ||
		   ConstructorRules.HasUsageDiagnostic(member) ||
		   member switch
		   {
			   IPropertySymbol property => property.ReturnsByRef || property.ReturnsByRefReadonly ||
			                               property.Parameters.Any(IsRefReadOnlyRefStruct),
			   IMethodSymbol method => method.TypeParameters.Any(typeParameter => typeParameter.AllowsRefLikeType) ||
			                           method.Parameters.Any(IsRefReadOnlyRefStruct),
			   _ => false,
		   } ||
		   GetSignatureTypes(member).Any(ConstructorRules.UsesUnusableType);

	private static bool IsRefReadOnlyRefStruct(IParameterSymbol parameter)
		=> parameter is { RefKind: RefKind.RefReadOnlyParameter, Type.IsRefLikeType: true, };

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
	///     Mirror of <c>MockClass.ConstructorWithoutArguments</c> of the source generator.
	/// </summary>
	private static bool HasCallableMockConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] constructors = type.Constructors
			.Where(constructor =>
				constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected
					or Accessibility.ProtectedOrInternal &&
				constructor.Parameters.All(parameter =>
					MockabilityAnalyzer.IsTypeAccessibleFrom(parameter.Type, compilation.Assembly)))
			.ToArray();
		return MockConstructorSelector.FindConstructorWithoutArguments(constructors, compilation) is { } selected
			? !ConstructorRules.HasUsageDiagnostic(constructors[selected])
			: HasConstructorWithDependencies(type, compilation.Assembly);
	}

	/// <summary>
	///     Mirror of <c>GetDependencyConstructor</c> of the source generator: the single constructor with the most
	///     parameters that <c>CreateMock()</c> fills, which must not be obsolete.
	/// </summary>
	private static bool HasConstructorWithDependencies(INamedTypeSymbol type, IAssemblySymbol sourceAssembly)
	{
		IMethodSymbol[] candidates = type.InstanceConstructors
			.Where(constructor => constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected
				                      or Accessibility.ProtectedOrInternal &&
			                      constructor.Parameters.Length > 0 &&
			                      !ConstructorRules.HasUsageDiagnostic(constructor) &&
			                      constructor.Parameters.All(parameter =>
				                      MockabilityAnalyzer.IsTypeAccessibleFrom(parameter.Type, sourceAssembly) &&
				                      parameter.RefKind is RefKind.None or RefKind.In &&
				                      !parameter.Type.IsRefLikeType &&
				                      parameter.Type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol)))
			.OrderByDescending(constructor => constructor.Parameters.Length)
			.ToArray();
		return candidates.Length > 0 &&
		       (candidates.Length == 1 || candidates[0].Parameters.Length != candidates[1].Parameters.Length);
	}

	private static bool HasStaticAbstractMembers(INamedTypeSymbol type)
		=> type.GetMembers().Concat(type.AllInterfaces.SelectMany(@interface => @interface.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

	/// <summary>
	///     Mirror of <c>GetSutConstructor</c> of the source generator.
	/// </summary>
	private static bool HasSingleLongestConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] constructors = ConstructorRules.GetUsableConstructors(type, compilation);
		return constructors.Length == 1 ||
		       (constructors.Length > 1 && constructors[0].Parameters.Length != constructors[1].Parameters.Length);
	}

	/// <summary>
	///     The number of values that the <paramref name="attribute" /> supplies for the first parameters: the elements of
	///     the <see langword="params" /> array of an attribute like <c>AutoMockArguments</c>, where a single
	///     <see langword="null" /> is one value. Another attribute supplies none.
	/// </summary>
	private static int GetSuppliedArgumentCount(AttributeData attribute)
	{
		if (attribute.AttributeConstructor is not { Parameters.Length: 1, } constructor ||
		    !constructor.Parameters[0].IsParams ||
		    attribute.ConstructorArguments.Length != 1)
		{
			return 0;
		}

		TypedConstant arguments = attribute.ConstructorArguments[0];
		if (arguments.IsNull)
		{
			return 1;
		}

		return arguments.Values.Length;
	}

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass is { } attributeClass &&
		   attributeClass.AllInterfaces.Any(@interface => @interface is
		   {
			   Name: "IMockDataSource",
			   ContainingNamespace: { Name: "Internals", ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, }, },
		   });
}
