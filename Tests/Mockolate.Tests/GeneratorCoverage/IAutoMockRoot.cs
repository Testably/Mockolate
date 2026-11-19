using System;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Interface return types that the generator mocks transitively for auto-mocking: on a method, a property, an
///     indexer with up to four and one with five keys, wrapped in tasks, as a closed generic, on a method with a
///     ref-struct parameter and on a delegate. <see cref="IAutoMockChild" /> returns the root again, so the walk has
///     to stop at the cycle, and the framework interface is excluded.
/// </summary>
public interface IAutoMockRoot
{
	IAutoMockChild Child { get; }
	IAutoMockChild? NullableChild { get; }
	IAutoMockChild this[int key] { get; set; }
	IAutoMockChild this[int a, int b, int c, int d, int e] { get; }
	IAutoMockChild GetChild(int id);
	Task<IAutoMockChild> GetChildAsync();
	ValueTask<IAutoMockChild> GetChildValueTaskAsync();
	IAutoMockGeneric<int> GetGeneric();
	IServiceProvider GetServiceProvider();
#if NET10_0_OR_GREATER
	IAutoMockChild Consume(Packet packet);
#endif
}

public interface IAutoMockChild
{
	string Name { get; }
	int GetValue();
	IAutoMockRoot GetRoot();
}

public interface IAutoMockGeneric<out T>
{
	T Value { get; }
}

public delegate IAutoMockChild AutoMockChildFactory(int id);
