#if NET9_0_OR_GREATER
using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers.RefStruct;

namespace Mockolate.Tests.RefStruct;

/// <summary>
///     Low-level unit coverage for the ref-struct primitives (matchers, setup types, the
///     <see cref="RefStructMethodInvocation" /> interaction).
///     End-to-end coverage through the generator lives in <see cref="GeneratedPacketSinkTests" />,
///     which uses the <see cref="Packet" /> ref struct defined alongside it.
/// </summary>
public sealed class RefStructPrimitivesTests
{
	public sealed class MatcherTests
	{
		[Fact]
		public async Task IsAnyRefStruct_ShouldMatchAnyValue()
		{
			IParameter<Packet> sut = It.IsAnyRefStruct<Packet>();

			bool result = ((IParameterMatch<Packet>)sut).Matches(new Packet(1, []));

			await That(result).IsTrue();
		}

		[Fact]
		public async Task IsAnyRefStruct_ToString_ShouldIncludeTypeName()
		{
			IParameter<Packet> sut = It.IsAnyRefStruct<Packet>();

			string? result = sut.ToString();

			await That(result).IsEqualTo("It.IsAnyRefStruct<Packet>()");
		}

		[Fact]
		public async Task IsRefStruct_WithPredicate_ShouldFilterById()
		{
			IParameter<Packet> sut = It.IsRefStruct<Packet>(p => p.Id > 100);
			IParameterMatch<Packet> match = (IParameterMatch<Packet>)sut;

			bool high = match.Matches(new Packet(500, []));
			bool low = match.Matches(new Packet(1, []));

			await That(high).IsTrue();
			await That(low).IsFalse();
		}

		[Fact]
		public async Task IsRefStruct_PredicateSeesSpanPayload()
		{
			// Demonstrates the predicate can read into the ref struct's inline Span — which is the
			// whole reason ref-struct parameters exist. A non-ref-struct matcher could never do this
			// because the Span couldn't flow through the parameter type.
			IParameter<Packet> sut = It.IsRefStruct<Packet>(p =>
				p.Payload.Length > 0 && p.Payload[0] == 0xFF);
			IParameterMatch<Packet> match = (IParameterMatch<Packet>)sut;

			byte[] hit = [0xFF, 0x01];
			byte[] miss = [0x00, 0x01];

			bool matchedHit = match.Matches(new Packet(1, hit));
			bool matchedMiss = match.Matches(new Packet(1, miss));

			await That(matchedHit).IsTrue();
			await That(matchedMiss).IsFalse();
		}

		[Fact]
		public async Task IsAnyRefStruct_NonGenericMatches_ReturnsFalseForBoxedSlot()
		{
			IParameter sut = It.IsAnyRefStruct<Packet>();

			bool result = sut.Matches(null);

			await That(result).IsFalse()
				.Because(
					"the non-generic IParameter.Matches(object?) is the covariance-safe fallback and is deliberately inert for ref-struct matchers, as the boxed path can never be a ref struct");
		}
	}

	public sealed class InteractionTests
	{
		[Fact]
		public async Task SingleArityInteraction_RendersParameterNameWithPlaceholder()
		{
			RefStructMethodInvocation invocation = new("Consume", "packet");

			string? rendered = invocation.ToString();

			await That(rendered).IsEqualTo("invoke method Consume(packet: <ref struct>)");
		}

		[Fact]
		public async Task MultiArityInteraction_RendersAllParameterNames()
		{
			RefStructMethodInvocation invocation = new("Encode", "left", "right", "scratch");

			string? rendered = invocation.ToString();

			await That(rendered).IsEqualTo(
				"invoke method Encode(left: <ref struct>, right: <ref struct>, scratch: <ref struct>)");
		}
	}

	public sealed class MultiAritySetupTests
	{
		[Fact]
		public async Task Arity2_Matches_AllMatchersMustAccept()
		{
			RefStructVoidMethodSetup<Packet, Packet> setup = new(
				"Encode",
				(IParameterMatch<Packet>)It.IsRefStruct<Packet>(p => p.Id == 1),
				(IParameterMatch<Packet>)It.IsRefStruct<Packet>(p => p.Id == 2));

			bool bothMatch = setup.Matches(new Packet(1, []), new Packet(2, []));
			bool firstOnly = setup.Matches(new Packet(1, []), new Packet(99, []));
			bool secondOnly = setup.Matches(new Packet(99, []), new Packet(2, []));

			await That(bothMatch).IsTrue();
			await That(firstOnly).IsFalse();
			await That(secondOnly).IsFalse();
		}

