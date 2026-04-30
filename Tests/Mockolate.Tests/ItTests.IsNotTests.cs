using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	public sealed class IsNotTests
	{
		[Test]
		public async Task ShouldCorrectlyHandleNull()
		{
			IMyService sut = IMyService.CreateMock();
			MyImplementation value1 = new();
			sut.Mock.Setup.DoSomething(It.IsNot<IMyBase>(null!))
				.Returns(3);

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(null!);

			await That(result1).IsEqualTo(3);
			await That(result2).IsEqualTo(0);
		}

		[Test]
		public async Task ShouldCorrectlyHandleNullWithCovariance()
		{
			IMyService sut = IMyService.CreateMock();
			MyOtherImplementation value1 = new();
			sut.Mock.Setup.DoSomething(It.IsNot<MyImplementation>(null!))
				.Returns(3);

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(null!);

			await That(result1).IsEqualTo(3);
			await That(result2).IsEqualTo(0);
		}

		[Test]
		[Arguments(1, true)]
		[Arguments(5, false)]
		[Arguments(-5, true)]
		[Arguments(42, true)]
		public async Task ShouldMatchWhenNotEqual(int value, bool expectMatch)
		{
			IParameter<int> sut = It.IsNot(5);

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsEqualTo(expectMatch);
		}

		[Test]
		public async Task ShouldSupportCovarianceInSetup()
		{
			IMyService sut = IMyService.CreateMock();
			MyImplementation value1 = new();
			MyOtherImplementation value2 = new();
			sut.Mock.Setup.DoSomething(It.IsNot(value1))
				.Returns(3);

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(value2);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(3);
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.IsNot("foo");
			string expectedValue = "It.IsNot(\"foo\")";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		public async Task ToString_WithComparer_ShouldReturnExpectedValue()
		{
			IParameter<int> sut = It.IsNot(4).Using(new AllEqualComparer());
			string expectedValue = "It.IsNot(4).Using(new AllEqualComparer())";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
		}

		[Test]
		[Arguments(1)]
		[Arguments(5)]
		[Arguments(-42)]
		public async Task WithComparer_ShouldUseComparer(int value)
		{
			IParameter<int> sut = It.IsNot(5).Using(new AllEqualComparer());

			bool result = ((IParameterMatch<int>)sut).Matches(value);

			await That(result).IsFalse();
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
