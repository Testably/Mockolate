using Mockolate.Interactions;
using Mockolate.Monitor;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Interactions;

public sealed class CombinedMockRecordingTests
{
	[Test]
	public async Task Calls_ShouldBeRecordedTypedForTheMockedTypeAndTheAdditionalInterface()
	{
		ICombinedBase sut = ICombinedBase.CreateMock().Implementing<ICombinedAdditional>();

		sut.Do(1);
		((ICombinedAdditional)sut).Run(2);
		_ = ((ICombinedAdditional)sut).Level;

		FastMockInteractions interactions = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int boxedRecords = 0;
		for (RecordNode? node = interactions.First; node is not null; node = node.Next)
		{
			if (node is InteractionRecord)
			{
				boxedRecords++;
			}
		}

		await That(interactions.Count).IsEqualTo(3);
		await That(boxedRecords).IsEqualTo(0)
			.Because("the member ids of the additional interface continue the ids of the mocked type, so every call can be recorded typed");
	}

	[Test]
	public async Task Monitor_ShouldSeeTheCallsOfBothTypes()
	{
		ICombinedBase sut = ICombinedBase.CreateMock().Implementing<ICombinedAdditional>();
		MockMonitor<Mock.IMockVerifyForCombinedMockRecordingTests_ICombinedBase> monitor = sut.Mock.Monitor();

		sut.Do(1);
		using (monitor.Run())
		{
			sut.Do(2);
			((ICombinedAdditional)sut).Run(3);
		}

		monitor.Verify.Do(It.IsAny<int>()).Once();
		sut.Mock.Verify.Do(It.IsAny<int>()).Twice();
		await That(((IMock)sut).MockRegistry.Interactions.Count).IsEqualTo(3);
	}

	[Test]
	public async Task Verify_ShouldCountAndMarkTheCallsOfBothTypes()
	{
		ICombinedBase sut = ICombinedBase.CreateMock().Implementing<ICombinedAdditional>();

		sut.Do(1);
		((ICombinedAdditional)sut).Run(2);
		((ICombinedAdditional)sut).Run(3);

		sut.Mock.Verify.Do(1).Once();
		sut.Mock.As<ICombinedAdditional>().Verify.Run(It.IsAny<int>()).Twice();
		sut.Mock.As<ICombinedAdditional>().Verify.Level.Got().Never();

		IMockInteractions interactions = ((IMock)sut).MockRegistry.Interactions;
		await That(interactions.GetUnverifiedInteractions()).IsEmpty();

		sut.Mock.ClearAllInteractions();
		await That(interactions.Count).IsEqualTo(0);
		sut.Mock.Verify.Do(It.IsAny<int>()).Never();
	}

	public interface ICombinedBase
	{
		void Do(int value);
	}

	public interface ICombinedAdditional
	{
		int Level { get; }
		void Run(int value);
	}
}
