namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: indexer keyed by a ref struct. Not wired up in commit E — both
///     accessors throw <c>NotSupportedException</c>. The analyzer will flag this pattern at
///     compile time in commit F.
/// </summary>
public interface IGeneratedPacketLookup
{
	string this[Packet key] { get; }
}