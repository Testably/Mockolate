#if NET8_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Tests.Parameters;

public sealed partial class ItTests
{
	public sealed class IsReadOnlySpanTests
	{
		[Test]
		[Arguments("", false)]
		[Arguments("foo", false)]
		[Arguments("bar", true)]
		public async Task ShouldMatchWhenThePredicateHolds(string value, bool expectSuccess)
		{
			ReadOnlySpan<char> valueSpan = value.AsSpan();
			IVerifyReadOnlySpanParameter<char> sut = It.IsReadOnlySpan<char>(c => c.Length > 0 && c[0] == 'b');

			bool result = ((IParameterMatch<ReadOnlySpanWrapper<char>>)sut).Matches((ReadOnlySpanWrapper<char>)valueSpan);

			await That(result).IsEqualTo(expectSuccess);
		}

		[Test]
		public async Task ToString_ShouldDescribeTheMatcher()
		{
			IVerifyReadOnlySpanParameter<char> sut = It.IsReadOnlySpan<char>(c => c[0] == 'a');
			string expectedValue = "It.IsReadOnlySpan<char>(c => c[0] == 'a')";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
#endif
