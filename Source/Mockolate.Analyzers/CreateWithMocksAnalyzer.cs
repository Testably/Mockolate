using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
///     when it does not bind, so a drift changes the reason, never whether valid code is reported. The analyzer sees
///     the generated code, so a generated <c>CreateWithMocks</c> of the class is looked for first.
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
		if (symbolInfo.Symbol is not null ||
		    GetReceiverType(memberAccess.Expression, context.SemanticModel, context.CancellationToken) is not { } sut)
		{
			return;
		}

		// The generated method only applies to the arguments when the call binds. Otherwise it is a candidate
		// (CS7036), the generated method of another class is (CS1929), or none is in scope (CS0117).
		if (FindGeneratedMethod(sut, context.Compilation) is { } method)
		{
			ReportMissingDependencies(context, invocation, memberAccess, method);
			return;
		}

		// A member of the class named CreateWithMocks keeps the generator away, and the compiler reports the call to it.
		if (symbolInfo.CandidateSymbols.Any(candidate => !IsInMockolateNamespace(candidate)))
		{
			return;
		}

		if (GetReason(sut, context.Compilation) is { } reason)
		{
			context.ReportDiagnostic(Diagnostic.Create(s_creatableRule, memberAccess.Expression.GetLocation(),
				sut.ToDisplayString(), reason));
		}
	}

	/// <summary>
	///     Mirror of <c>GetReceiverType</c> in the generator, which also accepts a member that hides the type of the same
	///     name (<c>Svc Svc { get; }</c>).
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
	///     The <c>CreateWithMocks</c> method that the generator declared for <paramref name="sut" /> in the extension
	///     blocks of <c>Mockolate.Mock</c>.
	/// </summary>
	private static IMethodSymbol? FindGeneratedMethod(INamedTypeSymbol sut, Compilation compilation)
		=> compilation.Assembly.GlobalNamespace.GetNamespaceMembers()
			.Where(@namespace => @namespace.Name == "Mockolate")
			.SelectMany(@namespace => @namespace.GetTypeMembers("Mock"))
			.SelectMany(mock => mock.GetTypeMembers())
			.Where(extension => extension.ExtensionParameter is { } parameter &&
			                    SymbolEqualityComparer.Default.Equals(parameter.Type, sut))
			.SelectMany(extension => extension.GetMembers(CreateWithMocksName))
			.OfType<IMethodSymbol>()
			.FirstOrDefault();

	private static bool IsInMockolateNamespace(ISymbol symbol)
		=> symbol.ContainingNamespace is { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, };

	/// <summary>
	///     Reports the required parameters of the generated <paramref name="method" /> that the call leaves without an
	///     argument and whose type someone could expect to be mocked.
	/// </summary>
	private static void ReportMissingDependencies(SyntaxNodeAnalysisContext context,
		InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, IMethodSymbol method)
	{
		// A positional argument binds the parameter at its position, also after named arguments in their position.
		HashSet<string> named = [];
		HashSet<int> positions = [];
		SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
		for (int i = 0; i < arguments.Count; i++)
		{
			if (arguments[i].NameColon is { } nameColon)
			{
				named.Add(nameColon.Name.Identifier.ValueText);
			}
			else
			{
				positions.Add(i);
			}
		}

		for (int i = 0; i < method.Parameters.Length; i++)
		{
			IParameterSymbol parameter = method.Parameters[i];
			if (parameter.RefKind == RefKind.Out || parameter.HasExplicitDefaultValue ||
			    positions.Contains(i) || named.Contains(parameter.Name) ||
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
	///     for a type that nobody would expect to be mocked, such as <see langword="string" /> or a struct.
	/// </summary>
	/// <remarks>
	///     Mirrors <c>IsDependencyMockable</c> in the generator for the most common reasons; the last reason covers the
	///     remaining checks of <c>CanBeMockedImplicitly</c>.
	/// </remarks>
	private static string? GetNotMockedReason(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol
		    {
			    TypeKind: TypeKind.Interface or TypeKind.Class or TypeKind.Delegate, SpecialType: SpecialType.None,
		    } named)
		{
			return null;
		}

		if (named.TypeKind != TypeKind.Interface)
		{
			return Resources.Mockolate0009NotAnInterfaceReason;
		}

		if (named.ContainingNamespace.IsGlobalNamespace)
		{
			return Resources.Mockolate0009GlobalNamespaceReason;
		}

		if (IsFrameworkNamespace(named.ContainingNamespace))
		{
			return Resources.Mockolate0009FrameworkNamespaceReason;
		}

		if (IsNestedInGenericType(named))
		{
			return Resources.Mockolate0009NestedInGenericTypeReason;
		}

		if (HasStaticAbstractMembers(named))
		{
			return Resources.Mockolate0009StaticAbstractMembersReason;
		}

		if (!named.GetMembers("CreateMock").IsEmpty ||
		    named.AllInterfaces.Any(@interface => !@interface.GetMembers("CreateMock").IsEmpty))
		{
			return Resources.Mockolate0009CreateMockMemberReason;
		}

		return Resources.Mockolate0009OtherReason;
	}

	/// <summary>
	///     Why the generator gives <paramref name="sut" /> no <c>CreateWithMocks</c>, or <see langword="null" /> when
	///     no rule of the generator applies.
	/// </summary>
	private static string? GetReason(INamedTypeSymbol sut, Compilation compilation)
		=> GetClassReason(sut, compilation) ?? GetConstructionReason(sut, compilation);

	private static string? GetClassReason(INamedTypeSymbol sut, Compilation compilation)
	{
		if (sut.TypeKind != TypeKind.Class)
		{
			return sut.TypeKind == TypeKind.Struct
				? Resources.Mockolate0008StructReason
				: Resources.Mockolate0008NotAClassReason;
		}

		if (sut.IsStatic)
		{
			return Resources.Mockolate0008StaticReason;
		}

		if (sut.IsAbstract)
		{
			return Resources.Mockolate0008AbstractReason;
		}

		if (sut.IsGenericType || ContainsTypeParameter(sut) || IsNestedInGenericType(sut))
		{
			return Resources.Mockolate0008GenericReason;
		}

		if (IsFileLocal(sut))
		{
			return Resources.Mockolate0008FileLocalReason;
		}

		if (HasUsageDiagnostic(sut))
		{
			return Resources.Mockolate0008ObsoleteReason;
		}

		if (!IsAccessible(sut, compilation.Assembly))
		{
			return Resources.Mockolate0008InaccessibleReason;
		}

		return IsFrameworkNamespace(sut.ContainingNamespace)
			? Resources.Mockolate0008FrameworkNamespaceReason
			: null;
	}

	private static string? GetConstructionReason(INamedTypeSymbol sut, Compilation compilation)
	{
		// Only reached when the generator emitted no container, so a type of that name is the user's.
		string containerName = GetContainerName(sut);
		if (compilation.GetCompilationNamespace(sut.ContainingNamespace)?.GetMembers(containerName).Any(member =>
			    member is INamespaceSymbol or INamedTypeSymbol { Arity: 0, } &&
			    compilation.IsSymbolAccessibleWithin(member, compilation.Assembly)) == true)
		{
			return string.Format(Resources.Mockolate0008ContainerNameReason, containerName);
		}

		IMethodSymbol[] constructors = GetUsableConstructors(sut, compilation);
		if (constructors.Length == 0)
		{
			return Resources.Mockolate0008NoConstructorReason;
		}

		if (constructors.Length > 1 && constructors[0].Parameters.Length == constructors[1].Parameters.Length)
		{
			return string.Format(Resources.Mockolate0008TiedConstructorsReason, constructors[0].Parameters.Length);
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
