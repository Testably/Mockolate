#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifySetIndexerTests
{
	public sealed class SpanTests
	{
		[Test]
		public async Task Memory_WhenPredicateMatches_ShouldApplySetup()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new Memory<int>([1, 2, 3,])] = 3;

			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 2)].Set(It.IsAny<int>())).Never();
			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 3)].Set(It.IsAny<int>())).Once();
			await That(sut.Mock.Verify[It.Satisfies<Memory<int>>(v => v.Length == 4)].Set(It.IsAny<int>())).Never();
		}

		[Test]
		public async Task Memory_WithoutPredicate_ShouldApplyAllCalls()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new Memory<int>()] = 3;
			sut[new Span<int>([1, 2, 3,])] = 3;
			sut[new Memory<int>([1, 2,])] = 3;

			await That(sut.Mock.Verify[It.IsAny<Memory<int>>()].Set(It.IsAny<int>())).Twice();
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateMatches_ShouldApplySetup()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new ReadOnlySpan<int>([1, 2, 3,])] = 3;

			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 2)].Set(It.IsAny<int>())).Never();
			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 3)].Set(It.IsAny<int>())).Once();
			await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(v => v.Length == 4)].Set(It.IsAny<int>())).Never();
		}

		[Test]
		public async Task ReadOnlySpan_WithoutPredicate_ShouldApplyAllCalls()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new ReadOnlySpan<int>()] = 3;
			sut[new Span<int>([1, 2, 3,])] = 3;
			sut[new ReadOnlySpan<int>([1, 2,])] = 3;

			await That(sut.Mock.Verify[It.IsAnyReadOnlySpan<int>()].Set(It.IsAny<int>())).Twice();
		}

		[Test]
		public async Task Span_WhenPredicateMatches_ShouldApplySetup()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new Span<int>([1, 2, 3,])] = 3;

			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 2)].Set(It.IsAny<int>())).Never();
			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 3)].Set(It.IsAny<int>())).Once();
			await That(sut.Mock.Verify[It.IsSpan<int>(v => v.Length == 4)].Set(It.IsAny<int>())).Never();
		}

		[Test]
		public async Task Span_WithoutPredicate_ShouldApplyAllCalls()
		{
			ISpanMock sut = ISpanMock.CreateMock();

			sut[new Span<int>()] = 3;
			sut[new ReadOnlySpan<int>([1, 2, 3,])] = 3;
			sut[new Span<int>([1, 2,])] = 3;

			await That(sut.Mock.Verify[It.IsAnySpan<int>()].Set(It.IsAny<int>())).Twice();
		}

		internal interface ISpanMock
		{
			int this[Memory<int> v] { get; set; }
			int this[ReadOnlySpan<int> v] { get; set; }
			int this[Span<int> v] { get; set; }
		}
	}
}
#endif
