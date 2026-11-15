using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using NSubstitute;
using Arg = NSubstitute.Arg;

[assembly: GenerateImposter(typeof(CallbackBenchmarks.IChocolateTaster))]

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
		IChocolateTaster sut = IChocolateTaster.CreateMock();
		sut.Mock.Setup.Taste(It.IsAny<int>()).Do(() => count++);

		sut.Taste(1);
		sut.Taste(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public int Callback_Imposter()
	{
		int count = 0;
		IChocolateTasterImposter imposter = IChocolateTaster.Imposter();
		imposter.Taste(Imposter.Abstractions.Arg<int>.Any()).Callback(_ => count++);

		IChocolateTaster instance = imposter.Instance();
		instance.Taste(1);
		instance.Taste(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public int Callback_TUnitMocks()
	{
		int count = 0;
		CallbackBenchmarks_IChocolateTasterMock mock = IChocolateTaster.Mock();
		mock.Taste(Any<int>())
			.Callback(() => count++);

		IChocolateTaster svc = mock.Object;
		svc.Taste(1);
		svc.Taste(2);
		return count;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public int Callback_Moq()
	{
		int count = 0;
		Moq.Mock<IChocolateTaster> mock = new();
		mock.Setup(x => x.Taste(Moq.It.IsAny<int>())).Callback(() => count++);

		IChocolateTaster instance = mock.Object;
		instance.Taste(1);
		instance.Taste(2);
		return count;
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public int Callback_NSubstitute()
	{
		int count = 0;
		IChocolateTaster mock = Substitute.For<IChocolateTaster>();
		mock.When(x => x.Taste(Arg.Any<int>())).Do(_ => count++);

		mock.Taste(1);
		mock.Taste(2);
		return count;
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public int Callback_FakeItEasy()
	{
		int count = 0;
		IChocolateTaster mock = A.Fake<IChocolateTaster>();
		A.CallTo(() => mock.Taste(A<int>.Ignored)).Invokes(() => count++);

		mock.Taste(1);
		mock.Taste(2);
		return count;
	}

	public interface IChocolateTaster
	{
		void Taste(int pieces);
	}
}
#pragma warning restore CA1822 // Mark members as static
