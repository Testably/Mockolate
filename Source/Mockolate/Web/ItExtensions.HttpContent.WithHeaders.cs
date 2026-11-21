using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Web;

/// <summary>
///     Extensions for parameter matchers for HTTP-related types.
/// </summary>
public static partial class ItExtensions
{
	/// <inheritdoc cref="IHttpHeaderParameter{TParameter}" />
	extension<TParameter>(IHttpHeaderParameter<TParameter> parameter)
	{
		/// <summary>
		///     Expects the <see cref="HttpContent" /> to carry a header <paramref name="name" />: <paramref name="value" />.
		/// </summary>
		/// <param name="name">The header name to require (case-insensitive per RFC 7230).</param>
		/// <param name="value">The expected value. Implicitly converts from <see langword="string" /> or can be a subclass of <see cref="HttpHeaderValue" /> for pattern matching.</param>
		/// <returns>The outer parameter for chaining additional <c>.With*</c> constraints.</returns>
		public TParameter WithHeaders(string name, HttpHeaderValue value)
			=> parameter.WithHeaders((name, value));

		/// <summary>
		///     Expects the <see cref="HttpContent" /> to carry every header parsed from <paramref name="headers" />
		///     &#8212; one per line in <c>"Name: Value"</c> form.
		/// </summary>
		/// <param name="headers">Raw header block; blank lines terminate parsing.</param>
		/// <returns>The outer parameter for chaining additional <c>.With*</c> constraints.</returns>
		/// <exception cref="ArgumentException">A non-empty line does not contain a <c>:</c> separator.</exception>
		public TParameter WithHeaders(string headers)
		{
			List<(string, HttpHeaderValue)> headerList = new();
			using StringReader reader = new(headers);
			string? line = reader.ReadLine();
			while (!string.IsNullOrWhiteSpace(line))
			{
				string[] parts = line.Split(':', 2);

				if (parts.Length != 2)
				{
					// ReSharper disable once LocalizableElement
					throw new ArgumentException("The header contained an invalid line: " + line, nameof(headers));
				}

				headerList.Add((parts[0].Trim(), parts[1].TrimStart(' ')));
				line = reader.ReadLine();
			}

			return parameter.WithHeaders(headerList);
		}
	}

	/// <summary>
	///     Fluent entry point for asserting HTTP headers on a request or response.
	/// </summary>
	/// <typeparam name="TParameter">The concrete parameter type returned for further chaining.</typeparam>
	public interface IHttpHeaderParameter<out TParameter>
	{
		/// <summary>
		///     Expects the <see cref="HttpContent" /> to carry every header in <paramref name="headers" />.
		/// </summary>
		/// <param name="headers">Pairs of header name and expected value. Additional headers on the request are allowed.</param>
		/// <returns>The outer parameter for chaining additional <c>.With*</c> constraints.</returns>
		TParameter WithHeaders(params IEnumerable<(string Name, HttpHeaderValue Value)> headers);
	}

	private sealed class HttpHeadersMatcher
	{
		private readonly List<(string Name, HttpHeaderValue Value)> _requiredHeaders = [];

		public bool IncludeRequestHeaders { get; private set; }

		public void AddRequiredHeader(IEnumerable<(string Name, HttpHeaderValue Value)> headers)
			=> _requiredHeaders.AddRange(headers);

		public bool Matches(HttpHeaders messageHeaders, HttpHeaders? alternativeHeaders = null)
		{
			foreach ((string Name, HttpHeaderValue Value) header in _requiredHeaders)
			{
				if (!MatchesHeader(header.Name, header.Value, messageHeaders) &&
				    (alternativeHeaders is null ||
				     !MatchesHeader(header.Name, header.Value, alternativeHeaders)))
				{
					return false;
				}
			}

			return true;
		}

		private static bool MatchesHeader(string name, HttpHeaderValue value, HttpHeaders messageHeaders)
		{
			if (!messageHeaders.TryGetValues(name, out IEnumerable<string>? values))
			{
				return false;
			}

			foreach (string headerValue in values)
			{
				if (value.Matches(headerValue))
				{
					return true;
				}
			}

			return false;
		}

		public void IncludingRequestHeaders()
			=> IncludeRequestHeaders = true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> (_requiredHeaders.Count == 1 ? "header " : "headers ")
			   + string.Join(", ", _requiredHeaders.Select(header => $"\"{header.Name}: {header.Value}\""));
	}
}
