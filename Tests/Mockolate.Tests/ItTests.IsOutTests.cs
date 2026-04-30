using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsOutTests
	{
		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IOutParameter<int> sut = It.IsOut(() => 3);
			string expectedValue = "It.IsOut<int>(() => 3)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_Verify_ShouldReturnExpectedValue()
		{
			IVerifyOutParameter<int> sut = It.IsOut<int>();
			string expectedValue = "It.IsOut<int>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

#if NET9_0_OR_GREATER
		[Test]
		public async Task ToString_WithRefStructFactory_ShouldReturnExpectedValue()
		{
			IOutRefStructParameter<Span<int>> sut = It.IsOut<Span<int>>(() => default);
			string expectedValue = "It.IsOut<Span<int>>(() => default)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
#endif

		[Test]
		[Arguments(42)]
		[Arguments(-2)]
		public async Task WithOut_ShouldReturnValue(int? value)
		{
			IOutParameter<int?> sut = It.IsOut(() => value);

			bool found = sut.TryGetValue(out int? result);

			await That(found).IsTrue();
			await That(result).IsEqualTo(value);
		}

		[Test]
		public async Task WithOut_Verify_ShouldAlwaysMatch()
		{
			IVerifyOutParameter<int?> sut = It.IsOut<int?>();

			bool result = ((IParameterMatch<int?>)sut).Matches(42);

			await That(result).IsTrue();
			await That(() => ((IParameterMatch<int?>)sut).InvokeCallbacks(0)).DoesNotThrow();
		}
	}
}
