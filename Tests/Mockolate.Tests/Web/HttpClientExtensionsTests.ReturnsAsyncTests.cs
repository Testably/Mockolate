using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	public sealed class ReturnsAsyncTests
	{
		[Test]
		[Arguments(HttpStatusCode.OK)]
		[Arguments(HttpStatusCode.NotFound)]
		[Arguments(HttpStatusCode.InternalServerError)]
		[Arguments(HttpStatusCode.Forbidden)]
		public async Task WithStatusCode_ShouldReturnHttpResponseMessageWithStatusCode(HttpStatusCode statusCode)
		{
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
#if NET8_0_OR_GREATER
			await That(result.Content.ReadAsByteArrayAsync()).IsEmpty();
#else
			await That(result.Content).IsNull();
#endif
		}

		[Test]
		[Arguments(HttpStatusCode.OK, "foo")]
		[Arguments(HttpStatusCode.NotFound, "bar")]
		public async Task WithStatusCodeAndBytes_ShouldReturnHttpResponseMessageWithStatusCodeAndByteArrayContent(
			HttpStatusCode statusCode, string stringContent)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(stringContent);
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode, bytes);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
			await That(result.Content).Is<ByteArrayContent>();
			await That(result.Content.ReadAsByteArrayAsync()).IsEqualTo(bytes);
			await That(result.Content.Headers.ContentType).IsNull();
		}

		[Test]
		[Arguments(HttpStatusCode.OK, "foo")]
		[Arguments(HttpStatusCode.NotFound, "bar")]
		public async Task
			WithStatusCodeAndHttpContent_ShouldReturnHttpResponseMessageWithStatusCodeAndByteArrayContent(
				HttpStatusCode statusCode, string stringContent)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(stringContent);
			ByteArrayContent content = new(bytes);
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode, content);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
			await That(result.Content).Is<ByteArrayContent>();
			await That(result.Content.ReadAsStringAsync()).IsEqualTo(stringContent);
			await That(result.Content.Headers.ContentType).IsNull();
		}

		[Test]
		[Arguments(HttpStatusCode.OK, "foo")]
		[Arguments(HttpStatusCode.NotFound, "bar")]
		public async Task WithStatusCodeAndString_ShouldReturnHttpResponseMessageWithStatusCodeAndStringContent(
			HttpStatusCode statusCode, string content)
		{
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode, content);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
			await That(result.Content).Is<StringContent>();
			await That(result.Content.ReadAsStringAsync()).IsEqualTo(content);
			await That(result.Content.Headers.ContentType?.MediaType).IsEqualTo("text/plain");
		}

		[Test]
		[Arguments(HttpStatusCode.OK, "foo", "text/plain")]
		[Arguments(HttpStatusCode.NotFound, "bar", "application/json")]
		public async Task
			WithStatusCodeBytesAndMediaType_ShouldReturnHttpResponseMessageWithStatusCodeAndByteArrayContent(
				HttpStatusCode statusCode, string stringContent, string mediaType)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(stringContent);
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode, bytes, mediaType);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
			await That(result.Content).Is<ByteArrayContent>();
			await That(result.Content.ReadAsByteArrayAsync()).IsEqualTo(bytes);
			await That(result.Content.Headers.ContentType?.MediaType).IsEqualTo(mediaType);
		}

		[Test]
		[Arguments(HttpStatusCode.OK, "foo", "text/plain")]
		[Arguments(HttpStatusCode.NotFound, "bar", "application/json")]
		public async Task
			WithStatusCodeStringContentAndMediaType_ShouldReturnHttpResponseMessageWithStatusCodeAndStringContent(
				HttpStatusCode statusCode, string content, string mediaType)
		{
			HttpClient httpClient = HttpClient.CreateMock();
			httpClient.Mock.Setup
				.GetAsync(It.IsAny<Uri>())
				.ReturnsAsync(statusCode, content, mediaType);

			HttpResponseMessage result = await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

			await That(result.StatusCode).IsEqualTo(statusCode);
			await That(result.Content).Is<StringContent>();
			await That(result.Content.ReadAsStringAsync()).IsEqualTo(content);
			await That(result.Content.Headers.ContentType?.MediaType).IsEqualTo(mediaType);
		}
	}
}
