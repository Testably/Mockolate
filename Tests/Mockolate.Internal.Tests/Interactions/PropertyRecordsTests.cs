using System.Collections.Generic;
using System.Linq;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class PropertyRecordsTests
{
	[Test]
	public async Task PropertyGet_BufferRecordsArePreservedOnEnumeration()
	{
		IPropertyService sut = IPropertyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		sut.Counter = 7;
		_ = sut.Counter;

		IInteraction[] all = ((IMock)sut).MockRegistry.Interactions.ToArray();
		await That(all).HasCount(3);
		await That(all[0]).IsExactly<PropertyGetterAccess>();
		await That(all[1]).IsExactly<PropertySetterAccess<int>>();
		await That(all[2]).IsExactly<PropertyGetterAccess>();
	}

	[Test]
	public async Task PropertyGet_RoutesThroughTypedBuffer()
	{
		IPropertyService sut = IPropertyService.CreateMock();
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.PropertyRecordsTests_IPropertyService.MemberId_Counter_Get;
		PropertyGetterRecords buffer = new(mockInteractions, getterId);
		await That(buffer.Count).IsEqualTo(2);
		await That(mockInteractions.Count).IsEqualTo(2);
	}

	[Test]
	public async Task PropertySet_RoutesThroughTypedBuffer()
	{
		IPropertyService sut = IPropertyService.CreateMock();

		sut.Counter = 7;
		sut.Counter = 9;
		sut.Counter = 11;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.PropertyRecordsTests_IPropertyService.MemberId_Counter_Set;
		PropertySetterRecords<int> buffer = new(mockInteractions, setterId);
		await That(buffer.Count).IsEqualTo(3);
	}

	[Test]
	public async Task SkipInteractionRecording_DoesNotAppendToBuffer()
	{
		MockBehavior behavior = MockBehavior.Default with
		{
			SkipInteractionRecording = true,
		};
		IPropertyService sut = IPropertyService.CreateMock(behavior);
		sut.Mock.Setup.Counter.InitializeWith(42);

		_ = sut.Counter;
		_ = sut.Counter;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		await That(mockInteractions.Count).IsEqualTo(0);
	}

	[Test]
	public async Task PropertyGetterRecords_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
			return () => buffer.Append();
		});

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_RepeatedCallsReturnSameSingleton()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);

		buffer.Append();

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_ShouldReturnTheRecordedAccessForEachRecord()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
		PropertyGetterAccess access = new("P");

		buffer.Append(access);
		buffer.Append(access);

		List<IInteraction> dest = [];
		buffer.AppendBoxed(dest);
		dest.Reverse();

		await That(dest).HasCount(2);
		await That(dest[0]).IsSameAs(dest[1]);
	}

	[Test]
	public async Task PropertyGetterRecords_AppendBoxed_ShouldKeepTheRecordedAccessAcrossSnapshots()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = InstallPropertyGetter(store, 0);
		PropertyGetterAccess access = new("P");

		buffer.Append(access);
		List<IInteraction> firstSnapshot = [];
		buffer.AppendBoxed(firstSnapshot);

		buffer.Append(access);
		List<IInteraction> secondSnapshot = [];
		buffer.AppendBoxed(secondSnapshot);

		await That(firstSnapshot).HasCount(1);
		await That(secondSnapshot).HasCount(2);
		await That(secondSnapshot[0]).IsSameAs(firstSnapshot[0]);
		await That(secondSnapshot[1]).IsSameAs(firstSnapshot[0]);
	}

	[Test]
	public async Task PropertySetterRecords_Append_ShouldRaiseInteractionAdded()
		=> await VerifyRaisesInteractionAdded(store =>
		{
			PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
			return () => buffer.Append("P", 1);
		});

	[Test]
	public async Task PropertySetterRecords_AppendBoxed_CachesAndReusesAlreadyBoxedRecord()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);

		buffer.Append("P", 5);

		List<IInteraction> first = [];
		List<IInteraction> second = [];
		buffer.AppendBoxed(first);
		buffer.AppendBoxed(second);

		await That(first).HasCount(1);
		await That(second).HasCount(1);
		await That(second[0]).IsSameAs(first[0]);
	}

	[Test]
	public async Task PropertySetterRecords_GetUnverifiedInteractions_ShouldSkipMatchedSlots()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 2);
		buffer.Append("P", 3);

		_ = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(2));

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).HasCount(2);
		await That(((PropertySetterAccess<int>)dest[0]).Value).IsEqualTo(1);
		await That(((PropertySetterAccess<int>)dest[1]).Value).IsEqualTo(3);
	}

	[Test]
	public async Task PropertySetterRecords_ConsumeMatching_ShouldMarkSlotsVerified()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = InstallPropertySetter<int>(store, 0);
		buffer.Append("P", 1);
		buffer.Append("P", 1);

		int firstPass = buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1));
		await That(firstPass).IsEqualTo(2);

		List<IInteraction> dest = [..store.GetUnverifiedInteractions(),];

		await That(dest).IsEmpty()
			.Because(
				"ConsumeMatching must write `_storage.VerifiedUnderLock(slot) = true`, otherwise the matched slots stay unverified and a second call would re-count the same records");
	}

	[Test]
	public async Task PropertyGetterRecords_ConsumeMatching_ShouldReturnCount()
	{
		MockInteractions store = new(1);
		PropertyGetterRecords buffer = new(store, 0);

		await That(buffer.ConsumeAll()).IsEqualTo(0);

		buffer.Append();
		buffer.Append();

		await That(buffer.ConsumeAll()).IsEqualTo(2);
	}

	[Test]
	public async Task PropertySetterRecords_ConsumeMatching_ShouldHonorMatcher()
	{
		MockInteractions store = new(1);
		PropertySetterRecords<int> buffer = new(store, 0);

		buffer.Append("Bar", 1);
		buffer.Append("Bar", 2);
		buffer.Append("Bar", 1);

		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>())).IsEqualTo(3);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(1))).IsEqualTo(2);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.Is(99))).IsEqualTo(0);
	}

	private static PropertyGetterRecords InstallPropertyGetter(MockInteractions store, int memberId)
		=> new PropertyGetterRecords(store, memberId);

	private static PropertySetterRecords<T> InstallPropertySetter<T>(MockInteractions store, int memberId)
		=> new PropertySetterRecords<T>(store, memberId);

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

	public interface IPropertyService
	{
		int Counter { get; set; }
	}
}
