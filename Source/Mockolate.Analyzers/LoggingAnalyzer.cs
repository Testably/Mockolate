using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that checks that the logging setups and verifications are only used on mocks that support them.
/// </summary>
/// <remarks>
///     The logging members extend <c>IMockSetup&lt;ILogger&gt;</c> and <c>IMockVerify&lt;ILogger&gt;</c>, which are
///     covariant, so they also apply to the mock of a class that implements <c>ILogger</c>. Such a mock only supports
///     them when it can override <c>ILogger.Log</c>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoggingAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_loggingRule = new(
		"Mockolate0005",
		new LocalizableResourceString(nameof(Resources.Mockolate0005Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0005MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Error,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0005Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> ImmutableArray.Create(s_loggingRule);

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
		    !IsLoggingMember(method) ||
		    context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type is not { } receiverType ||
		    GetUnsupportedLoggerClass(receiverType) is not { } loggerClass)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(s_loggingRule, memberAccess.Name.GetLocation(),
			loggerClass.ToDisplayString()));
	}

	private static bool IsLoggingMember(IMethodSymbol method)
	{
		for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
		{
			if (type.Name is "MockolateLogging" or "MockolateLoggerFactoryLogging" &&
			    type.ContainingNamespace is { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, })
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     The mocked class behind the setup or verify facade <paramref name="receiverType" /> that implements
	///     <c>ILogger</c> without an overridable <c>Log</c>, or <see langword="null" /> when the logging members are
	///     supported.
	/// </summary>
	private static INamedTypeSymbol? GetUnsupportedLoggerClass(ITypeSymbol receiverType)
	{
		foreach (INamedTypeSymbol facade in new[] { receiverType, }.Concat(receiverType.AllInterfaces)
			         .OfType<INamedTypeSymbol>())
		{
			if (facade is not { Name: "IMockSetup" or "IMockVerify", TypeArguments.Length: 1, } ||
			    facade.ContainingNamespace is not { Name: "Setup" or "Verify", ContainingNamespace.Name: "Mockolate", } ||
			    facade.TypeArguments[0] is not INamedTypeSymbol { TypeKind: TypeKind.Class, } mockedClass ||
			    mockedClass.AllInterfaces.FirstOrDefault(IsLoggerInterface) is not { } logger)
			{
				continue;
			}

			if (logger.GetMembers("Log").OfType<IMethodSymbol>().FirstOrDefault() is not { } log)
			{
				continue;
			}

			if (mockedClass.FindImplementationForInterfaceMember(log) is not IMethodSymbol implementation ||
			    GetMostDerivedOverride(mockedClass, implementation) is not
			    {
				    MethodKind: MethodKind.Ordinary, IsSealed: false,
			    } mostDerived ||
			    !(mostDerived.IsVirtual || mostDerived.IsAbstract || mostDerived.IsOverride))
			{
				return mockedClass;
			}
		}

		return null;
	}

	/// <summary>
	///     The override of <paramref name="implementation" /> in the most derived type of <paramref name="mockedClass" />,
	///     which the mock would have to override in turn.
	/// </summary>
	private static IMethodSymbol GetMostDerivedOverride(INamedTypeSymbol mockedClass, IMethodSymbol implementation)
	{
		for (INamedTypeSymbol? type = mockedClass; type is not null; type = type.BaseType)
		{
			foreach (IMethodSymbol candidate in type.GetMembers(implementation.Name).OfType<IMethodSymbol>())
			{
				for (IMethodSymbol? overridden = candidate; overridden is not null; overridden = overridden.OverriddenMethod)
				{
					if (SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition,
						    implementation.OriginalDefinition))
					{
						return candidate;
					}
				}
			}
		}

		return implementation;
	}

	private static bool IsLoggerInterface(INamedTypeSymbol type)
		=> type is
		   {
			   Name: "ILogger",
			   TypeArguments.Length: 0,
			   ContainingNamespace:
			   {
				   Name: "Logging",
				   ContainingNamespace:
				   {
					   Name: "Extensions",
					   ContainingNamespace: { Name: "Microsoft", ContainingNamespace.IsGlobalNamespace: true, },
				   },
			   },
		   };
}
