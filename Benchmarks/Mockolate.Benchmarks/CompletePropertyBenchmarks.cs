using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompletePropertyBenchmarks.IMyPropertyInterface))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the case of an interface mock with a property, setup the property and verify
///     that both the getter and the setter were called exactly <see cref="N" /> times.
/// </summary>
public class CompletePropertyBenchmarks : BenchmarksBase
{
	[Params(1, 10)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Property_Mockolate()
	{
		IMyPropertyInterface sut = IMyPropertyInterface.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		for (int i = 0; i < N; i++)
		{
			_ = sut.Counter;
			sut.Counter = i;
		}

		sut.Mock.Verify.Counter.Got().Exactly(N);
		sut.Mock.Verify.Counter.Set(It.IsAny<int>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Property_Imposter()
	{
		IMyPropertyInterfaceImposter imposter = IMyPropertyInterface.Imposter();
		imposter.Counter.Getter().Returns(42);
		IMyPropertyInterface sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			_ = sut.Counter;
			sut.Counter = i;
		}

		imposter.Counter.Getter().Called(Count.Exactly(N));
		imposter.Counter.Setter(Imposter.Abstractions.Arg<int>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Property_TUnitMocks()
	{
		Mock<IMyPropertyInterface> mock = IMyPropertyInterface.Mock();
		mock.Counter.Returns(42);
		IMyPropertyInterface sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			_ = sut.Counter;
			sut.Counter = i;
		}

		mock.Counter.WasCalled(TUnit.Mocks.Times.Exactly(N));
		mock.Counter.Setter.WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Property_Moq()
	{
		Moq.Mock<IMyPropertyInterface> mock = new();
		mock.SetupGet(x => x.Counter).Returns(42);
		IMyPropertyInterface sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			_ = sut.Counter;
			sut.Counter = i;
		}

		mock.VerifyGet(x => x.Counter, Times.Exactly(N));
		mock.VerifySet(x => x.Counter = Moq.It.IsAny<int>(), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Property_NSubstitute()
	{
		IMyPropertyInterface mock = Substitute.For<IMyPropertyInterface>();
		mock.Counter.Returns(42);

		for (int i = 0; i < N; i++)
		{
			_ = mock.Counter;
			mock.Counter = i;
		}

		_ = mock.Received(N).Counter;
		mock.Received(N).Counter = Arg.Any<int>();
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Property_FakeItEasy()
	{
		IMyPropertyInterface mock = A.Fake<IMyPropertyInterface>();
		A.CallTo(() => mock.Counter).Returns(42);

		for (int i = 0; i < N; i++)
		{
			_ = mock.Counter;
			mock.Counter = i;
		}

		A.CallTo(() => mock.Counter).MustHaveHappened(N, FakeItEasy.Times.Exactly);
		A.CallToSet(() => mock.Counter).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IMyPropertyInterface
	{
		int Counter { get; set; }
	}
}
#pragma warning restore CA1822 // Mark members as static
