using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public sealed partial class VerificationResultTests
{
	[Test]
	public async Task AnyParameters_OnOverloadedGenericMethod_LegacyPath_ShouldOnlyCountTheTargetedOverload()
	{
		IOverloadedMethodService sut = IOverloadedMethodService.CreateMock();

		sut.GetInstance<int>();
		sut.GetInstance<int>();
		sut.GetInstance<int>("key");

		await That(sut.Mock.Verify.GetInstance<int>().AnyParameters()).Exactly(2);
		await That(sut.Mock.Verify.GetInstance<int>("ignored").AnyParameters()).Once();
	}

	[Test]
	public async Task AnyParameters_OnOverloadedMethod_FastPath_ShouldOnlyCountTheTargetedOverload()
	{
		IOverloadedMethodService sut = IOverloadedMethodService.CreateMock();

		sut.DoSomething(1);
		sut.DoSomething(2);
		sut.DoSomething(3, true);

		await That(sut.Mock.Verify.DoSomething(0).AnyParameters()).Exactly(2);
		await That(sut.Mock.Verify.DoSomething(0, false).AnyParameters()).Once();
	}

	[Test]
	public async Task AnyParameters_OnUntypedDefaultArguments_ShouldBindToValuesOverload()
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
	public async Task CustomVerificationResult_ShouldKeepExpectation()
	{
		IMockInteractions interactions = new FastMockInteractions(0);
		VerificationResult<int> sut = new(1, interactions, _ => false, "foo");

		string result = ((IVerificationResult)sut).Expectation;

		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task CustomVerificationResult_WithFuncExpectation_ShouldKeepExpectation()
	{
		IMockInteractions interactions = new FastMockInteractions(0);
		VerificationResult<int> sut = new(1, interactions, _ => false, () => "foo");

		string result = ((IVerificationResult)sut).Expectation;

		await That(result).IsEqualTo("foo");
	}

	[Test]
	public async Task CustomVerificationResult_WithIMockInteractions_ShouldExposeInteractions()
	{
		IMockInteractions interactions = new FastMockInteractions(0);
		VerificationResult<int> sut = new(1, interactions, _ => false, "foo");

		IMockInteractions exposed = ((IVerificationResult)sut).Interactions;

		await That(exposed).IsSameAs(interactions);
	}

	[Test]
	public async Task VerificationResult_Got_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.TotalDispensed.Got();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got property TotalDispensed");
	}

	[Test]
	public async Task VerificationResult_GotIndexer_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify[It.IsAny<string>()].Got();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [It.IsAny<string>()]");
	}

	[Test]
	public async Task VerificationResult_GotIndexerWithMultipleParameters_ShouldHaveExpectedValue()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.IsAny<string>(), null].Got();

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("got indexer [It.IsAny<string>(), null]");
	}

	[Test]
	public async Task VerificationResult_Invoked_AnyParameters_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.Dispense(Match.AnyParameters());

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("invoked method Dispense(Match.AnyParameters())");
	}

	[Test]
	public async Task VerificationResult_Invoked_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>());

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("invoked method Dispense(It.IsAny<string>(), It.IsAny<int>())");
	}

	[Test]
	public async Task VerificationResult_MockInteractions_HasAllInteractions()
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
	public async Task VerificationResult_Set_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.TotalDispensed.Set(It.Is(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set property TotalDispensed to 5");
	}

	[Test]
	public async Task VerificationResult_SetIndexer_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify[It.IsAny<string>()].Set(It.Is(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [It.IsAny<string>()] to 5");
	}

	[Test]
	public async Task VerificationResult_SetIndexerWithMultipleParameters_ShouldHaveExpectedValue()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.Is("foo"), null].Set(It.Is<int?>(5));

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [\"foo\", null] to 5");
	}

	[Test]
	public async Task VerificationResult_SetIndexerWithMultipleParametersToNull_ShouldHaveExpectedValue()
	{
		IIndexerVerificationService sut = IIndexerVerificationService.CreateMock();

		VerificationResult<Mock.IMockVerifyForVerificationResultTests_IIndexerVerificationService> result = sut.Mock.Verify[It.Is("foo"), null].Set(null);

		await That(((IVerificationResult)result).Expectation)
			.IsEqualTo("set indexer [\"foo\", null] to null");
	}

	[Test]
	public async Task VerificationResult_SubscribedTo_ShouldHaveExpectedValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		VerificationResult<Mock.IMockVerifyForIChocolateDispenser> result
			= sut.Mock.Verify.ChocolateDispensed.Subscribed();

		await That(((IVerificationResult)result).Expectation).IsEqualTo("subscribed to event ChocolateDispensed");
	}

	[Test]
	public async Task VerificationResult_UnsubscribedFrom_ShouldHaveExpectedValue()
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
