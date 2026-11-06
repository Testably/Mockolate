using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Raise = NSubstitute.Raise;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteEventBenchmarks.IMyEventInterface))]

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
		IMyEventInterface sut = IMyEventInterface.CreateMock();
		EventHandler handler = (_, _) => { };

		sut.SomeEvent += handler;
		sut.Mock.Raise.SomeEvent(null, EventArgs.Empty);

		sut.Mock.Verify.SomeEvent.Subscribed().Once();
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public void Event_Imposter()
	{
		IMyEventInterfaceImposter imposter = IMyEventInterface.Imposter();
		EventHandler handler = (_, _) => { };

		imposter.Instance().SomeEvent += handler;
		imposter.SomeEvent.Raise(null!, EventArgs.Empty);

		imposter.SomeEvent.Subscribed(handler, Count.Once());
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public void Event_TUnitMocks()
	{
		CompleteEventBenchmarks_IMyEventInterfaceMock mock = IMyEventInterface.Mock();
		EventHandler handler = (_, _) => { };

		mock.Object.SomeEvent += handler;
		mock.RaiseSomeEvent(EventArgs.Empty);

		_ = mock.Events.SomeEvent.SubscriberCount;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public void Event_Moq()
	{
		Moq.Mock<IMyEventInterface> mock = new();
		mock.SetupAdd(m => m.SomeEvent += Moq.It.IsAny<EventHandler>());
		EventHandler handler = (_, _) => { };

		mock.Object.SomeEvent += handler;
		mock.Raise(m => m.SomeEvent += null, null!, EventArgs.Empty);

		mock.VerifyAdd(m => m.SomeEvent += Moq.It.IsAny<EventHandler>(), Times.Once());
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public void Event_NSubstitute()
	{
		IMyEventInterface mock = Substitute.For<IMyEventInterface>();
		EventHandler handler = (_, _) => { };

		mock.SomeEvent += handler;
		mock.SomeEvent += Raise.EventWith(null!, EventArgs.Empty);

		mock.Received(1).SomeEvent += Arg.Any<EventHandler>();
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public void Event_FakeItEasy()
	{
		IMyEventInterface mock = A.Fake<IMyEventInterface>();
		EventHandler handler = (_, _) => { };

		mock.SomeEvent += handler;
		mock.SomeEvent += FakeItEasy.Raise.FreeForm.With(null, EventArgs.Empty);

		A.CallTo(mock)
			.Where(call => call.Method.Name == "add_SomeEvent")
			// Expect 2, because raising an event in FakeItEasy is implemented as a call to the add accessor of the event.
			.MustHaveHappened(2, FakeItEasy.Times.Exactly);
	}

	public interface IMyEventInterface
	{
		event EventHandler? SomeEvent;
	}
}
#pragma warning restore CA1822 // Mark members as static
