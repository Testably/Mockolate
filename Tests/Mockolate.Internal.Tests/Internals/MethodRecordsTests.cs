using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Internals;

public class MethodRecordsTests
{
	[Test]
	public async Task AppendBoxed_WithoutParameters_ShouldReuseTheBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation(r.Name)` cache in Method0 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		buffer.Append("M");

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task Append_With1Parameter_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int> buffer = new(store, 0);
			return () => buffer.Append("M", 1);
		});

	[Test]
	public async Task AppendBoxed_With1Parameter_ShouldReuseTheBoxedRecord()
	{
		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		buffer.Append("M", 1);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task Append_With2Parameters_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string> buffer = new(store, 0);
			return () => buffer.Append("M", 1, "a");
		});

	[Test]
	public async Task Append_With3Parameters_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
			return () => buffer.Append("M", 1, "a", true);
		});

	[Test]
	public async Task AppendBoxed_With3Parameters_ShouldReuseTheBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3>(...)` cache in Method3 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);

		buffer.Append("M", 1, "a", true);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task GetUnverifiedInteractions_With3Parameters_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
		buffer.Append("M", 0, "a", true);
		buffer.Append("M", 1, "b", false);
		buffer.Append("M", 2, "c", true);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task Append_With4Parameters_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool, double> buffer =
				InstallMethod<int, string, bool, double>(store, 0);
			return () => buffer.Append("M", 1, "a", true, 1.0);
		});

	[Test]
	public async Task AppendBoxed_With4Parameters_ShouldReuseTheBoxedRecord()
	{
		// Pins the `r.Boxed ??= new MethodInvocation<T1,T2,T3,T4>(...)` cache in Method4 AppendBoxed.
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer =
			InstallMethod<int, string, bool, double>(store, 0);

		buffer.Append("M", 1, "a", true, 1.0);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task GetUnverifiedInteractions_With4Parameters_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer =
			InstallMethod<int, string, bool, double>(store, 0);
		buffer.Append("M", 0, "a", true, 0.5);
		buffer.Append("M", 1, "b", false, 1.5);
		buffer.Append("M", 2, "c", true, 2.5);

		_ = buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.Is(1.5));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((MethodInvocation<int, string, bool, double>)dest[0]).Parameter1).IsEqualTo(0);
		await That(((MethodInvocation<int, string, bool, double>)dest[1]).Parameter1).IsEqualTo(2);
	}

	[Test]
	public async Task ConsumeAll_WithoutParameters_ShouldReturnTheNumberOfRecords()
	{
		MockInteractions store = new(1);
		MethodRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append("Foo");
		buffer.Append("Foo");
		buffer.Append("Foo");

		await That(buffer.ConsumeAll()).IsEqualTo(3);
	}

	[Test]
	public async Task ConsumeAll_With1Parameter_WhenEmpty_ShouldReturnZero()
	{
		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0)
			.Because(
				"the `slot < n` loop bound in Method1 ConsumeAll must keep an empty buffer out of the loop; entering it at slot 0 would index past the verified flags, which are sized to the published slots");
	}

	[Test]
	public async Task ConsumeMatching_With1Parameter_ShouldHonorMatcher()
	{
		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		buffer.Append("Foo", 1);
		buffer.Append("Foo", 2);
		buffer.Append("Foo", 3);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2))).IsEqualTo(1);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeAll_With2Parameters_WhenEmpty_ShouldReturnZero()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeMatching_With2Parameters_ShouldHonorAllMatchers()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a");
		buffer.Append("Foo", 1, "b");
		buffer.Append("Foo", 2, "a");

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.Is<string>("a"))).IsEqualTo(1);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>())).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeAll_With3Parameters_WhenEmpty_ShouldReturnZero()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeMatching_With3Parameters_ShouldHonorAllMatchers()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a", true);
		buffer.Append("Foo", 2, "a", false);
		buffer.Append("Foo", 1, "b", true);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(true))).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(false))).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.Is<string>("b"),
			(IParameterMatch<bool>)It.Is(false))).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeAll_With4Parameters_WhenEmpty_ShouldReturnZero()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		int consumed = buffer.ConsumeAll();

		await That(consumed).IsEqualTo(0);
	}

	[Test]
	public async Task ConsumeMatching_With4Parameters_ShouldHonorAllMatchers()
	{
		MockInteractions store = new(1);
		MethodRecords<int, string, bool, double> buffer = new(store, 0);

		buffer.Append("Foo", 1, "a", true, 1.0);
		buffer.Append("Foo", 1, "b", true, 2.0);
		buffer.Append("Foo", 2, "a", false, 3.0);

		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(2);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(99),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.Is(1),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.Is(false),
			(IParameterMatch<double>)It.IsAny<double>())).IsEqualTo(0);
		await That(buffer.ConsumeMatching(
			(IParameterMatch<int>)It.IsAny<int>(),
			(IParameterMatch<string>)It.IsAny<string>(),
			(IParameterMatch<bool>)It.IsAny<bool>(),
			(IParameterMatch<double>)It.Is(99.0))).IsEqualTo(0);
	}

	private static MethodRecords<T1, T2, T3> InstallMethod<T1, T2, T3>(MockInteractions store, int memberId)
		=> new MethodRecords<T1, T2, T3>(store, memberId);

	private static MethodRecords<T1, T2, T3, T4> InstallMethod<T1, T2, T3, T4>(MockInteractions store, int memberId)
		=> new MethodRecords<T1, T2, T3, T4>(store, memberId);

	private static async Task VerifyRaisesInteractionAdded(Func<MockInteractions, Action> appendFactory)
	{
		MockInteractions store = new(1);
		Action append = appendFactory(store);
		int invocations = 0;
		EventHandler handler = (_, _) => invocations++;
		store.InteractionAdded += handler;

		append();
		append();

		store.InteractionAdded -= handler;

		await That(invocations).IsEqualTo(2);
	}
}
