using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.Monitor;

public sealed class MockMonitorTests
{
	[Fact]
	public async Task ClearAllInteractions_WhenMonitorIsRunning_ShouldClearInternalCollection()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		sut.IsValid(1);
		using IDisposable disposable = monitor.Run();
		sut.IsValid(1);
		await That(monitor.Verify.IsValid(It.Is(1))).Once();
		sut.Mock.ClearAllInteractions();
		await That(monitor.Verify.IsValid(It.Is(1))).Never();
	}

	[Fact]
	public async Task DisposeTwice_ShouldNotIncludeMoreInvocations()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		sut.IsValid(1);
		sut.IsValid(2);
		IDisposable disposable = monitor.Run();
		sut.IsValid(3);
		sut.IsValid(4);
		disposable.Dispose();
		sut.IsValid(5);
		sut.IsValid(6);
		disposable.Dispose();
		sut.IsValid(7);
		sut.IsValid(8);

		await That(monitor.Verify.IsValid(It.Is(1))).Never();
		await That(monitor.Verify.IsValid(It.Is(2))).Never();
		await That(monitor.Verify.IsValid(It.Is(3))).Once();
		await That(monitor.Verify.IsValid(It.Is(4))).Once();
		await That(monitor.Verify.IsValid(It.Is(5))).Never();
		await That(monitor.Verify.IsValid(It.Is(6))).Never();
		await That(monitor.Verify.IsValid(It.Is(7))).Never();
		await That(monitor.Verify.IsValid(It.Is(8))).Never();
	}

	[Fact]
	public async Task MultipleRun_ShouldMonitorInvocationsDuringTheRun()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		sut.IsValid(1);
		sut.IsValid(2);
		using (monitor.Run())
		{
			sut.IsValid(3);
			sut.IsValid(4);
		}

		sut.IsValid(5);
		sut.IsValid(6);
		using (monitor.Run())
		{
			sut.IsValid(7);
			sut.IsValid(8);
		}

		sut.IsValid(9);
		sut.IsValid(10);

		await That(monitor.Verify.IsValid(It.Is(1))).Never();
		await That(monitor.Verify.IsValid(It.Is(2))).Never();
		await That(monitor.Verify.IsValid(It.Is(3))).Once();
		await That(monitor.Verify.IsValid(It.Is(4))).Once();
		await That(monitor.Verify.IsValid(It.Is(5))).Never();
		await That(monitor.Verify.IsValid(It.Is(6))).Never();
		await That(monitor.Verify.IsValid(It.Is(7))).Once();
		await That(monitor.Verify.IsValid(It.Is(8))).Once();
		await That(monitor.Verify.IsValid(It.Is(9))).Never();
		await That(monitor.Verify.IsValid(It.Is(10))).Never();
	}

	[Fact]
	public async Task NestedRun_ShouldThrowInvalidOperationException()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		void Act()
		{
			monitor.Run();
		}

		IDisposable outerRun = monitor.Run();

		await That(Act).Throws<InvalidOperationException>()
			.WithMessage("Monitoring is already running. Dispose the previous scope before starting a new one.");

		outerRun.Dispose();

		await That(Act).DoesNotThrow();
	}

	[Fact]
	public async Task Run_ShouldIncludeAllInvocations()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		using (monitor.Run())
		{
			sut.IsValid(1);
			sut.IsValid(2);
			sut.IsValid(3);
			sut.IsValid(4);
		}

		await That(monitor.Verify.IsValid(It.Is(1))).Once();
		await That(monitor.Verify.IsValid(It.Is(2))).Once();
		await That(monitor.Verify.IsValid(It.Is(3))).Once();
		await That(monitor.Verify.IsValid(It.Is(4))).Once();
	}

	[Fact]
	public async Task Run_ShouldMonitorInvocationsDuringTheRun()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		sut.IsValid(1);
		sut.IsValid(2);
		using (monitor.Run())
		{
			sut.IsValid(3);
			sut.IsValid(4);
		}

		sut.IsValid(5);

		await That(monitor.Verify.IsValid(It.Is(1))).Never();
		await That(monitor.Verify.IsValid(It.Is(2))).Never();
		await That(monitor.Verify.IsValid(It.Is(3))).Once();
		await That(monitor.Verify.IsValid(It.Is(4))).Once();
		await That(monitor.Verify.IsValid(It.Is(5))).Never();
		await That(sut.Mock.Verify.IsValid(It.Is(1))).Once();
		await That(sut.Mock.Verify.IsValid(It.Is(2))).Once();
		await That(sut.Mock.Verify.IsValid(It.Is(3))).Once();
		await That(sut.Mock.Verify.IsValid(It.Is(4))).Once();
		await That(sut.Mock.Verify.IsValid(It.Is(5))).Once();
	}

	[Fact]
	public async Task Verify_WhileRunning_ShouldBeUpToDate()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		using (monitor.Run())
		{
			sut.IsValid(1);
			await That(monitor.Verify.IsValid(It.Is(1))).Once();
			await That(monitor.Verify.IsValid(It.Is(2))).Never();
			await That(monitor.Verify.IsValid(It.Is(3))).Never();
			await That(monitor.Verify.IsValid(It.Is(4))).Never();

			sut.IsValid(2);
			await That(monitor.Verify.IsValid(It.Is(1))).Once();
			await That(monitor.Verify.IsValid(It.Is(2))).Once();
			await That(monitor.Verify.IsValid(It.Is(3))).Never();
			await That(monitor.Verify.IsValid(It.Is(4))).Never();

			sut.IsValid(3);
			await That(monitor.Verify.IsValid(It.Is(1))).Once();
			await That(monitor.Verify.IsValid(It.Is(2))).Once();
			await That(monitor.Verify.IsValid(It.Is(3))).Once();
			await That(monitor.Verify.IsValid(It.Is(4))).Never();
		}
	}

	[Fact]
	public async Task WhenMonitoringIsNotDisposed_ShouldStillVerify()
	{
		IMyService sut = IMyService.CreateMock();
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		sut.IsValid(1);
		sut.IsValid(2);
		using IDisposable disposable = monitor.Run();
		sut.IsValid(3);
		sut.IsValid(4);

		await That(monitor.Verify.IsValid(It.Is(1))).Never();
		await That(monitor.Verify.IsValid(It.Is(2))).Never();
		await That(monitor.Verify.IsValid(It.Is(3))).Once();
		sut.IsValid(5);
		await That(monitor.Verify.IsValid(It.Is(4))).Once();
		await That(monitor.Verify.IsValid(It.Is(5))).Once();
		await That(monitor.Verify.IsValid(It.Is(6))).Never();
	}

	[Fact]
	public async Task WhenSkippingInteractionRecording_Verify_ShouldThrow()
	{
		IMyService sut = IMyService.CreateMock(MockBehavior.Default.SkippingInteractionRecording());
		MockMonitor<Mock.IMockVerifyForIMyService> monitor = sut.Mock.Monitor();

		using IDisposable _ = monitor.Run();
		sut.IsValid(1);

		await That(async () => await That(monitor.Verify.IsValid(It.Is(1))).Once())
			.Throws<MockException>()
			.WithMessage("*SkipInteractionRecording*").AsWildcard();
	}
}
