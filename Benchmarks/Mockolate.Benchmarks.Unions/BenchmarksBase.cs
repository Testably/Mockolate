using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Mockolate.Benchmarks.Unions;

/// <summary>
///     Same job as the classic <c>Mockolate.Benchmarks</c>, so the union-mode numbers compare job for job.
/// </summary>
[Config(typeof(Config))]
[MarkdownExporterAttribute.GitHub]
[MemoryDiagnoser]
public abstract class BenchmarksBase
{
	private sealed class Config : ManualConfig
	{
		public Config()
		{
			AddJob(Job.MediumRun
				.WithLaunchCount(1)
				.WithToolchain(InProcessEmitToolchain.Instance)
				.WithId("InProcess"));
		}
	}
}
