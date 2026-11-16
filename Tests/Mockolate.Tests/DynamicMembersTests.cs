using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class DynamicMembersTests
{
	[Test]
	public async Task DynamicParameters_ShouldMatchLiteralsAndMatchers()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		List<object> received = [];
		sut.Mock.Setup.Dyn1(It.IsAny<object>()).Do(d => received.Add(d));
		sut.Mock.Setup.Dyn3(It.IsAny<int>(), "a", It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.Dyn3(2, It.Satisfies<object>(v => v is string), "c").Returns(2);
		sut.Mock.Setup.Dyn4(It.IsAny<object>(), 5, It.IsAny<int>(), It.IsAny<string>()).Returns("four");

		sut.Dyn1(42);
		sut.Dyn1("x");
		int result1 = sut.Dyn3(1, "a", "b");
		int result2 = sut.Dyn3(2, "b", "c");
		int result3 = sut.Dyn3(2, 7, "c");
		object result4 = sut.Dyn4("any", 5, 0, "d");
		object result5 = sut.Dyn4("any", 6, 0, "d");

		await That(received).IsEqualTo([42, "x",]);
		await That(result1).IsEqualTo(1);
		await That(result2).IsEqualTo(2);
		await That(result3).IsEqualTo(0);
		await That(result4).IsEqualTo("four");
		await That(result5).IsNull().Because("an unmatched dynamic return falls back to the default value of object");
	}

	[Test]
	public async Task DynamicParameters_ShouldBeVerifiable()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();

		sut.Dyn1(42);
		sut.Dyn2("a", 1);
		sut.Dyn2("a", 2);
		sut.Dyn3(1, 2.5, "c");
		sut.Dyn4(1, "b", 3, "d");
		sut.Dyn5("a", 2, 3, 4, 5);
		sut.DynParams(1, "a", 2);

		await That(sut.Mock.Verify.Dyn1(42)).Once();
		await That(sut.Mock.Verify.Dyn1(It.IsAny<object>())).Once();
		await That(sut.Mock.Verify.Dyn2("a", It.IsAny<int>())).Twice();
		await That(sut.Mock.Verify.Dyn2(It.IsAny<object>(), 2)).Once();
		await That(sut.Mock.Verify.Dyn3(1, 2.5, "c")).Once();
		await That(sut.Mock.Verify.Dyn4(It.IsAny<object>(), "b", 3, It.IsAny<string>())).Once();
		await That(sut.Mock.Verify.Dyn5("a", 2, 3, 4, 5)).Once();
		await That(sut.Mock.Verify.Dyn5(It.IsAny<object>(), 2, It.IsAny<int>(), 4, It.Satisfies<object>(v => v is int))).Once();
		await That(sut.Mock.Verify.Dyn5("b", 2, 3, 4, 5)).Never();
		await That(sut.Mock.Verify.DynParams(Match.AnyParameters())).Once();
	}

	[Test]
	public async Task DynamicOutAndRefParameters_ShouldBeSetUp()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		sut.Mock.Setup.DynTuple(It.IsOut<object>(() => "out"), It.IsRef<object>(r => $"{r}!"))
			.Returns(("value", 1));
		object r = "ref";

		(object Value, int Number) result = sut.DynTuple(out object o, ref r);

		await That(result.Value).IsEqualTo("value");
		await That(result.Number).IsEqualTo(1);
		await That(o).IsEqualTo("out");
		await That(r).IsEqualTo("ref!");
	}

	[Test]
	public async Task DynamicReturnType_ShouldReturnSetupValue()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		sut.Mock.Setup.Get().Returns(3);

		object result = sut.Get();

		await That(result).IsEqualTo(3);
		await That(sut.Mock.Verify.Get()).Once();
	}

	[Test]
	public async Task DynamicProperty_ShouldBeSetUpAndVerified()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		sut.Mock.Setup.Value.InitializeWith("initial");

		object initial = sut.Value;
		sut.Value = 5;
		object updated = sut.Value;

		await That(initial).IsEqualTo("initial");
		await That(updated).IsEqualTo(5);
		await That(sut.Mock.Verify.Value.Set(5)).Once();
		await That(sut.Mock.Verify.Value.Got()).Twice();
	}

	[Test]
	public async Task DynamicIndexers_ShouldBeSetUpAndVerified()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		sut.Mock.Setup["a"].Returns(1);
		sut.Mock.Setup[It.Satisfies<object>(k => k is int)].Returns(2);
		sut.Mock.Setup[1, It.IsAny<object>(), "c", 4, It.IsAny<int>()].Returns(5);

		object result1 = sut["a"];
		object result2 = sut[7];
		object result3 = sut[1, "b", "c", 4, 5];
		sut["k"] = "v";
		sut[1, 2, "c", 4, 5] = "w";

		await That(result1).IsEqualTo(1);
		await That(result2).IsEqualTo(2);
		await That(result3).IsEqualTo(5);
		await That(sut.Mock.Verify["a"].Got()).Once();
		await That(sut.Mock.Verify[It.IsAny<object>()].Got()).Twice();
		await That(sut.Mock.Verify["k"].Set("v")).Once();
		await That(sut.Mock.Verify[1, It.IsAny<object>(), "c", 4, 5].Got()).Once();
		await That(sut.Mock.Verify[1, 2, "c", 4, 5].Set("w")).Once();
	}

	[Test]
	public async Task DynamicMembersOfClass_ShouldForwardToBase()
	{
		DynamicMembersBase sut = DynamicMembersBase.CreateMock();

		object combined = sut.Combine("base", 1);
		object indexed = sut["key"];
		object value = sut.Value;
		sut.Consume(1, "x");

		await That(combined).IsEqualTo("base");
		await That(indexed).IsEqualTo("key");
		await That(value).IsEqualTo(0);
		await That(sut.Mock.Verify.Combine("base", It.IsAny<int>())).Once();
		await That(sut.Mock.Verify.Consume(It.IsAny<int>(), "x")).Once();
	}

	[Test]
	public async Task InheritedDynamicAndObjectOverloads_ShouldBothBeImplemented()
	{
		IDynamicOverloads sut = IDynamicOverloads.CreateMock();

		((IDynamicOverloadA)sut).M(1);
		((IDynamicOverloadB)sut).M(2);

		await That(sut.Mock.Verify.M(It.IsAny<object>())).Once()
			.Because("the second member with the same emitted signature is implemented explicitly and recorded separately");
	}

	[Test]
	public async Task ObjectMemberHidingDynamicMember_ShouldOverrideTheHidingMember()
	{
		DynamicHidingDerived sut = DynamicHidingDerived.CreateMock();

		object result = sut.M(1);

		await That(result).IsEqualTo("derived 1");
		await That(sut.Mock.Verify.M(1)).Once();
	}

	[Test]
	public async Task FailedVerify_ShouldDescribeDynamicArguments()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock();
		sut.Dyn2("a", 1);

		string[] messages =
		[
			Capture(() => sut.Mock.Verify.Dyn1(42).Once()),
			Capture(() => sut.Mock.Verify.Dyn2(It.IsAny<object>(), 2).Once()),
			Capture(() => sut.Mock.Verify.Dyn2("b", It.IsAny<int>()).Once()),
			Capture(() => sut.Mock.Verify.Dyn3(1, null, "c").Once()),
			Capture(() => sut.Mock.Verify.Dyn4(It.IsAny<object>(), "b", 3, It.IsAny<string>()).Once()),
			Capture(() => sut.Mock.Verify.Dyn5("a", 2, 3, 4, It.IsAny<object>()).Once()),
			Capture(() => sut.Mock.Verify.Get().Once()),
			Capture(() => sut.Mock.Verify.Value.Set(5).Once()),
			Capture(() => sut.Mock.Verify["k"].Got().Once()),
		];

		await That(messages).IsEqualTo([
			"Expected that mock invoked method Dyn1(42) exactly once, but it never did.",
			"Expected that mock invoked method Dyn2(It.IsAny<object>(), 2) exactly once, but it never did.",
			"Expected that mock invoked method Dyn2(b, It.IsAny<int>()) exactly once, but it never did.",
			"Expected that mock invoked method Dyn3(1, null, c) exactly once, but it never did.",
			"Expected that mock invoked method Dyn4(It.IsAny<object>(), b, 3, It.IsAny<string>()) exactly once, but it never did.",
			"Expected that mock invoked method Dyn5(a, 2, 3, 4, It.IsAny<object>()) exactly once, but it never did.",
			"Expected that mock invoked method Get() exactly once, but it never did.",
			"Expected that mock set property Value to 5 exactly once, but it never did.",
			"Expected that mock got indexer [k] exactly once, but it never did.",
		]).Because("dynamic arguments are described like object arguments");
	}

	private static string Capture(Action act)
	{
		try
		{
			act();
			return "no exception";
		}
		catch (MockVerificationException exception)
		{
			return exception.Message;
		}
	}
}
