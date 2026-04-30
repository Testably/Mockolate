#if NET8_0_OR_GREATER
using System;
using Mockolate.Setup;

namespace Mockolate.Tests.Setup;

public class SpanWrapperTests
{
	[Test]
	public async Task Equals_WithDifferentLength_ShouldReturnFalse()
	{
		SpanWrapper<int> sut = new([1, 2,]);

		await That(sut.Equals(new SpanWrapper<int>([1,]))).IsFalse()
			.Because("a shorter span is not the same span");
	}

	[Test]
	public async Task Equals_WithDifferentValues_ShouldReturnFalse()
	{
		SpanWrapper<int> sut = new([1, 2,]);

		await That(sut.Equals(new SpanWrapper<int>([1, 3,]))).IsFalse();
	}

	[Test]
	public async Task Equals_WithEqualValues_ShouldReturnTrue()
	{
		SpanWrapper<int> sut = new([1, 2,]);

		await That(sut.Equals(new SpanWrapper<int>([1, 2,]))).IsTrue()
			.Because("equality is content-based, so a re-wrapped span with the same values matches");
	}

	[Test]
	public async Task Equals_WithNull_ShouldReturnFalse()
	{
		SpanWrapper<int> sut = new([1,]);

		await That(sut.Equals(null)).IsFalse();
	}

	[Test]
	public async Task Equals_WithReadOnlySpanWrapper_ShouldReturnFalse()
	{
		SpanWrapper<int> sut = new([1,]);

		await That(sut.Equals(new ReadOnlySpanWrapper<int>([1,]))).IsFalse()
			.Because("the two wrappers represent different parameter shapes");
	}

	[Test]
	public async Task GetHashCode_WithEqualValues_ShouldMatch()
	{
		SpanWrapper<int> sut = new([1, 2,]);

		await That(sut.GetHashCode()).IsEqualTo(new SpanWrapper<int>([1, 2,]).GetHashCode());
	}

	[Test]
	public async Task GetHashCode_WithNullElement_ShouldNotThrow()
	{
		SpanWrapper<string?> sut = new([null, "a",]);

		await That(sut.GetHashCode).DoesNotThrow()
			.Because("a reference-typed span may carry null elements");
	}

	[Test]
	public async Task ToString_ShouldRenderElementTypeAndValues()
	{
		SpanWrapper<int> sut = new([1, 2,]);

		await That(sut.ToString()).IsEqualTo("Span<int>[1, 2]")
			.Because("recorded interactions render the wrapper, not the underlying span");
	}
}
#endif
