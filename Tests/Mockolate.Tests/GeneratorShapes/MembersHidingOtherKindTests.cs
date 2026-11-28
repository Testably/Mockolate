using System;
using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.GeneratorShapes;

public sealed class MembersHidingOtherKindTests
{
	[Test]
	public async Task Property_WithHiddenToStringMethod_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		sut.Mock.Setup.ToString.InitializeWith(42);
		sut.Mock.As<IMembersHiddenByOtherKind>().Setup.ToString().Returns("hidden");

		int property = sut.ToString;
		string method = ((IMembersHiddenByOtherKind)sut).ToString();

		await That(property).IsEqualTo(42);
		await That(method).IsEqualTo("hidden");
		await That(sut.Mock.Verify.ToString.Got()).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.ToString()).Once();
	}

	[Test]
	public async Task Event_WithHiddenMethod_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		List<string> received = [];
		sut.Mock.As<IMembersHiddenByOtherKind>().Setup.Start(It.IsAny<int>()).Do(value => received.Add($"method {value}"));
		sut.Start += (_, _) => received.Add("event");

		sut.Mock.Raise.Start(sut, EventArgs.Empty);
		((IMembersHiddenByOtherKind)sut).Start(3);

		await That(received).IsEqualTo(["event", "method 3",]);
		await That(sut.Mock.Verify.Start.Subscribed()).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.Start(3)).Once();
	}

	[Test]
	public async Task Method_WithHiddenProperty_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		sut.Mock.Setup.Size().Returns(3);
		sut.Mock.As<IMembersHiddenByOtherKind>().Setup.Size.InitializeWith(4);

		int method = sut.Size();
		int property = ((IMembersHiddenByOtherKind)sut).Size;

		await That(method).IsEqualTo(3);
		await That(property).IsEqualTo(4);
		await That(sut.Mock.Verify.Size()).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.Size.Got()).Once();
	}

	[Test]
	public async Task Property_WithHiddenEvent_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Stop.InitializeWith(5);
		((IMembersHiddenByOtherKind)sut).Stop += (_, _) => received.Add("event");

		sut.Mock.As<IMembersHiddenByOtherKind>().Raise.Stop(sut, EventArgs.Empty);
		sut.Stop = 6;

		await That(received).IsEqualTo(["event",]);
		await That(sut.Stop).IsEqualTo(6);
		await That(sut.Mock.Verify.Stop.Set(6)).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.Stop.Subscribed()).Once();
	}

	[Test]
	public async Task Event_WithHiddenProperty_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		List<string> received = [];
		sut.Mock.As<IMembersHiddenByOtherKind>().Setup.Level.InitializeWith(7);
		sut.Level += (_, _) => received.Add("event");

		sut.Mock.Raise.Level(sut, EventArgs.Empty);
		int property = ((IMembersHiddenByOtherKind)sut).Level;

		await That(received).IsEqualTo(["event",]);
		await That(property).IsEqualTo(7);
		await That(sut.Mock.Verify.Level.Subscribed()).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.Level.Got()).Once();
	}

	[Test]
	public async Task Method_WithHiddenEvent_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersHidingOtherKind sut = IMembersHidingOtherKind.CreateMock();
		List<string> received = [];
		sut.Mock.Setup.Reset().Do(() => received.Add("method"));
		((IMembersHiddenByOtherKind)sut).Reset += (_, _) => received.Add("event");

		sut.Reset();
		sut.Mock.As<IMembersHiddenByOtherKind>().Raise.Reset(sut, EventArgs.Empty);

		await That(received).IsEqualTo(["method", "event",]);
		await That(sut.Mock.Verify.Reset()).Once();
		await That(sut.Mock.As<IMembersHiddenByOtherKind>().Verify.Reset.Subscribed()).Once();
	}

	[Test]
	public async Task WithSiblingMembers_ShouldSetUpAndVerifyBothMembers()
	{
		IMembersOfOtherKindSiblings sut = IMembersOfOtherKindSiblings.CreateMock();
		List<int> received = [];
		sut.Mock.Setup.Start(It.IsAny<int>()).Do(value => received.Add(value));
		sut.Mock.As<IMembersOfOtherKindSibling>().Setup.Start.InitializeWith(8);

		((IMembersHiddenByOtherKind)sut).Start(2);
		int property = ((IMembersOfOtherKindSibling)sut).Start;

		await That(received).IsEqualTo([2,]);
		await That(property).IsEqualTo(8);
		await That(sut.Mock.Verify.Start(2)).Once();
		await That(sut.Mock.As<IMembersOfOtherKindSibling>().Verify.Start.Got()).Once();
	}

	[Test]
	public async Task Property_WithHiddenVirtualMethodOfBaseClass_ShouldOnlyMockTheProperty()
	{
		MembersHidingOtherKindBase sut = MembersHidingOtherKindBase.CreateMock();
		sut.Mock.Setup.Run.InitializeWith(5);

		int property = sut.Run;
		int method = ((MembersHiddenByOtherKindBase)sut).Run();

		await That(property).IsEqualTo(5);
		await That(method).IsEqualTo(1)
			.Because("the mock cannot override a method that the property of the same name hides");
		await That(sut.Mock.Verify.Run.Got()).Once();
	}
}
