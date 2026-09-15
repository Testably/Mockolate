namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: ref-struct-keyed indexer with both a getter and a setter. Exercises the
///     combined <c>IRefStructIndexerSetup&lt;TValue, T&gt;</c> facade.
/// </summary>
public interface IGeneratedPacketStore
{
	string this[Packet key] { get; set; }
}