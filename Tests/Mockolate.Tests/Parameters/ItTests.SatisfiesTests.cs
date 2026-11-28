using Mockolate.Parameters;
using Mockolate.TestHelpers;

namespace Mockolate.Tests.Parameters;

public sealed partial class ItTests
{
	public sealed class SatisfiesTests
	{
		[Test]
		[Arguments(null, true)]
		[Arguments(1, false)]
		public async Task CheckForNull_ShouldMatchForExpectedResult(int? value, bool expectedResult)
		{
			IParameter<int?> sut = It.Satisfies<int?>(v => v is null);

			bool result = ((IParameterMatch<int?>)sut).Matches(value);

			await That(result).IsEqualTo(expectedResult);
		}

		[Test]
		[Arguments(true)]
		[Arguments(false)]
		public async Task ShouldMatchForExpectedResult(bool predicateValue)
		{
			IParameter<string> sut = It.Satisfies<string>(_ => predicateValue);

			bool result = ((IParameterMatch<string>)sut).Matches("foo");

			await That(result).IsEqualTo(predicateValue);
		}

		[Test]
		public async Task WhenSetupDoesNotMatch_ShouldEvaluatePredicateOnce()
		{
			int evaluations = 0;
			IChocolateDispenser sut = IChocolateDispenser.CreateMock();
			sut.Mock.Setup.Dispense(It.Satisfies<string>(_ =>
				{
					evaluations++;
					return false;
				}), It.IsAny<int>())
				.Returns(true);

			bool result = sut.Dispense("Dark", 1);

			await That(result).IsFalse();
			await That(evaluations).IsEqualTo(1)
				.Because("the setups of the member are only searched once per call");
		}

		[Test]
		public async Task WhenSetupDoesNotMatchAndOverloadHasSetup_ShouldEvaluatePredicateOnce()
		{
			int evaluations = 0;
			IOverloadedByRef sut = IOverloadedByRef.CreateMock();
			sut.Mock.Setup.Get(It.Satisfies<int>(_ =>
				{
					evaluations++;
					return false;
				}))
				.Returns(1);
			sut.Mock.Setup.Get(It.IsRef<int>(_ => false)).Returns(2);

			int result = sut.Get(3);

			await That(result).IsEqualTo(0);
			await That(evaluations).IsEqualTo(1)
				.Because("the setups of the member are only searched once per call, even when an overload with the same name has setups");
		}

		[Test]
		public async Task ShouldSupportCovarianceInSetup()
		{
			IMyService sut = IMyService.CreateMock();
			sut.Mock.Setup.DoSomething(It.Satisfies<MyImplementation>(_ => true))
				.Returns(3);
			MyImplementation value1 = new();
			MyOtherImplementation value2 = new();

			int result1 = sut.DoSomething(value1);
			int result2 = sut.DoSomething(value2);

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

			int result1 = sut.DoSomething(value1);

			await That(sut.Mock.Verify.DoSomething(It.Satisfies<MyImplementation>(p => p.Progress > 0))).Once();
			await That(sut.Mock.Verify.DoSomething(It.Satisfies<MyImplementation>(p => p.Progress > 1))).Never();
			await That(value1.Progress).IsEqualTo(1);
			await That(result1).IsEqualTo(3);
			await That(sut.Mock.Verify.DoSomething(It.Satisfies<MyOtherImplementation>(_ => true))).Never();
		}

		[Test]
		public async Task ToString_ShouldReturnExpectedValue()
		{
			IParameter<string> sut = It.Satisfies<string>(x => x.Length == 3);
			string expectedValue = "It.Satisfies<string>(x => x.Length == 3)";

			string? result = sut.ToString();

			await That(result).IsEqualTo(expectedValue);
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

		public interface IOverloadedByRef
		{
			int Get(int value);
			int Get(ref int value);
		}
	}
}
