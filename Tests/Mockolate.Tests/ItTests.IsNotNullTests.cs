using Mockolate.Parameters;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsNotNullTests
	{
		[Test]
		public async Task CachedMatcher_CallbackFromPriorDo_ShouldNotLeakIntoSubsequentUsage()
		{
			_ = It.IsNotNull<string>().Do(_ => throw new InvalidOperationException("callback leaked from prior use"));

			IParameter<string> subsequent = It.IsNotNull<string>();

			await That(() => ((IParameterMatch<string>)subsequent).InvokeCallbacks("value")).DoesNotThrow();
		}

		[Test]
		[Arguments(null, 0)]
		[Arguments(1, 1)]
		public async Task NotNull_ShouldMatchWhenNotNull(int? value, int expectedCount)
		{
			IMyServiceWithNullable sut = IMyServiceWithNullable.CreateMock();
			sut.Mock.Setup.DoSomething(It.IsNotNull<int?>(), It.Is(true));

			sut.DoSomething(value, true);

			await That(sut.Mock.Verify.DoSomething(It.IsNotNull<int?>(), It.Is(true))).Exactly(expectedCount);
		}

		[Test]
		public async Task ShouldReturnSharedInstance_WhenToStringIsOmitted()
		{
			IParameter<string> first = It.IsNotNull<string>();
			IParameter<string> second = It.IsNotNull<string>();

			await That(first).IsSameAs(second);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsNotNull<string>();
			string expectedValue = "It.IsNotNull<string>()";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithCustomValue_ShouldReturnCustomValue()
		{
			IParameter<string> sut = It.IsNotNull<string>("custom");
			string expectedValue = "custom";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task WhenTypeDoesNotMatch_ShouldReturnTrue()
		{
			MyFlavor flavor = MyFlavor.Dark;
			IParameter<string?> sut = It.IsNotNull<string?>();

			bool result = sut.Matches(flavor);

			await That(result).IsTrue();
		}

		[Test]
		[Arguments(null, false)]
		[Arguments("", true)]
		[Arguments("foo", true)]
		public async Task WithValue_Nullable_ShouldMatchWhenNotNull(string? value, bool expectMatch)
		{
			IParameter<string?> sut = It.IsNotNull<string?>();

			bool result = ((IParameterMatch<string?>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}
	}
}
