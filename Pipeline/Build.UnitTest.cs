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

	Project[] UnionTestProjects =>
	[
		Solution.Tests.Mockolate_Tests,
		Solution.Tests.Mockolate_Internal_Tests,
		Solution.Tests.Mockolate_SourceGenerators_Tests,
		Solution.Tests.Mockolate_ExampleTests,
	];

	/// <summary>
	///     Runs the test projects on net11.0 only, where <c>Tests/Directory.Build.props</c> enables the C# preview
	///     language version and the union-typed setup surface (<c>MockolateUnionParameters</c>).
	/// </summary>
	Target UnionTests => _ => _
		.Unlisted()
		.DependsOn(Compile)
		.Executes(() =>
		{
			DotNetTest(s => s
					.SetConfiguration(Configuration)
					.SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
					.EnableNoBuild()
					.SetFramework("net11.0")
					.SetResultsDirectory(TestResultsDirectory)
					.CombineWith(
						UnionTestProjects,
						(settings, project) => settings
							.AddProcessAdditionalArguments(
								$"--project \"{project.Path}\"",
								"--report-trx",
								$"--report-trx-filename {project.Name}_net11.0_unions.trx")
					), completeOnFailure: true
			);
		});
}
