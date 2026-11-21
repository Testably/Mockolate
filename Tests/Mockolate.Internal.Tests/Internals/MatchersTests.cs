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
	public async Task AsParamsMatcher_WithValues_ShouldReturnNull()
	{
		IParameterMatch<object?[]>?[] results =
		[
			Matchers.AsParamsMatcher<object?>(["a", 1, null,]),
			Matchers.AsParamsMatcher<object?>([]),
			Matchers.AsParamsMatcher<object?>([Match.AnyParameters(),]),
		];

		await That(results).All().AreEqualTo(null)
			.Because("the generated overload compares plain values without allocating a matcher");
	}

	[Test]
	public async Task AsParamsMatcher_WithNullArray_ShouldMatchNullArray()
	{
		IParameterMatch<object?[]>? sut = Matchers.AsParamsMatcher<object?>(null);

		bool[] results = [sut!.Matches(null!), sut.Matches([]), sut.Matches([null,]),];

		await That(results).IsEqualTo([true, false, false,]);
		await That(sut.ToString()).IsEqualTo("null");
	}

	[Test]
	public async Task AsParamsMatcher_WithLoneArrayMatcher_ShouldMatchTheWholeArray()
	{
		List<object?[]> received = [];
		IParameterMatch<object?[]>? sut = Matchers.AsParamsMatcher<object?>(
			[It.Satisfies<object?[]>(values => values.Length == 2).Do(values => received.Add(values)),]);

		bool[] results = [sut!.Matches(["a", 1,]), sut.Matches(["a",]),];
		sut.InvokeCallbacks(["b", 2,]);

		await That(results).IsEqualTo([true, false,]);
		await That(received).HasCount(1);
	}

	[Test]
	public async Task AsParamsMatcher_WithElementMatchers_ShouldMatchElementByElement()
	{
		List<string> received = [];
		IParameterMatch<object?[]>? sut = Matchers.AsParamsMatcher<object?>(
			[1, It.IsAny<string>().Do(value => received.Add(value)), null, It.IsAny<object?[]>(),]);

		bool[] results =
		[
			sut!.Matches([1, "a", null, Array.Empty<object>(),]),
			sut.Matches([2, "a", null, Array.Empty<object>(),]),
			sut.Matches([1, 2, null, Array.Empty<object>(),]),
			sut.Matches([1, "a", 3, Array.Empty<object>(),]),
			sut.Matches([1, "a", null,]),
		];
		sut.InvokeCallbacks([1, "b", null, Array.Empty<object>(),]);

		await That(results).IsEqualTo([true, false, false, false, false,])
			.Because("a matcher for the array next to other values matches one element");
		await That(received).IsEqualTo(["b",]);
		await That(sut.ToString()).IsEqualTo("[1, It.IsAny<string>(), null, It.IsAny<object[]>()]");
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
