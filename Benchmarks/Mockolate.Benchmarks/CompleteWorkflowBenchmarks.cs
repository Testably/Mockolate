using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IOrderRepository))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IPaymentGateway))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IClock))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.INotifier))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we simulate a complete unit test: an <see cref="OrderService" /> checks out an order with four
///     mocked dependencies, which are set up with exact values, argument matchers and a property, and verified afterwards.
/// </summary>
public class CompleteWorkflowBenchmarks : BenchmarksBase
{
	private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public bool Complete_Mockolate()
	{
		IOrderRepository repository = IOrderRepository.CreateMock();
		IPaymentGateway payment = IPaymentGateway.CreateMock();
		IClock clock = IClock.CreateMock();
		INotifier notifier = INotifier.CreateMock();
		repository.Mock.Setup.Find(1).Returns(new Order(1, "Alice", 42m));
		payment.Mock.Setup.Charge(It.IsAny<string>(), It.IsAny<decimal>()).Returns(true);
		clock.Mock.Setup.Now.InitializeWith(Now);

		bool result = new OrderService(repository, payment, clock, notifier).Checkout(1);

		payment.Mock.Verify.Charge("Alice", 42m).Once();
		repository.Mock.Verify.Save(It.IsAny<Order>()).Once();
		notifier.Mock.Verify.Send("Alice", It.IsAny<string>()).Once();
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public bool Complete_Imposter()
	{
		IOrderRepositoryImposter repository = IOrderRepository.Imposter();
		IPaymentGatewayImposter payment = IPaymentGateway.Imposter();
		IClockImposter clock = IClock.Imposter();
		INotifierImposter notifier = INotifier.Imposter();
		repository.Find(1).Returns(new Order(1, "Alice", 42m));
		payment.Charge(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<decimal>.Any()).Returns(true);
		clock.Now.Getter().Returns(Now);

		bool result = new OrderService(repository.Instance(), payment.Instance(), clock.Instance(), notifier.Instance())
			.Checkout(1);

		payment.Charge("Alice", 42m).Called(Count.Once());
		repository.Save(Imposter.Abstractions.Arg<Order>.Any()).Called(Count.Once());
		notifier.Send("Alice", Imposter.Abstractions.Arg<string>.Any()).Called(Count.Once());
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public bool Complete_TUnitMocks()
	{
		CompleteWorkflowBenchmarks_IOrderRepositoryMock repository = IOrderRepository.Mock();
		CompleteWorkflowBenchmarks_IPaymentGatewayMock payment = IPaymentGateway.Mock();
		CompleteWorkflowBenchmarks_IClockMock clock = IClock.Mock();
		CompleteWorkflowBenchmarks_INotifierMock notifier = INotifier.Mock();
		repository.Find(1).Returns(new Order(1, "Alice", 42m));
		payment.Charge(Any<string>(), Any<decimal>()).Returns(true);
		clock.Now.Returns(Now);

		bool result = new OrderService(repository.Object, payment.Object, clock.Object, notifier.Object).Checkout(1);

		payment.Charge("Alice", 42m).WasCalled(TUnit.Mocks.Times.Once);
		repository.Save(Any<Order>()).WasCalled(TUnit.Mocks.Times.Once);
		notifier.Send("Alice", Any<string>()).WasCalled(TUnit.Mocks.Times.Once);
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public bool Complete_Moq()
	{
		Moq.Mock<IOrderRepository> repository = new();
		Moq.Mock<IPaymentGateway> payment = new();
		Moq.Mock<IClock> clock = new();
		Moq.Mock<INotifier> notifier = new();
		repository.Setup(x => x.Find(1)).Returns(new Order(1, "Alice", 42m));
		payment.Setup(x => x.Charge(Moq.It.IsAny<string>(), Moq.It.IsAny<decimal>())).Returns(true);
		clock.SetupGet(x => x.Now).Returns(Now);

		bool result = new OrderService(repository.Object, payment.Object, clock.Object, notifier.Object).Checkout(1);

		payment.Verify(x => x.Charge("Alice", 42m), Times.Once());
		repository.Verify(x => x.Save(Moq.It.IsAny<Order>()), Times.Once());
		notifier.Verify(x => x.Send("Alice", Moq.It.IsAny<string>()), Times.Once());
		return result;
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public bool Complete_NSubstitute()
	{
		IOrderRepository repository = Substitute.For<IOrderRepository>();
		IPaymentGateway payment = Substitute.For<IPaymentGateway>();
		IClock clock = Substitute.For<IClock>();
		INotifier notifier = Substitute.For<INotifier>();
		repository.Find(1).Returns(new Order(1, "Alice", 42m));
		payment.Charge(Arg.Any<string>(), Arg.Any<decimal>()).Returns(true);
		clock.Now.Returns(Now);

		bool result = new OrderService(repository, payment, clock, notifier).Checkout(1);

		payment.Received(1).Charge("Alice", 42m);
		repository.Received(1).Save(Arg.Any<Order>());
		notifier.Received(1).Send("Alice", Arg.Any<string>());
		return result;
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public bool Complete_FakeItEasy()
	{
		IOrderRepository repository = A.Fake<IOrderRepository>();
		IPaymentGateway payment = A.Fake<IPaymentGateway>();
		IClock clock = A.Fake<IClock>();
		INotifier notifier = A.Fake<INotifier>();
		A.CallTo(() => repository.Find(1)).Returns(new Order(1, "Alice", 42m));
		A.CallTo(() => payment.Charge(A<string>.Ignored, A<decimal>.Ignored)).Returns(true);
		A.CallTo(() => clock.Now).Returns(Now);

		bool result = new OrderService(repository, payment, clock, notifier).Checkout(1);

		A.CallTo(() => payment.Charge("Alice", 42m)).MustHaveHappenedOnceExactly();
		A.CallTo(() => repository.Save(A<Order>.Ignored)).MustHaveHappenedOnceExactly();
		A.CallTo(() => notifier.Send("Alice", A<string>.Ignored)).MustHaveHappenedOnceExactly();
		return result;
	}

	public class Order(int id, string customer, decimal total)
	{
		public int Id { get; } = id;
		public string Customer { get; } = customer;
		public decimal Total { get; } = total;
		public DateTime? PaidAt { get; set; }
	}

	public interface IOrderRepository
	{
		Order? Find(int id);
		void Save(Order order);
	}

	public interface IPaymentGateway
	{
		bool Charge(string customer, decimal amount);
	}

	public interface IClock
	{
		DateTime Now { get; }
	}

	public interface INotifier
	{
		void Send(string customer, string message);
	}

	private sealed class OrderService(
		IOrderRepository repository,
		IPaymentGateway payment,
		IClock clock,
		INotifier notifier)
	{
		public bool Checkout(int orderId)
		{
			Order? order = repository.Find(orderId);
			if (order is null || !payment.Charge(order.Customer, order.Total))
			{
				return false;
			}

			order.PaidAt = clock.Now;
			repository.Save(order);
			notifier.Send(order.Customer, $"Order {order.Id} paid");
			return true;
		}
	}
}
#pragma warning restore CA1822 // Mark members as static
