using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that reports <c>MockBehavior.AutoMocking()</c> in a project without the <c>MockolateAutoMocking</c>
///     build property, where the source generator creates no auto-mocks, so the call has no effect.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoMockingAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_autoMockingRule = new(
		"Mockolate0005",
		new LocalizableResourceString(nameof(Resources.Mockolate0005Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0005MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Warning,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0005Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> ImmutableArray.Create(s_autoMockingRule);

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			if (compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
				    "build_property.MockolateAutoMocking", out string? value) &&
			    string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			compilationContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
			compilationContext.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
		});
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context)
	{
		if (context.Operation is IInvocationOperation { TargetMethod: { Name: "AutoMocking", } method, } invocation &&
		    IsMockBehavior(method.ContainingType) &&
		    !(invocation.Arguments.Length == 1 && IsFalse(invocation.Arguments[0].Value)))
		{
			context.ReportDiagnostic(Diagnostic.Create(s_autoMockingRule, invocation.Syntax.GetLocation()));
		}
	}

	/// <summary>
	///     Reports <c>AutoMock = true</c> in a <see langword="with" /> expression or an object initializer.
	/// </summary>
	private static void AnalyzeAssignment(OperationAnalysisContext context)
	{
		if (context.Operation is ISimpleAssignmentOperation
		    {
			    Target: IPropertyReferenceOperation { Property: { Name: "AutoMock", } property, },
		    } assignment &&
		    IsMockBehavior(property.ContainingType) &&
		    !IsMockBehavior(context.ContainingSymbol.ContainingType) &&
		    !IsFalse(assignment.Value))
		{
			context.ReportDiagnostic(Diagnostic.Create(s_autoMockingRule, assignment.Syntax.GetLocation()));
		}
	}

	private static bool IsMockBehavior(INamedTypeSymbol? type)
		=> type is
		{
			Name: "MockBehavior",
			ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, },
		};

	private static bool IsFalse(IOperation value)
		=> value.ConstantValue is { HasValue: true, Value: false, };
}
