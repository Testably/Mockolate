#if NET9_0_OR_GREATER
using GcPacket = Mockolate.Tests.GeneratorCoverage.Packet;
using Mockolate.Setup;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Tests.TestHelpers.RefStruct;

namespace Mockolate.Tests.RefStruct;

/// <summary>
///     End-to-end coverage for `out`, `ref`, and `ref readonly` ref-struct parameters routed
///     through <see cref="Mockolate.Parameters.IOutRefStructParameter{T}" /> /
///     <see cref="Mockolate.Parameters.IRefRefStructParameter{T}" />. Uses the
///     <see cref="GeneratorCoverage.Packet" /> ref struct (single-int ctor) to isolate this
///     scenario from the payload-carrying <see cref="TestHelpers.RefStruct.Packet" /> used by other RefStruct tests.
/// </summary>
/// <remarks>
///     Ref struct values cannot survive an <c>await</c> boundary, so each test captures
///     <see cref="GeneratorCoverage.Packet.Id" /> into an int before awaiting the assertion.
/// </remarks>
public sealed class RefStructOutRefParameterTests
{
	[Fact]
	public async Task OutRefStruct_WithIsOut_AssignsSetterValue()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.Produce(It.IsOut<GcPacket>(() => new GcPacket(7)));

		GcPacket packet = default;
		sut.Produce(out packet);
		int id = packet.Id;

