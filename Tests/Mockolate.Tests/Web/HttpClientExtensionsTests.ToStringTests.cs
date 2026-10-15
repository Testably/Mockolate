using System.Net.Http;
using Mockolate.Exceptions;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed class ToStringTests
	{
		[Test]
		public async Task GetAsync_StringUri_ShouldIncludeMethodAndUriInVerificationMessage()
		{
			HttpClient httpClient = HttpClient.CreateMock();

			void Act()
			{
				httpClient.Mock.Verify.GetAsync(It.Matches("*testably.org*")).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*GET-Request with Uri matching It.Matches(\"*testably.org*\")*").AsWildcard();
		}

		[Test]
		public async Task GetAsync_UriParameter_ShouldIncludeMethodAndUriInVerificationMessage()
		{
			HttpClient httpClient = HttpClient.CreateMock();

			void Act()
			{
				httpClient.Mock.Verify.GetAsync(It.IsUri().ForHttps()).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*GET-Request with https Uri*").AsWildcard();
		}

		[Test]
		public async Task PostAsync_ShouldIncludeMethodInVerificationMessage()
		{
			HttpClient httpClient = HttpClient.CreateMock();

			void Act()
			{
				httpClient.Mock.Verify
					.PostAsync(It.IsAny<string?>(), It.IsHttpContent("application/json"))
					.Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*POST-Request with Uri matching It.IsAny<string>() and Http content with media type \"application/json\"*").AsWildcard();
		}

		[Test]
		public async Task PostAsync_ShouldIncludeStringInformationInVerificationMessage()
		{
			HttpClient httpClient = HttpClient.CreateMock();

			void Act()
			{
				httpClient.Mock.Verify
					.PostAsync(It.IsUri(), It.IsHttpContent().WithString("foo").Exactly())
					.Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*POST-Request with any Uri and string content equal to \"foo\"*").AsWildcard();
		}

		[Test]
		public async Task SendAsync_ShouldIncludeRequestMessageInVerificationMessage()
		{
			HttpClient httpClient = HttpClient.CreateMock();

			void Act()
			{
				httpClient.Mock.Verify
					.SendAsync(It.IsHttpRequestMessage(HttpMethod.Delete).WhoseUriIs("*api*"), null)
					.Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("*DELETE-Request with Uri matching \"*api*\"*")
				.AsWildcard();
		}
	}
}
