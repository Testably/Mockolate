using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests.Verify;

public class MockVerifyTests
{
	[Test]
	public async Task ThatAllInteractionsAreVerified_MultipleCalls_ShouldRepresentLatestValue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		bool result1 = sut.Mock.VerifyThatAllInteractionsAreVerified();
		sut.Dispense("Dark", 1);
		bool result2 = sut.Mock.VerifyThatAllInteractionsAreVerified();

		await That(result1).IsTrue();
		await That(result2).IsFalse();
	}

	[Test]
	public async Task ThatAllInteractionsAreVerified_MultipleVerifications_ShouldBeCombined()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 2);

		sut.Mock.Verify.Dispense(It.IsAny<string>(), It.Is(1)).Once();
		sut.Mock.Verify.Dispense(It.IsAny<string>(), It.Is(2)).Once();

		await That(sut.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue();
	}

	[Test]
	public async Task ThatAllInteractionsAreVerified_WithoutInteractions_ShouldReturnTrue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		await That(sut.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue();
	}

	[Test]
	public async Task ThatAllInteractionsAreVerified_WithUnverifiedInteractions_ShouldReturnFalse()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 2);

		await That(sut.Mock.Verify.Dispense(It.Is("Dark"), It.Is(1))).Once();
		await That(sut.Mock.VerifyThatAllInteractionsAreVerified()).IsFalse();
	}

	[Test]
	public async Task ThatAllInteractionsAreVerified_WithVerifiedInteractions_ShouldReturnTrue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 2);

		await That(sut.Mock.Verify.Dispense(It.IsAny<string>(), It.IsAny<int>())).AtLeastOnce();
		await That(sut.Mock.VerifyThatAllInteractionsAreVerified()).IsTrue();
	}

	[Test]
	[Arguments(0, false)]
	[Arguments(1, true)]
	[Arguments(5, true)]
	public async Task ThatAllSetupsAreUsed_ShouldCheckIndexersWithGetter(int interactionCount, bool expectedResult)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup[It.IsAny<string>()].InitializeWith(1);

		for (int i = 0; i < interactionCount; i++)
		{
			_ = sut["Dark"];
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsEqualTo(expectedResult);
	}

	[Test]
	[Arguments(0, false)]
	[Arguments(1, true)]
	[Arguments(5, true)]
	public async Task ThatAllSetupsAreUsed_ShouldCheckIndexersWithSetter(int interactionCount, bool expectedResult)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup[It.IsAny<string>()].InitializeWith(1);

		for (int i = 0; i < interactionCount; i++)
		{
			sut["Dark"] = i;
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsEqualTo(expectedResult);
	}

	[Test]
	[Arguments(0, false)]
	[Arguments(1, true)]
	[Arguments(5, true)]
	public async Task ThatAllSetupsAreUsed_ShouldCheckMethods(int interactionCount, bool expectedResult)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>());

		for (int i = 0; i < interactionCount; i++)
		{
			sut.Dispense("Dark", i);
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsEqualTo(expectedResult);
	}

	[Test]
	[Arguments(0, false)]
	[Arguments(1, true)]
	[Arguments(5, true)]
	public async Task ThatAllSetupsAreUsed_ShouldCheckPropertiesWithGetter(int interactionCount, bool expectedResult)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.TotalDispensed.InitializeWith(1);

		for (int i = 0; i < interactionCount; i++)
		{
			_ = sut.TotalDispensed;
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsEqualTo(expectedResult);
	}

	[Test]
	[Arguments(0, false)]
	[Arguments(1, true)]
	[Arguments(5, true)]
	public async Task ThatAllSetupsAreUsed_ShouldCheckPropertiesWithSetter(int interactionCount, bool expectedResult)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.TotalDispensed.InitializeWith(1);

		for (int i = 0; i < interactionCount; i++)
		{
			sut.TotalDispensed = i;
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsEqualTo(expectedResult);
	}

	[Test]
	public async Task ThatAllSetupsAreUsed_WithMultipleUsedSetups_ShouldReturnTrue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(1));
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(2));

		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 2);
		sut.Dispense("Dark", 3);

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsTrue();
	}

	[Test]
	public async Task ThatAllSetupsAreUsed_WithoutSetup_ShouldReturnTrue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		sut.TotalDispensed = 0;

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsTrue();
	}

	[Test]
	[Arguments(0)]
	[Arguments(1)]
	[Arguments(5)]
	public async Task ThatAllSetupsAreUsed_WithoutSetups_ShouldReturnTrue(int interactionCount)
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		for (int i = 0; i < interactionCount; i++)
		{
			sut.Dispense("Dark", i);
		}

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsTrue();
	}

	[Test]
	public async Task ThatAllSetupsAreUsed_WithPartlyUsedSetups_ShouldReturnFalse()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(1));
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(2));
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(3));
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.Is(4));

		sut.Dispense("Dark", 1);
		sut.Dispense("Dark", 3);

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsFalse();
	}

	[Test]
	public async Task ThatAllSetupsAreUsed_WithUnusedSetup_ShouldReturnFalse()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>());

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsFalse();
	}

	[Test]
	public async Task ThatAllSetupsAreUsed_WithUsedSetup_ShouldReturnTrue()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();
		sut.Mock.Setup.Dispense(It.IsAny<string>(), It.IsAny<int>());

		sut.Dispense("Dark", 1);

		await That(sut.Mock.VerifyThatAllSetupsAreUsed()).IsTrue();
	}

	[Test]
	public async Task VerificationResult_ShouldUpdateWhenInteractionsChange()
	{
		IChocolateDispenser sut = IChocolateDispenser.CreateMock();

		IVerificationResult result = sut.Mock.Verify.Dispense(It.Is("Dark"), It.IsAny<int>());
		bool r0 = result.Verify(f => f.Length == 0);
		sut.Dispense("Dark", 1);
		bool r1 = result.Verify(f => f.Length == 1);
		sut.Dispense("White", 2);
		bool r2 = result.Verify(f => f.Length == 1);
		sut.Dispense("Dark", 2);
		bool r3 = result.Verify(f => f.Length == 2);

		await That(r0).IsTrue().Because("No interaction was performed yet");
		await That(r1).IsTrue().Because("One interaction was performed");
		await That(r2).IsTrue().Because("The second interaction did not match");
		await That(r3).IsTrue().Because("The third interaction did again match");
	}
}
