using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class WithHeadersTests
		{
			[Test]
			public async Task IncludingRequestHeaders_ShouldMatchRequestHeadersFromContentAndRequest()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(("foo", "my-value"), ("Authorization", "Basic abcdef")).IncludingRequestHeaders())
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("foo", "my-value");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			public async Task IncludingRequestHeaders_WhenNotSet_ShouldOnlyMatchRequestHeadersFromContent()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "abcdef");
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(("foo", "my-value"), ("Authorization", "Basic abcdef")))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("foo", "my-value");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x-myHeader1", "foo", "x-myHeader3", "baz", true)]
			[Arguments("x-myHeader3", "baz", "x-myHeader1", "foo", true)]
			[Arguments("x-myHeader2", "baz", "x-myHeader1", "foo", false)]
			[Arguments("x-myHeader1", "foo", "x-myHeader2", "baz", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValueHeaders(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(key1, value1).WithHeaders(key2, value2))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", "foo");
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x-myHeader1", "foo", "x-myHeader3", "baz", true)]
			[Arguments("x-myHeader3", "baz", "x-myHeader1", "foo", true)]
			[Arguments("x-myHeader2", "baz", "x-myHeader1", "foo", false)]
			[Arguments("x-myHeader1", "foo", "x-myHeader2", "baz", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValuePairHeaders(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders((key1, value1)).WithHeaders((key2, value2)))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", "foo");
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x-myHeader1: foo", "x-myHeader3: baz", true)]
			[Arguments("x-myHeader3: baz", "x-myHeader1: foo", true)]
			[Arguments("x-myHeader2: baz", "x-myHeader1: foo", false)]
			[Arguments("x-myHeader1: foo", "x-myHeader2: baz", false)]
			public async Task MultipleCalls_ShouldVerifyStringHeaders(
				string headers1, string headers2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(headers1).WithHeaders(headers2))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", "foo");
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			public async Task ShouldOnlyRequireOneMatchingValue()
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent().WithHeaders("x-myHeader", "foo"))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader", "foo");
				content.Headers.Add("x-myHeader", "bar");
				content.Headers.Add("x-myHeader", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("foo", true)]
			[Arguments("bar", false)]
			[Arguments("FOO", true)]
			public async Task ShouldVerifyHeaderKeyCaseInsensitive(string key, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent().WithHeaders("foo", "my-value"))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add(key, "my-value");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
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
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent().WithHeaders("x-myHeader", "foo"))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader", value);

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("foo", true)]
			[Arguments("bar", false)]
			[Arguments("FOO", true)]
			public async Task ShouldVerifyMultipleHeaderKeyCaseInsensitive(string key, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(
							("foo", "my-foo-value"),
							("bar", "my-bar-value")))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add(key, "my-foo-value");
				content.Headers.Add("bar", "my-bar-value");
				content.Headers.Add("baz", "my-baz-value");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
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
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(
							("x-myHeader1", "foo"),
							("x-myHeader2", "bar")))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", value);
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
					CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("""
			            x-myHeader1 : foo
			             x-myHeader3: baz
			            """, true)]
			[Arguments("""
			            x-myHeader3: baz
			            x-myHeader1: foo
			            """, true)]
			[Arguments("""
			            x-myHeader2: foo
			            x-myHeader3: baz
			            """, false)]
			public async Task ShouldVerifyStringHeaders(string headers, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<string>(), It.IsHttpContent()
						.WithHeaders(headers))
					.ReturnsAsync(HttpStatusCode.OK);
				StringContent content = new("");
				content.Headers.Add("x-myHeader1", "foo");
				content.Headers.Add("x-myHeader2", "bar");
				content.Headers.Add("x-myHeader3", "baz");

				HttpResponseMessage result = await httpClient.PostAsync("https://www.testably.org",
					content,
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
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent()
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
