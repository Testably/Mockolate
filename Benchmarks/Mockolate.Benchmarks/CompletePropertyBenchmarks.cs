using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompletePropertyBenchmarks.IChocolateShelf))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the case of an interface mock with a property, setup the property and verify
///     that both the getter and the setter were called exactly <see cref="N" /> times.
/// </summary>
public class CompletePropertyBenchmarks : BenchmarksBase
{
	[Params(1, 10, 1000)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Property_Mockolate()
	{
		IChocolateShelf sut = IChocolateShelf.CreateMock();
		sut.Mock.Setup.Stock.InitializeWith(42);

		for (int i = 0; i < N; i++)
		{
			_ = sut.Stock;
			sut.Stock = i;
		}

		sut.Mock.Verify.Stock.Got().Exactly(N);
		sut.Mock.Verify.Stock.Set(It.IsAny<int>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Property_Imposter()
	{
		IChocolateShelfImposter imposter = IChocolateShelf.Imposter();
		imposter.Stock.Getter().Returns(42);
		IChocolateShelf sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			_ = sut.Stock;
			sut.Stock = i;
		}

		imposter.Stock.Getter().Called(Count.Exactly(N));
		imposter.Stock.Setter(Imposter.Abstractions.Arg<int>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Property_TUnitMocks()
	{
		CompletePropertyBenchmarks_IChocolateShelfMock mock = IChocolateShelf.Mock();
		mock.Stock.Returns(42);
		IChocolateShelf sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			_ = sut.Stock;
			sut.Stock = i;
		}

		mock.Stock.WasCalled(TUnit.Mocks.Times.Exactly(N));
		mock.Stock.Setter.WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Property_Moq()
	{
		Moq.Mock<IChocolateShelf> mock = new();
		mock.SetupGet(x => x.Stock).Returns(42);
		IChocolateShelf sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			_ = sut.Stock;
			sut.Stock = i;
		}

		mock.VerifyGet(x => x.Stock, Times.Exactly(N));
		mock.VerifySet(x => x.Stock = Moq.It.IsAny<int>(), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Property_NSubstitute()
	{
		IChocolateShelf mock = Substitute.For<IChocolateShelf>();
		mock.Stock.Returns(42);

		for (int i = 0; i < N; i++)
		{
			_ = mock.Stock;
			mock.Stock = i;
		}

		_ = mock.Received(N).Stock;
		mock.Received(N).Stock = Arg.Any<int>();
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Property_FakeItEasy()
	{
		IChocolateShelf mock = A.Fake<IChocolateShelf>();
		A.CallTo(() => mock.Stock).Returns(42);

		for (int i = 0; i < N; i++)
		{
			_ = mock.Stock;
			mock.Stock = i;
		}

		A.CallTo(() => mock.Stock).MustHaveHappened(N, FakeItEasy.Times.Exactly);
		A.CallToSet(() => mock.Stock).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IChocolateShelf
	{
		int Stock { get; set; }
	}
}
#pragma warning restore CA1822 // Mark members as static
