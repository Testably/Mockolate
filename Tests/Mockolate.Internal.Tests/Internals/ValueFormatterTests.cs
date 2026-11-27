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

	[Test]
	[Arguments("global::Foo.IBar.Baz", "Baz")]
	[Arguments("Baz", "Baz")]
	[Arguments("global::Foo.IBar.Baz<System.String>", "Baz<string>")]
	[Arguments("global::Foo.IBar<global::System.String>.Baz<System.Int32>", "Baz<int>")]
	[Arguments("global::Foo.IBar.Baz<System.String, Foo.Qux>", "Baz<string, Qux>")]
	[Arguments("global::Foo.IBar.Baz<System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Int32]]>", "Baz<Dictionary<string, List<int>>>")]
	[Arguments("global::Foo.IBar.Baz<System.Nullable`1[System.Int32]>", "Baz<int?>")]
	[Arguments("global::Foo.IBar.Baz<System.Int32[]>", "Baz<int[]>")]
	[Arguments("global::Foo.IBar.Baz<System.Int32[,][]>", "Baz<int[,][]>")]
	[Arguments("global::Foo.IBar.Baz<System.Collections.Generic.List`1[System.String][]>", "Baz<List<string>[]>")]
	[Arguments("global::Foo.IBar.Baz<Foo.Outer`1+Inner`1[System.String,System.Int32]>", "Baz<Outer<string>.Inner<int>>")]
	[Arguments("global::Foo.IBar.Baz<Foo.Outer+Inner>", "Baz<Outer.Inner>")]
	[Arguments("global::Foo.IBar.Baz<<>f__AnonymousType0`1[System.Int32]>", "Baz<<>f__AnonymousType0<int>>")]
	public async Task FormatMethodName_ShouldStripTheContainingTypeAndShowTheTypeArgumentsInCSharp(string name, string expected)
	{
		string result = ValueFormatter.FormatMethodName(name);

		await That(result).IsEqualTo(expected);
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
