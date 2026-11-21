using System.Globalization;

namespace Mockolate.Internal.Tests.Internals;

public class ValueFormatterTests
{
	[Test]
	public async Task Format_ShouldFormatIndependentOfCurrentCulture()
	{
		CultureInfo previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("de-AT");

			string decimalResult = ValueFormatter.Format(4.5m);
			string doubleResult = ValueFormatter.Format<object>(0.25);
			string dateResult = ValueFormatter.Format(new DateTime(2026, 1, 2, 3, 4, 5));

			await That(decimalResult).IsEqualTo("4.5");
			await That(doubleResult).IsEqualTo("0.25");
			await That(dateResult).IsEqualTo("01/02/2026 03:04:05");
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[Test]
	public async Task Format_WithString_ShouldQuoteTheValue()
	{
		string result = ValueFormatter.Format("Alice");

		await That(result).IsEqualTo("\"Alice\"");
	}

	[Test]
	public async Task Format_WithNull_ShouldReturnNull()
	{
		string? value = null;

		string result = ValueFormatter.Format(value);

		await That(result).IsEqualTo("null");
	}

	[Test]
	public async Task Format_WithOtherValue_ShouldUseToString()
	{
		string result = ValueFormatter.Format(new FixedToString("foo"));

		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task Format_WhenToStringReturnsNull_ShouldReturnNull()
	{
		string result = ValueFormatter.Format(new FixedToString(null));

		await That(result).IsEqualTo("null");
	}

	[Test]
	public async Task Format_WhenFormattableReturnsNull_ShouldReturnNull()
	{
		string result = ValueFormatter.Format(new NullFormattable());

		await That(result).IsEqualTo("null");
	}

	private sealed class FixedToString(string? value)
	{
		public override string? ToString() => value;
	}

	private sealed class NullFormattable : IFormattable
	{
		public string ToString(string? format, IFormatProvider? formatProvider) => null!;
	}
}
