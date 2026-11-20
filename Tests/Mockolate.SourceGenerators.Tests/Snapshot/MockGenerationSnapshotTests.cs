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
using Microsoft.Extensions.Logging;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

/// <summary>
///     Snapshot coverage for the source generator output. Each scenario mirrors a fact in
///     <c>Mockolate.Tests.MockCreationTests</c> and reuses the same
///     <c>Tests/Mockolate.Tests/GeneratorCoverage</c> source files as input, so the
///     example types remain the single source of truth for "every special case in the
///     source generator". Every generated <c>.g.cs</c> file is compared with an aweXpect snapshot in
///     <c>MockGenerationSnapshotTests/GeneratorOutput_MatchesExpectedSnapshot/</c>.
///     When a scenario fails because the change was intentional, take over the change in the diff tool that opens
///     locally or run the tests with the environment variable <c>AWEXPECT_ACCEPT_SNAPSHOTS</c> set to <c>all</c>.
/// </summary>
public sealed partial class MockGenerationSnapshotTests
{
    private static readonly Lazy<Dictionary<string, GeneratedScenario>> Generated = new(GenerateAll);

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
            "AutoMock_CanBeCreated",
            ["IAutoMockRoot.cs", "Packet.cs",],
            """
            IAutoMockRoot sut1 = IAutoMockRoot.CreateMock();
            AutoMockChildFactory sut2 = AutoMockChildFactory.CreateMock();
            """,
            [],
            AutoMocking: true),
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
            "ClosedGeneric_CanBeCreated",
            ["IClosedGeneric.cs",],
            """
            IClosedGeneric<int> sut1 = IClosedGeneric<int>.CreateMock();
            ClosedGenericBase<string> sut2 = ClosedGenericBase<string>.CreateMock();
            ClosedGenericDelegate<int, string> sut3 = ClosedGenericDelegate<int, string>.CreateMock();
            IClosedGeneric<int> sut4 = IClosedGeneric<int>.CreateMock().Implementing<IClosedGeneric<string>>();
            IClosedGenericDerived sut5 = IClosedGenericDerived.CreateMock();
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
            "DynamicMembers_CanBeCreated",
            ["IDynamicMembers.cs",],
            """
            IDynamicMembers sut1 = IDynamicMembers.CreateMock();
            DynamicMembersBase sut2 = DynamicMembersBase.CreateMock();
            IDynamicOverloads sut3 = IDynamicOverloads.CreateMock();
            DynamicHidingDerived sut4 = DynamicHidingDerived.CreateMock();
            IDynamicRepository<dynamic> sut5 = IDynamicRepository<dynamic>.CreateMock();
            IDynamicRepository<System.Collections.Generic.List<dynamic>> sut6 = IDynamicRepository<System.Collections.Generic.List<dynamic>>.CreateMock();
            DynamicRepositoryBase<dynamic> sut7 = DynamicRepositoryBase<dynamic>.CreateMock(1);
            DynamicFactory<dynamic> sut8 = DynamicFactory<dynamic>.CreateMock();
            IDynamicMembers sut9 = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<dynamic>>();
            """,
            [typeof(DynamicAttribute),]),
        new(
            "EnumDefaultValues_CanBeCreated",
            ["IEnumDefaultValues.cs",],
            """
            IEnumDefaultValues sut1 = IEnumDefaultValues.CreateMock();
            EnumDefaultValuesDelegate sut2 = EnumDefaultValuesDelegate.CreateMock();
            EnumDefaultValuesBase sut3 = EnumDefaultValuesBase.CreateMock();
            """,
            []),
        new(
            "EqualsMembers_CanBeCreated",
            ["IEqualsMembers.cs",],
            """
            IEqualsOnly sut1 = IEqualsOnly.CreateMock();
            IEqualsOnlyDerived sut2 = IEqualsOnlyDerived.CreateMock();
            IEqualsNotNull sut3 = IEqualsNotNull.CreateMock();
            IEqualsAndGetHashCode sut4 = IEqualsAndGetHashCode.CreateMock();
            EqualsOnlyBase sut5 = EqualsOnlyBase.CreateMock();
            IEqualsOnly sut6 = IEqualsOnly.CreateMock().Implementing<IEqualsAndGetHashCode>();
            HiddenGetHashCodeBase sut7 = HiddenGetHashCodeBase.CreateMock();
            HiddenGetHashCodeBase sut8 = HiddenGetHashCodeBase.CreateMock().Implementing<IEqualsAndGetHashCode>();
            """,
            []),
        new(
            "GenericMethodClosedOverObject_CanBeCreated",
            ["IGenericMethodClosedOverObject.cs",],
            """
            IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
            """,
            []),
        new(
            "GenericMethodOverride_CanBeCreated",
            ["GenericMethodOverrideBase.cs",],
            """
            GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
            """,
            []),
        new(
            "GenericParameterDefaultValues_CanBeCreated",
            ["IGenericParameterDefaultValues.cs",],
            """
            IGenericParameterDefaultValues sut1 = IGenericParameterDefaultValues.CreateMock();
            GenericParameterDefaultValuesBase sut2 = GenericParameterDefaultValuesBase.CreateMock();
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
            IKeywordEdgeCases sut1 = IKeywordEdgeCases.CreateMock();
            KeywordEdgeCasesBase sut2 = KeywordEdgeCasesBase.CreateMock("class", 1);
            """,
            []),
        new(
            "Logging_CanBeCreated",
            [],
            """
            Microsoft.Extensions.Logging.ILogger sut1 = Microsoft.Extensions.Logging.ILogger.CreateMock();
            Microsoft.Extensions.Logging.ILogger<Program> sut2 = Microsoft.Extensions.Logging.ILogger<Program>.CreateMock();
            Microsoft.Extensions.Logging.ILoggerFactory sut3 = Microsoft.Extensions.Logging.ILoggerFactory.CreateMock();
            """,
            [typeof(ILogger),]),
        new(
            "MethodConstrainedToTypeArgument_CanBeCreated",
            ["IMethodConstrainedToTypeArgument.cs",],
            """
            IMethodConstrainedToTypeArgument<int> sut1 = IMethodConstrainedToTypeArgument<int>.CreateMock();
            IMethodConstrainedToTypeArgument<string> sut2 = IMethodConstrainedToTypeArgument<string>.CreateMock();
            MethodConstrainedToTypeArgumentBase<string> sut3 = MethodConstrainedToTypeArgumentBase<string>.CreateMock();
            """,
            []),
        new(
            "NullableMethodTypeParameter_CanBeCreated",
            ["INullableMethodTypeParameter.cs",],
            """
            INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();
            """,
            []),
        new(
            "NullableTypeParameter_CanBeCreated",
            ["INullableTypeParameter.cs",],
            """
            INullableTypeParameter<int> sut1 = INullableTypeParameter<int>.CreateMock();
            INullableTypeParameter<string> sut2 = INullableTypeParameter<string>.CreateMock();
            """,
            []),
        new(
            "ObjectLookAlikes_CanBeCreated",
            ["IObjectLookAlikes.cs",],
            """
            IObjectLookAlikes sut1 = IObjectLookAlikes.CreateMock();
            IObjectLookAlikesDerived sut2 = IObjectLookAlikesDerived.CreateMock();
            IVoidObjectLookAlikes sut3 = IVoidObjectLookAlikes.CreateMock();
            IRefObjectLookAlikes sut4 = IRefObjectLookAlikes.CreateMock();
            IObjectLookAlikeProperties sut5 = IObjectLookAlikeProperties.CreateMock();
            IStaticObjectLookAlikes sut6 = IStaticObjectLookAlikes.CreateMock();
            IVoidObjectLookAlikes sut7 = IVoidObjectLookAlikes.CreateMock().Implementing<IObjectLookAlikes>();
            """,
            []),
        new(
            "OptionalConstructorParameters_CanBeCreated",
            ["OptionalConstructorParameters.cs", "ParamsSpanConstructors.cs", "ICombinationMockA.cs",],
            """
            OptionalConstructorBase sut1 = OptionalConstructorBase.CreateMock();
            OptionalAndParameterlessConstructorBase sut2 = OptionalAndParameterlessConstructorBase.CreateMock();
            SeveralOptionalConstructorsBase sut3 = SeveralOptionalConstructorsBase.CreateMock();
            OptionalConstructorBase sut4 = OptionalConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            ParamsConstructorBase sut5 = ParamsConstructorBase.CreateMock();
            OptionalAndParamsConstructorBase sut6 = OptionalAndParamsConstructorBase.CreateMock();
            ParamsConstructorSelectionBase sut7 = ParamsConstructorSelectionBase.CreateMock();
            OptionalOverParamsConstructorBase sut8 = OptionalOverParamsConstructorBase.CreateMock();
            OptionalAndParamsConstructorBase sut9 = OptionalAndParamsConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            ParamsCollectionConstructorBase sut10 = ParamsCollectionConstructorBase.CreateMock();
            ParamsSpanConstructorBase sut11 = ParamsSpanConstructorBase.CreateMock();
            OnlyParamsSpanConstructorBase sut12 = OnlyParamsSpanConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            """,
            []),
        new(
            "OptionalIndexerKeys_CanBeCreated",
            ["IOptionalIndexerKeys.cs",],
            """
            IOptionalIndexerKeys sut1 = IOptionalIndexerKeys.CreateMock();
            IOptionalIndexerKeysGetterOnly sut2 = IOptionalIndexerKeysGetterOnly.CreateMock();
            IOptionalIndexerKeysSetterOnly sut3 = IOptionalIndexerKeysSetterOnly.CreateMock();
            IOptionalIndexerKeysWithSixKeys sut4 = IOptionalIndexerKeysWithSixKeys.CreateMock();
            IOptionalIndexerKeysWithSibling sut5 = IOptionalIndexerKeysWithSibling.CreateMock();
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
            "ParamsObjectArray_CanBeCreated",
            ["IParamsObjectArray.cs",],
            """
            IParamsObjectArray sut1 = IParamsObjectArray.CreateMock();
            ParamsObjectArrayDelegate sut2 = ParamsObjectArrayDelegate.CreateMock();
            ParamsObjectArrayOnlyDelegate sut3 = ParamsObjectArrayOnlyDelegate.CreateMock();
            ParamsArrayDelegate sut4 = ParamsArrayDelegate.CreateMock();
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
            "ClosedGeneric_CanBeCreated_Unions",
            ["IClosedGeneric.cs",],
            """
            IClosedGeneric<int> sut1 = IClosedGeneric<int>.CreateMock();
            ClosedGenericBase<string> sut2 = ClosedGenericBase<string>.CreateMock();
            ClosedGenericDelegate<int, string> sut3 = ClosedGenericDelegate<int, string>.CreateMock();
            IClosedGeneric<int> sut4 = IClosedGeneric<int>.CreateMock().Implementing<IClosedGeneric<string>>();
            IClosedGenericDerived sut5 = IClosedGenericDerived.CreateMock();
            """,
            [],
            UnionMode: true),
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
            "DynamicMembers_CanBeCreated_Unions",
            ["IDynamicMembers.cs",],
            """
            IDynamicMembers sut1 = IDynamicMembers.CreateMock();
            DynamicMembersBase sut2 = DynamicMembersBase.CreateMock();
            IDynamicOverloads sut3 = IDynamicOverloads.CreateMock();
            DynamicHidingDerived sut4 = DynamicHidingDerived.CreateMock();
            IDynamicRepository<dynamic> sut5 = IDynamicRepository<dynamic>.CreateMock();
            IDynamicRepository<System.Collections.Generic.List<dynamic>> sut6 = IDynamicRepository<System.Collections.Generic.List<dynamic>>.CreateMock();
            DynamicRepositoryBase<dynamic> sut7 = DynamicRepositoryBase<dynamic>.CreateMock(1);
            DynamicFactory<dynamic> sut8 = DynamicFactory<dynamic>.CreateMock();
            IDynamicMembers sut9 = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<dynamic>>();
            """,
            [typeof(DynamicAttribute),],
            UnionMode: true),
        new(
            "EnumDefaultValues_CanBeCreated_Unions",
            ["IEnumDefaultValues.cs",],
            """
            IEnumDefaultValues sut1 = IEnumDefaultValues.CreateMock();
            EnumDefaultValuesDelegate sut2 = EnumDefaultValuesDelegate.CreateMock();
            EnumDefaultValuesBase sut3 = EnumDefaultValuesBase.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "EqualsMembers_CanBeCreated_Unions",
            ["IEqualsMembers.cs",],
            """
            IEqualsOnly sut1 = IEqualsOnly.CreateMock();
            IEqualsOnlyDerived sut2 = IEqualsOnlyDerived.CreateMock();
            IEqualsNotNull sut3 = IEqualsNotNull.CreateMock();
            IEqualsAndGetHashCode sut4 = IEqualsAndGetHashCode.CreateMock();
            EqualsOnlyBase sut5 = EqualsOnlyBase.CreateMock();
            IEqualsOnly sut6 = IEqualsOnly.CreateMock().Implementing<IEqualsAndGetHashCode>();
            HiddenGetHashCodeBase sut7 = HiddenGetHashCodeBase.CreateMock();
            HiddenGetHashCodeBase sut8 = HiddenGetHashCodeBase.CreateMock().Implementing<IEqualsAndGetHashCode>();
            """,
            [],
            UnionMode: true),
        new(
            "GenericMethodClosedOverObject_CanBeCreated_Unions",
            ["IGenericMethodClosedOverObject.cs",],
            """
            IGenericMethodClosedOverObject sut = IGenericMethodClosedOverObject.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "GenericMethodOverride_CanBeCreated_Unions",
            ["GenericMethodOverrideBase.cs",],
            """
            GenericMethodOverrideBase sut = GenericMethodOverrideBase.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "GenericParameterDefaultValues_CanBeCreated_Unions",
            ["IGenericParameterDefaultValues.cs",],
            """
            IGenericParameterDefaultValues sut1 = IGenericParameterDefaultValues.CreateMock();
            GenericParameterDefaultValuesBase sut2 = GenericParameterDefaultValuesBase.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "KeywordEdgeCases_CanBeCreated_Unions",
            ["IKeywordEdgeCases.cs",],
            """
            IKeywordEdgeCases sut1 = IKeywordEdgeCases.CreateMock();
            KeywordEdgeCasesBase sut2 = KeywordEdgeCasesBase.CreateMock("class", 1);
            """,
            [],
            UnionMode: true),
        new(
            "MethodConstrainedToTypeArgument_CanBeCreated_Unions",
            ["IMethodConstrainedToTypeArgument.cs",],
            """
            IMethodConstrainedToTypeArgument<int> sut1 = IMethodConstrainedToTypeArgument<int>.CreateMock();
            IMethodConstrainedToTypeArgument<string> sut2 = IMethodConstrainedToTypeArgument<string>.CreateMock();
            MethodConstrainedToTypeArgumentBase<string> sut3 = MethodConstrainedToTypeArgumentBase<string>.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "NullableMethodTypeParameter_CanBeCreated_Unions",
            ["INullableMethodTypeParameter.cs",],
            """
            INullableMethodTypeParameter sut = INullableMethodTypeParameter.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "NullableTypeParameter_CanBeCreated_Unions",
            ["INullableTypeParameter.cs",],
            """
            INullableTypeParameter<int> sut1 = INullableTypeParameter<int>.CreateMock();
            INullableTypeParameter<string> sut2 = INullableTypeParameter<string>.CreateMock();
            """,
            [],
            UnionMode: true),
        new(
            "ObjectLookAlikes_CanBeCreated_Unions",
            ["IObjectLookAlikes.cs",],
            """
            IObjectLookAlikes sut1 = IObjectLookAlikes.CreateMock();
            IObjectLookAlikesDerived sut2 = IObjectLookAlikesDerived.CreateMock();
            IVoidObjectLookAlikes sut3 = IVoidObjectLookAlikes.CreateMock();
            IRefObjectLookAlikes sut4 = IRefObjectLookAlikes.CreateMock();
            IObjectLookAlikeProperties sut5 = IObjectLookAlikeProperties.CreateMock();
            IStaticObjectLookAlikes sut6 = IStaticObjectLookAlikes.CreateMock();
            IVoidObjectLookAlikes sut7 = IVoidObjectLookAlikes.CreateMock().Implementing<IObjectLookAlikes>();
            """,
            [],
            UnionMode: true),
        new(
            "OptionalConstructorParameters_CanBeCreated_Unions",
            ["OptionalConstructorParameters.cs", "ParamsSpanConstructors.cs", "ICombinationMockA.cs",],
            """
            OptionalConstructorBase sut1 = OptionalConstructorBase.CreateMock();
            OptionalAndParameterlessConstructorBase sut2 = OptionalAndParameterlessConstructorBase.CreateMock();
            SeveralOptionalConstructorsBase sut3 = SeveralOptionalConstructorsBase.CreateMock();
            OptionalConstructorBase sut4 = OptionalConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            ParamsConstructorBase sut5 = ParamsConstructorBase.CreateMock();
            OptionalAndParamsConstructorBase sut6 = OptionalAndParamsConstructorBase.CreateMock();
            ParamsConstructorSelectionBase sut7 = ParamsConstructorSelectionBase.CreateMock();
            OptionalOverParamsConstructorBase sut8 = OptionalOverParamsConstructorBase.CreateMock();
            OptionalAndParamsConstructorBase sut9 = OptionalAndParamsConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            ParamsCollectionConstructorBase sut10 = ParamsCollectionConstructorBase.CreateMock();
            ParamsSpanConstructorBase sut11 = ParamsSpanConstructorBase.CreateMock();
            OnlyParamsSpanConstructorBase sut12 = OnlyParamsSpanConstructorBase.CreateMock().Implementing<ICombinationMockA>();
            """,
            [],
            UnionMode: true),
        new(
            "OptionalIndexerKeys_CanBeCreated_Unions",
            ["IOptionalIndexerKeys.cs",],
            """
            IOptionalIndexerKeys sut1 = IOptionalIndexerKeys.CreateMock();
            IOptionalIndexerKeysGetterOnly sut2 = IOptionalIndexerKeysGetterOnly.CreateMock();
            IOptionalIndexerKeysSetterOnly sut3 = IOptionalIndexerKeysSetterOnly.CreateMock();
            IOptionalIndexerKeysWithSixKeys sut4 = IOptionalIndexerKeysWithSixKeys.CreateMock();
            IOptionalIndexerKeysWithSibling sut5 = IOptionalIndexerKeysWithSibling.CreateMock();
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
            "ParamsObjectArray_CanBeCreated_Unions",
            ["IParamsObjectArray.cs",],
            """
            IParamsObjectArray sut1 = IParamsObjectArray.CreateMock();
            ParamsObjectArrayDelegate sut2 = ParamsObjectArrayDelegate.CreateMock();
            ParamsObjectArrayOnlyDelegate sut3 = ParamsObjectArrayOnlyDelegate.CreateMock();
            ParamsArrayDelegate sut4 = ParamsArrayDelegate.CreateMock();
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
    private static Dictionary<string, GeneratedScenario> GenerateAll()
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

        // The coverage types have no documentation comments, so missing documentation (CS1591) is only checked in
        // the generated files.
        return results.ToDictionary(result => result.Key, result => new GeneratedScenario(
            result.Value.Diagnostics
                .Where(diagnostic => !diagnostic.Contains("CS1591") || diagnostic.Contains(".g.cs("))
                .ToArray(),
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

        // Classic scenarios are pinned to C# 14 so that they keep describing the classic overload set.
        // Documentation comments are diagnosed so that an invalid cref in the generated code fails the scenario.
        Dictionary<string, string>? globalOptions = scenario.AutoMocking
            ? new Dictionary<string, string> { ["build_property.MockolateAutoMocking"] = "true", }
            : null;
        if (scenario.UnionMode)
        {
            return Generator.Run(
                sources.ToArray(),
                ["NET9_0_OR_GREATER", "NET10_0_OR_GREATER", "NET11_0_OR_GREATER",],
                LanguageVersion.CSharp15,
                DocumentationMode.Diagnose,
                globalOptions,
                scenario.AssemblyTypes);
        }

        return Generator.Run(
            sources.ToArray(),
            ["NET9_0_OR_GREATER", "NET10_0_OR_GREATER",],
            LanguageVersion.CSharp14,
            DocumentationMode.Diagnose,
            globalOptions,
            scenario.AssemblyTypes);
    }

    private static string ReadCoverageFile(string coverageFileName, [CallerFilePath] string path = "")
        => File.ReadAllText(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "Mockolate.Tests", "GeneratorCoverage",
            coverageFileName));

    private sealed record GeneratedScenario(string[] Diagnostics, GeneratedFile[] Files);

    private sealed record GeneratedFile(string SnapshotName, string Content);
}
