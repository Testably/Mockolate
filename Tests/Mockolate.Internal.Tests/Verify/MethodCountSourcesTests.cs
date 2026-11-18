using System.Collections.Generic;
using System.Reflection;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Verify;

public class MethodCountSourcesTests
{
	private static readonly Func<object?, object?, string> Expectation = static (_, _) => "expectation";

	[Test]
	public async Task EventCountSource_Matches_ShouldAcceptEveryInteraction()
	{
		FastMockInteractions store = new(1);
		EventRecords buffer = new(store, 0);

		EventCountSource<object> source = new(new object(), store, buffer, null, Expectation);

		await That(source.Matches(new MethodInvocation("M"))).IsTrue();
	}

	[Test]
	public async Task EventCountSource_ShouldReturnSubscribeCount()
	{
		FastMockInteractions store = new(1);
		EventRecords buffer = new(store, 0);
		MethodInfo handler = typeof(MethodCountSourcesTests).GetMethod(
			nameof(EventCountSource_ShouldReturnSubscribeCount))!;
		buffer.AppendSubscription("E", null, handler);
		buffer.AppendSubscription("E", null, handler);

		EventCountSource<object> source = new(new object(), store, buffer, null, Expectation);

		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetter1CountSource_Matches_ShouldCheckTypeAndKey()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = new(store, 0);

		IndexerGetter1CountSource<object, int> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1), null, Expectation);

		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsTrue();
		await That(source.Matches(new IndexerGetterAccess<int>(2))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<long>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerGetter1CountSource_ShouldHonorKeyMatcher()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int> buffer = new(store, 0);
		buffer.Append(1);
		buffer.Append(2);
		buffer.Append(1);

		IndexerGetter1CountSource<object, int> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1), null, Expectation);

		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerGetter2CountSource_Matches_ShouldCheckTypeAndEveryKey()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string> buffer = new(store, 0);

		IndexerGetter2CountSource<object, int, string> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, Expectation);

		await That(source.Matches(new IndexerGetterAccess<int, string>(1, "a"))).IsTrue();
		await That(source.Matches(new IndexerGetterAccess<int, string>(2, "a"))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string>(1, "b"))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerGetter2CountSource_ShouldHonorBothMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string> buffer = new(store, 0);
		buffer.Append(1, "a");
		buffer.Append(2, "a");
		buffer.Append(1, "b");

		IndexerGetter2CountSource<object, int, string> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerGetter3CountSource_Matches_ShouldCheckTypeAndEveryKey()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer = new(store, 0);

		IndexerGetter3CountSource<object, int, string, bool> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, Expectation);

		await That(source.Matches(new IndexerGetterAccess<int, string, bool>(1, "a", true))).IsTrue();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool>(2, "a", true))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool>(1, "b", true))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool>(1, "a", false))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerGetter3CountSource_ShouldHonorAllThreeMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append(1, "a", true);
		buffer.Append(1, "a", false);
		buffer.Append(2, "a", true);

		IndexerGetter3CountSource<object, int, string, bool> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerGetter4CountSource_Matches_ShouldCheckTypeAndEveryKey()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer = new(store, 0);

		IndexerGetter4CountSource<object, int, string, bool, double> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, Expectation);

		await That(source.Matches(new IndexerGetterAccess<int, string, bool, double>(1, "a", true, 0.5))).IsTrue();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool, double>(2, "a", true, 0.5))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool, double>(1, "b", true, 0.5))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool, double>(1, "a", false, 0.5))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int, string, bool, double>(1, "a", true, 1.5))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerGetter4CountSource_ShouldHonorAllFourMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerGetterRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append(1, "a", true, 0.5);
		buffer.Append(1, "a", true, 1.5);
		buffer.Append(2, "a", true, 0.5);

		IndexerGetter4CountSource<object, int, string, bool, double> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerSetter1CountSource_Matches_ShouldCheckTypeKeyAndValue()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = new(store, 0);

		IndexerSetter1CountSource<object, int, string> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, Expectation);

		await That(source.Matches(new IndexerSetterAccess<int, string>(1, "a"))).IsTrue();
		await That(source.Matches(new IndexerSetterAccess<int, string>(2, "a"))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string>(1, "b"))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerSetter1CountSource_ShouldHonorKeyAndValueMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string> buffer = new(store, 0);
		buffer.Append(1, "a");
		buffer.Append(1, "b");
		buffer.Append(2, "a");

		IndexerSetter1CountSource<object, int, string> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerSetter2CountSource_Matches_ShouldCheckTypeKeysAndValue()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool> buffer = new(store, 0);

		IndexerSetter2CountSource<object, int, string, bool> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, Expectation);

		await That(source.Matches(new IndexerSetterAccess<int, string, bool>(1, "a", true))).IsTrue();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool>(2, "a", true))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool>(1, "b", true))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool>(1, "a", false))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerSetter2CountSource_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append(1, "a", true);
		buffer.Append(1, "a", false);
		buffer.Append(2, "a", true);

		IndexerSetter2CountSource<object, int, string, bool> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerSetter3CountSource_Matches_ShouldCheckTypeKeysAndValue()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer = new(store, 0);

		IndexerSetter3CountSource<object, int, string, bool, double> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, Expectation);

		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double>(1, "a", true, 0.5))).IsTrue();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double>(2, "a", true, 0.5))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double>(1, "b", true, 0.5))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double>(1, "a", false, 0.5))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double>(1, "a", true, 1.5))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerSetter3CountSource_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append(1, "a", true, 0.5);
		buffer.Append(1, "a", true, 1.5);
		buffer.Append(2, "a", true, 0.5);

		IndexerSetter3CountSource<object, int, string, bool, double> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task IndexerSetter4CountSource_Matches_ShouldCheckTypeKeysAndValue()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer = new(store, 0);

		IndexerSetter4CountSource<object, int, string, bool, double, char> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			(IParameterMatch<char>)It.Is('x'),
			null, Expectation);

		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(1, "a", true, 0.5, 'x'))).IsTrue();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(2, "a", true, 0.5, 'x'))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(1, "b", true, 0.5, 'x'))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(1, "a", false, 0.5, 'x'))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(1, "a", true, 1.5, 'x'))).IsFalse();
		await That(source.Matches(new IndexerSetterAccess<int, string, bool, double, char>(1, "a", true, 0.5, 'y'))).IsFalse();
		await That(source.Matches(new IndexerGetterAccess<int>(1))).IsFalse();
	}

	[Test]
	public async Task IndexerSetter4CountSource_ShouldHonorAllMatchers()
	{
		FastMockInteractions store = new(1);
		IndexerSetterRecords<int, string, bool, double, char> buffer = new(store, 0);
		buffer.Append(1, "a", true, 0.5, 'x');
		buffer.Append(1, "a", true, 0.5, 'y');
		buffer.Append(2, "a", true, 0.5, 'x');

		IndexerSetter4CountSource<object, int, string, bool, double, char> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			(IParameterMatch<char>)It.Is('x'),
			null, Expectation);

		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task Method0CountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);
		buffer.Append("M");
		buffer.Append("M");

		Method0CountSource<object> source = new(new object(), store, buffer, "M", null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method0CountSource_Matches_ShouldAcceptEveryInteraction()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		Method0CountSource<object> source = new(new object(), store, buffer, "M", null, Expectation);

		await That(source.Matches(new MethodInvocation("M"))).IsTrue();
	}

	[Test]
	public async Task Method0CountSource_ShouldRouteCountAndCountAllThroughBuffer()
	{
		FastMockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);
		buffer.Append("M");
		buffer.Append("M");

		Method0CountSource<object> source = new(new object(), store, buffer, "M", null, Expectation);

		await That(source.CountAll()).IsEqualTo(2);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task Method1CountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);
		buffer.Append("M", 1);
		buffer.Append("M", 2);

		Method1CountSource<object, int> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), null, null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method1CountSource_Matches_ShouldCheckTypeAndParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		Method1CountSource<object, int> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), null, null, Expectation);

		await That(source.Matches(new MethodInvocation<int>("M", 1))).IsTrue();
		await That(source.Matches(new MethodInvocation<int>("M", 2))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method1CountSource_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);
		buffer.Append("M", 1);
		buffer.Append("M", 2);
		buffer.Append("M", 1);

		Method1CountSource<object, int> matchAny = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.IsAny<int>(), null, null, Expectation);
		await That(matchAny.CountAll()).IsEqualTo(3);

		Method1CountSource<object, int> matchOne = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), null, null, Expectation);
		await That(matchOne.Count()).IsEqualTo(2);

		Method1CountSource<object, int> matchNone = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(99), null, null, Expectation);
		await That(matchNone.Count()).IsEqualTo(0);
	}

	[Test]
	public async Task Method1LiteralCountSource_Matches_ShouldCheckTypeAndParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		Method1LiteralCountSource<object, int> source = new(new object(), store, buffer, "M", 1, null, Expectation);

		await That(source.Matches(new MethodInvocation<int>("M", 1))).IsTrue();
		await That(source.Matches(new MethodInvocation<int>("M", 2))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method2CountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);
		buffer.Append("M", 1, "a");
		buffer.Append("M", 2, "b");

		Method2CountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, null, null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method2CountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		Method2CountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, null, null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "a"))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string>("M", 2, "a"))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "b"))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method2CountSource_ShouldHonorBothMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);
		buffer.Append("M", 1, "a");
		buffer.Append("M", 2, "a");
		buffer.Append("M", 1, "b");

		Method2CountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			null, null, null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task Method2LiteralCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		Method2LiteralCountSource<object, int, string> source = new(new object(), store, buffer, "M",
			1, "a", null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "a"))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string>("M", 2, "a"))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "b"))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method3CountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true);
		buffer.Append("M", 2, "b", false);

		Method3CountSource<object, int, string, bool> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, null, null, null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method3CountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		Method3CountSource<object, int, string, bool> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			null, null, null, null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", true))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 2, "a", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "b", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", false))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method3CountSource_ShouldHonorAllThreeMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true);
		buffer.Append("M", 2, "a", false);
		buffer.Append("M", 1, "b", true);

		Method3CountSource<object, int, string, bool> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true),
			null, null, null, null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task Method3LiteralCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		Method3LiteralCountSource<object, int, string, bool> source = new(new object(), store, buffer, "M",
			1, "a", true, null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", true))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 2, "a", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "b", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", false))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method4CountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true, 0.5);
		buffer.Append("M", 2, "b", false, 1.5);

		Method4CountSource<object, int, string, bool, double> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, null, null, null, null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method4CountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		Method4CountSource<object, int, string, bool, double> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			null, null, null, null, null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 0.5))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 2, "a", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "b", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", false, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 1.5))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method4CountSource_ShouldHonorAllFourMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true, 0.5);
		buffer.Append("M", 2, "a", false, 1.5);
		buffer.Append("M", 1, "a", true, 2.5);

		Method4CountSource<object, int, string, bool, double> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.IsAny<double>(),
			null, null, null, null, null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task Method4LiteralCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		Method4LiteralCountSource<object, int, string, bool, double> source = new(new object(), store, buffer, "M",
			1, "a", true, 0.5, null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 0.5))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 2, "a", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "b", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", false, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 1.5))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method1StateCountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);
		buffer.Append("M", 1);
		buffer.Append("M", 2);

		Method1StateCountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), "s", null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method1StateCountSource_Describe_ShouldPassTheStateToTheFormatter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		Method1StateCountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), "x => x > 0", null, Expectation);

		await That(source.Describe(static state => $"M({state})")).IsEqualTo("M(x => x > 0)");
	}

	[Test]
	public async Task Method1StateCountSource_Matches_ShouldCheckTypeAndParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		Method1StateCountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), "s", null, Expectation);

		await That(source.Matches(new MethodInvocation<int>("M", 1))).IsTrue();
		await That(source.Matches(new MethodInvocation<int>("M", 2))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method1StateCountSource_ShouldHonorMatcher()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);
		buffer.Append("M", 1);
		buffer.Append("M", 2);
		buffer.Append("M", 1);

		Method1StateCountSource<object, int, string> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1), "s", null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task Method2StateCountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);
		buffer.Append("M", 1, "a");
		buffer.Append("M", 2, "b");

		Method2StateCountSource<object, int, string, (int, string)> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(1, "a"), null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method2StateCountSource_Describe_ShouldPassTheStateToTheFormatter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		Method2StateCountSource<object, int, string, (int, string)> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(1, "a"), null, Expectation);

		await That(source.Describe(static state => $"M({state.Item1}, {state.Item2})")).IsEqualTo("M(1, a)");
	}

	[Test]
	public async Task Method2StateCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		Method2StateCountSource<object, int, string, (int, string)> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(1, "a"), null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "a"))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string>("M", 2, "a"))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string>("M", 1, "b"))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method2StateCountSource_ShouldHonorBothMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);
		buffer.Append("M", 1, "a");
		buffer.Append("M", 2, "a");
		buffer.Append("M", 1, "b");

		Method2StateCountSource<object, int, string, (int, string)> source = new(new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(1, "a"), null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(1);
	}

	[Test]
	public async Task Method3StateCountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true);
		buffer.Append("M", 2, "b", false);

		Method3StateCountSource<object, int, string, bool, (int, string, bool)> source = new(new object(), store,
			buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(1, "a", true), null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method3StateCountSource_Describe_ShouldPassTheStateToTheFormatter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		Method3StateCountSource<object, int, string, bool, (int, string, bool)> source = new(new object(), store,
			buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(1, "a", true), null, Expectation);

		await That(source.Describe(static state => $"M({state.Item1}, {state.Item2}, {state.Item3})"))
			.IsEqualTo("M(1, a, True)");
	}

	[Test]
	public async Task Method3StateCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		Method3StateCountSource<object, int, string, bool, (int, string, bool)> source = new(new object(), store,
			buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(1, "a", true), null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", true))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 2, "a", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "b", true))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool>("M", 1, "a", false))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method3StateCountSource_ShouldHonorAllThreeMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true);
		buffer.Append("M", 2, "a", false);
		buffer.Append("M", 1, "b", true);

		Method3StateCountSource<object, int, string, bool, (int, string, bool)> source = new(new object(), store,
			buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true),
			(1, "a", true), null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task Method4StateCountSource_CountAll_ShouldMarkSlotsVerified()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true, 0.5);
		buffer.Append("M", 2, "b", false, 1.5);

		Method4StateCountSource<object, int, string, bool, double, (int, string, bool, double)> source = new(
			new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			(1, "a", true, 0.5), null, Expectation);
		await That(source.CountAll()).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];
		await That(dest).IsEmpty();
	}

	[Test]
	public async Task Method4StateCountSource_Describe_ShouldPassTheStateToTheFormatter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		Method4StateCountSource<object, int, string, bool, double, (int, string, bool, string)> source = new(
			new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			(1, "a", true, "d"), null, Expectation);

		await That(source.Describe(static state
				=> $"M({state.Item1}, {state.Item2}, {state.Item3}, {state.Item4})"))
			.IsEqualTo("M(1, a, True, d)");
	}

	[Test]
	public async Task Method4StateCountSource_Matches_ShouldCheckTypeAndEveryParameter()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		Method4StateCountSource<object, int, string, bool, double, (int, string, bool, double)> source = new(
			new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.Is(0.5),
			(1, "a", true, 0.5), null, Expectation);

		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 0.5))).IsTrue();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 2, "a", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "b", true, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", false, 0.5))).IsFalse();
		await That(source.Matches(new MethodInvocation<int, string, bool, double>("M", 1, "a", true, 1.5))).IsFalse();
		await That(source.Matches(new MethodInvocation("M"))).IsFalse();
	}

	[Test]
	public async Task Method4StateCountSource_ShouldHonorAllFourMatchers()
	{
		FastMockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);
		buffer.Append("M", 1, "a", true, 0.5);
		buffer.Append("M", 2, "a", false, 1.5);
		buffer.Append("M", 1, "a", true, 2.5);

		Method4StateCountSource<object, int, string, bool, double, (int, string, bool, double)> source = new(
			new object(), store, buffer, "M",
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"),
			(IParameterMatch<bool>)It.Is(true),
			(IParameterMatch<double>)It.IsAny<double>(),
			(1, "a", true, 0.5), null, Expectation);

		await That(source.CountAll()).IsEqualTo(3);
		await That(source.Count()).IsEqualTo(2);
	}

	[Test]
	public async Task PropertyGetterCountSource_Matches_ShouldAcceptEveryInteraction()
	{
		FastMockInteractions store = new(1);
		PropertyGetterRecords buffer = new(store, 0);

		PropertyGetterCountSource<object> source = new(new object(), store, buffer, null, Expectation);

		await That(source.Matches(new PropertyGetterAccess("P"))).IsTrue();
	}

	[Test]
	public async Task PropertyGetterCountSource_ShouldReturnRecordedCount()
	{
		FastMockInteractions store = new(1);
		PropertyGetterRecords buffer = new(store, 0);
		buffer.Append();
		buffer.Append();
		buffer.Append();

		PropertyGetterCountSource<object> source = new(new object(), store, buffer, null, Expectation);

		await That(source.Count()).IsEqualTo(3);
	}

	[Test]
	public async Task PropertySetterCountSource_Matches_ShouldCheckTypeAndValue()
	{
		FastMockInteractions store = new(1);
		PropertySetterRecords<int> buffer = new(store, 0);

		PropertySetterCountSource<object, int> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1), null, Expectation);

		await That(source.Matches(new PropertySetterAccess<int>("P", 1))).IsTrue();
		await That(source.Matches(new PropertySetterAccess<int>("P", 2))).IsFalse();
		await That(source.Matches(new PropertyGetterAccess("P"))).IsFalse();
	}

	[Test]
	public async Task PropertySetterCountSource_ShouldHonorValueMatcher()
	{
		FastMockInteractions store = new(1);
		PropertySetterRecords<int> buffer = new(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 1);

		PropertySetterCountSource<object, int> source = new(new object(), store, buffer,
			(IParameterMatch<int>)It.Is(1), null, Expectation);

		await That(source.Count()).IsEqualTo(2);
	}
}
