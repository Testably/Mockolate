using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that reports a <see langword="null" /> or <see langword="default" /> literal passed to a
///     non-nullable <c>Mockolate.Parameters.IParameters</c> parameter, where it matches any parameters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullParametersAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	///     A <see langword="null" /> argument for a non-nullable <c>IParameters</c> parameter matches any parameters.
	/// </summary>
	/// <remarks>
	///     Declared here instead of in <see cref="Rules" />, because the release tracking analyzer reports RS2002 for an
	///     unshipped rule declared in another file.
	/// </remarks>
	public static readonly DiagnosticDescriptor NullParametersRule = new(
		"Mockolate0006",
		new LocalizableResourceString(nameof(Resources.Mockolate0006Title),
			Resources.ResourceManager, typeof(Resources)),
		new LocalizableResourceString(nameof(Resources.Mockolate0006MessageFormat),
			Resources.ResourceManager, typeof(Resources)),
		"Usage",
		DiagnosticSeverity.Warning,
		true,
		new LocalizableResourceString(nameof(Resources.Mockolate0006Description),
			Resources.ResourceManager, typeof(Resources)));

	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		NullParametersRule,
	];

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
	}

	// The generator declares the `IParameters` parameter nullable exactly when it forwards a lone `null` as the null
	// argument (`ForwardsLoneNull` in `Sources.MockClass.cs`), so the annotation alone tells the two cases apart.
	private static void AnalyzeArgument(OperationAnalysisContext context)
	{
		if (context.Operation is IArgumentOperation
		    {
			    Parameter: { NullableAnnotation: NullableAnnotation.NotAnnotated, Type: { } parameterType, },
			    Value.Syntax: { } valueSyntax,
		    } &&
		    IsParameters(parameterType) &&
		    (valueSyntax.IsKind(SyntaxKind.NullLiteralExpression) ||
		     valueSyntax.IsKind(SyntaxKind.DefaultLiteralExpression)))
		{
			// The operation skips parentheses and `!`, which the code fix replaces as well.
			while (valueSyntax.Parent is ParenthesizedExpressionSyntax or PostfixUnaryExpressionSyntax
			       {
				       RawKind: (int)SyntaxKind.SuppressNullableWarningExpression,
			       })
			{
				valueSyntax = valueSyntax.Parent;
			}

			context.ReportDiagnostic(Diagnostic.Create(NullParametersRule, valueSyntax.GetLocation(),
				valueSyntax.ToString()));
		}
	}

	private static bool IsParameters(ITypeSymbol type)
		=> type is
		{
			Name: "IParameters",
			ContainingNamespace:
			{
				Name: "Parameters",
				ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, },
			},
		};
}
