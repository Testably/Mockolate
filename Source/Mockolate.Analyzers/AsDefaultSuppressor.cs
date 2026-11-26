using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     A suppressor that silences the unused parameter diagnostics IDE0060 and xUnit1026 for an <c>[AsDefault]</c>
///     parameter of a test method with an attribute that implements <c>Mockolate.Internals.IMockDataSource</c>,
///     because the other mocks of the test case may return the parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsDefaultSuppressor : DiagnosticSuppressor
{
	/// <inheritdoc />
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
		Rules.AsDefaultSuppressions;

	/// <inheritdoc />
	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
		{
			if (IsAsDefaultParameter(context, diagnostic))
			{
				// The compiler only passes diagnostics with the ID of a supported suppression.
				SuppressionDescriptor suppression = Rules.AsDefaultSuppressions
					.First(descriptor => descriptor.SuppressedDiagnosticId == diagnostic.Id);
				context.ReportSuppression(Suppression.Create(suppression, diagnostic));
			}
		}
	}

	private static bool IsAsDefaultParameter(SuppressionAnalysisContext context, Diagnostic diagnostic)
		=> diagnostic.Location.SourceTree is { } tree &&
		   tree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan)
			   .FirstAncestorOrSelf<ParameterSyntax>() is { } parameterSyntax &&
		   context.GetSemanticModel(tree).GetDeclaredSymbol(parameterSyntax, context.CancellationToken) is
			   IParameterSymbol { ContainingSymbol: IMethodSymbol method, } parameter &&
		   MockDataSourceAnalyzer.HasMarker(parameter, "AsDefaultParameterAttribute") &&
		   method.GetAttributes().Any(MockDataSourceAnalyzer.IsMockDataSourceAttribute);
}
