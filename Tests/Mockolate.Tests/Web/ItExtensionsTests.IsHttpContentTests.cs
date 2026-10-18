using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Mockolate.Parameters;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		[Test]
		public async Task NonGeneric_DispatchesContentThroughCallback()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();
			HttpContent? captured = null;
			sut.Do(content => captured = content);

			MyHttpContent target = new();
			sut.InvokeCallbacks(target);

			await That(captured).IsSameAs(target);
		}

		[Test]
		public async Task NonGeneric_DispatchesNullThroughCallback()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();
			int invocations = 0;
			HttpContent? captured = new MyHttpContent();
			sut.Do(content =>
			{
				invocations++;
				captured = content;
			});

			sut.InvokeCallbacks(null);

			await That(invocations).IsEqualTo(1);
			await That(captured).IsNull();
		}

		[Test]
		public async Task NonGeneric_IgnoresUnrelatedTypes()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();
			int invocations = 0;
			sut.Do(_ => invocations++);

			sut.InvokeCallbacks("not http content");

			await That(invocations).IsEqualTo(0);
		}

		[Test]
		public async Task NonGenericMatches_ReturnsFalseForNullValue()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();

			bool result = sut.Matches(null);

			await That(result).IsFalse();
		}

		[Test]
		public async Task NonGenericMatches_ReturnsFalseForUnrelatedType()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();

			bool result = sut.Matches("not http content");

			await That(result).IsFalse();
		}

		[Test]
		public async Task ShouldSupportMonitoring()
		{
			int callbackCount = 0;
			List<ByteArrayContent> responses =
			[
				new([]),
				new([0x66,]),
				new([0x62, 0x61, 0x72,]),
			];
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup.PostAsync(It.IsAny<Uri>(),
				It.IsHttpContent()
					.Do(_ => callbackCount++)
					.Monitor(out IParameterMonitor<HttpContent?> monitor));

			foreach (ByteArrayContent response in responses)
			{
				await httpClient.PostAsync("https://www.testably.org", response, CancellationToken.None);
			}

#if !NETFRAMEWORK
			await That(
					(await Task.WhenAll(monitor.Values.Select(c => c!.ReadAsByteArrayAsync())))
					.Select(x => x.Length))
				.IsEqualTo([0, 1, 3,]);
#endif
			await That(callbackCount).IsEqualTo(3);
		}

		[Test]
		public async Task ShouldSupportMultipleCombinations()
		{
			byte[] bytes = "foo"u8.ToArray();
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.PostAsync(It.IsAny<Uri>(), It.IsHttpContent()
					.WithStringMatching("*")
					.WithString("foo")
					.WithBytes(b => b.Length == 3))
				.ReturnsAsync(HttpStatusCode.OK);
			ByteArrayContent content = new(bytes);
			content.Headers.Add("x-my-header", "my-value");

			HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
				content,
				CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
		}

		[Test]
		public async Task ShouldSupportWithHeadersInWrapper()
		{
			byte[] bytes = "foo"u8.ToArray();
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.PostAsync(It.IsAny<Uri>(), It.IsHttpContent()
					.WithStringMatching("*")
					.WithHeaders("x-my-header", "my-value"))
				.ReturnsAsync(HttpStatusCode.OK);
			ByteArrayContent content = new(bytes);
			content.Headers.Add("x-my-header", "my-value");

			HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
				content,
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
				.PostAsync(It.IsAny<Uri>(), It.IsHttpContent("image/png"))
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
		public async Task TypedMatches_ReturnsFalseForNullValue()
		{
			ItExtensions.IHttpContentParameter sut = It.IsHttpContent();

			bool result = ((IParameterMatch<HttpContent?>)sut).Matches(null);

			await That(result).IsFalse();
		}

		[Test]
		[Arguments("image/png", true)]
		[Arguments("text/plain", false)]
		[Arguments("image/gif", false)]
		public async Task WithMediaType_ShouldVerifyMediaType(string mediaType, bool expectSuccess)
		{
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithMediaType("image/png"))
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
		public async Task Wrapper_NonGenericInvokeCallbacks_DelegatesToInnerParameter()
		{
			ItExtensions.IStringContentBodyMatchingParameter sut = It.IsHttpContent().WithStringMatching("foo*");
			int invocations = 0;
			HttpContent? captured = new MyHttpContent();
			sut.Do(content =>
			{
				invocations++;
				captured = content;
			});

			sut.InvokeCallbacks(null);

			await That(invocations).IsEqualTo(1);
			await That(captured).IsNull();
		}

		[Test]
		public async Task Wrapper_NonGenericMatches_DelegatesToInnerParameter()
		{
			ItExtensions.IStringContentBodyMatchingParameter sut = It.IsHttpContent().WithStringMatching("foo*");

			bool resultForNull = sut.Matches(null);
			bool resultForUnrelated = sut.Matches("not http content");

			await That(resultForNull).IsFalse();
			await That(resultForUnrelated).IsFalse();
		}

		[Test]
		public async Task Wrapper_TypedMatches_ReturnsFalseForNullValue()
		{
			ItExtensions.IStringContentBodyMatchingParameter sut = It.IsHttpContent().WithStringMatching("foo*");

			bool result = ((IParameterMatch<HttpContent?>)sut).Matches(null);

			await That(result).IsFalse();
		}

		private sealed class MyHttpContent : HttpContent
		{
			protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
				=> Task.CompletedTask;

			protected override bool TryComputeLength(out long length)
			{
				length = 0;
				return false;
			}
		}
	}
}
