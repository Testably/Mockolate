namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: an arity-6 ref-struct-parameter return method that mixes Packet with
///     non-ref-struct types (int, string), verifying <c>allows ref struct</c> is satisfied by any
///     type and that return-side wiring works at extended arity.
/// </summary>
public interface IBigPacketParser
{
	int TryParse(Packet p1, Packet p2, int offset, Packet p4, Packet p5, string format);
}