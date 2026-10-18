#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyRefReadOnlySpanTests
	{
		[Test]
		public async Task GetValue_ShouldReturnSameValue()
		{
			IRefParameter<ReadOnlySpanWrapper<int>> sut = It.IsAnyRefReadOnlySpan<int>();
			ReadOnlySpanWrapper<int> input = new(new[]
			{
				1, 2, 3,
			});

			ReadOnlySpanWrapper<int> result = sut.GetValue(input);

			await That(result.ReadOnlySpanValues.Length).IsEqualTo(3);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IRefParameter<ReadOnlySpanWrapper<int>> sut = It.IsAnyRefReadOnlySpan<int>();
			string expectedValue = "It.IsAnyRef<ReadOnlySpanWrapper<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
