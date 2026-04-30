using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using aweXpect.Chronology;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class WithStringMatchingTests
		{
			[Test]
			[Arguments("foo", "f[aeiou]*", true)]
			[Arguments("foo", "F[aeiou]*", true)]
			[Arguments("foo", ".a.", false)]
			public async Task AsRegex_IgnoringCase_ShouldCheckForCaseInsensitiveMatchingWildcard(
				string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithStringMatching(pattern).AsRegex().IgnoringCase())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", "f[aeiou]*", true)]
			[Arguments("foo", "F[aeiou]*", false)]
			[Arguments("foo", ".a.", false)]
			public async Task AsRegex_ShouldCheckForMatchingWildcard(string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithStringMatching(pattern).AsRegex())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task AsRegex_ShouldUseProvidedOptions()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(),
						It.IsHttpContent().WithStringMatching("F[A-Z]*").AsRegex(RegexOptions.IgnoreCase))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent("foo"),
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			public async Task AsRegex_ShouldUseTimeout()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(),
						It.IsHttpContent().WithStringMatching("F[A-Z]*").AsRegex(timeout: 0.Seconds()))
					.ReturnsAsync(HttpStatusCode.OK);

				Task Act()
				{
					return httpClient.PostAsync("https://www.testably.org",
						new StringContent("foo"),
						CancellationToken.None);
				}

				await That(Act)
					.Throws<ArgumentOutOfRangeException>()
					.WithParamName("matchTimeout");
			}

			[Test]
			[Arguments("foo", "f?", true)]
			[Arguments("foo", "f??", true)]
			[Arguments("foo", "f*", true)]
			[Arguments("foo", "*", true)]
			[Arguments("foo", "F*", true)]
			[Arguments("foo", "*a*", false)]
			public async Task IgnoringCase_ShouldCheckForCaseInsensitiveMatchingWildcard(
				string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithStringMatching(pattern).IgnoringCase())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", "f?", true)]
			[Arguments("foo", "f??", true)]
			[Arguments("foo", "f*", true)]
			[Arguments("foo", "*", true)]
			[Arguments("foo", "o?", true)]
			[Arguments("foo", "?o", true)]
			[Arguments("foo", "o", true)]
			[Arguments("foo", "F*", false)]
			[Arguments("foo", "*a*", false)]
			public async Task ShouldCheckForMatchingWildcard(string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithStringMatching(pattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(true, "f?o", "foo")]
			[Arguments(true, "f?o", "bar")]
			[Arguments(true, "bar", "f?o")]
			[Arguments(true, "bar", "bar")]
			[Arguments(true, "f?o", "?oo", "b?r", "?ar")]
			[Arguments(false, "f?o", "b?r", "baz")]
			[Arguments(false, "f?o", "baz", "b?r")]
			[Arguments(false, "?az", "f?o", "b?r")]
			[Arguments(false, "?az")]
			public async Task WithMultipleExpectations_ShouldVerifyAll(bool expectSuccess,
				params string[] expectedValues)
			{
				ItExtensions.IHttpContentParameter isHttpContent = It.IsHttpContent();
				foreach (string expectedValue in expectedValues)
				{
					isHttpContent = isHttpContent.WithStringMatching(expectedValue);
				}

				string body = "foo;bar";
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), isHttpContent)
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}
		}
	}
}
