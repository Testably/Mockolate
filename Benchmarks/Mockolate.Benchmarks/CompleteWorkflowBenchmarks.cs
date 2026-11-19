using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Raise = NSubstitute.Raise;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IChocolateDispenser))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.ICashRegister))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IClock))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IReceiptPrinter))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we simulate a complete unit test: a <see cref="ChocolateShop" /> sells chocolate to three customers
///     with four mocked dependencies, of which it uses only some members. The mocks are set up with exact values, argument
///     matchers, a predicate, a computed return value, two setups on the same method and a property. The last sale fails
///     at the dispenser, which then raises an event that makes the shop set a property. Afterwards the calls, a call that
///     must not have happened and the property write are verified.
/// </summary>
public class CompleteWorkflowBenchmarks : BenchmarksBase
{
	private const decimal PricePerBar = 1.5m;
	private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

	/// <summary>
	///     <see href="https://docs.testably.org/Mockolate" />
	/// </summary>
	[Benchmark(Baseline = true)]
	public bool Complete_Mockolate()
	{
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		ICashRegister register = ICashRegister.CreateMock();
		IClock clock = IClock.CreateMock();
		IReceiptPrinter printer = IReceiptPrinter.CreateMock();
		dispenser.Mock.Setup.Dispense("Dark", 3).Returns(true);
		dispenser.Mock.Setup.Dispense("Milk", It.IsAny<int>()).Returns(false);
		register.Mock.Setup.Total(It.IsAny<string>(), It.IsAny<int>()).Returns((_, amount) => amount * PricePerBar);
		register.Mock.Setup.Charge(It.IsAny<string>(), It.Satisfies<decimal>(total => total > 0)).Returns(true);
		clock.Mock.Setup.Now.InitializeWith(Now);

		ChocolateShop shop = new(dispenser, register, clock, printer);
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.Mock.Raise.SoldOut(null, EventArgs.Empty);

		register.Mock.Verify.Charge(It.IsAny<string>(), It.IsAny<decimal>()).Exactly(3);
		dispenser.Mock.Verify.Dispense("Dark", 3).Twice();
		printer.Mock.Verify.Print("Alice", It.IsAny<string>()).Once();
		printer.Mock.Verify.Print("Carol", It.IsAny<string>()).Never();
		register.Mock.Verify.IsOpen.Set(false).Once();
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/themidnightgospel/Imposter" />
	/// </summary>
	[Benchmark]
	public bool Complete_Imposter()
	{
		IChocolateDispenserImposter dispenser = IChocolateDispenser.Imposter();
		ICashRegisterImposter register = ICashRegister.Imposter();
		IClockImposter clock = IClock.Imposter();
		IReceiptPrinterImposter printer = IReceiptPrinter.Imposter();
		dispenser.Dispense("Dark", 3).Returns(true);
		dispenser.Dispense("Milk", Imposter.Abstractions.Arg<int>.Any()).Returns(false);
		register.Total(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<int>.Any()).Returns((_, amount) => amount * PricePerBar);
		register.Charge(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<decimal>.Is(total => total > 0)).Returns(true);
		clock.Now.Getter().Returns(Now);

		ChocolateShop shop = new(dispenser.Instance(), register.Instance(), clock.Instance(), printer.Instance());
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.SoldOut.Raise(null!, EventArgs.Empty);

		register.Charge(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<decimal>.Any()).Called(Count.Exactly(3));
		dispenser.Dispense("Dark", 3).Called(Count.Twice());
		printer.Print("Alice", Imposter.Abstractions.Arg<string>.Any()).Called(Count.Once());
		printer.Print("Carol", Imposter.Abstractions.Arg<string>.Any()).Called(Count.Never());
		register.IsOpen.Setter(false).Called(Count.Once());
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/thomhurst/TUnit/" />
	/// </summary>
	[Benchmark]
	public bool Complete_TUnitMocks()
	{
		CompleteWorkflowBenchmarks_IChocolateDispenserMock dispenser = IChocolateDispenser.Mock();
		CompleteWorkflowBenchmarks_ICashRegisterMock register = ICashRegister.Mock();
		CompleteWorkflowBenchmarks_IClockMock clock = IClock.Mock();
		CompleteWorkflowBenchmarks_IReceiptPrinterMock printer = IReceiptPrinter.Mock();
		dispenser.Dispense("Dark", 3).Returns(true);
		dispenser.Dispense("Milk", Any<int>()).Returns(false);
		register.Total(Any<string>(), Any<int>()).Returns((_, amount) => amount * PricePerBar);
		register.Charge(Any<string>(), Is<decimal>(total => total > 0)).Returns(true);
		clock.Now.Returns(Now);

		ChocolateShop shop = new(dispenser.Object, register.Object, clock.Object, printer.Object);
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.RaiseSoldOut(EventArgs.Empty);

		register.Charge(Any<string>(), Any<decimal>()).WasCalled(TUnit.Mocks.Times.Exactly(3));
		dispenser.Dispense("Dark", 3).WasCalled(TUnit.Mocks.Times.Exactly(2));
		printer.Print("Alice", Any<string>()).WasCalled(TUnit.Mocks.Times.Once);
		printer.Print("Carol", Any<string>()).WasNeverCalled();
		register.IsOpen.Set(false).WasCalled(TUnit.Mocks.Times.Once);
		return result;
	}

	/// <summary>
	///     <see href="https://github.com/devlooped/moq" />
	/// </summary>
	[Benchmark]
	public bool Complete_Moq()
	{
		Moq.Mock<IChocolateDispenser> dispenser = new();
		Moq.Mock<ICashRegister> register = new();
		Moq.Mock<IClock> clock = new();
		Moq.Mock<IReceiptPrinter> printer = new();
		dispenser.Setup(x => x.Dispense("Dark", 3)).Returns(true);
		dispenser.Setup(x => x.Dispense("Milk", Moq.It.IsAny<int>())).Returns(false);
		register.Setup(x => x.Total(Moq.It.IsAny<string>(), Moq.It.IsAny<int>())).Returns((string _, int amount) => amount * PricePerBar);
		register.Setup(x => x.Charge(Moq.It.IsAny<string>(), Moq.It.Is<decimal>(total => total > 0))).Returns(true);
		clock.SetupGet(x => x.Now).Returns(Now);

		ChocolateShop shop = new(dispenser.Object, register.Object, clock.Object, printer.Object);
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.Raise(x => x.SoldOut += null, null!, EventArgs.Empty);

		register.Verify(x => x.Charge(Moq.It.IsAny<string>(), Moq.It.IsAny<decimal>()), Times.Exactly(3));
		dispenser.Verify(x => x.Dispense("Dark", 3), Times.Exactly(2));
		printer.Verify(x => x.Print("Alice", Moq.It.IsAny<string>()), Times.Once());
		printer.Verify(x => x.Print("Carol", Moq.It.IsAny<string>()), Times.Never());
		register.VerifySet(x => x.IsOpen = false, Times.Once());
		return result;
	}

	/// <summary>
	///     <see href="https://nsubstitute.github.io/" />
	/// </summary>
	[Benchmark]
	public bool Complete_NSubstitute()
	{
		IChocolateDispenser dispenser = Substitute.For<IChocolateDispenser>();
		ICashRegister register = Substitute.For<ICashRegister>();
		IClock clock = Substitute.For<IClock>();
		IReceiptPrinter printer = Substitute.For<IReceiptPrinter>();
		dispenser.Dispense("Dark", 3).Returns(true);
		dispenser.Dispense("Milk", Arg.Any<int>()).Returns(false);
		register.Total(Arg.Any<string>(), Arg.Any<int>()).Returns(call => call.ArgAt<int>(1) * PricePerBar);
		register.Charge(Arg.Any<string>(), Arg.Is<decimal>(total => total > 0)).Returns(true);
		clock.Now.Returns(Now);

		ChocolateShop shop = new(dispenser, register, clock, printer);
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.SoldOut += Raise.EventWith(null!, EventArgs.Empty);

		register.Received(3).Charge(Arg.Any<string>(), Arg.Any<decimal>());
		dispenser.Received(2).Dispense("Dark", 3);
		printer.Received(1).Print("Alice", Arg.Any<string>());
		printer.DidNotReceive().Print("Carol", Arg.Any<string>());
		register.Received(1).IsOpen = false;
		return result;
	}

	/// <summary>
	///     <see href="https://fakeiteasy.github.io/" />
	/// </summary>
	[Benchmark]
	public bool Complete_FakeItEasy()
	{
		IChocolateDispenser dispenser = A.Fake<IChocolateDispenser>();
		ICashRegister register = A.Fake<ICashRegister>();
		IClock clock = A.Fake<IClock>();
		IReceiptPrinter printer = A.Fake<IReceiptPrinter>();
		A.CallTo(() => dispenser.Dispense("Dark", 3)).Returns(true);
		A.CallTo(() => dispenser.Dispense("Milk", A<int>.Ignored)).Returns(false);
		A.CallTo(() => register.Total(A<string>.Ignored, A<int>.Ignored)).ReturnsLazily((string _, int amount) => amount * PricePerBar);
		A.CallTo(() => register.Charge(A<string>.Ignored, A<decimal>.That.Matches(total => total > 0))).Returns(true);
		A.CallTo(() => clock.Now).Returns(Now);

		ChocolateShop shop = new(dispenser, register, clock, printer);
		shop.Sell("Alice", "Dark", 3);
		shop.Sell("Bob", "Dark", 3);
		bool result = shop.Sell("Carol", "Milk", 2);
		dispenser.SoldOut += FakeItEasy.Raise.FreeForm.With(null, EventArgs.Empty);

		A.CallTo(() => register.Charge(A<string>.Ignored, A<decimal>.Ignored)).MustHaveHappened(3, FakeItEasy.Times.Exactly);
		A.CallTo(() => dispenser.Dispense("Dark", 3)).MustHaveHappenedTwiceExactly();
		A.CallTo(() => printer.Print("Alice", A<string>.Ignored)).MustHaveHappenedOnceExactly();
		A.CallTo(() => printer.Print("Carol", A<string>.Ignored)).MustNotHaveHappened();
		A.CallToSet(() => register.IsOpen).To(false).MustHaveHappenedOnceExactly();
		return result;
	}

	public interface IChocolateDispenser
	{
		int Capacity { get; }
		bool IsJammed { get; }
		event EventHandler? SoldOut;
		bool Dispense(string type, int amount);
		int Remaining(string type);
		void Refill(string type, int amount);
		void Clean();
	}

	public interface ICashRegister
	{
		bool IsOpen { get; set; }
		string Currency { get; }
		decimal Total(string type, int amount);
		bool Charge(string customer, decimal amount);
		bool Refund(string customer, decimal amount);
		decimal Balance();
	}

	public interface IClock
	{
		DateTime Now { get; }
		DateTime UtcNow { get; }
		DateTime Today { get; }
		TimeSpan Elapsed(DateTime since);
		bool IsHoliday(DateTime date);
	}

	public interface IReceiptPrinter
	{
		bool IsOnline { get; }
		int PaperLeft { get; }
		void Print(string customer, string text);
		void Reprint(string customer);
		void Feed(int lines);
	}

	private sealed class ChocolateShop
	{
		private readonly IClock _clock;
		private readonly IChocolateDispenser _dispenser;
		private readonly IReceiptPrinter _printer;
		private readonly ICashRegister _register;

		public ChocolateShop(
			IChocolateDispenser dispenser,
			ICashRegister register,
			IClock clock,
			IReceiptPrinter printer)
		{
			_dispenser = dispenser;
			_register = register;
			_clock = clock;
			_printer = printer;
			_dispenser.SoldOut += (_, _) => _register.IsOpen = false;
		}

		public bool Sell(string customer, string type, int amount)
		{
			decimal total = _register.Total(type, amount);
			if (!_register.Charge(customer, total) || !_dispenser.Dispense(type, amount))
			{
				return false;
			}

			_printer.Print(customer, $"{amount} x {type} on {_clock.Now:yyyy-MM-dd}");
			return true;
		}
	}
}
#pragma warning restore CA1822 // Mark members as static
