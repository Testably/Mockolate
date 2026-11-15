using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Raise = NSubstitute.Raise;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteEventBenchmarks.IChocolateFactory))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we check the case of an interface mock with an event, subscribe to the event and verify
///     the subscription was recorded once.
/// </summary>
public class CompleteEventBenchmarks : BenchmarksBase
{
	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public void Event_Mockolate()
	{
		IChocolateFactory sut = IChocolateFactory.CreateMock();
		EventHandler handler = (_, _) => { };

		sut.BatchFinished += handler;
		sut.Mock.Raise.BatchFinished(null, EventArgs.Empty);

		sut.Mock.Verify.BatchFinished.Subscribed().Once();
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Event_Imposter()
	{
		IChocolateFactoryImposter imposter = IChocolateFactory.Imposter();
		EventHandler handler = (_, _) => { };

		imposter.Instance().BatchFinished += handler;
		imposter.BatchFinished.Raise(null!, EventArgs.Empty);

		imposter.BatchFinished.Subscribed(handler, Count.Once());
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Event_TUnitMocks()
	{
		CompleteEventBenchmarks_IChocolateFactoryMock mock = IChocolateFactory.Mock();
		EventHandler handler = (_, _) => { };

		mock.Object.BatchFinished += handler;
		mock.RaiseBatchFinished(EventArgs.Empty);

		_ = mock.Events.BatchFinished.SubscriberCount;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Event_Moq()
	{
		Moq.Mock<IChocolateFactory> mock = new();
		mock.SetupAdd(m => m.BatchFinished += Moq.It.IsAny<EventHandler>());
		EventHandler handler = (_, _) => { };

		mock.Object.BatchFinished += handler;
		mock.Raise(m => m.BatchFinished += null, null!, EventArgs.Empty);

		mock.VerifyAdd(m => m.BatchFinished += Moq.It.IsAny<EventHandler>(), Times.Once());
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Event_NSubstitute()
	{
		IChocolateFactory mock = Substitute.For<IChocolateFactory>();
		EventHandler handler = (_, _) => { };

		mock.BatchFinished += handler;
		mock.BatchFinished += Raise.EventWith(null!, EventArgs.Empty);

		mock.Received(1).BatchFinished += Arg.Any<EventHandler>();
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Event_FakeItEasy()
	{
		IChocolateFactory mock = A.Fake<IChocolateFactory>();
		EventHandler handler = (_, _) => { };

		mock.BatchFinished += handler;
		mock.BatchFinished += FakeItEasy.Raise.FreeForm.With(null, EventArgs.Empty);

		A.CallTo(mock)
			.Where(call => call.Method.Name == "add_BatchFinished")
			// Expect 2, because raising an event in FakeItEasy is implemented as a call to the add accessor of the event.
			.MustHaveHappened(2, FakeItEasy.Times.Exactly);
	}

	public interface IChocolateFactory
	{
		event EventHandler? BatchFinished;
	}
}
#pragma warning restore CA1822 // Mark members as static
