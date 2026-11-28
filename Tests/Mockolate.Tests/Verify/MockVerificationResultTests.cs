using Mockolate.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed partial class MockVerificationResultTests
{
	[Test]
	public async Task AnyParameters_WithOverloadedGenericMethod_ShouldOnlyCountTheTargetedOverload()
	{
		IOverloadedMethodService sut = IOverloadedMethodService.CreateMock();

		sut.GetInstance<int>();
		sut.GetInstance<int>();
		sut.GetInstance<int>("key");

		await That(sut.Mock.Verify.GetInstance<int>().AnyParameters()).Exactly(2);
		await That(sut.Mock.Verify.GetInstance<int>("ignored").AnyParameters()).Once();
	}

	[Test]
	public async Task AnyParameters_WithOverloadedMethod_ShouldOnlyCountTheTargetedOverload()
	{
		IOverloadedMethodService sut = IOverloadedMethodService.CreateMock();

		sut.DoSomething(1);
		sut.DoSomething(2);
		sut.DoSomething(3, true);

		await That(sut.Mock.Verify.DoSomething(0).AnyParameters()).Exactly(2);
		await That(sut.Mock.Verify.DoSomething(0, false).AnyParameters()).Once();
	}

	[Test]
	public async Task AnyParameters_WithUntypedDefaultArguments_ShouldBindToValuesOverload()
	{
		IOverloadedMethodService sut = IOverloadedMethodService.CreateMock();

		sut.DoSomething(1);
		sut.DoSomething(2);
		sut.DoSomething(3, true);

		await That(sut.Mock.Verify.DoSomething(default).AnyParameters()).Exactly(2);
		await That(sut.Mock.Verify.DoSomething(default, default).AnyParameters()).Once();
		await That(sut.Mock.Verify.DoSomething(default!, default!).AnyParameters()).Once();
	}

	[Test]
	public async Task Expectation_WithPropertyGot_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.TotalDispensed.Got();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got property TotalDispensed");
	}

	[Test]
	public async Task Expectation_WithIndexerGot_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify[It.IsAny<string>()].Got();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [It.IsAny<string>()]");
	}

	[Test]
	public async Task Expectation_WithIndexerGotAndMultipleKeys_ShouldDescribeTheVerification()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForMockVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.IsAny<string>(), null].Got();

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("got indexer [It.IsAny<string>(), null]");
	}

	[Test]
	public async Task Expectation_WithInvokedAndAnyParameters_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.Dispense(Match.AnyParameters());

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("invoked method Dispense(Match.AnyParameters())");
	}

	[Test]
	public async Task Expectation_WithInvoked_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>());

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("invoked method Dispense(It.IsAny<string>(), It.IsAny<int>())");
	}

	[Test]
	public async Task Interactions_ShouldContainAllInteractionsOfTheMock()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Dispense("Dark", 2);
		_ = sut.TotalDispensed;
		sut["Dark"] = 5;
		sut.TotalDispensed = 10;
		_ = sut["Milk"];

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.TotalDispensed.Got();

		await That(((IVerificationResult)result).Interactions.Count).IsEqualTo(5);
	}

	[Test]
	public async Task Expectation_WithPropertySet_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.TotalDispensed.Set(It.Is(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set property TotalDispensed to 5");
	}

	[Test]
	public async Task Expectation_WithIndexerSet_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify[It.IsAny<string>()].Set(It.Is(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [It.IsAny<string>()] to 5");
	}

	[Test]
	public async Task Expectation_WithIndexerSetAndMultipleKeys_ShouldDescribeTheVerification()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForMockVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.Is("foo"), null].Set(It.Is<int?>(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [\"foo\", null] to 5");
	}

	[Test]
	public async Task Expectation_WithIndexerSetToNullAndMultipleKeys_ShouldDescribeTheVerification()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForMockVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.Is("foo"), null].Set(null);

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [\"foo\", null] to null");
	}

	[Test]
	public async Task Expectation_WithSubscribed_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.ChocolateDispensed.Subscribed();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event ChocolateDispensed");
	}

	[Test]
	public async Task Expectation_WithUnsubscribed_ShouldDescribeTheVerification()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.ChocolateDispensed.Unsubscribed();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("unsubscribed from event ChocolateDispensed");
	}

	internal interface IIndexerVerificationService
	{
		int? this[string p1, int? p2] { get; set; }
	}

	internal interface IOverloadedMethodService
	{
		void DoSomething(int value);
		void DoSomething(int value, bool flag);

		TService GetInstance<TService>();
		TService GetInstance<TService>(string key);
	}
}
