using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	public sealed class OptionalParametersTests
	{
		[Test]
		public async Task WithExplicitNull_ShouldCountTheTypeDefaultInsteadOfTheDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Method(1, 0, null);
			sut.Method(1);

			await That(sut.Mock.Verify.Method(It.IsAny<int>(), null, null)).Once()
				.Because("an explicit null stands for default(T), only a left-out parameter for its declared default");
			await That(sut.Mock.Verify.Method(It.IsAny<int>(), It.IsAny<int>(), null)).Once();
		}

		[Test]
		public async Task WithOmittedParameters_ShouldOnlyCountTheDeclaredDefaults()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Method(1);
			sut.Method(1, 6);
			sut.Method(1, 5, null);

			await That(sut.Mock.Verify.Method(It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method(1, 6)).Once();
			await That(sut.Mock.Verify.Method(1).AnyParameters()).Exactly(3);
		}

		[Test]
		public async Task WithOmittedParameters_WhenFailing_ShouldShowTheDeclaredDefaults()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();

			void Act()
				=> sut.Mock.Verify.Method(It.IsAny<int>()).Once();

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method(It.IsAny<int>(), 5, x) exactly once, but it never did.");
		}

		[Test]
		public async Task AllOptional_WithoutArguments_ShouldCountTheDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.AllOptional();
			sut.AllOptional(0);

			await That(sut.Mock.Verify.AllOptional()).Once();
		}

		[Test]
		public async Task Generic_WithExplicitNullOrOmittedParameter_ShouldCountTheTypeOrDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Generic("a", 0);
			sut.Generic("a");
			sut.Generic("a");

			await That(sut.Mock.Verify.Generic<string>(It.IsAny<string>(), null)).Once();
			await That(sut.Mock.Verify.Generic<string>(It.IsAny<string>())).Twice();
		}

		[Test]
		public async Task Six_WithExplicitNullOrOmittedParameters_ShouldCountTheTypeOrDeclaredDefaults()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Six(1, 2, 3, 4, 0, null);
			sut.Six(1, 2, 3, 4);
			sut.Six(1, 2, 3, 4);

			await That(sut.Mock.Verify.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null,
				null)).Once();
			await That(sut.Mock.Verify.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
				.Twice();
			await That(sut.Mock.Verify.Six(1, 2, 3, 4)).Twice();
		}

		[Test]
		public async Task Delegate_WithExplicitNullOrOmittedParameter_ShouldCountTheTypeOrDeclaredDefault()
		{
			OptionalParametersDelegate sut = OptionalParametersDelegate.CreateMock();
			sut(1, null);
			sut(1);
			sut(1);

			await That(sut.Mock.Verify(It.IsAny<int>(), null)).Once();
			await That(sut.Mock.Verify(It.IsAny<int>())).Twice();
		}

		[Test]
		public async Task WithSibling_WithExplicitNull_ShouldStillCountTheDeclaredDefault()
		{
			IOptionalMethodParametersWithSibling sut = IOptionalMethodParametersWithSibling.CreateMock();
			sut.Method(1, 5);
			sut.Method(1, 0);

			await That(sut.Mock.Verify.Method(It.IsAny<int>(), null)).Once()
				.Because("next to a same-named method with fewer parameters the optional parameters keep their = null defaults");
			await That(sut.Mock.Verify.Method(It.IsAny<int>(), 0)).Once();
		}
	}
}
