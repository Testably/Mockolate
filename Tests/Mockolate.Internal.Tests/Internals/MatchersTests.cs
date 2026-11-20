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
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>("a");

		bool result = sut.Matches(value);

		await That(result).IsEqualTo(expectedResult);
		await That(sut.ToString()).IsEqualTo("\"a\"");
	}

	[Test]
	public async Task IsValueOrMatcher_WithMatcher_ShouldUseTheMatcher()
	{
		List<string> received = [];
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>(
			It.IsAny<string>().Do(value => received.Add(value)));

		bool[] results = [sut.Matches("a"), sut.Matches(1),];
		sut.InvokeCallbacks("b");

		await That(results).IsEqualTo([true, false,]);
		await That(received).IsEqualTo(["b",])
			.Because("the callbacks of the matcher must run, also when its type argument differs from the parameter type");
		await That(sut.ToString()).IsEqualTo("It.IsAny<string>()");
	}

	[Test]
	public async Task IsValueOrMatcher_WithParametersMatch_ShouldCompareWithValue()
	{
		IParameters parameters = Match.AnyParameters();
		IParameterMatch<object?> sut = Matchers.IsValueOrMatcher<object?>(parameters);

		bool[] results = [sut.Matches(parameters), sut.Matches(1),];

		await That(results).IsEqualTo([true, false,])
			.Because("the generated overload decides whether a parameters match stands for the parameters");
	}
}
