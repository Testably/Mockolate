using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Members that return an <see cref="IAsyncEnumerable{T}" />, which return an empty sequence without a setup: a
///     property, methods with and without a cancellation token, with a nested generic element type, with a method type
///     parameter and wrapped in a task. An interface that derives from it is not covered.
/// </summary>
public interface IAsyncEnumerableDefaultValues
{
	IAsyncEnumerable<int> Numbers { get; }
	IAsyncEnumerable<string> Read(string filter, CancellationToken cancellationToken = default);
	IAsyncEnumerable<Dictionary<string, List<int[]>>> ReadNested();
	IAsyncEnumerable<T> ReadGeneric<T>();
	Task<IAsyncEnumerable<int>> ReadAsync();
	IDerivedAsyncEnumerable<int> Derived { get; }
}

public interface IDerivedAsyncEnumerable<out T> : IAsyncEnumerable<T>;
