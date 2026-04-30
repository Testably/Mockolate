#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsRefReadOnlySpanTests
	{
		[Test]
		public async Task ToString_WithPredicateAndSetter_ShouldReturnExpectedValue()
		{
			IRefParameter<ReadOnlySpanWrapper<int>> sut = It.IsRefReadOnlySpan<int>(
				value => value.ReadOnlySpanValues.Length > 0,
				value => value);
			string expectedValue =
				"It.IsRef<ReadOnlySpanWrapper<int>>(value => value.ReadOnlySpanValues.Length > 0, value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithPredicateOnly_ShouldReturnExpectedValue()
		{
			IRefParameter<ReadOnlySpanWrapper<int>> sut =
				It.IsRefReadOnlySpan<int>(value => value.ReadOnlySpanValues.Length > 0);
			string expectedValue =
				"It.IsRef<ReadOnlySpanWrapper<int>>(value => value.ReadOnlySpanValues.Length > 0)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithSetterOnly_ShouldReturnExpectedValue()
		{
			IRefParameter<ReadOnlySpanWrapper<int>> sut = It.IsRefReadOnlySpan<int>(value => value);
			string expectedValue = "It.IsRef<ReadOnlySpanWrapper<int>>(value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
