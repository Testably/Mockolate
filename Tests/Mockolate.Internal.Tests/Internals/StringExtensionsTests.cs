using Mockolate.Internals;

namespace Mockolate.Internal.Tests.Internals;

public sealed class StringExtensionsTests
{
	[Test]
	public async Task SubstringAfterLast_WhenNameContainsMultipleDots_ShouldUseLastOccurrence()
	{
		string value = "foo.bar.baz";

		string result = value.SubstringAfterLast('.');

		await That(result).IsEqualTo("baz");
	}

	[Test]
	public async Task SubstringAfterLast_WhenNameDoesNotContainAnyDots_ShouldReturnFullString()
	{
		string value = "foo-bar";

		string result = value.SubstringAfterLast('.');

		await That(result).IsEqualTo("foo-bar");
	}

	[Test]
	public async Task SubstringAfterLast_WhenNameEndsWithDot_ShouldReturnEmptyString()
	{
		string value = "foo.";

		string result = value.SubstringAfterLast('.');

		await That(result).IsEqualTo("");
	}

	[Test]
	public async Task SubstringUntilFirst_WhenNameContainsMultipleDots_ShouldUseFirstOccurrence()
	{
		string value = "foo.bar.baz";

		string result = value.SubstringUntilFirst('.');

		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task SubstringUntilFirst_WhenNameDoesNotContainAnyDots_ShouldReturnFullString()
	{
		string value = "foo-bar";

		string result = value.SubstringUntilFirst('.');

		await That(result).IsEqualTo("foo-bar");
	}

	[Test]
	public async Task SubstringUntilFirst_WhenNameStartsWithDot_ShouldReturnEmptyString()
	{
		string value = ".foo";

		string result = value.SubstringUntilFirst('.');

		await That(result).IsEqualTo("");
	}
}
