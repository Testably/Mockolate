using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using NSubstitute;

[assembly: GenerateImposter(typeof(MockCreationBenchmarks.ICalculatorService))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     Measures the cost of creating an empty mock — no setup, no invocations, no verification.
/// </summary>
public class MockCreationBenchmarks : BenchmarksBase
{
	[Benchmark(Baseline = true)]
	public object CreateMock_Mockolate()
		=> ICalculatorService.CreateMock();

	[Benchmark]
	public object CreateMock_Imposter()
	{
		ICalculatorServiceImposter imposter = ICalculatorService.Imposter();
		return imposter.Instance();
	}

	[Benchmark]
	public object CreateMock_TUnitMocks()
	{
		Mock<ICalculatorService> mock = ICalculatorService.Mock();
		return mock.Object;
	}

	[Benchmark]
	public object CreateMock_Moq()
	{
		Moq.Mock<ICalculatorService> mock = new();
		return mock.Object;
	}

	[Benchmark]
	public object CreateMock_NSubstitute()
		=> Substitute.For<ICalculatorService>();

	[Benchmark]
	public object CreateMock_FakeItEasy()
		=> A.Fake<ICalculatorService>();

	public interface ICalculatorService
	{
		int Zero { get; }
		int Add(int a, int b);
		double Divide(double numerator, double denominator);
		string Format(int value);
	}
}
#pragma warning restore CA1822 // Mark members as static
