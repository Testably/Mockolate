#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests.Parameters;

public sealed partial class ItTests
{
	public sealed class IsRefSpanTests
	{
		[Test]
		public async Task ToString_WithPredicateAndSetter_ShouldDescribeTheMatcher()
		{
			IRefParameter<SpanWrapper<int>> sut = It.IsRefSpan<int>(
				value => value.SpanValues.Length > 0,
				value => value);
			string expectedValue =
				"It.IsRef<SpanWrapper<int>>(value => value.SpanValues.Length > 0, value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithPredicateOnly_ShouldDescribeTheMatcher()
		{
			IRefParameter<SpanWrapper<int>> sut =
				It.IsRefSpan<int>(value => value.SpanValues.Length > 0);
			string expectedValue = "It.IsRef<SpanWrapper<int>>(value => value.SpanValues.Length > 0)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithSetterOnly_ShouldDescribeTheMatcher()
		{
			IRefParameter<SpanWrapper<int>> sut = It.IsRefSpan<int>(value => value);
			string expectedValue = "It.IsRef<SpanWrapper<int>>(value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
