using System.Collections.Generic;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed class MockSetupsTests
{
	[Test]
	public async Task ClearAllInteractions_ShouldRemovePreviousInteractions()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;

		sut.Dispense("Dark", 1);
		sut.Dispense("Light", 2);
		sut.Mock.ClearAllInteractions();
		sut.Dispense("Dark", 3);
		sut.Dispense("Light", 4);
		sut.Dispense("Milk", 5);
		IReadOnlyCollection<IInteraction> result = interactions.GetUnverifiedInteractions();

		await That(result).HasCount(3);
	}
}
