namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: arity-5 getter-only indexer mixing three ref-struct keys and two
///     non-ref-struct slots. Exercises the generator-emitted getter-only
///     <c>RefStructIndexerGetterSetup</c> at arity 5+ — the get+set fixture (<see cref="IBigPacketStore5" />)
///     wires a getter through the combined facade rather than the standalone getter-only path.
/// </summary>
public interface IBigPacketLookup5
{
	string this[Packet k1, int a, Packet k2, string b, Packet k3] { get; }
}
