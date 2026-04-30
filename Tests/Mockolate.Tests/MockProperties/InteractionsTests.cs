namespace Mockolate.Tests.MockProperties;

public sealed partial class InteractionsTests
{
	[Test]
	public async Task PropertyGetterAccess_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		PropertyGetterAccess interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new PropertyGetterAccess("global::Mockolate.InteractionsTests.SomeProperty"));
		string expectedValue = "get property SomeProperty";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task PropertySetterAccess_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		PropertySetterAccess<int> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new PropertySetterAccess<int>("global::Mockolate.InteractionsTests.SomeProperty", 5));
		string expectedValue = "set property SomeProperty to 5";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task PropertySetterAccess_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		PropertySetterAccess<string?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new PropertySetterAccess<string?>("SomeProperty", null));
		string expectedValue = "set property SomeProperty to null";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}
}
