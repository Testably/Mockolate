using System;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed class OverrideOfAbstractMemberTests
{
	[Test]
	public async Task Event_WhenSubscribed_ShouldForwardToOverride()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock();
		int callCount = 0;
		EventHandler handler = (_, _) => callCount++;

		sut.Changed += handler;
		sut.RaiseChanged();

		await That(callCount).IsEqualTo(1).Because("the subscription is forwarded to the override of the abstract event");
	}

	[Test]
	public async Task Indexer_WhenNotSetUp_ShouldCallOverride()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock();

		int result = sut[3];

		await That(result).IsEqualTo(6);
	}

	[Test]
	public async Task Method_WhenNotSetUp_ShouldCallOverride()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock();

		int result = sut.GetValue();

		await That(result).IsEqualTo(42);
		await That(sut.Mock.Verify.GetValue()).Once();
	}

	[Test]
	public async Task Event_WhenReabstracted_ShouldBeRaisedByMock()
	{
		ReabstractedMember sut = ReabstractedMember.CreateMock();
		int callCount = 0;
		sut.Changed += (_, _) => callCount++;

		sut.Mock.Raise.Changed(sut, EventArgs.Empty);
		sut.RaiseChanged();

		await That(callCount).IsEqualTo(1).Because("the re-abstracted event is not forwarded to the override of the base class");
	}

	[Test]
	public async Task Method_WhenReabstracted_ShouldReturnDefaultValue()
	{
		ReabstractedMember sut = ReabstractedMember.CreateMock();

		int result = sut.GetValue();

		await That(result).IsEqualTo(0).Because("the mocked class declares the method abstract again, so there is no implementation to call");
		await That(sut.Mock.Verify.GetValue()).Once();
	}

	[Test]
	public async Task Property_WhenReabstracted_ShouldUseSetup()
	{
		ReabstractedMember sut = ReabstractedMember.CreateMock();
		sut.Mock.Setup.Name.InitializeWith("initial");

		string result = sut.Name;

		await That(result).IsEqualTo("initial");
	}

	[Test]
	public async Task Method_WhenSetUp_ShouldUseSetup()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock();
		sut.Mock.Setup.GetValue().Returns(5);

		int result = sut.GetValue();

		await That(result).IsEqualTo(5);
	}

	[Test]
	public async Task Method_WhenSkippingBaseClass_ShouldReturnDefaultValue()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock(MockBehavior.Default.SkippingBaseClass());

		int result = sut.GetValue();

		await That(result).IsEqualTo(0);
	}

	[Test]
	public async Task Property_WhenNotSetUp_ShouldInitializeFromOverride()
	{
		OverrideOfAbstractMember sut = OverrideOfAbstractMember.CreateMock();

		string result = sut.Name;

		await That(result).IsEqualTo("base");
	}
}
