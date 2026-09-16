using System.Collections.Generic;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

public sealed class MockGenerationSnapshotAcceptance
{
	/// <summary>
	///     Set the environment variable <c>MOCKOLATE_ACCEPT_SNAPSHOTS</c> to <c>true</c> and execute this test to
	///     overwrite the expected snapshot files for <see cref="MockGenerationSnapshotTests" /> with the current
	///     generator output. Without the variable the test is a no-op (xunit v2 has no explicit tests).
	/// </summary>
	[Fact]
	public void AcceptSnapshotChanges()
	{
		if (!string.Equals(Environment.GetEnvironmentVariable("MOCKOLATE_ACCEPT_SNAPSHOTS"), "true",
			    StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		Dictionary<string, IReadOnlyDictionary<string, string>> scenarios = new();
		foreach (var scenario in MockGenerationSnapshotTests.Scenarios)
		{
			var result = MockGenerationSnapshotTests.RunGenerator(scenario);
			scenarios[scenario.Name] = MockGenerationSnapshotTests.NormalizeSources(result);
		}

		SnapshotStorage.SetExpected(scenarios);
	}
}
