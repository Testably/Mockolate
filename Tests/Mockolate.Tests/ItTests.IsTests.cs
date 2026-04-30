using System.Collections.Generic;
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsTests
	{
		[Test]
		[Arguments(1, false)]
		[Arguments(5, true)]
		[Arguments(-5, false)]
		[Arguments(42, false)]
		public async Task ShouldMatchWhenEqual(int value, bool expectMatch)
		{
			IParameter<int> sut = It.Is(5);

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task ShouldSupportCovarianceInSetup()
		{
			IMyService sut = IMyService.CreateMock();
			MyImplementation value1 = new();
			MyOtherImplementation value2 = new();
			sut.Mock.Setup.DoSomething(It.Is(value1))
				.Returns(3);

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(value2);

			await That(result1).IsEqualTo(3);
			await That(result2).IsEqualTo(0);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Is("foo");
			string expectedValue = "\"foo\"";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithComparer_ShouldReturnExpectedValue()
		{
			IParameter<int> sut = It.Is(4).Using(new AllEqualComparer());
			string expectedValue = "It.Is(4).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		[Arguments(1)]
		[Arguments(5)]
		[Arguments(-42)]
		public async Task WithComparer_ShouldUseComparer(int value)
		{
			IParameter<int> sut = It.Is(5).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsTrue();
		}

		[Test]
		public async Task Within_AfterUsing_ShouldReplaceTheComparer()
		{
			IParameter<double> sut = It.Is(5.0).Using(EqualityComparer<double>.Default).Within(0.1);

			bool result = ((IParameterMatch<double>)sut).Matches(4.95);

			await That(result).IsTrue();
			await That(sut.ToString()).IsEqualTo("It.Is(5.0).Within(0.1)");
		}

		[Test]
		[Arguments(-11, false)]
		[Arguments(-10, true)]
		[Arguments(0, true)]
		[Arguments(10, true)]
		[Arguments(11, false)]
		public async Task Within_DateTime_ShouldMatchWhenWithinTolerance(int offsetInSeconds, bool expectMatch)
		{
			DateTime expected = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
			IParameter<DateTime> sut = It.Is(expected).Within(TimeSpan.FromSeconds(10));

			bool result = ((IParameterMatch<DateTime>)sut).Matches(expected.AddSeconds(offsetInSeconds));

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		[Arguments(-11, false)]
		[Arguments(-10, true)]
		[Arguments(0, true)]
		[Arguments(10, true)]
		[Arguments(11, false)]
		public async Task Within_DateTimeOffset_ShouldMatchWhenWithinTolerance(int offsetInSeconds, bool expectMatch)
		{
			DateTimeOffset expected = new(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
			IParameter<DateTimeOffset> sut = It.Is(expected).Within(TimeSpan.FromSeconds(10));

			bool result = ((IParameterMatch<DateTimeOffset>)sut).Matches(expected.AddSeconds(offsetInSeconds));

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		[Arguments(4.85, false)]
		[Arguments(4.9, true)]
		[Arguments(5.0, true)]
		[Arguments(5.1, true)]
		[Arguments(5.15, false)]
		public async Task Within_Decimal_ShouldMatchWhenWithinTolerance(double value, bool expectMatch)
		{
			IParameter<decimal> sut = It.Is(5.0m).Within(0.1m);

			bool result = ((IParameterMatch<decimal>)sut).Matches((decimal)value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		[Arguments(4.85, false)]
		[Arguments(4.9, true)]
		[Arguments(5.0, true)]
		[Arguments(5.1, true)]
		[Arguments(5.15, false)]
		public async Task Within_Double_ShouldMatchWhenWithinTolerance(double value, bool expectMatch)
		{
			IParameter<double> sut = It.Is(5.0).Within(0.1);

			bool result = ((IParameterMatch<double>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		[Arguments(double.NaN)]
		[Arguments(double.PositiveInfinity)]
		[Arguments(double.NegativeInfinity)]
		public async Task Within_Double_WithNonFiniteValue_ShouldMatchItself(double value)
		{
			IParameter<double> sut = It.Is(value).Within(0.1);

			bool result = ((IParameterMatch<double>)sut).Matches(value);

			await That(result).IsTrue();
		}

		[Test]
		[Arguments(4.85f, false)]
		[Arguments(4.9f, true)]
		[Arguments(5.0f, true)]
		[Arguments(5.1f, true)]
		[Arguments(5.15f, false)]
		public async Task Within_Float_ShouldMatchWhenWithinTolerance(float value, bool expectMatch)
		{
			IParameter<float> sut = It.Is(5.0f).Within(0.1f);

			bool result = ((IParameterMatch<float>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		[Arguments(float.NaN)]
		[Arguments(float.PositiveInfinity)]
		[Arguments(float.NegativeInfinity)]
		public async Task Within_Float_WithNonFiniteValue_ShouldMatchItself(float value)
		{
			IParameter<float> sut = It.Is(value).Within(0.1f);

			bool result = ((IParameterMatch<float>)sut).Matches(value);

			await That(result).IsTrue();
		}

		[Test]
		public async Task Within_NaNTolerance_ShouldThrowArgumentOutOfRangeException()
		{
			await That(() => It.Is(5.0).Within(double.NaN)).Throws<ArgumentOutOfRangeException>()
				.WithParamName("tolerance");
			await That(() => It.Is(5.0f).Within(float.NaN)).Throws<ArgumentOutOfRangeException>()
				.WithParamName("tolerance");
		}

		[Test]
		public async Task Within_NegativeTolerance_ShouldThrowArgumentOutOfRangeException()
		{
			await That(() => It.Is(5.0).Within(-0.1)).Throws<ArgumentOutOfRangeException>()
				.WithParamName("tolerance");
			await That(() => It.Is(5.0f).Within(-0.1f)).Throws<ArgumentOutOfRangeException>()
				.WithParamName("tolerance");
			await That(() => It.Is(5.0m).Within(-0.1m)).Throws<ArgumentOutOfRangeException>()
				.WithParamName("tolerance");
			await That(() => It.Is(DateTime.UtcNow).Within(TimeSpan.FromSeconds(-1)))
				.Throws<ArgumentOutOfRangeException>().WithParamName("tolerance");
			await That(() => It.Is(TimeSpan.Zero).Within(TimeSpan.FromSeconds(-1)))
				.Throws<ArgumentOutOfRangeException>().WithParamName("tolerance");
		}

		[Test]
		public async Task Within_Nullable_ShouldMatchWhenWithinTolerance()
		{
			DateTime dateTime = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
			DateTimeOffset dateTimeOffset = new(dateTime, TimeSpan.Zero);
			TimeSpan timeSpan = TimeSpan.FromMinutes(3);
			TimeSpan tolerance = TimeSpan.FromSeconds(10);
			IParameter<double?> doubleMatcher = It.Is<double?>(5.0).Within(0.1);
			IParameter<float?> floatMatcher = It.Is<float?>(5.0f).Within(0.1f);
			IParameter<decimal?> decimalMatcher = It.Is<decimal?>(5.0m).Within(0.1m);
			IParameter<DateTime?> dateTimeMatcher = It.Is<DateTime?>(dateTime).Within(tolerance);
			IParameter<DateTimeOffset?> dateTimeOffsetMatcher = It.Is<DateTimeOffset?>(dateTimeOffset).Within(tolerance);
			IParameter<TimeSpan?> timeSpanMatcher = It.Is<TimeSpan?>(timeSpan).Within(tolerance);

			await That(((IParameterMatch<double?>)doubleMatcher).Matches(4.95)).IsTrue();
			await That(((IParameterMatch<float?>)floatMatcher).Matches(4.95f)).IsTrue();
			await That(((IParameterMatch<decimal?>)decimalMatcher).Matches(4.95m)).IsTrue();
			await That(((IParameterMatch<DateTime?>)dateTimeMatcher).Matches(dateTime.AddSeconds(5))).IsTrue();
			await That(((IParameterMatch<DateTimeOffset?>)dateTimeOffsetMatcher).Matches(dateTimeOffset.AddSeconds(5)))
				.IsTrue();
			await That(((IParameterMatch<TimeSpan?>)timeSpanMatcher).Matches(timeSpan.Add(TimeSpan.FromSeconds(5))))
				.IsTrue();
		}

		[Test]
		public async Task Within_Nullable_ShouldOnlyMatchNullWithNull()
		{
			IParameter<double?> nullMatcher = It.Is<double?>(null).Within(0.1);
			IParameter<double?> valueMatcher = It.Is<double?>(5.0).Within(0.1);

			await That(((IParameterMatch<double?>)nullMatcher).Matches(null)).IsTrue();
			await That(((IParameterMatch<double?>)nullMatcher).Matches(5.0)).IsFalse();
			await That(((IParameterMatch<double?>)valueMatcher).Matches(null)).IsFalse();
			await That(((IParameterMatch<double?>)valueMatcher).Matches(4.85)).IsFalse();
		}

		[Test]
		public async Task Within_ShouldVerifyMockInvocation()
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithDouble(4.95);

			await That(sut.Mock.Verify.DoSomethingWithDouble(It.Is(5.0).Within(0.1))).Once();
			await That(sut.Mock.Verify.DoSomethingWithDouble(It.Is(5.0).Within(0.01))).Never();
		}

		[Test]
		public async Task Within_ShouldVerifyMockInvocationWithNullableParameter()
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();

			sut.DoSomethingWithNullableDouble(4.95);

			await That(sut.Mock.Verify.DoSomethingWithNullableDouble(It.Is<double?>(5.0).Within(0.1))).Once();
			await That(sut.Mock.Verify.DoSomethingWithNullableDouble(It.Is<double?>(5.0).Within(0.01))).Never();
		}

		[Test]
		public async Task Within_ThenUsing_ShouldReplaceTheTolerance()
		{
			// Within() does not return an It.IIsParameter, so Using() can only be reached via the original reference.
			It.IIsParameter<double> sut = It.Is(5.0);
			sut.Within(0.1);
			sut.Using(EqualityComparer<double>.Default);

			bool result = ((IParameterMatch<double>)sut).Matches(4.95);

			await That(result).IsFalse();
			await That(sut.ToString()).IsEqualTo("It.Is(5.0).Using(EqualityComparer<double>.Default)");
		}

		[Test]
		[Arguments(-11, false)]
		[Arguments(-10, true)]
		[Arguments(0, true)]
		[Arguments(10, true)]
		[Arguments(11, false)]
		public async Task Within_TimeSpan_ShouldMatchWhenWithinTolerance(int offsetInSeconds, bool expectMatch)
		{
			TimeSpan expected = TimeSpan.FromMinutes(3);
			IParameter<TimeSpan> sut = It.Is(expected).Within(TimeSpan.FromSeconds(10));

			bool result = ((IParameterMatch<TimeSpan>)sut).Matches(expected + TimeSpan.FromSeconds(offsetInSeconds));

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task Within_ToString_ShouldReturnExpectedValue()
		{
			double tolerance = 0.1;
			IParameter<double> sut = It.Is(5.0).Within(tolerance);
			string expectedValue = "It.Is(5.0).Within(tolerance)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task Within_WithExtremeValues_ShouldNotOverflow()
		{
			IParameter<decimal> decimalMatcher = It.Is(decimal.MaxValue).Within(1m);
			IParameter<TimeSpan> timeSpanMatcher = It.Is(TimeSpan.Zero).Within(TimeSpan.FromSeconds(1));
			IParameter<DateTime> dateTimeMatcher = It.Is(DateTime.MaxValue).Within(TimeSpan.FromSeconds(1));

			await That(((IParameterMatch<decimal>)decimalMatcher).Matches(decimal.MinValue)).IsFalse();
			await That(((IParameterMatch<decimal>)decimalMatcher).Matches(decimal.MaxValue - 1m)).IsTrue();
			await That(((IParameterMatch<TimeSpan>)timeSpanMatcher).Matches(TimeSpan.MinValue)).IsFalse();
			await That(((IParameterMatch<TimeSpan>)timeSpanMatcher).Matches(TimeSpan.FromSeconds(-1))).IsTrue();
			await That(((IParameterMatch<DateTime>)dateTimeMatcher).Matches(DateTime.MinValue)).IsFalse();
		}

		public interface IMyBase
		{
			int DoWork();
		}

		public class MyImplementation : IMyBase
		{
			public int Progress { get; private set; }

			public int DoWork()
			{
				Progress++;
				return Progress;
			}
		}

		public class MyOtherImplementation : IMyBase
		{
			public string Output { get; private set; } = "";

			public int DoWork()
			{
				Output += "did something\n";
				return 1;
			}
		}

		public interface IMyService
		{
			int DoSomething(IMyBase value);
		}
	}
}
