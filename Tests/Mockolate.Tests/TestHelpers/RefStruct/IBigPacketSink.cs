namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: an arity-5 ref-struct-parameter void method. The runtime types for
///     arity 1-4 are hand-written; arity 5+ are generator-emitted into
///     <c>RefStructMethodSetups.g.cs</c>.
/// </summary>
public interface IBigPacketSink
{
	void Absorb(Packet p1, Packet p2, Packet p3, Packet p4, Packet p5);
}