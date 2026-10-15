using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsOneOfTests
	{
		[Test]
		[Arguments(1, false)]
		[Arguments(4, false)]
		[Arguments(5, true)]
		[Arguments(6, true)]
		[Arguments(7, true)]
		[Arguments(8, false)]
		[Arguments(-5, false)]
		[Arguments(42, false)]
		public async Task ShouldMatchWhenEqualToAny(int value, bool expectMatch)
		{
			IParameter<int> sut = It.IsOneOf(5, 6, 7);

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task ShouldSupportCovarianceInSetup()
		{
			IMyService sut = IMyService.CreateMock();
			MyImplementation value1 = new();
			MyImplementation value2 = new();
			MyOtherImplementation other1 = new();
			sut.Mock.Setup.DoSomething(It.IsOneOf(value1, value2))
				.Returns(3);

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(other1);

			await That(result1).IsEqualTo(3);
			await That(result2).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldSupportCovarianceInVerify()
		{
			IMyService sut = IMyService.CreateMock();
			sut.Mock.Setup.DoSomething(It.Satisfies<MyImplementation>(_ => true))
				.Do(d => d.DoWork())
				.Returns(3);
			MyImplementation value1 = new();
			MyImplementation value2 = new();
			MyOtherImplementation other1 = new();
			MyOtherImplementation other2 = new();

			int result1 = sut.DoSomething(value1);

			await That(sut.Mock.Verify.DoSomething(It.IsOneOf(value1, value2))).Once();
			await That(value1.Progress).IsEqualTo(1);
			await That(result1).IsEqualTo(3);
			await That(sut.Mock.Verify.DoSomething(It.IsOneOf(other1, other2))).Never();
		}

		[Test]
		public async Task ToString_Using_ShouldReturnExpectedValue()
		{
			IParameter<int> sut = It.IsOneOf(3, 5).Using(new AllEqualComparer());
			string expectedValue = "It.IsOneOf(3, 5).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithNullableIntValues_ShouldReturnExpectedValue()
		{
			IParameter<int?> sut = It.IsOneOf<int?>(3, null, 5);
			string expectedValue = "It.IsOneOf(3, null, 5)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithStringValues_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsOneOf("foo", "bar");
			string expectedValue = "It.IsOneOf(\"foo\", \"bar\")";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		[Arguments(1)]
		[Arguments(5)]
		[Arguments(-42)]
		public async Task WithComparer_ShouldUseComparer(int value)
		{
			IParameter<int> sut = It.IsOneOf(4, 5, 6).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsTrue();
		}

		public interface IMyBase
		{
			int DoWork();
		}

		public class MyImplementation : IMyBase
		{
			public int Progress { get; private set; }

			public int DoWork()
			{
				Progress++;
				return Progress;
			}
		}

		public class MyOtherImplementation : IMyBase
		{
			public string Output { get; private set; } = "";

			public int DoWork()
			{
				Output += "did something\n";
				return 1;
			}
		}

		public interface IMyService
		{
			int DoSomething(IMyBase value);
		}
	}
}
