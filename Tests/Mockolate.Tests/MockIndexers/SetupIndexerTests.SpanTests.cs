#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class SpanTests
	{
		[Test]
		public async Task Memory_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.Satisfies<Memory<int>>(v => v.Length == 2)].Returns(4);

			int result = sut[new Memory<int>([1, 2, 3,])];

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task Memory_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.Satisfies<Memory<int>>(v => v.Length == 3)].Returns(42);

			int result = sut[new Memory<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Memory_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsAny<Memory<int>>()].Returns(42);

			int result = sut[new Memory<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReadOnlySpan_IndexerReturnValueSetup_ShouldReturnExpectedSpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			char[] expectedData =
			[
				'x', 'y', 'z',
			];
			sut.Mock.Setup[It.IsAny<string>()].Returns(new ReadOnlySpan<char>(expectedData));

			ReadOnlySpan<char> result = sut["test"];

			await That(result).IsEqualTo(expectedData);
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsReadOnlySpan<int>(v => v.Length == 2)].Returns(4);

			int result = sut[new ReadOnlySpan<int>([1, 2, 3,])];

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsReadOnlySpan<int>(v => v is [1, _, _,])].Returns(42);

			int result = sut[new ReadOnlySpan<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReadOnlySpan_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsAnyReadOnlySpan<int>()].Returns(42);

			int result = sut[new ReadOnlySpan<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Span_IndexerReturnValueSetup_ShouldReturnExpectedSpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			int[] expectedData =
			[
				1, 2, 3,
			];
			sut.Mock.Setup[It.IsAny<int>()].Returns(new Span<int>(expectedData));

			Span<int> result = sut[5];

			await That(result).IsEqualTo(expectedData);
		}

		[Test]
		public async Task Span_IndexerWithSpecificKey_ShouldMatchAndReturn()
		{
			SpanMock sut = SpanMock.CreateMock();
			int[] key1Data =
			[
				10, 20,
			];
			int[] key2Data =
			[
				30, 40, 50,
			];

			sut.Mock.Setup[It.Satisfies<int>(k => k == 1)].Returns(new Span<int>(key1Data));
			sut.Mock.Setup[It.Satisfies<int>(k => k == 2)].Returns(new Span<int>(key2Data));

			int[] result1 = sut[1].ToArray();
			int[] result2 = sut[2].ToArray();

			await That(result1).IsEqualTo(key1Data);
			await That(result2).IsEqualTo(key2Data);
		}

		[Test]
		public async Task Span_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsSpan<int>(v => v.Length == 2)].Returns(4);

			int result = sut[new Span<int>([1, 2, 3,])];

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task Span_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsSpan<int>(v => v is [1, _, _,])].Returns(42);

			int result = sut[new Span<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Span_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup[It.IsAnySpan<int>()].Returns(42);

			int result = sut[new Span<int>([1, 2, 3,])];

			await That(result).IsEqualTo(42);
		}

		internal abstract class SpanMock
		{
			public virtual int this[Memory<int> v]
				=> v.Length;

			public virtual int this[ReadOnlySpan<int> v]
				=> v.Length;

			public virtual int this[Span<int> v]
				=> v.Length;

			public abstract Span<int> this[int key] { get; }

			public abstract ReadOnlySpan<char> this[string key] { get; }
		}
	}
}
#endif
