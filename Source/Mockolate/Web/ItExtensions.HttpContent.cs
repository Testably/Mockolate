using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using Mockolate.Parameters;

namespace Mockolate.Web;

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="ItExtensions" />
	extension(It _)
	{
		/// <summary>
		///     Matches any non-<see langword="null" /> <see cref="HttpContent" /> on a mocked
		///     <see cref="System.Net.Http.HttpClient" /> setup or verification.
		/// </summary>
		/// <remarks>
		///     Narrow the match by chaining builders on the returned <see cref="IHttpContentParameter" />:
		///     <list type="bullet">
		///         <item>
		///             <description><c>.WithString(predicate)</c> - match the request body as a string (UTF-8 unless the <c>Content-Type</c> header specifies a charset).</description>
		///         </item>
		///         <item>
		///             <description><c>.WithBytes(predicate)</c> - match the raw byte payload.</description>
		///         </item>
		///         <item>
		///             <description><c>.WithMediaType(mediaType)</c> - constrain the <c>Content-Type</c> media type.</description>
		///         </item>
		///         <item>
		///             <description><c>.WithHeaders(...)</c> - require specific content headers; chain <c>.IncludingRequestHeaders()</c> to also consider <see cref="HttpRequestMessage.Headers" />.</description>
		///         </item>
		///     </list>
		///     A <see langword="null" /> body never matches.
		/// </remarks>
		/// <returns>A fluent matcher; pass it to <c>Setup.PostAsync</c>/<c>PutAsync</c>/<c>PatchAsync</c> or <c>Verify</c>.</returns>
		public static IHttpContentParameter IsHttpContent()
			=> new HttpContentParameter();

		/// <summary>
		///     Matches any non-<see langword="null" /> <see cref="HttpContent" /> whose <c>Content-Type</c> media type
		///     equals <paramref name="mediaType" /> (case-insensitive).
		/// </summary>
		/// <param name="mediaType">The expected media type, e.g. <c>"application/json"</c> or <c>"text/plain"</c>.</param>
		/// <remarks>
		///     Shorthand for <c>It.IsHttpContent().WithMediaType(mediaType)</c>. Chain additional builders
		///     (<c>.WithString(...)</c>, <c>.WithBytes(...)</c>, <c>.WithHeaders(...)</c>) to further narrow the match.
		/// </remarks>
		/// <returns>A fluent matcher pre-configured to require the given media type.</returns>
		public static IHttpContentParameter IsHttpContent(string mediaType)
			=> new HttpContentParameter().WithMediaType(mediaType);
	}

	/// <summary>
	///     Further expectations on the headers of the <see cref="HttpContent" />.
	/// </summary>
	public interface IHttpContentHeaderParameter : IHttpContentParameter
	{
		/// <summary>
		///     If set, also includes the headers from the <see cref="HttpRequestMessage" /> when matching the headers.
		/// </summary>
		IHttpContentParameter IncludingRequestHeaders();
	}

	/// <summary>
	///     Further expectations on the <see cref="HttpContent" />.
	/// </summary>
	public interface IHttpContentParameter : IParameterWithCallback<HttpContent?>, IHttpHeaderParameter<IHttpContentHeaderParameter>
	{
		/// <summary>
		///     Expects the content to have a string body that satisfies the <paramref name="predicate" />.
		/// </summary>
		IHttpContentParameter WithString(Func<string, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "");

		/// <summary>
		///     Expects the binary content to satisfy the <paramref name="predicate" />.
		/// </summary>
		IHttpContentParameter WithBytes(Func<byte[], bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "");

		/// <summary>
		///     Expects the <see cref="HttpContent" /> to have the given <paramref name="mediaType" />.
		/// </summary>
		IHttpContentParameter WithMediaType(string? mediaType);
	}

	private sealed class HttpContentParameter
		: IHttpContentHeaderParameter, IHttpRequestMessagePropertyParameter<HttpContent?>, IParameterMatch<HttpContent?>
	{
		private BinaryMatcher? _binaryContentMatcher;
		private List<Action<HttpContent?>>? _callbacks;
		private HttpHeadersMatcher? _headers;
		private string? _mediaType;
		private PredicateStringMatcher? _stringContentMatcher;

		public IHttpContentParameter IncludingRequestHeaders()
		{
			_headers!.IncludingRequestHeaders();
			return this;
		}

		public IHttpContentParameter WithString(Func<string, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "")
		{
			_stringContentMatcher ??= new PredicateStringMatcher();
			_stringContentMatcher.AddPredicate(predicate, doNotPopulateThisValue);
			return this;
		}

		public IHttpContentParameter WithBytes(Func<byte[], bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "")
		{
			_binaryContentMatcher ??= new BinaryMatcher(predicate, doNotPopulateThisValue);
			return this;
		}

		/// <inheritdoc cref="IHttpContentParameter.WithMediaType(string?)" />
		public IHttpContentParameter WithMediaType(string? mediaType)
		{
			_mediaType = mediaType;
			return this;
		}

		public IHttpContentHeaderParameter WithHeaders(params IEnumerable<(string Name, HttpHeaderValue Value)> headers)
		{
			_headers ??= new HttpHeadersMatcher();
			_headers.AddRequiredHeader(headers);
			return this;
		}

		/// <inheritdoc cref="IParameterWithCallback{T}.Do(Action{T})" />
		public IParameterWithCallback<HttpContent?> Do(Action<HttpContent?> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		/// <inheritdoc cref="IParameter.Matches(object?)" />
		bool IParameter.Matches(object? value)
			=> value is HttpContent content && Matches(content);

		/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
		void IParameter.InvokeCallbacks(object? value)
		{
			if (value is HttpContent content)
			{
				InvokeCallbacks(content);
			}
			else if (value is null)
			{
				InvokeCallbacks(null);
			}
		}

		/// <inheritdoc cref="IHttpRequestMessagePropertyParameter{HttpContent}.Matches(HttpContent, HttpRequestMessage)" />
		public bool Matches(HttpContent? value, HttpRequestMessage? requestMessage)
		{
			if (value is null)
			{
				return false;
			}

			if (_mediaType is not null &&
			    value.Headers.ContentType?.MediaType?.Equals(_mediaType, StringComparison.OrdinalIgnoreCase) != true)
			{
				return false;
			}

			if (_headers is not null &&
			    !_headers.Matches(value.Headers, _headers.IncludeRequestHeaders ? requestMessage?.Headers : null))
			{
				return false;
			}

			if (_stringContentMatcher is not null &&
			    !_stringContentMatcher.Matches(value, requestMessage))
			{
				return false;
			}

			if (_binaryContentMatcher is not null &&
			    !_binaryContentMatcher.Matches(value, requestMessage))
			{
				return false;
			}

			return true;
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(HttpContent? value)
			=> Matches(value, null);

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(HttpContent? value)
		{
			if (_callbacks is not null)
			{
				_callbacks.ForEach(a => a.Invoke(value));
			}
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			StringBuilder sb = new();
			if (_stringContentMatcher is not null)
			{
				sb.Append("string content ").Append(_stringContentMatcher);
			}

			if (_binaryContentMatcher is not null)
			{
				if (_stringContentMatcher is not null)
				{
					sb.Append(" and ");
				}

				sb.Append("binary content ").Append(_binaryContentMatcher);
			}
			else if (_stringContentMatcher is null)
			{
				sb.Append("Http content");
			}

			AppendAdditionalDescription(sb);

			return sb.ToString();
		}

		internal void AppendAdditionalDescription(StringBuilder sb)
		{
			if (_mediaType is not null)
			{
				sb.Append(" with media type \"").Append(_mediaType).Append('"');
			}

			if (_headers is not null)
			{
				sb.Append(" with ").Append(_headers);
			}
		}

		private interface IContentMatcher
		{
			bool Matches(HttpContent content, HttpRequestMessage? message);
		}

		private sealed class PredicateStringMatcher : IContentMatcher
		{
			private readonly List<(Func<string, bool> Predicate, string Description)> _predicates = [];

			public bool Matches(HttpContent content, HttpRequestMessage? message)
			{
				static Encoding GetEncodingFromCharset(string? charset)
				{
					if (!string.IsNullOrEmpty(charset))
					{
						try
						{
							return Encoding.GetEncoding(charset);
						}
						catch (ArgumentException)
						{
							// If the charset is invalid or not supported, we fall back to the default encoding (UTF-8).
						}
					}

					return Encoding.UTF8;
				}

				string? charset = content.Headers.ContentType?.CharSet;
				Encoding encoding = GetEncodingFromCharset(charset);
#if NET8_0_OR_GREATER
				Stream stream = content.ReadAsStream();
				string stringContent;
				// HttpContent caches its read stream, so concurrent matchers (e.g. a setup and a verification) share it.
				lock (stream)
				{
					long position = stream.Position;
					using StreamReader reader = new(stream, encoding, leaveOpen: true);
					stringContent = reader.ReadToEnd();
					stream.Position = position;
				}
#else
				string stringContent;
				if (message?.Properties.TryGetValue("Mockolate:HttpContent", out object value) == true && value is byte[] bytes)
				{
					stringContent = encoding.GetString(bytes);
				}
				else
				{
					Stream stream = content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
					lock (stream)
					{
						long position = stream.Position;
						using StreamReader reader = new(stream, encoding, true, 1024, true);
						stringContent = reader.ReadToEnd();
						stream.Position = position;
					}
				}
#endif
				return _predicates.All(predicate => predicate.Predicate.Invoke(stringContent));
			}

			public void AddPredicate(Func<string, bool> predicate, string predicateExpression)
				=> _predicates.Add((predicate, predicateExpression));

			/// <inheritdoc cref="object.ToString()" />
			public override string ToString()
				=> string.Join(" and ", _predicates.Select(p => p.Description));
		}

		private sealed class BinaryMatcher(Func<byte[], bool> predicate, string predicateExpression) : IContentMatcher
		{
			public bool Matches(HttpContent content, HttpRequestMessage? message)
			{
#if NET8_0_OR_GREATER
				Stream stream = content.ReadAsStream();
				byte[] bytes;
				// HttpContent caches its read stream, so concurrent matchers (e.g. a setup and a verification) share it.
				lock (stream)
				{
					long position = stream.Position;
					using MemoryStream ms = new();
					stream.CopyTo(ms);
					bytes = ms.ToArray();
					stream.Position = position;
				}
#else
				byte[] bytes;
				if (message?.Properties.TryGetValue("Mockolate:HttpContent", out object value) == true
				    && value is byte[] b)
				{
					bytes = b;
				}
				else
				{
					Stream stream = content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
					lock (stream)
					{
						long position = stream.Position;
						using MemoryStream ms = new();
						stream.CopyTo(ms);
						bytes = ms.ToArray();
						stream.Position = position;
					}
				}
#endif
				return predicate.Invoke(bytes);
			}

			/// <inheritdoc cref="object.ToString()" />
			public override string ToString()
				=> predicateExpression;
		}
	}

	/// <summary>
	///     An abstract wrapper base class for <see cref="IHttpContentParameter" />.
	/// </summary>
	public abstract class HttpContentParameterWrapper(IHttpContentParameter parameter, Func<string> parameterString) : IHttpContentParameter,
		IHttpRequestMessagePropertyParameter<HttpContent?>, IParameterMatch<HttpContent?>
	{
		/// <inheritdoc cref="IParameterWithCallback{T}.Do(Action{T})" />
		public IParameterWithCallback<HttpContent?> Do(Action<HttpContent?> callback)
			=> parameter.Do(callback);

		/// <inheritdoc cref="IHttpHeaderParameter{T}.WithHeaders" />
		public IHttpContentHeaderParameter WithHeaders(
			params IEnumerable<(string Name, HttpHeaderValue Value)> headers)
			=> parameter.WithHeaders(headers);

		/// <inheritdoc cref="IHttpContentParameter.WithString(Func{string, bool}, string)" />
		public IHttpContentParameter WithString(Func<string, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "")
			=> parameter.WithString(predicate, doNotPopulateThisValue);

		/// <inheritdoc cref="IHttpContentParameter.WithBytes(Func{byte[], bool}, string)" />
		public IHttpContentParameter WithBytes(Func<byte[], bool> predicate, [CallerArgumentExpression(nameof(predicate))] string doNotPopulateThisValue = "")
			=> parameter.WithBytes(predicate, doNotPopulateThisValue);

		/// <inheritdoc cref="IHttpContentParameter.WithMediaType(string)" />
		public IHttpContentParameter WithMediaType(string? mediaType)
			=> parameter.WithMediaType(mediaType);

		/// <inheritdoc cref="IParameter.Matches(object?)" />
		bool IParameter.Matches(object? value)
			=> parameter.Matches(value);

		/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
		void IParameter.InvokeCallbacks(object? value)
			=> parameter.InvokeCallbacks(value);

		bool IHttpRequestMessagePropertyParameter<HttpContent?>.Matches(HttpContent? value,
			HttpRequestMessage? requestMessage)
		{
			if (parameter is IHttpRequestMessagePropertyParameter<HttpContent?> requestMessagePropertyParameter)
			{
				return requestMessagePropertyParameter.Matches(value, requestMessage);
			}

			return Matches(value);
		}

		/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
		public bool Matches(HttpContent? value)
			=> parameter.AsParameterMatch().Matches(value);

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		public void InvokeCallbacks(HttpContent? value)
			=> parameter.AsParameterMatch().InvokeCallbacks(value);

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> parameterString();
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
