namespace Mockolate.Tests.TestHelpers.RefStruct;

/// <summary>
///     Generator-target: two-key indexer mixing a non-ref-struct parameter and a ref-struct key.
///     Exercises arity-2 projection storage with one boxed raw key and one projected key.
/// </summary>
public interface IGeneratedMixedStore
{
	string this[int priority, Packet key] { get; set; }
}
