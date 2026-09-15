namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: arity-5 indexer mixing three ref-struct keys and two non-ref-struct
///     slots. Exercises the generator-emitted <c>RefStructIndexerGetterSetup</c> /
///     <c>RefStructIndexerSetterSetup</c> / <c>RefStructIndexerSetup</c> classes at arity 5+.
/// </summary>
public interface IBigPacketStore5
{
	string this[Packet k1, int a, Packet k2, string b, Packet k3] { get; set; }
}
