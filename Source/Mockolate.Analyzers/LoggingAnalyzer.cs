using System;
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
///     The logging members extend <c>IMockSetup&lt;ILogger&gt;</c>, <c>IMockVerify&lt;ILogger&gt;</c> and
///     <c>IMockVerify&lt;ILoggerFactory&gt;</c>, which are covariant, so they also apply to the mock of a class that
///     implements one of these interfaces. A logger class only supports them when it can override <c>ILogger.Log</c>,
///     and a logger factory class does not support them.
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
isEnabledByDefault: true,
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
		    GetLoggingClass(method) is not { } loggingClass ||
		    context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type is not { } receiverType ||
		    GetUnsupportedClass(receiverType, string.Equals(loggingClass, "MockolateLoggerFactoryLogging", StringComparison.Ordinal)) is not { } unsupportedClass)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(s_loggingRule, memberAccess.Name.GetLocation(),
			unsupportedClass.ToDisplayString()));
	}

	/// <summary>
	///     The name of the generated class that declares the logging <paramref name="method" />, or
	///     <see langword="null" /> for any other method.
	/// </summary>
	private static string? GetLoggingClass(IMethodSymbol method)
	{
		for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.ContainingType)
		{
			if (type.Name is "MockolateLogging" or "MockolateLoggerFactoryLogging" &&
			    AnalyzerHelpers.IsMockolateNamespace(type.ContainingNamespace))
			{
				return type.Name;
			}
		}

		return null;
	}

	/// <summary>
	///     The mocked class behind the setup or verify facade <paramref name="receiverType" /> that does not support the
	///     logging members, or <see langword="null" /> when they are supported.
	/// </summary>
	/// <remarks>
	///     A logger factory is only supported as an interface. A logger class is supported when it can override
	///     <c>ILogger.Log</c>.
	/// </remarks>
	private static INamedTypeSymbol? GetUnsupportedClass(ITypeSymbol receiverType, bool isFactoryMember)
	{
		foreach (INamedTypeSymbol facade in new[] { receiverType, }.Concat(receiverType.AllInterfaces)
			         .OfType<INamedTypeSymbol>())
		{
			if (facade is { Name: "IMockSetup" or "IMockVerify", TypeArguments.Length: 1, } &&
			    facade.ContainingNamespace is { Name: "Setup" or "Verify", } facadeNamespace &&
			    AnalyzerHelpers.IsMockolateNamespace(facadeNamespace.ContainingNamespace) &&
			    facade.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Class, } mockedClass &&
			    IsUnsupported(mockedClass, isFactoryMember))
			{
				return mockedClass;
			}
		}

		return null;
	}

	private static bool IsUnsupported(INamedTypeSymbol mockedClass, bool isFactoryMember)
	{
		if (isFactoryMember)
		{
			return mockedClass.AllInterfaces.Any(@interface => IsLoggingInterface(@interface, "ILoggerFactory"));
		}

		if (mockedClass.AllInterfaces.FirstOrDefault(@interface => IsLoggingInterface(@interface, "ILogger")) is not
		    { } logger ||
		    logger.GetMembers("Log").OfType<IMethodSymbol>().FirstOrDefault() is not { } log)
		{
			return false;
		}

		return mockedClass.FindImplementationForInterfaceMember(log) is not IMethodSymbol implementation ||
		       GetMostDerivedOverride(mockedClass, implementation) is not
		       {
			       MethodKind: MethodKind.Ordinary, IsSealed: false,
		       } mostDerived ||
		       !(mostDerived.IsVirtual || mostDerived.IsAbstract || mostDerived.IsOverride);
	}

	/// <summary>
	///     The override of <paramref name="implementation" /> in the most derived type of <paramref name="mockedClass" />,
	///     which the mock would have to override in turn.
	/// </summary>
	/// <remarks>
	///     Mirrors <c>Class.GetAdapterBindings</c> in the generator, which decides on the same override and then dispatches
	///     the declaration that it overrides.
	/// </remarks>
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

	private static bool IsLoggingInterface(INamedTypeSymbol type, string name)
		=> string.Equals(type.Name, name, StringComparison.Ordinal) &&
		   type is
		   {
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
