using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

public static class OverloadMatrix
{
	private const string Runner = """
	                              public static class Runner
	                              {
	                                  public static string Setup<T>(Func<T> create, Action<T> setup, (string Label, Func<T, int> Call)[] probes)
	                                  {
	                                      T sut = create();
	                                      try
	                                      {
	                                          setup(sut);
	                                      }
	                                      catch (Exception exception)
	                                      {
	                                          return "throws " + exception.GetType().Name;
	                                      }

	                                      return Describe(probes.Where(probe => probe.Call(sut) == 1).Select(probe => probe.Label).ToArray(), probes.Length);
	                                  }

	                                  public static string Verify<T>(Func<T> create, Action<T> verify, (string Label, Func<T, int> Call)[] probes)
	                                  {
	                                      List<string> matched = new();
	                                      foreach ((string Label, Func<T, int> Call) probe in probes)
	                                      {
	                                          T sut = create();
	                                          probe.Call(sut);
	                                          try
	                                          {
	                                              verify(sut);
	                                              matched.Add(probe.Label);
	                                          }
	                                          catch (Mockolate.Exceptions.MockVerificationException)
	                                          {
	                                          }
	                                          catch (Exception exception)
	                                          {
	                                              return "throws " + exception.GetType().Name;
	                                          }
	                                      }

	                                      return Describe(matched, probes.Length);
	                                  }

	                                  private static string Describe(IReadOnlyCollection<string> labels, int probeCount)
	                                      => labels.Count == 0 ? "matches nothing"
	                                          : labels.Count == probeCount ? "matches everything"
	                                          : "matches " + string.Join(" ", labels);
	                              }
	                              """;

	public static string[] Run(string declarations, IReadOnlyList<(Member Member, string Call)> calls,
		LanguageVersion languageVersion)
	{
		CSharpParseOptions parseOptions = new(languageVersion);
		Dictionary<string, string> bindings = Bind(declarations, calls, parseOptions, [], out Compilation _);
		HashSet<string> failed = [..bindings.Where(x => x.Value.StartsWith("CS")).Select(x => x.Key),];
		Bind(declarations, calls, parseOptions, failed, out Compilation compilation);

		using MemoryStream stream = new();
		EmitResult emitResult = compilation.Emit(stream);
		if (!emitResult.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics
				.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
				.Select(diagnostic => diagnostic.ToString())));
		}

		Type cases = Assembly.Load(stream.ToArray()).GetType("MyCode.Cases")!;
		string[] results = new string[calls.Count];
		for (int i = 0; i < calls.Count; i++)
		{
			string setup = Describe(cases, bindings, $"S{i}");
			string verify = Describe(cases, bindings, $"V{i}");
			results[i] = setup == verify ? setup : $"Setup: {setup}; Verify: {verify}";
		}

		return results;
	}

	private static string Describe(Type cases, Dictionary<string, string> bindings, string method)
		=> bindings[method].StartsWith("CS")
			? bindings[method]
			: $"{bindings[method]} {cases.GetMethod(method)!.Invoke(null, null)}";

	private static Dictionary<string, string> Bind(string declarations, IReadOnlyList<(Member Member, string Call)> calls,
		CSharpParseOptions parseOptions, HashSet<string> skipped, out Compilation outputCompilation)
	{
		StringBuilder sb = new();
		sb.AppendLine("using System;").AppendLine("using System.Collections.Generic;").AppendLine("using System.Linq;")
			.AppendLine("using Mockolate;").AppendLine("using Mockolate.Parameters;").AppendLine("using Mockolate.Verify;")
			.AppendLine("namespace MyCode;").AppendLine(declarations).AppendLine(Runner)
			.AppendLine("public static class Cases").AppendLine("{");
		List<Member> members = [];
		for (int i = 0; i < calls.Count; i++)
		{
			(Member member, string call) = calls[i];
			if (!members.Contains(member))
			{
				members.Add(member);
				sb.Append($"\tprivate static readonly (string, Func<{member.Type}, int>)[] Probes{members.Count - 1} = [");
				foreach ((string label, string invocation) in member.Probes)
				{
					sb.Append($"(\"{label}\", sut => {invocation}), ");
				}

				sb.AppendLine("];");
			}

			string probes = $"Probes{members.IndexOf(member)}";
			foreach ((string prefix, string kind, string suffix) in new[]
			         {
				         ("S", "Setup", ".Returns(1)"), ("V", "Verify", ".Once()"),
			         })
			{
				string access = member.Name is null ? kind : $"{kind}.{member.Name}";
				sb.AppendLine(skipped.Contains($"{prefix}{i}")
					? $"\tpublic static string {prefix}{i}() => \"\";"
					: $"\tpublic static string {prefix}{i}() => Runner.{kind}<{member.Type}>(() => {member.Type}.CreateMock(), sut => sut.Mock.{access}{call}{suffix}, {probes});");
			}
		}

		sb.AppendLine("}");

		SyntaxTree tree = CSharpSyntaxTree.ParseText(sb.ToString(), parseOptions);
		CSharpCompilation compilation = CSharpCompilation.Create(
			$"TestAssembly{Guid.NewGuid():N}",
			[tree,],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
				nullableContextOptions: NullableContextOptions.Enable));
		CSharpGeneratorDriver.Create([new MockGenerator().AsSourceGenerator(),], [], parseOptions)
			.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);

		SyntaxTree userTree = outputCompilation.SyntaxTrees.First();
		SemanticModel model = outputCompilation.GetSemanticModel(userTree);
		List<Diagnostic> errors = outputCompilation.GetDiagnostics()
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
		Dictionary<string, string> bindings = new();
		foreach (MethodDeclarationSyntax method in userTree.GetRoot().DescendantNodes()
			         .OfType<ClassDeclarationSyntax>().Single(c => c.Identifier.Text == "Cases").Members
			         .OfType<MethodDeclarationSyntax>())
		{
			bool IsInMethod(Diagnostic error)
				=> error.Location.SourceTree == userTree && method.Span.Contains(error.Location.SourceSpan);

			string[] methodErrors = errors.Where(IsInMethod).Select(error => error.Id).Distinct().ToArray();
			errors.RemoveAll(IsInMethod);
			if (methodErrors.Length > 0)
			{
				bindings[method.Identifier.Text] = string.Join(" ", methodErrors);
				continue;
			}

			InvocationExpressionSyntax? invocation = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
				.FirstOrDefault(x => x.Expression.ToString().StartsWith("sut.Mock.") &&
				                     !x.Expression.DescendantNodes().OfType<InvocationExpressionSyntax>().Any());
			bindings[method.Identifier.Text] = invocation is not null &&
			                                   model.GetSymbolInfo(invocation).Symbol is IMethodSymbol symbol
				? $"({string.Join(", ", symbol.Parameters.Select(p => (p.IsParams ? "params " : "") + p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))})"
				: "";
		}

		if (errors.Count > 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
		}

		return bindings;
	}

	public sealed record Member(string Type, string? Name, (string Label, string Invocation)[] Probes);
}
