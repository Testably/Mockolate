using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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

		if (CreateWithMocksAnalyzer.IsFileLocal(namedType))
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

		return type.TypeKind == TypeKind.Class && !HasParameterlessConstructorForMock(type, compilation)
			? Resources.Mockolate0010ParameterlessConstructorReason
			: null;
	}

	/// <summary>
	///     Mirror of <c>HasParameterlessConstructorForMock</c> of the source generator.
	/// </summary>
	private static bool HasParameterlessConstructorForMock(INamedTypeSymbol type, Compilation compilation)
		=> type.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 &&
		                                                !CreateWithMocksAnalyzer.HasUsageDiagnostic(constructor) &&
		                                                constructor.DeclaredAccessibility switch
		                                                {
			                                                Accessibility.Public or Accessibility.Protected
				                                                or Accessibility.ProtectedOrInternal => true,
			                                                Accessibility.Internal or Accessibility.ProtectedAndInternal =>
				                                                compilation.IsSymbolAccessibleWithin(constructor,
					                                                compilation.Assembly),
			                                                _ => false,
		                                                });

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
