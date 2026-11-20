using System.Collections.Generic;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	public sealed class OptionalParametersTests
	{
		[Test]
		public async Task WithExplicitNull_ShouldMatchTheTypeDefaultInsteadOfTheDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Method(It.IsAny<int>(), null, It.IsAny<string?>()).Returns(1);
			sut.Mock.Setup.Method(It.IsAny<int>(), It.IsAny<int>(), null).Returns(2);

			int[] results = [sut.Method(1, 0, "y"), sut.Method(1, 5, null), sut.Method(1, 5, "y"),];

			await That(results).IsEqualTo([1, 2, 0,])
				.Because("an explicit null stands for default(T), only a left-out parameter for its declared default");
		}

		[Test]
		public async Task WithOmittedParameters_ShouldOnlyMatchTheDeclaredDefaults()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Method(It.IsAny<int>()).Returns(1);
			sut.Mock.Setup.Method(It.IsAny<int>(), 6).Returns(2);

			int[] results =
			[
				sut.Method(1), sut.Method(1, 6), sut.Method(1, 0), sut.Method(1, 5, null), sut.Method(1, 6, "y"),
			];

			await That(results).IsEqualTo([1, 2, 0, 0, 0,]);
		}

		[Test]
		public async Task WithOmittedParametersAfterValues_ShouldKeepAnyParameters()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Method(2).AnyParameters().Returns(2);
			sut.Mock.Setup.Method(1).Returns(1);

			int[] results = [sut.Method(1), sut.Method(1, 0), sut.Method(3, 0, null),];

			await That(results).IsEqualTo([1, 2, 2,]);
		}

		[Test]
		public async Task AllOptional_WithoutArguments_ShouldMatchTheDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.AllOptional().Returns(1);

			int[] results = [sut.AllOptional(), sut.AllOptional(0),];

			await That(results).IsEqualTo([1, 0,]);
		}

		[Test]
		public async Task WithObject_WithOmittedParameter_ShouldTreatMatchersAsMatchers()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.WithObject(It.IsAny<string>()).Returns(1);

			int[] results = [sut.WithObject("a"), sut.WithObject(1), sut.WithObject("a", 6),];

			await That(results).IsEqualTo([1, 0, 0,])
				.Because("a matcher for an object parameter must not bind to the overload that takes it as a value");
		}

		[Test]
		public async Task VoidMethod_WithExplicitNullOrOmittedParameter_ShouldMatchTheTypeOrDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			List<string> calls = [];
			sut.Mock.Setup.VoidMethod(It.IsAny<int>(), null).Do(() => calls.Add("null"));
			sut.Mock.Setup.VoidMethod(It.IsAny<int>()).Do(() => calls.Add("omitted"));

			sut.VoidMethod(1, 0);
			sut.VoidMethod(1);
			sut.VoidMethod(1, 6);

			await That(calls).IsEqualTo(["null", "omitted",]);
		}

		[Test]
		public async Task Generic_WithExplicitNullOrOmittedParameter_ShouldMatchTheTypeOrDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Generic<string>(It.IsAny<string>(), null).Returns(1);
			sut.Mock.Setup.Generic<string>(It.IsAny<string>()).Returns(2);

			int[] results = [sut.Generic("a", 0), sut.Generic("a"), sut.Generic("a", 6),];

			await That(results).IsEqualTo([1, 2, 0,]);
		}

		[Test]
		public async Task Six_WithExplicitNullOrOmittedParameters_ShouldMatchTheTypeOrDeclaredDefaults()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null, null)
				.Returns(1);
			sut.Mock.Setup.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(2);
			sut.Mock.Setup.Six(1, 2, 3, 4, 6).Returns(3);

			int[] results =
			[
				sut.Six(1, 2, 3, 4, 0, null), sut.Six(1, 2, 3, 4), sut.Six(1, 2, 3, 4, 6), sut.Six(1, 2, 3, 4, 6, null),
			];

			await That(results).IsEqualTo([1, 2, 3, 0,]);
		}

		[Test]
		public async Task Delegate_WithExplicitNullOrOmittedParameter_ShouldMatchTheTypeOrDeclaredDefault()
		{
			OptionalParametersDelegate sut = OptionalParametersDelegate.CreateMock();
			sut.Mock.Setup(It.IsAny<int>(), null).Returns(1);
			sut.Mock.Setup(It.IsAny<int>()).Returns(2);

			int[] results = [sut(1, null), sut(1), sut(1, "y"),];

			await That(results).IsEqualTo([1, 2, 0,]);
		}

		[Test]
		public async Task WithSibling_WithExplicitNull_ShouldStillMatchTheDeclaredDefault()
		{
			IOptionalMethodParametersWithSibling sut = IOptionalMethodParametersWithSibling.CreateMock();
			sut.Mock.Setup.Method(It.IsAny<int>(), null).Returns(1);

			int[] results = [sut.Method(1, 5), sut.Method(1, 0),];

			await That(results).IsEqualTo([1, 0,])
				.Because("next to a same-named method with fewer parameters the optional parameters keep their = null defaults");
		}
	}
}
