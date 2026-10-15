using Mockolate.Parameters;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsNotOneOfTests
	{
		[Test]
		[Arguments(1, true)]
		[Arguments(4, true)]
		[Arguments(5, false)]
		[Arguments(6, false)]
		[Arguments(7, false)]
		[Arguments(8, true)]
		[Arguments(-5, true)]
		[Arguments(42, true)]
		public async Task ShouldMatchWhenNotEqualToAny(int value, bool expectMatch)
		{
			IParameter<int> sut = It.IsNotOneOf(5, 6, 7);

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task ShouldSupportCovarianceInSetup()
		{
			IMyServiceIsNotOneOf sut = IMyServiceIsNotOneOf.CreateMock();
			MyBaseIsNotOneOf value1 = new MyImplementationIsNotOneOf();
			MyBaseIsNotOneOf value2 = new MyImplementationIsNotOneOf();
			MyBaseIsNotOneOf other = new MyOtherImplementationIsNotOneOf();
			sut.Mock.Setup.DoSomething(It.IsNotOneOf(value1, value2))
				.Returns(3);

			int result1 = sut.DoSomething(other);
			int result2 = sut.DoSomething(value1);

			await That(result1).IsEqualTo(3);
			await That(result2).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldSupportCovarianceInVerify()
		{
			IMyServiceIsNotOneOf sut = IMyServiceIsNotOneOf.CreateMock();
			MyImplementationIsNotOneOf value1 = new();
			MyImplementationIsNotOneOf value2 = new();
			MyImplementationIsNotOneOf other = new();

			sut.DoSomething(other);

			await That(sut.Mock.Verify.DoSomething(It.IsNotOneOf(value1, value2))).Once()
				.Because("'other' is not in [value1, value2]");
			await That(sut.Mock.Verify.DoSomething(It.IsNotOneOf(other))).Never()
				.Because("'other' IS in [other]");
		}

		[Test]
		public async Task ToString_Using_ShouldReturnExpectedValue()
		{
			IParameter<int> sut = It.IsNotOneOf(3, 5).Using(new AllEqualComparer());
			string expectedValue = "It.IsNotOneOf(3, 5).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithNullableIntValues_ShouldReturnExpectedValue()
		{
			IParameter<int?> sut = It.IsNotOneOf<int?>(3, null, 5);
			string expectedValue = "It.IsNotOneOf(3, null, 5)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithStringValues_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsNotOneOf("foo", "bar");
			string expectedValue = "It.IsNotOneOf(\"foo\", \"bar\")";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task WhenTypeDoesNotMatch_ShouldReturnTrue()
		{
			MyFlavor flavor = MyFlavor.Dark;
			IParameter<int> sut = It.IsNotOneOf(4, 5, 6);

			bool result = sut.Matches(flavor);

			await That(result).IsTrue();
		}

		[Test]
		[Arguments(1)]
		[Arguments(5)]
		[Arguments(-42)]
		public async Task WithComparer_ShouldUseComparer(int value)
		{
			IParameter<int> sut = It.IsNotOneOf(4, 5, 6).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsFalse();
		}

		public abstract class MyBaseIsNotOneOf;

		public class MyImplementationIsNotOneOf : MyBaseIsNotOneOf;

		public class MyOtherImplementationIsNotOneOf : MyBaseIsNotOneOf;

		public interface IMyServiceIsNotOneOf
		{
			int DoSomething(MyBaseIsNotOneOf value);
		}
	}
}