		[Fact]
		public async Task Arity2_Invoke_ThrowsConfiguredException()
		{
			RefStructVoidMethodSetup<Packet, Packet> setup = new("Encode");
			((IRefStructVoidMethodSetup<Packet, Packet>)setup)
				.Throws<InvalidOperationException>();

			void Act() => setup.Invoke(new Packet(1, []), new Packet(2, []));

			await That(Act).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task Arity3_Matches_AllMatchersMustAccept()
		{
			RefStructVoidMethodSetup<Packet, Packet, Packet> setup = new(
				"Fold",
				It.IsRefStruct<Packet>(p => p.Id == 1) as IParameterMatch<Packet>,
				null,
				It.IsRefStruct<Packet>(p => p.Id == 3) as IParameterMatch<Packet>);

			bool match = setup.Matches(
				new Packet(1, []), new Packet(99, []), new Packet(3, []));
			bool miss = setup.Matches(
				new Packet(2, []), new Packet(99, []), new Packet(3, []));

			await That(match).IsTrue();
			await That(miss).IsFalse();
		}

		[Fact]
		public async Task Arity4_Invoke_ThrowsFromFactory()
		{
			RefStructVoidMethodSetup<Packet, Packet, Packet, Packet> setup =
				new("Merge");
			((IRefStructVoidMethodSetup<Packet, Packet, Packet, Packet>)setup)
				.Throws(() => new NotSupportedException("arity-4"));

			void Act() => setup.Invoke(
				new Packet(1, []), new Packet(2, []),
				new Packet(3, []), new Packet(4, []));

			await That(Act).Throws<NotSupportedException>().WithMessage("arity-4");
		}
	}

	public sealed class ReturnSetupTests
	{
		[Fact]
		public async Task Arity1_Returns_Value_ReturnsConfiguredValue()
		{
			RefStructReturnMethodSetup<int, Packet> setup = new(
				"TryParse", It.IsAnyRefStruct<Packet>() as IParameterMatch<Packet>);
			((IRefStructReturnMethodSetup<int, Packet>)setup).Returns(42);

			int result = setup.Invoke(new Packet(1, []));

			await That(result).IsEqualTo(42);
		}

		[Fact]
		public async Task Arity1_Returns_Factory_InvokedPerCall()
		{
			int calls = 0;
			RefStructReturnMethodSetup<int, Packet> setup = new("TryParse");
			((IRefStructReturnMethodSetup<int, Packet>)setup)
				.Returns(() => ++calls);

			int first = setup.Invoke(new Packet(1, []));
			int second = setup.Invoke(new Packet(2, []));

			await That(first).IsEqualTo(1);
			await That(second).IsEqualTo(2);
		}

		[Fact]
		public async Task Arity1_NoReturnConfigured_UsesDefaultFactory()
		{
			RefStructReturnMethodSetup<string, Packet> setup = new("Decode");

			string result = setup.Invoke(new Packet(1, []), static () => "fallback");

			await That(result).IsEqualTo("fallback");
		}

		[Fact]
		public async Task Arity1_Throws_BeatsReturn_WhenBothConfigured()
		{
			RefStructReturnMethodSetup<int, Packet> setup = new("TryParse");
			((IRefStructReturnMethodSetup<int, Packet>)setup)
				.Returns(42)
				.Throws<InvalidOperationException>();

			void Act() => setup.Invoke(new Packet(1, []));

			await That(Act).Throws<InvalidOperationException>();
		}

		[Fact]
		public async Task Arity1_HasReturnValue_ReflectsConfiguration()
		{
			RefStructReturnMethodSetup<int, Packet> setup = new("TryParse");
			bool before = setup.HasReturnValue;

			((IRefStructReturnMethodSetup<int, Packet>)setup).Returns(7);

			await That(before).IsFalse();
			await That(setup.HasReturnValue).IsTrue();
		}

		[Fact]
		public async Task Arity2_Matches_AllMatchersMustAccept()
		{
			RefStructReturnMethodSetup<bool, Packet, Packet> setup = new(
				"Compare",
				It.IsRefStruct<Packet>(p => p.Id == 1) as IParameterMatch<Packet>,
				It.IsRefStruct<Packet>(p => p.Id == 2) as IParameterMatch<Packet>);
			((IRefStructReturnMethodSetup<bool, Packet, Packet>)setup).Returns(true);

			bool match = setup.Matches(new Packet(1, []), new Packet(2, []));
			bool miss = setup.Matches(new Packet(1, []), new Packet(99, []));

			await That(match).IsTrue();
			await That(miss).IsFalse();
		}

