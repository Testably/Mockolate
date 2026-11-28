using System.Net.Http;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpRequestMessageTests
	{
		public sealed class ToStringTests
		{
			[Test]
			public async Task WithoutConditions_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage();

				string? result = sut.ToString();

				await That(result).IsEqualTo("a Http-Request");
			}

			[Test]
			public async Task WithMultipleConditions_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage(HttpMethod.Post)
					.WhoseUriIs("https://example.com")
					.WhoseContentIs("application/json");

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"POST-Request with Uri matching \"https://example.com\" and Http content with media type \"application/json\"");
			}

			[Test]
			public async Task WhoseContentIs_WithAction_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage().WhoseContentIs(c => c.WithMediaType("text/plain"));

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"a Http-Request with Http content with media type \"text/plain\"");
			}

			[Test]
			public async Task WhoseContentIs_WithMediaType_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage().WhoseContentIs("application/json");

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"a Http-Request with Http content with media type \"application/json\"");
			}

			[Test]
			public async Task WhoseUriIs_WithAction_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage().WhoseUriIs(u => u.ForHttps().WithHost("example.com"));

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"a Http-Request with https Uri with host matching \"example.com\"");
			}

			[Test]
			public async Task WhoseUriIs_WithString_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage().WhoseUriIs("https://example.com");

				string? result = sut.ToString();

				await That(result).IsEqualTo("a Http-Request with Uri matching \"https://example.com\"");
			}

			[Test]
			public async Task WithHeaders_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage().WithHeaders(("x-my-header", "my-value"));

				string? result = sut.ToString();

				await That(result).IsEqualTo("a Http-Request with header \"x-my-header: my-value\"");
			}

			[Test]
			public async Task WithMethod_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpRequestMessageParameter sut =
					It.IsHttpRequestMessage(HttpMethod.Get);

				string? result = sut.ToString();

				await That(result).IsEqualTo("GET-Request");
			}
		}
	}
}
