using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed partial class VerifyTests
	{
		public sealed class GetTests
		{
			[Test]
			[Arguments(nameof(HttpMethod.Delete), 0)]
			[Arguments(nameof(HttpMethod.Get), 1)]
			[Arguments(nameof(HttpMethod.Post), 0)]
			[Arguments(nameof(HttpMethod.Put), 0)]
			public async Task WithStringUri_ShouldVerifyHttpMethod(string method, int expected)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.SendAsync(
					new HttpRequestMessage(new HttpMethod(method), "https://www.testably.org"),
					CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.IsAny<string?>()))
					.Exactly(expected);
			}

			[Test]
			public async Task Then_ShouldVerifyTheOrderOfRequests()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.GetAsync("https://www.testably.org/first", CancellationToken.None);
				await httpClient.PostAsync("https://www.testably.org/second", new StringContent("content"),
					CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(It.Matches("*first")))
					.Then(m => m.PostAsync(It.Matches("*second")));
			}

			[Test]
			public async Task Then_WhenInWrongOrder_ShouldFail()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.PostAsync("https://www.testably.org/first", new StringContent("content"),
					CancellationToken.None);
				await httpClient.GetAsync("https://www.testably.org/second", CancellationToken.None);

				void Act() => httpClient.Mock.Verify.GetAsync(It.Matches("*second"))
					.Then(m => m.PostAsync(It.Matches("*first")));

				await That(Act).Throws<MockVerificationException>();
			}

			[Test]
			[Arguments("*testably.org*", 1)]
			[Arguments("*testably.org", 1)]
			[Arguments("testably.org*", 0)]
			[Arguments("*foo*", 0)]
			public async Task WithStringUri_ShouldVerifyUri(string pattern, int expected)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.Matches(pattern),
						It.IsAny<CancellationToken>()))
					.Exactly(expected);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task WithStringUriAndCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.Matches("*"),
						It.Satisfies<CancellationToken>(_ => tokenMatches)))
					.Exactly(tokenMatches ? 1 : 0);
			}

			[Test]
			public async Task WithStringUri_WhenHttpMessageHandlerIsNotMocked_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Verify
						.GetAsync(It.Matches("*testably.org*")).Never();
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot verify HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}

			[Test]
			[Arguments(nameof(HttpMethod.Delete), 0)]
			[Arguments(nameof(HttpMethod.Get), 1)]
			[Arguments(nameof(HttpMethod.Post), 0)]
			[Arguments(nameof(HttpMethod.Put), 0)]
			public async Task WithUri_ShouldVerifyHttpMethod(string method, int expected)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.SendAsync(
					new HttpRequestMessage(new HttpMethod(method), "https://www.testably.org"),
					CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.IsUri("*testably.org*")))
					.Exactly(expected);
			}

			[Test]
			[Arguments("*testably.org*", 1)]
			[Arguments("*testably.org", 1)]
			[Arguments("testably.org*", 1)]
			[Arguments("*foo*", 0)]
			public async Task WithUri_ShouldVerifyUri(string pattern, int expected)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.IsUri(pattern)))
					.Exactly(expected);
			}

			[Test]
			[Arguments(true)]
			[Arguments(false)]
			public async Task WithUriAndCancellationToken_ShouldVerifyCancellationToken(bool tokenMatches)
			{
				HttpClient httpClient = HttpClient.CreateMock();

				await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

				await That(httpClient.Mock.Verify.GetAsync(
						It.IsUri("*testably.org*"),
						It.Satisfies<CancellationToken>(_ => tokenMatches)))
					.Exactly(tokenMatches ? 1 : 0);
			}

			[Test]
			public async Task WithUri_WhenHttpMessageHandlerIsNotMocked_ShouldThrowMockException()
			{
				HttpClient httpClient = HttpClient.CreateMock([]);

				void Act()
				{
					httpClient.Mock.Verify
						.GetAsync(It.IsUri("*testably.org*")).Never();
				}

				await That(Act).Throws<MockException>()
					.WithMessage(
						"Cannot verify HttpClient when it is not mocked with a mockable HttpMessageHandler.");
			}
		}
	}
}
