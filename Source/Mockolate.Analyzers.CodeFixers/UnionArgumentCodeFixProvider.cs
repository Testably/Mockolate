using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mockolate.Analyzers.CodeFixers;

/// <summary>
///     A code fix provider that adds the explicit parameter type to an argument of a union-typed setup or verify
///     parameter (<c>Mockolate.ParameterArg&lt;T&gt;</c>). A union conversion does not pass the parameter type on, so a
///     collection expression, a target-typed <c>new(…)</c> or a user-defined conversion only compiles with an explicit type.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnionArgumentCodeFixProvider))]
[Shared]
public class UnionArgumentCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc />
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CS1503", "CS1729", "CS9174",];

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
			ArgumentSyntax? argument = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
				.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();
			if (argument is null ||
			    argument.Expression.SpanStart != diagnostic.Location.SourceSpan.Start ||
			    GetUnionCaseType(semanticModel, argument, context.CancellationToken) is not { } type)
			{
				continue;
			}

			string typeName = type.ToMinimalDisplayString(semanticModel, argument.SpanStart);
			context.RegisterCodeFix(
				CodeAction.Create(
					string.Format(Resources.UnionArgumentCodeFixTitle, typeName),
					_ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(argument.Expression,
						AddExplicitType(argument.Expression, SyntaxFactory.ParseTypeName(typeName))))),
					nameof(Resources.UnionArgumentCodeFixTitle)),
				diagnostic);
		}
	}

	/// <summary>
	///     The case type <c>T</c> when <paramref name="argument" /> binds to a <c>Mockolate.ParameterArg&lt;T&gt;</c>
	///     parameter of a candidate and converts implicitly to <c>T</c>; otherwise <see langword="null" />, so that
	///     a genuinely mistyped argument gets no fix. A target-typed <c>new(…)</c> or collection expression has no type
	///     to classify, so it always takes <c>T</c>.
	/// </summary>
	private static ITypeSymbol? GetUnionCaseType(SemanticModel semanticModel, ArgumentSyntax argument,
		CancellationToken cancellationToken)
	{
		if (argument.Parent is not BaseArgumentListSyntax { Parent: { } call, } argumentList)
		{
			return null;
		}

		SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(call, cancellationToken);
		IEnumerable<ISymbol> candidates = symbolInfo.Symbol is null
			? symbolInfo.CandidateSymbols
			: [symbolInfo.Symbol,];
		int position = argumentList.Arguments.IndexOf(argument);
		foreach (ISymbol candidate in candidates)
		{
			IParameterSymbol? parameter = GetParameter(candidate, argument, position);
			if (parameter is not null &&
			    GetCaseType(parameter.Type) is { } caseType &&
			    (argument.Expression is ImplicitObjectCreationExpressionSyntax or CollectionExpressionSyntax ||
			     semanticModel.ClassifyConversion(argument.Expression, caseType).IsImplicit))
			{
				return caseType.IsReferenceType ? caseType.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : caseType;
			}
		}

		return null;
	}

	private static IParameterSymbol? GetParameter(ISymbol candidate, ArgumentSyntax argument, int position)
	{
		ImmutableArray<IParameterSymbol> parameters = candidate switch
		{
			IMethodSymbol method => method.Parameters,
			IPropertySymbol property => property.Parameters,
			_ => ImmutableArray<IParameterSymbol>.Empty,
		};
		if (argument.NameColon is { } nameColon)
		{
			return parameters.FirstOrDefault(p => p.Name == nameColon.Name.Identifier.ValueText);
		}

		return position < parameters.Length ? parameters[position] : null;
	}

	private static ITypeSymbol? GetCaseType(ITypeSymbol parameterType)
	{
		if (parameterType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, } nullable)
		{
			parameterType = nullable.TypeArguments[0];
		}

		return parameterType is INamedTypeSymbol
		{
			Name: "ParameterArg", Arity: 1, ContainingType: null,
			ContainingNamespace: { Name: "Mockolate", ContainingNamespace.IsGlobalNamespace: true, },
		} union
			? union.TypeArguments[0]
			: null;
	}

	private static ExpressionSyntax AddExplicitType(ExpressionSyntax expression, TypeSyntax type)
	{
		if (expression is ImplicitObjectCreationExpressionSyntax implicitNew)
		{
			return SyntaxFactory.ObjectCreationExpression(
					implicitNew.NewKeyword.WithTrailingTrivia(SyntaxFactory.Space), type, implicitNew.ArgumentList,
					implicitNew.Initializer)
				.WithTriviaFrom(expression);
		}

		ExpressionSyntax operand = expression is IdentifierNameSyntax or MemberAccessExpressionSyntax
			or InvocationExpressionSyntax or ElementAccessExpressionSyntax or LiteralExpressionSyntax
			or ObjectCreationExpressionSyntax or ParenthesizedExpressionSyntax or CollectionExpressionSyntax
			? expression.WithoutTrivia()
			: SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia());
		return SyntaxFactory.CastExpression(type, operand).WithTriviaFrom(expression);
	}
}
