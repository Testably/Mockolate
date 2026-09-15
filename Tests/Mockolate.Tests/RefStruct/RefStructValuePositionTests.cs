using System;
using Mockolate.Tests.TestHelpers.RefStruct;

namespace Mockolate.Tests.RefStruct;

/// <summary>
///     Covers ref structs in <i>value</i> positions - event arguments, method and delegate returns,
///     property types and indexer values. These once made the generator emit uncompilable code
///     (CS9244); they now either flow through the Span wrapper or lose their setup surface - a
///     <c>virtual</c> class member then forwards to the wrapped instance or to <c>base</c>, everything
///     else throws <see cref="NotSupportedException" />.
/// </summary>
/// <remarks>
///     Deliberately not gated to net9.0+: none of these shapes route through the ref-struct setup
///     pipeline, so they must degrade identically on every supported target. Two nested fixtures are
///     gated on what the <i>target</i> offers, not on the mock: <c>SpanWrapper&lt;T&gt;</c> ships from
///     net8.0 upwards, and <c>EventHandler&lt;T&gt;</c> gained its <c>allows ref struct</c>
///     anti-constraint in .NET 9.
/// </remarks>
public sealed class RefStructValuePositionTests
{
	public delegate void PacketHandler(Packet packet);

	public delegate Packet PacketFactory();

	public interface IPacketNotifier
	{
		event PacketHandler PacketReceived;
	}

	public interface IPacketHolder
	{
		Packet Current { get; }
	}

	public interface IPacketProducer
	{
		Packet Produce();
	}

	public interface IPacketCatalog
	{
		Packet this[int index] { get; }
	}

	public sealed class EventTests
	{
		[Fact]
		public async Task RaiseWithRefStructArgument_ShouldReachSubscriber()
		{
			IPacketNotifier sut = IPacketNotifier.CreateMock();
			int receivedId = 0;
			sut.PacketReceived += packet => receivedId = packet.Id;

			sut.Mock.Raise.PacketReceived(new Packet(42, []));

			await That(receivedId).IsEqualTo(42)
				.Because("the strongly-typed Raise overload passes the ref struct straight to the delegate");
		}

		[Fact]
		public async Task RaiseWithoutSubscriber_ShouldNotThrow()
		{
			IPacketNotifier sut = IPacketNotifier.CreateMock();

			void Act() => sut.Mock.Raise.PacketReceived(new Packet(1, []));

			await That(Act).DoesNotThrow();
		}

#if NET9_0_OR_GREATER
		public interface ISpanEventSource
		{
			event EventHandler<Packet> Received;
		}

		[Fact]
		public async Task RaiseWithEventHandlerOfRefStruct_ShouldReachSubscriber()
		{
			ISpanEventSource sut = ISpanEventSource.CreateMock();
			int receivedId = 0;
			sut.Received += (_, packet) => receivedId = packet.Id;

			sut.Mock.Raise.Received(sut, new Packet(7, []));

			await That(receivedId).IsEqualTo(7)
				.Because("EventHandler<T> with a ref-struct T is raised like any other event");
		}
#endif
	}

#if NET8_0_OR_GREATER
	public sealed class SpanPropertyTests
	{
		public interface ISpanBuffer
		{
			Span<byte> Buffer { get; set; }
		}

		[Fact]
		public async Task Getter_ShouldReturnConfiguredSpan()
		{
			ISpanBuffer sut = ISpanBuffer.CreateMock();
			byte[] expected = [1, 2, 3,];
			sut.Mock.Setup.Buffer.InitializeWith(expected.AsSpan());

			byte[] result = sut.Buffer.ToArray();

			await That(result).IsEqualTo(expected)
				.Because("a Span-typed property round-trips through SpanWrapper<T>");
		}

		[Fact]
		public async Task Setter_ShouldBeRecorded()
		{
			ISpanBuffer sut = ISpanBuffer.CreateMock();

			sut.Buffer = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify.Buffer.Set(It.IsAny<Mockolate.Setup.SpanWrapper<byte>>())).Once();
		}
	}

	public sealed class SpanIndexerTests
	{
		public interface ISpanBufferGetter
		{
			Span<byte> this[int index] { get; }
		}

		public interface ISpanBufferSetter
		{
			Span<byte> this[int index] { set; }
		}

		public interface ISpanBufferStore
		{
			Span<byte> this[int index] { get; set; }
		}

		[Fact]
		public async Task GetterOnly_ShouldReturnConfiguredSpan()
		{
			ISpanBufferGetter sut = ISpanBufferGetter.CreateMock();
			byte[] expected = [1, 2, 3,];
			sut.Mock.Setup[2].Returns(expected.AsSpan());

			byte[] result = sut[2].ToArray();

			await That(result).IsEqualTo(expected)
				.Because("a Span-valued indexer round-trips through SpanWrapper<T>");
		}

		[Fact]
		public async Task SetterOnly_ShouldBeRecorded()
		{
			ISpanBufferSetter sut = ISpanBufferSetter.CreateMock();

			sut[4] = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify[4].Set(It.IsAny<Mockolate.Setup.SpanWrapper<byte>>())).Once()
				.Because("the setter dispatches through ApplyIndexerSetter<SpanWrapper<byte>>");
		}

		[Fact]
		public async Task GetAndSet_ShouldBeRecorded()
		{
			ISpanBufferStore sut = ISpanBufferStore.CreateMock();

			sut[7] = new byte[] { 5, }.AsSpan();

			await That(sut.Mock.Verify[7].Set(It.IsAny<Mockolate.Setup.SpanWrapper<byte>>())).Once();
		}
	}
