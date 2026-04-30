using Mockolate.Parameters;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class MatchExtensionsTests
{
	[Test]
	public async Task OutParameterMonitor_MultipleMonitors_ShouldAllMonitorValues()
	{
		int idx = 1;
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithOutParam(It.IsOut(() => idx++)
			.Monitor(out IParameterMonitor<int> monitor1)
			.Monitor(out IParameterMonitor<int> monitor2));

		sut.MyMethodWithOutParam(out _);
		sut.MyMethodWithOutParam(out _);
		sut.MyMethodWithOutParam(out _);
		sut.MyMethodWithOptionalParameters(5);

		await That(monitor1.Values).IsEqualTo([1, 2, 3,]);
		await That(monitor2.Values).IsEqualTo([1, 2, 3,]);
	}

	[Test]
	public async Task OutParameterMonitor_ShouldMonitorReceivedValues()
	{
		int idx = 1;
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithOutParam(
			It.IsOut(() => idx++).Monitor(out IParameterMonitor<int> monitor));

		sut.MyMethodWithOutParam(out _);
		sut.MyMethodWithOutParam(out _);

		await That(monitor.Values).IsEqualTo([1, 2,]);
	}

	[Test]
	public async Task ParameterMonitor_MultipleMonitors_ShouldAllMonitorValues()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(
			It.IsAny<string>()
				.Monitor(out IParameterMonitor<string> monitorA),
			It.IsAny<int>()
				.Monitor(out IParameterMonitor<int> monitorB1)
				.Monitor(out IParameterMonitor<int> monitorB2));

		sut.Dispense("Dark", 5);
		sut.Dispense("White", 6);

		await That(monitorA.Values).IsEqualTo(["Dark", "White",]);
		await That(monitorB1.Values).IsEqualTo([5, 6,]);
		await That(monitorB2.Values).IsEqualTo([5, 6,]);
	}

	[Test]
	public async Task ParameterMonitor_ShouldMonitorReceivedValues()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>().Monitor(out IParameterMonitor<string> monitor), It.IsAny<int>());

		sut.Dispense("Dark", 5);
		sut.Dispense("White", 5);

		await That(monitor.Values).IsEqualTo(["Dark", "White",]);
	}

	[Test]
	public async Task ParameterMonitor_WithFilter_ShouldMonitorMatchingValues()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(),
			It.Satisfies<int>(x => x > 4).Monitor(out IParameterMonitor<int> monitor));

		sut.Dispense("Dark", 3);
		sut.Dispense("White", 5);
		sut.Dispense("White", 4);
		sut.Dispense("White", 6);
		sut.Dispense("White", 7);
		sut.Dispense("White", 1);
		sut.Dispense("White", 8);

		await That(monitor.Values).IsEqualTo([5, 6, 7, 8,]);
	}

	[Test]
	public async Task RefParameterMonitor_MultipleMonitors_ShouldAllMonitorValues()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParam(It.IsRef<int>(v => 2 * v)
			.Monitor(out IParameterMonitor<int> monitor1)
			.Monitor(out IParameterMonitor<int> monitor2));
		int value1 = 3;
		int value2 = 7;

		sut.MyMethodWithRefParam(ref value1);
		sut.MyMethodWithRefParam(ref value2);

		await That(monitor1.Values).IsEqualTo([6, 14,]);
		await That(monitor2.Values).IsEqualTo([6, 14,]);
	}

	[Test]
	public async Task RefParameterMonitor_ShouldMonitorReceivedValues()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParam(It.IsRef<int>(v => 2 * v)
			.Monitor(out IParameterMonitor<int> monitor));
		int value1 = 3;
		int value2 = 7;

		sut.MyMethodWithRefParam(ref value1);
		sut.MyMethodWithRefParam(ref value2);

		await That(monitor.Values).IsEqualTo([6, 14,]);
	}

	[Test]
	public async Task RefParameterMonitor_WithFilter_ShouldMonitorMatchingValues()
	{
		IMyService sut = IMyService.CreateMock();
		sut.Mock.Setup.MyMethodWithRefParam(It.IsRef<int>(i => i > 4, v => 2 * v)
			.Monitor(out IParameterMonitor<int> monitor));

		int[] values = [3, 5, 4, 6, 7, 1, 8,];
		foreach (int value in values)
		{
			int refValue = value;
			sut.MyMethodWithRefParam(ref refValue);
		}

		await That(monitor.Values).IsEqualTo([10, 12, 14, 16,]);
	}
}
