using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class IndexerRecordsTests
{
	[Test]
	public async Task IndexerGet_RoutesThroughTypedBuffer()
	{
		IFastIndexerService sut = IFastIndexerService.CreateMock();

		_ = sut["a"];
		_ = sut["b"];

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.IndexerRecordsTests_IFastIndexerService.MemberId_Indexer_string_Get;
		IndexerGetterRecords<string> buffer = new(fast, getterId);
		await That(buffer.Count).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSet_RoutesThroughTypedBuffer()
	{
		IFastIndexerService sut = IFastIndexerService.CreateMock();

		sut["a"] = true;
		sut["b"] = false;
		sut["c"] = true;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.IndexerRecordsTests_IFastIndexerService.MemberId_Indexer_string_Set;
		IndexerSetterRecords<string, bool> buffer = new(fast, setterId);
		await That(buffer.Count).IsEqualTo(3);
	}

	public interface IFastIndexerService
	{
		bool this[string key] { get; set; }
	}
}
