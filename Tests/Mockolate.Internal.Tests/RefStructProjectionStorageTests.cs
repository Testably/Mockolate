#if NET9_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests;

/// <summary>
///     White-box tests for the projection-based storage on
///     <see cref="RefStructIndexerGetterSetup{TValue, T}" /> and the
///     <see cref="RefStructIndexerSetterSetup{TValue, T}" /> <c>BoundGetter</c> slot. These
///     exercise the internal <c>StoreValue</c> / <c>TryGetStoredValue</c> surface that the
///     end-to-end tests in <c>Mockolate.Tests</c> cannot reach.
/// </summary>
public sealed class RefStructProjectionStorageTests
{
	/// <summary>
	///     Local ref struct used instead of the test-project <c>Packet</c> (which lives in
	///     <c>Mockolate.Tests</c> — not referenced from this project).
	/// </summary>
	private readonly ref struct Key(int id)
	{
		public int Id { get; } = id;
	}

	[Test]
	public async Task GetterSetup_WithProjection_StoreAndTryGet_RoundTrip()
	{
		IParameter<Key> matcher = It.IsRefStructBy<Key, int>(k => k.Id);
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item", (IParameterMatch<Key>)matcher);

		getter.StoreValue(new Key(42), "forty-two");

		bool found = getter.TryGetStoredValue(new Key(42), out string? value);

		await That(found).IsTrue();
		await That(value).IsEqualTo("forty-two");
	}

	[Test]
	public async Task GetterSetup_WithoutProjection_StoreIsNoOp_TryGetReturnsFalse()
	{
		IParameter<Key> matcher = It.IsAnyRefStruct<Key>();
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item", (IParameterMatch<Key>)matcher);

		getter.StoreValue(new Key(1), "ignored");

		bool found = getter.TryGetStoredValue(new Key(1), out string? _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task GetterSetup_NullMatcher_HasNoStorage()
	{
		RefStructIndexerGetterSetup<string, Key> getter = new("get_Item", null);

		getter.StoreValue(new Key(1), "ignored");

		bool found = getter.TryGetStoredValue(new Key(1), out _);

		await That(found).IsFalse();
	}

	[Test]
	public async Task GetterSetup_WithProjectionAndPredicate_StoreSkipsNonMatchingKey()
	{
		IParameter<Key> matcher = It.IsRefStructBy<Key, int>(k => k.Id, id => id > 10);
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item", (IParameterMatch<Key>)matcher);

		getter.StoreValue(new Key(5), "small"); // rejected by predicate
		getter.StoreValue(new Key(42), "big");  // accepted

		bool smallFound = getter.TryGetStoredValue(new Key(5), out _);
		bool bigFound = getter.TryGetStoredValue(new Key(42), out string? bigValue);

		await That(smallFound).IsFalse();
		await That(bigFound).IsTrue();
		await That(bigValue).IsEqualTo("big");
	}

	[Test]
	public async Task GetterSetup_WithProjection_Invoke_ReturnsStoredValue_BeforeReturnFactory()
	{
		IRefStructIndexerGetterSetup<string, Key> setup;
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item",
			(IParameterMatch<Key>)It.IsRefStructBy<Key, int>(k => k.Id));
		setup = getter;
		setup.Returns("fallback");

		getter.StoreValue(new Key(1), "stored");

		string read = getter.Invoke(new Key(1));

		await That(read).IsEqualTo("stored");
	}

	[Test]
	public async Task GetterSetup_WithProjection_EmptyBucket_Invoke_FallsBackToReturns()
	{
		IRefStructIndexerGetterSetup<string, Key> setup;
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item",
			(IParameterMatch<Key>)It.IsRefStructBy<Key, int>(k => k.Id));
		setup = getter;
		setup.Returns("fallback");

		string read = getter.Invoke(new Key(99));

		await That(read).IsEqualTo("fallback");
	}

	[Test]
	public async Task GetterSetup_WithProjection_HasReturnValue_IsTrueEvenWithoutReturnFactory()
	{
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item",
			(IParameterMatch<Key>)It.IsRefStructBy<Key, int>(k => k.Id));

