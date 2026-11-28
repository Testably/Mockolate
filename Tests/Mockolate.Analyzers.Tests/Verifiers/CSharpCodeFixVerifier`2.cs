using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Mockolate.Analyzers.Tests.Verifiers;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
{
	/// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic" />
	public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
		=> CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

	/// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)" />
	public static async Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source,
		[StringSyntax("c#-test")] string fixedSource)
		=> await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

	/// <inheritdoc
	///     cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)" />
	public static async Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source, DiagnosticResult expected,
		[StringSyntax("c#-test")] string fixedSource)
		=> await VerifyCodeFixAsync(source, [expected,], fixedSource);

	/// <inheritdoc
	///     cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)" />
	public static async Task VerifyCodeFixAsync(
		[StringSyntax("c#-test")] string source,
		IEnumerable<DiagnosticResult> expected,
		[StringSyntax("c#-test")] string fixedSource
	)
	{
		Test test = new()
		{
			TestCode = source,
			FixedCode = fixedSource,
			ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
			TestState =
			{
				AdditionalReferences =
				{
					typeof(MockBehavior).Assembly.Location,
				},
			},
		};

		// TODO: Remove once CS1705 no longer occurs
		test.CompilerDiagnostics = CompilerDiagnostics.None;
		test.ExpectedDiagnostics.AddRange(expected);
		await test.RunAsync(CancellationToken.None);
	}
}
