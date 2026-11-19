#if NET10_0_OR_GREATER
namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A <see langword="params" /> span, which C# prefers over a <see langword="params" /> array of the same element type
///     for <c>new T()</c>. The span must stay a local of the generated code, as it cannot be boxed.
/// </summary>
public abstract class ParamsSpanConstructorBase
{
	protected ParamsSpanConstructorBase(params int[] numbers)
	{
		CalledConstructor = $"int[]({numbers.Length})";
	}

	protected ParamsSpanConstructorBase(params System.ReadOnlySpan<int> numbers)
	{
		CalledConstructor = $"ReadOnlySpan<int>({numbers.Length})";
	}

	public string CalledConstructor { get; }
}

public abstract class OnlyParamsSpanConstructorBase
{
	protected OnlyParamsSpanConstructorBase(int number = 1, params System.Span<string> texts)
	{
		Number = number;
		TextCount = texts.Length;
	}

	public int Number { get; }
	public int TextCount { get; }
}
#endif
