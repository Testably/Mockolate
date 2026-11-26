using System;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Interfaces inside tuple return values that get auto-mocks: in value tuples and reference tuples, twice in one
///     tuple, nested, beyond the seventh element, with one or no element, wrapped in a task or around a task, on
///     methods with and without parameters, on a property and on indexers.
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
	ValueTuple<IAutoMockTupleElement> GetSingle();
	ValueTuple GetNothing();
	(int Stock, IAutoMockTupleElement Element) GetPairFor(string type);
	Tuple<int, IAutoMockTupleElement> GetReferenceTupleFor(string type);
	(int Stock, Task<IAutoMockTupleElement> Element) this[long key] { get; }
}

public interface IAutoMockTupleElement
{
	int GetValue();
}
