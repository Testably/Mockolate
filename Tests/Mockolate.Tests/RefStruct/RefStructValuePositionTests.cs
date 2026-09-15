#if NET9_0_OR_GREATER
using System;

namespace Mockolate.Tests.RefStruct;

/// <summary>
///     Covers ref structs in <i>value</i> positions - event arguments, method and delegate returns,
///     property types and indexer values. These once made the generator emit uncompilable code
///     (CS9244); they now either flow through the Span wrapper or degrade to a
///     <see cref="NotSupportedException" /> stub with no setup surface.
/// </summary>
public sealed class RefStructValuePositionTests
{
	public delegate void PacketEventHandler(Packet packet);

	public delegate Packet PacketFactory();

	public delegate void PacketConsumer(Packet packet);

	public interface IPacketNotifier
	{
		event PacketEventHandler PacketReceived;
	}

	public interface ISpanEventSource
	{
		event EventHandler<Packet> Received;
	}

	public interface ISpanBuffer
	{
		Span<byte> Buffer { get; set; }
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
		public async Task RaiseWithEventHandlerOfRefStruct_ShouldReachSubscriber()
		{
			ISpanEventSource sut = ISpanEventSource.CreateMock();
			int receivedId = 0;
			sut.Received += (_, packet) => receivedId = packet.Id;

			sut.Mock.Raise.Received(sut, new Packet(7, []));

			await That(receivedId).IsEqualTo(7)
				.Because("EventHandler<T> with a ref-struct T is raised like any other event");
		}

		[Fact]
		public async Task RaiseWithoutSubscriber_ShouldNotThrow()
		{
			IPacketNotifier sut = IPacketNotifier.CreateMock();

			void Act() => sut.Mock.Raise.PacketReceived(new Packet(1, []));

			await That(Act).DoesNotThrow();
		}
	}

	public sealed class SpanPropertyTests
	{
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
			PacketConsumer sut = PacketConsumer.CreateMock();

			void Act() => sut(new Packet(1, []));

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*ref-struct parameters are not supported on delegate types*").AsWildcard();
		}
	}
}
#endif
