#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnySpanTests
	{
		[Test]
		[Arguments("")]
		[Arguments("foo")]
		[Arguments("bar")]
		public async Task ShouldAlwaysMatch(string value)
		{
			Span<char> valueSpan = new(value.ToCharArray());
			IVerifySpanParameter<char> sut = It.IsAnySpan<char>();

			bool result = ((IParameterMatch<SpanWrapper<char>>)sut).Matches((SpanWrapper<char>)valueSpan);

			await That(result).IsTrue();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IVerifySpanParameter<char> sut = It.IsAnySpan<char>();
			string expectedValue = "It.IsAnySpan<char>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
