using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed partial class SetupTests
	{
		public sealed class GetTests
		{
			[Test]
			[Arguments(nameof(HttpMethod.Delete), false)]
			[Arguments(nameof(HttpMethod.Get), true)]
			[Arguments(nameof(HttpMethod.Post), false)]
			[Arguments(nameof(HttpMethod.Put), false)]
			public async Task WithStringUri_ShouldVerifyHttpMethod(string method, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.Matches("*testably.org*"))
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
			public async Task WithStringUri_ShouldVerifyUri(string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.Matches(pattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result =
					await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task WithStringUriAndCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(
						It.Matches("*testably.org*"),
						It.Satisfies<CancellationToken>(_ => tokenMatches))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result =
					await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(tokenMatches ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task WithStringUri_WhenHttpMessageHandlerIsNotMocked_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Setup
						.GetAsync(It.Matches("*testably.org*"))
						.ReturnsAsync(HttpStatusCode.OK);
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}

			[Test]
			[Arguments(nameof(HttpMethod.Delete), false)]
			[Arguments(nameof(HttpMethod.Get), true)]
			[Arguments(nameof(HttpMethod.Post), false)]
			[Arguments(nameof(HttpMethod.Put), false)]
			public async Task WithUri_ShouldVerifyHttpMethod(string method, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri("*testably.org*"))
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
			public async Task WithUri_ShouldVerifyUri(string pattern, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri(pattern))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result =
					await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task WithUriAndCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(
						It.IsUri("*testably.org*"),
						It.Satisfies<CancellationToken>(_ => tokenMatches))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result =
					await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(tokenMatches ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task WithUri_WhenHttpMessageHandlerIsNotMocked_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Setup
						.GetAsync(It.IsUri("*testably.org*"))
						.ReturnsAsync(HttpStatusCode.OK);
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}
		}
	}
}
