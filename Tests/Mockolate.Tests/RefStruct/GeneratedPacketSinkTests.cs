#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers.RefStruct;

namespace Mockolate.Tests.RefStruct;

public sealed class GeneratedPacketSinkTests
{
	public sealed class VoidArity1Tests
	{
		[Fact]
		public async Task ConsumeCount_ViaInteractions_ReflectsEveryInvocation()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();

			sut.Consume(new Packet(1, []));
			sut.Consume(new Packet(2, []));
			sut.Consume(new Packet(3, []));

			// Ref-struct methods do not generate Verify.XXX entries — the interaction type
			// cannot carry the parameter value. Count via the IMock.MockRegistry instead.
			int count = ((IMock)sut).MockRegistry.Interactions
				.OfType<RefStructMethodInvocation>()
				.Count(i => i.Name.EndsWith(".Consume", StringComparison.Ordinal));

			await That(count).IsEqualTo(3);
		}

		[Fact]
		public async Task LatestMatchingSetupWins()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();
			sut.Mock.Setup.Consume(It.IsAnyRefStruct<Packet>())
				.Throws<InvalidOperationException>();
			sut.Mock.Setup.Consume(It.IsRefStruct<Packet>(p => p.Id == 42))
				.Throws<NotSupportedException>();

			void ActSpecific()
			{
				sut.Consume(new Packet(42, []));
			}

			void ActFallback()
			{
				sut.Consume(new Packet(1, []));
			}

			await That(ActSpecific).Throws<NotSupportedException>();
			await That(ActFallback).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task NoSetup_ShouldBeNoOp()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();

			void Act()
			{
				sut.Consume(new Packet(42, []));
			}

