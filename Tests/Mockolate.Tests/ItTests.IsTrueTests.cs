using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsTrueTests
	{
		[Test]
		public async Task CachedMatcher_CallbackFromPriorDo_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsTrue().Do(_ => throw new InvalidOperationException("callback leaked from prior use"));

			IParameter<bool> subsequent = It.IsTrue();

			await That(() => ((IParameterMatch<bool>)subsequent).InvokeCallbacks(true)).DoesNotThrow();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<bool> sut = It.IsTrue();
			string expectedValue = "It.IsTrue()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		[Arguments(true, 1)]
		[Arguments(false, 0)]
		public async Task True_ShouldMatchWhenTrue(bool value, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();
			sut.Mock.Setup.DoSomething(null, It.IsTrue());

			sut.DoSomething(null, value);

			await That(sut.Mock.Verify.DoSomething(null, It.IsTrue())).Exactly(expectedCount);
		}
	}
}
