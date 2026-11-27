using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class IndexerRecordsTests
{
	[Test]
	public async Task IndexerGet_RoutesThroughTypedBuffer()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		_ = sut["a"];
		_ = sut["b"];

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.IndexerRecordsTests_IIndexerService.MemberId_Indexer_string_Get;
		IndexerGetterRecords<string> buffer = new(mockInteractions, getterId);
		await That(buffer.Count).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSet_RoutesThroughTypedBuffer()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		sut["a"] = true;
		sut["b"] = false;
		sut["c"] = true;

		MockInteractions mockInteractions = (MockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.IndexerRecordsTests_IIndexerService.MemberId_Indexer_string_Set;
		IndexerSetterRecords<string, bool> buffer = new(mockInteractions, setterId);
		await That(buffer.Count).IsEqualTo(3);
	}

	public interface IIndexerService
	{
		bool this[string key] { get; set; }
	}
}
