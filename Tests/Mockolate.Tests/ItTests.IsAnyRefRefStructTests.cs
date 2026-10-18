#if NET9_0_OR_GREATER
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyRefRefStructTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IRefRefStructParameter<Span<int>> sut = It.IsAnyRefRefStruct<Span<int>>();
			string expectedValue = "It.IsAnyRefRefStruct<Span<int>>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task GetValue_ShouldReturnSameValue()
		{
			IRefRefStructParameter<Span<int>> sut = It.IsAnyRefRefStruct<Span<int>>();
			int[] backing = [1, 2, 3];

			Span<int> roundTripped = sut.GetValue(backing.AsSpan());
			int length = roundTripped.Length;
			int first = roundTripped[0];

			await That(length).IsEqualTo(3);
			await That(first).IsEqualTo(1);
		}
	}
}
#endif
