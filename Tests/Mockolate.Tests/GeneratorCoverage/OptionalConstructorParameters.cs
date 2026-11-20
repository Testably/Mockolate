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
	protected OptionalAndParameterlessConstructorBase(params int[] numbers)
	{
		CalledConstructor = $"params({numbers.Length})";
	}

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

/// <summary>
///     Constructors with a <see langword="params" /> array, which <c>CreateMock()</c> calls with an empty array and the
///     declared defaults, like <c>new T()</c>.
/// </summary>
public abstract class ParamsConstructorBase
{
	protected ParamsConstructorBase(params int[] numbers)
	{
		Numbers = numbers;
	}

	public int[] Numbers { get; }
}

public abstract class OptionalAndParamsConstructorBase
{
	protected OptionalAndParamsConstructorBase(int number = 1, params string?[] texts)
	{
		Number = number;
		Texts = texts;
	}

	public int Number { get; }
	public string?[] Texts { get; }
}

/// <summary>
///     C# prefers the constructor that is applicable without expanding a <see langword="params" /> array, then the one
///     that needs no default values, then the one with the more specific <see langword="params" /> array type.
/// </summary>
public abstract class ParamsConstructorSelectionBase
{
	protected ParamsConstructorSelectionBase(params object[] values)
	{
		CalledConstructor = "params object[]";
	}

	protected ParamsConstructorSelectionBase(int number = 1, params string[] texts)
	{
		CalledConstructor = "optional, params string[]";
	}

	protected ParamsConstructorSelectionBase(params string[] texts)
	{
		CalledConstructor = "params string[]";
	}

	public string CalledConstructor { get; }
}

/// <summary>
///     <see langword="params" /> collections: of two collection types, C# prefers the one that converts to the other.
/// </summary>
public abstract class ParamsCollectionConstructorBase
{
	protected ParamsCollectionConstructorBase(params System.Collections.Generic.IEnumerable<int> numbers)
	{
		CalledConstructor = "IEnumerable<int>";
	}

	protected ParamsCollectionConstructorBase(params System.Collections.Generic.List<int> numbers)
	{
		CalledConstructor = $"List<int>({numbers.Count})";
	}

	public string CalledConstructor { get; }
}

public abstract class OptionalOverParamsConstructorBase
{
	protected OptionalOverParamsConstructorBase(params int[] numbers)
	{
		CalledConstructor = "params int[]";
	}

	protected OptionalOverParamsConstructorBase(int number = 1)
	{
		CalledConstructor = $"optional({number})";
	}

	public string CalledConstructor { get; }
}
