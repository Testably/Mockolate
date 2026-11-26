using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Mockolate.Analyzers.Tests.Verifiers;

public static class CSharpSuppressorVerifier<TSuppressor>
	where TSuppressor : DiagnosticSuppressor, new()
{
	/// <summary>
	///     An unused parameter warning with the <paramref name="diagnosticId" /> at the location, which the suppressor must
	///     suppress or not.
	/// </summary>
	public static DiagnosticResult UnusedParameter(string diagnosticId, int location, bool isSuppressed)
		=> UnusedParameter(diagnosticId, isSuppressed).WithLocation(location);

	/// <summary>
	///     An unused parameter warning with the <paramref name="diagnosticId" /> without a location, which the suppressor
	///     must suppress or not.
	/// </summary>
	public static DiagnosticResult UnusedParameter(string diagnosticId, bool isSuppressed)
		=> new DiagnosticResult(diagnosticId, DiagnosticSeverity.Warning).WithIsSuppressed(isSuppressed);

	/// <summary>
	///     Verifies the suppressions in the <paramref name="source" />, in which a stand-in analyzer reports the
	///     <paramref name="diagnosticId" /> for every parameter.
	/// </summary>
	public static async Task VerifySuppressorAsync(string diagnosticId, [StringSyntax("c#-test")] string source,
		params DiagnosticResult[] expected)
	{
		Test test = new(diagnosticId)
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

	public class Test : CSharpAnalyzerTest<TSuppressor, DefaultVerifier>
	{
		private readonly string _diagnosticId;

		public Test(string diagnosticId)
		{
			_diagnosticId = diagnosticId;
			SolutionTransforms.Add((solution, projectId) =>
				solution.GetProject(projectId)?.ParseOptions is CSharpParseOptions parseOptions
					? solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.Preview))
					: solution);
		}

		/// <inheritdoc />
		protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
			=> [..base.GetDiagnosticAnalyzers(), new UnusedParameterAnalyzer(_diagnosticId),];
	}

	// The stand-in is never shipped, so the rules for analyzer releases and assemblies do not apply.
#pragma warning disable RS1017, RS1036, RS1038, RS1041, RS2008
	/// <summary>
	///     Reports the diagnostic for every parameter of an ordinary method or an indexer, so that the tests need no
	///     reference to the analyzers that report unused parameters. For a parameter named <c>atMethod</c> it reports at the
	///     method instead, and for one named <c>noLocation</c> without a location.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	private sealed class UnusedParameterAnalyzer(string diagnosticId) : DiagnosticAnalyzer
	{
		private readonly DiagnosticDescriptor _rule = new(diagnosticId, "Remove unused parameter",
			"Remove unused parameter '{0}'", "Usage", DiagnosticSeverity.Warning, true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule,];

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction(symbolContext =>
			{
				ImmutableArray<IParameterSymbol> parameters = symbolContext.Symbol switch
				{
					IMethodSymbol { MethodKind: MethodKind.Ordinary, } method => method.Parameters,
					IPropertySymbol property => property.Parameters,
					_ => [],
				};
				foreach (IParameterSymbol parameter in parameters)
				{
					Location location = parameter.Name switch
					{
						"atMethod" => symbolContext.Symbol.Locations[0],
						"noLocation" => Location.None,
						_ => parameter.Locations[0],
					};
					symbolContext.ReportDiagnostic(Diagnostic.Create(_rule, location, parameter.Name));
				}
			}, SymbolKind.Method, SymbolKind.Property);
		}
	}
#pragma warning restore RS1017, RS1036, RS1038, RS1041, RS2008
}
