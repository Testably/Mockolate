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
		});
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context)
	{
		if (context.Operation is not IInvocationOperation
		    {
			    TargetMethod:
			    {
				    Name: "AutoMocking",
				    ContainingType:
				    {
					    Name: "MockBehavior",
					    ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, },
				    },
			    },
		    } invocation)
		{
			return;
		}

		if (invocation.Arguments.Length == 1 &&
		    invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: false, })
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(s_autoMockingRule,
			invocation.Syntax.GetLocation()));
	}
}
