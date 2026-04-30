namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class SkippingBaseClassTests
	{
		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyProperty_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass, int expectedCallCount)
		{
			MyPropertyService sut = MyPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.SkippingBaseClass(skipBaseClass);

			_ = sut.MyProperty;

			await That(sut.MyPropertyGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyProperty_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass, int expectedCallCount)
		{
			MyPropertyService sut = MyPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.SkippingBaseClass(skipBaseClass);

			sut.MyProperty = 1;

			await That(sut.MyPropertySetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		public async Task SetupSkippingBaseClassWithoutParameter_ShouldReturnDefaultValue()
		{
			MyPropertyService sut = MyPropertyService.CreateMock();
			sut.Mock.Setup.MyPropertyReturning2.SkippingBaseClass();

			int result = sut.MyPropertyReturning2;

			await That(result).IsEqualTo(0);
		}

		public class MyPropertyService
		{
			public int MyPropertyGetterCallCount { get; private set; }
			public int MyPropertySetterCallCount { get; private set; }

			public virtual int MyProperty
			{
				get => MyPropertyGetterCallCount++;
				set => MyPropertySetterCallCount += value;
			}

			public virtual int MyPropertyReturning2 => 2;
		}
	}
}
