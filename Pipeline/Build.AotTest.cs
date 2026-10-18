using System.Runtime.InteropServices;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable UnusedMember.Local
// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	/// <summary>
	///     Runs the smoke tests from the compiled output and again published with Native AOT for the current platform.
	/// </summary>
	/// <remarks>
	///     Most of the code that runs in a test is generated into the consumer's assembly, which the analyzers of the
	///     library never see, so only publishing a consumer verifies that the generated mocks work without the JIT.
	/// </remarks>
	Target AotSmokeTests => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			Project project = Solution.Tests.Aot.Mockolate_AotCompatibility_TestApp;
			DotNetRun(s => s
				.SetProjectFile(project)
				.SetConfiguration(Configuration)
				.EnableNoBuild());

			AbsolutePath output = ArtifactsDirectory / "Aot" / project.Name;
			DotNetPublish(s => s
				.SetProject(project)
				.SetConfiguration(Configuration)
				.SetRuntime(RuntimeInformation.RuntimeIdentifier)
				.SetOutput(output));

			string executable = output / (project.Name + (EnvironmentInfo.IsWin ? ".exe" : ""));
			ProcessTasks.StartProcess(executable, string.Empty, output)
				.AssertZeroExitCode();
		});
}
