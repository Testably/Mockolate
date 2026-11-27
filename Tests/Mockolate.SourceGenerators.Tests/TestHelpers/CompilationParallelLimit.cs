using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<CompilationParallelLimit>]

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

/// <summary>
///     Each test compiles its own sources against the full framework references, so unlimited parallelism needs more
///     than 12 GB and gets the test host killed on CI runners with less memory.
/// </summary>
public sealed class CompilationParallelLimit : IParallelLimit
{
	public int Limit => 3;
}
