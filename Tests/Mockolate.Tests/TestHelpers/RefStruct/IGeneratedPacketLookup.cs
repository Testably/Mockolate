namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: getter-only indexer keyed by a ref struct. Exercises the standalone
///     <c>IRefStructIndexerGetterSetup&lt;TValue, T&gt;</c> facade.
/// </summary>
public interface IGeneratedPacketLookup
{
	string this[Packet key] { get; }
}