		[Fact]
		public async Task Arity3_Invoke_ReturnsConfiguredValue()
		{
			RefStructReturnMethodSetup<string, Packet, Packet, Packet> setup =
				new("Blend");
			((IRefStructReturnMethodSetup<string, Packet, Packet, Packet>)setup)
				.Returns("blended");

			string result = setup.Invoke(
				new Packet(1, []), new Packet(2, []), new Packet(3, []));

			await That(result).IsEqualTo("blended");
		}

		[Fact]
		public async Task Arity4_Invoke_RunsThrowOverReturn()
		{
			RefStructReturnMethodSetup<int, Packet, Packet, Packet, Packet> setup =
				new("Combine");
			((IRefStructReturnMethodSetup<int, Packet, Packet, Packet, Packet>)setup)
				.Returns(99)
				.Throws(new NotSupportedException());

			void Act() => setup.Invoke(
				new Packet(1, []), new Packet(2, []),
				new Packet(3, []), new Packet(4, []));

			await That(Act).Throws<NotSupportedException>();
		}
	}

	public sealed class IndexerGetterSetupTests
	{
		[Fact]
		public async Task Arity1_Returns_Value_ReturnsConfiguredValue()
		{
			RefStructIndexerGetterSetup<int, Packet> setup = new(
				"get_Item", It.IsAnyRefStruct<Packet>() as IParameterMatch<Packet>);
			((IRefStructIndexerGetterSetup<int, Packet>)setup).Returns(7);

			int result = setup.Invoke(new Packet(1, []));

			await That(result).IsEqualTo(7);
		}

		[Fact]
		public async Task Arity1_PredicateMatches_FiltersByKey()
		{
			RefStructIndexerGetterSetup<string, Packet> setup = new(
				"get_Item",
				It.IsRefStruct<Packet>(p => p.Id == 42) as IParameterMatch<Packet>);
			((IRefStructIndexerGetterSetup<string, Packet>)setup).Returns("hit");

			bool matchesHit = setup.Matches(new Packet(42, []));
			bool matchesMiss = setup.Matches(new Packet(7, []));

			await That(matchesHit).IsTrue();
			await That(matchesMiss).IsFalse();
		}

		[Fact]
		public async Task Arity1_NoReturnConfigured_UsesDefaultFactory()
		{
			RefStructIndexerGetterSetup<string, Packet> setup = new("get_Item");

			string result = setup.Invoke(new Packet(1, []), static () => "fallback");

			await That(result).IsEqualTo("fallback");
		}

		[Fact]
		public async Task Arity1_Throws_TakesPrecedenceOverReturn()
		{
			RefStructIndexerGetterSetup<int, Packet> setup = new("get_Item");
			((IRefStructIndexerGetterSetup<int, Packet>)setup)
				.Returns(42)
				.Throws<KeyNotFoundException>();

			void Act() => setup.Invoke(new Packet(1, []));

			await That(Act).Throws<KeyNotFoundException>();
		}

		[Fact]
		public async Task Arity2_Matches_AllMatchersMustAccept()
		{
			RefStructIndexerGetterSetup<int, Packet, Packet> setup = new(
				"get_Item",
				It.IsRefStruct<Packet>(p => p.Id == 1) as IParameterMatch<Packet>,
				It.IsRefStruct<Packet>(p => p.Id == 2) as IParameterMatch<Packet>);

			bool bothMatch = setup.Matches(new Packet(1, []), new Packet(2, []));
			bool secondOnly = setup.Matches(new Packet(99, []), new Packet(2, []));

			await That(bothMatch).IsTrue();
			await That(secondOnly).IsFalse();
		}

		[Fact]
		public async Task Arity4_Invoke_ReturnsFactoryValue()
		{
			RefStructIndexerGetterSetup<string, Packet, Packet, Packet, Packet>
				setup = new("get_Item");
			((IRefStructIndexerGetterSetup<string, Packet, Packet, Packet, Packet>)
				setup).Returns(() => "lazy");

			string result = setup.Invoke(
				new Packet(1, []), new Packet(2, []),
				new Packet(3, []), new Packet(4, []));

			await That(result).IsEqualTo("lazy");
		}
	}
}
#endif
