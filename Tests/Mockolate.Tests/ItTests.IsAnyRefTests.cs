using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyRefTests
	{
		[Test]
		public async Task ShouldMatchInt()
		{
			IRefParameter<int?> sut = It.IsAnyRef<int?>();

			bool result = ((IParameterMatch<int?>)sut).Matches(123);

			await That(result).IsTrue();
		}

		[Test]
		public async Task ShouldMatchNull()
		{
			IRefParameter<int?> sut = It.IsAnyRef<int?>();

			bool result = ((IParameterMatch<int?>)sut).Matches(null);

			await That(result).IsTrue();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IRefParameter<int> sut = It.IsAnyRef<int>();
			string expectedValue = "It.IsAnyRef<int>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
