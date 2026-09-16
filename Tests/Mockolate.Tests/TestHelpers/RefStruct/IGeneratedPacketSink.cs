namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target interface: single void method with a single ref-struct parameter. This
///     is the smallest non-trivial shape that exercises the ref-struct setup pipeline.
/// </summary>
public interface IGeneratedPacketSink
{
	void Consume(Packet packet);
}
