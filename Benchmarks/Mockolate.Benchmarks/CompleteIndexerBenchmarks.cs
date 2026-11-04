using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteIndexerBenchmarks.IMyIndexerInterface))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the case of an interface mock with an indexer, setup the indexer and verify
///     that both the getter and the setter were called exactly <see cref="N" /> times.
/// </summary>
public class CompleteIndexerBenchmarks : BenchmarksBase
{
	[Params(1, 10)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Indexer_Mockolate()
	{
		IMyIndexerInterface sut = IMyIndexerInterface.CreateMock();
		sut.Mock.Setup[It.IsAny<int>()].Returns("foo");

		for (int i = 0; i < N; i++)
		{
			_ = sut[42];
			sut[42] = "bar";
		}

		sut.Mock.Verify[It.IsAny<int>()].Got().Exactly(N);
		sut.Mock.Verify[It.IsAny<int>()].Set(It.IsAny<string>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Indexer_Imposter()
	{
		IMyIndexerInterfaceImposter imposter = IMyIndexerInterface.Imposter();
		imposter[Imposter.Abstractions.Arg<int>.Any()].Getter().Returns("foo");
		IMyIndexerInterface sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			_ = sut[42];
			sut[42] = "bar";
		}

		imposter[Imposter.Abstractions.Arg<int>.Any()].Getter().Called(Count.Exactly(N));
		imposter[Imposter.Abstractions.Arg<int>.Any()].Setter().Called(Count.Exactly(N));
	}

	/* Indexers not supported on TUnit.Mocks
	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Indexer_TUnitMocks()
	{
		TUnit.Mocks.Mock<IMyIndexerInterface> mock = IMyIndexerInterface.Mock();
		mock[Any<int>()].Returns("foo");

		for (int i = 0; i < N; i++)
		{
			_ = mock.Object[42];
			mock.Object[42] = "bar";
		}

		mock[Any<int>()].WasCalled(TUnit.Mocks.Times.Exactly(N));
	}
	*/

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Indexer_Moq()
	{
		Moq.Mock<IMyIndexerInterface> mock = new();
		mock.Setup(x => x[Moq.It.IsAny<int>()]).Returns("foo");
		IMyIndexerInterface sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			_ = sut[42];
			sut[42] = "bar";
		}

		mock.Verify(x => x[Moq.It.IsAny<int>()], Times.Exactly(N));
		mock.VerifySet(x => x[Moq.It.IsAny<int>()] = Moq.It.IsAny<string>(), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Indexer_NSubstitute()
	{
		IMyIndexerInterface mock = Substitute.For<IMyIndexerInterface>();
		mock[Arg.Any<int>()].Returns("foo");

		for (int i = 0; i < N; i++)
		{
			_ = mock[42];
			mock[42] = "bar";
		}

		_ = mock.Received(N)[Arg.Any<int>()];
		mock.Received(N)[Arg.Any<int>()] = Arg.Any<string>();
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Indexer_FakeItEasy()
	{
		IMyIndexerInterface mock = A.Fake<IMyIndexerInterface>();
		A.CallTo(() => mock[A<int>.Ignored]).Returns("foo");

		for (int i = 0; i < N; i++)
		{
			_ = mock[42];
			mock[42] = "bar";
		}

		A.CallTo(() => mock[A<int>.Ignored]).MustHaveHappened(N, FakeItEasy.Times.Exactly);
		A.CallToSet(() => mock[A<int>.Ignored]).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IMyIndexerInterface
	{
		string this[int index] { get; set; }
	}
}
#pragma warning restore CA1822 // Mark members as static
