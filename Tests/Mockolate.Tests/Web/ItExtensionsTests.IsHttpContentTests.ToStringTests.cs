using System.Text.RegularExpressions;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class ToStringTests
		{
			[Test]
			public async Task WithoutConditions_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent();

				string? result = sut.ToString();

				await That(result).IsEqualTo("Http content");
			}

			[Test]
			public async Task WithMultipleConditions_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent("image/png")
					.WithString(c => c.Length > 0)
					.WithBytes(b => b.Length > 0);

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"string content c => c.Length > 0 and binary content b => b.Length > 0 with media type \"image/png\"");
			}

			[Test]
			public async Task WithBytesPredicate_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent().WithBytes(b => b.Length > 0);

				string? result = sut.ToString();

				await That(result).IsEqualTo("binary content b => b.Length > 0");
			}

			[Test]
			public async Task WithFormDataExactly_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData("x", "123").Exactly();

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content exactly \"x=123\"");
			}

			[Test]
			public async Task WithFormDataKeyAndValue_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData("x", "123");

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content \"x=123\"");
			}

			[Test]
			public async Task WithMultipleFormDataValuesExactly_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData(("x", "123"), ("y", "234")).Exactly();

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content exactly \"x=123\", \"y=234\"");
			}

			[Test]
			public async Task WithMultipleFormDataValues_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData(("x", "123"), ("y", "234"));

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content \"x=123\", \"y=234\"");
			}

			[Test]
			public async Task WithFormDataString_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData("x=123");

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content \"x=123\"");
			}

			[Test]
			public async Task WithFormDataStringOfMultipleValues_ShouldDescribeTheMatcher()
			{
				ItExtensions.IFormDataContentParameter sut =
					It.IsHttpContent().WithFormData("x=123&y=234");

				string? result = sut.ToString();

				await That(result).IsEqualTo("form data content \"x=123\", \"y=234\"");
			}

			[Test]
			public async Task WithMultipleHeaders_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut =
					It.IsHttpContent()
						.WithHeaders(("x-my-header", "my-value"), ("x-my-other-header", "my-other-value"));

				string? result = sut.ToString();

				await That(result)
					.IsEqualTo(
						"Http content with headers \"x-my-header: my-value\", \"x-my-other-header: my-other-value\"");
			}

			[Test]
			public async Task WithHeaders_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut =
					It.IsHttpContent().WithHeaders(("x-my-header", "my-value"));

				string? result = sut.ToString();

				await That(result).IsEqualTo("Http content with header \"x-my-header: my-value\"");
			}

			[Test]
			public async Task WithMediaType_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent().WithMediaType("text/plain");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Http content with media type \"text/plain\"");
			}

			[Test]
			public async Task WithMediaTypeAndString_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent("text/plain").WithString("foo");

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"string content containing \"foo\" with media type \"text/plain\"");
			}

			[Test]
			public async Task WithMediaTypeViaFactory_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent("application/json");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Http content with media type \"application/json\"");
			}

			[Test]
			public async Task WithStringExactlyAndIgnoringCase_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut = It.IsHttpContent().WithString("foo").Exactly()
					.IgnoringCase();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content equal to \"foo\" ignoring case");
			}

			[Test]
			public async Task WithStringExactly_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut = It.IsHttpContent().WithString("foo").Exactly();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content equal to \"foo\"");
			}

			[Test]
			public async Task WithStringAndIgnoringCase_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithString("foo").IgnoringCase();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content containing \"foo\" ignoring case");
			}

			[Test]
			public async Task WithString_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut = It.IsHttpContent().WithString("foo");

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content containing \"foo\"");
			}

			[Test]
			public async Task WithRegexAndIgnoringCase_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithStringMatching("^foo").AsRegex().IgnoringCase();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content matching regex pattern \"^foo\" ignoring case");
			}

			[Test]
			public async Task WithRegexOptionsAndIgnoringCase_ShouldIncludeTheOptions()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithStringMatching("^foo").AsRegex(RegexOptions.Multiline).IgnoringCase();

				string? result = sut.ToString();

				await That(result)
					.IsEqualTo("string content matching regex pattern \"^foo\" ignoring case with options Multiline");
			}

			[Test]
			public async Task WithRegex_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithStringMatching("^foo").AsRegex();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content matching regex pattern \"^foo\"");
			}

			[Test]
			public async Task WithRegexOptions_ShouldIncludeTheOptions()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithStringMatching("^foo").AsRegex(RegexOptions.Multiline);

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content matching regex pattern \"^foo\" with options Multiline");
			}

			[Test]
			public async Task WithStringMatchingAndIgnoringCase_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyParameter sut =
					It.IsHttpContent().WithStringMatching("*foo*").IgnoringCase();

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content matching pattern \"*foo*\" ignoring case");
			}

			[Test]
			public async Task WithStringMatching_ShouldDescribeTheMatcher()
			{
				ItExtensions.IStringContentBodyMatchingParameter sut =
					It.IsHttpContent().WithStringMatching("*foo*");

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content matching pattern \"*foo*\"");
			}

			[Test]
			public async Task WithStringPredicate_ShouldDescribeTheMatcher()
			{
				ItExtensions.IHttpContentParameter sut = It.IsHttpContent().WithString(c => c.Length > 0);

				string? result = sut.ToString();

				await That(result).IsEqualTo("string content c => c.Length > 0");
			}
		}
	}
}
