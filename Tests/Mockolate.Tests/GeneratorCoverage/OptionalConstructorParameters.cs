namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A constructor whose parameters are all optional, which <c>CreateMock()</c> calls with the declared defaults. The
///     required constructor with the same number of parameters would make a <see langword="null" /> literal ambiguous.
/// </summary>
public abstract class OptionalConstructorBase
{
	protected OptionalConstructorBase(int[] numbers, int number)
	{
		Text = string.Join(",", numbers);
		Number = number;
	}

	protected OptionalConstructorBase(string? text = null, int number = 1)
	{
		Text = text;
		Number = number;
	}

	public int Number { get; }
	public string? Text { get; }
}

public abstract class OptionalAndParameterlessConstructorBase
{
	protected OptionalAndParameterlessConstructorBase(int number = 1)
	{
		CalledConstructor = $"optional({number})";
	}

	protected OptionalAndParameterlessConstructorBase()
	{
		CalledConstructor = "parameterless";
	}

	public string CalledConstructor { get; }
}

public abstract class SeveralOptionalConstructorsBase
{
	protected SeveralOptionalConstructorsBase(int number = 1)
	{
		CalledConstructor = $"int({number})";
	}

	protected SeveralOptionalConstructorsBase(string text = "default")
	{
		CalledConstructor = $"string({text})";
	}

	public string CalledConstructor { get; }
}
