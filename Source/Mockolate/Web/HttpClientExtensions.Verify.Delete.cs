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
		///     Verifies invocations of <see cref="System.Net.Http.HttpClient.DeleteAsync(string?)" /> on the mocked
		///     <see cref="HttpClient" /> matching <paramref name="requestUri" />.
		/// </summary>
		/// <remarks>
		///     Matches on the underlying <c>SendAsync</c> invocation, so the verification succeeds for any
		///     <c>DeleteAsync</c> overload (with or without <see cref="CancellationToken" />) that produced a request
		///     satisfying the supplied matchers.
		/// </remarks>
		/// <param name="requestUri">A <see langword="string" /> URI matcher - typically <c>It.Is(uri)</c> or a raw string.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>DeleteAsync(
			IParameter<string?> requestUri)
			=> verify.DeleteAsync(requestUri, It.IsAny<CancellationToken>());

		/// <summary>
		///     Verifies invocations of <see cref="System.Net.Http.HttpClient.DeleteAsync(Uri?)" /> on the mocked
		///     <see cref="HttpClient" /> matching <paramref name="requestUri" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher - typically <c>It.IsUri(...)</c>, <c>It.Is(uri)</c> or a raw <see cref="Uri" />.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>DeleteAsync(
			IParameter<Uri?> requestUri)
			=> verify.DeleteAsync(requestUri, It.IsAny<CancellationToken>());

		/// <summary>
		///     Verifies invocations of
		///     <see cref="System.Net.Http.HttpClient.DeleteAsync(string?, System.Threading.CancellationToken)" />
		///     matching <paramref name="requestUri" /> and <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see langword="string" /> URI matcher.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>DeleteAsync(
			IParameter<string?> requestUri,
			IParameter<CancellationToken> cancellationToken)
		{
			if (verify is HttpClient and IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				HttpRequestMessageParameters requestMatcher = new(HttpMethod.Delete, new HttpStringUriParameter(requestUri));
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
		///     <see cref="System.Net.Http.HttpClient.DeleteAsync(Uri?, System.Threading.CancellationToken)" />
		///     matching <paramref name="requestUri" /> and <paramref name="cancellationToken" />.
		/// </summary>
		/// <param name="requestUri">A <see cref="Uri" /> matcher.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken" /> matcher - pass <c>It.IsAny&lt;CancellationToken&gt;()</c> to accept any token.</param>
		/// <returns>An intermediate <see cref="VerificationResult{TVerify}" /> - terminate with a count assertion.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public VerificationResult<TVerify>DeleteAsync(
			IParameter<Uri?> requestUri,
			IParameter<CancellationToken> cancellationToken)
		{
			if (verify is HttpClient and IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				HttpRequestMessageParameters requestMatcher = new(HttpMethod.Delete,
					new HttpRequestMessageParameter<Uri?>(r => r.RequestUri, requestUri));
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
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
