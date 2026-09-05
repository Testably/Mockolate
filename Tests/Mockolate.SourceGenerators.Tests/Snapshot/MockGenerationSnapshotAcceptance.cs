using System.Collections.Generic;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

public sealed class MockGenerationSnapshotAcceptance
{
	/// <summary>
	///     Execute this test to overwrite the expected snapshot files for
	///     <see cref="MockGenerationSnapshotTests" /> with the current generator output.
	/// </summary>
	[Test]
	[Explicit]
    public void AcceptSnapshotChanges()
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> scenarios = new();
        foreach (var scenario in MockGenerationSnapshotTests.Scenarios)
        {
            var result = MockGenerationSnapshotTests.RunGenerator(scenario);
            scenarios[scenario.Name] = MockGenerationSnapshotTests.NormalizeSources(result);
        }

        SnapshotStorage.SetExpected(scenarios);
    }
}
