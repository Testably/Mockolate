using aweXpect.Chronology;

namespace Mockolate.Tests.MockMethods;

public sealed partial class InteractionsTests
{
	[Test]
	public async Task MethodInvocation_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation("global::Mockolate.InteractionsTests.SomeMethod"));
		string expectedValue = "invoke method SomeMethod()";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation1_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<int> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<int>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				5));
		string expectedValue = "invoke method SomeMethod(5)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation1_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string?>(
				"SomeMethod",
				null));
		string expectedValue = "invoke method SomeMethod(null)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation2_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<int, string> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<int, string>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				5,
				"foo"));
		string expectedValue = "invoke method SomeMethod(5, foo)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation2_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string?, long?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string?, long?>(
				"SomeMethod",
				null,
				null));
		string expectedValue = "invoke method SomeMethod(null, null)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation3_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<int, long?, TimeSpan> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<int, long?, TimeSpan>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				1,
				2L,
				90.Seconds()));
		string expectedValue = "invoke method SomeMethod(1, 2, 00:01:30)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation3_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<int?, long?, TimeSpan?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<int?, long?, TimeSpan?>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				null,
				null,
				null));
		string expectedValue = "invoke method SomeMethod(null, null, null)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation4_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string, int, long?, TimeSpan> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string, int, long?, TimeSpan>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				"foo",
				4,
				7L,
				150.Seconds()));
		string expectedValue = "invoke method SomeMethod(foo, 4, 7, 00:02:30)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation4_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string?, int?, long?, TimeSpan?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string?, int?, long?, TimeSpan?>(
				"SomeMethod",
				null,
				null,
				null,
				null));
		string expectedValue = "invoke method SomeMethod(null, null, null, null)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation5_ToString_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string, int, long?, TimeSpan, bool> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string, int, long?, TimeSpan, bool>(
				"global::Mockolate.InteractionsTests.SomeMethod",
				"foo",
				4,
				7L,
				150.Seconds(),
				true));
		string expectedValue = "invoke method SomeMethod(foo, 4, 7, 00:02:30, True)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task MethodInvocation5_ToString_WithNull_ShouldReturnExpectedValue()
	{
		FastMockInteractions interactions = new(0);
		MethodInvocation<string?, int?, long?, TimeSpan?, bool?> interaction = ((IMockInteractions)interactions).RegisterInteraction(
			new MethodInvocation<string?, int?, long?, TimeSpan?, bool?>(
				"SomeMethod",
				null,
				null,
				null,
				null,
				null));
		string expectedValue = "invoke method SomeMethod(null, null, null, null, null)";

		await That(interaction.ToString()).IsEqualTo(expectedValue);
	}
}
