using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Web;
#if !NET11_0_OR_GREATER
using Mockolate.Internals.Polyfills;
#endif

// ReSharper disable once CheckNamespace
namespace Mockolate;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
/// <summary>
///     Fluent <c>Setup</c> / <c>Verify</c> surface for a mocked <see cref="HttpClient" /> - turns HTTP method
///     calls (<c>GetAsync</c>, <c>PostAsync</c>, <c>PutAsync</c>, <c>PatchAsync</c>, <c>DeleteAsync</c> and the
///     underlying <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)" />) into expectations.
/// </summary>
/// <remarks>
///     How it works: calling <c>HttpClient.CreateMock()</c> returns a mock whose constructor injects a mocked
///     <see cref="HttpMessageHandler" />. These extensions route setup/verify calls through that handler, so any
///     <c>HttpClient</c> method that ultimately goes through <c>SendAsync</c> can be intercepted by verb.
///     <para />
///     Typical flow:
///     <list type="bullet">
///         <item>
///             <description>Setup: <c>httpClient.Mock.Setup.PostAsync(uriMatcher, contentMatcher).ReturnsAsync(response)</c> -  see <c>HttpClientExtensions.Setup.*</c>.</description>
///         </item>
///         <item>
///             <description>Verify: <c>httpClient.Mock.Verify.GetAsync(uriMatcher).Once()</c> - see <c>HttpClientExtensions.Verify.*</c>.</description>
///         </item>
///         <item>
///             <description>Match request pieces with <see cref="ItExtensions" />: <c>It.IsUri(...)</c>, <c>It.IsHttpContent(...)</c>, <c>It.IsHttpRequestMessage(...)</c> and their fluent <c>.WithString</c> / <c>.WithBytes</c> / <c>.WithFormData</c> / <c>.WithHeaders</c> builders.</description>
///         </item>
///     </list>
///     <para />
///     Throws <see cref="MockException" /> if the mock was constructed without a mockable
///     <see cref="HttpMessageHandler" /> (e.g. <c>new HttpClient()</c> passed by hand) - the message handler
///     injection is what makes the interception possible.
/// </remarks>
public static partial class HttpClientExtensions
{
	/// <inheritdoc cref="HttpClientExtensions" />
	extension(IMockSetup<HttpClient> setup)
	{
		/// <summary>
		///     Sets up <see cref="HttpClient.SendAsync(HttpRequestMessage, CancellationToken)" /> directly on the
		///     mocked <see cref="HttpClient" /> - the catch-all entry point that every verb-specific overload
		///     (<c>GetAsync</c>, <c>PostAsync</c>, ...) routes through.
		/// </summary>
		/// <remarks>
		///     Prefer <c>Setup.GetAsync</c>/<c>PostAsync</c>/<c>PutAsync</c>/<c>PatchAsync</c>/<c>DeleteAsync</c> when
		///     you only need to match by verb and URI; reach for <c>SendAsync</c> when you need the full
		///     <see cref="HttpRequestMessage" /> (e.g. to inspect headers or non-standard verbs like <c>HEAD</c> or
		///     <c>OPTIONS</c>).
		/// </remarks>
		/// <param name="request">A matcher for the <see cref="HttpRequestMessage" /> - usually <c>It.IsHttpRequestMessage(...)</c>.</param>
		/// <returns>A setup handle - chain a terminator like <c>.ReturnsAsync(...)</c>.</returns>
		/// <exception cref="MockException">The mock was not created with a mockable <see cref="System.Net.Http.HttpMessageHandler" /> constructor parameter.</exception>
		public IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> SendAsync(
			IParameter<HttpRequestMessage> request)
		{
			if (setup is IMock { MockRegistry.ConstructorParameters.Length: > 0, } httpClientMock &&
			    httpClientMock.MockRegistry.ConstructorParameters[0] is IMock httpMessageHandlerMock)
			{
				ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>.WithParameterCollection methodSetup =
					new(httpMessageHandlerMock.MockRegistry,
						"global::System.Net.Http.HttpMessageHandler.SendAsync",
						request.AsParameterMatch(),
						It.IsAny<CancellationToken>().AsParameterMatch());
				httpMessageHandlerMock.MockRegistry.SetupMethod(methodSetup);
				return methodSetup;
			}

			throw new MockException(
				"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
		}
	}

	private sealed class HttpRequestMessageParameters(
		HttpMethod method,
		params IHttpRequestMessageParameter[] parameters)
		: IParameterMatch<HttpRequestMessage>
	{
		public bool Matches(HttpRequestMessage value)
		{
			if (value.Method == method)
			{
				foreach (IHttpRequestMessageParameter parameter in parameters)
				{
					if (!parameter.Matches(value))
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}

		public void InvokeCallbacks(HttpRequestMessage value)
		{
			foreach (IHttpRequestMessageParameter parameter in parameters)
			{
				parameter.InvokeCallbacks(value);
			}
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			// At least one parameter is always present: all construction sites in HttpClientExtensions
			// (GetAsync, PostAsync, PutAsync, PatchAsync, DeleteAsync) pass at least one IHttpRequestMessageParameter.
			=> $"{method.Method}-Request with {string.Join(" and ", parameters.Select(p => p.ToString()))}";
	}

	private interface IHttpRequestMessageParameter
	{
		bool Matches(HttpRequestMessage value);

		void InvokeCallbacks(HttpRequestMessage value);
	}

	private sealed class HttpRequestMessageParameter<T>(
		Func<HttpRequestMessage, T> valueSelector,
		IParameter<T> parameter)
		: IHttpRequestMessageParameter
	{
		public bool Matches(HttpRequestMessage value)
		{
			if (parameter is IHttpRequestMessagePropertyParameter<T> httpRequestMessageParameter)
			{
				return httpRequestMessageParameter.Matches(valueSelector(value), value);
			}

			return parameter.AsParameterMatch().Matches(valueSelector(value));
		}

		public void InvokeCallbacks(HttpRequestMessage value)
			=> parameter.AsParameterMatch().InvokeCallbacks(valueSelector(value));

		/// <inheritdoc cref="object.ToString()" />
		public override string? ToString()
			=> parameter.ToString();
	}

	private sealed class HttpStringUriParameter(IParameter<string?> parameter)
		: IHttpRequestMessageParameter
	{
		public bool Matches(HttpRequestMessage value)
		{
			if (value.RequestUri is null)
			{
				return false;
			}

			IParameterMatch<string?> matcher = parameter.AsParameterMatch();
			string requestUri = value.RequestUri.ToString();
			return matcher.Matches(requestUri) ||
			       (requestUri.EndsWith('/', StringComparison.Ordinal) && matcher.Matches(requestUri.TrimEnd('/')));
		}

		public void InvokeCallbacks(HttpRequestMessage value)
			=> parameter.AsParameterMatch().InvokeCallbacks(value.RequestUri?.ToString());

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> $"Uri matching {parameter}";
	}
}
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
