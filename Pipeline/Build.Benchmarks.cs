using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.Git;
using Octokit;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable AllUnderscoreLocalParameterName

namespace Build;

partial class Build
{
	private const string BenchmarkBranch = "benchmarks";
	private const string BenchmarkDataPath = "Docs/pages/static/js/data.js";
	private const string BenchmarkLimitedDataPath = "Docs/pages/static/js/limited-data.js";
	private const int BenchmarkLimit = 50;

	[Parameter("Filter for BenchmarkDotNet - Default is '*'")] readonly string BenchmarkFilter = "*";

	Target BenchmarkDotNet => _ => _
		.Executes(() =>
		{
			AbsolutePath benchmarkDirectory = ArtifactsDirectory / "Benchmarks";
			benchmarkDirectory.CreateOrCleanDirectory();

			DotNetBuild(s => s
				.SetProjectFile(Solution.Benchmarks.Mockolate_Benchmarks)
				.SetConfiguration(Configuration.Release)
				.EnableNoLogo());

			DotNet(
				$"{Solution.Benchmarks.Mockolate_Benchmarks.Name}.dll --exporters json --filter {BenchmarkFilter} --artifacts \"{benchmarkDirectory}\"",
				Solution.Benchmarks.Mockolate_Benchmarks.Directory / "bin" / "Release");
		});

	Target BenchmarkResult => _ => _
		.After(BenchmarkDotNet)
		.Executes(async () =>
		{
			if (!Directory.Exists(ArtifactsDirectory / "Benchmarks" / "results"))
			{
				Log.Information("Skip benchmark result, because no results directory was generated.");
				return;
			}

			string[] files = Directory.GetFiles(ArtifactsDirectory / "Benchmarks" / "results", "*-report-github.md");
			if (files.Length == 0)
			{
				Log.Information("Skip benchmark result, because no report file was generated.");
				return;
			}

			foreach (string file in files)
			{
				string fileContent = await File.ReadAllTextAsync(file);
				Log.Information("Report ({FileName}):\n {FileContent}", Path.GetFileName(file), fileContent);
			}
		});

	Target BenchmarkComment => _ => _
		.Executes(async () =>
		{
			// The artifacts are untrusted, because the pull request controls the code that produces them, so the pull
			// request to comment on comes from the event of the workflow run.
			int? prId = await BuildExtensions.ResolvePullRequestOfWorkflowRun(GithubToken);
			if (prId == null)
			{
				Log.Information("Skip writing a comment, as the workflow run belongs to no pull request.");
				return;
			}

			await "Benchmarks-".DownloadArtifactsStartingWith(ArtifactsDirectory, GithubToken);
			if (!Directory.Exists(ArtifactsDirectory / "Benchmarks" / "results"))
			{
				Log.Information("Skip benchmark comment, because no results directory was generated.");
				return;
			}

			string[] files = Directory.GetFiles(ArtifactsDirectory / "Benchmarks" / "results", "*-report-github.md");
			if (files.Length == 0)
			{
				Log.Information("Skip benchmark comment, because no report file was generated.");
				return;
			}

			BuildExtensions.GithubFile baselineFile =
				await BuildExtensions.ReadBranchFileAsync(BenchmarkLimitedDataPath, BenchmarkBranch, GithubToken);
			List<BenchmarkReportFile> reportFiles = new();
			foreach (string file in files.Order(StringComparer.Ordinal))
			{
				string prefix = file.Substring(0, file.Length - "-report-github.md".Length);
				reportFiles.Add(new BenchmarkReportFile(
					prefix.Substring(prefix.LastIndexOf('.') + 1),
					await File.ReadAllLinesAsync(file),
					await File.ReadAllTextAsync(prefix + "-report-full-compressed.json")));
			}

			string body = BenchmarkReport.BuildBody(reportFiles, BenchmarkReport.ReadBaseline(baselineFile?.Content),
				BenchmarkReport.DefaultColumnsToRemove).AsUntrustedCommentContent();
			GitHubClient gitHubClient = new(new ProductHeaderValue("Fallout"));
			Credentials tokenAuth = new(GithubToken);
			gitHubClient.Credentials = tokenAuth;
			IReadOnlyList<IssueComment> comments =
				await gitHubClient.Issue.Comment.GetAllForIssue(BuildExtensions.Owner, BuildExtensions.Repo, prId.Value);
			long? commentId = null;
			Log.Information($"Found {comments.Count} comments");
			foreach (IssueComment comment in comments)
			{
				if (comment.IsPipelineComment(BenchmarkReport.Heading))
				{
					Log.Information($"Found comment: {comment.Body}");
					commentId = comment.Id;
				}
			}

			if (commentId == null)
			{
				Log.Information($"Create comment:\n{body}");
				await gitHubClient.Issue.Comment.Create(BuildExtensions.Owner, BuildExtensions.Repo, prId.Value, body);
			}
			else
			{
				Log.Information($"Update comment:\n{body}");
				await gitHubClient.Issue.Comment.Update(BuildExtensions.Owner, BuildExtensions.Repo, commentId.Value, body);
			}
		});

