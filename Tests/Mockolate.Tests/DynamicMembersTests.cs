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
		await That(sut.Mock.Verify.Dyn5(It.IsAny<object>(), It.Is(2), It.IsAny<int>(), It.Is(4), It.Satisfies<object>(v => v is int))).Once();
		await That(sut.Mock.Verify.Dyn5("b", 2, 3, 4, 5)).Never();
		await That(sut.Mock.Verify.DynParams(Match.AnyParameters())).Once();
		await That(sut.Mock.Verify.DynParams(1, It.IsAny<object[]>())).Once()
			.Because("a matcher for the array type matches the whole params array instead of one element");
		await That(sut.Mock.Verify.DynParams(1, "a", 2)).Once();
		await That(sut.Mock.Verify.DynParams(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(2))).Once();
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
		sut.Mock.Setup[It.Is(1), It.IsAny<object>(), It.Is("c"), It.Is<object>(4), It.IsAny<int>()].Returns(5);

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
		await That(sut.Mock.Verify[It.Is(1), It.IsAny<object>(), It.Is("c"), It.Is<object>(4), It.Is(5)].Got()).Once();
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

	[Test]
	public async Task InterfaceClosedOverDynamic_ShouldBeSetUpInvokedAndVerified()
	{
		IDynamicRepository<dynamic> sut = IDynamicRepository<dynamic>.CreateMock();
		List<dynamic> values = [1, "two",];
		object? raised = null;
		sut.Mock.Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("value");
		sut.Mock.Setup.Value.InitializeWith("initial");
		sut.Mock.Setup["a"].Returns(1);
		sut.Changed += (_, e) => raised = e;

		object result = sut.Get("key", values);
		object unmatched = sut.Get("other", values);
		object initial = sut.Value;
		sut.Value = 5;
		object indexed = sut["a"];
		sut.Add(42);
		sut.Mock.Raise.Changed(sut, "event");

		await That(result).IsEqualTo("value");
		await That(unmatched).IsNull();
		await That(initial).IsEqualTo("initial");
		await That(indexed).IsEqualTo(1);
		await That(raised).IsEqualTo("event");
		await That(sut.Mock.Verify.Get("key", values)).Once();
		await That(sut.Mock.Verify.Get(It.IsAny<object>(), It.IsAny<List<dynamic>>())).Twice();
		await That(sut.Mock.Verify.Value.Set(5)).Once();
		await That(sut.Mock.Verify["a"].Got()).Once();
		await That(sut.Mock.Verify.Add(42)).Once();
		await That(sut.Mock.Verify.Add("other")).Never();
	}

	[Test]
	public async Task InterfaceClosedOverDynamic_ShouldShareTheMockOfTheInterfaceClosedOverObject()
	{
		IDynamicRepository<object> objectMock = IDynamicRepository<object>.CreateMock();
		IDynamicRepository<dynamic> dynamicMock = IDynamicRepository<dynamic>.CreateMock();
		IDynamicMembers objectCombination = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<object>>();
		IDynamicMembers dynamicCombination = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<dynamic>>();

		await That(dynamicMock.GetType()).IsEqualTo(objectMock.GetType())
			.Because("both types are identical, so a mock for each would make every call to CreateMock ambiguous");
		await That(dynamicCombination.GetType()).IsEqualTo(objectCombination.GetType())
			.Because("both types are identical, so a mock for each would make every call to Implementing ambiguous");
	}

	[Test]
	public async Task InterfaceClosedOverNestedDynamic_ShouldBeSetUpInvokedAndVerified()
	{
		IDynamicRepository<List<dynamic>> listSut = IDynamicRepository<List<dynamic>>.CreateMock();
		IDynamicRepository<(dynamic, int)[]> arraySut = IDynamicRepository<(dynamic, int)[]>.CreateMock();
		List<dynamic> list = [1, "two",];
		(dynamic, int)[] array = [("a", 1),];
		listSut.Mock.Setup.Get(list, It.IsAny<List<List<dynamic>>>()).Returns(list);
		arraySut.Mock.Setup.Value.InitializeWith(array);

		List<dynamic> listResult = listSut.Get(list, []);
		listSut.Add(list);
		(dynamic, int)[] arrayResult = arraySut.Value;
		arraySut.Add(array);

		await That(listResult).IsSameAs(list);
		await That(arrayResult).IsSameAs(array);
		await That(listSut.Mock.Verify.Add(list)).Once();
		await That(arraySut.Mock.Verify.Add(It.IsAny<(dynamic, int)[]>())).Once();
	}

	[Test]
	public async Task ClassClosedOverDynamic_ShouldForwardToBaseAndBeVerified()
	{
		DynamicRepositoryBase<dynamic> sut = DynamicRepositoryBase<dynamic>.CreateMock("initial");

		object initial = sut.Initial;
		object echoed = sut.Echo("echo");
		sut.Mock.Setup.Echo(It.IsAny<object>()).Returns("setup");
		object setup = sut.Echo(1);
		sut.Consume(2, [3,]);

		await That(initial).IsEqualTo("initial");
		await That(echoed).IsEqualTo("echo");
		await That(setup).IsEqualTo("setup");
		await That(sut.Mock.Verify.Echo("echo")).Once();
		await That(sut.Mock.Verify.Consume(2, It.IsAny<IEnumerable<dynamic>>())).Once();
	}

	[Test]
	public async Task DelegateClosedOverDynamic_ShouldBeSetUpInvokedAndVerified()
	{
		DynamicFactory<dynamic> sut = DynamicFactory<dynamic>.CreateMock();
		sut.Mock.Setup("a", It.IsAny<List<dynamic>>()).Returns("created");

		object result = sut("a", []);

		await That(result).IsEqualTo("created");
		await That(sut.Mock.Verify("a", It.IsAny<List<dynamic>>())).Once();
		await That(sut.Mock.Verify("b", It.IsAny<List<dynamic>>())).Never();
	}

	[Test]
	public async Task AdditionalInterfaceClosedOverDynamic_ShouldBeSetUpInvokedAndVerified()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<dynamic>>();
		DynamicRepositoryBase<dynamic> classSut = DynamicRepositoryBase<dynamic>.CreateMock(1)
			.Implementing<IDynamicRepository<List<dynamic>>>();
		List<dynamic> list = [1,];
		sut.Mock.As<IDynamicRepository<dynamic>>().Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("value");

		object result = ((IDynamicRepository<dynamic>)sut).Get("key", []);
		((IDynamicRepository<List<dynamic>>)classSut).Add(list);

		await That(result).IsEqualTo("value");
		await That(sut.Mock.As<IDynamicRepository<dynamic>>().Verify.Get("key", It.IsAny<List<dynamic>>())).Once();
		await That(classSut.Mock.As<IDynamicRepository<List<dynamic>>>().Verify.Add(list)).Once();
	}

	[Test]
	public async Task InterfaceClosedOverObject_ShouldShareTheAsExtensionsOfTheInterfaceClosedOverDynamic()
	{
		IDynamicMembers sut = IDynamicMembers.CreateMock().Implementing<IDynamicRepository<dynamic>>();
		IDynamicRepository<object> reversedSut = IDynamicRepository<object>.CreateMock().Implementing<IDynamicMembers>();
		sut.Mock.As<IDynamicRepository<object>>().Setup.Get("key", It.IsAny<List<object>>()).Returns("value");
		reversedSut.Mock.As<IDynamicMembers>().Setup.Dyn3(1, "b", "c").Returns(3);

		object result = ((IDynamicRepository<dynamic>)sut).Get("key", []);
		int reversedResult = ((IDynamicMembers)reversedSut).Dyn3(1, "b", "c");

		await That(result).IsEqualTo("value");
		await That(reversedResult).IsEqualTo(3)
			.Because("both combinations use the same pair of types, so an As<T> extension for each spelling would be a duplicate");
		await That(reversedSut.Mock.As<IDynamicMembers>().Verify.Dyn3(1, "b", "c")).Once();
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
