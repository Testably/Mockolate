using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit.Runners;

namespace Mockolate.Xunit2.Tests;

public sealed class XunitRunnerTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

	[Test]
	public async Task ShouldRunTheTheoriesWithTheArguments()
	{
		List<string> passed = [];
		List<string> problems = [];
		TaskCompletionSource<bool> finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
		AssemblyRunner runner = AssemblyRunner.WithoutAppDomain(typeof(XunitRunnerTests).Assembly.Location);
		runner.OnTestPassed = info => Add(passed, info.TestDisplayName);
		runner.OnTestFailed = info => Add(problems, $"failed {info.TestDisplayName}: {info.ExceptionMessage}");
		runner.OnTestSkipped = info => Add(problems, $"skipped {info.TestDisplayName}: {info.SkipReason}");
		runner.OnErrorMessage = info => Add(problems, $"error {info.ExceptionType}: {info.ExceptionMessage}");
		runner.OnExecutionComplete = _ => finished.TrySetResult(true);
		runner.Start(new AssemblyRunnerStartOptions { TypesToRun = [typeof(XunitTheories).FullName!,], });

		DateTime deadline = DateTime.UtcNow + Timeout;
		using CancellationTokenSource timeout = new();
		bool hasFinished = await Task.WhenAny(finished.Task, Task.Delay(Timeout, timeout.Token)) == finished.Task;
		timeout.Cancel();
		if (hasFinished)
		{
			while (runner.Status != AssemblyRunnerStatus.Idle && DateTime.UtcNow < deadline)
			{
				await Task.Delay(10);
			}
		}
		else
		{
			runner.Cancel();
		}

		// Dispose throws while the runner is not idle, which would hide the failure below, so a busy runner is left alone.
		if (runner.Status == AssemblyRunnerStatus.Idle)
		{
			runner.Dispose();
		}

		await That(hasFinished).IsTrue().Because("the xUnit runner must finish within a minute");

		await That(problems).IsEmpty();
		await That(passed).Contains(
			"Mockolate.Xunit2.Tests.XunitTheories.DisplayName_ShouldNotReadTheProperties(stock: Mockolate.Frameworks.TestHelpers.Domain.IChocolateStock mock)");
		await That(passed.Select(name => name.Split('(')[0])).IsEqualTo([
			"Mockolate.Xunit2.Tests.XunitTheories.AutoMockArguments_ShouldPassTheParameterMocksToTheClassUnderTest",
			"Mockolate.Xunit2.Tests.XunitTheories.AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach",
			"Mockolate.Xunit2.Tests.XunitTheories.AutoMockArguments_WithSeveralAttributes_ShouldRunOneTestCaseForEach",
			"Mockolate.Xunit2.Tests.XunitTheories.AutoMockArguments_WithSingleNullArgument_ShouldPassNull",
			"Mockolate.Xunit2.Tests.XunitTheories.AutoMockArguments_WithNotSharedParameter_ShouldNotShareItWithTheClassUnderTest",
			"Mockolate.Xunit2.Tests.XunitTheories.DisplayName_ShouldNotReadTheProperties",
		]).InAnyOrder();
	}

	private static void Add(List<string> list, string entry)
	{
		lock (list)
		{
			list.Add(entry);
		}
	}
}
