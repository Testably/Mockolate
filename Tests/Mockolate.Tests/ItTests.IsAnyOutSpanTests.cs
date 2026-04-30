#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyOutSpanTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutParameter<SpanWrapper<int>> sut = It.IsAnyOutSpan<int>();
			string expectedValue = "It.IsAnyOut<SpanWrapper<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task TryGetValue_ShouldReturnFalse()
		{
			IOutParameter<SpanWrapper<int>> sut = It.IsAnyOutSpan<int>();

			bool result = sut.TryGetValue(out _);

			await That(result).IsFalse();
		}
	}
}
#endif
