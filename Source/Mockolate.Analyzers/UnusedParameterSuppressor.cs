using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mockolate.Analyzers;

/// <summary>
///     A suppressor that silences the unused parameter diagnostics IDE0060 and xUnit1026 for a parameter of a test method
///     with an attribute that implements <c>Mockolate.Internals.IMockDataSource</c>, when the parameter is <c>[AsDefault]</c>,
///     because the other mocks of the test case may return it, or when its value comes from <c>[ValueFrom]</c> or a value
///     provider and it is not <c>[NotShared]</c>, because a class under test may receive it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedParameterSuppressor : DiagnosticSuppressor
{
	/// <inheritdoc />
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
		Rules.UnusedParameterSuppressions;

	/// <inheritdoc />
	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
		{
			if (IsUsedWithoutReference(context, diagnostic))
			{
				// The compiler only passes diagnostics with the ID of a supported suppression.
				SuppressionDescriptor suppression = Rules.UnusedParameterSuppressions
					.First(descriptor => string.Equals(descriptor.SuppressedDiagnosticId, diagnostic.Id, StringComparison.Ordinal));
				context.ReportSuppression(Suppression.Create(suppression, diagnostic));
			}
		}
	}

	private static bool IsUsedWithoutReference(SuppressionAnalysisContext context, Diagnostic diagnostic)
		=> diagnostic.Location.SourceTree is { } tree &&
		   tree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan)
			   .FirstAncestorOrSelf<ParameterSyntax>() is { } parameterSyntax &&
		   context.GetSemanticModel(tree).GetDeclaredSymbol(parameterSyntax, context.CancellationToken) is
			   IParameterSymbol { ContainingSymbol: IMethodSymbol method, } parameter &&
		   (MockDataSourceAnalyzer.HasMarker(parameter, "AsDefaultParameterAttribute") ||
		    (HasValueSource(parameter) && !MockDataSourceAnalyzer.HasMarker(parameter, "NotSharedParameterAttribute"))) &&
		   method.GetAttributes().Any(MockDataSourceAnalyzer.IsMockDataSourceAttribute);

	private static bool HasValueSource(IParameterSymbol parameter)
		=> MockDataSourceAnalyzer.HasMarker(parameter, "ValueFromParameterAttribute") ||
		   MockDataSourceAnalyzer.GetValueProviders(parameter).Length > 0;
}
