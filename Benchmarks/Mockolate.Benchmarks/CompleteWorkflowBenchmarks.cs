using BenchmarkDotNet.Attributes;
using FakeItEasy;
using Imposter.Abstractions;
using Mockolate.Benchmarks;
using Mockolate.Verify;
using NSubstitute;
using Arg = NSubstitute.Arg;
using Times = Moq.Times;

[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IChocolateDispenser))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.ICashRegister))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IClock))]
[assembly: GenerateImposter(typeof(CompleteWorkflowBenchmarks.IReceiptPrinter))]

namespace Mockolate.Benchmarks;
#pragma warning disable CA1822 // Mark members as static
/// <summary>
///     In this benchmark we simulate a complete unit test: a <see cref="ChocolateShop" /> sells chocolate to three customers
///     with four mocked dependencies, which are set up with exact values, argument matchers and a property, and verified
///     afterwards. Each mocked member is called two or three times, because the last sale fails at the dispenser.
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
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		ICashRegister register = ICashRegister.CreateMock();
		IClock clock = IClock.CreateMock();
		IReceiptPrinter printer = IReceiptPrinter.CreateMock();
		dispenser.Mock.Setup.Dispense("Dark", 3).Returns(true);
		register.Mock.Setup.Charge(It.IsAny<string>(), It.IsAny<decimal>()).Returns(true);
		clock.Mock.Setup.Now.InitializeWith(Now);

		bool result = SellToCustomers(new ChocolateShop(dispenser, register, clock, printer));

		register.Mock.Verify.Charge(It.IsAny<string>(), It.IsAny<decimal>()).Exactly(3);
		dispenser.Mock.Verify.Dispense("Dark", 3).Twice();
		printer.Mock.Verify.Print("Alice", It.IsAny<string>()).Once();
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
		register.Charge(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<decimal>.Any()).Returns(true);
		clock.Now.Getter().Returns(Now);

		bool result = SellToCustomers(
			new ChocolateShop(dispenser.Instance(), register.Instance(), clock.Instance(), printer.Instance()));

		register.Charge(Imposter.Abstractions.Arg<string>.Any(), Imposter.Abstractions.Arg<decimal>.Any()).Called(Count.Exactly(3));
		dispenser.Dispense("Dark", 3).Called(Count.Twice());
		printer.Print("Alice", Imposter.Abstractions.Arg<string>.Any()).Called(Count.Once());
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
		register.Charge(Any<string>(), Any<decimal>()).Returns(true);
		clock.Now.Returns(Now);

		bool result = SellToCustomers(new ChocolateShop(dispenser.Object, register.Object, clock.Object, printer.Object));

		register.Charge(Any<string>(), Any<decimal>()).WasCalled(TUnit.Mocks.Times.Exactly(3));
		dispenser.Dispense("Dark", 3).WasCalled(TUnit.Mocks.Times.Exactly(2));
		printer.Print("Alice", Any<string>()).WasCalled(TUnit.Mocks.Times.Once);
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
		register.Setup(x => x.Charge(Moq.It.IsAny<string>(), Moq.It.IsAny<decimal>())).Returns(true);
		clock.SetupGet(x => x.Now).Returns(Now);

		bool result = SellToCustomers(new ChocolateShop(dispenser.Object, register.Object, clock.Object, printer.Object));

		register.Verify(x => x.Charge(Moq.It.IsAny<string>(), Moq.It.IsAny<decimal>()), Times.Exactly(3));
		dispenser.Verify(x => x.Dispense("Dark", 3), Times.Exactly(2));
		printer.Verify(x => x.Print("Alice", Moq.It.IsAny<string>()), Times.Once());
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
		register.Charge(Arg.Any<string>(), Arg.Any<decimal>()).Returns(true);
		clock.Now.Returns(Now);

		bool result = SellToCustomers(new ChocolateShop(dispenser, register, clock, printer));

		register.Received(3).Charge(Arg.Any<string>(), Arg.Any<decimal>());
		dispenser.Received(2).Dispense("Dark", 3);
		printer.Received(1).Print("Alice", Arg.Any<string>());
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
		A.CallTo(() => register.Charge(A<string>.Ignored, A<decimal>.Ignored)).Returns(true);
		A.CallTo(() => clock.Now).Returns(Now);

		bool result = SellToCustomers(new ChocolateShop(dispenser, register, clock, printer));

		A.CallTo(() => register.Charge(A<string>.Ignored, A<decimal>.Ignored)).MustHaveHappened(3, FakeItEasy.Times.Exactly);
		A.CallTo(() => dispenser.Dispense("Dark", 3)).MustHaveHappenedTwiceExactly();
		A.CallTo(() => printer.Print("Alice", A<string>.Ignored)).MustHaveHappenedOnceExactly();
		return result;
	}

	private static bool SellToCustomers(ChocolateShop shop)
		=> shop.Sell("Alice", "Dark", 3) & shop.Sell("Bob", "Dark", 3) & !shop.Sell("Carol", "Milk", 2);

	public interface IChocolateDispenser
	{
		bool Dispense(string type, int amount);
	}

	public interface ICashRegister
	{
		bool Charge(string customer, decimal amount);
	}

	public interface IClock
	{
		DateTime Now { get; }
	}

	public interface IReceiptPrinter
	{
		void Print(string customer, string text);
	}

	private sealed class ChocolateShop(
		IChocolateDispenser dispenser,
		ICashRegister register,
		IClock clock,
		IReceiptPrinter printer)
	{
		private const decimal PricePerBar = 1.5m;

		public bool Sell(string customer, string type, int amount)
		{
			if (!register.Charge(customer, amount * PricePerBar) || !dispenser.Dispense(type, amount))
			{
				return false;
			}

			printer.Print(customer, $"{amount} x {type} on {clock.Now:yyyy-MM-dd}");
			return true;
		}
	}
}
#pragma warning restore CA1822 // Mark members as static
