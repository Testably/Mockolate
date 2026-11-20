using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mockolate.Analyzers.CodeFixers;

/// <summary>
///     A code fix provider that replaces a <see langword="null" /> argument for <c>IParameters</c> with
///     <c>Match.AnyParameters()</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullParametersCodeFixProvider))]
[Shared]
public class NullParametersCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc />
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
		[NullParametersAnalyzer.NullParametersRule.Id,];

	/// <inheritdoc />
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc />
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (root is null || semanticModel is null)
		{
			return;
		}

		foreach (Diagnostic diagnostic in context.Diagnostics)
		{
			if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax
			    literal)
			{
				continue;
			}

			ExpressionSyntax anyParameters = SyntaxFactory
				.ParseExpression(MatchSpelling(semanticModel, literal.SpanStart) + ".AnyParameters()")
				.WithTriviaFrom(literal);
			context.RegisterCodeFix(
				CodeAction.Create(
					Resources.Mockolate0006CodeFixTitle,
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(literal, anyParameters))),
					nameof(Resources.Mockolate0006CodeFixTitle)),
				diagnostic);
		}
	}

	private static string MatchSpelling(SemanticModel semanticModel, int position)
	{
		INamedTypeSymbol? match = semanticModel.Compilation.GetTypeByMetadataName("Mockolate.Match");
		return semanticModel.LookupNamespacesAndTypes(position, name: "Match")
			.Any(symbol => SymbolEqualityComparer.Default.Equals(symbol, match))
			? "Match"
			: "global::Mockolate.Match";
	}
}
