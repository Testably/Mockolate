#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyOutReadOnlySpanTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutParameter<ReadOnlySpanWrapper<int>> sut = It.IsAnyOutReadOnlySpan<int>();
			string expectedValue = "It.IsAnyOut<ReadOnlySpanWrapper<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task TryGetValue_ShouldReturnFalse()
		{
			IOutParameter<ReadOnlySpanWrapper<int>> sut = It.IsAnyOutReadOnlySpan<int>();

			bool result = sut.TryGetValue(out _);

			await That(result).IsFalse();
		}
	}
}
#endif
