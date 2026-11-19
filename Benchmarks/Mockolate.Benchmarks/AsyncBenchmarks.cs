using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(AsyncBenchmarks.IChocolateOrders))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check an interface mock with async methods: a <see cref="Task{TResult}" /> and a
///     <see cref="ValueTask{TResult}" /> method with a setup and one of each without are awaited <see cref="N" /> times.
///     Afterwards one method is verified to be called exactly <see cref="N" /> times.
/// </summary>
public class AsyncBenchmarks : BenchmarksBase
{
	[Params(1, 10, 1000)] public int N { get; set; }

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public async Task Async_Mockolate()
	{
		IChocolateOrders sut = IChocolateOrders.CreateMock();
		sut.Mock.Setup.PlaceAsync(It.IsAny<string>()).ReturnsAsync(42);
		sut.Mock.Setup.IsAvailableAsync(It.IsAny<string>()).ReturnsAsync(true);

		for (int i = 0; i < N; i++)
		{
			await sut.PlaceAsync("Dark");
			await sut.IsAvailableAsync("Dark");
			await sut.CountAsync("Dark");
			await sut.IsPaidAsync("Dark");
		}

		sut.Mock.Verify.PlaceAsync(It.IsAny<string>()).Exactly(N);
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public async Task Async_Imposter()
	{
		IChocolateOrdersImposter imposter = IChocolateOrders.Imposter();
		imposter.PlaceAsync(Imposter.Abstractions.Arg<string>.Any()).ReturnsAsync(42);
		imposter.IsAvailableAsync(Imposter.Abstractions.Arg<string>.Any()).ReturnsAsync(true);
		IChocolateOrders sut = imposter.Instance();

		for (int i = 0; i < N; i++)
		{
			await sut.PlaceAsync("Dark");
			await sut.IsAvailableAsync("Dark");
			await sut.CountAsync("Dark");
			await sut.IsPaidAsync("Dark");
		}

		imposter.PlaceAsync(Imposter.Abstractions.Arg<string>.Any()).Called(Count.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public async Task Async_TUnitMocks()
	{
		AsyncBenchmarks_IChocolateOrdersMock mock = IChocolateOrders.Mock();
		mock.PlaceAsync(Any<string>()).Returns(42);
		mock.IsAvailableAsync(Any<string>()).Returns(true);
		IChocolateOrders sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			await sut.PlaceAsync("Dark");
			await sut.IsAvailableAsync("Dark");
			await sut.CountAsync("Dark");
			await sut.IsPaidAsync("Dark");
		}

		mock.PlaceAsync(Any<string>()).WasCalled(TUnit.Mocks.Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public async Task Async_Moq()
	{
		Moq.Mock<IChocolateOrders> mock = new();
		Moq.ReturnsExtensions.ReturnsAsync(mock.Setup(x => x.PlaceAsync(Moq.It.IsAny<string>())), 42);
		Moq.ReturnsExtensions.ReturnsAsync(mock.Setup(x => x.IsAvailableAsync(Moq.It.IsAny<string>())), true);
		IChocolateOrders sut = mock.Object;

		for (int i = 0; i < N; i++)
		{
			await sut.PlaceAsync("Dark");
			await sut.IsAvailableAsync("Dark");
			await sut.CountAsync("Dark");
			await sut.IsPaidAsync("Dark");
		}

		mock.Verify(x => x.PlaceAsync(Moq.It.IsAny<string>()), Times.Exactly(N));
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public async Task Async_NSubstitute()
	{
		IChocolateOrders mock = Substitute.For<IChocolateOrders>();
		mock.PlaceAsync(Arg.Any<string>()).Returns(42);
		mock.IsAvailableAsync(Arg.Any<string>()).Returns(true);

		for (int i = 0; i < N; i++)
		{
			await mock.PlaceAsync("Dark");
			await mock.IsAvailableAsync("Dark");
			await mock.CountAsync("Dark");
			await mock.IsPaidAsync("Dark");
		}

		await mock.Received(N).PlaceAsync(Arg.Any<string>());
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public async Task Async_FakeItEasy()
	{
		IChocolateOrders mock = A.Fake<IChocolateOrders>();
		A.CallTo(() => mock.PlaceAsync(A<string>.Ignored)).Returns(42);
		A.CallTo(() => mock.IsAvailableAsync(A<string>.Ignored)).Returns(new ValueTask<bool>(true));

		for (int i = 0; i < N; i++)
		{
			await mock.PlaceAsync("Dark");
			await mock.IsAvailableAsync("Dark");
			await mock.CountAsync("Dark");
			await mock.IsPaidAsync("Dark");
		}

		A.CallTo(() => mock.PlaceAsync(A<string>.Ignored)).MustHaveHappened(N, FakeItEasy.Times.Exactly);
	}

	public interface IChocolateOrders
	{
		Task<int> PlaceAsync(string type);
		ValueTask<bool> IsAvailableAsync(string type);
		Task<int> CountAsync(string type);
		ValueTask<bool> IsPaidAsync(string type);
	}
}
#pragma warning restore CA1822 // Mark members as static
