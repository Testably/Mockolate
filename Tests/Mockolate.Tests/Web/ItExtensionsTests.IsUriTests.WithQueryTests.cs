using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsUriTests
	{
		public sealed class WithQueryTests
		{
			[Test]
			[Arguments("x", "123", "z", "345", true)]
			[Arguments("z", "345", "x", "123", true)]
			[Arguments("x", "123", "y", "345", false)]
			[Arguments("y", "345", "x", "123", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValuePairQueryParameters(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery((key1, value1)).WithQuery((key2, value2)))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient
					.GetAsync("https://www.testably.org/foo/bar?x=123&y=234&z=345", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x", "123", "z", "345", true)]
			[Arguments("z", "345", "x", "123", true)]
			[Arguments("x", "123", "y", "345", false)]
			[Arguments("y", "345", "x", "123", false)]
			public async Task MultipleCalls_ShouldVerifyKeyValueQueryParameters(
				string key1, string value1, string key2, string value2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(key1, value1).WithQuery(key2, value2))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient
					.GetAsync("https://www.testably.org/foo/bar?x=123&y=234&z=345", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x=123", "z=345", true)]
			[Arguments("z=345", "x=123", true)]
			[Arguments("x=123", "y=345", false)]
			[Arguments("y=345", "x=123", false)]
			public async Task MultipleCalls_ShouldVerifyStringQueryParameters(
				string query1, string query2, bool expectSuccess)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(query1).WithQuery(query2))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient
					.GetAsync("https://www.testably.org/foo/bar?x=123&y=234&z=345", CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x<>", "1<2> 3")]
			public async Task ShouldEncodeValues(string key, string value)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(key, value))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org/?x%3c%3e=1%3c2%3e+3",
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x", "123", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "y", "234", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x", "", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "y", "", false)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x", "234", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "y", "123", false)]
			[Arguments("https://www.testably.org/foo/bar?x=123&foo&y=234", "foo", "", true)]
			public async Task WithQueryParameter_ShouldVerifyQueryParameters(string uri, string key, string value,
				bool expectMatch)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(key, value))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.GetAsync(uri, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectMatch ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x", "123", "z", "345")]
			[Arguments("z", "345", "x", "123")]
			public async Task WithQueryParameters_ShouldMatchQueryParametersInAnyOrder(params string[] query)
			{
				List<(string, HttpQueryParameterValue)> queryParameters = new();
				for (int i = 0; i < query.Length; i += 2)
				{
					queryParameters.Add((query[i], new HttpQueryParameterValue(query[i + 1])));
				}

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(queryParameters))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org?x=123&y=234&z=345",
					CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "?x=123&y=234", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "?y=234&x=123&", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x=123", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "y=234", true)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "y", false)]
			[Arguments("https://www.testably.org/foo/bar?x=123&y=234", "x=234", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "y=123", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&y=234", "x=123&y=23", false)]
			[Arguments("http://www.testably.org/foo/bar?x=123&foo&y=234", "foo", true)]
			public async Task WithQueryString_ShouldVerifyQueryParameters(string uri, string query, bool expectMatch)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.GetAsync(It.IsUri().WithQuery(query))
					.ReturnsAsync(HttpStatusCode.OK);

				HttpResponseMessage result = await httpClient.GetAsync(uri, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectMatch ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}
		}
	}
}
