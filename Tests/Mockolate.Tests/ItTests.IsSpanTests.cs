#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsSpanTests
	{
		[Test]
		[Arguments("", false)]
		[Arguments("foo", false)]
		[Arguments("bar", true)]
		public async Task ShouldAlwaysMatch(string value, bool expectSuccess)
		{
			Span<char> valueSpan = new(value.ToCharArray());
			IVerifySpanParameter<char> sut = It.IsSpan<char>(c => c.Length > 0 && c[0] == 'b');

			bool result = ((IParameterMatch<SpanWrapper<char>>)sut).Matches((SpanWrapper<char>)valueSpan);

			await That(result).IsEqualTo(expectSuccess);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IVerifySpanParameter<char> sut = It.IsSpan<char>(c => c[0] == 'a');
			string expectedValue = "It.IsSpan<char>(c => c[0] == 'a')";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
