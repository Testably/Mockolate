namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: arity-5 setter-only indexer mixing three ref-struct keys and two
///     non-ref-struct slots. Exercises the generator-emitted setter-only
///     <c>RefStructIndexerSetterSetup</c> at arity 5+ — the get+set fixture (<see cref="IBigPacketStore5" />)
///     does not cover the setter-only emit branch because it always wires a getter.
/// </summary>
public interface IBigPacketSetter5
{
	string this[Packet k1, int a, Packet k2, string b, Packet k3] { set; }
}