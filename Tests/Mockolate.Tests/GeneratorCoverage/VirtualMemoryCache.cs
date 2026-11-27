using System;
using Microsoft.Extensions.Caching.Memory;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     A class that implements <see cref="IMemoryCache" /> with base implementations that the mock calls, so the mock does
///     not behave like a cache.
/// </summary>
public class VirtualMemoryCache : IMemoryCache
{
	public virtual ICacheEntry CreateEntry(object key)
		=> throw new NotSupportedException();

	public virtual void Dispose()
		=> GC.SuppressFinalize(this);

	public virtual void Remove(object key)
	{
	}

	public virtual bool TryGetValue(object key, out object? value)
	{
		value = null;
		return false;
	}
}
