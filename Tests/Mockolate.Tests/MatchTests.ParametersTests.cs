using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class MatchTests
{
	public sealed class ParametersTests
	{
		[Test]
		[Arguments(true, null, null)]
		[Arguments(true, "", 1)]
		[Arguments(true, "foo", null)]
		[Arguments(false, null, null)]
		[Arguments(false, "", 1)]
		[Arguments(false, "foo", null)]
		public async Task ShouldMatchWhenPredicateReturnsTrue(bool expectedResult, string? value1, int? value2)
		{
			IParameters sut = Match.Parameters(_ => expectedResult);
			IParametersMatch match = (IParametersMatch)sut;

			bool result = match.Matches([value1, value2,]);

			await That(result).IsEqualTo(expectedResult);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameters sut = Match.Parameters(_ => true);
			string expectedValue = "Match.Parameters(_ => true)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
