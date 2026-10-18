using Mockolate.Monitor;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockExtensionsTests
{
	public sealed class ClearAllInteractionsTests
	{
		[Test]
		public async Task Monitor_WhenRunning_ShouldClearAllInteractions()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			sut.Dispense("Dark", 1);
			MockMonitor<Mock.IMockVerifyForIChocolateDispenser> monitor = sut.Mock.Monitor();
			using (_ = monitor.Run())
			{
				sut.Dispense("Light", 2);
				sut.Dispense("Dark", 3);
				sut.Mock.ClearAllInteractions();
			}

			sut.Dispense("Light", 4);

			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(3))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(4))).Once();

			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(3))).Never();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(4))).Never();
		}

		[Test]
		public async Task Monitor_WhenRunning_ShouldContinueAfterClearAllInteractions()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			sut.Dispense("Dark", 1);
			MockMonitor<Mock.IMockVerifyForIChocolateDispenser> monitor = sut.Mock.Monitor();
			using (_ = monitor.Run())
			{
				sut.Dispense("Light", 2);
				sut.Mock.ClearAllInteractions();
				sut.Dispense("Dark", 3);
			}

			sut.Dispense("Light", 4);

			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(3))).Once();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(4))).Once();

			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(3))).Once();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(4))).Never();
		}

		[Test]
		public async Task Monitor_WhenStopped_ShouldIgnoreClearAllInteractions()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			sut.Dispense("Dark", 1);
			MockMonitor<Mock.IMockVerifyForIChocolateDispenser> monitor = sut.Mock.Monitor();
			using (_ = monitor.Run())
			{
				sut.Dispense("Light", 2);
				sut.Dispense("Dark", 3);
			}

			sut.Mock.ClearAllInteractions();
			sut.Dispense("Light", 4);

			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(3))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(4))).Once();

			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(2))).Once();
			await That(monitor.Verify.Dispense(It.Is("Dark"), It.Is(3))).Once();
			await That(monitor.Verify.Dispense(It.Is("Light"), It.Is(4))).Never();
		}

		[Test]
		public async Task ShouldClearRecordedInteractions()
		{
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();

			sut.Dispense("Dark", 1);
			sut.Dispense("Light", 2);
			sut.Mock.ClearAllInteractions();
			sut.Dispense("Dark", 3);
			sut.Dispense("Light", 4);

			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(1))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(2))).Never();
			await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(3))).Once();
			await That(sut.Mock.Verify.Dispense(It.Is("Light"), It.Is(4))).Once();
		}
	}
}
