#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyRefSpanTests
	{
		[Test]
		public async Task GetValue_ShouldReturnSameValue()
		{
			IRefParameter<SpanWrapper<int>> sut = It.IsAnyRefSpan<int>();
			SpanWrapper<int> input = new(new[]
			{
				1, 2, 3,
			});

			SpanWrapper<int> result = sut.GetValue(input);

			await That(result.SpanValues.Length).IsEqualTo(3);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IRefParameter<SpanWrapper<int>> sut = It.IsAnyRefSpan<int>();
			string expectedValue = "It.IsAnyRef<SpanWrapper<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
