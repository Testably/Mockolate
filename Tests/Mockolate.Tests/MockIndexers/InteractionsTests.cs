using aweXpect.Chronology;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class InteractionsTests
{
	[Test]
	public async Task IndexerGetterAccess1_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string>(
					"SomeProperty"));
		string expectedValue = "get indexer [SomeProperty]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess1_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string?>(
					null));
		string expectedValue = "get indexer [null]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess2_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string, int>(
					"SomeProperty",
					4));
		string expectedValue = "get indexer [SomeProperty, 4]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess2_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string?, long?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string?, long?>(
					null,
					null));
		string expectedValue = "get indexer [null, null]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess3_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string, int, long?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string, int, long?>(
					"SomeProperty",
					4,
					7L));
		string expectedValue = "get indexer [SomeProperty, 4, 7]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess3_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string?, int, long?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string?, int, long?>(
					null,
					4,
					null));
		string expectedValue = "get indexer [null, 4, null]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess4_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string, int, long?, TimeSpan> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string, int, long?, TimeSpan>(
					"SomeProperty",
					4,
					7L,
					150.Seconds()));
		string expectedValue = "get indexer [SomeProperty, 4, 7, 00:02:30]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess4_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string, int, long?, TimeSpan> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string, int, long?, TimeSpan>(
					"SomeProperty",
					4,
					null,
					150.Seconds()));
		string expectedValue = "get indexer [SomeProperty, 4, null, 00:02:30]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess5_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string, int, long?, TimeSpan, bool> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string, int, long?, TimeSpan, bool>(
					"SomeProperty",
					4,
					7L,
					150.Seconds(),
					true));
		string expectedValue = "get indexer [SomeProperty, 4, 7, 00:02:30, True]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerGetterAccess5_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerGetterAccess<string?, int, long?, TimeSpan, bool?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerGetterAccess<string?, int, long?, TimeSpan, bool?>(
					null,
					4,
					null,
					150.Seconds(),
					null));
		string expectedValue = "get indexer [null, 4, null, 00:02:30, null]";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess1_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int>(
					"SomeProperty",
					6));
		string expectedValue = "set indexer [SomeProperty] to 6";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess1_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string?, string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string?, string?>(
					null,
					null));
		string expectedValue = "set indexer [null] to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess2_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int, int>(
					"SomeProperty",
					4,
					6));
		string expectedValue = "set indexer [SomeProperty, 4] to 6";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess2_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string?, long?, string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string?, long?, string?>(
					null,
					null,
					null));
		string expectedValue = "set indexer [null, null] to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess3_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int, long?, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int, long?, int>(
					"SomeProperty",
					4,
					7L,
					6));
		string expectedValue = "set indexer [SomeProperty, 4, 7] to 6";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess3_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string?, int, long?, string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string?, int, long?, string?>(
					null,
					4,
					null,
					null));
		string expectedValue = "set indexer [null, 4, null] to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess4_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int, long?, TimeSpan, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int, long?, TimeSpan, int>(
					"SomeProperty",
					4,
					7L,
					150.Seconds(),
					6));
		string expectedValue = "set indexer [SomeProperty, 4, 7, 00:02:30] to 6";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess4_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int, long?, TimeSpan, string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int, long?, TimeSpan, string?>(
					"SomeProperty",
					4,
					null,
					150.Seconds(),
					null));
		string expectedValue = "set indexer [SomeProperty, 4, null, 00:02:30] to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess5_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string, int, long?, TimeSpan, bool, int> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string, int, long?, TimeSpan, bool, int>(
					"SomeProperty",
					4,
					7L,
					150.Seconds(),
					true,
					6));
		string expectedValue = "set indexer [SomeProperty, 4, 7, 00:02:30, True] to 6";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task IndexerSetterAccess5_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		IndexerSetterAccess<string?, int, long?, TimeSpan, bool?, string?> interaction =
			((IMockInteractions)interactions).RegisterInteraction(
				new IndexerSetterAccess<string?, int, long?, TimeSpan, bool?, string?>(
					null,
					4,
					null,
					150.Seconds(),
					null,
					null));
		string expectedValue = "set indexer [null, 4, null, 00:02:30, null] to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}
}