		await That(getter.HasReturnValue).IsTrue()
			.Because(
				"activating the storage dictionary must flip HasReturnValue, so that the generator's emitted getter body takes the return path and serves stored values");
	}

	[Test]
	public async Task GetterSetup_WithoutProjection_HasReturnValue_IsFalse_UntilReturnsConfigured()
	{
		RefStructIndexerGetterSetup<string, Key> getter = new(
			"get_Item", (IParameterMatch<Key>)It.IsAnyRefStruct<Key>());

		await That(getter.HasReturnValue).IsFalse();

		((IRefStructIndexerGetterSetup<string, Key>)getter).Returns("x");

		await That(getter.HasReturnValue).IsTrue();
	}

	[Test]
	public async Task CombinedSetup_Constructor_WiresSetterBoundGetter()
	{
		IParameter<Key> matcher = It.IsRefStructBy<Key, int>(k => k.Id);
		RefStructIndexerSetup<string, Key> setup = new(
			"get_Item", "set_Item", (IParameterMatch<Key>)matcher);

		await That(setup.Setter.BoundGetter).IsSameAs(setup.Getter);
	}

	[Test]
	public async Task CombinedSetup_WithProjection_SetterInvoke_StoresIntoGetter()
	{
		IParameter<Key> matcher = It.IsRefStructBy<Key, int>(k => k.Id);
		RefStructIndexerSetup<string, Key> setup = new(
			"get_Item", "set_Item", (IParameterMatch<Key>)matcher);

		setup.Setter.Invoke(new Key(7), "seven");

		bool found = setup.Getter.TryGetStoredValue(new Key(7), out string? value);

		await That(found).IsTrue();
		await That(value).IsEqualTo("seven");
	}

	[Test]
	public async Task SetterOnlySetup_BoundGetter_DefaultsToNull_InvokeIsStorageNoOp()
	{
		IParameter<Key> matcher = It.IsRefStructBy<Key, int>(k => k.Id);
		RefStructIndexerSetterSetup<string, Key> setter = new(
			"set_Item", (IParameterMatch<Key>)matcher);

		await That(setter.BoundGetter).IsNull();

		// Should be a no-op for storage but still execute any OnSet/Throws slots (neither set
		// here, so this must simply not throw).
		setter.Invoke(new Key(7), "seven");
	}

	[Test]
	public async Task CombinedSetup_WithoutProjection_NoStorageActivated()
	{
		IParameter<Key> matcher = It.IsAnyRefStruct<Key>();
		RefStructIndexerSetup<string, Key> setup = new(
			"get_Item", "set_Item", (IParameterMatch<Key>)matcher);

		await That(setup.Setter.BoundGetter).IsSameAs(setup.Getter)
			.Because("the combined setup always wires the bound getter, projection or not");

		setup.Setter.Invoke(new Key(7), "seven");
		bool found = setup.Getter.TryGetStoredValue(new Key(7), out _);

		await That(found).IsFalse()
			.Because("without a projection the getter's storage is never activated, so writes cannot be stored");
	}

	public sealed class Arity2Tests
	{
		[Test]
		public async Task AllProjections_StoreAndTryGet_RoundTrip()
		{
			IParameter<Key> p1 = It.IsRefStructBy<Key, int>(k => k.Id);
			IParameter<Key> p2 = It.IsRefStructBy<Key, int>(k => k.Id);
			RefStructIndexerGetterSetup<string, Key, Key> getter = new(
				"get_Item",
				(IParameterMatch<Key>)p1, (IParameterMatch<Key>)p2);

			getter.StoreValue(new Key(1), new Key(2), "hit", null, null);
			bool found = getter.TryGetStoredValue(new Key(1), new Key(2), null, null, out string? value);
			bool miss = getter.TryGetStoredValue(new Key(1), new Key(99), null, null, out _);

			await That(found).IsTrue();
			await That(value).IsEqualTo("hit");
			await That(miss).IsFalse();
		}

		[Test]
		public async Task MixedNormalAndProjection_StoreAndTryGet_RoundTrip()
		{
			IParameter<int> normal = It.IsAny<int>();
			IParameter<Key> projected = It.IsRefStructBy<Key, int>(k => k.Id);
			RefStructIndexerGetterSetup<string, int, Key> getter = new(
				"get_Item",
				(IParameterMatch<int>)normal, (IParameterMatch<Key>)projected);

			getter.StoreValue(3, new Key(7), "hit", 3, null);
			bool hit = getter.TryGetStoredValue(3, new Key(7), 3, null, out string? value);
			bool missOnFirst = getter.TryGetStoredValue(4, new Key(7), 4, null, out _);
			bool missOnSecond = getter.TryGetStoredValue(3, new Key(8), 3, null, out _);

			await That(hit).IsTrue();
			await That(value).IsEqualTo("hit");
			await That(missOnFirst).IsFalse();
			await That(missOnSecond).IsFalse();
		}

		[Test]
		public async Task OneRefStructWithoutProjection_StorageInactive()
		{
			IParameter<int> normal = It.IsAny<int>();
			IParameter<Key> refNoProjection = It.IsAnyRefStruct<Key>();
			RefStructIndexerGetterSetup<string, int, Key> getter = new(
				"get_Item",
				(IParameterMatch<int>)normal, (IParameterMatch<Key>)refNoProjection);

			getter.StoreValue(1, new Key(2), "ignored", 1, null);
			bool found = getter.TryGetStoredValue(1, new Key(2), 1, null, out _);

			await That(found).IsFalse();
			await That(getter.HasReturnValue).IsFalse();
		}

		[Test]
		public async Task CombinedSetup_WiresBoundGetter_SetterWrite_FeedsGetterRead()
		{
			IParameter<int> normal = It.IsAny<int>();
			IParameter<Key> projected = It.IsRefStructBy<Key, int>(k => k.Id);
			RefStructIndexerSetup<string, int, Key> setup = new(
				"get_Item", "set_Item",
				(IParameterMatch<int>)normal, (IParameterMatch<Key>)projected);

			await That(setup.Setter.BoundGetter).IsSameAs(setup.Getter);

			setup.Setter.Invoke(3, new Key(7), "stored", 3, null);
			bool found = setup.Getter.TryGetStoredValue(3, new Key(7), 3, null, out string? value);

			await That(found).IsTrue();
			await That(value).IsEqualTo("stored");
		}
	}

	public sealed class Arity3Tests
	{
		[Test]
		public async Task MixedProjectionAndNormal_RoundTrip()
		{
			IParameter<int> p1 = It.IsAny<int>();
			IParameter<Key> p2 = It.IsRefStructBy<Key, int>(k => k.Id);
			IParameter<string> p3 = It.IsAny<string>();
			RefStructIndexerGetterSetup<string, int, Key, string> getter = new(
				"get_Item",
				(IParameterMatch<int>)p1,
				(IParameterMatch<Key>)p2,
				(IParameterMatch<string>)p3);

			getter.StoreValue(1, new Key(42), "tag", "v", 1, null, "tag");
			bool found = getter.TryGetStoredValue(1, new Key(42), "tag", 1, null, "tag", out string? value);

			await That(found).IsTrue();
			await That(value).IsEqualTo("v");
		}
	}

	public sealed class Arity4Tests
	{
		[Test]
		public async Task AllRefStructWithProjection_RoundTrip()
		{
			IParameter<Key> p = It.IsRefStructBy<Key, int>(k => k.Id);
			RefStructIndexerGetterSetup<string, Key, Key, Key, Key> getter = new(
				"get_Item",
				(IParameterMatch<Key>)p, (IParameterMatch<Key>)p,
				(IParameterMatch<Key>)p, (IParameterMatch<Key>)p);

			getter.StoreValue(new Key(1), new Key(2), new Key(3), new Key(4), "v",
				null, null, null, null);
			bool found = getter.TryGetStoredValue(
				new Key(1), new Key(2), new Key(3), new Key(4),
				null, null, null, null, out string? value);

			await That(found).IsTrue();
			await That(value).IsEqualTo("v");
		}
	}
}
#endif
