using aweXpect.Snapshots;

namespace Mockolate.SourceGenerators.Tests.Snapshot;

public sealed class OrphanedSnapshotCheck
{
	/// <summary>
	///     Reports the snapshots of generated files that the generator no longer emits, and deletes them while accepting
	///     all changes. It only runs on a build server, as a partial test run would report the snapshots of the others.
	/// </summary>
	[After(HookType.TestSession)]
	public static async Task CheckSnapshots() => await OrphanedSnapshots.CheckAsync();
}
