#if NET8_0_OR_GREATER
using System;
using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

// ReSharper disable once CheckNamespace
namespace Mockolate;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class HttpClientExtensions
{
	/// <inheritdoc cref="HttpClientExtensions" />
	extension<TVerify>(TVerify verify) where TVerify : class, IMockVerify<HttpClient>
	{
		/// <summary>
		///     Verifies invocations of <see cref="System.Net.Http.HttpClient.PatchAsync(string?, HttpContent?)" /> on the
		///     mocked <see cref="HttpClient" /> matching <paramref name="requestUri" /> and <paramref name="content" />.
		/// </summary>
		/// <remarks>
		///     Matches on the underlying <c>SendAsync</c> invocation, so the verification succeeds for any
		///     <c>PatchAsync</c> overload (with or without <see cref="CancellationToken" />) that produced a request
		///     satisfying the supplied matchers.
		/// </remarks>
		/// <param name="requestUri">A <see langword="string" /> URI matcher - typically <c>It.Is(uri)</c> or a raw string.</param>
		/// <param name="content">An optional <see cref="HttpContent" /> matcher; <see langword="null" /> accepts any body.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>PatchAsync(
			IParameter<string?> requestUri,
			IParameter<HttpContent?>? content = null)
			=> verify.PatchAsync(
				requestUri,
				content ?? It.IsAny<HttpContent?>(),
				It.IsAny<CancellationToken>());

		/// <summary>
		///     Verifies invocations of <see cref="System.Net.Http.HttpClient.PatchAsync(Uri?, HttpContent?)" /> on the
		///     mocked <see cref="HttpClient" /> matching <paramref name="requestUri" /> and <paramref name="content" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher - typically <c>It.IsUri(...)</c>, <c>It.Is(uri)</c> or a raw <see cref="Uri" />.</param>
		/// <param name="content">An optional <see cref="HttpContent" /> matcher; <see langword="null" /> accepts any body.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>PatchAsync(
			IParameter<Uri?> requestUri,
			IParameter<HttpContent?>? content = null)
			=> verify.PatchAsync(
				requestUri,
				content ?? It.IsAny<HttpContent?>(),
				It.IsAny<CancellationToken>());

		/// <summary>
		///     Verifies invocations of
		///     <see cref="System.Net.Http.HttpClient.PatchAsync(string?, HttpContent?, System.Threading.CancellationToken)" />
		///     matching <paramref name="requestUri" />, <paramref name="content" /> and <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see langword="string" /> URI matcher.</param>
		/// <param name="content">An <see cref="HttpContent" /> matcher for the request body.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>PatchAsync(
			IParameter<string?> requestUri,
			IParameter<HttpContent?> content,
			IParameter<CancellationToken> cancellationToken)
		{
			if (verify is HttpClient and IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				HttpRequestMessageParameters requestMatcher = new(HttpMethod.Patch,
					new HttpStringUriParameter(requestUri),
					new HttpRequestMessageParameter<HttpContent?>(r => r.Content, content));
				IParameterMatch<CancellationToken> cancellationTokenMatcher = cancellationToken.AsParameterMatch();
				return httpMessageHandlerMock.MockRegistry.VerifyMethod<object, MethodInvocation<HttpRequestMessage, CancellationToken>>(
						httpClientMock.MockRegistry.ConstructorParameters[0]!,
						"global::System.Net.Http.HttpMessageHandler.SendAsync",
						method => requestMatcher.Matches(method.Parameter1) && cancellationTokenMatcher.Matches(method.Parameter2),
						() => $"SendAsync({requestMatcher}, {cancellationTokenMatcher})")
					.Map(verify);
			}

			throw new MockException(
				"Cannot verify HttpClient when it is not mocked with a mockable HttpMessageHandler.");
		}

		/// <summary>
		///     Verifies invocations of
		///     <see cref="System.Net.Http.HttpClient.PatchAsync(Uri?, HttpContent?, System.Threading.CancellationToken)" />
		///     matching <paramref name="requestUri" />, <paramref name="content" /> and <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher.</param>
		/// <param name="content">An <see cref="HttpContent" /> matcher for the request body.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>PatchAsync(
			IParameter<Uri?> requestUri,
			IParameter<HttpContent?> content,
			IParameter<CancellationToken> cancellationToken)
		{
			if (verify is HttpClient and IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				HttpRequestMessageParameters requestMatcher = new(HttpMethod.Patch,
					new HttpRequestMessageParameter<Uri?>(r => r.RequestUri, requestUri),
					new HttpRequestMessageParameter<HttpContent?>(r => r.Content, content));
				IParameterMatch<CancellationToken> cancellationTokenMatcher = cancellationToken.AsParameterMatch();
				return httpMessageHandlerMock.MockRegistry.VerifyMethod<object, MethodInvocation<HttpRequestMessage, CancellationToken>>(
						httpClientMock.MockRegistry.ConstructorParameters[0]!,
						"global::System.Net.Http.HttpMessageHandler.SendAsync",
						method => requestMatcher.Matches(method.Parameter1) && cancellationTokenMatcher.Matches(method.Parameter2),
						() => $"SendAsync({requestMatcher}, {cancellationTokenMatcher})")
					.Map(verify);
			}

			throw new MockException(
				"Cannot verify HttpClient when it is not mocked with a mockable HttpMessageHandler.");
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#endif
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
