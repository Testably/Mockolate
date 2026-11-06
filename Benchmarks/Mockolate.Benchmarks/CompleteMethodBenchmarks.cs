using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteMethodBenchmarks.IMyMethodInterface))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the simple case of an interface mock, setup a single method that gets called and
///     verified to be called exactly <see cref="N" /> times.
/// </summary>
public class CompleteMethodBenchmarks : BenchmarksBase
{
	[Params(1, 10)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Method_Mockolate()
	{
		IMyMethodInterface sut = IMyMethodInterface.CreateMock();
		sut.Mock.Setup.MyFunc(It.IsAny<int>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		sut.Mock.Verify.MyFunc(It.IsAny<int>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Method_Imposter()
	{
		IMyMethodInterfaceImposter imposter = IMyMethodInterface.Imposter();
		imposter.MyFunc(Imposter.Abstractions.Arg<int>.Any()).Returns(true);
		IMyMethodInterface sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		imposter.MyFunc(Imposter.Abstractions.Arg<int>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Method_TUnitMocks()
	{
		CompleteMethodBenchmarks_IMyMethodInterfaceMock mock = IMyMethodInterface.Mock();
		mock.MyFunc(Any<int>()).Returns(true);
		IMyMethodInterface sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		mock.MyFunc(Any<int>()).WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Method_Moq()
	{
		Moq.Mock<IMyMethodInterface> mock = new();
		mock.Setup(x => x.MyFunc(Moq.It.IsAny<int>())).Returns(true);
		IMyMethodInterface sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.MyFunc(42);
		}

		mock.Verify(x => x.MyFunc(Moq.It.IsAny<int>()), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Method_NSubstitute()
	{
		IMyMethodInterface mock = Substitute.For<IMyMethodInterface>();
		mock.MyFunc(Arg.Any<int>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			mock.MyFunc(42);
		}

		mock.Received(N).MyFunc(Arg.Any<int>());
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Method_FakeItEasy()
	{
		IMyMethodInterface mock = A.Fake<IMyMethodInterface>();
		A.CallTo(() => mock.MyFunc(A<int>.Ignored)).Returns(true);

		for (int i = 0; i < N; i++)
		{
			mock.MyFunc(42);
		}

		A.CallTo(() => mock.MyFunc(A<int>.Ignored)).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IMyMethodInterface
	{
		bool MyFunc(int value);
	}
}
#pragma warning restore CA1822 // Mark members as static
