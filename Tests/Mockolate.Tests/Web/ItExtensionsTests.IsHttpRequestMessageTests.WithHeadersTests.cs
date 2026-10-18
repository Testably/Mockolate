using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpRequestMessageTests
	{
		public sealed class WithHeadersTests
		{
			[Test]
			[Arguments("x-myHeader", "foo", "Authorization", "Basic abcdef", true)]
			[Arguments("Authorization", "Basic abcdef", "x-myHeader", "foo", true)]
			[Arguments("Authorization", "Basic xyz", "x-myHeader", "foo", false)]
			[Arguments("x-myHeader", "foo", "Authorization", "Basic xyz", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValueHeaders(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders(key1, value1).WithHeaders(key2, value2))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader", "foo");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x-myHeader", "foo", "Authorization", "Basic abcdef", true)]
			[Arguments("Authorization", "Basic abcdef", "x-myHeader", "foo", true)]
			[Arguments("Authorization", "Basic xyz", "x-myHeader", "foo", false)]
			[Arguments("x-myHeader", "foo", "Authorization", "Basic xyz", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValuePairHeaders(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders((key1, value1)).WithHeaders((key2, value2)))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader", "foo");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x-myHeader: foo", "Authorization: Basic abcdef", true)]
			[Arguments("Authorization: Basic abcdef", "x-myHeader: foo", true)]
			[Arguments("Authorization: Basic xyz", "x-myHeader: foo", false)]
			[Arguments("x-myHeader: foo", "Authorization: Basic xyz", false)]
			public async Task MultipleCalls_ShouldVerifyStringHeaders(
				string headers1, string headers2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders(headers1).WithHeaders(headers2))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader", "foo");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task ShouldAlsoMatchContentHeaders()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders("""
						             x-myHeader1 : foo
						              Authorization : Basic abcdef
						              x-myHeader3: baz
						             """))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", "foo");
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			public async Task ShouldMatchAgainstDefaultRequestHeaders()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders("""
						             Authorization: Basic abcdef
						             Accept: application/json
						             """))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("Authorization", true)]
			[Arguments("AUTHORIZATION", true)]
			[Arguments("Authentication", false)]
			public async Task ShouldVerifyHeaderKeyCaseInsensitive(string key, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WithHeaders(key, "Basic abcdef"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", true)]
			[Arguments("bar", false)]
			[Arguments("FOO", false)]
			public async Task ShouldVerifyHeaderValueCaseSensitive(string value, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "foo");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage().WithHeaders("Authorization", $"Basic {value}"))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("Authorization", true)]
			[Arguments("AUTHORIZATION", true)]
			[Arguments("Authentication", false)]
			public async Task ShouldVerifyMultipleHeaderKeyCaseInsensitive(string key, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "foo");
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders(
							(key, "Basic foo"),
							("Accept", "application/json")))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", true)]
			[Arguments("bar", false)]
			[Arguments("FOO", false)]
			public async Task ShouldVerifyMultipleHeaderValueCaseSensitive(string value, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", value);
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				httpClient.Mock.Setup
					.SendAsync(It.IsHttpRequestMessage()
						.WithHeaders(
							("Authorization", "Basic foo"),
							("Accept", "application/json")))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					new StringContent(""),
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task WithInvalidStringHeader_ShouldThrowArgumentException()
			{
				HttpClient httpClient = HttpClient.CreateMock();

				void Act()
				{
					httpClient.Mock.Setup
						.SendAsync(It.IsHttpRequestMessage()
							.WithHeaders("""
							             x-myHeader1: foo
							             x-myHeader2
							             x-myHeader3: baz
							             """))
						.ReturnsAsync(HttpStatusCode.OK);
				}

				await That(Act).Throws<ArgumentException>()
					.WithParamName("headers").And
					.WithMessage("The header contained an invalid line: x-myHeader2").AsPrefix();
			}
		}
	}
}