			await That(Act).DoesNotThrow();
		}

		[Fact]
		public async Task RecordedInteraction_StoresParameterNameButNoValue()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();

			sut.Consume(new Packet(999, []));

			RefStructMethodInvocation? recorded = ((IMock)sut).MockRegistry.Interactions
				.OfType<RefStructMethodInvocation>()
				.SingleOrDefault();

			await That(recorded).IsNotNull();
			await That(recorded!.Name).EndsWith(".Consume");
			await That(recorded.ParameterNames.Single()).IsEqualTo("packet");
		}

		[Fact]
		public async Task SetupPredicateInspectingSpanPayload_ShouldOnlyActOnMatch()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();
			// The predicate reads into the inline Span — the whole point of ref-struct mocking.
			sut.Mock.Setup.Consume(It.IsRefStruct<Packet>(p =>
					p.Payload.Length > 0 && p.Payload[0] == 0xFF))
				.Throws<InvalidOperationException>();

			byte[] hit = [0xFF, 0x01,];
			byte[] miss = [0x00, 0xFF,];

			void ActHit()
			{
				sut.Consume(new Packet(1, hit));
			}

			void ActMiss()
			{
				sut.Consume(new Packet(2, miss));
			}

			await That(ActHit).Throws<InvalidOperationException>();
			await That(ActMiss).DoesNotThrow();
		}

		[Fact]
		public async Task SetupThrows_ShouldThrowConfiguredException()
		{
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock();
			sut.Mock.Setup.Consume(It.IsAnyRefStruct<Packet>())
				.Throws<InvalidOperationException>();

			void Act()
			{
				sut.Consume(new Packet(1, []));
			}

			await That(Act).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task ThrowWhenNotSetup_ShouldThrowMockNotSetupException()
		{
			MockBehavior behavior = MockBehavior.Default with
			{
				ThrowWhenNotSetup = true,
			};
			IGeneratedPacketSink sut = IGeneratedPacketSink.CreateMock(behavior);

			void Act()
			{
				sut.Consume(new Packet(1, []));
			}

			await That(Act).Throws<MockNotSetupException>();
		}
	}

	public sealed class MixedParameterTests
	{
		[Fact]
		public async Task Write_WithMatcherForRefStructAndValueParam_Matches()
		{
			IGeneratedPacketWriter sut = IGeneratedPacketWriter.CreateMock();
			sut.Mock.Setup.Write(
					It.IsRefStruct<Packet>(p => p.Id == 1),
					It.IsAny<int>())
				.Throws<InvalidOperationException>();

			void ActHit()
			{
				sut.Write(new Packet(1, []), 5);
			}

			void ActMiss()
			{
				sut.Write(new Packet(2, []), 5);
			}

			await That(ActHit).Throws<InvalidOperationException>();
			await That(ActMiss).DoesNotThrow();
		}

		[Fact]
		public async Task Write_WithPriorityMatcher_GatesOnNonRefStructParameter()
		{
			IGeneratedPacketWriter sut = IGeneratedPacketWriter.CreateMock();
			sut.Mock.Setup.Write(
					It.IsAnyRefStruct<Packet>(),
					It.Satisfies<int>(p => p > 10))
				.Throws<InvalidOperationException>();

			void ActHit()
			{
				sut.Write(new Packet(1, []), 99);
			}

			void ActMiss()
			{
				sut.Write(new Packet(1, []), 1);
			}

			await That(ActHit).Throws<InvalidOperationException>();
			await That(ActMiss).DoesNotThrow();
		}
	}

	public sealed class ReturnMethodTests
	{
		[Fact]
		public async Task TryParse_NoReturnConfigured_ReturnsFrameworkDefault()
		{
			IGeneratedPacketParser sut = IGeneratedPacketParser.CreateMock();

			int result = sut.TryParse(new Packet(7, []));

			await That(result).IsEqualTo(0);
		}

		[Fact]
		public async Task TryParse_PredicateDictatesWhichSetupMatches()
		{
			IGeneratedPacketParser sut = IGeneratedPacketParser.CreateMock();
			sut.Mock.Setup.TryParse(It.IsRefStruct<Packet>(p => p.Id > 100)).Returns(100);
			sut.Mock.Setup.TryParse(It.IsRefStruct<Packet>(p => p.Id < 10)).Returns(1);

			int high = sut.TryParse(new Packet(500, []));
			int low = sut.TryParse(new Packet(3, []));
			int mid = sut.TryParse(new Packet(50, []));

			await That(high).IsEqualTo(100);
			await That(low).IsEqualTo(1);
			await That(mid).IsEqualTo(0)
				.Because("nothing matches, so the framework default applies");
		}

		[Fact]
		public async Task TryParse_ReturnsConfiguredValue()
		{
			IGeneratedPacketParser sut = IGeneratedPacketParser.CreateMock();
			sut.Mock.Setup.TryParse(It.IsAnyRefStruct<Packet>()).Returns(42);

			int result = sut.TryParse(new Packet(1, []));

			await That(result).IsEqualTo(42);
		}

		[Fact]
		public async Task TryParse_ReturnsFunc_InvokedPerCall()
		{
			IGeneratedPacketParser sut = IGeneratedPacketParser.CreateMock();
			int counter = 0;
			sut.Mock.Setup.TryParse(It.IsAnyRefStruct<Packet>())
				.Returns(() => ++counter);

			int first = sut.TryParse(new Packet(1, []));
			int second = sut.TryParse(new Packet(2, []));

			await That(first).IsEqualTo(1);
			await That(second).IsEqualTo(2);
		}

		[Fact]
		public async Task TryParse_ThrowsConfiguredException()
		{
			IGeneratedPacketParser sut = IGeneratedPacketParser.CreateMock();
			sut.Mock.Setup.TryParse(It.IsAnyRefStruct<Packet>())
				.Throws<InvalidOperationException>();

			void Act()
			{
				sut.TryParse(new Packet(1, []));
			}

			await That(Act).Throws<InvalidOperationException>();
		}
	}

	public sealed class IndexerSetterTests
	{
		[Fact]
		public async Task OnSet_Callback_ReceivesValue()
		{
			IGeneratedPacketSetter sut = IGeneratedPacketSetter.CreateMock();
			string? captured = null;
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].OnSet(v => captured = v);

			sut[new Packet(1, [])] = "hello";

			await That(captured).IsEqualTo("hello");
		}

		[Fact]
		public async Task Predicate_FiltersByKey_OnSetOnlyForHit()
		{
			IGeneratedPacketSetter sut = IGeneratedPacketSetter.CreateMock();
			string? captured = null;
			sut.Mock.Setup[It.IsRefStruct<Packet>(p => p.Id == 42)].OnSet(v => captured = v);

			sut[new Packet(99, [])] = "no";
			sut[new Packet(42, [])] = "yes";

			await That(captured).IsEqualTo("yes");
		}

		[Fact]
		public async Task RecordedInteraction_UsesSetItemName()
		{
			IGeneratedPacketSetter sut = IGeneratedPacketSetter.CreateMock();

			sut[new Packet(1, [])] = "v";

			RefStructMethodInvocation? recorded = ((IMock)sut).MockRegistry.Interactions
				.OfType<RefStructMethodInvocation>()
				.SingleOrDefault();

			await That(recorded).IsNotNull();
			await That(recorded!.Name).EndsWith(".set_Item");
			await That(recorded.ParameterNames).IsEqualTo(new[]
			{
				"key", "value",
			});
		}

		[Fact]
		public async Task Throws_ConfiguredException()
		{
			IGeneratedPacketSetter sut = IGeneratedPacketSetter.CreateMock();
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Throws<InvalidOperationException>();

			void Act()
			{
				sut[new Packet(1, [])] = "x";
			}

			await That(Act).Throws<InvalidOperationException>();
		}
	}

	public sealed class CombinedIndexerTests
	{
		[Fact]
		public async Task Predicate_FiltersByKey_AppliesToBothAccessors()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsRefStruct<Packet>(p => p.Id == 42)]
				.Returns("hit")
				.OnSet(_ => throw new InvalidOperationException("write to 42"));

			string match = sut[new Packet(42, [])];
			string miss = sut[new Packet(1, [])];

			void ActWriteHit()
			{
				sut[new Packet(42, [])] = "boom";
			}

			await That(match).IsEqualTo("hit");
			await That(miss).IsEqualTo("")
				.Because("nothing matches, so Mockolate's default string value applies");
			await That(ActWriteHit).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task Returns_ConfiguresGet_OnSet_ConfiguresSet_Independent()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			string? captured = null;
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()]
				.Returns("get-value")
				.OnSet(v => captured = v);

			sut[new Packet(1, [])] = "set-value";
			string result = sut[new Packet(1, [])];

			await That(captured).IsEqualTo("set-value");
			await That(result).IsEqualTo("get-value");
		}

		[Fact]
		public async Task Throws_AppliesToBothAccessors()
		{
			IGeneratedPacketStore sut = IGeneratedPacketStore.CreateMock();
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Throws<InvalidOperationException>();

			void ActGet()
			{
				_ = sut[new Packet(1, [])];
			}

			void ActSet()
			{
				sut[new Packet(1, [])] = "x";
			}

			await That(ActGet).Throws<InvalidOperationException>();
			await That(ActSet).Throws<InvalidOperationException>();
		}
	}

	public sealed class ExtendedArityTests
	{
		[Fact]
		public async Task GetterOnlyArity5_PredicateFiltersByProjectedKey()
		{
			IBigPacketLookup5 sut = IBigPacketLookup5.CreateMock();
			sut.Mock.Setup[
					It.IsRefStruct<Packet>(p => p.Id == 1),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>(),
					It.IsAnyRefStruct<Packet>()]
				.Returns("matched");

			string hit = sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])];
			string miss = sut[new Packet(99, []), 10, new Packet(2, []), "tag", new Packet(3, [])];

			await That(hit).IsEqualTo("matched");
			await That(miss).IsEqualTo("");
		}

		[Fact]
		public async Task GetterOnlyArity5_Returns_ConfiguredValue()
		{
			IBigPacketLookup5 sut = IBigPacketLookup5.CreateMock();
			sut.Mock.Setup[
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>(),
					It.IsAnyRefStruct<Packet>()]
				.Returns("hit");

			string result = sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])];

			await That(result).IsEqualTo("hit");
		}

		[Fact]
		public async Task GetterOnlyArity5_Throws_ConfiguredException()
		{
			IBigPacketLookup5 sut = IBigPacketLookup5.CreateMock();
			sut.Mock.Setup[
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>(),
					It.IsAnyRefStruct<Packet>()]
				.Throws<KeyNotFoundException>();

			string Act()
			{
				return sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])];
			}

			await That(Act).Throws<KeyNotFoundException>();
		}

		[Fact]
		public async Task ReturnArity6_NonRefStructParameterGates_Matches()
		{
			IBigPacketParser sut = IBigPacketParser.CreateMock();
			sut.Mock.Setup.TryParse(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.Satisfies<int>(o => o > 0),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>())
				.Throws<InvalidOperationException>();

			void ActHit()
			{
				sut.TryParse(
					new Packet(1, []), new Packet(2, []), 7,
					new Packet(4, []), new Packet(5, []), "x");
			}

			void ActMiss()
			{
				sut.TryParse(
					new Packet(1, []), new Packet(2, []), -1,
					new Packet(4, []), new Packet(5, []), "x");
			}

			await That(ActHit).Throws<InvalidOperationException>();
			await That(ActMiss).DoesNotThrow();
		}

		[Fact]
		public async Task ReturnArity6_ReturnsConfiguredValue()
		{
			IBigPacketParser sut = IBigPacketParser.CreateMock();
			sut.Mock.Setup.TryParse(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>())
				.Returns(1234);

			int result = sut.TryParse(
				new Packet(1, []), new Packet(2, []), 10,
				new Packet(4, []), new Packet(5, []), "x");

			await That(result).IsEqualTo(1234);
		}

		[Fact]
		public async Task ReturnArity6_ReturnsFactory_InvokedPerCall()
		{
			IBigPacketParser sut = IBigPacketParser.CreateMock();
			int counter = 0;
			sut.Mock.Setup.TryParse(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>())
				.Returns(() => ++counter);

			int first = sut.TryParse(
				new Packet(1, []), new Packet(2, []), 10,
				new Packet(4, []), new Packet(5, []), "x");
			int second = sut.TryParse(
				new Packet(1, []), new Packet(2, []), 10,
				new Packet(4, []), new Packet(5, []), "x");

			await That(first).IsEqualTo(1);
			await That(second).IsEqualTo(2);
		}

		[Fact]
		public async Task ReturnArity6_ThrowsExceptionFactory_InvokedPerCall()
		{
			IBigPacketParser sut = IBigPacketParser.CreateMock();
			int built = 0;
			sut.Mock.Setup.TryParse(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>())
				.Throws(() => new InvalidOperationException($"call-{++built}"));

			Exception? first = null;
			try
			{
				_ = sut.TryParse(
					new Packet(1, []), new Packet(2, []), 0,
					new Packet(4, []), new Packet(5, []), "x");
			}
			catch (Exception ex)
			{
				first = ex;
			}

			Exception? second = null;
			try
			{
				_ = sut.TryParse(
					new Packet(1, []), new Packet(2, []), 0,
					new Packet(4, []), new Packet(5, []), "x");
			}
			catch (Exception ex)
			{
				second = ex;
			}

			await That(first!.Message).IsEqualTo("call-1");
			await That(second!.Message).IsEqualTo("call-2");
		}

		[Fact]
		public async Task ReturnArity6_ThrowsExceptionInstance_ThrowsSameInstance()
		{
			IBigPacketParser sut = IBigPacketParser.CreateMock();
			InvalidOperationException expected = new("arity6-boom");
			sut.Mock.Setup.TryParse(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>())
				.Throws(expected);

			Exception thrown = null!;
			try
			{
				_ = sut.TryParse(
					new Packet(1, []), new Packet(2, []), 0,
					new Packet(4, []), new Packet(5, []), "x");
			}
			catch (Exception ex)
			{
				thrown = ex;
			}

			await That(thrown).IsSameAs(expected);
		}

		[Fact]
		public async Task SetterOnlyArity5_OnSet_ReceivesValue()
		{
			IBigPacketSetter5 sut = IBigPacketSetter5.CreateMock();
			string? captured = null;
			sut.Mock.Setup[
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>(),
					It.IsAnyRefStruct<Packet>()]
				.OnSet(v => captured = v);

			sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])] = "stored";

			await That(captured).IsEqualTo("stored");
		}

		[Fact]
		public async Task SetterOnlyArity5_Throws_ConfiguredException()
		{
			IBigPacketSetter5 sut = IBigPacketSetter5.CreateMock();
			sut.Mock.Setup[
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<int>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAny<string>(),
					It.IsAnyRefStruct<Packet>()]
				.Throws<InvalidOperationException>();

			void Act()
			{
				sut[new Packet(1, []), 10, new Packet(2, []), "tag", new Packet(3, [])] = "x";
			}

			await That(Act).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task VoidArity5_DoesNotThrow_AfterThrows_OverridesPreviousConfiguration()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();

			// The ref-struct setup surface uses single-slot last-call-wins throw storage. A later
			// DoesNotThrow() on the same setup chain must clear the earlier Throws(...).
			sut.Mock.Setup.Absorb(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>())
				.Throws<InvalidOperationException>()
				.DoesNotThrow();

			void Act()
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(5, []));
			}

			await That(Act).DoesNotThrow();
		}

		[Fact]
		public async Task VoidArity5_NoSetup_IsNoOp()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();

			void Act()
			{
				sut.Absorb(
					new Packet(1, []),
					new Packet(2, []),
					new Packet(3, []),
					new Packet(4, []),
					new Packet(5, []));
			}

			await That(Act).DoesNotThrow();
		}

		[Fact]
		public async Task VoidArity5_PredicateMatchesSelectively()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();
			sut.Mock.Setup.Absorb(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsRefStruct<Packet>(p => p.Id == 99))
				.Throws<InvalidOperationException>();

			void ActHit()
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(99, []));
			}

			void ActMiss()
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(5, []));
			}

			await That(ActHit).Throws<InvalidOperationException>();
			await That(ActMiss).DoesNotThrow();
		}

		[Fact]
		public async Task VoidArity5_SetupThrows_ShouldThrowConfiguredException()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();
			sut.Mock.Setup.Absorb(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>())
				.Throws<InvalidOperationException>();

			void Act()
			{
				sut.Absorb(
					new Packet(1, []),
					new Packet(2, []),
					new Packet(3, []),
					new Packet(4, []),
					new Packet(5, []));
			}

			await That(Act).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task VoidArity5_ThrowsExceptionFactory_InvokedPerCall()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();
			int built = 0;
			sut.Mock.Setup.Absorb(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>())
				.Throws(() => new InvalidOperationException($"call-{++built}"));

			Exception? first = null;
			Exception? second = null;
			try
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(5, []));
			}
			catch (Exception ex)
			{
				first = ex;
			}

			try
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(5, []));
			}
			catch (Exception ex)
			{
				second = ex;
			}

			await That(first!.Message).IsEqualTo("call-1");
			await That(second!.Message).IsEqualTo("call-2");
		}

		[Fact]
		public async Task VoidArity5_ThrowsExceptionInstance_ThrowsSameInstance()
		{
			IBigPacketSink sut = IBigPacketSink.CreateMock();
			InvalidOperationException expected = new("boom");
			sut.Mock.Setup.Absorb(
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>(),
					It.IsAnyRefStruct<Packet>())
				.Throws(expected);

			Exception thrown = null!;
			try
			{
				sut.Absorb(
					new Packet(1, []), new Packet(2, []), new Packet(3, []),
					new Packet(4, []), new Packet(5, []));
			}
			catch (Exception ex)
			{
				thrown = ex;
			}

			await That(thrown).IsSameAs(expected);
		}
	}

	public sealed class IndexerGetterTests
	{
		[Fact]
		public async Task NoSetup_ReturnsFrameworkDefault()
		{
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();

			string result = sut[new Packet(42, [])];

			await That(result).IsEqualTo("");
		}

		[Fact]
		public async Task Predicate_FiltersByKey_PayloadReadable()
		{
			// Predicate reads the inline Span on the ref-struct key — the whole point of the
			// ref-struct pipeline: the payload flows through to the matcher without ever being
			// captured in a field.
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();
			sut.Mock.Setup[It.IsRefStruct<Packet>(p =>
					p.Payload.Length > 0 && p.Payload[0] == 0xFF)]
				.Returns("matched");

			byte[] hitBytes = [0xFF, 0x01,];
			byte[] missBytes = [0x00, 0x01,];

			string hit = sut[new Packet(1, hitBytes)];
			string miss = sut[new Packet(2, missBytes)];

			await That(hit).IsEqualTo("matched");
			await That(miss).IsEqualTo("")
				.Because("nothing matches, so the framework default applies and Mockolate's default for string is \"\"");
		}

		[Fact]
		public async Task RecordedInteraction_UsesRefStructMethodInvocation()
		{
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();

			_ = sut[new Packet(7, [])];

			RefStructMethodInvocation? recorded = ((IMock)sut).MockRegistry.Interactions
				.OfType<RefStructMethodInvocation>()
				.SingleOrDefault();

			await That(recorded).IsNotNull();
			await That(recorded!.Name).EndsWith(".get_Item");
			await That(recorded.ParameterNames.Single()).IsEqualTo("key");
		}

		[Fact]
		public async Task Returns_ConfiguredValue()
		{
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Returns("hit");

			string result = sut[new Packet(1, [])];

			await That(result).IsEqualTo("hit");
		}

		[Fact]
		public async Task Returns_Factory_InvokedPerCall()
		{
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();
			int calls = 0;
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Returns(() => $"call-{++calls}");

			string first = sut[new Packet(1, [])];
			string second = sut[new Packet(2, [])];

			await That(first).IsEqualTo("call-1");
			await That(second).IsEqualTo("call-2");
		}

		[Fact]
		public async Task Throws_ConfiguredException()
		{
			IGeneratedPacketLookup sut = IGeneratedPacketLookup.CreateMock();
			sut.Mock.Setup[It.IsAnyRefStruct<Packet>()].Throws<KeyNotFoundException>();

			string Act()
			{
				return sut[new Packet(1, [])];
			}

			await That(Act).Throws<KeyNotFoundException>();
		}
	}
}
#endif
