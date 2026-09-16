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

	public interface IPacketSlot
	{
		Packet Current { get; set; }
	}

	public interface IPacketProducer
	{
		Packet Produce();
	}

	public interface IPacketCatalog
	{
		Packet this[int index] { get; }
	}

	public interface IPacketBin
	{
		Packet this[int index] { get; set; }
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

		public class SpanHost
		{
			private byte[] _data = [1, 2,];

			public virtual Span<byte> Buffer
			{
				get => _data.AsSpan();
				set => _data = value.ToArray();
			}
		}

		public abstract class AbstractSpanHost
		{
			public abstract Span<byte> Buffer { get; set; }
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
		public async Task Setter_ShouldBeVerifiableByValue()
		{
			ISpanBuffer sut = ISpanBuffer.CreateMock();

			sut.Buffer = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify.Buffer.Set(new byte[] { 9, 8, }.AsSpan())).Once()
				.Because("SpanWrapper<T> compares its contents, so the raw-span overload matches");
		}

		[Fact]
		public async Task Setter_WithNonMatchingValue_ShouldNotBeVerified()
		{
			ISpanBuffer sut = ISpanBuffer.CreateMock();

			sut.Buffer = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify.Buffer.Set(new byte[] { 9, 7, }.AsSpan())).Never();
		}

		[Fact]
		public async Task Setter_ShouldBeVerifiableByPredicate()
		{
			ISpanBuffer sut = ISpanBuffer.CreateMock();

			sut.Buffer = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify.Buffer.Set(It.IsSpan<byte>(values => values.Length == 2))).Once();
		}

		[Fact]
		public async Task OnVirtualClassMember_ShouldRoundTripThroughBase()
		{
			SpanHost sut = SpanHost.CreateMock();

			byte[] initial = sut.Buffer.ToArray();
			sut.Buffer = new byte[] { 7, }.AsSpan();

			await That(initial).IsEqualTo(new byte[] { 1, 2, })
				.Because("without a setup the getter falls back to the base implementation");
			await That(sut.Buffer.ToArray()).IsEqualTo(new byte[] { 7, });
		}

		[Fact]
		public async Task OnAbstractClassMember_ShouldReturnConfiguredSpan()
		{
			AbstractSpanHost sut = AbstractSpanHost.CreateMock();
			sut.Mock.Setup.Buffer.InitializeWith(new byte[] { 3, 4, }.AsSpan());

			await That(sut.Buffer.ToArray()).IsEqualTo(new byte[] { 3, 4, });
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

		public class SpanHost
		{
			private byte[] _data = [1, 2,];

			public virtual Span<byte> this[int index]
			{
				get => _data.AsSpan(index);
				set => _data = value.ToArray();
			}
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
		public async Task SetterOnly_ShouldBeVerifiableByValue()
		{
			ISpanBufferSetter sut = ISpanBufferSetter.CreateMock();

			sut[4] = new byte[] { 9, 8, }.AsSpan();

			await That(sut.Mock.Verify[4].Set(new byte[] { 9, 8, }.AsSpan())).Once()
				.Because("the setter dispatches through ApplyIndexerSetter<SpanWrapper<byte>>");
		}

		[Fact]
		public async Task GetAndSet_ShouldBeVerifiableByValue()
		{
			ISpanBufferStore sut = ISpanBufferStore.CreateMock();

			sut[7] = new byte[] { 5, }.AsSpan();

			await That(sut.Mock.Verify[7].Set(new byte[] { 5, }.AsSpan())).Once();
		}

		[Fact]
		public async Task OnVirtualClassMember_ShouldRoundTripThroughBase()
		{
			SpanHost sut = SpanHost.CreateMock();

			byte[] initial = sut[0].ToArray();
			sut[0] = new byte[] { 7, }.AsSpan();

			await That(initial).IsEqualTo(new byte[] { 1, 2, })
				.Because("without a setup the getter falls back to the base implementation");
			await That(sut[0].ToArray()).IsEqualTo(new byte[] { 7, });
		}
	}
#endif

	public class PacketSource
	{
		private int _slotId = 1;

		public virtual Packet Current => new(11, []);

		public virtual Packet Slot
		{
			get => new(_slotId, []);
			set => _slotId = value.Id;
		}

		public virtual Packet this[int index]
		{
			get => new(_slotId + index, []);
			set => _slotId = value.Id;
		}

		public virtual Packet Produce() => new(12, []);
	}

	public class ProtectedPacketSource
	{
		private int _id = 3;

		protected virtual Packet Current
		{
			get => new(_id, []);
			set => _id = value.Id;
		}

		protected virtual Packet Produce() => new(_id + 100, []);

		public int ExerciseProtectedMembers()
		{
			Current = new Packet(9, []);
			return Current.Id + Produce().Id;
		}
	}

	// Gated because an `init` accessor needs IsExternalInit, which .NET Framework does not ship and
	// this project does not polyfill - both this declaration and the generated override need it.
#if NET8_0_OR_GREATER
	public class InitOnlyPacketSource
	{
		private readonly int _id = 4;

		public virtual Packet Current
		{
			get => new(_id, []);
			init => _id = value.Id;
		}
	}
#endif

	/// <summary>
	///     A ref struct in a value position has no setup surface, but a <c>virtual</c> class member still
	///     has a real implementation behind it and keeps working. No <c>Mockolate0003</c> suppression is
	///     needed here: the analyzer does not flag members that forward.
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
		public async Task VirtualPropertySetter_ShouldFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock();

			sut.Slot = new Packet(7, []);

			await That(sut.Slot.Id).IsEqualTo(7)
				.Because("the setter forwards to base, so the base getter observes the written value");
		}

		[Fact]
		public async Task VirtualIndexer_ShouldFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock();

			sut[0] = new Packet(8, []);

			await That(sut[0].Id).IsEqualTo(8)
				.Because("both indexer accessors forward to base");
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
		public async Task VirtualPropertySetter_WhenWrapping_ShouldUseWrappedInstance()
		{
			LoudPacketSource wrapped = new();
			PacketSource sut = PacketSource.CreateMock().Wrapping(wrapped);

			sut.Slot = new Packet(5, []);

			await That(wrapped.Slot.Id).IsEqualTo(5)
				.Because("the setter writes to the wrapped instance rather than to base");
		}

		[Fact]
		public async Task VirtualIndexerSetter_WhenWrapping_ShouldUseWrappedInstance()
		{
			LoudPacketSource wrapped = new();
			PacketSource sut = PacketSource.CreateMock().Wrapping(wrapped);

			sut[0] = new Packet(6, []);

			await That(wrapped[0].Id).IsEqualTo(6)
				.Because("the indexer setter writes to the wrapped instance rather than to base");
		}

		[Fact]
		public async Task ProtectedMembers_ShouldFallBackToBase()
		{
			ProtectedPacketSource sut = ProtectedPacketSource.CreateMock();

			int result = sut.ExerciseProtectedMembers();

			await That(result).IsEqualTo(9 + 109)
				.Because("a protected member cannot be reached through Wraps and forwards to base only");
		}

