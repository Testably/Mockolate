using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockEvents;

public sealed partial class InteractionsTests
{
	[Test]
	public async Task EventSubscription_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		EventSubscription interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new EventSubscription("global::Mockolate.InteractionsTests.SomeEvent", this, Helper.GetMethodInfo()));
		string expectedValue = "subscribe to event SomeEvent";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task EventUnsubscription_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		EventUnsubscription interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new EventUnsubscription("global::Mockolate.InteractionsTests.SomeEvent", this, Helper.GetMethodInfo()));
		string expectedValue = "unsubscribe from event SomeEvent";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}
}
