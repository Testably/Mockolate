#if NET9_0_OR_GREATER
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyOutRefStructTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutRefStructParameter<Span<int>> sut = It.IsAnyOutRefStruct<Span<int>>();
			string expectedValue = "It.IsAnyOutRefStruct<Span<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task TryGetValue_ShouldReturnFalse()
		{
			IOutRefStructParameter<Span<int>> sut = It.IsAnyOutRefStruct<Span<int>>();

			bool result = sut.TryGetValue(out _);

			await That(result).IsFalse();
		}
	}
}
#endif
