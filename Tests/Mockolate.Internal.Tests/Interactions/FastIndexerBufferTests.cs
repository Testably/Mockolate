using Mockolate.Interactions;

namespace Mockolate.Internal.Tests.Interactions;

public class FastIndexerBufferTests
{
	[Test]
	public async Task IndexerGet_RoutesThroughTypedBuffer()
	{
		IFastIndexerService sut = IFastIndexerService.CreateMock();

		_ = sut["a"];
		_ = sut["b"];

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int getterId = Mock.FastIndexerBufferTests_IFastIndexerService.MemberId_Indexer_string_Get;
		FastIndexerGetterBuffer<string>? buffer = fast.Buffers[getterId] as FastIndexerGetterBuffer<string>;
		await That(buffer).IsNotNull();
		await That(buffer!.Count).IsEqualTo(2);
	}

	[Test]
	public async Task IndexerSet_RoutesThroughTypedBuffer()
	{
		IFastIndexerService sut = IFastIndexerService.CreateMock();

		sut["a"] = true;
		sut["b"] = false;
		sut["c"] = true;

		FastMockInteractions fast = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int setterId = Mock.FastIndexerBufferTests_IFastIndexerService.MemberId_Indexer_string_Set;
		FastIndexerSetterBuffer<string, bool>? buffer = fast.Buffers[setterId] as FastIndexerSetterBuffer<string, bool>;
		await That(buffer).IsNotNull();
		await That(buffer!.Count).IsEqualTo(3);
	}

	public interface IFastIndexerService
	{
		bool this[string key] { get; set; }
	}
}
