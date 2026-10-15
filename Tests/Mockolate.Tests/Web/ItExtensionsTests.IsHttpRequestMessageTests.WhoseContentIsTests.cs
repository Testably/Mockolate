using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using aweXpect.Chronology;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpRequestMessageTests
	{
		public sealed class WhoseContentIsTests
		{
			[Test]
			public async Task ShouldNotCheckHttpContentType()
			{
				string expectedValue = "foo";
				byte[] bytes = Encoding.UTF8.GetBytes(expectedValue);
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithString("foo")))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(bytes),
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("image/png", true)]
			[Arguments("text/plain", false)]
			[Arguments("image/gif", false)]
			public async Task ShouldVerifyMediaType(string mediaType, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs("image/png"))
					.ReturnsAsync(HttpStatusCode.OK);
				ByteArrayContent content = new([]);
				content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("text/plain", "foo", true)]
			[Arguments("image/png", "foo", false)]
			[Arguments("text/plain", "bar", false)]
			public async Task ShouldVerifyMediaTypeAndContent(string mediaType, string value, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(mediaType, c => c.WithString(value)))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("foo", Encoding.UTF8, "text/plain");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(new byte[0], 0x1, false)]
			[Arguments(new byte[]
			{
				0x1,
			}, 0x1, true)]
			[Arguments(new byte[]
			{
				0x1,
			}, 0x2, false)]
			[Arguments(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x1, true)]
			[Arguments(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x2, false)]
			[Arguments(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x3, false)]
			public async Task WithBytes_Predicate_ShouldValidatePredicate(
				byte[] body, byte expectedFirstByte, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithBytes(b => b.Length > 0 && b[0] == expectedFirstByte)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(new byte[0], new byte[0], true)]
			[Arguments(new byte[]
			{
				0x66,
			}, new byte[]
			{
				0x66,
			}, true)]
			[Arguments(new byte[]
			{
				0x66,
			}, new byte[]
			{
				0x67,
			}, false)]
			[Arguments(new byte[]
			{
				0x66, 0x67,
			}, new byte[]
			{
				0x67,
			}, false)]
			[Arguments(new byte[]
			{
				0x66, 0x67,
			}, new byte[]
			{
				0x67, 0x68, 0x69,
			}, false)]
			public async Task WithBytes_ShouldCheckForEquality(byte[] body, byte[] expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithBytes(expected)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("image/png", true)]
			[Arguments("text/plain", false)]
			[Arguments("image/gif", false)]
			public async Task WithMediaType_ShouldVerifyMediaType(string mediaType, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithMediaType("image/png")))
					.ReturnsAsync(HttpStatusCode.OK);
				ByteArrayContent content = new([]);
				content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", "foo", true)]
			[Arguments("foo", "FOO", true)]
			[Arguments("foo", "bar", false)]
			public async Task WithString_IgnoringCase_ShouldCheckForCaseInsensitiveEquality(string body,
				string expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithString(expected).IgnoringCase()))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("", true)]
			[Arguments("foo", true)]
			[Arguments("FOO", false)]
			[Arguments("bar", true)]
			[Arguments("BAR", false)]
			public async Task WithString_Predicate_ShouldValidatePredicate(string content, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c
						.WithString(v => v.Equals(v.ToLowerInvariant(), StringComparison.Ordinal))))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(content),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", "foo", true)]
			[Arguments("foo", "FOO", false)]
			[Arguments("foo", "bar", false)]
			public async Task WithString_ShouldCheckForEquality(string body, string expected,
				bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c.WithString(expected)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", "f[aeiou]*", true)]
			[Arguments("foo", "F[aeiou]*", true)]
			[Arguments("foo", ".a.", false)]
			public async Task
				WithStringMatching_AsRegex_IgnoringCase_ShouldCheckForCaseInsensitiveMatchingWildcard(
					string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WhoseContentIs(c => c.WithStringMatching(pattern).AsRegex().IgnoringCase()))
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
			public async Task WithStringMatching_AsRegex_ShouldCheckForMatchingWildcard(
				string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WhoseContentIs(c => c.WithStringMatching(pattern).AsRegex()))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task WithStringMatching_AsRegex_ShouldUseProvidedOptions()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c
						.WithStringMatching("F[A-Z]*").AsRegex(RegexOptions.IgnoreCase)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent("foo"),
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			public async Task WithStringMatching_AsRegex_ShouldUseTimeout()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c
						.WithStringMatching("F[A-Z]*").AsRegex(timeout: 0.Seconds())))
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
			public async Task WithStringMatching_IgnoringCase_ShouldCheckForCaseInsensitiveMatchingWildcard(
				string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c
						.WithStringMatching(pattern).IgnoringCase()))
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
			[Arguments("foo", "F*", false)]
			[Arguments("foo", "*a*", false)]
			public async Task WithStringMatching_ShouldCheckForMatchingWildcard(
				string body, string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WhoseContentIs(c => c
						.WithStringMatching(pattern)))
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
