#if NET8_0_OR_GREATER
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed partial class Setup
	{
		public sealed class PatchTests
		{
			[Test]
			[Arguments("application/json", true)]
			[Arguments("text/plain", false)]
			[Arguments("application/txt", false)]
			public async Task StringUri_ShouldVerifyHttpContent(string mediaType, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.IsAny<string>(), It.IsHttpContent("application/json"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent("", Encoding.UTF8, mediaType),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(nameof(HttpMethod.Delete), false)]
			[Arguments(nameof(HttpMethod.Get), false)]
			[Arguments(nameof(HttpMethod.Patch), true)]
			[Arguments(nameof(HttpMethod.Post), false)]
			[Arguments(nameof(HttpMethod.Put), false)]
			public async Task StringUri_ShouldVerifyHttpMethod(string method, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.Matches("*testably.org*"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.SendAsync(
					new HttpRequestMessage(new HttpMethod(method), "https://www.testably.org"),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("*testably.org*", true)]
			[Arguments("*testably.org", true)]
			[Arguments("testably.org*", false)]
			[Arguments("*foo*", false)]
			public async Task StringUri_ShouldVerifyUriString(string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.Matches(pattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task StringUri_WithCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(
						It.Matches("*testably.org*"),
						It.IsAny<HttpContent>(),
						It.Satisfies<CancellationToken>(_ => tokenMatches))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(tokenMatches ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task StringUri_WithoutMockedHttpMessageHandler_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Setup
						.PatchAsync(It.Matches("*testably.org*"))
						.ReturnsAsync(HttpStatusCode.OK);
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}

			[Test]
			[Arguments("application/json", true)]
			[Arguments("text/plain", false)]
			[Arguments("application/txt", false)]
			public async Task Uri_ShouldVerifyHttpContent(string mediaType, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.IsAny<Uri>(), It.IsHttpContent("application/json"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent("", Encoding.UTF8, mediaType),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(nameof(HttpMethod.Delete), false)]
			[Arguments(nameof(HttpMethod.Get), false)]
			[Arguments(nameof(HttpMethod.Patch), true)]
			[Arguments(nameof(HttpMethod.Post), false)]
			[Arguments(nameof(HttpMethod.Put), false)]
			public async Task Uri_ShouldVerifyHttpMethod(string method, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.IsUri("*testably.org*"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.SendAsync(
					new HttpRequestMessage(new HttpMethod(method), "https://www.testably.org"),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("*testably.org*", true)]
			[Arguments("*testably.org", true)]
			[Arguments("testably.org*", true)]
			[Arguments("*foo*", false)]
			public async Task Uri_ShouldVerifyUri(string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(It.IsUri(pattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task Uri_WithCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PatchAsync(
						It.IsUri("*testably.org*"),
						It.IsAny<HttpContent>(),
						It.Satisfies<CancellationToken>(_ => tokenMatches))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PatchAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(tokenMatches ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task Uri_WithoutMockedHttpMessageHandler_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Setup
						.PatchAsync(It.IsUri("*testably.org*"))
						.ReturnsAsync(HttpStatusCode.OK);
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}
		}
	}
}
#endif
