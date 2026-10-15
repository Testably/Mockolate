using System.Linq;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

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
					.SetDataCollector("XPlat Code Coverage")
					.SetResultsDirectory(TestResultsDirectory)
					.CombineWith(
						UnitTestProjects,
						(settings, project) => settings
							.SetProjectFile(project)
							.CombineWith(
								project.GetTargetFrameworks()?.Except(excludedFrameworks),
								(frameworkSettings, framework) => frameworkSettings
									.SetFramework(framework)
									.AddLoggers($"trx;LogFileName={project.Name}_{framework}.trx")
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
							.SetProjectFile(project)
							.AddLoggers($"trx;LogFileName={project.Name}_net11.0_unions.trx")
					), completeOnFailure: true
			);
		});
}
