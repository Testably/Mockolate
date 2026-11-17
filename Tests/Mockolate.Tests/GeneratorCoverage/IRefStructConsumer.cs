#if NET10_0_OR_GREATER
namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Isolates the ref-struct setup pipeline: ref-struct method parameters at arity &gt; 4
///     trigger <c>RefStructMethodSetups.g.cs</c>, and ref-struct indexer keys at arity &gt; 4
///     trigger the ref-struct indexer-setup variant.
/// </summary>
public interface IRefStructConsumer
{
	string this[Packet k1, int k2, Packet k3, int k4, Packet k5] { get; set; }
	void Consume5(Packet p1, Packet p2, Packet p3, Packet p4, Packet p5);
	int Compute5(Packet p1, Packet p2, Packet p3, Packet p4, Packet p5);
	void Produce(out Packet packet);
	void Mutate(ref Packet packet);
	void Inspect(ref readonly Packet packet);
	void ProduceSpan(out System.Span<int> span);
	void MutateSpan(ref System.Span<int> span);
	void ProduceReadOnlySpan(out System.ReadOnlySpan<int> span);
	void MutateReadOnlySpan(ref System.ReadOnlySpan<int> span);
	void InspectSpan(ref readonly System.Span<int> span);
}
#endif
