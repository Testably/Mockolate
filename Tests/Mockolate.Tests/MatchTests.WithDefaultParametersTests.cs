using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class MatchTests
{
	public sealed class WithDefaultParametersTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IDefaultEventParameters sut = Match.WithDefaultParameters();
			string expectedValue = "Match.WithDefaultParameters()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
