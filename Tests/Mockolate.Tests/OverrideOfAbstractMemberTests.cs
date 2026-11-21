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
	public async Task GetterOnlyOverride_ShouldUseSetups()
	{
		GetterOnlyOverride sut = GetterOnlyOverride.CreateMock();
		sut.Mock.Setup.Value.InitializeWith("initial");
		sut.Mock.Setup[It.IsAny<int>()].Returns(5);

		string value = sut.Value;
		int indexerValue = sut[3];

		await That(value).IsEqualTo("initial");
		await That(indexerValue).IsEqualTo(5);
	}

	[Test]
	public async Task GetterOnlyOverride_WhenSet_ShouldStoreValueInMock()
	{
		GetterOnlyOverride sut = GetterOnlyOverride.CreateMock();

		sut.Value = "foo";
		sut[1] = 4;

		await That(sut.Value).IsEqualTo("foo").Because("the setter is still abstract, so the mock stores the value");
		await That(sut[1]).IsEqualTo(4);
	}

	[Test]
	public async Task SetterOnlyOverride_ShouldUseSetups()
	{
		SetterOnlyOverride sut = SetterOnlyOverride.CreateMock();
		sut.Mock.Setup.Value.InitializeWith("initial");
		sut.Mock.Setup[It.IsAny<int>()].Returns(5);

		string value = sut.Value;
		int indexerValue = sut[3];

		await That(value).IsEqualTo("initial");
		await That(indexerValue).IsEqualTo(5);
	}

	[Test]
	public async Task ReabstractedWithRenamedParameters_ShouldUseSetups()
	{
		ReabstractedWithRenamedParameters sut = ReabstractedWithRenamedParameters.CreateMock();
		sut.Mock.Setup[It.IsAny<int>()].Returns(5);
		sut.Mock.Setup.Echo<int>(It.IsAny<int>()).Returns(7);

		int indexerValue = sut[3];
		int echoed = sut.Echo(1);
		string? echoedString = sut.Echo("a");

		await That(indexerValue).IsEqualTo(5);
		await That(echoed).IsEqualTo(7);
		await That(echoedString).IsNotEqualTo("a").Because("the re-abstracted method does not call the virtual method of the base class");
		await That(sut.Mock.Verify.Echo(1)).Once();
	}

	[Test]
	public async Task ReabstractedGenericMethod_WithRenamedTypeParameter_ShouldUseSetup()
	{
		ReabstractedGenericMethod sut = ReabstractedGenericMethod.CreateMock();
		sut.Mock.Setup.Get<int>().Returns(7);

		int result = sut.Get<int>();

		await That(result).IsEqualTo(7);
		await That(sut.Mock.Verify.Get<int>()).Once();
	}

	[Test]
	public async Task ReabstractedGetterOnly_ShouldUseSetup()
	{
		ReabstractedGetterOnly sut = ReabstractedGetterOnly.CreateMock();
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
