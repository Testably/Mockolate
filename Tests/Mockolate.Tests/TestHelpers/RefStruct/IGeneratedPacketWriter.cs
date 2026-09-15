namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: ref-struct parameter plus a non-ref-struct parameter in the same
///     signature. Proves mixed parameters route through <c>RefStructVoidMethodSetup&lt;T1, T2&gt;</c>.
/// </summary>
public interface IGeneratedPacketWriter
{
	void Write(Packet packet, int priority);
}