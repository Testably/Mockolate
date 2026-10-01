using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class WithBytesTests
		{
			[Fact]
			public async Task CalledTwice_ShouldUseFirstPredicate()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				byte[] payload =
				{
					1, 2, 3,
				};

				await httpClient.PostAsync("https://testably.org", new ByteArrayContent(payload), CancellationToken.None);

				await That(httpClient.Mock.Verify.PostAsync(
						It.IsAny<string?>(),
						It.IsHttpContent()
							.WithBytes(b => b.Length == 3)
							.WithBytes(b => b.Length == 99)))
					.Once()
					.Because(
						"the first WithBytes predicate matches (length == 3) while the second does not, and the first one must win");
			}

			[Theory]
			[InlineData(new byte[0], 0x1, false)]
			[InlineData(new byte[]
			{
				0x1,
			}, 0x1, true)]
			[InlineData(new byte[]
			{
				0x1,
			}, 0x2, false)]
			[InlineData(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x1, true)]
			[InlineData(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x2, false)]
			[InlineData(new byte[]
			{
				0x1, 0x2, 0x3,
			}, 0x3, false)]
			public async Task Predicate_ShouldValidatePredicate(byte[] body, byte expectedFirstByte, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(),
						It.IsHttpContent().WithBytes(b => b.Length > 0 && b[0] == expectedFirstByte))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Theory]
			[InlineData(new byte[0], new byte[0], true)]
			[InlineData(new byte[]
			{
				0x66,
			}, new byte[]
			{
				0x66,
			}, true)]
			[InlineData(new byte[]
			{
				0x66,
			}, new byte[]
			{
				0x67,
			}, false)]
			[InlineData(new byte[]
			{
				0x66, 0x67,
			}, new byte[]
			{
				0x67,
			}, false)]
			[InlineData(new byte[]
			{
				0x66, 0x67,
			}, new byte[]
			{
				0x67, 0x68, 0x69,
			}, false)]
			public async Task ShouldCheckForEquality(byte[] body, byte[] expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithBytes(expected))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Fact]
			public async Task WhenValidatedAndSetup_ShouldResetStreamPosition()
			{
				byte[] body = [0x66, 0x67,];
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithBytes(body))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient
					.PostAsync("https://www.testably.org", new ByteArrayContent(body), CancellationToken.None);

				await That(httpClient.Mock.Verify
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithBytes(body)))
					.Once();
				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Fact]
			public async Task WhenVerifiedConcurrently_ShouldMatchEveryTime()
			{
				byte[] body = [0x66, 0x67,];
				HttpClient httpClient = HttpClient.CreateMock();
				await httpClient.PostAsync("https://www.testably.org", new ByteArrayContent(body), CancellationToken.None);

				void Act()
				{
					Parallel.For(0, 10_000, _ => httpClient.Mock.Verify
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithBytes(body))
						.Once());
				}

				await That(Act).DoesNotThrow();
			}
		}
	}
}
