#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifyGotIndexerTests
{
	public sealed class SpanTests
	{
		[Test]
		public async Task Memory_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new Memory<int>([1, 2, 3,])];

			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 2)].Got()).Never();
			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 3)].Got()).Once();
			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 4)].Got()).Never();
		}

		[Test]
		public async Task Memory_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new Memory<int>()];
			_ = sut[new Span<int>([1, 2, 3,])];
			_ = sut[new Memory<int>([1, 2,])];

			await That(sut.Mock.Verify[It.IsAny<Memory<int>>()].Got()).Twice();
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new ReadOnlySpan<int>([1, 2, 3,])];

			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 2)].Got()).Never();
			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 3)].Got()).Once();
			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 4)].Got()).Never();
		}

		[Test]
		public async Task ReadOnlySpan_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new ReadOnlySpan<int>()];
			_ = sut[new Span<int>([1, 2, 3,])];
			_ = sut[new ReadOnlySpan<int>([1, 2,])];

			await That(sut.Mock.Verify[It.IsAnyReadOnlySpan<int>()].Got()).Twice();
		}

		[Test]
		public async Task Span_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new Span<int>([1, 2, 3,])];

			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 2)].Got()).Never();
			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 3)].Got()).Once();
			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 4)].Got()).Never();
		}

		[Test]
		public async Task Span_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			_ = sut[new Span<int>()];
			_ = sut[new ReadOnlySpan<int>([1, 2, 3,])];
			_ = sut[new Span<int>([1, 2,])];

			await That(sut.Mock.Verify[It.IsAnySpan<int>()].Got()).Twice();
		}

		internal class SpanMock
		{
			public virtual int this[Memory<int> v]
				=> v.Length;

			public virtual int this[ReadOnlySpan<int> v]
				=> v.Length;

			public virtual int this[Span<int> v]
				=> v.Length;
		}
	}
}
#endif
