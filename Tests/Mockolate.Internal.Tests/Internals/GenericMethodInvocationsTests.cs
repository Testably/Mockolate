using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Internals;

public sealed class GenericMethodInvocationsTests
{
	[Test]
	[Arguments("Ns.IFoo.Send<T>", "Ns.IFoo.Send<")]
	[Arguments("Ns.IFoo<int>.Send<T1, T2>", "Ns.IFoo<int>.Send<")]
	public async Task GetNamePrefix_ShouldEndAfterTheLastAngleBracket(string genericMethodName, string expected)
	{
		string prefix = GenericMethodInvocations.GetNamePrefix(genericMethodName);

		await That(prefix).IsEqualTo(expected);
	}

	[Test]
	public async Task TryGetInvocation_ShouldRequireTheNamePrefixAndTheParameterCount()
	{
		MethodInvocation<int, string> invocation = new("Ns.IFoo.Send<System.Int32>", 1, "a");

		bool matching = GenericMethodInvocations.TryGetInvocation(invocation, "Ns.IFoo.Send<", 2,
			out IMethodInvocationParameters result);
		bool otherName = GenericMethodInvocations.TryGetInvocation(invocation, "Ns.IFoo.Get<", 2, out _);
		bool otherCount = GenericMethodInvocations.TryGetInvocation(invocation, "Ns.IFoo.Send<", 1, out _);
		bool noMethod = GenericMethodInvocations.TryGetInvocation(new PropertyGetterAccess("Ns.IFoo.Send<"),
			"Ns.IFoo.Send<", 0, out _);

		await That(matching).IsTrue();
		await That(result).IsSameAs(invocation);
		await That(otherName).IsFalse();
		await That(otherCount).IsFalse();
		await That(noMethod).IsFalse();
	}

	[Test]
	public async Task TryGetValue_ShouldCastTheArgument()
	{
		MethodInvocation<object?, object?> invocation = new("M<T>", "a", null);

		bool asString = GenericMethodInvocations.TryGetValue(invocation, 0, out string? text);
		bool asInt = GenericMethodInvocations.TryGetValue(invocation, 0, out int _);
		bool nullAsString = GenericMethodInvocations.TryGetValue(invocation, 1, out string? nullText);
		bool nullAsInt = GenericMethodInvocations.TryGetValue(invocation, 1, out int _);

		await That(asString).IsTrue();
		await That(text).IsEqualTo("a");
		await That(asInt).IsFalse();
		await That(nullAsString).IsTrue();
		await That(nullText).IsNull();
		await That(nullAsInt).IsFalse().Because("a value type cannot be null");
	}

	[Test]
	public async Task Matches_WithIParametersMatch_ShouldForwardTheArguments()
	{
		RecordingParametersMatch matcher = new(true);
		MethodInvocation<int, string> invocation = new("M<T>", 1, "a");

		bool result = GenericMethodInvocations.Matches(matcher, invocation, "p1", "p2");

		await That(result).IsTrue();
		await That(matcher.Received).HasCount(1);
		await That(matcher.Received[0]).IsEqualTo(new object?[] { 1, "a", });
	}

	[Test]
	public async Task Matches_WithINamedParametersMatch_ShouldForwardTheNamedArguments()
	{
		RecordingNamedParametersMatch matcher = new(false);
		MethodInvocation<int, string> invocation = new("M<T>", 1, "a");

		bool result = GenericMethodInvocations.Matches(matcher, invocation, "p1", "p2");

		await That(result).IsFalse();
		await That(matcher.Received).HasCount(1);
		await That(matcher.Received[0]).IsEqualTo(new[] { ("p1", (object?)1), ("p2", (object?)"a"), });
	}

	[Test]
	public async Task Matches_WithUnknownIParameters_ShouldReturnTrue()
	{
		MethodInvocation<int> invocation = new("M<T>", 1);

		bool result = GenericMethodInvocations.Matches(Match.AnyParameters(), invocation, "p1");

		await That(result).IsTrue();
	}

	private sealed class RecordingParametersMatch(bool result) : IParameters, IParametersMatch
	{
		public List<object?[]> Received { get; } = new();

		public bool Matches(ReadOnlySpan<object?> values)
		{
			Received.Add(values.ToArray());
			return result;
		}
	}

	private sealed class RecordingNamedParametersMatch(bool result) : IParameters, INamedParametersMatch
	{
		public List<(string, object?)[]> Received { get; } = new();

		public bool Matches(ReadOnlySpan<(string, object?)> values)
		{
			Received.Add(values.ToArray());
			return result;
		}
	}
}
