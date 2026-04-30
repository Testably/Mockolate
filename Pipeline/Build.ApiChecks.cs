using Fallout.Common;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	Target ApiChecks => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			Project project = Solution.Tests.Mockolate_Api_Tests;

			DotNetTest(s => s
				.SetConfiguration(Configuration == Configuration.Debug ? "Debug" : "Release")
				.SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
				.EnableNoBuild()
				.SetResultsDirectory(TestResultsDirectory)
				.AddProcessAdditionalArguments(
					$"--project \"{project.Path}\"",
					"--report-trx",
					$"--report-trx-filename {project.Name}.trx"));
		});
}
