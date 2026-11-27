using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.Setup;

// ReSharper disable once CheckNamespace
namespace Mockolate;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
public static partial class HttpClientExtensions
{
	/// <inheritdoc cref="HttpClientExtensions" />
	extension(IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> setup)
	{
		/// <summary>
		///     Completes the mocked request with an empty <see cref="HttpResponseMessage" /> carrying the given
		///     <paramref name="statusCode" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <returns>The setup's return-builder - chain <c>.For(n)</c>, <c>.Forever()</c> or additional <c>Returns*</c>/<c>Throws</c> entries to build a sequence.</returns>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode)
			=> setup.ReturnsAsync(new HttpResponseMessage(statusCode));

		/// <summary>
		///     Completes the mocked request with an <see cref="HttpResponseMessage" /> whose status is
		///     <paramref name="statusCode" /> and whose body is the given <see langword="string" /> <paramref name="content" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <param name="content">The response body, encoded as UTF-8 with a default <c>text/plain</c> media type.</param>
		/// <returns>The setup's return-builder.</returns>
		/// <remarks>
		///     For a different character encoding use the <see cref="HttpContent" /> overload; for a different media
		///     type use the <c>(statusCode, content, mediaType)</c> overload.
		/// </remarks>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode, string content)
			=> setup.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content),
			});

		/// <summary>
		///     Completes the mocked request with an <see cref="HttpResponseMessage" /> whose status is
		///     <paramref name="statusCode" />, body is the given <see langword="string" /> <paramref name="content" />
		///     and <c>Content-Type</c> is <paramref name="mediaType" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <param name="content">The response body, encoded as UTF-8.</param>
		/// <param name="mediaType">The response media type, e.g. <c>"application/json"</c>.</param>
		/// <returns>The setup's return-builder.</returns>
		/// <remarks>
		///     For a non-UTF-8 encoding use the <see cref="HttpContent" /> overload.
		/// </remarks>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode, string content, string mediaType)
			=> setup.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content, Encoding.UTF8, mediaType),
			});

		/// <summary>
		///     Completes the mocked request with an <see cref="HttpResponseMessage" /> whose status is
		///     <paramref name="statusCode" /> and whose body is the given raw <paramref name="bytes" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <param name="bytes">The response body as a raw byte array (wrapped in a <see cref="ByteArrayContent" />).</param>
		/// <returns>The setup's return-builder.</returns>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode, byte[] bytes)
			=> setup.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new ByteArrayContent(bytes),
			});

		/// <summary>
		///     Completes the mocked request with an <see cref="HttpResponseMessage" /> whose status is
		///     <paramref name="statusCode" />, body is the given raw <paramref name="bytes" /> and <c>Content-Type</c>
		///     is <paramref name="mediaType" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <param name="bytes">The response body as a raw byte array.</param>
		/// <param name="mediaType">The response media type applied as a <see cref="MediaTypeHeaderValue" />.</param>
		/// <returns>The setup's return-builder.</returns>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode, byte[] bytes, string mediaType)
		{
			ByteArrayContent content = new(bytes);
			content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
			return setup.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = content,
			});
		}

		/// <summary>
		///     Completes the mocked request with an <see cref="HttpResponseMessage" /> whose status is
		///     <paramref name="statusCode" /> and whose body is the supplied <see cref="HttpContent" />.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode" /> to return.</param>
		/// <param name="content">The response body - use this overload for custom encodings, streams or structured content (e.g. <see cref="System.Net.Http.MultipartContent" />).</param>
		/// <returns>The setup's return-builder.</returns>
		public IReturnMethodSetupReturnBuilder<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>
			ReturnsAsync(HttpStatusCode statusCode, HttpContent content)
			=> setup.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = content,
			});
	}
}
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
