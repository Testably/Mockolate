using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public sealed class CallbackTests
{
	[Test]
	public async Task SkipInvocations_ShouldContinueCountingAfterTheSkippedInvocations()
	{
		Callback<Func<int, int>> sut = new(count => count);
		sut.SkipInvocations(3);

		int index = 0;
		sut.Invoke<object?, int>(ref index, null, static (count, callback, _) => callback(count), out int result);

		await That(result).IsEqualTo(3);
	}

	[Test]
	public async Task SkipInvocations_ShouldCountTowardsOnly()
	{
		Callback<Func<int, int>> sut = new(count => count);
		sut.SkipInvocations(3);
		sut.Only(3);

		int index = 0;
		bool wasInvoked = sut.Invoke<object?, int>(ref index, null, static (count, callback, _) => callback(count),
			out int _);

		await That(wasInvoked).IsFalse().Because("the callback was already used three times");
	}
}