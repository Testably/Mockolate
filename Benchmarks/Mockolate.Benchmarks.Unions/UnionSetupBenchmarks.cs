using BenchmarkDotNet.Attributes;

namespace Mockolate.Benchmarks.Unions;

#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     Isolates the argument conversion and setup dispatch of the union-typed surface: creates a mock and registers one
///     setup with a literal value, an <c>It</c> matcher or a predicate.
/// </summary>
public class UnionSetupBenchmarks : BenchmarksBase
{
	[Benchmark(Baseline = true)]
	public void Value()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(42).Returns(true);
	}

	[Benchmark]
	public void Matcher()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(It.IsAny<int>()).Returns(true);
	}

	[Benchmark]
	public void Predicate()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(x => x > 0).Returns(true);
	}
}
#pragma warning restore CA1822 // Mark members as static