	Target Benchmarks => _ => _
		.DependsOn(BenchmarkDotNet)
		.DependsOn(BenchmarkResult);

	Target PublishBenchmarkReport => _ => _
		.Description("Aggregates BenchmarkDotNet JSON results from the matrix Benchmarks-* artifacts of the " +
		             "current workflow run, then commits an updated data.js / limited-data.js to the " +
		             $"'{BenchmarkBranch}' branch.")
		.Requires(() => GithubToken)
		.Executes(async () =>
		{
			await "Benchmarks-".DownloadArtifactsStartingWith(ArtifactsDirectory, GithubToken);

			List<string> benchmarkReports = LoadBenchmarkJsonReports(ArtifactsDirectory / "Benchmarks" / "results");
			if (benchmarkReports.Count == 0)
			{
				Log.Warning("Skip benchmark report: no benchmark JSON reports found.");
				return;
			}

			PageBenchmarkReportGenerator.CommitInfo commitInfo = ReadCurrentCommitInfo();
			Log.Information(
				"Appending benchmark data for commit {Sha} ({Author}, {Date}): {Message}",
				commitInfo.Sha, commitInfo.Author, commitInfo.Date, commitInfo.Message);

			BuildExtensions.GithubFile dataFile =
				await BuildExtensions.ReadBranchFileAsync(BenchmarkDataPath, BenchmarkBranch, GithubToken);
			BuildExtensions.GithubFile limitedFile =
				await BuildExtensions.ReadBranchFileAsync(BenchmarkLimitedDataPath, BenchmarkBranch, GithubToken);

			(string updated, string limited) = PageBenchmarkReportGenerator.Append(
				commitInfo,
				dataFile.Content,
				benchmarkReports,
				BenchmarkLimit);

			if (string.IsNullOrWhiteSpace(updated))
			{
				Log.Information("No changes to publish (commit already recorded).");
				return;
			}

			string commitMessage =
				$"Update benchmark for {commitInfo.Sha.Substring(0, 8)}: {commitInfo.Message} by {commitInfo.Author}";
			await BuildExtensions.WriteBranchFileAsync(BenchmarkDataPath, BenchmarkBranch, commitMessage, updated,
				dataFile.Sha, GithubToken);
			await BuildExtensions.WriteBranchFileAsync(BenchmarkLimitedDataPath, BenchmarkBranch, commitMessage,
				limited, limitedFile.Sha, GithubToken);
		});

	private static List<string> LoadBenchmarkJsonReports(AbsolutePath resultsDirectory)
	{
		List<string> reports = new();
		if (!Directory.Exists(resultsDirectory))
		{
			return reports;
		}

		foreach (string file in Directory.GetFiles(resultsDirectory, "*-report-full-compressed.json"))
		{
			reports.Add(File.ReadAllText(file));
		}

		return reports;
	}

	private static PageBenchmarkReportGenerator.CommitInfo ReadCurrentCommitInfo()
	{
		Output[] lines = GitTasks.Git("log -1").ToArray();
		string commitId = null, author = null, date = null, message = null;
		foreach (string line in lines.Select(x => x.Text))
		{
			if (commitId == null && line.StartsWith("commit "))
			{
				commitId = line.Substring("commit ".Length).Substring(0, 40);
				continue;
			}

			if (author == null && line.StartsWith("Author: "))
			{
				author = line.Substring("Author: ".Length);
				int index = author.IndexOf(" <", StringComparison.Ordinal);
				if (index > 0)
				{
					author = author.Substring(0, index);
				}

				continue;
			}

			if (date == null && line.StartsWith("Date:   "))
			{
				date = line.Substring("Date:   ".Length);
				continue;
			}

			if (commitId != null && author != null && date != null && !string.IsNullOrWhiteSpace(line))
			{
				message = line.Trim();
				break;
			}
		}

		return new PageBenchmarkReportGenerator.CommitInfo(commitId, author, date, message);
	}
}
