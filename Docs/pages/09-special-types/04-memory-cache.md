---
description: Mock IMemoryCache with Mockolate, set up the values of its keys and verify the reads and the stored entries with their options.
---

# Memory cache

Mockolate supports mocking `IMemoryCache` from `Microsoft.Extensions.Caching.Memory` without any configuration when the project references `Microsoft.Extensions.Caching.Abstractions` 2.1 or later. Without a setup, the mock behaves like a cache: the `Set(…)` and `GetOrCreate(…)` extension methods store an entry, `Get(…)` and `TryGetValue(…)` read it and `Remove(…)` deletes it. The cache setups and verifications work with the extension methods, which call the corresponding `TryGetValue`, `CreateEntry` or `Remove` members of the cache.

```csharp
IMemoryCache cache = IMemoryCache.CreateMock();
ChocolateShop sut = new ChocolateShop(cache);

sut.GetPrice("Dark");
sut.GetPrice("Dark");

await That(cache.Mock.Verify.Get("price-Dark")).Twice();
await That(cache.Mock.Verify.Set("price-Dark", 2.5m)).Once();
```

<details>
<summary>Example types used on this page</summary>

```csharp
public class ChocolateShop(IMemoryCache cache)
{
    public decimal GetPrice(string type)
        => cache.GetOrCreate($"price-{type}", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return 2.5m;
        });
}
```

</details>

An entry is stored when the entry that `CreateEntry` returned is disposed with a value, as with a real cache. The expiration options are recorded for the verifications, but the entries do not expire and the eviction callbacks are not called.

On a mock created with [`ThrowingWhenNotSetup()`](../03-create-mocks.md#customizing-mock-behavior), reading a key without a setup and without a stored entry throws a `MockNotSetupException`. Storing and removing entries still works. To let a read miss, e.g. for `GetOrCreate(…)`, set up the key with `Misses()`. The setup answers every read of the key, so the key always misses and `GetOrCreate(…)` calls the factory every time:

```csharp
cache.Mock.Setup.Get("price-Dark").Misses();
```

A setup of `TryGetValue` without a return value, e.g. with only a callback, keeps the cache behavior: the stored entries answer the reads, also with their values, and a strict mock misses without throwing.

## Setup

`Setup.Get(key)` sets up the reads of a key, or of the keys that match a [matcher](../06-parameter-matching.md). Like `cache.Get<TItem>(key)`, `Setup.Get<TItem>(key)` takes the type of the value, so a value of another type does not compile:

| Member                   | Reads of the matching keys …                                                                          |
|--------------------------|-------------------------------------------------------------------------------------------------------|
| `InitializeWith(value)`  | return the start value. The cache keeps behaving like a cache, so storing or removing a key replaces it. |
| `Returns(value)`         | always return the value, also after an entry was stored.                                              |
| `Misses()`               | always miss, also after an entry was stored.                                                          |
| `Throws<TException>()`   | throw an exception of the type.                                                                       |
| `Throws(exception)`      | throw the exception.                                                                                  |

```csharp
cache.Mock.Setup.Get<decimal>(It.Matches("price-*")).InitializeWith(1.0m);
cache.Mock.Setup.Get("price-White").Throws<TimeoutException>();
```

`Returns`, `Misses` and `Throws` set up `TryGetValue`, so they take precedence over the stored entries and the start values and can be verified with `VerifySetup(…)`. All members apply in [scenarios](../08-advanced-features/06-scenarios.md), and a start value of the active scenario takes precedence over one of the default scope. A matcher can match keys of any type, e.g. `It.IsAny<int>()`.

## Verification

`Verify.Get(key)` counts the reads of a key, e.g. with `Get(…)`, `TryGetValue(…)` or `GetOrCreate(…)`. `Verify.Set(key, value)` counts the stored entries, e.g. with `Set(…)` or `GetOrCreate(…)` on a miss. Both accept values and matchers. `Remove(key)` is a member of the cache, so `Verify.Remove(key)` verifies it as usual.

Like the `Set(…)` extension methods, `Verify.Set(…)` takes an absolute expiration, an expiration relative to now or `MemoryCacheEntryOptions` as third argument. The options match when the entry has the same expirations, size and priority; their change tokens and eviction callbacks are not compared:

```csharp
await That(cache.Mock.Verify.Set("price-Dark", 2.5m,
    new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) })).Once();
```

Or narrow `Verify.Set(…)` down by single options of the entry:

| Member                                         | Counts the entries …                                  |
|------------------------------------------------|-------------------------------------------------------|
| `WithAbsoluteExpiration(…)`                    | with the absolute expiration.                         |
| `WithAbsoluteExpirationRelativeToNow(…)`       | with the absolute expiration relative to now.         |
| `WithSlidingExpiration(…)`                     | with the sliding expiration.                          |
| `WithSize(…)`                                  | with the size.                                        |
| `WithPriority(…)`                              | with the priority.                                    |

Each member accepts a value or a matcher. To count the entries without an option, use `It.IsNull`, e.g. `WithSlidingExpiration(It.IsNull<TimeSpan?>())`:

```csharp
await That(cache.Mock.Verify.Set(It.Matches("price-*"), It.IsAny<object?>())
    .WithSlidingExpiration(TimeSpan.FromMinutes(5))).Once();
await That(cache.Mock.Verify.Remove("price-Dark")).Never();
```

`CreateEntry` is called by the extension methods themselves, so `VerifyThatAllInteractionsAreVerified()` ignores its calls; verify the stored entries with `Verify.Set(…)` instead.

## Wrapping and classes

A mock that [wraps](../03-create-mocks.md#wrapping-existing-instances) a cache reads and stores the entries in the wrapped cache and still records them for `Verify.Get(…)` and `Verify.Set(…)`. `Verify.Set(…)` counts the entries passed to the wrapped cache, also when it does not store them, e.g. because of a size limit. Set start values in the wrapped cache, as `InitializeWith(…)` throws on the wrapping mock and start values set before `Wrapping(…)` have no effect.

A mock of a class that implements `IMemoryCache` behaves like a cache when `TryGetValue`, `CreateEntry` and `Remove` are abstract. Otherwise the mock calls the base implementations, and [Mockolate0014](../10-analyzers.md#mockolate0014) reports the cache setups and verifications.
