using System.Collections.Generic;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Internals;

public sealed class MatchersTests
{
	[Test]
	[Arguments("a", true)]
	[Arguments("b", false)]
	[Arguments(null, false)]
	public async Task IsValueOrMatcher_WithValue_ShouldCompareWithValue(string? value, bool expectedResult)
	{
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>("a", "value");

		bool result = sut.Matches(value);

		await That(result).IsEqualTo(expectedResult);
		await That(sut.ToString()).IsEqualTo("\"a\"");
	}

	[Test]
	public async Task IsValueOrMatcher_WithMatcher_ShouldUseTheMatcher()
	{
		List<string> received = [];
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>(
			It.IsAny<string>().Do(value => received.Add(value)), null);

		bool[] results = [sut.Matches("a"), sut.Matches(1),];
		sut.InvokeCallbacks("b");

		await That(results).IsEqualTo([true, false,]);
		await That(received).IsEqualTo(["b",])
			.Because("the callbacks of the matcher must run, also when its type argument differs from the parameter type");
		await That(sut.ToString()).IsEqualTo("It.IsAny<string>()");
	}

	[Test]
	public async Task IsValueOrMatcher_WithParametersMatchAndParameterName_ShouldMatchTheParameters()
	{
		IParameterMatch<object?> any = Matchers.IsValueOrMatcher<object?>(Match.AnyParameters(), "value");
		IParameterMatch<object?> predicate =
			Matchers.IsValueOrMatcher<object?>(Match.Parameters(values => values[0] is "a"), "value");
		IParameterMatch<object?> named = Matchers.IsValueOrMatcher<object?>(new NamedParametersMatch(), "value");

		bool[] results =
		[
			any.Matches(1), predicate.Matches("a"), predicate.Matches("b"), named.Matches("a"), named.Matches("b"),
		];
		any.InvokeCallbacks(1);

		await That(results).IsEqualTo([true, true, false, true, false,]);
		await That(any.ToString()).IsEqualTo("Match.AnyParameters()");
	}

	[Test]
	public async Task IsValueOrMatcher_WithParametersMatchWithoutParameterName_ShouldCompareWithValue()
	{
		IParameters parameters = Match.AnyParameters();
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>(parameters, null);

		bool[] results = [sut.Matches(parameters), sut.Matches(1),];

		await That(results).IsEqualTo([true, false,])
			.Because("only the IParameters overload of a method with a single parameter takes a parameters match");
	}

	private sealed class NamedParametersMatch : IParameters, INamedParametersMatch
	{
		public bool Matches(ReadOnlySpan<(string, object?)> values)
			=> values.Length == 1 && values[0].Item1 == "value" && Equals(values[0].Item2, "a");
	}
}
