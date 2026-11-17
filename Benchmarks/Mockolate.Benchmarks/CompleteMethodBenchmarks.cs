using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteMethodBenchmarks.IChocolateMachine))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the simple case of an interface mock, setup a single method that gets called and
///     verified to be called exactly <see cref="N" /> times.
/// </summary>
public class CompleteMethodBenchmarks : BenchmarksBase
{
	[Params(1, 10, 1000)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Method_Mockolate()
	{
		IChocolateMachine sut = IChocolateMachine.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<int>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			sut.Dispense(42);
		}

		sut.Mock.Verify.Dispense(It.IsAny<int>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Method_Imposter()
	{
		IChocolateMachineImposter imposter = IChocolateMachine.Imposter();
		imposter.Dispense(Imposter.Abstractions.Arg<int>.Any()).Returns(true);
		IChocolateMachine sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			sut.Dispense(42);
		}

		imposter.Dispense(Imposter.Abstractions.Arg<int>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Method_TUnitMocks()
	{
		CompleteMethodBenchmarks_IChocolateMachineMock mock = IChocolateMachine.Mock();
		mock.Dispense(Any<int>()).Returns(true);
		IChocolateMachine sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.Dispense(42);
		}

		mock.Dispense(Any<int>()).WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Method_Moq()
	{
		Moq.Mock<IChocolateMachine> mock = new();
		mock.Setup(x => x.Dispense(Moq.It.IsAny<int>())).Returns(true);
		IChocolateMachine sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.Dispense(42);
		}

		mock.Verify(x => x.Dispense(Moq.It.IsAny<int>()), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Method_NSubstitute()
	{
		IChocolateMachine mock = Substitute.For<IChocolateMachine>();
		mock.Dispense(Arg.Any<int>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			mock.Dispense(42);
		}

		mock.Received(N).Dispense(Arg.Any<int>());
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Method_FakeItEasy()
	{
		IChocolateMachine mock = A.Fake<IChocolateMachine>();
		A.CallTo(() => mock.Dispense(A<int>.Ignored)).Returns(true);

		for (int i = 0; i < N; i++)
		{
			mock.Dispense(42);
		}

		A.CallTo(() => mock.Dispense(A<int>.Ignored)).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IChocolateMachine
	{
		bool Dispense(int amount);
	}
}
#pragma warning restore CA1822 // Mark members as static
