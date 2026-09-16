#if NET9_0_OR_GREATER
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers.RefStruct;

namespace Mockolate.Tests.RefStruct;

/// <summary>
///     Tests for projection-based storage on ref-struct-keyed indexers (arity 1). Verifies that
///     supplying a <see cref="RefStructProjection{T, TProjected}" /> via
///     <see cref="It.IsRefStructBy{T, TProjected}(RefStructProjection{T, TProjected})" /> activates
///     write-then-read correlation: values set via the setter are stored keyed by the projected
///     scalar and returned by subsequent reads of any key that projects to the same scalar.
/// </summary>
public sealed class ProjectionIndexerTests
{
	public sealed class EndToEndTests
	{
		[Fact]
		public async Task WithProjection_SetterWrite_FeedsGetterRead_UnderProjectedKey()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)].Returns("fallback");

			sut[new Packet(1, [])] = "written";

			string hit = sut[new Packet(1, [])];
			string miss = sut[new Packet(2, [])];

			await That(hit).IsEqualTo("written");
			await That(miss).IsEqualTo("fallback");
		}

		[Fact]
		public async Task WithoutProjection_SetterWrite_IsStorageNoOp()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Returns("fallback");

			sut[new Packet(1, [])] = "ignored-by-storage";

			string v = sut[new Packet(1, [])];

			await That(v).IsEqualTo("fallback");
		}

		[Fact]
		public async Task WithProjection_MultipleWrites_LastWriteWinsUnderSameProjectedKey()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)].Returns("fallback");

			sut[new Packet(1, [])] = "first";
			byte[] bytes = [0x01, 0x02];
			// Different ref-struct (payload differs) but same projected id — overwrites.
			sut[new Packet(1, bytes)] = "second";

			string v = sut[new Packet(1, [])];

			await That(v).IsEqualTo("second");
		}

		[Fact]
		public async Task WithProjectionAndPredicate_StoresOnlyWhenPredicateMatches()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id, id => id > 10)]
				.Returns("big");

			sut[new Packet(42, [])] = "wrote-big";
			// The setter's matcher rejects id <= 10 → storage write is skipped (the setup is not
			// even reached for non-matching keys, so the write never reaches StoreValue).
			sut[new Packet(5, [])] = "wrote-small";

			string big = sut[new Packet(42, [])];
			string small = sut[new Packet(5, [])];

			await That(big).IsEqualTo("wrote-big");
			await That(small).IsEqualTo("")
				.Because("small does not match the setup, so the getter falls through to the framework default");
		}

		[Fact]
		public async Task MatcherOnlySetup_NoProjection_ReturnsConfiguredValueRegardlessOfWrite()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStruct<Packet>(p => p.Id > 100)].Returns("hit");

			sut[new Packet(500, [])] = "written-but-lost";
			string v = sut[new Packet(500, [])];

			await That(v).IsEqualTo("hit");
		}

		[Fact]
		public async Task WithProjection_NoReturnsConfigured_ReadsStoredValue()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			_ = sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)];

			sut[new Packet(7, [])] = "stored";

			string v = sut[new Packet(7, [])];

			await That(v).IsEqualTo("stored");
		}

		[Fact]
		public async Task WithProjection_NoWrite_EmptyBucket_FallsBackToReturns()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)].Returns("fallback");

			string v = sut[new Packet(99, [])];

			await That(v).IsEqualTo("fallback");
		}

		[Fact]
		public async Task WithProjection_NoWriteNoReturns_ReturnsDefault()
		{
			// When storage is active but the bucket is empty and no Returns(...) was configured,
			// we reach rule 5: the setup takes the default factory provided by the generator,
			// which yields default(TValue). For string that is null. If the user wants the
			// string framework default ("") they should configure Returns(...) explicitly.
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			_ = sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)];

			string v = sut[new Packet(99, [])];

			await That(v).IsNull();
		}

		[Fact]
		public async Task SetterOnly_NoBoundGetter_DoesNotStore()
		{
			// IGeneratedPacketSetter exposes only a setter. Writing via a projection matcher
			// must not fail, but there is no getter to read the value back from — the matcher
			// was asked for correlation but there's no companion getter.
			IGeneratedPacketSetter sut = IGeneratedPacketSetter.CreateMock();
			string? captured = null;
			sut.Mock.Setup[It.IsRefStructBy<Packet, int>(p => p.Id)]
				.OnSet(v => captured = v);

			sut[new Packet(1, [])] = "hello";

			await That(captured).IsEqualTo("hello");
		}
	}

	public sealed class UnitLevelTests
	{
		[Fact]
		public async Task CombinedSetup_WiresSetterBoundGetter_WritesFeedReads()
		{
			// Exercises the setup plumbing at a coarser level than the internal StoreValue /
			// TryGetStoredValue tests (which live in Mockolate.Internal.Tests and can reach the
			// internal storage surface). Here we only drive the two inner setups' public
			// Invoke entry points and rely on end-to-end behaviour.
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);
			RefStructIndexerSetup<string, Packet> setup = new(
				"get_Item", "set_Item", (IParameterMatch<Packet>)matcher);

			setup.Setter.Invoke(new Packet(7, []), "seven");
			string read = setup.Getter.Invoke(new Packet(7, []));

			await That(read).IsEqualTo("seven");
		}
	}

	public sealed class MultiParameterTests
	{
		[Fact]
		public async Task MixedIndexer_NormalAndProjection_WritesFeedReadsUnderProjectedKey()
		{
			IGeneratedMixedStore sut = IGeneratedMixedStore.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsRefStructBy<Packet, int>(p => p.Id)]
				.Returns("fallback");

			sut[3, new Packet(1, [])] = "written";

			string hit = sut[3, new Packet(1, [])];
			string missOnFirstSlot = sut[4, new Packet(1, [])];
			string missOnSecondSlot = sut[3, new Packet(2, [])];

			await That(hit).IsEqualTo("written");
			await That(missOnFirstSlot).IsEqualTo("fallback");
			await That(missOnSecondSlot).IsEqualTo("fallback");
		}

		[Fact]
		public async Task MixedIndexer_RefStructWithoutProjection_DisablesStorage()
		{
			IGeneratedMixedStore sut = IGeneratedMixedStore.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAnyRefStruct<Packet>()].Returns("fallback");

			sut[3, new Packet(1, [])] = "ignored-by-storage";

			string v = sut[3, new Packet(1, [])];

			await That(v).IsEqualTo("fallback");
		}

		[Fact]
		public async Task Arity5_MixedProjectionsAndNormals_WritesFeedReads()
		{
			// Exercises the generator-emitted arity 5+ RefStructIndexerSetup class — three
			// ref-struct slots with projections interleaved with two non-ref-struct slots.
			IBigPacketStore5 sut = IBigPacketStore5.CreateMock();
			sut.Mock.Setup[
					It.IsRefStructBy<Packet, int>(p => p.Id),
					It.IsAny<int>(),
					It.IsRefStructBy<Packet, int>(p => p.Id),
					It.IsAny<string>(),
					It.IsRefStructBy<Packet, int>(p => p.Id)]
				.Returns("fallback");

			sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])] = "written";

			string hit = sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])];
			string miss = sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(99, [])];
			string missOnNormal = sut[new Packet(1, []), 99, new Packet(2, []), "tag", new Packet(3, [])];

			await That(hit).IsEqualTo("written");
			await That(miss).IsEqualTo("fallback");
			await That(missOnNormal).IsEqualTo("fallback");
		}

		[Fact]
		public async Task DoubleRefStructIndexer_BothProjections_WritesFeedReads()
		{
			IGeneratedDoublePacketStore sut = IGeneratedDoublePacketStore.CreateMock();
			sut.Mock.Setup[
					It.IsRefStructBy<Packet, int>(p => p.Id),
					It.IsRefStructBy<Packet, int>(p => p.Id)]
				.Returns("fallback");

			sut[new Packet(1, []), new Packet(2, [])] = "written";
			byte[] bytes = [0xA];
			string hit = sut[new Packet(1, bytes), new Packet(2, [])];
			string miss = sut[new Packet(1, []), new Packet(3, [])];

			await That(hit).IsEqualTo("written")
				.Because("the projected ids are the same, so the differing inline spans still match the stored bucket");
			await That(miss).IsEqualTo("fallback");
		}
	}

	public sealed class MatcherTests
	{
		[Fact]
		public async Task IsRefStructBy_ReturnsProjectionMatcher()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);

			await That(matcher).Is<IRefStructProjectionMatch<Packet>>();
			await That(matcher).Is<IRefStructProjectionMatch<Packet, int>>();
		}

		[Fact]
		public async Task IsRefStructBy_Project_ReturnsTypedProjection()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);
			IRefStructProjectionMatch<Packet, int> typed = (IRefStructProjectionMatch<Packet, int>)matcher;

			int projected = typed.Project(new Packet(42, []));

			await That(projected).IsEqualTo(42);
		}

		[Fact]
		public async Task IsRefStructBy_NonGenericProject_BoxesProjection()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);
			IRefStructProjectionMatch<Packet> nonGeneric = (IRefStructProjectionMatch<Packet>)matcher;

			object projected = nonGeneric.Project(new Packet(42, []));

			await That(projected).IsEqualTo(42);
			await That(projected).Is<int>();
		}

		[Fact]
		public async Task IsRefStructBy_NoPredicate_MatchesAny()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);
			IParameterMatch<Packet> asMatch = (IParameterMatch<Packet>)matcher;

			await That(asMatch.Matches(new Packet(1, []))).IsTrue();
			await That(asMatch.Matches(new Packet(int.MaxValue, []))).IsTrue();
		}

		[Fact]
		public async Task IsRefStructBy_WithPredicate_FiltersOnProjectedValue()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id, id => id == 7);
			IParameterMatch<Packet> asMatch = (IParameterMatch<Packet>)matcher;

			await That(asMatch.Matches(new Packet(7, []))).IsTrue();
			await That(asMatch.Matches(new Packet(8, []))).IsFalse();
		}

		[Fact]
		public async Task IsRefStructBy_ToString_WithoutPredicate()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);

			await That(matcher.ToString()).IsEqualTo("It.IsRefStructBy<Packet, Int32>(<projection>)");
		}

		[Fact]
		public async Task IsRefStructBy_ToString_WithPredicate()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id, id => id > 0);

			await That(matcher.ToString())
				.IsEqualTo("It.IsRefStructBy<Packet, Int32>(<projection>, <predicate>)");
		}

		[Fact]
		public async Task IsRefStructBy_Matches_ObjectOverload_ReturnsFalse()
		{
			IParameter<Packet> matcher = It.IsRefStructBy<Packet, int>(p => p.Id);

			await That(matcher.Matches(null)).IsFalse()
				.Because(
					"the untyped IParameter.Matches(object?) overload is only a fallback for covariance-safe dispatch, and a ref struct can never be boxed into object");
			await That(matcher.Matches(42)).IsFalse()
				.Because(
					"the untyped IParameter.Matches(object?) overload is only a fallback for covariance-safe dispatch, and a ref struct can never be boxed into object");
		}
	}
}
#endif
