#if NET8_0_OR_GREATER

namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	public sealed class SpanTests
	{
		[Test]
		public async Task Memory_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.Satisfies<Memory<int>>(v => v.Length == 2)).Returns(4);

			int result = sut.MyMethod(new Memory<int>([1, 2, 3,]));

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task Memory_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.Satisfies<Memory<int>>(v => v.Length == 3)).Returns(42);

			int result = sut.MyMethod(new Memory<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Memory_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsAny<Memory<int>>()).Returns(42);

			int result = sut.MyMethod(new Memory<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReadOnlySpan_ReturnValueSetup_ShouldReturnExpectedSpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			char[] expectedData = ['a', 'b', 'c',];
			sut.Mock.Setup.GetReadOnlySpan().Returns(new ReadOnlySpan<char>(expectedData));

			char[] result = sut.GetReadOnlySpan().ToArray();

			await That(result).IsEqualTo(expectedData);
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsReadOnlySpan<int>(v => v.Length == 2)).Returns(4);

			int result = sut.MyMethod(new ReadOnlySpan<int>([1, 2, 3,]));

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task ReadOnlySpan_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsReadOnlySpan<int>(v => v is [1, _, _,])).Returns(42);

			int result = sut.MyMethod(new ReadOnlySpan<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReadOnlySpan_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsAnyReadOnlySpan<int>()).Returns(42);

			int result = sut.MyMethod(new ReadOnlySpan<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReadOnlySpan_WithParameter_ShouldMatchAndReturn()
		{
			SpanMock sut = SpanMock.CreateMock();
			byte[] inputData = [10, 20, 30,];
			byte[] outputData = [100, 200,];

			sut.Mock.Setup.ProcessData(It.IsAnySpan<byte>())
				.Returns(new ReadOnlySpan<byte>(outputData));

			byte[] result = sut.ProcessData(new Span<byte>(inputData)).ToArray();

			await That(result).IsEqualTo(outputData);
		}

		[Test]
		public async Task Span_MultipleReturnValues_ShouldCycleThroughValues()
		{
			SpanMock sut = SpanMock.CreateMock();
			int[] firstData = [1, 2,];
			int[] secondData = [3, 4, 5,];
			sut.Mock.Setup.GetSpan()
				.Returns(new Span<int>(firstData))
				.Returns(new Span<int>(secondData));

			int[] firstResult = sut.GetSpan().ToArray();
			int[] secondResult = sut.GetSpan().ToArray();
			int[] thirdResult = sut.GetSpan().ToArray();

			await That(firstResult).IsEqualTo(firstData);
			await That(secondResult).IsEqualTo(secondData);
			await That(thirdResult).IsEqualTo(firstData);
		}

		[Test]
		public async Task Span_ReturnValueSetup_ShouldReturnExpectedSpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			int[] expectedData = [1, 2, 3,];
			sut.Mock.Setup.GetSpan().Returns(new Span<int>(expectedData));

			Span<int> result = sut.GetSpan();

			await That(result).IsEqualTo(expectedData);
		}

		[Test]
		public async Task Span_WhenPredicateDoesNotMatch_ShouldUseDefaultValue()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsSpan<int>(v => v.Length == 2)).Returns(4);

			int result = sut.MyMethod(new Span<int>([1, 2, 3,]));

			await That(result).IsEqualTo(3);
		}

		[Test]
		public async Task Span_WhenPredicateMatches_ShouldApplySetup()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsSpan<int>(v => v is [1, _, _,])).Returns(42);

			int result = sut.MyMethod(new Span<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Span_WithCallback_ShouldExecuteCallback()
		{
			SpanMock sut = SpanMock.CreateMock();
			bool callbackExecuted = false;
			int[] returnData = [42,];

			sut.Mock.Setup.GetSpan()
				.Do(() => callbackExecuted = true)
				.Returns(new Span<int>(returnData));

			int[] result = sut.GetSpan().ToArray();

			await That(callbackExecuted).IsTrue();
			await That(result).IsEqualTo(returnData);
		}

		[Test]
		public async Task Span_WithoutPredicate_ShouldMatchAnySpan()
		{
			SpanMock sut = SpanMock.CreateMock();
			sut.Mock.Setup.MyMethod(It.IsAnySpan<int>()).Returns(42);

			int result = sut.MyMethod(new Span<int>([1, 2, 3,]));

			await That(result).IsEqualTo(42);
		}

		internal abstract class SpanMock
		{
			public virtual int MyMethod(Memory<int> value)
				=> value.Length;

			public virtual int MyMethod(ReadOnlySpan<int> value)
				=> value.Length;

			public virtual int MyMethod(Span<int> value)
				=> value.Length;

			public abstract Span<int> GetSpan();
			public abstract ReadOnlySpan<char> GetReadOnlySpan();
			public abstract ReadOnlySpan<byte> ProcessData(Span<byte> input);
		}
	}
}
#endif
