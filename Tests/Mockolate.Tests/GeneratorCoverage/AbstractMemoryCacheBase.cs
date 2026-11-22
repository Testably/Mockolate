using Microsoft.Extensions.Caching.Memory;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A class that implements <see cref="IMemoryCache" /> with members that the mock overrides, so the mock behaves like
///     a cache.
/// </summary>
public abstract class AbstractMemoryCacheBase : IMemoryCache
{
	public abstract ICacheEntry CreateEntry(object key);

	public abstract void Dispose();

	public abstract void Remove(object key);

	public abstract bool TryGetValue(object key, out object? value);
}
