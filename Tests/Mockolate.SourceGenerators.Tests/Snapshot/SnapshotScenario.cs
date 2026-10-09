namespace Mockolate.SourceGenerators.Tests.Snapshot;

internal sealed record SnapshotScenario(
    string Name,
    string[] CoverageFiles,
    string MainBody,
    Type[] AssemblyTypes,
    bool UnionMode = false);
