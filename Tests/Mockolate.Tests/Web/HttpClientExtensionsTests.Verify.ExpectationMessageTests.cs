using System.Net.Http;
using Mockolate.Exceptions;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed partial class Verify
	{
		public sealed class ExpectationMessageTests
		{
			[Test]
			public async Task DeleteAsync_StringUri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.DeleteAsync(It.Matches("*testably*")).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}

			[Test]
			public async Task DeleteAsync_Uri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.DeleteAsync(It.IsUri("*testably*")).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}

			[Test]
			public async Task PutAsync_StringUri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.PutAsync(It.Matches("*testably*"), It.IsAny<HttpContent>()).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}

			[Test]
			public async Task PutAsync_Uri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.PutAsync(It.IsUri("*testably*"), It.IsAny<HttpContent>()).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}

#if NET8_0_OR_GREATER
			[Test]
			public async Task PatchAsync_StringUri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.PatchAsync(It.Matches("*testably*"), It.IsAny<HttpContent>()).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}

			[Test]
			public async Task PatchAsync_Uri_FailingVerification_ShouldMentionSendAsyncInMessage()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.PatchAsync(It.IsUri("*testably*"), It.IsAny<HttpContent>()).AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*SendAsync*").AsWildcard();
			}
#endif
		}
	}
}
