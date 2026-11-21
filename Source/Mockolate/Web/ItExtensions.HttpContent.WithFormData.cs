using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Web;

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="IHttpContentParameter" />
	extension(IHttpContentParameter parameter)
	{
		/// <summary>
		///     Expects the form-data body to contain the pair <paramref name="key" />=<paramref name="value" />.
		/// </summary>
		/// <param name="key">The form-data key that must be present.</param>
		/// <param name="value">The expected value for <paramref name="key" />. Implicitly converts from <see langword="string" />.</param>
		/// <returns>A <see cref="IFormDataContentParameter" /> that can be narrowed further or tightened with <see cref="IFormDataContentParameter.Exactly" />.</returns>
		/// <remarks>
		///     By default only the listed pairs must appear; the body may contain extras. Chain
		///     <see cref="IFormDataContentParameter.Exactly" /> to reject any additional pair.
		/// </remarks>
		public IFormDataContentParameter WithFormData(string key, HttpFormDataValue value)
			=> parameter.WithFormData((key, value)!);

		/// <summary>
		///     Expects the form-data body to contain all given <paramref name="values" /> (additional pairs are allowed).
		/// </summary>
		/// <param name="values">The expected key/value pairs; each <see cref="HttpFormDataValue" /> can be a literal, a pattern or a predicate.</param>
		/// <returns>A <see cref="IFormDataContentParameter" /> that can be narrowed further or tightened with <see cref="IFormDataContentParameter.Exactly" />.</returns>
		/// <remarks>
		///     Chain <see cref="IFormDataContentParameter.Exactly" /> to require that no other keys are present. Useful
		///     for matching <c>application/x-www-form-urlencoded</c> and <c>multipart/form-data</c> request bodies.
		/// </remarks>
		public IFormDataContentParameter WithFormData(params IEnumerable<(string Key, HttpFormDataValue Value)> values)
		{
			FormDataMatcher data = new(values);
			FormDataContentParameter contentParameter = new(parameter, data);
			return contentParameter;
		}

		/// <summary>
		///     Expects the form-data body to contain all pairs parsed from the URL-encoded <paramref name="values" />
		///     string (e.g. <c>"a=1&amp;b=2"</c>).
		/// </summary>
		/// <param name="values">A URL-encoded key/value string; a leading <c>?</c> is stripped. Keys and values are URL-decoded.</param>
		/// <returns>A <see cref="IFormDataContentParameter" /> that can be narrowed further or tightened with <see cref="IFormDataContentParameter.Exactly" />.</returns>
		public IFormDataContentParameter WithFormData(string values)
			=> parameter.WithFormData(FormDataMatcher.ParseFormDataParameters(values)
				.Select(pair => (pair.Key, new HttpFormDataValue(pair.Value))));
	}

	/// <summary>
	///     Further expectations on a form-data <see cref="HttpContent" /> body.
	/// </summary>
	public interface IFormDataContentParameter : IHttpContentParameter
	{
		/// <summary>
		///     Requires the body to contain <em>exactly</em> the previously specified pairs &#8212; any additional
		///     key/value pair causes the match to fail.
		/// </summary>
		/// <returns>The same <see cref="IFormDataContentParameter" />, for chaining.</returns>
		IFormDataContentParameter Exactly();
	}

	private sealed class FormDataMatcher
	{
		private readonly List<(string Name, HttpFormDataValue Value)> _requiredFormDataParameters = [];
		private bool _isExactly;

		public FormDataMatcher(IEnumerable<(string Name, HttpFormDataValue Value)> formDataParameters)
		{
			_requiredFormDataParameters.AddRange(formDataParameters);
		}

		public bool Matches(string content)
		{
			List<(string Key, string Value)> formDataParameters = GetFormData(content).ToList();
			foreach ((string Name, HttpFormDataValue Value) requiredParameter in _requiredFormDataParameters)
			{
				if (!ContainsMatch(formDataParameters, requiredParameter.Name, requiredParameter.Value))
				{
					return false;
				}
			}

			if (_isExactly)
			{
				foreach ((string Key, string Value) parameter in formDataParameters)
				{
					if (!IsRequired(parameter.Key, parameter.Value))
					{
						return false;
					}
				}
			}

			return true;
		}

		private static bool ContainsMatch(List<(string Key, string Value)> formDataParameters, string name,
			HttpFormDataValue value)
		{
			foreach ((string Key, string Value) parameter in formDataParameters)
			{
				if (parameter.Key == name && value.Matches(parameter.Value))
				{
					return true;
				}
			}

			return false;
		}

		private bool IsRequired(string key, string value)
		{
			foreach ((string Name, HttpFormDataValue Value) requiredParameter in _requiredFormDataParameters)
			{
				if (key == requiredParameter.Name && requiredParameter.Value.Matches(value))
				{
					return true;
				}
			}

			return false;
		}

		public void Exactly()
			=> _isExactly = true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string parameters = string.Join(", ", _requiredFormDataParameters.Select(p => $"\"{p.Name}={p.Value}\""));
			return _isExactly ? $"exactly {parameters}" : parameters;
		}

		internal static IEnumerable<(string Key, string Value)> ParseFormDataParameters(string input)
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

		private static IEnumerable<(string, string)> GetFormData(string rawContent)
		{
			string rawFormData = ExtractFormDataFromMultipartContent(rawContent);
			return ParseFormDataParameters(rawFormData);
		}

		private static string ExtractFormDataFromMultipartContent(string rawContent)
		{
			string[] lines = rawContent.Split('\n');
			StringBuilder sb = new();
			foreach (string line in lines)
			{
				if (line.StartsWith("--") ||
				    line.StartsWith("Content-Type: ", StringComparison.OrdinalIgnoreCase) ||
				    line.StartsWith("Content-Disposition: ", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (!string.IsNullOrWhiteSpace(line))
				{
					sb.AppendLine(line);
				}
			}

			return sb.ToString().Trim();
		}
	}

	private sealed class FormDataContentParameter : HttpContentParameterWrapper, IFormDataContentParameter
	{
		private readonly FormDataMatcher _data;

		public FormDataContentParameter(IHttpContentParameter parameter, FormDataMatcher data)
			: base(parameter, () => $"form data content {data}")
		{
			_data = data;
			parameter.WithString(data.Matches);
		}

		public IFormDataContentParameter Exactly()
		{
			_data.Exactly();
			return this;
		}
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
