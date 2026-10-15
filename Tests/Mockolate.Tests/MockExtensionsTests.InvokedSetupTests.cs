using Mockolate.Exceptions;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed partial class MockExtensionsTests
{
	public sealed class InvokedSetupTests
	{
		[Test]
		public async Task WhenMethodSetupIsNotVerifiable_ShouldThrowMockException()
		{
			MyServiceBase sut = MyServiceBase.CreateMock();
			IMethodSetup setup = new MyMethodSetup();

			void Act()
			{
				sut.Mock.VerifySetup(setup).Never();
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The setup is not verifiable.");
		}

		[Test]
		public async Task WhenSubjectIsNoMock_ShouldThrowMockException()
		{
			MyServiceBase mock = MyServiceBase.CreateMock();
			IMethodSetup setup = mock.Mock.Setup.DoSomething(Match.AnyParameters());
			MyServiceBase sut = new();

			void Act()
			{
				sut.Mock.VerifySetup(setup).Never();
			}

			await That(Act).Throws<MockException>()
				.WithMessage("The subject is no mock.");
		}

		private sealed class MyMethodSetup : IMethodSetup
		{
			public string Name { get; } = "Foo";
		}
	}
}
