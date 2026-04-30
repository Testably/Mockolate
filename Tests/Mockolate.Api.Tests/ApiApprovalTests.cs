using System.Collections.Generic;

namespace Mockolate.Api.Tests;

/// <summary>
///     Whenever a test fails, this means that the public API surface changed.
///     If the change was intentional, execute the <see cref="ApiAcceptance.AcceptApiChanges()" /> test to take over the
///     current public API surface. The changes will become part of the pull request and will be reviewed accordingly.
/// </summary>
public sealed class ApiApprovalTests
{
	[Test]
	[MethodDataSource(nameof(TargetFrameworks))]
	public async Task VerifyPublicApiForMockolate(string framework)
	{
		const string assemblyName = "Mockolate";

		string publicApi = Helper.CreatePublicApi(framework, assemblyName);
		string expectedApi = Helper.GetExpectedApi(framework, assemblyName);

		await That(publicApi).IsEqualTo(expectedApi);
	}

	public static IEnumerable<string> TargetFrameworks()
	{
		foreach (string targetFramework in Helper.GetTargetFrameworks())
		{
			yield return targetFramework;
		}
	}
}
