using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Mockolate.Analyzers.Tests.Verifiers;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	/// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic" />
	public static DiagnosticResult Diagnostic()
		=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

	/// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic" />
	public static DiagnosticResult Diagnostic(string diagnosticId)
		=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

	/// <inheritdoc cref="Microsoft.CodeAnalysis.Diagnostic" />
	public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
		=> CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

	/// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])" />
	public static async Task VerifyAnalyzerAsync([StringSyntax("c#-test")] string source,
		params DiagnosticResult[] expected)
	{
		Test test = new()
		{
			TestCode = source,
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

	public static async Task VerifyAnalyzerWithReferencedProjectAsync([StringSyntax("c#-test")] string source,
		[StringSyntax("c#-test")] string externalSource,
		params DiagnosticResult[] expected)
	{
		Test test = new()
		{
			TestCode = source,
			TestState =
			{
				AdditionalReferences =
				{
					typeof(MockBehavior).Assembly.Location,
				},
				AdditionalProjects =
				{
					["ExternalProject"] =
					{
						Sources = { ("/ExternalProject/External.cs", externalSource), },
					},
				},
				AdditionalProjectReferences = { "ExternalProject", },
			},
		};

		// TODO: Remove once CS1705 no longer occurs
		test.CompilerDiagnostics = CompilerDiagnostics.None;
		test.ExpectedDiagnostics.AddRange(expected);
		await test.RunAsync(CancellationToken.None);
	}

	/// <summary>
	///     Overload that pins the compilation's <see cref="LanguageVersion" /> to a specific
	///     value, overriding the default <see cref="LanguageVersion.Preview" /> selection in
	///     <see cref="Test" />. Used to exercise analyzer branches that react to the effective
	///     C# language version (e.g. the <c>allows ref struct</c> availability gate).
	/// </summary>
	public static async Task VerifyAnalyzerAsync([StringSyntax("c#-test")] string source,
		LanguageVersion languageVersion,
		params DiagnosticResult[] expected)
	{
		Test test = new()
		{
			TestCode = source,
			TestState =
			{
				AdditionalReferences =
				{
					typeof(MockBehavior).Assembly.Location,
				},
			},
		};

		test.CompilerDiagnostics = CompilerDiagnostics.None;
		test.SolutionTransforms.Add((solution, projectId) =>
		{
			Project? project = solution.GetProject(projectId);
			if (project?.ParseOptions is CSharpParseOptions parseOptions)
			{
				solution = solution.WithProjectParseOptions(projectId,
					parseOptions.WithLanguageVersion(languageVersion));
			}

			return solution;
		});
		test.ExpectedDiagnostics.AddRange(expected);
		await test.RunAsync(CancellationToken.None);
	}
}
