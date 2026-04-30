#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsOutReadOnlySpanTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutParameter<ReadOnlySpanWrapper<int>> sut =
				It.IsOutReadOnlySpan(() => new ReadOnlySpanWrapper<int>([1, 2,]));
			string expectedValue = "It.IsOut<ReadOnlySpanWrapper<int>>(() => new ReadOnlySpanWrapper<int>([1, 2,]))";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task TryGetValue_ShouldReturnSetterValue()
		{
			IOutParameter<ReadOnlySpanWrapper<int>> sut =
				It.IsOutReadOnlySpan(() => new ReadOnlySpanWrapper<int>([7, 8,]));

			bool found = sut.TryGetValue(out ReadOnlySpanWrapper<int> value);

			await That(found).IsTrue();
			await That(value.ReadOnlySpanValues.Length).IsEqualTo(2);
			await That(value.ReadOnlySpanValues[0]).IsEqualTo(7);
		}
	}
}
#endif
