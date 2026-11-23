using System.Collections.Generic;
using System.Threading;
using Xunit.Runners;

namespace Mockolate.Xunit2.Tests;

public sealed class XunitRunnerTests
{
	[Test]
	public async Task XunitRunner_ShouldRunTheTheoriesWithTheArguments()
	{
		List<string> passed = [];
		List<string> failed = [];
		using ManualResetEventSlim finished = new();
		using (AssemblyRunner runner = AssemblyRunner.WithoutAppDomain(typeof(XunitRunnerTests).Assembly.Location))
		{
			runner.OnTestPassed = info =>
			{
				lock (passed)
				{
					passed.Add(info.TestDisplayName);
				}
			};
			runner.OnTestFailed = info =>
			{
				lock (failed)
				{
					failed.Add($"{info.TestDisplayName}: {info.ExceptionMessage}");
				}
			};
			runner.OnExecutionComplete = _ => finished.Set();
			runner.Start(new AssemblyRunnerStartOptions { TypesToRun = [typeof(XunitTheories).FullName!,], });
			finished.Wait(TimeSpan.FromMinutes(1));
			while (runner.Status != AssemblyRunnerStatus.Idle)
			{
				await Task.Delay(10);
			}
		}

		await That(failed).IsEmpty();
		await That(passed).HasCount(6);
	}
}
