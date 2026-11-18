using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Verify;

namespace Mockolate.Internal.Tests.Interactions;

public sealed class UncalledMemberVerificationTests
{
	[Test]
	public async Task Verify_AfterClear_ShouldNotCreateInteractionsAgain()
	{
		IUncalledService sut = IUncalledService.CreateMock();
		sut.Generic("boxed");
		sut.Mock.ClearAllInteractions();
		sut.Do(1);

		sut.Mock.Verify.Other(It.IsAny<int>()).Never();

		await That(CountCreatedInteractions(sut)).IsEqualTo(0)
			.Because("clearing the interactions also removes the boxed records that force the slow path");
	}

	[Test]
	public async Task Verify_OfAnUncalledMember_ShouldFailWithTheUsualMessage()
	{
		IUncalledService sut = IUncalledService.CreateMock();
		sut.Do(1);

		void Act()
		{
			sut.Mock.Verify.Other(It.IsAny<int>()).Once();
		}

		await That(Act).Throws<MockVerificationException>()
			.WithMessage("Expected that mock invoked method Other(It.IsAny<int>()) exactly once, but it never did.");
	}

	[Test]
	public async Task Verify_OfAnUncalledMember_ShouldNotCreateInteractionsForTheOtherCalls()
	{
		IUncalledService sut = IUncalledService.CreateMock();
		sut.Do(1);
		sut.Do(2);

		sut.Mock.Verify.Other(It.IsAny<int>()).Never();
		sut.Mock.Verify.Other(3).Never();
		sut.Mock.Verify.Level.Got().Never();

		await That(CountCreatedInteractions(sut)).IsEqualTo(0)
			.Because("a member without calls is answered from its empty typed view, without looking at the calls of other members");
	}

	[Test]
	public async Task Verify_OnAMonitor_ShouldOnlyCountTheCallsOfItsSession()
	{
		IUncalledService sut = IUncalledService.CreateMock();
		MockMonitor<Mock.IMockVerifyForUncalledMemberVerificationTests_IUncalledService> monitor = sut.Mock.Monitor();

		sut.Other(1);
		using (monitor.Run())
		{
			sut.Do(2);
		}

		monitor.Verify.Other(It.IsAny<int>()).Never();
		monitor.Verify.Do(It.IsAny<int>()).Once();
		sut.Mock.Verify.Other(It.IsAny<int>()).Once();
		await That(((IMock)sut).MockRegistry.Interactions.Count).IsEqualTo(2);
	}

	[Test]
	public async Task Verify_WhenBoxedRecordsExist_ShouldStillCountCorrectly()
	{
		IUncalledService sut = IUncalledService.CreateMock();
		sut.Generic("boxed");
		sut.Do(1);

		sut.Mock.Verify.Other(It.IsAny<int>()).Never();
		sut.Mock.Verify.Do(It.IsAny<int>()).Once();
		sut.Mock.Verify.Generic<string>(It.IsAny<string>()).Once();

		await That(((IMock)sut).MockRegistry.Interactions.GetUnverifiedInteractions()).IsEmpty();
	}

	private static int CountCreatedInteractions(IUncalledService sut)
	{
		FastMockInteractions interactions = (FastMockInteractions)((IMock)sut).MockRegistry.Interactions;
		int created = 0;
		for (RecordNode? node = interactions.First; node is not null; node = node.Next)
		{
			if (node.Boxed is not null)
			{
				created++;
			}
		}

		return created;
	}

	public interface IUncalledService
	{
		int Level { get; }
		void Do(int value);
		void Other(int value);
		void Generic<T>(T value);
	}
}
