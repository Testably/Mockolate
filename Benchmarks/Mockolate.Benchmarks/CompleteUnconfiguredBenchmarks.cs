using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteUnconfiguredBenchmarks.IChocolateWarehouse))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check an interface mock without any setup: a method that returns a number, a method that
///     returns a string, a method without return value, a property and an indexer are each used <see cref="N" /> times
///     and answer with their default values. Afterwards one method is verified to be called exactly <see cref="N" /> times.
/// </summary>
public class CompleteUnconfiguredBenchmarks : BenchmarksBase
{
	[Params(1, 10, 1000)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Unconfigured_Mockolate()
	{
		IChocolateWarehouse sut = IChocolateWarehouse.CreateMock();

		for (int i = 0; i < N; i++)
		{
			sut.CountBars("Dark");
			sut.FindShelf("Dark", 3);
			sut.Restock("Dark", 3);
			_ = sut.IsOpen;
			_ = sut[42];
		}

		sut.Mock.Verify.CountBars(It.IsAny<string>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Unconfigured_Imposter()
	{
		IChocolateWarehouseImposter imposter = IChocolateWarehouse.Imposter();
		IChocolateWarehouse sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			sut.CountBars("Dark");
			sut.FindShelf("Dark", 3);
			sut.Restock("Dark", 3);
			_ = sut.IsOpen;
			_ = sut[42];
		}

		imposter.CountBars(Imposter.Abstractions.Arg<string>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Unconfigured_TUnitMocks()
	{
		CompleteUnconfiguredBenchmarks_IChocolateWarehouseMock mock = IChocolateWarehouse.Mock();
		IChocolateWarehouse sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.CountBars("Dark");
			sut.FindShelf("Dark", 3);
			sut.Restock("Dark", 3);
			_ = sut.IsOpen;
			_ = sut[42];
		}

		mock.CountBars(Any<string>()).WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Unconfigured_Moq()
	{
		Moq.Mock<IChocolateWarehouse> mock = new();
		IChocolateWarehouse sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			sut.CountBars("Dark");
			sut.FindShelf("Dark", 3);
			sut.Restock("Dark", 3);
			_ = sut.IsOpen;
			_ = sut[42];
		}

		mock.Verify(x => x.CountBars(Moq.It.IsAny<string>()), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Unconfigured_NSubstitute()
	{
		IChocolateWarehouse mock = Substitute.For<IChocolateWarehouse>();

		for (int i = 0; i < N; i++)
		{
			mock.CountBars("Dark");
			mock.FindShelf("Dark", 3);
			mock.Restock("Dark", 3);
			_ = mock.IsOpen;
			_ = mock[42];
		}

		mock.Received(N).CountBars(Arg.Any<string>());
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Unconfigured_FakeItEasy()
	{
		IChocolateWarehouse mock = A.Fake<IChocolateWarehouse>();

		for (int i = 0; i < N; i++)
		{
			mock.CountBars("Dark");
			mock.FindShelf("Dark", 3);
			mock.Restock("Dark", 3);
			_ = mock.IsOpen;
			_ = mock[42];
		}

		A.CallTo(() => mock.CountBars(A<string>.Ignored)).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IChocolateWarehouse
	{
		bool IsOpen { get; }
		string this[int number] { get; }
		int CountBars(string type);
		string FindShelf(string type, int amount);
		void Restock(string type, int amount);
	}
}
#pragma warning restore CA1822 // Mark members as static
