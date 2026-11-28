using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockEvents;

public sealed partial class EventInteractionsTests
{
	[Test]
	public async Task ToString_WithSubscription_ShouldDescribeTheSubscription()
	{
		MockInteractions interactions = new(0);
		EventSubscription interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new EventSubscription("global::Mockolate.InteractionsTests.SomeEvent", this, Helper.GetMethodInfo()));
		string expectedValue = "subscribe to event SomeEvent";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task ToString_WithUnsubscription_ShouldDescribeTheUnsubscription()
	{
		MockInteractions interactions = new(0);
		EventUnsubscription interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new EventUnsubscription("global::Mockolate.InteractionsTests.SomeEvent", this, Helper.GetMethodInfo()));
		string expectedValue = "unsubscribe from event SomeEvent";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}
}
