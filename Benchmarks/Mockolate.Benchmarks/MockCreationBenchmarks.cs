using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using NSubstitute;

[assembly: GenerateImposter(typeof(MockCreationBenchmarks.IChocolateRecipe))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     Measures the cost of creating an empty mock — no setup, no invocations, no verification.
/// </summary>
public class MockCreationBenchmarks : BenchmarksBase
{
	[Benchmark(Baseline = true)]
	public object CreateMock_Mockolate()
		=> IChocolateRecipe.CreateMock();

	[Benchmark]
	public object CreateMock_Imposter()
	{
		IChocolateRecipeImposter imposter = IChocolateRecipe.Imposter();
		return imposter.Instance();
	}

	[Benchmark]
	public object CreateMock_TUnitMocks()
	{
		MockCreationBenchmarks_IChocolateRecipeMock mock = IChocolateRecipe.Mock();
		return mock.Object;
	}

	[Benchmark]
	public object CreateMock_Moq()
	{
		Moq.Mock<IChocolateRecipe> mock = new();
		return mock.Object;
	}

	[Benchmark]
	public object CreateMock_NSubstitute()
		=> Substitute.For<IChocolateRecipe>();

	[Benchmark]
	public object CreateMock_FakeItEasy()
		=> A.Fake<IChocolateRecipe>();

	public interface IChocolateRecipe
	{
		int Servings { get; }
		int Mix(int cocoa, int sugar);
		double Split(double weight, double pieces);
		string Label(int batch);
	}
}
#pragma warning restore CA1822 // Mark members as static
