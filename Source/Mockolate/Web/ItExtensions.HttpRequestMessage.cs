using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Mockolate.Parameters;

namespace Mockolate.Web;

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="ItExtensions" />
	extension(It _)
	{
		/// <summary>
		///     Matches any <see cref="HttpRequestMessage" />, optionally restricted to a specific HTTP
		///     <paramref name="method" /> (<c>GET</c>, <c>POST</c>, ...).
		/// </summary>
		/// <param name="method">When non-<see langword="null" />, only requests with this <see cref="HttpMethod" /> match.</param>
		/// <remarks>
		///     Use this when you need to match on the whole request rather than individual parts. Chain builders on the
		///     returned <see cref="IHttpRequestMessageParameter" /> to add constraints:
		///     <list type="bullet">
		///         <item>
		///             <description><c>.WhoseUriIs(uri)</c> / <c>.WhoseUriIs(configure)</c> - constrain the request URI (same surface as <c>It.IsUri(...)</c>).</description>
		///         </item>
		///         <item>
		///             <description><c>.WhoseContentIs(configure)</c> / <c>.WhoseContentIs(mediaType, configure)</c> - constrain the request body (same surface as <c>It.IsHttpContent(...)</c>).</description>
		///         </item>
		///         <item>
		///             <description><c>.WithHeaders(...)</c> - require specific request headers.</description>
		///         </item>
		///     </list>
		///     For the common verbs prefer <c>Setup.GetAsync</c>/<c>PostAsync</c>/... which internally use this matcher;
		///     reach for <c>It.IsHttpRequestMessage(...)</c> when composing a <c>SendAsync</c>-level setup.
		/// </remarks>
		/// <returns>A fluent matcher for <see cref="HttpRequestMessage" /> parameters.</returns>
		public static IHttpRequestMessageParameter IsHttpRequestMessage(HttpMethod? method = null)
			=> new HttpRequestMessageParameter(method);
	}

	/// <summary>
	///     Further expectations on the <see cref="HttpRequestMessage" />.
	/// </summary>
	public interface IHttpRequestMessageParameter : IHttpRequestMessageParameter<IHttpRequestMessageParameter>;

	/// <summary>
	///     Further expectations on the <see cref="HttpRequestMessage" />.
	/// </summary>
	public interface IHttpRequestMessageParameter<out TParameter> : IParameterWithCallback<HttpRequestMessage>,
		IHttpHeaderParameter<TParameter>
	{
		/// <summary>
		///     Add expectations on the <see cref="HttpContent" /> of the <see cref="HttpRequestMessage" />.
		/// </summary>
		TParameter WhoseContentIs(Action<IHttpContentParameter> configureContent);

		/// <summary>
		///     Add expectations on the <see cref="HttpContent" /> of the <see cref="HttpRequestMessage" />
		///     with the given <paramref name="mediaType" />.
		/// </summary>
		TParameter WhoseContentIs(string mediaType, Action<IHttpContentParameter>? configureContent = null);

		/// <summary>
		///     Add expectations on the URI of the <see cref="HttpRequestMessage" />.
		/// </summary>
		TParameter WhoseUriIs(Action<IUriParameter> configureUri);

		/// <summary>
		///     Add expectations on the URI of the <see cref="HttpRequestMessage" />.
		/// </summary>
		TParameter WhoseUriIs(string uri, Action<IUriParameter>? configureUri = null);
	}

	private abstract class HttpRequestMessageParameter<TParameter>
		: IHttpRequestMessageParameter<TParameter>, IParameterMatch<HttpRequestMessage>
	{
		private List<Action<HttpRequestMessage>>? _callbacks;
		private IParameter<HttpContent?>? _contentParameter;
		private HttpHeadersMatcher? _headers;
		private IParameter<Uri?>? _uriParameter;

		/// <summary>
		///     Returns <see langword="this" /> typed as <typeparamref name="TParameter" /> for fluent API.
		/// </summary>
		protected abstract TParameter GetThis { get; }

		public TParameter WithHeaders(params IEnumerable<(string Name, HttpHeaderValue Value)> headers)
		{
			_headers ??= new HttpHeadersMatcher();
			_headers.AddRequiredHeader(headers);
			return GetThis;
		}

		/// <inheritdoc cref="IParameterWithCallback{T}.Do(Action{T})" />
		public IParameterWithCallback<HttpRequestMessage> Do(Action<HttpRequestMessage> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		public TParameter WhoseContentIs(Action<IHttpContentParameter> configureContent)
		{
			IHttpContentParameter parameter = new HttpContentParameter();
			configureContent.Invoke(parameter);
			_contentParameter = parameter;
			return GetThis;
		}

		public TParameter WhoseContentIs(string mediaType, Action<IHttpContentParameter>? configureContent = null)
		{
			IHttpContentParameter parameter = new HttpContentParameter();
			parameter.WithMediaType(mediaType);
			configureContent?.Invoke(parameter);
			_contentParameter = parameter;
			return GetThis;
		}

		public TParameter WhoseUriIs(Action<IUriParameter> configureUri)
		{
			IUriParameter parameter = new UriParameter(pattern: null);
			configureUri.Invoke(parameter);
			_uriParameter = parameter;
			return GetThis;
		}

		public TParameter WhoseUriIs(string uri, Action<IUriParameter>? configureUri = null)
		{
			IUriParameter parameter = new UriParameter(uri);
			configureUri?.Invoke(parameter);
			_uriParameter = parameter;
			return GetThis;
		}

		/// <inheritdoc cref="IParameter.Matches(object?)" />
		bool IParameter.Matches(object? value)
			=> value is HttpRequestMessage message && Matches(message);

		/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
		void IParameter.InvokeCallbacks(object? value)
		{
			if (value is HttpRequestMessage message)
			{
				InvokeCallbacks(message);
			}
		}

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(HttpRequestMessage value)
		{
			if (_callbacks is not null)
			{
				foreach (Action<HttpRequestMessage> callback in _callbacks)
				{
					callback.Invoke(value);
				}
			}
		}

		bool IParameterMatch<HttpRequestMessage>.Matches(HttpRequestMessage value)
			=> Matches(value);

		/// <summary>
		///     Checks whether the given <see cref="HttpRequestMessage" /> <paramref name="value" /> matches the expectations.
		/// </summary>
		protected virtual bool Matches(HttpRequestMessage value)
		{
			if (_headers is not null &&
			    !_headers.Matches(value.Headers, value.Content?.Headers))
			{
				return false;
			}

			if (_uriParameter is not null &&
			    !_uriParameter.AsParameterMatch().Matches(value.RequestUri))
			{
				return false;
			}

			if (_contentParameter is not null &&
			    !_contentParameter.AsParameterMatch().Matches(value.Content))
			{
				return false;
			}

			return true;
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			List<string?> parts = new();
			if (_uriParameter is not null)
			{
				parts.Add(_uriParameter.ToString());
			}

			if (_contentParameter is not null)
			{
				parts.Add(_contentParameter.ToString());
			}

			if (_headers is not null)
			{
				parts.Add(_headers.ToString());
			}

			return string.Join(" and ", parts.Where(p => !string.IsNullOrEmpty(p)));
		}
	}

	private sealed class HttpRequestMessageParameter(HttpMethod? method)
		: HttpRequestMessageParameter<IHttpRequestMessageParameter>,
			IHttpRequestMessageParameter
	{
		/// <inheritdoc cref="HttpRequestMessageParameter{TParameter}.GetThis" />
		protected override IHttpRequestMessageParameter GetThis => this;

		protected override bool Matches(HttpRequestMessage value)
		{
			if (method is not null && value.Method != method)
			{
				return false;
			}

			return base.Matches(value);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string prefix = method is null ? "a Http-Request" : $"{method}-Request";
			string parameterString = base.ToString();
			if (string.IsNullOrEmpty(parameterString))
			{
				return prefix;
			}

			return $"{prefix} with {parameterString}";
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
