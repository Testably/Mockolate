using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that checks that the memory cache setups and verifications are only used on mocks that behave like a
///     cache.
/// </summary>
/// <remarks>
///     The cache members extend <c>IMockSetup&lt;IMemoryCache&gt;</c> and <c>IMockVerify&lt;IMemoryCache&gt;</c>, which
///     are covariant, so they also apply to the mock of a class that implements <c>IMemoryCache</c>. Such a mock only
///     behaves like a cache when <c>TryGetValue</c>, <c>CreateEntry</c> and <c>Remove</c> are abstract.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MemoryCacheAnalyzer : DiagnosticAnalyzer
{
	private static readonly string[] s_cacheMembers = ["TryGetValue", "CreateEntry", "Remove",];

	private static readonly DiagnosticDescriptor s_memoryCacheRule = new(
		"Mockolate0014",
		new LocalizableResourceString(nameof(Resources.Mockolate0014Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0014MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Error,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0014Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> ImmutableArray.Create(s_memoryCacheRule);

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess, } invocation ||
		    context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
		    !IsCacheMember(method) ||
		    context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type is not { } receiverType ||
		    GetUnsupportedClass(receiverType) is not { } unsupportedClass)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(s_memoryCacheRule, memberAccess.Name.GetLocation(),
			unsupportedClass.ToDisplayString()));
	}

	private static bool IsCacheMember(IMethodSymbol method)
	{
		for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
		{
			if (type.Name == "MockolateCaching" &&
			    AnalyzerHelpers.IsMockolateNamespace(type.ContainingNamespace))
			{
				return true;
			}
		}

		return false;
	}

	private static INamedTypeSymbol? GetUnsupportedClass(ITypeSymbol receiverType)
		=> new[] { receiverType, }.Concat(receiverType.AllInterfaces)
			.OfType<INamedTypeSymbol>()
			.Where(facade => facade is { Name: "IMockSetup" or "IMockVerify", TypeArguments.Length: 1, } &&
			                 facade.ContainingNamespace is { Name: "Setup" or "Verify", } facadeNamespace &&
			                 AnalyzerHelpers.IsMockolateNamespace(facadeNamespace.ContainingNamespace))
			.Select(facade => facade.TypeArguments[0])
			.OfType<INamedTypeSymbol>()
			.FirstOrDefault(mockedClass => mockedClass.TypeKind == TypeKind.Class && IsUnsupported(mockedClass));

	/// <remarks>
	///     Mirrors <c>Class.GetAdapterClassMembers</c> in the generator for <c>MockAdapterClasses.WithAbstractMembers</c>.
	/// </remarks>
	private static bool IsUnsupported(INamedTypeSymbol mockedClass)
	{
		if (mockedClass.AllInterfaces.FirstOrDefault(IsMemoryCacheInterface) is not { } memoryCache)
		{
			return false;
		}

		return s_cacheMembers.Any(name =>
			memoryCache.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault() is not { } member ||
			mockedClass.FindImplementationForInterfaceMember(member) is not IMethodSymbol implementation ||
			LoggingAnalyzer.GetMostDerivedOverride(mockedClass, implementation) is not
			{
				MethodKind: MethodKind.Ordinary, IsSealed: false, IsAbstract: true,
			});
	}

	private static bool IsMemoryCacheInterface(INamedTypeSymbol type)
		=> type is
		{
			Name: "IMemoryCache",
			TypeArguments.Length: 0,
			ContainingNamespace:
			{
				Name: "Memory",
				ContainingNamespace:
				{
					Name: "Caching",
					ContainingNamespace:
					{
						Name: "Extensions",
						ContainingNamespace: { Name: "Microsoft", ContainingNamespace.IsGlobalNamespace: true, },
					},
				},
			},
		};
}
