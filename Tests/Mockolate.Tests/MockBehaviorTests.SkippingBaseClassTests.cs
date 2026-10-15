namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class SkippingBaseClassTests
	{
		[Test]
		public async Task DefaultBehavior_ForRefAndOutParameter_WhenMethodNotSetup_ShouldUseDefaultValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);
			int value1 = 5;

			int sum = sut.VirtualMethodWithRefAndOutParameters(ref value1, out int value2);

			await That(value1).IsEqualTo(15);
			await That(value2).IsEqualTo(30);
			await That(sum).IsEqualTo(45);
			await That(sut.VirtualMethodWithRefAndOutParametersCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_ForRefAndOutParameter_WhenMethodSetup_ShouldUseSetupValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);
			int value1 = 5;
			sut.Mock.Setup.VirtualMethodWithRefAndOutParameters(It.IsRef<int>(x => x + 1), It.IsOut(() => 8))
				.Returns(10);

			int sum = sut.VirtualMethodWithRefAndOutParameters(ref value1, out int value2);

			await That(value1).IsEqualTo(16);
			await That(value2).IsEqualTo(8);
			await That(sum).IsEqualTo(10);
			await That(sut.VirtualMethodWithRefAndOutParametersCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenIndexerNotSetup_ShouldInitializeIndexerValuesFromBaseClass()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);

			int result = sut[4];
			sut[4] = 42;
			int result2 = sut[4];
			int result3 = sut[3];

			await That(result).IsEqualTo(8);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(6);
			await That(sut.VirtualIndexerGetterCallCount).IsEqualTo(3);
			await That(sut.VirtualIndexerSetterCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenIndexerSetup_ShouldUseSetupValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);
			sut.Mock.Setup[It.IsAny<int>()].Returns(15);

			int result = sut[1];
			sut[1] = 42;
			int result2 = sut[1];

			await That(result).IsEqualTo(15);
			await That(result2).IsEqualTo(15);
			await That(sut.VirtualIndexerGetterCallCount).IsEqualTo(2);
			await That(sut.VirtualIndexerSetterCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenMethodNotSetup_ShouldReturnBaseValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);

			int[] value = sut.VirtualMethod();

			await That(value).IsEqualTo([4, 5,]);
			await That(sut.VirtualMethodCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenMethodSetup_ShouldReturnSetupValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);
			sut.Mock.Setup.VirtualMethod().Returns([10, 20,]);

			int[] value = sut.VirtualMethod();

			await That(value).IsEqualTo([10, 20,]);
			await That(sut.VirtualMethodCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenPropertyNotSetup_ShouldInitializePropertyWithValueFromBaseClass()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);

			int result = sut.VirtualProperty;
			sut.VirtualProperty = 42;
			int result2 = sut.VirtualProperty;

			await That(result).IsEqualTo(8);
			await That(sut.VirtualPropertyValue).IsEqualTo(42);
			await That(result2).IsEqualTo(42);
			await That(sut.VirtualPropertyGetterCallCount).IsEqualTo(2);
			await That(sut.VirtualPropertySetterCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task DefaultBehavior_WhenPropertySetup_ShouldUseSetupValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default);
			sut.Mock.Setup.VirtualProperty.Returns(15);

			int result = sut.VirtualProperty;
			sut.VirtualProperty = 42;
			int result2 = sut.VirtualProperty;

			await That(result).IsEqualTo(15);
			await That(sut.VirtualPropertyValue).IsEqualTo(42);
			await That(result2).IsEqualTo(15);
			await That(sut.VirtualPropertyGetterCallCount).IsEqualTo(2);
			await That(sut.VirtualPropertySetterCallCount).IsEqualTo(1);
		}

		[Test]
		public async Task
			SkippingBaseClass_ForRefAndOutParameter_WhenMethodNotSetup_ShouldSetToPreviousOrDefaultValues()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default.SkippingBaseClass());
			int value1 = 5;

			int sum = sut.VirtualMethodWithRefAndOutParameters(ref value1, out int value2);

			await That(value1).IsEqualTo(5);
			await That(value2).IsEqualTo(0);
			await That(sum).IsEqualTo(0);
			await That(sut.VirtualMethodWithRefAndOutParametersCallCount).IsEqualTo(0);
		}

		[Test]
		public async Task SkippingBaseClass_ShouldNotCallIndexersOfBaseClass()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default.SkippingBaseClass());

			int result = sut[1];
			sut[1] = 2;
			int result2 = sut[1];

			await That(result).IsEqualTo(0);
			await That(result2).IsEqualTo(2);
			await That(sut.VirtualIndexerGetterCallCount).IsEqualTo(0);
			await That(sut.VirtualIndexerSetterCallCount).IsEqualTo(0);
		}

		[Test]
		public async Task SkippingBaseClass_ShouldNotCallMethodOfBaseClass()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default.SkippingBaseClass());

			_ = sut.VirtualMethod();

			await That(sut.VirtualMethodCallCount).IsEqualTo(0);
		}

		[Test]
		public async Task SkippingBaseClass_ShouldNotCallPropertiesOfBaseClass()
		{
			MyBaseClassWithVirtualCalls sut =
				MyBaseClassWithVirtualCalls.CreateMock(MockBehavior.Default.SkippingBaseClass());

			int result = sut.VirtualProperty;
			sut.VirtualProperty = 42;
			int result2 = sut.VirtualProperty;

			await That(result).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(sut.VirtualPropertyGetterCallCount).IsEqualTo(0);
			await That(sut.VirtualPropertySetterCallCount).IsEqualTo(0);
		}

		public class MyBaseClassWithVirtualCalls
		{
			public int Sum { get; private set; }

			public int VirtualMethodCallCount { get; private set; }
			public int VirtualMethodWithRefAndOutParametersCallCount { get; private set; }

			public int VirtualPropertyGetterCallCount { get; private set; }
			public int VirtualPropertySetterCallCount { get; private set; }
			public int VirtualPropertyValue { get; set; } = 8;
			public int VirtualIndexerGetterCallCount { get; private set; }
			public int VirtualIndexerSetterCallCount { get; private set; }

			public virtual int VirtualProperty
			{
				get
				{
					VirtualPropertyGetterCallCount++;
					return VirtualPropertyValue;
				}
				set
				{
					VirtualPropertySetterCallCount++;
					VirtualPropertyValue = value;
				}
			}

			public virtual int this[int key]
			{
				get
				{
					VirtualIndexerGetterCallCount++;
					return key * 2;
				}
				// ReSharper disable once ValueParameterNotUsed
				set => VirtualIndexerSetterCallCount++;
			}

			public virtual int[] VirtualMethod()
			{
				VirtualMethodCallCount++;
				return [4, 5,];
			}

			public virtual int VirtualMethodWithRefAndOutParameters(ref int value1, out int value2)
			{
				VirtualMethodWithRefAndOutParametersCallCount++;
				value1 += 10;
				value2 = 2 * value1;
				return value1 + value2;
			}

			public virtual void VirtualVoidMethod(int increment) => Sum += increment;
		}
	}
}
