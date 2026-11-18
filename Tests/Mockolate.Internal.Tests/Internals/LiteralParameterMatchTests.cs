using System.Globalization;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Internals;

public class LiteralParameterMatchTests
{
	[Test]
	[Arguments("a", true)]
	[Arguments("b", false)]
	[Arguments(null, false)]
	public async Task Matches_ShouldCompareWithExpectedValue(string? value, bool expectedResult)
	{
		LiteralParameterMatch<string?> sut = new("a");

		bool result = sut.Matches(value);

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task Matches_ShouldCallEqualsOnExpectedValue()
	{
		AsymmetricEquality expected = new(true);
		LiteralParameterMatch<AsymmetricEquality> sut = new(expected);

		bool result = sut.Matches(new AsymmetricEquality(false));

		await That(result).IsTrue().Because("the slow path compared with the expected value as the first argument");
	}

	[Test]
	[Arguments(null, null, true)]
	[Arguments(null, "a", false)]
	[Arguments("a", null, false)]
	[Arguments("a", "a", true)]
	[Arguments("a", "b", false)]
	public async Task Matches_Object_ShouldCompareWithExpectedValue(string? expected, string? value,
		bool expectedResult)
	{
		IParameter sut = new LiteralParameterMatch<string?>(expected);

		bool result = sut.Matches(value);

		await That(result).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task Matches_Object_WithOtherType_ShouldReturnFalse()
	{
		IParameter sut = new LiteralParameterMatch<int>(1);

		bool result = sut.Matches("1");

		await That(result).IsFalse();
	}

	[Test]
	public async Task InvokeCallbacks_ShouldNotThrow()
	{
		LiteralParameterMatch<int> sut = new(1);

		void Act()
		{
			sut.InvokeCallbacks(1);
			((IParameter)sut).InvokeCallbacks(1);
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task ToString_ShouldFormatLikeStringInterpolation()
	{
		CultureInfo previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("de-AT");
			decimal value = 4.5m;
			string? nullValue = null;

			string result = new LiteralParameterMatch<decimal>(value).ToString();
			string stringResult = new LiteralParameterMatch<string?>("Alice").ToString();
			string nullResult = new LiteralParameterMatch<string?>(nullValue).ToString();

			await That(result).IsEqualTo($"{value}");
			await That(result).IsEqualTo("4,5");
			await That(stringResult).IsEqualTo("Alice");
			await That(nullResult).IsEqualTo("null")
				.Because("failure messages print a null argument as null instead of an empty string");
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	private sealed class AsymmetricEquality(bool equalsAnything)
	{
		public override bool Equals(object? obj) => equalsAnything;

		public override int GetHashCode() => 0;
	}
}
