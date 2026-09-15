namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: ref-struct-keyed indexer with only a setter. Exercises the
///     setter-only <c>IRefStructIndexerSetterSetup&lt;TValue, T&gt;</c> facade.
/// </summary>
public interface IGeneratedPacketSetter
{
	string this[Packet key] { set; }
}