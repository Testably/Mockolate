using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Web;

/// <summary>
///     HTTP-aware parameter matchers for <see cref="It" /> - match against <see cref="Uri" />,
///     <see cref="System.Net.Http.HttpRequestMessage" /> and <see cref="System.Net.Http.HttpContent" /> values
///     when setting up or verifying a mocked <see cref="System.Net.Http.HttpClient" />.
/// </summary>
/// <remarks>
///     Used alongside <see cref="HttpClientExtensions" />. Entry-point matchers:
///     <list type="bullet">
///         <item>
///             <description><c>It.IsUri(...)</c> - match the request URI by exact string, glob pattern or predicate, optionally asserting required query parameters.</description>
///         </item>
///         <item>
///             <description><c>It.IsHttpRequestMessage(...)</c> - match the raw <see cref="System.Net.Http.HttpRequestMessage" />; chain <c>.WithHeaders(...)</c> to constrain headers.</description>
///         </item>
///         <item>
///             <description><c>It.IsHttpContent(...)</c> - match the request body; chain <c>.WithString(...)</c>, <c>.WithBytes(...)</c>, <c>.WithFormData(...)</c> or <c>.WithHeaders(...)</c> to constrain by payload shape.</description>
///         </item>
///     </list>
///     Query-parameter matching is URL-decoded, and a trailing <c>/</c> on the request URI is tolerated when
///     comparing against a matcher without it.
/// </remarks>
public static partial class ItExtensions
{
	private sealed class HttpQueryMatcher
	{
		private readonly List<(string Name, HttpQueryParameterValue Value)> _requiredQueryParameters = [];

		public void AddRequiredQueryParameter(string name, HttpQueryParameterValue value)
			=> _requiredQueryParameters.Add((name, value));

		public void AddRequiredQueryParameter(IEnumerable<(string Name, HttpQueryParameterValue Value)> queryParameters)
			=> _requiredQueryParameters.AddRange(queryParameters);

		public void AddRequiredQueryParameter(string queryParameters)
			=> _requiredQueryParameters.AddRange(
				ParseQueryParameters(queryParameters)
					.Select(pair => (pair.Key, new HttpQueryParameterValue(pair.Value))));

		public bool Matches(Uri uri)
		{
			List<(string Key, string Value)> queryParameters = ParseQueryParameters(uri.Query).ToList();
			foreach ((string Name, HttpQueryParameterValue Value) requiredParameter in _requiredQueryParameters)
			{
				if (!ContainsMatch(queryParameters, requiredParameter.Name, requiredParameter.Value))
				{
					return false;
				}
			}

			return true;
		}

		private static bool ContainsMatch(List<(string Key, string Value)> queryParameters, string name,
			HttpQueryParameterValue value)
		{
			foreach ((string Key, string Value) parameter in queryParameters)
			{
				if (parameter.Key == name && value.Matches(parameter.Value))
				{
					return true;
				}
			}

			return false;
		}

		private static IEnumerable<(string Key, string Value)> ParseQueryParameters(string input)
			=> input.TrimStart('?')
				.Split('&')
				.Select(pair => pair.Split('=', 2))
				.Where(pair => !string.IsNullOrWhiteSpace(pair[0]))
				.Select(pair =>
					(
						WebUtility.UrlDecode(pair[0]),
						pair.Length == 2 ? WebUtility.UrlDecode(pair[1]) : ""
					)
				);

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> string.Join(", ", _requiredQueryParameters.Select(p => $"\"{p.Name}={p.Value}\""));
	}
}
