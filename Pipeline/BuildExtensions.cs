using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Tools.SonarScanner;
using Octokit;
using Serilog;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Build;

public static class BuildExtensions
{
	internal const string Owner = "Testably";
	internal const string Repo = "Mockolate";
	private const string RepositoryApiBaseUrl = "https://api.github.com/repos/" + Owner + "/" + Repo;

	public static SonarScannerBeginSettings SetPullRequestOrBranchName(
		this SonarScannerBeginSettings settings,
		GitHubActions gitHubActions,
		GitVersion gitVersion)
	{
		if (gitHubActions?.IsPullRequest == true)
		{
			Log.Information("Use pull request analysis");
			return settings
				.SetPullRequestKey(gitHubActions.PullRequestNumber.ToString())
				.SetPullRequestBranch(gitHubActions.Ref)
				.SetPullRequestBase(gitHubActions.BaseRef);
		}

		if (gitHubActions?.Ref.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase) == true)
		{
			string version = gitHubActions.Ref.Substring("refs/tags/".Length);
			string branchName = "release/" + version;
			Log.Information("Use release branch analysis for '{BranchName}'", branchName);
			return settings.SetBranchName(branchName);
		}

		Log.Information("Use branch analysis for '{BranchName}'", gitVersion.BranchName);
		return settings.SetBranchName(gitVersion.BranchName);
	}

	/// <summary>
	///     Resolves the pull request that triggered the workflow run under analysis, or <see langword="null" />
	///     when there is none.
	/// </summary>
	/// <remarks>
	///     The artifacts of that run are untrusted, because the pull request controls the code that produces them, so
	///     the pull request is identified by the <c>workflow_run</c> event alone. The event lists no pull requests for
	///     a fork, which is why the number is looked up by the head branch and verified against the head commit.
	///     <para />
	///     A pull request that was merged or closed while its run was analysed is still found. When the same commit
	///     of the same branch belongs to several pull requests, the open one is taken.
	/// </remarks>
	public static async Task<int?> ResolvePullRequestOfWorkflowRun(string githubToken)
	{
		string workflowRunEvent = Environment.GetEnvironmentVariable("WorkflowRunEvent");
		string headOwner = Environment.GetEnvironmentVariable("WorkflowRunHeadOwner");
		string headBranch = Environment.GetEnvironmentVariable("WorkflowRunHeadBranch");
		string headSha = Environment.GetEnvironmentVariable("WorkflowRunHeadSha");
		if (workflowRunEvent != "pull_request" ||
		    string.IsNullOrEmpty(headOwner) || string.IsNullOrEmpty(headBranch) || string.IsNullOrEmpty(headSha))
		{
			Log.Information("The workflow run was not triggered by a pull request");
			return null;
		}

		GitHubClient gitHubClient = new(new ProductHeaderValue("Fallout"))
		{
			Credentials = new Credentials(githubToken),
		};
		IReadOnlyList<PullRequest> pullRequests = await gitHubClient.PullRequest.GetAllForRepository(
			Owner, Repo,
			new PullRequestRequest
			{
				State = ItemStateFilter.All,
				Head = $"{headOwner}:{headBranch}",
			});
		// GitHub ignores a head filter it cannot interpret and then lists every pull request.
		PullRequest[] matches = pullRequests
			.Where(pullRequest => pullRequest.Head.Sha == headSha &&
			                      pullRequest.Head.Ref == headBranch &&
			                      string.Equals(pullRequest.Head.User?.Login, headOwner,
				                      StringComparison.OrdinalIgnoreCase))
			.ToArray();
		if (matches.Length > 1)
		{
			matches = matches.Where(pullRequest => pullRequest.State.Value == ItemState.Open).ToArray();
		}

		int[] numbers = matches.Select(pullRequest => pullRequest.Number).ToArray();
		if (numbers.Length != 1)
		{
			Log.Information("Found {Count} pull requests for the commit {Sha} of the workflow run",
				numbers.Length, headSha);
			return null;
		}

		Log.Information("The workflow run was triggered by the pull request #{PullRequest}", numbers[0]);
		return numbers[0];
	}

	/// <summary>
	///     Whether the <paramref name="comment" /> is the one that the pipeline wrote under the
	///     <paramref name="heading" />.
	/// </summary>
	/// <remarks>
	///     Anyone can write a comment that contains the heading, so the author has to match too.
	/// </remarks>
	public static bool IsPipelineComment(this IssueComment comment, string heading)
		=> comment.User?.Login == "github-actions[bot]" &&
		   comment.Body?.StartsWith(heading, StringComparison.Ordinal) == true;

	/// <summary>
	///     Prepares the <paramref name="markdown" /> of an artifact for a comment of the pipeline.
	/// </summary>
	/// <remarks>
	///     The artifact is untrusted, so the markdown is bound to what a comment can hold, and a zero-width space
	///     after every <c>@</c> keeps it from notifying users and teams.
	/// </remarks>
	public static string AsUntrustedCommentContent(this string markdown)
	{
		const int maxLength = 60000;
		markdown = markdown.Replace("@", "@​");
		if (markdown.Length > maxLength)
		{
			markdown = markdown.Substring(0, maxLength) + Environment.NewLine + Environment.NewLine +
			           "_The report was truncated._";
		}

		return markdown;
	}

	public static Task DownloadArtifactTo(this string artifactName, string artifactsDirectory, string githubToken)
		=> DownloadArtifactsWhere(name => name.Equals(artifactName, StringComparison.OrdinalIgnoreCase),
			artifactsDirectory, githubToken);

	public static Task DownloadArtifactsStartingWith(this string artifactNamePrefix, string artifactsDirectory,
		string githubToken)
		=> DownloadArtifactsWhere(name => name.StartsWith(artifactNamePrefix, StringComparison.OrdinalIgnoreCase),
			artifactsDirectory, githubToken);

	public static async Task<long[]> FindRecentSuccessfulRunIds(string workflowFileName, string branch, int count,
		string githubToken)
	{
		if (string.IsNullOrEmpty(githubToken))
		{
			throw new ArgumentException("A GitHub token is required.", nameof(githubToken));
		}

		using HttpClient client = new();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("Mockolate");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

		string url = $"{RepositoryApiBaseUrl}/actions/workflows/{Uri.EscapeDataString(workflowFileName)}/runs" +
		             $"?status=success&branch={Uri.EscapeDataString(branch)}&per_page={count}";
		HttpResponseMessage response = await client.GetAsync(url);
		string responseContent = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			Log.Warning(
				$"Could not find recent runs for workflow '{workflowFileName}' on branch '{branch}': {responseContent}");
			return [];
		}

		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(responseContent);
			JsonElement runs = jsonDocument.RootElement.GetProperty("workflow_runs");
			long[] ids = new long[runs.GetArrayLength()];
			for (int i = 0; i < ids.Length; i++)
			{
				ids[i] = runs[i].GetProperty("id").GetInt64();
			}

			return ids;
		}
		catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
		{
			Log.Error($"Could not parse workflow runs response: {e.Message}\n{responseContent}");
			return [];
		}
	}

	public static Task DownloadArtifactsFromRunStartingWith(long runId, string artifactNamePrefix,
		string artifactsDirectory, string githubToken)
		=> DownloadArtifactsFromRun(runId.ToString(),
			name => name.StartsWith(artifactNamePrefix, StringComparison.OrdinalIgnoreCase),
			artifactsDirectory, githubToken);

	private static Task DownloadArtifactsWhere(Func<string, bool> namePredicate, string artifactsDirectory,
		string githubToken)
	{
		string runId = Environment.GetEnvironmentVariable("WorkflowRunId");
		if (string.IsNullOrEmpty(runId))
		{
			Log.Information("Skip downloading artifacts, because no 'WorkflowRunId' environment variable is set.");
			return Task.CompletedTask;
		}

		return DownloadArtifactsFromRun(runId, namePredicate, artifactsDirectory, githubToken);
	}

	private static async Task DownloadArtifactsFromRun(string runId, Func<string, bool> namePredicate,
		string artifactsDirectory, string githubToken)
	{
		using HttpClient client = new();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("Mockolate");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
		HttpResponseMessage response = await client.GetAsync(
			$"{RepositoryApiBaseUrl}/actions/runs/{runId}/artifacts");

		string responseContent = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"Could not find artifacts for run #{runId}': {responseContent}");
		}

		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(responseContent);
			foreach (JsonElement artifact in jsonDocument.RootElement.GetProperty("artifacts").EnumerateArray())
			{
				string name = artifact.GetProperty("name").GetString()!;
				if (namePredicate(name))
				{
					long artifactId = artifact.GetProperty("id").GetInt64();
					HttpResponseMessage fileResponse = await client.GetAsync(
						$"{RepositoryApiBaseUrl}/actions/artifacts/{artifactId}/zip");
					if (fileResponse.IsSuccessStatusCode)
					{
						using ZipArchive archive = new(await fileResponse.Content.ReadAsStreamAsync());
						// The artifact can be untrusted, and a link in it could point a later read at any file of
						// the runner. The upper half of the attributes is the Unix mode, where 0xA000 marks a link.
						if (archive.Entries.Any(entry => ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000))
						{
							throw new InvalidOperationException(
								$"The artifact #{artifactId} contains a symbolic link");
						}

						archive.ExtractToDirectory(artifactsDirectory, true);
						Log.Information(
							$"Extracted artifact '{name}' (#{artifactId}) with {archive.Entries.Count} entries to {artifactsDirectory}:\n - {string.Join("\n - ", archive.Entries.Select(entry => $"{entry.Name} ({entry.Length})"))}");
					}
					else
					{
						string fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
						throw new InvalidOperationException(
							$"Could not download the artifacts with id #{artifactId}': {fileResponseContent}");
					}
				}
			}
		}
		catch (JsonException e)
		{
			Log.Error($"Could not parse JSON: {e.Message}\n{responseContent}");
		}
	}

	/// <summary>
	///     Reads a file from the given <paramref name="branch" /> using the GitHub contents API.
	///     Returns <see langword="null" /> if the file does not exist on that branch.
	/// </summary>
	public static async Task<GithubFile> ReadBranchFileAsync(string path, string branch, string githubToken)
	{
		using HttpClient client = CreateGithubClient(githubToken);
		HttpResponseMessage response = await client.GetAsync(
			$"{RepositoryApiBaseUrl}/contents/{path}?ref={Uri.EscapeDataString(branch)}");
		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		string responseContent = await response.Content.ReadAsStringAsync();
		using JsonDocument document = JsonDocument.Parse(responseContent);
		string sha = document.RootElement.GetProperty("sha").GetString();
		string downloadUrl =
			$"https://raw.githubusercontent.com/{Owner}/{Repo}/refs/heads/{branch}/{path}";
		using HttpClient rawClient = new();
		rawClient.DefaultRequestHeaders.UserAgent.ParseAdd(Repo);
		string content = await rawClient.GetStringAsync(downloadUrl);
		return new GithubFile(content, sha);
	}

	/// <summary>
	///     Writes a file to the given <paramref name="branch" /> using the GitHub contents API.
	/// </summary>
	public static async Task WriteBranchFileAsync(string path, string branch, string commitMessage, string content,
		string existingSha, string githubToken)
	{
		using HttpClient client = CreateGithubClient(githubToken);
		GithubUpdateFile body = new(commitMessage, Base64Encode(content), existingSha, branch);
		HttpResponseMessage response = await client.PutAsync(
			$"{RepositoryApiBaseUrl}/contents/{path}",
			new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
		if (response.IsSuccessStatusCode)
		{
			Log.Information("Updated {Path} on branch '{Branch}'.", path, branch);
		}
		else
		{
			string responseContent = await response.Content.ReadAsStringAsync();
			throw new InvalidOperationException(
				$"Could not update '{path}' on branch '{branch}': {responseContent}");
		}
	}

	private static HttpClient CreateGithubClient(string githubToken)
	{
		HttpClient client = new();
		client.DefaultRequestHeaders.UserAgent.ParseAdd(Repo);
		if (!string.IsNullOrEmpty(githubToken))
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
		}

		return client;
	}

	private static string Base64Encode(string plainText)
	{
		byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
		return Convert.ToBase64String(plainTextBytes);
	}

	public record GithubFile(string Content, string Sha);

	// ReSharper disable InconsistentNaming
	// ReSharper disable NotAccessedPositionalProperty.Local
	private record GithubUpdateFile(string message, string content, string sha, string branch);
	// ReSharper restore NotAccessedPositionalProperty.Local
	// ReSharper restore InconsistentNaming
}
