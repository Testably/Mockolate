#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsOutSpanTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutParameter<SpanWrapper<int>> sut =
				It.IsOutSpan(() => new SpanWrapper<int>([1, 2, 3,]));
			string expectedValue = "It.IsOut<SpanWrapper<int>>(() => new SpanWrapper<int>([1, 2, 3,]))";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task TryGetValue_ShouldReturnSetterValue()
		{
			IOutParameter<SpanWrapper<int>> sut =
				It.IsOutSpan(() => new SpanWrapper<int>([7, 8,]));

			bool found = sut.TryGetValue(out SpanWrapper<int> value);

			await That(found).IsTrue();
			await That(value.SpanValues.Length).IsEqualTo(2);
			await That(value.SpanValues[0]).IsEqualTo(7);
		}
	}
}
#endif