		await That(id).IsEqualTo(7);
	}

	[Fact]
	public async Task OutRefStruct_WithIsAnyOutRefStruct_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.Produce(It.IsAnyOutRefStruct<GcPacket>());

		GcPacket packet = new(99);
		sut.Produce(out packet);
		int id = packet.Id;

		await That(id).IsEqualTo(0);
	}

	[Fact]
	public async Task OutRefStruct_NoSetup_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();

		GcPacket packet = new(99);
		sut.Produce(out packet);
		int id = packet.Id;

		await That(id).IsEqualTo(0);
	}

	[Fact]
	public async Task RefRefStruct_WithIsRef_TransformsValue()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.Mutate(It.IsRef<GcPacket>(p => new GcPacket(p.Id + 1)));

		GcPacket packet = new(41);
		sut.Mutate(ref packet);
		int id = packet.Id;

		await That(id).IsEqualTo(42);
	}

	[Fact]
	public async Task RefRefStruct_WithIsAnyRefRefStruct_LeavesValueUnchanged()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.Mutate(It.IsAnyRefRefStruct<GcPacket>());

		GcPacket packet = new(13);
		sut.Mutate(ref packet);
		int id = packet.Id;

		await That(id).IsEqualTo(13);
	}

	[Fact]
	public async Task RefRefStruct_WithPredicate_OnlyTransformsWhenPredicateHolds()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.Mutate(It.IsRef<GcPacket>(p => p.Id == 41, _ => new GcPacket(99)));

		GcPacket matching = new(41);
		sut.Mutate(ref matching);
		int matchingId = matching.Id;

		GcPacket nonMatching = new(1);
		sut.Mutate(ref nonMatching);
		int nonMatchingId = nonMatching.Id;

		await That(matchingId).IsEqualTo(99);
		await That(nonMatchingId).IsEqualTo(1);
	}

	[Fact]
	public async Task RefReadOnlyRefStruct_PassesValueUnchanged()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();

		GcPacket packet = new(17);
		sut.Inspect(in packet);
		int idAfter = packet.Id;

		await That(idAfter).IsEqualTo(17);
	}

	[Fact]
	public async Task OutSpan_WithIsOutSpan_AssignsSetterArray()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.ProduceSpan(It.IsOutSpan(() => new SpanWrapper<int>(new[] { 1, 2, 3 })));

		Span<int> span = default;
		sut.ProduceSpan(out span);
		int length = span.Length;
		int first = span[0];
		int last = span[2];

		await That(length).IsEqualTo(3);
		await That(first).IsEqualTo(1);
		await That(last).IsEqualTo(3);
	}

	[Fact]
	public async Task OutSpan_WithIsAnyOutSpan_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.ProduceSpan(It.IsAnyOutSpan<int>());

		Span<int> span = new[] { 99 };
		sut.ProduceSpan(out span);
		int length = span.Length;

		await That(length).IsEqualTo(0);
	}

	[Fact]
	public async Task OutSpan_NoSetup_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();

		Span<int> span = new[] { 99 };
		sut.ProduceSpan(out span);
		int length = span.Length;

		await That(length).IsEqualTo(0);
	}

	[Fact]
	public async Task OutSpan_WithDoCallback_RunsAction()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		int observedFirst = 0;
		int observedLength = 0;
		sut.Mock.Setup.ProduceSpan(
			It.IsOutSpan(() => new SpanWrapper<int>(new[] { 7, 8 }))
				.Do(w =>
				{
					observedFirst = w.SpanValues[0];
					observedLength = w.SpanValues.Length;
				}));

		Span<int> span = default;
		sut.ProduceSpan(out span);

		await That(observedFirst).IsEqualTo(7);
		await That(observedLength).IsEqualTo(2);
	}

	[Fact]
	public async Task RefSpan_WithIsRefSpan_TransformsArray()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateSpan(It.IsRefSpan<int>(w => new SpanWrapper<int>(new[] { w.SpanValues[0] + 1 })));

		Span<int> span = new[] { 41 };
		sut.MutateSpan(ref span);
		int first = span[0];
		int length = span.Length;

		await That(first).IsEqualTo(42);
		await That(length).IsEqualTo(1);
	}

	[Fact]
	public async Task RefSpan_WithIsAnyRefSpan_LeavesValueUnchanged()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateSpan(It.IsAnyRefSpan<int>());

		Span<int> span = new[] { 13, 14 };
		sut.MutateSpan(ref span);
		int first = span[0];
		int length = span.Length;

		await That(first).IsEqualTo(13);
		await That(length).IsEqualTo(2);
	}

	[Fact]
	public async Task RefSpan_WithPredicate_OnlyTransformsWhenPredicateHolds()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateSpan(
			It.IsRefSpan<int>(
				w => w.SpanValues.Length > 0 && w.SpanValues[0] == 41,
				_ => new SpanWrapper<int>(new[] { 99 })));

		Span<int> matching = new[] { 41 };
		sut.MutateSpan(ref matching);
		int matchingFirst = matching[0];

		Span<int> nonMatching = new[] { 1 };
		sut.MutateSpan(ref nonMatching);
		int nonMatchingFirst = nonMatching[0];

		await That(matchingFirst).IsEqualTo(99);
		await That(nonMatchingFirst).IsEqualTo(1);
	}

	[Fact]
	public async Task OutReadOnlySpan_WithIsOutReadOnlySpan_AssignsSetterArray()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.ProduceReadOnlySpan(
			It.IsOutReadOnlySpan(() => new ReadOnlySpanWrapper<int>(new[] { 5, 6, 7 })));

		ReadOnlySpan<int> span = default;
		sut.ProduceReadOnlySpan(out span);
		int length = span.Length;
		int first = span[0];

		await That(length).IsEqualTo(3);
		await That(first).IsEqualTo(5);
	}

	[Fact]
	public async Task OutReadOnlySpan_WithIsAnyOutReadOnlySpan_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.ProduceReadOnlySpan(It.IsAnyOutReadOnlySpan<int>());

		ReadOnlySpan<int> span = new[] { 99 };
		sut.ProduceReadOnlySpan(out span);
		int length = span.Length;

		await That(length).IsEqualTo(0);
	}

	[Fact]
	public async Task OutReadOnlySpan_NoSetup_AssignsDefault()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();

		ReadOnlySpan<int> span = new[] { 99 };
		sut.ProduceReadOnlySpan(out span);
		int length = span.Length;

		await That(length).IsEqualTo(0);
	}

	[Fact]
	public async Task RefReadOnlySpan_WithIsRefReadOnlySpan_TransformsArray()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateReadOnlySpan(
			It.IsRefReadOnlySpan<int>(
				w => new ReadOnlySpanWrapper<int>(new[] { w.ReadOnlySpanValues[0] + 1 })));

		ReadOnlySpan<int> span = new[] { 41 };
		sut.MutateReadOnlySpan(ref span);
		int first = span[0];

		await That(first).IsEqualTo(42);
	}

	[Fact]
	public async Task RefReadOnlySpan_WithIsAnyRefReadOnlySpan_LeavesValueUnchanged()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateReadOnlySpan(It.IsAnyRefReadOnlySpan<int>());

		ReadOnlySpan<int> span = new[] { 13 };
		sut.MutateReadOnlySpan(ref span);
		int first = span[0];

		await That(first).IsEqualTo(13);
	}

	[Fact]
	public async Task RefReadOnlySpan_WithPredicateOnly_GatesMatchWithoutMutating()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.MutateReadOnlySpan(
				It.IsRefReadOnlySpan<int>(w => w.ReadOnlySpanValues.Length == 1))
			.Throws(new InvalidOperationException("matched"));

		ReadOnlySpan<int> matching = new[] { 42 };
		Exception? matchingThrew = null;
		try
		{
			sut.MutateReadOnlySpan(ref matching);
		}
		catch (Exception ex)
		{
			matchingThrew = ex;
		}
		int matchingFirst = matching[0];
		int matchingLength = matching.Length;

		ReadOnlySpan<int> nonMatching = new[] { 13, 14 };
		Exception? nonMatchingThrew = null;
		try
		{
			sut.MutateReadOnlySpan(ref nonMatching);
		}
		catch (Exception ex)
		{
			nonMatchingThrew = ex;
		}
		int nonMatchingFirst = nonMatching[0];

		await That(matchingThrew).IsNotNull();
		await That(matchingFirst).IsEqualTo(42);
		await That(matchingLength).IsEqualTo(1);
		await That(nonMatchingThrew).IsNull();
		await That(nonMatchingFirst).IsEqualTo(13);
	}

	[Fact]
	public async Task RefReadOnlySpan_WithIsAnyRefRefStruct_MatchesViaRefStructPipeline()
	{
		IRefStructConsumer sut = IRefStructConsumer.CreateMock();
		sut.Mock.Setup.InspectSpan(It.IsAnyRefRefStruct<Span<int>>())
			.Throws(new InvalidOperationException("matched"));

		Span<int> span = new[] { 17, 18, 19 };
		Exception? thrown = null;
		try
		{
			sut.InspectSpan(in span);
		}
		catch (Exception ex)
		{
			thrown = ex;
		}
		int length = span.Length;
		int first = span[0];

		await That(thrown).IsNotNull();
		await That(length).IsEqualTo(3);
		await That(first).IsEqualTo(17);
	}
}
#endif
