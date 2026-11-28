using System.Threading;
using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed class TrackCallOrderTests
{
	[Test]
	public async Task Then_AcrossTrackedMocks_WhenInOrder_ShouldSucceed()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);
		warehouse.Restock("Milk", 3);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1), warehouse.Mock.Verify.Restock("Milk", 3));
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenTooEarly_ShouldThrowWithRecordedOrder()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		dispenser.Dispense("Dark", 1);
		warehouse.Restock("Dark", 5);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5), then Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) in order, but Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) too early.
			             Recorded order:
			               1. Mockolate.TestHelpers.IChocolateDispenser mock: invoke method Dispense("Dark", 1)
			               2. Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock: invoke method Restock("Dark", 5)
			             """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenNotCalled_ShouldThrowNotAtAll()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		warehouse.Restock("Dark", 5);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5), then Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) in order, but Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) not at all.
			             Recorded order:
			               1. Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock: invoke method Restock("Dark", 5)
			             """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenCalledOutsideOfTheScope_ShouldThrow()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		warehouse.Restock("Dark", 5);
		using (warehouse.Mock.TrackCallOrder(dispenser.Mock))
		{
			dispenser.Dispense("Dark", 1);
		}

		warehouse.Restock("Milk", 3);

		void Act()
		{
			dispenser.Mock.Verify.Dispense("Dark", 1)
				.Then(warehouse.Mock.Verify.Restock(It.IsAny<string>(), It.IsAny<int>()));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1), then Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock(It.IsAny<string>(), It.IsAny<int>()) in order, but Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock(It.IsAny<string>(), It.IsAny<int>()) not while the call order was tracked.
			             Recorded order:
			               1. Mockolate.TestHelpers.IChocolateDispenser mock: invoke method Dispense("Dark", 1)
			             """).IgnoringNewlineStyle()
			.Because("only the calls between TrackCallOrder and the disposal of its scope have an order");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenNoCallWasTracked_ShouldThrow()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5), then Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) in order, but Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5) not while the call order was tracked. No calls were recorded while the call order was tracked.
			             """);
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenInteractionsWereCleared_ShouldOnlyCompareTheNewCalls()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);
		dispenser.Dispense("Dark", 1);
		warehouse.Restock("Dark", 5);

		dispenser.Mock.ClearAllInteractions();
		dispenser.Dispense("Dark", 1);

		warehouse.Mock.Verify.Restock("Dark", 5).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		void Act()
		{
			dispenser.Mock.Verify.Dispense("Dark", 1).Then(warehouse.Mock.Verify.Restock("Dark", 5));
		}

		await That(Act).Throws<MockVerificationException>()
			.Because("the first dispense was cleared, so only the dispense after the restock is left");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_ShouldSupportSeveralCallsOfOneMock()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		dispenser.Dispense("Dark", 1);
		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);

		void Act()
		{
			dispenser.Mock.Verify.Dispense("Dark", 1)
				.Then(warehouse.Mock.Verify.Restock("Dark", 5), dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).DoesNotThrow()
			.Because("the second step must match the dispense after the restock, not the first one again");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WithinTimeout_ShouldWaitForCallsOnBackgroundThreads()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		warehouse.Restock("Dark", 5);
		Task background = Task.Run(async () =>
		{
			await Task.Delay(50, CancellationToken.None);
			dispenser.Dispense("Dark", 1);
		}, CancellationToken.None);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1).Within(5.Seconds()));
		}

		await That(Act).DoesNotThrow();
		await background;
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_ShouldSupportDelegateMocks()
	{
		ChocolateDispensedDelegate onDispensed = ChocolateDispensedDelegate.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = dispenser.Mock.TrackCallOrder(onDispensed.Mock);

		dispenser.Dispense("Dark", 1);
		onDispensed("Dark", 1);

		void Act()
		{
			onDispensed.Mock.Verify("Dark", 1).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*Mockolate.TestHelpers.ChocolateDispensedDelegate mock invoked*too early*").AsWildcard();
	}

	[Test]
	public async Task Then_AcrossUntrackedMocks_ShouldThrowMockException()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"The call order of the Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock and the Mockolate.TestHelpers.IChocolateDispenser mock was not tracked. Call `TrackCallOrder` on one of the mocks with the others before the calls, e.g. `sut.Mock.TrackCallOrder(other.Mock)`.");
	}

	[Test]
	public async Task Then_AcrossTrackedAndUntrackedMocks_ShouldNameTheUntrackedMock()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateDispenser otherDispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(otherDispenser.Mock);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"The call order of the Mockolate.TestHelpers.IChocolateDispenser mock was not tracked. Call `TrackCallOrder` on one of the mocks with the others before the calls, e.g. `sut.Mock.TrackCallOrder(other.Mock)`.");
	}

	[Test]
	public async Task Then_OnMonitors_ShouldThrowMockException()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);
		MockMonitor<Mock.IMockVerifyForIChocolateDispenser> monitor = dispenser.Mock.Monitor();
		using (monitor.Run())
		{
			warehouse.Restock("Dark", 5);
			dispenser.Dispense("Dark", 1);
		}

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5).Then(monitor.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockException>()
			.WithMessage("The call order of the mock was not tracked.*").AsWildcard()
			.Because("a monitor records the calls in its own interactions, whose order is not tracked");
	}

	[Test]
	public async Task Then_AcrossMocksTrackedSeparately_ShouldThrowMockException()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateDispenser otherDispenser = IChocolateDispenser.CreateMock();
		IChocolateWarehouse otherWarehouse = IChocolateWarehouse.CreateMock();
		using IDisposable first = warehouse.Mock.TrackCallOrder(otherWarehouse.Mock);
		using IDisposable second = dispenser.Mock.TrackCallOrder(otherDispenser.Mock);

		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"The call order of the Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock and the Mockolate.TestHelpers.IChocolateDispenser mock was not tracked in the same scope. A mock only keeps the call order of the latest scope that tracked it.");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenClearedAfterTheScope_ShouldNotCountTheRemovedCalls()
	{
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		using (dispenser.Mock.TrackCallOrder(warehouse.Mock))
		{
			_ = dispenser.TotalDispensed;
			warehouse.Restock("Dark", 5);
		}

		dispenser.Mock.ClearAllInteractions();
		_ = dispenser.TotalDispensed;

		void Act()
		{
			dispenser.Mock.Verify.TotalDispensed.Got().Then(warehouse.Mock.Verify.Restock("Dark", 5));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.TestHelpers.IChocolateDispenser mock got property TotalDispensed, then Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5) in order, but Mockolate.TestHelpers.IChocolateDispenser mock got property TotalDispensed not while the call order was tracked.
			             Recorded order:
			               1. Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock: invoke method Restock("Dark", 5)
			             """).IgnoringNewlineStyle()
			.Because("the read of the property within the scope was cleared, and the read afterwards was not tracked");
	}

	[Test]
	public async Task Then_AcrossTrackedMocksOfTheSameType_ShouldNumberTheMocks()
	{
		IChocolateDispenser first = IChocolateDispenser.CreateMock();
		IChocolateDispenser second = IChocolateDispenser.CreateMock();
		using IDisposable _ = first.Mock.TrackCallOrder(second.Mock);

		second.Dispense("Dark", 2);
		first.Dispense("Dark", 1);

		void Act()
		{
			first.Mock.Verify.Dispense("Dark", 1).Then(second.Mock.Verify.Dispense("Dark", 2));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.TestHelpers.IChocolateDispenser mock #1 invoked method Dispense("Dark", 1), then Mockolate.TestHelpers.IChocolateDispenser mock #2 invoked method Dispense("Dark", 2) in order, but Mockolate.TestHelpers.IChocolateDispenser mock #2 invoked method Dispense("Dark", 2) too early.
			             Recorded order:
			               1. Mockolate.TestHelpers.IChocolateDispenser mock #2: invoke method Dispense("Dark", 2)
			               2. Mockolate.TestHelpers.IChocolateDispenser mock #1: invoke method Dispense("Dark", 1)
			             """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task Then_AcrossThreeTrackedMocks_ShouldVerifyTheOrder()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		ChocolateDispensedDelegate onDispensed = ChocolateDispensedDelegate.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock, onDispensed.Mock);

		warehouse.Restock("Dark", 5);
		dispenser.Dispense("Dark", 1);
		onDispensed("Dark", 1);

		warehouse.Mock.Verify.Restock("Dark", 5)
			.Then(dispenser.Mock.Verify.Dispense("Dark", 1), onDispensed.Mock.Verify("Dark", 1));
		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(onDispensed.Mock.Verify("Dark", 1), dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("*but Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense(\"Dark\", 1) too early.*").AsWildcard();
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_ShouldDistinguishReadsOfAPropertyGetter()
	{
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		using IDisposable scope = dispenser.Mock.TrackCallOrder(warehouse.Mock);

		_ = dispenser.TotalDispensed;
		warehouse.Restock("Dark", 5);
		_ = dispenser.TotalDispensed;

		dispenser.Mock.Verify.TotalDispensed.Got()
			.Then(warehouse.Mock.Verify.Restock("Dark", 5), dispenser.Mock.Verify.TotalDispensed.Got());
		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.TotalDispensed.Got(), dispenser.Mock.Verify.TotalDispensed.Got());
		}

		await That(Act).Throws<MockVerificationException>()
			.Because("every read of the property records the same interaction, but only one read follows the restock");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WhenWithinTimesOut_ShouldThrowWithTheExpectations()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		warehouse.Restock("Dark", 5);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5)
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1).Within(50.Milliseconds()));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("""
			             Expected that Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock invoked method Restock("Dark", 5), then Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1) in order, but it timed out after 00:00:00.0500000 waiting until Mockolate.TestHelpers.IChocolateDispenser mock invoked method Dispense("Dark", 1).
			             Recorded order:
			               1. Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock: invoke method Restock("Dark", 5)
			             """).IgnoringNewlineStyle();
	}

	[Test]
	public async Task Then_AcrossMocksOfTheSameTypeTrackedSeparately_ShouldNameBothMocks()
	{
		IChocolateDispenser first = IChocolateDispenser.CreateMock();
		IChocolateDispenser second = IChocolateDispenser.CreateMock();
		using IDisposable firstScope = first.Mock.TrackCallOrder(IChocolateWarehouse.CreateMock().Mock);
		using IDisposable secondScope = second.Mock.TrackCallOrder(IChocolateWarehouse.CreateMock().Mock);

		void Act()
		{
			first.Mock.Verify.Dispense("Dark", 1).Then(second.Mock.Verify.Dispense("Dark", 2));
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"The call order of the Mockolate.TestHelpers.IChocolateDispenser mock and the Mockolate.TestHelpers.IChocolateDispenser mock was not tracked in the same scope. A mock only keeps the call order of the latest scope that tracked it.");
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_ShouldSupportClassMocks()
	{
		MyServiceBase service = MyServiceBase.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = service.Mock.TrackCallOrder(dispenser.Mock);

		service.DoSomething(1, true);
		dispenser.Dispense("Dark", 1);

		void Act()
		{
			service.Mock.Verify.DoSomething(1, true).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Then_AcrossTrackedMocks_WithinTimeoutOnAnEarlierStep_ShouldWaitForIt()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		Task background = Task.Run(async () =>
		{
			await Task.Delay(50, CancellationToken.None);
			warehouse.Restock("Dark", 5);
			dispenser.Dispense("Dark", 1);
		}, CancellationToken.None);

		void Act()
		{
			warehouse.Mock.Verify.Restock("Dark", 5).Within(5.Seconds())
				.Then(dispenser.Mock.Verify.Dispense("Dark", 1).Within(5.Seconds()));
		}

		await That(Act).DoesNotThrow();
		await background;
	}

	[Test]
	public async Task Then_OnOneMock_ShouldNotRequireTracking()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 2);

		sut.Mock.Verify.Dispense("Dark", 1).Then(sut.Mock.Verify.Dispense("Dark", 2));
		void Act()
		{
			sut.Mock.Verify.Dispense("Dark", 2).Then(sut.Mock.Verify.Dispense("Dark", 1));
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage(
				"Expected that mock invoked method Dispense(\"Dark\", 2), then invoked method Dispense(\"Dark\", 1) in order, but it invoked method Dispense(\"Dark\", 1) too early.");
	}

	[Test]
	public async Task TrackCallOrder_WhenAlreadyTracked_ShouldThrowMockException()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateDispenser otherDispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);

		void Act()
		{
			otherDispenser.Mock.TrackCallOrder(warehouse.Mock);
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"The call order of the Mockolate.Tests.Verify.TrackCallOrderTests.IChocolateWarehouse mock is already tracked. Dispose the previous scope before tracking it again.");
	}

	[Test]
	public async Task TrackCallOrder_WhenAlreadyTracked_ShouldNotTrackTheOtherMocks()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateDispenser otherDispenser = IChocolateDispenser.CreateMock();
		using IDisposable _ = warehouse.Mock.TrackCallOrder(dispenser.Mock);
		await That(() => otherDispenser.Mock.TrackCallOrder(warehouse.Mock)).Throws<MockException>();

		void Act()
		{
			using IDisposable scope = otherDispenser.Mock.TrackCallOrder(IChocolateWarehouse.CreateMock().Mock);
		}

		await That(Act).DoesNotThrow()
			.Because("the failed TrackCallOrder must not keep tracking the mocks it already started to track");
	}

	[Test]
	public async Task TrackCallOrder_WhenAlreadyTracked_ShouldKeepTheOrderOfTheEarlierScope()
	{
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateWarehouse busyWarehouse = IChocolateWarehouse.CreateMock();
		using (dispenser.Mock.TrackCallOrder(warehouse.Mock))
		{
			dispenser.Dispense("Dark", 1);
			warehouse.Restock("Dark", 5);
		}

		using IDisposable running = busyWarehouse.Mock.TrackCallOrder(IChocolateDispenser.CreateMock().Mock);
		await That(() => dispenser.Mock.TrackCallOrder(busyWarehouse.Mock)).Throws<MockException>();

		void Act()
		{
			dispenser.Mock.Verify.Dispense("Dark", 1).Then(warehouse.Mock.Verify.Restock("Dark", 5));
		}

		await That(Act).DoesNotThrow()
			.Because("a failed TrackCallOrder must not replace the scope that tracked the dispenser before");
	}

	[Test]
	public async Task TrackCallOrder_AfterDispose_ShouldOnlyCompareTheCallsOfTheNewScope()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		using (warehouse.Mock.TrackCallOrder(dispenser.Mock))
		{
			dispenser.Dispense("Dark", 1);
			warehouse.Restock("Dark", 5);
		}

		using (dispenser.Mock.TrackCallOrder(warehouse.Mock))
		{
			warehouse.Restock("Dark", 5);
			dispenser.Dispense("Dark", 1);
		}

		warehouse.Mock.Verify.Restock("Dark", 5).Then(dispenser.Mock.Verify.Dispense("Dark", 1));
		void Act()
		{
			dispenser.Mock.Verify.Dispense("Dark", 1).Then(warehouse.Mock.Verify.Restock("Dark", 5));
		}

		await That(Act).Throws<MockVerificationException>()
			.Because("the calls of the first scope must not be compared with the calls of the second one");
	}

	[Test]
	public async Task TrackCallOrder_WithTheSameMockTwice_ShouldTrackItOnce()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		void Act()
		{
			using IDisposable scope = sut.Mock.TrackCallOrder(sut.Mock, sut.Mock);
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task Dispose_Twice_ShouldNotThrow()
	{
		IChocolateWarehouse warehouse = IChocolateWarehouse.CreateMock();
		IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();
		IDisposable scope = warehouse.Mock.TrackCallOrder(dispenser.Mock);
		scope.Dispose();

		void Act()
		{
			scope.Dispose();
		}

		await That(Act).DoesNotThrow();
	}

	public interface IChocolateWarehouse
	{
		void Restock(string type, int amount);
	}
}
