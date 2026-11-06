using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using aweXpect.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

/// <summary>
///     Snapshot coverage for the source generator output. Each scenario mirrors a fact in
///     <c>Mockolate.Tests.MockCreationTests</c> and reuses the same
///     <c>Tests/Mockolate.Tests/GeneratorCoverage</c> source files as input, so the
///     example types remain the single source of truth for "every special case in the
///     source generator". Every generated <c>.g.cs</c> file is compared with an aweXpect snapshot in
///     <c>MockGenerationSnapshotTests/GeneratorOutput_MatchesExpectedSnapshot/</c>.
///     When a scenario fails because the change was intentional, run the tests with the environment variable
///     <c>AWEXPECT_ACCEPT_SNAPSHOTS</c> set to <c>all</c>.
/// </summary>
public sealed partial class MockGenerationSnapshotTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, GeneratedScenario>> Generated = new(GenerateAll);

    [GeneratedRegex(@"[ \t]*\[global::System\.Diagnostics\.DebuggerNonUserCode\]\r?\n?")]
    private static partial Regex DebuggerNonUserCodeRegex { get; }

    [Test]
    [MethodDataSource(nameof(ScenarioNames))]
    public async Task GeneratorOutput_MatchesExpectedSnapshot(string scenarioName)
    {
        var scenario = Generated.Value[scenarioName];
        await That(scenario.Diagnostics).IsEmpty();

        await ThatAll(scenario.Files
            .Select(file => (Expectation)That(file.Content).MatchesSnapshot().Named(file.SnapshotName).WithExtension("cs"))
            .ToArray());
    }

    internal static readonly IReadOnlyList<SnapshotScenario> Scenarios =
    [
        new(
            "BaseClass_WithMultipleAdditionalInterfaces_CanBeCreated",
            ["ComprehensiveAbstractClass.cs", "MyAbstractBase.cs", "ICombinationMockA.cs", "ICombinationMockB.cs",],
            """
            ComprehensiveAbstractClass sut = ComprehensiveAbstractClass.CreateMock()
            	.Implementing<ICombinationMockA>()
            	.Implementing<ICombinationMockB>();
            """,
            []),
        new(
            "ComprehensiveAbstractClass_CanBeCreated",
            ["ComprehensiveAbstractClass.cs", "MyAbstractBase.cs",],
            """
            ComprehensiveAbstractClass sut = ComprehensiveAbstractClass.CreateMock();
            """,
            []),
        new(
            "ComprehensiveDelegate_CanBeCreated",
            ["ComprehensiveDelegate.cs",],
            """
            ComprehensiveDelegate sut = ComprehensiveDelegate.CreateMock();
            """,
            []),
        new(
            "ComprehensiveInterface_CanBeCreated",
            [
                "ComprehensiveDelegate.cs", "IComprehensiveInterface.cs",
                "MyBase.cs", "MyEnum.cs", "MyEventArgs.cs", "MyStruct.cs",
            ],
            """
            IComprehensiveInterface sut = IComprehensiveInterface.CreateMock();
            """,
            []),
        new(
            "HttpClient_CanBeCreated",
            [],
            """
            System.Net.Http.HttpClient sut = System.Net.Http.HttpClient.CreateMock();
            """,
            [typeof(HttpClient), typeof(HttpStatusCode),]),
        new(
            "KeywordEdgeCases_CanBeCreated",
            ["IKeywordEdgeCases.cs",],
            """
            IKeywordEdgeCases sut = IKeywordEdgeCases.CreateMock();
            """,
            []),
        new(
            "ParameterNameCollisions_CanBeCreated",
            ["ParameterNameCollisions.cs",],
            """
            IParameterNameCollisions sut1 = IParameterNameCollisions.CreateMock();
            ParameterNameCollisionsBase sut2 = ParameterNameCollisionsBase.CreateMock();
            ParameterNameCollisionsDelegate sut3 = ParameterNameCollisionsDelegate.CreateMock();
            """,
            []),
        new(
            "RefStructConsumer_CanBeCreated",
            ["IRefStructConsumer.cs", "Packet.cs",],
            """
            IRefStructConsumer sut = IRefStructConsumer.CreateMock();
            """,
            []),
        new(
            "StaticAbstractMembers_CanBeCreated",
            ["IStaticAbstractMembers.cs",],
            """
            IStaticAbstractMembers sut = IStaticAbstractMembers.CreateMock();
            """,
            []),
        new(
            "ComprehensiveDelegate_CanBeCreated_Unions",
            ["ComprehensiveDelegate.cs",],
            """
            ComprehensiveDelegate sut = ComprehensiveDelegate.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "ComprehensiveInterface_CanBeCreated_Unions",
            [
                "ComprehensiveDelegate.cs", "IComprehensiveInterface.cs",
                "MyBase.cs", "MyEnum.cs", "MyEventArgs.cs", "MyStruct.cs",
            ],
            """
            IComprehensiveInterface sut = IComprehensiveInterface.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "KeywordEdgeCases_CanBeCreated_Unions",
            ["IKeywordEdgeCases.cs",],
            """
            IKeywordEdgeCases sut = IKeywordEdgeCases.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "ParameterNameCollisions_CanBeCreated_Unions",
            ["ParameterNameCollisions.cs",],
            """
            IParameterNameCollisions sut1 = IParameterNameCollisions.CreateMock();
            ParameterNameCollisionsBase sut2 = ParameterNameCollisionsBase.CreateMock();
            ParameterNameCollisionsDelegate sut3 = ParameterNameCollisionsDelegate.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "UnionIndexers_CanBeCreated_Unions",
            ["IUnionIndexers.cs",],
            """
            IUnionIndexers sut = IUnionIndexers.CreateMock();
            """,
            [],
            UnionMode: true),
    ];

    public static IEnumerable<string> ScenarioNames()
        => Scenarios.Select(scenario => scenario.Name);

    /// <summary>
    ///     Runs the generator for every scenario, so that scenarios with the same content of a generated file share its
    ///     snapshot. It is named after the file, or, when the scenarios generate several contents of the file, after the
    ///     first scenario with each content, e.g. <c>MethodSetups.ComprehensiveInterface_CanBeCreated.g</c>.
    /// </summary>
    /// <remarks>
    ///     <c>[DebuggerNonUserCode]</c> is only emitted by a Release build of the generator, so it is stripped to make the
    ///     snapshots independent of the build configuration.
    /// </remarks>
    private static IReadOnlyDictionary<string, GeneratedScenario> GenerateAll()
    {
        Dictionary<string, GeneratorResult> results = Scenarios.ToDictionary(s => s.Name, RunGenerator);
        Dictionary<(string FileName, string Content), string> snapshotNames = new();
        foreach (var file in results
                     .SelectMany(result => result.Value.Sources.Select(source =>
                         (Scenario: result.Key, FileName: source.Key, Content: Normalize(source.Value))))
                     .GroupBy(source => source.FileName))
        {
            var variants = file
                .GroupBy(source => source.Content)
                .Select(variant => (variant.Key, Scenario: variant.Select(source => source.Scenario).Min(StringComparer.Ordinal)!))
                .ToList();
            var name = Path.GetFileNameWithoutExtension(file.Key);
            foreach (var variant in variants)
                snapshotNames[(file.Key, variant.Key)] = variants.Count == 1
                    ? name
                    : $"{Path.GetFileNameWithoutExtension(name)}.{variant.Scenario}{Path.GetExtension(name)}";
        }

        return results.ToDictionary(result => result.Key, result => new GeneratedScenario(
            result.Value.Diagnostics,
            result.Value.Sources
                .OrderBy(source => source.Key, StringComparer.Ordinal)
                .Select(source =>
                {
                    var content = Normalize(source.Value);
                    return new GeneratedFile(snapshotNames[(source.Key, content)], content);
                })
                .ToArray()));

        static string Normalize(string content)
            => DebuggerNonUserCodeRegex.Replace(content.Replace("\r\n", "\n"), string.Empty);
    }

    private static GeneratorResult RunGenerator(SnapshotScenario scenario)
    {
        List<string> sources = new();
        foreach (var coverageFile in scenario.CoverageFiles) sources.Add("#nullable enable\n" + ReadCoverageFile(coverageFile));

        var usingDirective = scenario.CoverageFiles.Length > 0
            ? "using Mockolate.Tests.GeneratorCoverage;\n"
            : string.Empty;
        var program = $$"""
                        #nullable enable
                        using System;
                        using Mockolate;
                        {{usingDirective}}
                        namespace Mockolate.SourceGenerators.Tests.SnapshotDriver;

                        public class Program
                        {
                        	public static void Main(string[] args)
                        	{
                        		{{scenario.MainBody}}
                        	}
                        }
                        """;
        sources.Add(program);

        // Classic scenarios are pinned to C# 14 so that they keep describing the classic overload set once the
        // compiler ships C# 15 and union support is detected automatically. Union scenarios opt in through the
        // MockolateUnionParameters property, because the pinned test Roslyn predates C# 15 (see UnionParameterArgTests).
        if (scenario.UnionMode)
        {
            return Generator.Run(
                sources.ToArray(),
                ["NET9_0_OR_GREATER", "NET10_0_OR_GREATER", "NET11_0_OR_GREATER",],
                LanguageVersion.Preview,
                new Dictionary<string, string> { ["build_property.MockolateUnionParameters"] = "true", },
                scenario.AssemblyTypes);
        }

        return Generator.Run(
            sources.ToArray(),
            ["NET9_0_OR_GREATER", "NET10_0_OR_GREATER",],
            LanguageVersion.CSharp14,
            null,
            scenario.AssemblyTypes);
    }

    private static string ReadCoverageFile(string coverageFileName, [CallerFilePath] string path = "")
        => File.ReadAllText(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "Mockolate.Tests", "GeneratorCoverage",
            coverageFileName));

    private sealed record GeneratedScenario(string[] Diagnostics, GeneratedFile[] Files);

    private sealed record GeneratedFile(string SnapshotName, string Content);
}
