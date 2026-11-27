using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Setup;

// ReSharper disable once CheckNamespace
namespace Mockolate;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class HttpClientExtensions
{
	/// <inheritdoc cref="HttpClientExtensions" />
	extension(IMockSetup<HttpClient> setup)
	{
		/// <summary>
		///     Sets up <see cref="System.Net.Http.HttpClient.PostAsync(string?, HttpContent?)" /> on the mocked
		///     <see cref="HttpClient" /> for requests whose URI matches <paramref name="requestUri" /> and whose
		///     body matches <paramref name="content" />.
		/// </summary>
		/// <remarks>
		///     Mockolate intercepts the underlying <c>SendAsync</c> call on the injected handler, so this setup also
		///     covers code paths that call <c>HttpClient.PostAsync</c> with an explicit <see cref="CancellationToken" />.
		/// </remarks>
		/// <param name="requestUri">A <see langword="string" /> URI matcher - typically <c>It.Is(uri)</c> or a raw string.</param>
		/// <param name="content">An optional <see cref="HttpContent" /> matcher; <see langword="null" /> accepts any body (equivalent to <c>It.IsAny&lt;HttpContent?&gt;()</c>).</param>
		/// <returns>A setup handle - chain a terminator like <c>.ReturnsAsync(...)</c>.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> PostAsync(
			IParameter<string?> requestUri,
			IParameter<HttpContent?>? content = null)
			=> setup.PostAsync(
				requestUri,
				content ?? It.IsAny<HttpContent?>(),
				It.IsAny<CancellationToken>());

		/// <summary>
		///     Sets up <see cref="System.Net.Http.HttpClient.PostAsync(Uri?, HttpContent?)" /> on the mocked
		///     <see cref="HttpClient" /> for requests whose URI matches <paramref name="requestUri" /> and whose
		///     body matches <paramref name="content" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher - typically <c>It.IsUri(...)</c>, <c>It.Is(uri)</c> or a raw <see cref="Uri" />.</param>
		/// <param name="content">An optional <see cref="HttpContent" /> matcher; <see langword="null" /> accepts any body.</param>
		/// <returns>A setup handle - chain a terminator like <c>.ReturnsAsync(...)</c>.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> PostAsync(
			IParameter<Uri?> requestUri,
			IParameter<HttpContent?>? content = null)
			=> setup.PostAsync(
				requestUri,
				content ?? It.IsAny<HttpContent?>(),
				It.IsAny<CancellationToken>());

		/// <summary>
		///     Sets up <see cref="System.Net.Http.HttpClient.PostAsync(string?, HttpContent?, System.Threading.CancellationToken)" />
		///     for requests matching <paramref name="requestUri" />, <paramref name="content" /> and
		///     <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see langword="string" /> URI matcher.</param>
		/// <param name="content">An <see cref="HttpContent" /> matcher for the request body.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>A setup handle - chain a terminator like <c>.ReturnsAsync(...)</c>.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> PostAsync(
			IParameter<string?> requestUri,
			IParameter<HttpContent?> content,
			IParameter<CancellationToken> cancellationToken)
		{
			if (setup is IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>.WithParameterCollection methodSetup =
					new(httpMessageHandlerMock.MockRegistry,
						"global::System.Net.Http.HttpMessageHandler.SendAsync",
						new HttpRequestMessageParameters(HttpMethod.Post,
							new HttpStringUriParameter(requestUri),
							new HttpRequestMessageParameter<HttpContent?>(r => r.Content, content)),
						cancellationToken.AsParameterMatch());
				httpMessageHandlerMock.MockRegistry.SetupMethod(methodSetup);
				return methodSetup;
			}

			throw new MockException(
				"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
		}

		/// <summary>
		///     Sets up <see cref="System.Net.Http.HttpClient.PostAsync(Uri?, HttpContent?, System.Threading.CancellationToken)" />
		///     for requests matching <paramref name="requestUri" />, <paramref name="content" /> and
		///     <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher.</param>
		/// <param name="content">An <see cref="HttpContent" /> matcher for the request body.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>A setup handle - chain a terminator like <c>.ReturnsAsync(...)</c>.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> PostAsync(
			IParameter<Uri?> requestUri,
			IParameter<HttpContent?> content,
			IParameter<CancellationToken> cancellationToken)
		{
			if (setup is IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>.WithParameterCollection methodSetup =
					new(httpMessageHandlerMock.MockRegistry,
						"global::System.Net.Http.HttpMessageHandler.SendAsync",
						new HttpRequestMessageParameters(HttpMethod.Post,
							new HttpRequestMessageParameter<Uri?>(r => r.RequestUri, requestUri),
							new HttpRequestMessageParameter<HttpContent?>(r => r.Content, content)),
						cancellationToken.AsParameterMatch());
				httpMessageHandlerMock.MockRegistry.SetupMethod(methodSetup);
				return methodSetup;
			}

			throw new MockException(
				"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
