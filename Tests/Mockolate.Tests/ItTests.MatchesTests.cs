using System.Text.RegularExpressions;
using aweXpect.Chronology;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class MatchesTests
	{
		[Test]
		[Arguments("foo", "F[aeiou]+o", 0)]
		[Arguments("foo", "f[aeiou]+o", 1)]
		[Arguments("foobar", "f[aeiou]*BAR", 0)]
		[Arguments("foobar", "f[aeiou]*bar", 1)]
		public async Task AsRegex_CaseSensitive_ShouldMatchRegexCaseSensitive(
			string value, string regex, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithString(value);

			await That(sut.Mock.Verify.DoSomethingWithString(It.Matches(regex).AsRegex().CaseSensitive()))
				.Exactly(expectedCount);
		}

		[Test]
		[Arguments("foo", "g[aeiou]+o", 0)]
		[Arguments("foo", "F[aeiou]+o", 1)]
		[Arguments("foobar", "f[aeiou]*baz", 0)]
		[Arguments("foobar", "f[aeiou]*BAR", 1)]
		public async Task AsRegex_ShouldMatchRegexCaseInsensitive(string value, string regex, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithString(value);

			await That(sut.Mock.Verify.DoSomethingWithString(It.Matches(regex).AsRegex()))
				.Exactly(expectedCount);
		}

		[Test]
		public async Task AsRegex_WithRegexOptions_ShouldUseRegexOption()
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithString("foo");

			await That(sut.Mock.Verify.DoSomethingWithString(
					It.Matches("F[aeiou]+o").AsRegex(RegexOptions.IgnoreCase)))
				.Exactly(1);
		}

		[Test]
		public async Task AsRegex_WithTimeout_ShouldApplyTimeoutToRegex()
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();
			sut.DoSomethingWithString("foo");

			void Act()
			{
				sut.Mock.Verify.DoSomethingWithString(
					It.Matches("F[aeiou]+o").AsRegex(timeout: 0.Seconds())).AtLeastOnce();
			}

			await That(Act)
				.Throws<ArgumentOutOfRangeException>()
				.WithParamName("matchTimeout");
		}

		[Test]
		[Arguments("foo", "F?o", 0)]
		[Arguments("foo", "f?o", 1)]
		[Arguments("foobar", "f*BAR", 0)]
		[Arguments("foobar", "f*bar", 1)]
		[Arguments("foobar", "f?bar", 0)]
		[Arguments("foobar", "f??bar", 1)]
		public async Task CaseSensitive_ShouldMatchWildcardCaseSensitive(
			string value, string wildcard, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithString(value);

			await That(sut.Mock.Verify.DoSomethingWithString(It.Matches(wildcard).CaseSensitive()))
				.Exactly(expectedCount);
		}

		[Test]
		public async Task ShouldFreezeValuesOnFirstMatch()
		{
			It.IParameterMatches match = It.Matches("F*o");
			match.CaseSensitive();
			IParameterMatch<string> parameter = (IParameterMatch<string>)match;

			bool result1 = parameter.Matches("foo");

			match.CaseSensitive(false);

			bool result2 = parameter.Matches("foo");

			await That(result1).IsFalse();
			await That(result2).IsFalse();
		}

		[Test]
		[Arguments("foo", "g?o", 0)]
		[Arguments("foo", "F?o", 1)]
		[Arguments("foobar", "f*baz", 0)]
		[Arguments("foobar", "f*BAR", 1)]
		[Arguments("foobar", "f??baz", 0)]
		[Arguments("foobar", "f??bar", 1)]
		public async Task ShouldMatchWildcardCaseInsensitive(string value, string wildcard, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithString(value);

			await That(sut.Mock.Verify.DoSomethingWithString(It.Matches(wildcard))).Exactly(expectedCount);
		}

		[Test]
		public async Task ToString_AsRegex_CaseSensitive_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F\"[aeiou]+o").AsRegex().CaseSensitive();
			string expectedValue = "It.Matches(\"F\\\"[aeiou]+o\").AsRegex().CaseSensitive()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_AsRegex_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F\"[aeiou]+o").AsRegex();
			string expectedValue = "It.Matches(\"F\\\"[aeiou]+o\").AsRegex()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_AsRegex_WithInfiniteMatchTimeout_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F[aeiou]+o").AsRegex(timeout: Regex.InfiniteMatchTimeout);
			string expectedValue = "It.Matches(\"F[aeiou]+o\").AsRegex()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_AsRegex_WithOptions_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F[aeiou]+o").AsRegex(RegexOptions.Compiled | RegexOptions.ECMAScript);
			string expectedValue =
				"It.Matches(\"F[aeiou]+o\").AsRegex(RegexOptions.Compiled | RegexOptions.ECMAScript)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_AsRegex_WithOptionsAndTimeout_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F[aeiou]+o")
				.AsRegex(RegexOptions.Compiled, 400.Milliseconds());
			string expectedValue =
				"It.Matches(\"F[aeiou]+o\").AsRegex(RegexOptions.Compiled, 400.Milliseconds())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_AsRegex_WithTimeout_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("F[aeiou]+o").AsRegex(timeout: 2.Seconds());
			string expectedValue = "It.Matches(\"F[aeiou]+o\").AsRegex(timeout: 2.Seconds())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_CaseSensitive_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("f*\"oo").CaseSensitive();
			string expectedValue = "It.Matches(\"f*\\\"oo\").CaseSensitive()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Matches("f*\"oo");
			string expectedValue = "It.Matches(\"f*\\\"oo\")";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
