using System.Collections.Generic;
using Mockolate.Parameters;

namespace Mockolate.Tests.Parameters;

public sealed class ParamsArrayParameterMatchTests
{
	[Test]
	public async Task Matches_WhenLengthMatchesAndAllElementsSatisfy_ShouldReturnTrue()
	{
		StubParameter first = new(true, "first");
		StubParameter second = new(true, "second");
		ParamsArrayParameterMatch<int> sut = new(first, second);

		bool result = sut.Matches([1, 2,]);

		await That(result).IsTrue();
		await That(first.MatchedValues).IsEqualTo([1,]);
		await That(second.MatchedValues).IsEqualTo([2,]);
	}

	[Test]
	public async Task Matches_WhenAnElementFails_ShouldReturnFalse()
	{
		ParamsArrayParameterMatch<int> sut = new(new StubParameter(true, "first"), new StubParameter(false, "second"));

		bool result = sut.Matches([1, 2,]);

		await That(result).IsFalse();
	}

	[Test]
	public async Task Matches_WhenLengthDiffers_ShouldReturnFalseWithoutInvokingMatchers()
	{
		StubParameter only = new(true, "only");
		ParamsArrayParameterMatch<int> sut = new(only);

		bool result = sut.Matches([1, 2,]);

		await That(result).IsFalse();
		await That(only.MatchedValues).IsEmpty();
	}

	[Test]
	public async Task Matches_WhenValueIsNull_ShouldReturnFalse()
	{
		ParamsArrayParameterMatch<int> sut = new(new StubParameter(true, "only"));

		bool result = sut.Matches(null!);

		await That(result).IsFalse();
	}

	[Test]
	public async Task Matches_WhenMatcherElementIsNull_ShouldReturnFalseWithoutThrowing()
	{
		ParamsArrayParameterMatch<int> sut = new(new StubParameter(true, "first"), null!);

		bool result = sut.Matches([1, 2,]);

		await That(result).IsFalse();
	}

	[Test]
	public async Task InvokeCallbacks_WhenLengthMatches_ShouldInvokeEachMatcher()
	{
		StubParameter first = new(true, "first");
		StubParameter second = new(true, "second");
		ParamsArrayParameterMatch<int> sut = new(first, second);

		sut.InvokeCallbacks([1, 2,]);

		await That(first.InvokedValues).IsEqualTo([1,]);
		await That(second.InvokedValues).IsEqualTo([2,]);
	}

	[Test]
	public async Task InvokeCallbacks_WhenLengthDiffers_ShouldNotInvokeMatchers()
	{
		StubParameter only = new(true, "only");
		ParamsArrayParameterMatch<int> sut = new(only);

		sut.InvokeCallbacks([1, 2,]);

		await That(only.InvokedValues).IsEmpty();
	}

	[Test]
	public async Task InvokeCallbacks_WhenValueIsNull_ShouldNotInvokeMatchers()
	{
		StubParameter only = new(true, "only");
		ParamsArrayParameterMatch<int> sut = new(only);

		sut.InvokeCallbacks(null!);

		await That(only.InvokedValues).IsEmpty();
	}

	[Test]
	public async Task InvokeCallbacks_WhenMatcherElementIsNull_ShouldSkipItWithoutThrowing()
	{
		StubParameter first = new(true, "first");
		ParamsArrayParameterMatch<int> sut = new(first, null!);

		sut.InvokeCallbacks([1, 2,]);

		await That(first.InvokedValues).IsEqualTo([1,]);
	}

	[Test]
	public async Task ToString_ShouldRenderMatchersInOrder()
	{
		ParamsArrayParameterMatch<int> sut = new(new StubParameter(true, "first"), new StubParameter(true, "second"));

		await That(sut.ToString()).IsEqualTo("[first, second]");
	}

	[Test]
	public async Task ToString_WithNullMatcher_ShouldRenderNullToken()
	{
		ParamsArrayParameterMatch<int> sut = new(new StubParameter(true, "first"), null!);

		await That(sut.ToString()).IsEqualTo("[first, null]");
	}

	private sealed class StubParameter(bool matches, string label) : IParameter<int>
	{
		public List<int> MatchedValues { get; } = [];
		public List<int> InvokedValues { get; } = [];

		public bool Matches(object? value)
		{
			MatchedValues.Add((int)value!);
			return matches;
		}

		public void InvokeCallbacks(object? value) => InvokedValues.Add((int)value!);

		public override string ToString() => label;
	}
}