#if NET8_0_OR_GREATER
		// Suppressed rather than unflagged: the init accessor keeps the throwing stub, so the member
		// is only half-degraded and the analyzer is right to report it.
#pragma warning disable Mockolate0003 // Ref-struct usage is not supported on this compilation
		[Fact]
		public async Task InitOnlyProperty_ShouldFallBackToBase()
		{
			InitOnlyPacketSource sut = InitOnlyPacketSource.CreateMock();

			int id = sut.Current.Id;

			await That(id).IsEqualTo(4)
				.Because("the getter forwards even though the init accessor keeps the throwing stub");
		}
#pragma warning restore Mockolate0003
#endif

		[Fact]
		public async Task VirtualProperty_WithStrictBehavior_ShouldStillFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock(MockBehavior.Default with
			{
				ThrowWhenNotSetup = true,
			});

			int id = sut.Current.Id;

			await That(id).IsEqualTo(11)
				.Because("ThrowWhenNotSetup would reject a member that can never be set up");
		}

		[Fact]
		public async Task VirtualProperty_WithSkipBaseClass_ShouldStillFallBackToBase()
		{
			PacketSource sut = PacketSource.CreateMock(MockBehavior.Default with
			{
				SkipBaseClass = true,
			});

			int id = sut.Current.Id;

			await That(id).IsEqualTo(11)
				.Because("SkipBaseClass has no configured value to return in the base call's place");
		}
	}

#pragma warning disable Mockolate0003 // Ref-struct usage is not supported on this compilation
	public sealed class UnsupportedValuePositionTests
	{
		public abstract class AbstractPacketSource
		{
			public abstract Packet Current { get; }
		}

		public class PacketPicker
		{
			public virtual ref Packet Pick(ref Packet packet) => ref packet;
		}

		[Fact]
		public async Task RefStructProperty_ShouldThrowNotSupported()
		{
			IPacketHolder sut = IPacketHolder.CreateMock();

			void Act() => _ = sut.Current;

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*properties of a non-span ref struct type are not supported*").AsWildcard();
		}

		[Fact]
		public async Task RefStructPropertySetter_ShouldThrowNotSupported()
		{
			IPacketSlot sut = IPacketSlot.CreateMock();

			void Act() => sut.Current = new Packet(5, []);

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*properties of a non-span ref struct type are not supported*").AsWildcard();
		}

		[Fact]
		public async Task AbstractRefStructProperty_ShouldThrowNotSupported()
		{
			AbstractPacketSource sut = AbstractPacketSource.CreateMock();

			void Act() => _ = sut.Current;

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*properties of a non-span ref struct type are not supported*").AsWildcard()
				.Because("an abstract member has no implementation to forward to");
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
		public async Task RefReturningRefStructMethod_ShouldThrowNotSupported()
		{
			PacketPicker sut = PacketPicker.CreateMock();

			void Act()
			{
				Packet packet = new(1, []);
				_ = sut.Pick(ref packet);
			}

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*methods returning a non-span ref struct are not supported*").AsWildcard()
				.Because("`return base.Pick(ref packet)` is not valid for a by-ref return");
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
		public async Task RefStructIndexerSetter_ShouldThrowNotSupported()
		{
			IPacketBin sut = IPacketBin.CreateMock();

			void Act() => sut[0] = new Packet(5, []);

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

#if NET8_0_OR_GREATER
		public delegate void SpanInspector(ref readonly Span<int> values);

		[Fact]
		public async Task RefReadonlySpanParameterDelegate_ShouldThrowNotSupported()
		{
			SpanInspector sut = SpanInspector.CreateMock();

			void Act()
			{
				Span<int> values = new([1, 2,]);
				sut(in values);
			}

			await That(Act).Throws<NotSupportedException>()
				.WithMessage("*ref-struct parameters are not supported on delegate types*").AsWildcard()
				.Because(
					"`ref readonly` Span has no wrapper-based emit branch, so its setup surface would be the .NET 9-gated ref-struct one that a delegate's Invoke never dispatches through");
		}
#endif
	}
#pragma warning restore Mockolate0003
}
