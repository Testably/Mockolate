using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Internals;

public class MethodRecordsTests
{
	[Test]
	public async Task MethodRecords0_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task MethodRecords1_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int> buffer = new(store, 0);
			return () => buffer.Append("M", 1);
		});

	[Test]
	public async Task MethodRecords1_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task MethodRecords2_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string> buffer = new(store, 0);
			return () => buffer.Append("M", 1, "a");
		});

	[Test]
	public async Task MethodRecords3_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool> buffer = InstallMethod<int, string, bool>(store, 0);
			return () => buffer.Append("M", 1, "a", true);
		});

	[Test]
	public async Task MethodRecords3_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task MethodRecords3_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
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
	public async Task MethodRecords4_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			MethodRecords<int, string, bool, double> buffer =
				InstallMethod<int, string, bool, double>(store, 0);
			return () => buffer.Append("M", 1, "a", true, 1.0);
		});

	[Test]
	public async Task MethodRecords4_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
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
	public async Task MethodRecords4_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
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
