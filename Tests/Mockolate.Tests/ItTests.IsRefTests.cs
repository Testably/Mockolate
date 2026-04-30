using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsRefTests
	{
		[Test]
		public async Task PredicateOnly_ShouldNotChangeValue()
		{
			IRefParameter<string> sut = It.IsRef<string>(_ => true);

			string result = sut.GetValue("foo");

			await That(result).IsEqualTo("foo");
		}

		[Test]
		[Arguments(true)]
		[Arguments(false)]
		public async Task ShouldMatchForExpectedResult(bool predicateValue)
		{
			IRefParameter<string> sut = It.IsRef<string>(_ => predicateValue, _ => "");

			bool result = ((IParameterMatch<string>)sut).Matches("foo");

			await That(result).IsEqualTo(predicateValue);
		}

		[Test]
		public async Task Verify_ShouldAlwaysMatch()
		{
			IVerifyRefParameter<int?> sut = It.IsRef<int?>();

			bool result = ((IParameterMatch<int?>)sut).Matches(42);

			await That(result).IsTrue();
			await That(() => ((IParameterMatch<int?>)sut).InvokeCallbacks(0)).DoesNotThrow();
		}

#if NET9_0_OR_GREATER
		[Test]
		public async Task ToString_WithRefStructTransform_ShouldReturnExpectedValue()
		{
			IRefRefStructParameter<Span<int>> sut = It.IsRef<Span<int>>(value => value);
			string expectedValue = "It.IsRef<Span<int>>(value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithRefStructPredicateAndTransform_ShouldReturnExpectedValue()
		{
			IRefRefStructParameter<Span<int>> sut =
				It.IsRef<Span<int>>(value => value.Length > 0, value => value);
			string expectedValue = "It.IsRef<Span<int>>(value => value.Length > 0, value => value)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithRefStructPredicateOnly_ShouldReturnExpectedValue()
		{
			IRefRefStructParameter<Span<int>> sut = It.IsRef<Span<int>>(value => value.Length > 0);
			string expectedValue = "It.IsRef<Span<int>>(value => value.Length > 0)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
#endif
	}
}
