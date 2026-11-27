using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests.Caching;

public sealed class MemoryCacheTests
{
	[Test]
	public async Task Get_AfterSet_ShouldReturnStoredValue()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		cache.Set("order-42", "Dark");
		string? result = cache.Get<string>("order-42");

		await That(result).IsEqualTo("Dark");
	}

	[Test]
	public async Task Get_WithoutEntry_ShouldMiss()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		bool found = cache.TryGetValue("order-42", out string? result);

		await That(found).IsFalse();
		await That(result).IsNull();
	}

	[Test]
	public async Task GetOrCreate_ShouldCallFactoryOnlyOnFirstCall()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		int calls = 0;

		string? first = cache.GetOrCreate("order-42", _ => $"Dark{++calls}");
		string? second = cache.GetOrCreate("order-42", _ => $"Dark{++calls}");

		await That(first).IsEqualTo("Dark1");
		await That(second).IsEqualTo("Dark1").Because("the first call stored the value");
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task Remove_ShouldRemoveStoredValue()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Set("order-42", "Dark");

		cache.Remove("order-42");

		await That(cache.TryGetValue("order-42", out _)).IsFalse();
	}

	[Test]
	public async Task CreateEntry_WhenDisposedWithoutValue_ShouldNotStoreEntry()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		cache.CreateEntry("order-42").Dispose();

		await That(cache.TryGetValue("order-42", out _)).IsFalse();
		await That(cache.Mock.Verify.Set("order-42", It.IsAny<object?>())).Never();
	}

	[Test]
	public async Task CreateEntry_WhenDisposedTwice_ShouldStoreEntryOnce()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		ICacheEntry entry = cache.CreateEntry("order-42");
		entry.Value = "Dark";

		entry.Dispose();
		entry.Dispose();

		await That(cache.Mock.Verify.Set("order-42", "Dark")).Once();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WhenKeyIsMissing_ShouldThrow()
	{
		IMemoryCache cache = IMemoryCache.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act()
			=> cache.TryGetValue("order-42", out _);

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage("The key \"order-42\" of the cache was read without prior setup or stored entry.");
	}

	[Test]
	public async Task SetupTryGetValue_WithoutReturnValue_ShouldStillReadStoredEntries()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		int calls = 0;
		cache.Mock.Setup.TryGetValue(It.IsAny<object>(), It.IsAnyOut<object?>()).Do(() => calls++);
		cache.Set("order-42", "Dark");

		string? result = cache.Get<string>("order-42");

		await That(result).IsEqualTo("Dark").Because("a setup without a return value keeps the cache behavior");
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task SetupTryGetValue_WithoutReturnValue_ShouldPassStoredValueToCallback()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		object? observed = null;
		cache.Mock.Setup.TryGetValue(It.IsAny<object>(), It.IsAnyOut<object?>())
			.Do((object _, object? value) => observed = value);
		cache.Set("order-42", "Dark");

		cache.TryGetValue("order-42", out _);

		await That(observed).IsEqualTo("Dark").Because("the callback sees the value that the caller receives");
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithSetupWithoutReturnValue_ShouldMissWithoutThrowing()
	{
		IMemoryCache cache = IMemoryCache.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		int calls = 0;
		cache.Mock.Setup.TryGetValue(It.IsAny<object>(), It.IsAnyOut<object?>()).Do(() => calls++);

		bool found = cache.TryGetValue("order-42", out _);

		await That(found).IsFalse();
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task CreateEntry_WithInvalidOptions_ShouldThrowLikeRealCacheEntry()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		ICacheEntry entry = cache.CreateEntry("order-42");

		await That(() => entry.AbsoluteExpirationRelativeToNow = TimeSpan.Zero).Throws<ArgumentOutOfRangeException>()
			.WithParamName("AbsoluteExpirationRelativeToNow");
		await That(() => entry.SlidingExpiration = TimeSpan.FromSeconds(-1)).Throws<ArgumentOutOfRangeException>()
			.WithParamName("SlidingExpiration");
		await That(() => entry.Size = -1).Throws<ArgumentOutOfRangeException>()
			.WithParamName("value");
		await That(() => entry.SlidingExpiration = null).DoesNotThrow();
	}

	[Test]
	public async Task CreateEntry_WithNullKey_ShouldThrowArgumentNullException()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		void Act()
			=> cache.CreateEntry(null!);

		await That(Act).Throws<ArgumentNullException>().WithParamName("key");
	}

	[Test]
	public async Task SetupGet_InitializeWithAfterRemove_ShouldApply()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get("order-42").InitializeWith("Milk");
		cache.Remove("order-42");
		bool foundAfterRemove = cache.TryGetValue("order-42", out _);

		cache.Mock.Setup.Get("order-42").InitializeWith("Dark");

		await That(foundAfterRemove).IsFalse();
		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
	}

	[Test]
	public async Task CreateEntry_WithSetupWithoutReturnValue_ShouldStillStoreEntries()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		int calls = 0;
		cache.Mock.Setup.CreateEntry(It.IsAny<object>()).Do(() => calls++);

		cache.Set("order-42", "Dark");

		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(calls).IsEqualTo(1);
	}

	[Test]
	public async Task CreateEntry_WhenValueIsSetAfterDispose_ShouldNotStoreEntry()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		ICacheEntry entry = cache.CreateEntry("order-42");

		entry.Dispose();
		entry.Value = "Dark";
		entry.Dispose();

		await That(cache.TryGetValue("order-42", out _)).IsFalse();
	}

	[Test]
	public async Task Wrapping_ShouldRecordEntriesOfWrappedCache()
	{
		IMemoryCache wrapped = IMemoryCache.CreateMock();
		IMemoryCache cache = IMemoryCache.CreateMock().Wrapping(wrapped);

		cache.Set("order-42", "Dark", TimeSpan.FromMinutes(1));
		ICacheEntry withoutValue = cache.CreateEntry("order-7");
		withoutValue.Dispose();
		withoutValue.Dispose();

		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(wrapped.Get<string>("order-42")).IsEqualTo("Dark");
		await That(cache.Mock.Verify.Set("order-42", "Dark").WithAbsoluteExpirationRelativeToNow(TimeSpan.FromMinutes(1))).Once();
		await That(cache.Mock.Verify.Set("order-7", It.IsAny<object?>())).Never();
	}

	[Test]
	public async Task Wrapping_ShouldRecordOptionsBeforeWrappedCacheCommitsEntry()
	{
		ICacheEntry wrappedEntry = ICacheEntry.CreateMock();
		wrappedEntry.Mock.Setup.Key.Returns("order-42");
		wrappedEntry.Mock.Setup.Dispose().Do(() => wrappedEntry.AbsoluteExpiration = DateTimeOffset.MaxValue);
		IMemoryCache wrapped = IMemoryCache.CreateMock();
		wrapped.Mock.Setup.CreateEntry(It.IsAny<object>()).Returns(wrappedEntry);
		IMemoryCache cache = IMemoryCache.CreateMock().Wrapping(wrapped);

		cache.Set("order-42", "Dark", TimeSpan.FromMinutes(1));

		await That(cache.Mock.Verify.Set("order-42", It.IsAny<object?>()).WithAbsoluteExpiration(It.IsNull<DateTimeOffset?>())).Once()
			.Because("the options are recorded as the code set them");
		await That(wrappedEntry.AbsoluteExpiration).IsEqualTo(DateTimeOffset.MaxValue);
	}

	[Test]
	public async Task CombinationMock_OfClassWithBaseImplementationsAndLogger_ShouldThrowForCacheSetups()
	{
		VirtualMemoryCache cache = VirtualMemoryCache.CreateMock().Implementing<ILogger>();

		void Act()
			=> ((IMockSetup<IMemoryCache>)cache.Mock.Setup).Get("order-42");

		await That(Act).Throws<MockException>()
			.WithMessage("The cache setups and verifications only support mocks of *").AsWildcard();
	}

	[Test]
	public async Task Wrapping_InitializeWith_ShouldThrow()
	{
		IMemoryCache cache = IMemoryCache.CreateMock().Wrapping(IMemoryCache.CreateMock());

		void Act()
			=> cache.Mock.Setup.Get("order-42").InitializeWith("Dark");

		await That(Act).Throws<MockException>()
			.WithMessage("The mock wraps a cache, so store the start value in the wrapped cache instead of using 'InitializeWith'.");
	}

	[Test]
	public async Task ClassWithOverridableMembers_ShouldBehaveLikeCache()
	{
		AbstractMemoryCacheBase cache = AbstractMemoryCacheBase.CreateMock();
		cache.Mock.Setup.Get("order-7").Returns("Milk");

		cache.Set("order-42", "Dark");

		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(cache.Get<string>("order-7")).IsEqualTo("Milk");
		await That(cache.Mock.Verify.Set("order-42", "Dark")).Once();
	}

	[Test]
	public async Task ClassWithOverridableMembers_SetupInCreateMock_ShouldApply()
	{
		AbstractMemoryCacheBase cache = AbstractMemoryCacheBase.CreateMock(setup =>
		{
			setup.Get("order-7").Returns("Milk");
			setup.Get("order-42").InitializeWith("Dark");
		});

		await That(cache.Get<string>("order-7")).IsEqualTo("Milk");
		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
	}

	[Test]
	public async Task ConstructorDependency_ShouldBeMockedAsCache()
	{
		CachingService sut = CachingService.CreateMock();

		sut.Cache.Set("order-42", "Dark");

		await That(sut.Cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(sut.Mock.ConstructorArgument<IMemoryCache>().Mock.Verify.Set("order-42", "Dark")).Once();
	}

	[Test]
	public async Task ClassWithNonVirtualLogAndCacheMembers_ShouldThrowForLoggingSetups()
	{
		LoggingMemoryCacheBase cache = LoggingMemoryCacheBase.CreateMock();

		void Act()
			=> ((IMockSetup<ILogger>)cache.Mock.Setup).LogError();

		await That(Act).Throws<MockException>()
			.WithMessage("The logging setups and verifications do not support '*'.").AsWildcard()
			.Because("the mock supports the cache members, but cannot intercept the non-virtual Log");
	}

	[Test]
	public async Task ClassWithOverridableMembers_SetupGetInScenario_ShouldApplyInScenario()
	{
		AbstractMemoryCacheBase cache = AbstractMemoryCacheBase.CreateMock();
		cache.Mock.InScenario("cached").Setup.Get("order-42").Returns("Dark");
		cache.Mock.InScenario("cached").Setup.Get("order-7").Throws<TimeoutException>();

		cache.Mock.TransitionTo("cached");

		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(() => cache.Get("order-7")).Throws<TimeoutException>();
		await That(cache.Mock.Verify.Get("order-42")).Once();
	}

	[Test]
	public async Task ClassWithBaseImplementations_ShouldThrowForCacheSetups()
	{
		VirtualMemoryCache cache = VirtualMemoryCache.CreateMock();

		void Act()
			=> ((IMockSetup<IMemoryCache>)cache.Mock.Setup).Get("order-42");

		await That(Act).Throws<MockException>()
			.WithMessage("The cache setups and verifications only support mocks of 'IMemoryCache' and of classes whose 'TryGetValue', 'CreateEntry' and 'Remove' are abstract, not '*'.").AsWildcard();
	}

	[Test]
	public async Task ThrowingWhenNotSetup_ShouldStoreRemoveAndReadEntries()
	{
		IMemoryCache cache = IMemoryCache.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		cache.Set("order-42", "Dark");
		string? stored = cache.Get<string>("order-42");
		cache.Remove("order-42");

		await That(stored).IsEqualTo("Dark");
		await That(() => cache.Get("order-42")).Throws<MockNotSetupException>()
			.Because("the entry was removed");
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithSetupThatMisses_ShouldNotThrow()
	{
		IMemoryCache cache = IMemoryCache.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		cache.Mock.Setup.TryGetValue("order-42", It.IsAnyOut<object?>()).Returns(false);

		string? result = cache.GetOrCreate("order-42", _ => "Dark");

		await That(result).IsEqualTo("Dark");
	}

	[Test]
	public async Task SetupGet_InitializeWith_ShouldBehaveLikeStoredValue()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get(It.Matches("order-*")).InitializeWith("Dark");

		string? initial = cache.Get<string>("order-42");
		cache.Set("order-42", "Milk");
		string? replaced = cache.Get<string>("order-42");
		cache.Remove("order-7");
		bool removedFound = cache.TryGetValue("order-7", out _);

		await That(initial).IsEqualTo("Dark");
		await That(replaced).IsEqualTo("Milk");
		await That(removedFound).IsFalse().Because("a removed key does not get the start value again");
		await That(cache.Get<string>("other")).IsNull();
	}

	[Test]
	public async Task SetupGet_InitializeWithInScenario_ShouldOnlyApplyInScenario()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.InScenario("cached").Setup.Get("order-42").InitializeWith("Dark");

		bool before = cache.TryGetValue("order-42", out _);
		cache.Mock.TransitionTo("cached");
		string? after = cache.Get<string>("order-42");

		await That(before).IsFalse();
		await That(after).IsEqualTo("Dark");
	}

	[Test]
	public async Task SetupGet_InitializeWithInScenario_ShouldTakePrecedenceOverDefaultScope()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.InScenario("cached").Setup.Get("order-42").InitializeWith("Dark");
		cache.Mock.Setup.Get("order-42").InitializeWith("Milk");

		string? before = cache.Get<string>("order-42");
		cache.Mock.TransitionTo("cached");
		string? after = cache.Get<string>("order-42");

		await That(before).IsEqualTo("Milk");
		await That(after).IsEqualTo("Dark").Because("reading the start value does not store it");
	}

	[Test]
	public async Task SetupGet_WithValueTypeMatcher_ShouldMatchKeys()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get(It.IsAny<int>()).Returns("Dark");

		string? result = cache.Get<string>(42);

		await That(result).IsEqualTo("Dark");
		await That(cache.Get<string>("42")).IsNull();
	}

	[Test]
	public async Task SetupGet_InitializeWithValueTypeMatcher_ShouldMatchKeys()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get(It.IsAny<int>()).InitializeWith("Dark");

		await That(cache.Get<string>(42)).IsEqualTo("Dark");
		await That(cache.Get<string>("42")).IsNull();
	}

	[Test]
	public async Task SetupGet_Returns_ShouldAlwaysReturnValue()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get("order-42").Returns("Dark");

		cache.Set("order-42", "Milk");
		string? result = cache.Get<string>("order-42");

		await That(result).IsEqualTo("Dark").Because("a setup takes precedence over the stored entry");
	}

	[Test]
	public async Task SetupGetTyped_ShouldReturnAndInitializeTypedValues()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get<decimal>("price-Dark").Returns(2.5m);
		cache.Mock.Setup.Get<decimal>(It.Matches("price-*")).InitializeWith(1.0m);

		await That(cache.Get<decimal>("price-Dark")).IsEqualTo(2.5m);
		await That(cache.Get<decimal>("price-White")).IsEqualTo(1.0m);
	}

	[Test]
	public async Task SetupGet_Misses_ShouldMissAlsoAfterSet()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		int calls = 0;
		cache.Mock.Setup.Get("order-42").Misses();
		cache.Set("order-42", "Milk");

		string? first = cache.GetOrCreate("order-42", _ => $"Dark{++calls}");
		string? second = cache.GetOrCreate("order-42", _ => $"Dark{++calls}");

		await That(cache.Get("order-42")).IsNull();
		await That(first).IsEqualTo("Dark1");
		await That(second).IsEqualTo("Dark2").Because("every read of the key misses");
	}

	[Test]
	public async Task ThrowingWhenNotSetup_WithMisses_ShouldNotThrow()
	{
		IMemoryCache cache = IMemoryCache.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		cache.Mock.Setup.Get<string>("order-42").Misses();

		string? result = cache.GetOrCreate("order-42", _ => "Dark");

		await That(result).IsEqualTo("Dark");
	}

	[Test]
	public async Task SetupGet_ThrowsException_ShouldThrow()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.Setup.Get("order-42").Throws<TimeoutException>();
		cache.Mock.Setup.Get("order-7").Throws(new InvalidOperationException("unavailable"));

		await That(() => cache.Get("order-42")).Throws<TimeoutException>();
		await That(() => cache.Get("order-7")).Throws<InvalidOperationException>().WithMessage("unavailable");
	}

	[Test]
	public async Task SetupGet_InScenario_ShouldOnlyApplyInScenario()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		cache.Mock.InScenario("cached").Setup.Get("order-42").Returns("Dark");

		bool before = cache.TryGetValue("order-42", out _);
		cache.Mock.TransitionTo("cached");
		string? after = cache.Get<string>("order-42");

		await That(before).IsFalse();
		await That(after).IsEqualTo("Dark");
	}

	[Test]
	public async Task SetupGet_ShouldBeVerifiable()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		Mockolate.Setup.IMethodSetup setup = cache.Mock.Setup.Get("order-42").Returns("Dark");

		_ = cache.Get("order-42");

		await That(cache.Mock.VerifySetup(setup)).Once();
	}

	[Test]
	public async Task VerifyGet_ShouldCountReadsOfMatchingKeys()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		_ = cache.Get("order-42");
		_ = cache.GetOrCreate("order-42", _ => "Dark");
		_ = cache.TryGetValue("order-7", out _);

		await That(cache.Mock.Verify.Get("order-42")).Twice();
		await That(cache.Mock.Verify.Get(It.Matches("order-*"))).Exactly(3);
	}

	[Test]
	public async Task VerifyGet_WhenFailing_ShouldDescribeKey()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		void Act()
			=> cache.Mock.Verify.Get("order-42").Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Get(\"order-42\") exactly once, but it never did.");
	}

	[Test]
	public async Task VerifySet_ShouldCountStoredEntries()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		cache.Set("order-42", "Dark");
		_ = cache.GetOrCreate("order-7", _ => "Milk");
		_ = cache.GetOrCreate("order-7", _ => "White");
		cache.Set<string?>("order-8", null);

		await That(cache.Mock.Verify.Set("order-42", "Dark")).Once();
		await That(cache.Mock.Verify.Set("order-7", It.IsAny<object?>())).Once();
		await That(cache.Mock.Verify.Set(It.Matches("order-*"), "Milk")).Once();
		await That(cache.Mock.Verify.Set(It.Matches("order-*"), It.IsAny<object?>())).Exactly(3);
		await That(cache.Mock.Verify.Set("order-8", null)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), (IParameter<object?>?)null)).Once();
	}

	[Test]
	public async Task Verify_WithValueTypeMatchers_ShouldMatch()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		_ = cache.Get(42);
		cache.Set("price", 2.5m, new MemoryCacheEntryOptions
		{
			AbsoluteExpiration = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero),
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
			SlidingExpiration = TimeSpan.FromMinutes(5),
			Size = 3,
		});

		await That(cache.Mock.Verify.Get(It.IsAny<int>())).Once();
		await That(cache.Mock.Verify.Set("price", It.IsAny<decimal>())).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<int>(), It.IsAny<decimal>())).Never();
		await That(cache.Mock.Verify.Set("price", 2.5m)
			.WithAbsoluteExpiration(It.IsAny<DateTimeOffset>())
			.WithAbsoluteExpirationRelativeToNow(It.IsAny<TimeSpan>())
			.WithSlidingExpiration(It.IsAny<TimeSpan>())
			.WithSize(It.IsAny<long>())
			.WithPriority(It.IsAny<CacheItemPriority>())).Once();
	}

	[Test]
	public async Task VerifySet_WithEntryOptions_ShouldFilterEntries()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		DateTimeOffset expiration = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

		cache.Set("absolute", "Dark", expiration);
		cache.Set("relative", "Dark", TimeSpan.FromMinutes(1));
		cache.Set("options", "Dark", new MemoryCacheEntryOptions
		{
			SlidingExpiration = TimeSpan.FromMinutes(5),
			Size = 3,
			Priority = CacheItemPriority.High,
		});

		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithAbsoluteExpiration(expiration)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithAbsoluteExpiration(It.IsNull<DateTimeOffset?>())).Twice();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithAbsoluteExpirationRelativeToNow(TimeSpan.FromMinutes(1))).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithSlidingExpiration(TimeSpan.FromMinutes(5))).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithSize(3)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark").WithPriority(CacheItemPriority.High)).Once();
		await That(cache.Mock.Verify.Set("options", "Dark").WithSize(3).WithPriority(CacheItemPriority.Low)).Never();
	}

	[Test]
	public async Task VerifySet_WithExpirationArguments_ShouldMatchLikeTheSetExtensions()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		DateTimeOffset expiration = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

		cache.Set("absolute", "Dark", expiration);
		cache.Set("relative", "Dark", TimeSpan.FromMinutes(1));

		await That(cache.Mock.Verify.Set("absolute", "Dark", expiration)).Once();
		await That(cache.Mock.Verify.Set("absolute", It.IsAny<object?>(), expiration)).Once();
		await That(cache.Mock.Verify.Set(It.Matches("abs*"), "Dark", expiration)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), It.IsAny<object?>(), expiration.AddDays(1))).Never();
		await That(cache.Mock.Verify.Set("relative", "Dark", TimeSpan.FromMinutes(1))).Once();
		await That(cache.Mock.Verify.Set("relative", It.IsAny<object?>(), TimeSpan.FromMinutes(1))).Once();
		await That(cache.Mock.Verify.Set(It.Matches("rel*"), "Dark", TimeSpan.FromMinutes(1))).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), It.IsAny<object?>(), TimeSpan.FromMinutes(2))).Never();
	}

	[Test]
	public async Task VerifySet_WithOptions_ShouldMatchTheOptions()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();
		MemoryCacheEntryOptions options = new()
		{
			SlidingExpiration = TimeSpan.FromMinutes(5),
			Size = 3,
			Priority = CacheItemPriority.High,
		};

		cache.Set("options", "Dark", options);
		cache.Set("plain", "Dark");

		await That(cache.Mock.Verify.Set("options", "Dark", options)).Once();
		await That(cache.Mock.Verify.Set("options", It.IsAny<object?>(), options)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark", options)).Once();
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), It.IsAny<object?>(), new MemoryCacheEntryOptions { Size = 3, }))
			.Never().Because("all options have to match");
		await That(cache.Mock.Verify.Set(It.IsAny<object>(), "Dark", (MemoryCacheEntryOptions?)null)).Once()
			.Because("an entry without options matches the default options");
	}

	[Test]
	public async Task VerifySet_WithOptions_WhenFailing_ShouldDescribeOptions()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		void Act()
			=> cache.Mock.Verify.Set("order-42", "Dark", new MemoryCacheEntryOptions { Size = 3, }).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Set(\"order-42\", \"Dark\").WithAbsoluteExpiration(null).WithAbsoluteExpirationRelativeToNow(null).WithSlidingExpiration(null).WithSize(3).WithPriority(Normal) exactly once, but it never did.");
	}

	[Test]
	public async Task VerifySet_WhenFailing_ShouldDescribeEntry()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		void Act()
			=> cache.Mock.Verify.Set("order-42", "Dark").WithSlidingExpiration(TimeSpan.FromMinutes(5)).Once();

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Set(\"order-42\", \"Dark\").WithSlidingExpiration(00:05:00) exactly once, but it never did.");
	}

	[Test]
	public async Task VerifyThatAllInteractionsAreVerified_ShouldIgnoreCreateEntry()
	{
		IMemoryCache cache = IMemoryCache.CreateMock();

		_ = cache.GetOrCreate("order-42", _ => "Dark");

		bool beforeVerification = cache.Mock.VerifyThatAllInteractionsAreVerified();
		cache.Mock.Verify.Get("order-42").Once();
		cache.Mock.Verify.Set("order-42", "Dark").Once();
		bool afterVerification = cache.Mock.VerifyThatAllInteractionsAreVerified();

		await That(beforeVerification).IsFalse();
		await That(afterVerification).IsTrue();
	}

	[Test]
	public async Task DerivedInterface_ShouldBehaveLikeCache()
	{
		IOrderCache cache = IOrderCache.CreateMock();

		cache.Set("order-42", "Dark");

		await That(cache.Get<string>("order-42")).IsEqualTo("Dark");
		await That(cache.Mock.Verify.Set("order-42", "Dark")).Once();
	}

	public interface IOrderCache : IMemoryCache;

	public class CachingService(IMemoryCache cache)
	{
		public IMemoryCache Cache { get; } = cache;
	}

	public abstract class LoggingMemoryCacheBase : ILogger, IMemoryCache
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel)
			=> true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
		}

		public abstract ICacheEntry CreateEntry(object key);

		public abstract void Dispose();

		public abstract void Remove(object key);

		public abstract bool TryGetValue(object key, out object? value);
	}
}