#endif

	public class PacketSource
	{
		public virtual Packet Current => new(11, []);

		public virtual Packet Produce() => new(12, []);
	}

	public abstract class AbstractPacketSource
	{
		public abstract Packet Current { get; }
	}

#pragma warning disable Mockolate0003 // Ref-struct usage is not supported on this compilation
	/// <summary>
	///     A ref struct in a value position has no setup surface, but a <c>virtual</c> class member still
	///     has a real implementation behind it and keeps working.
	/// </summary>
	public sealed class ClassPassthroughTests
	{
		private sealed class LoudPacketSource : PacketSource
		{
			public override Packet Current => new(99, []);

			public override Packet Produce() => new(98, []);
		}

		[Fact]
		public async Task VirtualProperty_ShouldFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock();

			int id = sut.Current.Id;

			await That(id).IsEqualTo(11)
				.Because("a virtual member without a setup surface forwards to base instead of throwing");
		}

		[Fact]
		public async Task VirtualMethod_ShouldFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock();

			int id = sut.Produce().Id;

			await That(id).IsEqualTo(12)
				.Because("a virtual member without a setup surface forwards to base instead of throwing");
		}

		[Fact]
		public async Task VirtualProperty_WhenWrapping_ShouldUseWrappedInstance()
		{
			PacketSource sut = PacketSource.CreateMock().Wrapping(new LoudPacketSource());

			int id = sut.Current.Id;

			await That(id).IsEqualTo(99)
				.Because("the passthrough honours Wraps the same way every other member does");
		}

		[Fact]
		public async Task VirtualMethod_WhenWrapping_ShouldUseWrappedInstance()
		{
			PacketSource sut = PacketSource.CreateMock().Wrapping(new LoudPacketSource());

			int id = sut.Produce().Id;

			await That(id).IsEqualTo(98)
				.Because("the passthrough honours Wraps the same way every other member does");
		}

		[Fact]
		public async Task AbstractProperty_ShouldThrowNotSupported()
		{
			AbstractPacketSource sut = AbstractPacketSource.CreateMock();

			void Act() => _ = sut.Current;

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*properties of a non-span ref struct type are not supported*").AsWildcard()
				.Because("an abstract member has no implementation to forward to");
		}
	}

	public sealed class UnsupportedValuePositionTests
	{
		[Fact]
		public async Task RefStructProperty_ShouldThrowNotSupported()
		{
			IPacketHolder sut = IPacketHolder.CreateMock();

			void Act() => _ = sut.Current;

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*properties of a non-span ref struct type are not supported*").AsWildcard();
		}

		[Fact]
		public async Task RefStructReturningMethod_ShouldThrowNotSupported()
		{
			IPacketProducer sut = IPacketProducer.CreateMock();

			void Act() => _ = sut.Produce();

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*methods returning a non-span ref struct are not supported*").AsWildcard();
		}

		[Fact]
		public async Task RefStructIndexer_ShouldThrowNotSupported()
		{
			IPacketCatalog sut = IPacketCatalog.CreateMock();

			void Act() => _ = sut[0];

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*indexers returning a non-span ref struct are not supported*").AsWildcard();
		}

		[Fact]
		public async Task RefStructReturningDelegate_ShouldThrowNotSupported()
		{
			PacketFactory sut = PacketFactory.CreateMock();

			void Act() => _ = sut();

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*methods returning a non-span ref struct are not supported*").AsWildcard();
		}

		[Fact]
		public async Task RefStructParameterDelegate_ShouldThrowNotSupported()
		{
			PacketHandler sut = PacketHandler.CreateMock();

			void Act() => sut(new Packet(1, []));

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*ref-struct parameters are not supported on delegate types*").AsWildcard();
		}
	}
#pragma warning restore Mockolate0003
}
