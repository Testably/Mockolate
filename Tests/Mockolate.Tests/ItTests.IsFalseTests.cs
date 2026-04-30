using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsFalseTests
	{
		[Test]
		public async Task CachedMatcher_CallbackFromPriorDo_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsFalse().Do(_ => throw new InvalidOperationException("callback leaked from prior use"));

			IParameter<bool> subsequent = It.IsFalse();

			await That(() => ((IParameterMatch<bool>)subsequent).InvokeCallbacks(false)).DoesNotThrow();
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task False_ShouldMatchWhenFalse(bool value, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();
			sut.Mock.Setup.DoSomething(null, It.IsFalse());

			sut.DoSomething(null, value);

			await That(sut.Mock.Verify.DoSomething(null, It.IsFalse())).Exactly(expectedCount);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<bool> sut = It.IsFalse();
			string expectedValue = "It.IsFalse()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}
	}
}
