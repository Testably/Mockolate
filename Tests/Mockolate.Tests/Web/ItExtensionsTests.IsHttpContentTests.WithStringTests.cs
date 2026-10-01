using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class WithStringTests
		{
			[Theory]
			[InlineData("foo", "FOO", true)]
			public async Task Exactly_IgnoringCase_ShouldCheckCaseInsensitiveForFullContentBody(
				string body, string expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(expected).Exactly().IgnoringCase())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Theory]
			[InlineData("foo", "foo", true)]
			[InlineData("foo", "FOO", false)]
			[InlineData("foo", "baz", false)]
			[InlineData(" bar", "bar", false)]
			[InlineData("baz ", "baz", false)]
			public async Task Exactly_ShouldCheckForFullContentBody(string body, string expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(expected).Exactly())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Theory]
			[InlineData("foo", "foo", true)]
			[InlineData("foo", "FOO", true)]
			[InlineData(" foo", "FOO", true)]
			[InlineData("FOO ", "foo", true)]
			[InlineData("foo", "bar", false)]
			public async Task IgnoringCase_ShouldCheckForCaseInsensitiveEquality(string body,
				string expected, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(expected).IgnoringCase())
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Theory]
			[InlineData("", true)]
			[InlineData("foo", true)]
			[InlineData("FOO", false)]
			[InlineData("bar", true)]
			[InlineData("BAR", false)]
			public async Task Predicate_ShouldValidatePredicate(string content, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent()
						.WithString(c => c.Equals(c.ToLowerInvariant(), StringComparison.Ordinal)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(content),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Theory]
			[InlineData("foo", "foo", true)]
			[InlineData("foo", "FOO", false)]
			[InlineData("foo", "bar", false)]
			[InlineData("foo", "f*o", false)]
			public async Task ShouldCheckForEquality(string body, string expected,
				bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(expected))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(body),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Fact]
			public async Task ShouldNotCheckHttpContentType()
			{
				string expectedValue = "foo";
				byte[] bytes = Encoding.UTF8.GetBytes(expectedValue);
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString("foo"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new ByteArrayContent(bytes),
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Fact]
			public async Task ShouldSupportMonitoring()
			{
				int callbackCount = 0;
				List<StringContent> responses =
				[
					new("foo"),
					new("foobar"),
					new("something-different"),
					new("foo-baz"),
				];
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup.PostAsync(It.IsAny<Uri>(),
					It.IsHttpContent().WithStringMatching("foo*")
						.Do(_ => callbackCount++)
						.Monitor(out IParameterMonitor<HttpContent?> monitor));

				foreach (StringContent response in responses)
				{
					await httpClient.PostAsync("https://www.testably.org", response, CancellationToken.None);
				}

#if !NETFRAMEWORK
				await That(
						await Task.WhenAll(monitor.Values.Select(c => c!.ReadAsStringAsync())))
					.IsEqualTo(["foo", "foobar", "foo-baz",]);
#endif
				await That(callbackCount).IsEqualTo(3);
			}

			[Theory]
			[InlineData("image/png", false)]
			[InlineData("text/plain", true)]
			[InlineData("text/csv", false)]
			public async Task ShouldVerifyMediaType(string mediaType, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithStringMatching("*").WithMediaType(mediaType))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("foo", Encoding.UTF8, "text/plain");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Fact]
			public async Task WhenCharsetHeaderIsNotSet_ShouldFallbackToUtf8()
			{
				string original = "äöüß";
				Encoding encoding = Encoding.UTF8;
				byte[] bytes = encoding.GetBytes(original);

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(original))
					.ReturnsAsync(HttpStatusCode.OK);
				ByteArrayContent content = new(bytes);

				HttpResponseMessage result = await httpClient.PostAsync(
					"https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(HttpStatusCode.OK);
			}

			[Theory]
			[InlineData("UTF-8", false)]
			[InlineData("iso-8859-1", true)]
			[InlineData("ISO-8859-1", true)]
			[InlineData(" iso-8859-1\t", true)]
			public async Task WhenCharsetHeaderIsSet_ShouldApplyEncodingCorrectly(
				string charsetHeader, bool expectSuccess)
			{
				string original = "äöüß";
				Encoding encoding = Encoding.GetEncoding("iso-8859-1");
				byte[] bytes = encoding.GetBytes(original);

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(original))
					.ReturnsAsync(HttpStatusCode.OK);
				ByteArrayContent content = new(bytes);
				content.Headers.Add("Content-Type", $"text/plain; charset={charsetHeader}");

				HttpResponseMessage result = await httpClient.PostAsync(
					"https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Fact]
			public async Task WhenValidatedAndSetup_ShouldResetStreamPosition()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString("foo"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient
					.PostAsync("https://www.testably.org", new StringContent("foo"), CancellationToken.None);

				await That(httpClient.Mock.Verify
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString("foo")))
					.Once();
				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Fact]
			public async Task WhenVerifiedConcurrently_ShouldMatchEveryTime()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				await httpClient.PostAsync("https://www.testably.org", new StringContent("foo"), CancellationToken.None);

				void Act()
				{
					Parallel.For(0, 10_000, _ => httpClient.Mock.Verify
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString("foo"))
						.Once());
				}

				await That(Act).DoesNotThrow();
			}

			[Theory]
			[InlineData("foo")]
			public async Task WithInvalidCharsetHeader_ShouldFallbackToUtf8(string charsetHeader)
			{
				string original = "äöüß";
				Encoding encoding = Encoding.UTF8;
				byte[] bytes = encoding.GetBytes(original);

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithString(original))
					.ReturnsAsync(HttpStatusCode.OK);
				ByteArrayContent content = new(bytes);
				content.Headers.ContentType = new MediaTypeHeaderValue("text/plain")
				{
					CharSet = charsetHeader,
				};

				HttpResponseMessage result = await httpClient.PostAsync(
					"https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(HttpStatusCode.OK);
			}

			[Theory]
			[InlineData(true, "foo", "foo")]
			[InlineData(true, "foo", "bar")]
			[InlineData(true, "bar", "foo")]
			[InlineData(true, "bar", "bar")]
			[InlineData(true, "foo", "foo", "bar", "bar")]
			[InlineData(false, "foo", "bar", "baz")]
			[InlineData(false, "foo", "baz", "bar")]
			[InlineData(false, "baz", "foo", "bar")]
			[InlineData(false, "baz")]
			public async Task WithMultipleExpectations_ShouldVerifyAll(bool expectSuccess,
				params string[] expectedValues)
			{
				ItExtensions.IHttpContentParameter isHttpContent = It.IsHttpContent();
				foreach (string expectedValue in expectedValues)
				{
					isHttpContent = isHttpContent.WithString(expectedValue);
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

			[Fact]
			public async Task WithMultipleWithString_FailingVerification_ShouldJoinPredicatesWithAnd()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Verify.PostAsync(
							It.IsAny<string?>(),
							It.IsHttpContent()
								.WithString(s => s.StartsWith("abc"))
								.WithString(s => s.EndsWith("xyz")))
						.AtLeastOnce();
				}

				await That(Act).Throws<MockVerificationException>()
					.WithMessage("*s => s.StartsWith(\"abc\") and s => s.EndsWith(\"xyz\")*").AsWildcard();
			}
		}
	}
}
