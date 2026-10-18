namespace Mockolate.Tests;

public sealed partial class MockTests
{
	public sealed class HiddenInterfaceMemberTests
	{
		[Test]
		public async Task HiddenEvent_AsBase_ShouldVerifyBaseSlotSeparately()
		{
			IDerivedEvents mock = IDerivedEvents.CreateMock();
			EventHandler handler = (_, _) => { };
			((IBaseEvents)mock).Changed += handler;

			await That(mock.Mock.As<IBaseEvents>().Verify.Changed.Subscribed()).Once();
			await That(mock.Mock.Verify.Changed.Subscribed()).Never();
		}

		[Test]
		public async Task HiddenIndexer_AsBase_ShouldVerifyBaseAccess()
		{
			IDerivedIndexer mock = IDerivedIndexer.CreateMock();

			_ = ((IBaseIndexer)mock)[5];

			await That(mock.Mock.As<IBaseIndexer>().Verify[It.IsAny<int>()].Got()).Once()
				.Because(
					"indexers are keyed by their parameter signature rather than the declaring interface, so base and derived access share storage and As<TBase> reaches the same recorded interaction");
		}

		[Test]
		public async Task HiddenMethod_AsBase_ShouldConfigureAndVerifyBaseSlotSeparately()
		{
			IDerivedService mock = IDerivedService.CreateMock();
			mock.Mock.Setup.GetValue().Returns(42);
			mock.Mock.As<IBaseService>().Setup.GetValue().Returns(43);

			await That(mock.GetValue()).IsEqualTo(42);
			await That(((IBaseService)mock).GetValue()).IsEqualTo(43);
			await That(mock.Mock.Verify.GetValue()).Once();
			await That(mock.Mock.As<IBaseService>().Verify.GetValue()).Once();
		}

		[Test]
		public async Task HiddenProperty_AsBase_ShouldReadBaseSlot()
		{
			IDerivedProperty mock = IDerivedProperty.CreateMock();
			mock.Mock.Setup.SomeProperty.InitializeWith("derived");
			mock.Mock.As<IBaseProperty>().Setup.SomeProperty.Returns("base");

			mock.SomeProperty = "updated";

			await That(mock.SomeProperty).IsEqualTo("updated");
			await That(((IBaseProperty)mock).SomeProperty).IsEqualTo("base");
		}

		internal interface IBaseService
		{
			int GetValue();
		}

		internal interface IDerivedService : IBaseService
		{
			new int GetValue();
		}

		internal interface IBaseProperty
		{
			string SomeProperty { get; }
		}

		internal interface IDerivedProperty : IBaseProperty
		{
			new string SomeProperty { get; set; }
		}

		internal interface IBaseEvents
		{
			event EventHandler? Changed;
		}

		internal interface IDerivedEvents : IBaseEvents
		{
			new event EventHandler? Changed;
		}

		internal interface IBaseIndexer
		{
			int this[int index] { get; }
		}

		internal interface IDerivedIndexer : IBaseIndexer
		{
			new int this[int index] { get; set; }
		}
	}
}
