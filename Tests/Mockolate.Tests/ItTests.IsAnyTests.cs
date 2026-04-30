using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsAnyTests
	{
		[Test]
		public async Task CachedMatcher_CallbackFromPriorDo_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsAny<int>().Do(_ => throw new InvalidOperationException("callback leaked from prior use"));

			IParameter<int> subsequent = It.IsAny<int>();

			await That(() => ((IParameterMatch<int>)subsequent).InvokeCallbacks(42)).DoesNotThrow();
		}

		[Test]
		public async Task CachedMatcher_MonitorFromPriorUsage_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsAny<int>().Monitor(out IParameterMonitor<int> leakedMonitor);

			IParameter<int> subsequent = It.IsAny<int>();
			((IParameterMatch<int>)subsequent).InvokeCallbacks(42);

			await That(leakedMonitor.Values).IsEmpty();
		}

		[Test]
		[Arguments(null)]
		[Arguments("")]
		[Arguments("foo")]
		public async Task ShouldAlwaysMatch(string? value)
		{
			IParameter<string?> sut = It.IsAny<string?>();

			bool result = ((IParameterMatch<string?>)sut).Matches(value);

			await That(result).IsTrue();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsAny<string>();
			string expectedValue = "It.IsAny<string>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
