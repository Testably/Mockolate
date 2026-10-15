#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	public sealed class SpanTests
	{
		[Test]
		public async Task Memory_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new Memory<int>([1, 2, 3,]));

			await That(sut.Mock.Verify.MyMethod(It.Satisfies<Memory<int>>(v => v.Length == 2))).Never();
			await That(sut.Mock.Verify.MyMethod(It.Satisfies<Memory<int>>(v => v.Length == 3))).Once();
			await That(sut.Mock.Verify.MyMethod(It.Satisfies<Memory<int>>(v => v.Length == 4))).Never();
		}

		[Test]
		public async Task Memory_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new Memory<int>());
			sut.MyMethod(new Span<int>([1, 2, 3,]));
			sut.MyMethod(new Memory<int>([1, 2,]));

			await That(sut.Mock.Verify.MyMethod(It.IsAny<Memory<int>>())).Twice();
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new ReadOnlySpan<int>([1, 2, 3,]));

			await That(sut.Mock.Verify.MyMethod(It.IsReadOnlySpan<int>(v => v.Length == 2))).Never();
			await That(sut.Mock.Verify.MyMethod(It.IsReadOnlySpan<int>(v => v.Length == 3))).Once();
			await That(sut.Mock.Verify.MyMethod(It.IsReadOnlySpan<int>(v => v.Length == 4))).Never();
		}

		[Test]
		public async Task ReadOnlySpan_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new ReadOnlySpan<int>());
			sut.MyMethod(new Span<int>([1, 2, 3,]));
			sut.MyMethod(new ReadOnlySpan<int>([1, 2,]));

			await That(sut.Mock.Verify.MyMethod(It.IsAnyReadOnlySpan<int>())).Twice();
		}

		[Test]
		public async Task Span_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new Span<int>([1, 2, 3,]));

			await That(sut.Mock.Verify.MyMethod(It.IsSpan<int>(v => v.Length == 2))).Never();
			await That(sut.Mock.Verify.MyMethod(It.IsSpan<int>(v => v.Length == 3))).Once();
			await That(sut.Mock.Verify.MyMethod(It.IsSpan<int>(v => v.Length == 4))).Never();
		}

		[Test]
		public async Task Span_WithoutPredicate_ShouldApplyAllCalls()
		{
			SpanMock sut = SpanMock.CreateMock();

			sut.MyMethod(new Span<int>());
			sut.MyMethod(new ReadOnlySpan<int>([1, 2, 3,]));
			sut.MyMethod(new Span<int>([1, 2,]));

			await That(sut.Mock.Verify.MyMethod(It.IsAnySpan<int>())).Twice();
		}

		internal class SpanMock
		{
			public virtual int MyMethod(Memory<int> value)
				=> value.Length;

			public virtual int MyMethod(ReadOnlySpan<int> value)
				=> value.Length;

			public virtual int MyMethod(Span<int> value)
				=> value.Length;
		}
	}
}
#endif
