using BenchmarkDotNet.Attributes;
using Mockolate.Verify;

namespace Mockolate.Benchmarks.Unions;

#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     The <c>CompleteMethodBenchmarks.Method_Mockolate</c> workflow (create, set up one method, invoke <see cref="N" />
///     times, verify) on the union-typed surface, with a literal value, an <c>It</c> matcher and a predicate as the
///     argument. Compare against <c>Mockolate.Benchmarks</c>, which compiles the same workflow in classic mode.
/// </summary>
public class UnionParameterBenchmarks : BenchmarksBase
{
	[Params(1, 10)] public int N { get; set; }

	[Benchmark(Baseline = true)]
	public void Value()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(42).Returns(true);

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		sut.Mock.Verify.MyFunc(42).Exactly(N);
	}

	[Benchmark]
	public void Matcher()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(It.IsAny<int>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		sut.Mock.Verify.MyFunc(It.IsAny<int>()).Exactly(N);
	}

	[Benchmark]
	public void Predicate()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(x => x > 0).Returns(true);

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		sut.Mock.Verify.MyFunc(x => x > 0).Exactly(N);
	}
}
#pragma warning restore CA1822 // Mark members as static
