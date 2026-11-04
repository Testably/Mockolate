using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using NSubstitute;
using Arg = NSubstitute.Arg;

[assembly: GenerateImposter(typeof(CallbackBenchmarks.IMyCallbackInterface))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we measure the case of an interface mock with a method callback: configure a callback on a
///     method invocation and invoke it twice.
/// </summary>
public class CallbackBenchmarks : BenchmarksBase
{
	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public int Callback_Mockolate()
	{
		int count = 0;
		IMyCallbackInterface sut = IMyCallbackInterface.CreateMock();
		sut.Mock.Setup.MyFunc(It.IsAny<int>()).Do(() => count++);

		sut.MyFunc(1);
		sut.MyFunc(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public int Callback_Imposter()
	{
		int count = 0;
		IMyCallbackInterfaceImposter imposter = IMyCallbackInterface.Imposter();
		imposter.MyFunc(Imposter.Abstractions.Arg<int>.Any()).Callback(_ => count++);

		IMyCallbackInterface instance = imposter.Instance();
		instance.MyFunc(1);
		instance.MyFunc(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public int Callback_TUnitMocks()
	{
		int count = 0;
		Mock<IMyCallbackInterface> mock = IMyCallbackInterface.Mock();
		mock.MyFunc(Any<int>())
			.Callback(() => count++);

		IMyCallbackInterface svc = mock.Object;
		svc.MyFunc(1);
		svc.MyFunc(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public int Callback_Moq()
	{
		int count = 0;
		Moq.Mock<IMyCallbackInterface> mock = new();
		mock.Setup(x => x.MyFunc(Moq.It.IsAny<int>())).Callback(() => count++);

		IMyCallbackInterface instance = mock.Object;
		instance.MyFunc(1);
		instance.MyFunc(2);
		return count;
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public int Callback_NSubstitute()
	{
		int count = 0;
		IMyCallbackInterface mock = Substitute.For<IMyCallbackInterface>();
		mock.When(x => x.MyFunc(Arg.Any<int>())).Do(_ => count++);

		mock.MyFunc(1);
		mock.MyFunc(2);
		return count;
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public int Callback_FakeItEasy()
	{
		int count = 0;
		IMyCallbackInterface mock = A.Fake<IMyCallbackInterface>();
		A.CallTo(() => mock.MyFunc(A<int>.Ignored)).Invokes(() => count++);

		mock.MyFunc(1);
		mock.MyFunc(2);
		return count;
	}

	public interface IMyCallbackInterface
	{
		void MyFunc(int value);
	}
}
#pragma warning restore CA1822 // Mark members as static
