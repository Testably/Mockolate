using Mockolate.Exceptions;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class ThrowWhenNotSetupTests
	{
		[Test]
		public async Task WhenFalse_ShouldReturnDefaultValueInNotSetupMethods()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default with
			{
				ThrowWhenNotSetup = false,
			});

			string result = sut.DoSomethingAndReturn(5);

			await That(result).IsEqualTo("");
		}

		[Test]
		public async Task WhenTrue_ShouldThrowMockNotSetupException()
		{
			IMyService sut = IMyService.CreateMock(MockBehavior.Default with
			{
				ThrowWhenNotSetup = true,
			});

			void Act()
			{
				sut.DoSomethingAndReturn(5);
			}

			await That(Act).Throws<MockNotSetupException>()
				.WithMessage(
					"The method 'global::Mockolate.Tests.TestHelpers.IMyService.DoSomethingAndReturn(int)' was invoked without prior setup.");
		}
	}
}
