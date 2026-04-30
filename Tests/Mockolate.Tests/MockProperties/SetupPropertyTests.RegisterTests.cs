using Mockolate.Exceptions;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class RegisterTests
	{
		[Test]
		public async Task AccessWithoutSetup_ShouldThrowMockNotSetupException()
		{
			IPropertyService mock = IPropertyService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

			void Act()
			{
				mock.MyStringProperty = "1";
			}

			await That(Act).Throws<MockNotSetupException>()
				.WithMessage(
					"The property 'global::Mockolate.Tests.MockProperties.SetupPropertyTests.IPropertyService.MyStringProperty' was accessed without prior setup.");
		}

		[Test]
		public async Task RegisterBeforeAccess_ShouldNotThrowAndReturnDefaultValue()
		{
			IPropertyService sut = IPropertyService.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

			sut.Mock.Setup.MyStringProperty.Register();

			string? initialResult = sut.MyStringProperty;
			sut.MyStringProperty = "foo";
			string? otherResult = sut.MyStringProperty;

			await That(initialResult).IsEmpty();
			await That(otherResult).IsEqualTo("foo");
		}
	}
}
