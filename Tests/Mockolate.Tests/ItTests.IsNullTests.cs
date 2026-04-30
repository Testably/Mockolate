using Mockolate.Parameters;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsNullTests
	{
		[Test]
		public async Task CachedMatcher_CallbackFromPriorDo_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsNull<string?>().Do(_ => throw new InvalidOperationException("callback leaked from prior use"));

			IParameter<string?> subsequent = It.IsNull<string?>();

			await That(() => ((IParameterMatch<string?>)subsequent).InvokeCallbacks(null)).DoesNotThrow();
		}

		[Test]
		[Arguments(null, 1)]
		[Arguments(1, 0)]
		public async Task Null_ShouldMatchWhenNull(int? value, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();
			sut.Mock.Setup.DoSomething(null, It.Is(true));

			sut.DoSomething(value, true);

			await That(sut.Mock.Verify.DoSomething(null, It.Is(true))).Exactly(expectedCount);
		}

		[Test]
		public async Task ShouldReturnSharedInstance_WhenToStringIsOmitted()
		{
			IParameter<string> first = It.IsNull<string>();
			IParameter<string> second = It.IsNull<string>();

			await That(first).IsSameAs(second);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsNull<string>();
			string expectedValue = "It.IsNull<string>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task WhenTypeDoesNotMatch_ShouldReturnFalse()
		{
			MyFlavor flavor = MyFlavor.Dark;
			IParameter<string?> sut = It.IsNull<string?>();

			bool result = sut.Matches(flavor);

			await That(result).IsFalse();
		}

		[Test]
		[Arguments(null, true)]
		[Arguments("", false)]
		[Arguments("foo", false)]
		public async Task WithValue_Nullable_ShouldMatchWhenEqual(string? value, bool expectMatch)
		{
			IParameter<string?> sut = It.IsNull<string?>();

			bool result = ((IParameterMatch<string?>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}
	}
}
