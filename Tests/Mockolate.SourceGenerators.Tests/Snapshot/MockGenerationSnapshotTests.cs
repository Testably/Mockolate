using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

/// <summary>
///     Snapshot coverage for the source generator output. Each scenario mirrors a fact in
///     <c>Mockolate.Tests.MockCreationTests</c> and reuses the same
///     <c>Tests/Mockolate.Tests/GeneratorCoverage</c> source files as input, so the
///     example types remain the single source of truth for "every special case in the
///     source generator". The full set of generated <c>.g.cs</c> files is diffed against
///     <c>Tests/Mockolate.SourceGenerators.Tests/Snapshot/Expected/&lt;scenario&gt;/</c>; files identical across
///     scenarios are stored once under <c>Expected/_Shared/</c> and listed in the scenario's <c>_shared.txt</c>.
///     When a scenario fails because the change was intentional, run
///     <see cref="MockGenerationSnapshotAcceptance.AcceptSnapshotChanges" />.
/// </summary>
public sealed class MockGenerationSnapshotTests
{
    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public async Task GeneratorOutput_MatchesExpectedSnapshot(string scenarioName)
    {
        var scenario = Scenarios.Single(s => s.Name == scenarioName);
        var result = RunGenerator(scenario);
        await That(result.Diagnostics).IsEmpty();

        var generated = NormalizeSources(result);
        var expected = SnapshotStorage.GetExpected(scenarioName);

        await That(generated.Keys).IsEqualTo(expected.Keys).InAnyOrder();

        foreach (var fileName in expected.Keys)
            await That(SnapshotStorage.StripConfigSpecificLines(generated[fileName]))
                .IsEqualTo(SnapshotStorage.StripConfigSpecificLines(expected[fileName]))
                .IgnoringNewlineStyle();
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
            "UnionIndexers_CanBeCreated_Unions",
            ["IUnionIndexers.cs",],
            """
            IUnionIndexers sut = IUnionIndexers.CreateMock();
            """,
            [],
            UnionMode: true),
    ];

    public static TheoryData<string> ScenarioNames
    {
        get
        {
            TheoryData<string> data = new();
            foreach (var scenario in Scenarios) data.Add(scenario.Name);

            return data;
        }
    }

    internal static GeneratorResult RunGenerator(SnapshotScenario scenario)
    {
        List<string> sources = new();
        foreach (var coverageFile in scenario.CoverageFiles) sources.Add("#nullable enable\n" + SnapshotStorage.ReadCoverageFile(coverageFile));

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

    internal static IReadOnlyDictionary<string, string> NormalizeSources(GeneratorResult result)
    {
        Dictionary<string, string> normalized = new();
        foreach (var source in result.Sources
                     .OrderBy(s => s.Key, StringComparer.Ordinal))
            normalized[source.Key] = source.Value.Replace("\r\n", "\n");

        return normalized;
    }
}
