using System.Collections.Generic;
using System.Linq;
using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class ChunkedSlotStorageTests
{
	[Test]
	public async Task Clear_AfterMultipleChunks_ResetsCountAndAllowsRefill()
	{
		const int total = (ChunkedSlotStorage<int>.ChunkSize * 2) + 5;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		buffer.Clear();
		await That(buffer.Count).IsEqualTo(0);

		buffer.Append("Foo", 99);

		await That(buffer.Count).IsEqualTo(1);
		await That(((MethodInvocation<int>)store.Single()).Parameter1).IsEqualTo(99);
	}

	[Test]
	public async Task SlotForWrite_AcrossMultipleChunks_PreservesAllRecords()
	{
		const int total = ChunkedSlotStorage<int>.ChunkSize * 3;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		List<IInteraction> ordered = [..store,];

		await That(ordered).HasCount(total);
		for (int i = 0; i < total; i++)
		{
			await That(((MethodInvocation<int>)ordered[i]).Parameter1).IsEqualTo(i);
		}
	}

	[Test]
	public async Task SlotForWrite_BeyondInitialChunksArrayLength_ExpandsArrayUnderLock()
	{
		const int total = (ChunkedSlotStorage<int>.ChunkSize * 4) + 1;
		FastMockInteractions store = new(1);
		FastMethod1Buffer<int> buffer = store.GetOrCreateBuffer<FastMethod1Buffer<int>>(0,
			static f => new FastMethod1Buffer<int>(f));

		for (int i = 0; i < total; i++)
		{
			buffer.Append("Foo", i);
		}

		List<IInteraction> ordered = [..store,];

		await That(ordered).HasCount(total);
		for (int i = 0; i < total; i++)
		{
			await That(((MethodInvocation<int>)ordered[i]).Parameter1).IsEqualTo(i);
		}
	}
}
