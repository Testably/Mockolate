using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsUriTests
	{
		public sealed class WithHostTests
		{
			[Test]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "www.testably.org", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "*testably*", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "*Testably*", true)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "mockolate.com", false)]
			public async Task ShouldVerifyHost(string uri, string hostPattern, bool expectMatch)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithHost(hostPattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.GetAsync(uri, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectMatch ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}
		}
	}
}
