using System;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Interfaces inside tuple return values that get auto-mocks: in value tuples and reference tuples, twice in one
///     tuple, nested, beyond the seventh element, wrapped in a task and on a property and indexers.
///     <see cref="IAutoMockTupleElement" /> only appears inside tuples, so the generator has to discover it there.
/// </summary>
public interface IAutoMockTuples
{
	(int Stock, IAutoMockTupleElement Element) Pair { get; }
	(int Stock, IAutoMockTupleElement Element) this[int key] { get; }
	Tuple<int, IAutoMockTupleElement> this[string key] { get; }
	(IAutoMockTupleElement First, IAutoMockTupleElement Second) GetTwo();
	((int Stock, IAutoMockTupleElement Element) Inner, IAutoMockTupleElement Outer) GetNested();
	Task<(int Stock, IAutoMockTupleElement Element)> GetPairAsync();
	Tuple<int, IAutoMockTupleElement> GetReferenceTuple();
	Tuple<int, int, int, int, int, int, int, Tuple<IAutoMockTupleElement>> GetLongReferenceTuple();
	(int, int, int, int, int, int, int, IAutoMockTupleElement Eighth) GetLongValueTuple();
}

public interface IAutoMockTupleElement
{
	int GetValue();
}
