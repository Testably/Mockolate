using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Web;

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="ItExtensions" />
	extension(It _)
	{
		/// <summary>
		///     Matches a <see cref="Uri" /> (or <see langword="string" /> URI) against an optional glob
		///     <paramref name="pattern" /> - pass <see langword="null" /> to accept any URI and narrow it further
		///     with fluent builders on the returned <see cref="IUriParameter" />.
		/// </summary>
		/// <param name="pattern">
		///     Glob pattern matched against <see cref="Uri.ToString" /> (<c>*</c> matches any sequence, <c>?</c>
		///     matches one character). A trailing <c>/</c> on the request URI is tolerated when the pattern omits
		///     it. Pass <see langword="null" /> to skip the pattern check and only rely on the fluent constraints.
		/// </param>
		/// <remarks>
		///     Chain additional constraints on the returned matcher:
		///     <list type="bullet">
		///         <item>
		///             <description><c>.ForHttp()</c> / <c>.ForHttps()</c> - require the scheme.</description>
		///         </item>
		///         <item>
		///             <description><c>.WithHost(hostPattern)</c> / <c>.WithPort(port)</c> / <c>.WithPath(pathPattern)</c> - constrain individual URI components (host and path accept wildcards; path is URL-decoded before matching).</description>
		///         </item>
		///         <item>
		///             <description><c>.WithQuery(key, value)</c> / <c>.WithQuery(queryString)</c> / <c>.WithQuery(parameters)</c> - require query parameters to be present; the order is ignored, values are URL-decoded.</description>
		///         </item>
		///     </list>
		/// </remarks>
		/// <returns>A fluent <see cref="Uri" /> matcher; pass it to <c>Setup.GetAsync</c>/<c>PostAsync</c>/... or <c>Verify</c>.</returns>
		public static IUriParameter IsUri(string? pattern = null)
			=> new UriParameter(pattern);
	}

	/// <summary>
	///     Additional constraints chained off <see cref="IsUri(string?)" /> to narrow a <see cref="Uri" /> match.
	/// </summary>
	/// <remarks>
	///     Every <c>.With*</c>/<c>.For*</c> returns the same instance so constraints can be combined; every one of
	///     them must hold for the URI to match.
	/// </remarks>
	public interface IUriParameter : IParameterWithCallback<Uri?>
	{
		/// <summary>
		///     Requires <see cref="Uri.Scheme" /> to be <c>http</c> (case-insensitive).
		/// </summary>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter ForHttp();

		/// <summary>
		///     Requires <see cref="Uri.Scheme" /> to be <c>https</c> (case-insensitive).
		/// </summary>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter ForHttps();

		/// <summary>
		///     Requires <see cref="Uri.Host" /> to match <paramref name="hostPattern" /> (<c>*</c> = any sequence,
		///     <c>?</c> = one character; case-insensitive).
		/// </summary>
		/// <param name="hostPattern">Wildcard pattern for the host part.</param>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter WithHost(string hostPattern);

		/// <summary>
		///     Requires <see cref="Uri.Port" /> to equal <paramref name="port" />.
		/// </summary>
		/// <param name="port">Expected port number (as exposed by <see cref="Uri.Port" />, so the default port for the scheme is <c>80</c>/<c>443</c>).</param>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter WithPort(int port);

		/// <summary>
		///     Requires <see cref="Uri.AbsolutePath" /> to match <paramref name="pathPattern" /> after URL-decoding
		///     (<c>*</c> = any sequence, <c>?</c> = one character; case-insensitive).
		/// </summary>
		/// <param name="pathPattern">Wildcard pattern for the absolute path (e.g. <c>"/api/users/*"</c>).</param>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter WithPath(string pathPattern);

		/// <summary>
		///     Requires <see cref="Uri.Query" /> to contain every parameter parsed from <paramref name="queryString" />.
		/// </summary>
		/// <param name="queryString">Raw query string &#8212; pairs separated by <c>&amp;</c>, keys and values by <c>=</c>. A leading <c>?</c> is tolerated.</param>
		/// <returns>The same parameter, for chaining.</returns>
		/// <remarks>
		///     The order of parameters is ignored, values are URL-decoded, and additional parameters on the URI are allowed.
		/// </remarks>
		IUriParameter WithQuery(string queryString);

		/// <summary>
		///     Requires <see cref="Uri.Query" /> to contain a parameter <paramref name="key" />=<paramref name="value" />.
		/// </summary>
		/// <param name="key">The query parameter name that must be present.</param>
		/// <param name="value">The expected value. Implicitly converts from <see langword="string" />; use the <see cref="HttpQueryParameterValue" /> helpers for patterns or predicates.</param>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter WithQuery(string key, HttpQueryParameterValue value);

		/// <summary>
		///     Requires <see cref="Uri.Query" /> to contain every given <paramref name="parameters" /> pair
		///     (additional query parameters on the URI are allowed).
		/// </summary>
		/// <param name="parameters">Expected key/value pairs; the order is ignored.</param>
		/// <returns>The same parameter, for chaining.</returns>
		IUriParameter WithQuery(params IEnumerable<(string Key, HttpQueryParameterValue Value)> parameters);
	}

	private sealed class UriParameter(string? pattern) : IUriParameter, IParameterMatch<Uri?>
	{
		private List<Action<Uri?>>? _callbacks;
		private string? _hostPattern;
		private string? _pathPattern;
		private int? _port;
		private HttpQueryMatcher? _query;
		private string? _scheme;

		/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
		void IParameterMatch<Uri?>.InvokeCallbacks(Uri? value)
		{
			if (_callbacks is not null)
			{
				foreach (Action<Uri?> callback in _callbacks)
				{
					callback.Invoke(value);
				}
			}
		}

		bool IParameterMatch<Uri?>.Matches(Uri? value)
			=> Matches(value);

		/// <inheritdoc cref="IParameter.Matches(object?)" />
		bool IParameter.Matches(object? value)
			=> value is Uri uri && Matches(uri);

		/// <inheritdoc cref="IParameter.InvokeCallbacks(object?)" />
		void IParameter.InvokeCallbacks(object? value)
		{
			if (value is Uri uri)
			{
				((IParameterMatch<Uri?>)this).InvokeCallbacks(uri);
			}
			else if (value is null)
			{
				((IParameterMatch<Uri?>)this).InvokeCallbacks(null);
			}
		}

		public IParameterWithCallback<Uri?> Do(Action<Uri?> callback)
		{
			_callbacks ??= [];
			_callbacks.Add(callback);
			return this;
		}

		public IUriParameter ForHttp()
		{
			_scheme = "http";
			return this;
		}

		public IUriParameter ForHttps()
		{
			_scheme = "https";
			return this;
		}

		public IUriParameter WithHost(string hostPattern)
		{
			_hostPattern = hostPattern;
			return this;
		}

		public IUriParameter WithPort(int port)
		{
			_port = port;
			return this;
		}

		public IUriParameter WithPath(string pathPattern)
		{
			_pathPattern = pathPattern;
			return this;
		}

		public IUriParameter WithQuery(string queryString)
		{
			_query ??= new HttpQueryMatcher();
			_query.AddRequiredQueryParameter(queryString);
			return this;
		}

		public IUriParameter WithQuery(string key, HttpQueryParameterValue value)
		{
			_query ??= new HttpQueryMatcher();
			_query.AddRequiredQueryParameter(key, value);
			return this;
		}

		public IUriParameter WithQuery(params IEnumerable<(string Key, HttpQueryParameterValue Value)> parameters)
		{
			_query ??= new HttpQueryMatcher();
			_query.AddRequiredQueryParameter(parameters);
			return this;
		}

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
		private bool Matches(Uri? uri)
		{
			if (uri is null)
			{
				return false;
			}

			if (pattern is not null &&
			    !Wildcard.Pattern(pattern, true, false).Matches(uri.ToString()))
			{
				return false;
			}

			if (_scheme is not null &&
			    !string.Equals(_scheme, uri.Scheme, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (_port is not null && _port != uri.Port)
			{
				return false;
			}

			if (_hostPattern is not null &&
			    !Wildcard.Pattern(_hostPattern, true).Matches(uri.Host))
			{
				return false;
			}

			if (_pathPattern is not null &&
			    !Wildcard.Pattern(_pathPattern, true).Matches(WebUtility.UrlDecode(uri.AbsolutePath)))
			{
				return false;
			}

			if (_query is not null && !_query.Matches(uri))
			{
				return false;
			}

			return true;
		}
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			StringBuilder sb = new();
			if (_scheme is not null)
			{
				sb.Append($"{_scheme} Uri");
			}
			else
			{
				sb.Append("Uri");
			}

			if (pattern is not null)
			{
				sb.Append(" matching \"").Append(pattern).Append('"');
			}

			if (_hostPattern is not null)
			{
				sb.Append(" with host matching \"").Append(_hostPattern).Append('"');
			}

			if (_port is not null)
			{
				sb.Append(" with port ").Append(_port.Value);
			}

			if (_pathPattern is not null)
			{
				sb.Append(" with path matching \"").Append(_pathPattern).Append('"');
			}

			if (_query is not null)
			{
				sb.Append(" with query containing ").Append(_query);
			}

			string result = sb.ToString();
			return result == "Uri" ? "any Uri" : result;
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
