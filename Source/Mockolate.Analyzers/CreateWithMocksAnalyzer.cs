using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     Analyzer that explains why a <c>CreateWithMocks</c> call does not compile: the class gets no
///     <c>CreateWithMocks</c>, or a constructor dependency is not mocked and needs an argument.
/// </summary>
/// <remarks>
///     The source generator skips what it cannot generate without reporting a diagnostic, so the checks mirror
///     <c>MockGeneratorHelpers.CreateWithMocks.cs</c> in the generator; keep both in sync. A call is only reported
///     when it does not bind, so a drift changes the reason, never whether valid code is reported.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CreateWithMocksAnalyzer : DiagnosticAnalyzer
{
	private const string CreateWithMocksName = "CreateWithMocks";

	private static readonly DiagnosticDescriptor s_creatableRule = new(
		"Mockolate0008",
		new LocalizableResourceString(nameof(Resources.Mockolate0008Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0008MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Error,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0008Description),
			Resources.ResourceManager, typeof(Resources)));

	private static readonly DiagnosticDescriptor s_dependencyRule = new(
		"Mockolate0009",
		new LocalizableResourceString(nameof(Resources.Mockolate0009Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0009MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Error,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0009Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> ImmutableArray.Create(s_creatableRule, s_dependencyRule);

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax
		    {
			    Expression: MemberAccessExpressionSyntax
			    {
				    Name: IdentifierNameSyntax { Identifier.ValueText: CreateWithMocksName, },
			    } memberAccess,
		    } invocation)
		{
			return;
		}

		SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
		if (symbolInfo.Symbol is not null || !symbolInfo.CandidateSymbols.IsEmpty ||
		    context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol is not
			    INamedTypeSymbol sut)
		{
			return;
		}

		if (GetReason(sut, context.Compilation) is { } reason)
		{
			context.ReportDiagnostic(Diagnostic.Create(s_creatableRule, memberAccess.Expression.GetLocation(),
				sut.ToDisplayString(), reason));
		}
		// A static extension member that does not apply to the arguments is no candidate (CS0117), so the generated
		// method is looked up by its receiver.
		else if (FindGeneratedMethod(sut, context.Compilation) is { } method)
		{
			ReportMissingDependencies(context, invocation, memberAccess, method);
		}
	}

	private static IMethodSymbol? FindGeneratedMethod(INamedTypeSymbol sut, Compilation compilation)
		=> compilation.Assembly.GlobalNamespace.GetNamespaceMembers()
			.Where(@namespace => @namespace.Name == "Mockolate")
			.SelectMany(@namespace => @namespace.GetTypeMembers())
			.SelectMany(type => type.GetTypeMembers())
			.Where(extension => extension.ExtensionParameter is { } parameter &&
			                    SymbolEqualityComparer.Default.Equals(parameter.Type, sut))
			.SelectMany(extension => extension.GetMembers(CreateWithMocksName))
			.OfType<IMethodSymbol>()
			.FirstOrDefault();

	/// <summary>
	///     Reports the parameters of the generated <paramref name="method" /> of class or interface type that the call
	///     leaves without an argument, because the generator did not mock them.
	/// </summary>
	private static void ReportMissingDependencies(SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, IMethodSymbol method)
	{
		HashSet<string> named = [];
		int positional = 0;
		foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
		{
			if (argument.NameColon is { } nameColon)
			{
				named.Add(nameColon.Name.Identifier.ValueText);
			}
			else
			{
				positional++;
			}
		}

		for (int i = 0; i < method.Parameters.Length; i++)
		{
			IParameterSymbol parameter = method.Parameters[i];
			if (parameter.RefKind == RefKind.Out || parameter.HasExplicitDefaultValue || parameter.IsParams ||
			    i < positional || named.Contains(parameter.Name) ||
			    GetNotMockedReason(parameter.Type) is not { } reason)
			{
				continue;
			}

			context.ReportDiagnostic(Diagnostic.Create(s_dependencyRule, memberAccess.Name.GetLocation(),
				parameter.Name, parameter.Type.ToDisplayString(), reason));
		}
	}

	/// <summary>
	///     Why the generator did not mock a required parameter of <paramref name="type" />, or <see langword="null" />
	///     when nobody would expect a mock for it.
	/// </summary>
	private static string? GetNotMockedReason(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Class, SpecialType: SpecialType.None, } named)
		{
			return null;
		}

		if (named.TypeKind == TypeKind.Class)
		{
			return "only interfaces are mocked";
		}

		if (named.ContainingNamespace.IsGlobalNamespace)
		{
			return "interfaces in the global namespace are not mocked";
		}

		if (IsFrameworkNamespace(named.ContainingNamespace))
		{
			return
				"interfaces in the System, Microsoft and Windows namespaces are not mocked, except ILogger, ILogger<T> and ILoggerFactory";
		}

		return "its mock would not compile without warnings, for example because a member is obsolete";
	}

	/// <summary>
	///     Why the generator gives <paramref name="sut" /> no <c>CreateWithMocks</c>, or <see langword="null" /> when
	///     no rule of the generator applies.
	/// </summary>
	private static string? GetReason(INamedTypeSymbol sut, Compilation compilation)
	{
		if (sut.TypeKind != TypeKind.Class)
		{
			return sut.TypeKind == TypeKind.Struct ? "it is a struct" : "it is not a class";
		}

		if (sut.IsStatic)
		{
			return "it is static";
		}

		if (sut.IsAbstract)
		{
			return "it is abstract";
		}

		if (sut.IsGenericType || ContainsTypeParameter(sut) || IsNestedInGenericType(sut))
		{
			return "it is generic or nested in a generic type";
		}

		if (IsFileLocal(sut))
		{
			return "it is file-local";
		}

		if (HasUsageDiagnostic(sut))
		{
			return "it is obsolete or experimental";
		}

		if (!IsAccessible(sut, compilation.Assembly))
		{
			return "it is not accessible from the generated code, which needs a public or internal class";
		}

		if (IsFrameworkNamespace(sut.ContainingNamespace))
		{
			return "it is declared in the System, Microsoft or Windows namespace";
		}

		string containerName = GetContainerName(sut);
		if (compilation.GetCompilationNamespace(sut.ContainingNamespace)?.GetMembers(containerName).Any(member =>
			    compilation.IsSymbolAccessibleWithin(member, compilation.Assembly)) == true)
		{
			return $"the name '{containerName}' of its arguments container is already used in its namespace";
		}

		IMethodSymbol[] constructors = GetUsableConstructors(sut, compilation);
		if (constructors.Length == 0)
		{
			return
				"it has no accessible constructor without ref, out, pointer or ref struct parameters that is not obsolete and sets all required members";
		}

		if (constructors.Length > 1 && constructors[0].Parameters.Length == constructors[1].Parameters.Length)
		{
			return
				$"several accessible constructors have the most parameters ({constructors[0].Parameters.Length}), so none of them is chosen";
		}

		return null;
	}

	private static IMethodSymbol[] GetUsableConstructors(INamedTypeSymbol type, Compilation compilation)
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

	private static string GetContainerName(INamedTypeSymbol sut)
	{
		List<string> names = [];
		for (INamedTypeSymbol? current = sut; current is not null; current = current.ContainingType)
		{
			names.Insert(0, current.Name.Replace("_", "__"));
		}

		return string.Join("_0", names) + "Mocks";
	}

	private static bool IsFrameworkNamespace(INamespaceSymbol? namespaceSymbol)
	{
		while (namespaceSymbol is { ContainingNamespace.IsGlobalNamespace: false, })
		{
			namespaceSymbol = namespaceSymbol.ContainingNamespace;
		}

		return namespaceSymbol?.Name is "System" or "Microsoft" or "Windows";
	}

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

	private static bool IsNestedInGenericType(INamedTypeSymbol type)
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

	private static bool ContainsTypeParameter(ITypeSymbol type)
		=> type switch
		{
			ITypeParameterSymbol => true,
			IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
			IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
			INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter) ||
			                          (named.ContainingType is { } containingType && ContainsTypeParameter(containingType)),
			_ => false,
		};

	private static bool UsesUnusableType(ITypeSymbol type)
		=> type switch
		{
			IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
			IArrayTypeSymbol array => UsesUnusableType(array.ElementType),
			INamedTypeSymbol named => HasUsageDiagnostic(named) ||
			                          (named.TypeKind == TypeKind.Interface && HasStaticAbstractMembers(named)) ||
			                          named.TypeArguments.Any(UsesUnusableType),
			_ => false,
		};

	private static bool HasStaticAbstractMembers(INamedTypeSymbol type)
		=> type.GetMembers().Concat(type.AllInterfaces.SelectMany(i => i.GetMembers()))
			.Any(member => member is { IsStatic: true, IsAbstract: true, });

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
