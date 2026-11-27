using System.Threading;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Interactions;

public class RecordsConcurrencyTests
{
	[Test]
	public async Task MethodRecords1_ConcurrentAppend_ShouldRecordAllInteractions(CancellationToken cancellationToken)
	{
		const int writerCount = 8;
		const int appendsPerWriter = 1000;

		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		Task[] writers = new Task[writerCount];
		for (int w = 0; w < writerCount; w++)
		{
			int seed = w * appendsPerWriter;
			writers[w] = Task.Run(() =>
			{
				for (int i = 0; i < appendsPerWriter; i++)
				{
					buffer.Append("Foo", seed + i);
				}
			}, cancellationToken);
		}

		await Task.WhenAll(writers);

		await That(buffer.Count).IsEqualTo(writerCount * appendsPerWriter);
		await That(buffer.ConsumeMatching((IParameterMatch<int>)It.IsAny<int>()))
			.IsEqualTo(writerCount * appendsPerWriter);
	}

	[Test]
	public async Task MethodRecords1_InteractionAdded_ShouldFireWhenSubscribed()
	{
		MockInteractions store = new(1);
		MethodRecords<int> buffer = new(store, 0);

		int invocations = 0;

		void Handler(object? sender, EventArgs e)
		{
			invocations++;
		}

		store.InteractionAdded += Handler;
		try
		{
			buffer.Append("Foo", 1);
			buffer.Append("Foo", 2);
		}
		finally
		{
			store.InteractionAdded -= Handler;
		}

		buffer.Append("Foo", 3);

		await That(invocations).IsEqualTo(2);
	}
}
