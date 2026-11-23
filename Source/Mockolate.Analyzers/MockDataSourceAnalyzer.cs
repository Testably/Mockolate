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
///     <c>Mockolate.IMockDataSource</c> whose type asks for a mock or an instance, but that Mockolate cannot create.
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
			IMethodSymbol method when method.GetAttributes().Any(IsMockDataSourceAttribute) => method.Parameters,
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
		    CreateWithMocksAnalyzer.IsFrameworkNamespace(namedType.ContainingNamespace))
		{
			return null;
		}

		if (!MockabilityAnalyzer.IsTypeAccessibleFrom(namedType, compilation.Assembly))
		{
			return Resources.Mockolate0010InaccessibleReason;
		}

		if (CreateWithMocksAnalyzer.ContainsTypeParameter(namedType))
		{
			return Resources.Mockolate0010ContainsTypeParameterReason;
		}

		if (UsesFileLocalType(namedType))
		{
			return Resources.Mockolate0010FileLocalReason;
		}

		if (CreateWithMocksAnalyzer.IsNestedInGenericType(namedType))
		{
			return Resources.Mockolate0010NestedInGenericTypeReason;
		}

		if (CreateWithMocksAnalyzer.HasUsageDiagnostic(namedType))
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

		if (!type.GetMembers("CreateMock").IsEmpty ||
		    type.AllInterfaces.Any(@interface => !@interface.GetMembers("CreateMock").IsEmpty))
		{
			return Resources.Mockolate0010CreateMockMemberReason;
		}

		if (GetUnsupportedMember(type, compilation.Assembly) is { } member)
		{
			return string.Format(CultureInfo.InvariantCulture, Resources.Mockolate0010MemberReason,
				member.ToDisplayString());
		}

		return type.TypeKind == TypeKind.Class && !HasConstructorWithoutArguments(type, compilation)
			? Resources.Mockolate0010ParameterlessConstructorReason
			: null;
	}

	private static string? GetArrayReason(IArrayTypeSymbol array, Compilation compilation)
	{
		if (CreateWithMocksAnalyzer.ContainsTypeParameter(array))
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
			INamedTypeSymbol named => CreateWithMocksAnalyzer.IsFileLocal(named) || named.TypeArguments.Any(UsesFileLocalType),
			_ => false,
		};

	private static bool UsesUnusableTypeArgument(ITypeSymbol type)
		=> CreateWithMocksAnalyzer.UsesUnusableType(type) || UsesTypeNestedInGenericType(type);

	private static bool UsesTypeNestedInGenericType(ITypeSymbol type)
		=> type switch
		{
			IArrayTypeSymbol array => UsesTypeNestedInGenericType(array.ElementType),
			INamedTypeSymbol named => CreateWithMocksAnalyzer.IsNestedInGenericType(named) ||
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
		HashSet<ISymbol> overridden = new(SymbolEqualityComparer.Default);
		IEnumerable<ITypeSymbol> types = [type, ..type.AllInterfaces,];
		for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
		{
			if (!SymbolEqualityComparer.Default.Equals(current, type))
			{
				types = types.Append(current);
			}

			foreach (ISymbol member in current.GetMembers())
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
		}

		return types.SelectMany(declaring => declaring.GetMembers())
			.Where(member => member is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke, } ||
			                 (member is { IsStatic: false, IsSealed: false, } &&
			                  !overridden.Contains(member) &&
			                  (member.IsAbstract || member.IsVirtual || member.IsOverride) &&
			                  !(isClass && member.ContainingType.TypeKind == TypeKind.Interface &&
			                    type.FindImplementationForInterfaceMember(member) is not null) &&
			                  MockabilityAnalyzer.IsAccessibleFrom(member, sourceAssembly) &&
			                  GetSignatureTypes(member).All(signatureType =>
				                  MockabilityAnalyzer.IsTypeAccessibleFrom(signatureType, sourceAssembly))))
			.FirstOrDefault(member => IsUnsupportedMember(member, isClass));
	}

	private static bool IsUnsupportedMember(ISymbol member, bool isClass)
		=> (!isClass && member is { IsAbstract: true, } && member.DeclaredAccessibility != Accessibility.Public) ||
		   CreateWithMocksAnalyzer.HasUsageDiagnostic(member) ||
		   member switch
		   {
			   IPropertySymbol property => property.ReturnsByRef || property.ReturnsByRefReadonly ||
			                               property.Parameters.Any(IsRefReadOnlyRefStruct),
			   IMethodSymbol method => method.TypeParameters.Any(typeParameter => typeParameter.AllowsRefLikeType) ||
			                           method.Parameters.Any(IsRefReadOnlyRefStruct),
			   _ => false,
		   } ||
		   GetSignatureTypes(member).Any(CreateWithMocksAnalyzer.UsesUnusableType);

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
	private static bool HasConstructorWithoutArguments(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] constructors = type.Constructors
			.Where(constructor =>
				constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected
					or Accessibility.ProtectedOrInternal &&
				constructor.Parameters.All(parameter =>
					MockabilityAnalyzer.IsTypeAccessibleFrom(parameter.Type, compilation.Assembly)))
			.ToArray();
		return MockConstructorSelector.FindConstructorWithoutArguments(constructors, compilation) is { } selected &&
		       !CreateWithMocksAnalyzer.HasUsageDiagnostic(constructors[selected]);
	}

	private static bool HasStaticAbstractMembers(INamedTypeSymbol type)
		=> type.GetMembers().Concat(type.AllInterfaces.SelectMany(@interface => @interface.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

	/// <summary>
	///     Mirror of <c>GetSutConstructor</c> of the source generator.
	/// </summary>
	private static bool HasSingleLongestConstructor(INamedTypeSymbol type, Compilation compilation)
	{
		IMethodSymbol[] constructors = CreateWithMocksAnalyzer.GetUsableConstructors(type, compilation);
		return constructors.Length == 1 ||
		       (constructors.Length > 1 && constructors[0].Parameters.Length != constructors[1].Parameters.Length);
	}

	private static bool IsMockDataSourceAttribute(AttributeData attribute)
		=> attribute.AttributeClass is { } attributeClass &&
		   attributeClass.AllInterfaces.Any(@interface => @interface is
		   {
			   Name: "IMockDataSource",
			   ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, },
		   });
}
