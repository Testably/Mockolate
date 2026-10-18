using System.Linq;
using Fallout.Common;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	Target UnitTests => _ => _
		.DependsOn(DotNetUnitTests);

	Project[] UnitTestProjects =>
	[
		Solution.Pipeline.Build_Tests,
		Solution.Tests.Mockolate_Tests,
		Solution.Tests.Mockolate_Internal_Tests,
		Solution.Tests.Mockolate_Analyzers_Tests,
		Solution.Tests.Mockolate_SourceGenerators_Tests,
		Solution.Tests.Mockolate_ExampleTests,
	];

	Target DotNetUnitTests => _ => _
		.Unlisted()
		.DependsOn(Compile)
		.Executes(() =>
		{
			string[] excludedFrameworks =
				EnvironmentInfo.IsWin
					? []
					: ["net48",];
			DotNetTest(s => s
					.SetConfiguration(Configuration)
					.SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
					.EnableNoBuild()
					.SetResultsDirectory(TestResultsDirectory)
					.CombineWith(
						UnitTestProjects,
						(settings, project) => settings
							.CombineWith(
								project.GetTargetFrameworks()?.Except(excludedFrameworks),
								(frameworkSettings, framework) => frameworkSettings
									.SetFramework(framework)
									.AddProcessAdditionalArguments(
										$"--project \"{project.Path}\"",
										"--report-trx",
										$"--report-trx-filename {project.Name}_{framework}.trx",
										"--coverage",
										"--coverage-output-format cobertura",
										$"--coverage-output {project.Name}_{framework}.cobertura.xml")
							)
					), completeOnFailure: true
			);
		});
}
