using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class MatchTests
{
	public sealed class AnyParametersTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameters sut = Match.AnyParameters();
			string expectedValue = "Match.AnyParameters()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
