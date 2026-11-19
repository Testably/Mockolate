namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Optional parameters of an enum or nullable enum type. The generated code repeats the default as a cast of the
///     underlying value, which needs parentheses when it is negative.
/// </summary>
public interface IEnumDefaultValues
{
	int Negative(int a, EnumWithNegativeValue level = EnumWithNegativeValue.Unknown);
	int NegativeLong(int a, LongEnumWithNegativeValue level = LongEnumWithNegativeValue.Unknown);
	int NegativeSByte(int a, SByteEnumWithNegativeValue level = SByteEnumWithNegativeValue.Unknown);
	int Nullable(int a, EnumWithNegativeValue? level = EnumWithNegativeValue.High);
	int NullableNegative(int a, EnumWithNegativeValue? level = EnumWithNegativeValue.Unknown);
	int NullableZero(int a, EnumWithNegativeValue? level = EnumWithNegativeValue.Low);
	int NullableNull(int a, EnumWithNegativeValue? level = null);
}

public delegate int EnumDefaultValuesDelegate(
	EnumWithNegativeValue level = EnumWithNegativeValue.Unknown,
	EnumWithNegativeValue? nullableLevel = EnumWithNegativeValue.High);

public abstract class EnumDefaultValuesBase
{
	protected EnumDefaultValuesBase(
		EnumWithNegativeValue level = EnumWithNegativeValue.Unknown,
		EnumWithNegativeValue? nullableLevel = EnumWithNegativeValue.High,
		EnumWithNegativeValue? zeroLevel = EnumWithNegativeValue.Low)
	{
	}

	public abstract int Method(EnumWithNegativeValue level = EnumWithNegativeValue.Unknown);
}

public enum EnumWithNegativeValue { Unknown = -1, Low = 0, High = 1, }

public enum LongEnumWithNegativeValue : long { Unknown = -1, Low = 0, }

public enum SByteEnumWithNegativeValue : sbyte { Unknown = -1, Low = 0, }
