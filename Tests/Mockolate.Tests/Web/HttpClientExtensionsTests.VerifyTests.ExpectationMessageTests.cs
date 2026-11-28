using System.Net.Http;
using Mockolate.Exceptions;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed partial class VerifyTests
	{
		public sealed class ExpectationMessageTests
		{
			[Test]
			public async Task DeleteAsync_WithStringUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
			public async Task DeleteAsync_WithUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
			public async Task PutAsync_WithStringUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
			public async Task PutAsync_WithUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
			public async Task PatchAsync_WithStringUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
			public async Task PatchAsync_WithUri_WhenVerificationFails_ShouldMentionSendAsyncInMessage()
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
