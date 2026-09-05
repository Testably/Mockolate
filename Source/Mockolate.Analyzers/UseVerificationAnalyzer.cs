using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Mockolate.Analyzers;

/// <summary>
///     An analyzer that checks that all <c>VerificationResult</c> invocations are properly used.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseVerificationAnalyzer : DiagnosticAnalyzer
{
	/// <inheritdoc cref="DiagnosticAnalyzer.SupportedDiagnostics" />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		Rules.UseVerificationRule,
	];

	/// <inheritdoc cref="DiagnosticAnalyzer.Initialize(AnalysisContext)" />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
	}

	private static void AnalyzeOperation(OperationAnalysisContext context)
	{
		if (context.Operation is IInvocationOperation invocationOperation)
		{
			ITypeSymbol? returnType = invocationOperation.Type;
			if (returnType is INamedTypeSymbol namedReturnType && IsVerificationResult(namedReturnType))
			{
				CheckIsUsed(context, invocationOperation);
			}
		}
	}

	/// <summary>
	///     Matches <c>Mockolate.Verify.VerificationResult&lt;T&gt;</c> and its nested <c>IgnoreParameters</c>, which the
	///     value and the union-typed verify overloads return.
	/// </summary>
	private static bool IsVerificationResult(INamedTypeSymbol type)
	{
		INamedTypeSymbol candidate = type is { Name: "IgnoreParameters", ContainingType: { } containingType, }
			? containingType
			: type;
		return candidate.Name == "VerificationResult" &&
		       candidate.ContainingNamespace?.Name == "Verify" &&
		       candidate.ContainingNamespace.ContainingNamespace?.Name == "Mockolate" &&
		       candidate.ContainingNamespace.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
	}

	private static void CheckIsUsed(OperationAnalysisContext context, IInvocationOperation invocationOperation)
	{
		if (IsOperationUsed(invocationOperation))
		{
			return;
		}

		context.ReportDiagnostic(
			Diagnostic.Create(Rules.UseVerificationRule, context.Operation.Syntax.GetLocation())
		);
	}

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
	private static bool IsOperationUsed(IInvocationOperation invocationOperation)
	{
		IOperation? parent = invocationOperation.Parent;

		// Walk up the parent chain
		while (parent != null)
		{
			if (parent is
			    // Any assignment (including to variables, fields, properties, etc.)
			    IAssignmentOperation or
			    // Any variable initializer (e.g., var x = ...)
			    IVariableInitializerOperation or
			    // Used as an argument to another invocation)
			    IArgumentOperation or
			    // Used in a return statement
			    IReturnOperation or
			    // Used in a conditional expression
			    IConditionalOperation or
			    // Used in a member access or chained invocation (e.g. .Once(), .AtLeastOnce())
			    IInvocationOperation or IMemberReferenceOperation)
			{
				return true;
			}

			// If the parent is an ExpressionStatement, check if the operation is the same as the invocationOperation
			if (parent is IExpressionStatementOperation expr && expr.Operation != invocationOperation)
			{
				return true;
			}

			// If the parent is a block operation, we've reached the top of the statement without finding a usage
			if (parent is IBlockOperation)
			{
				break;
			}

			parent = parent.Parent;
		}

		return false;
	}
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
}
