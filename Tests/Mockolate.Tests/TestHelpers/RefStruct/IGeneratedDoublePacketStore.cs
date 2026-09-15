namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: two-key indexer with two ref-struct keys. Exercises arity-2 projection
///     storage where every slot carries a projection.
/// </summary>
public interface IGeneratedDoublePacketStore
{
	string this[Packet k1, Packet k2] { get; set; }
}