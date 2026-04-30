using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Parameters;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpRequestMessageTests
	{
		[Test]
		public async Task NonGeneric_DispatchesHttpRequestMessageThroughCallback()
		{
			ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage();
			HttpRequestMessage? captured = null;
			sut.Do(message => captured = message);

			HttpRequestMessage target = new(HttpMethod.Get, "https://www.testably.org");
			sut.InvokeCallbacks(target);

			await That(captured).IsSameAs(target);
		}

		[Test]
		public async Task NonGeneric_IgnoresUnrelatedTypes()
		{
			ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage();
			int invocations = 0;
			sut.Do(_ => invocations++);

			sut.InvokeCallbacks("not a request message");

			await That(invocations).IsEqualTo(0);
		}

		[Test]
		public async Task NonGenericMatches_ReturnsFalseForUnrelatedType()
		{
			ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage();

			bool result = sut.Matches("not a request message");

			await That(result).IsFalse();
		}

		[Test]
		public async Task NonGenericMatches_ReturnsTrueForHttpRequestMessage()
		{
			ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage();

			bool result = sut.Matches(new HttpRequestMessage(HttpMethod.Get, "https://www.testably.org"));

			await That(result).IsTrue();
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
			httpClient.Mock.Setup.SendAsync(It.IsHttpRequestMessage()
				.Do(_ => callbackCount++)
				.Monitor(out IParameterMonitor<HttpRequestMessage> monitor));

			foreach (ByteArrayContent response in responses)
			{
				await httpClient.PostAsync("https://www.testably.org", response, CancellationToken.None);
			}

#if !NETFRAMEWORK
			await That(
					(await Task.WhenAll(monitor.Values.Select(c => c!.Content!.ReadAsByteArrayAsync())))
					.Select(x => x.Length))
				.IsEqualTo([0, 1, 3,]);
#endif
			await That(callbackCount).IsEqualTo(3);
		}

		[Test]
		public async Task WithHeaders_OnRequestWithoutContent_ShouldStillEvaluate()
		{
			ItExtensions.IHttpRequestMessageParameter sut = It.IsHttpRequestMessage()
				.WithHeaders(("x-correlation", "abc"));
			HttpRequestMessage withoutContent = new(HttpMethod.Get, "https://www.testably.org");
			withoutContent.Headers.Add("x-correlation", "abc");
			HttpRequestMessage withContent = new(HttpMethod.Post, "https://www.testably.org")
			{
				Content = new StringContent("body"),
			};
			withContent.Headers.Add("x-correlation", "abc");

			bool matchesWithoutContent = ((IParameterMatch<HttpRequestMessage>)sut).Matches(withoutContent);
			bool matchesWithContent = ((IParameterMatch<HttpRequestMessage>)sut).Matches(withContent);

			await That(matchesWithoutContent).IsTrue();
			await That(matchesWithContent).IsTrue();
		}

		[Test]
		[Arguments(nameof(HttpMethod.Get), true)]
		[Arguments(nameof(HttpMethod.Delete), false)]
		[Arguments(nameof(HttpMethod.Post), false)]
		[Arguments(nameof(HttpMethod.Put), false)]
		public async Task WithMethod_ShouldVerifyMethod(string method, bool expectSuccess)
		{
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.SendAsync(It.IsHttpRequestMessage(new HttpMethod(method)))
				.ReturnsAsync(HttpStatusCode.OK);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org",
				CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
		}
	}
}
