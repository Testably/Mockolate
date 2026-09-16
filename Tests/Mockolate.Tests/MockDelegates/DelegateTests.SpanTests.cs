#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockDelegates;

public partial class DelegateTests
{
	public sealed class SpanTests
	{
		[Fact]
		public async Task ReadOnlySpanParameter_ShouldSupportSetup()
		{
			ReadOnlySpanConsumer sut = ReadOnlySpanConsumer.CreateMock();
			sut.Mock.Setup(It.IsReadOnlySpan<char>(values => values.Length == 3)).Returns(42);

			int result = sut(new ReadOnlySpan<char>(['a', 'b', 'c',]));

			await That(result).IsEqualTo(42)
				.Because(
					"a by-value ReadOnlySpan parameter flows through ReadOnlySpanWrapper<T>, so the delegate keeps its setup surface instead of degrading to a NotSupportedException stub");
		}

		[Fact]
		public async Task SpanParameter_ShouldSupportSetup()
		{
			SpanConsumer sut = SpanConsumer.CreateMock();
			sut.Mock.Setup(It.IsSpan<byte>(values => values.Length == 2)).Returns(42);

			int result = sut(new Span<byte>([1, 2,]));

			await That(result).IsEqualTo(42)
				.Because(
					"a by-value Span parameter flows through SpanWrapper<T>, so the delegate keeps its setup surface instead of degrading to a NotSupportedException stub");
		}

		[Fact]
		public async Task SpanParameter_ShouldSupportVerify()
		{
			SpanConsumer sut = SpanConsumer.CreateMock();

			_ = sut(new Span<byte>([1, 2,]));

			await That(sut.Mock.Verify(It.IsSpan<byte>(values => values.Length == 2))).Once()
				.Because("the invocation is recorded rather than stubbed out");
		}

		internal delegate int ReadOnlySpanConsumer(ReadOnlySpan<char> text);

		internal delegate int SpanConsumer(Span<byte> buffer);
	}
}
#endif
