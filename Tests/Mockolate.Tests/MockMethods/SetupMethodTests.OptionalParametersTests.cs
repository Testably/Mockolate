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
			sut.Mock.Setup.Count(It.IsAny<int>(), null).Returns(1);
			sut.Mock.Setup.Name(It.IsAny<int>(), null).Returns(2);

			int[] results = [sut.Count(1, 0), sut.Count(1), sut.Name(1, null), sut.Name(1),];

			await That(results).IsEqualTo([1, 0, 2, 0,])
				.Because("an explicit null stands for default(T), only a left-out parameter for its declared default");
		}

		[Test]
		public async Task WithOmittedParameter_ShouldOnlyMatchTheDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Count(It.IsAny<int>()).Returns(1);
			sut.Mock.Setup.Name(It.IsAny<int>()).Returns(2);

			int[] results = [sut.Count(1), sut.Count(1, 0), sut.Name(1), sut.Name(1, null),];

			await That(results).IsEqualTo([1, 0, 2, 0,]);
		}

		[Test]
		public async Task WithOmittedParameterAfterValue_ShouldKeepAnyParameters()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Count(2).AnyParameters().Returns(2);
			sut.Mock.Setup.Count(1).Returns(1);

			int[] results = [sut.Count(1), sut.Count(1, 0), sut.Count(3, 0),];

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
		public async Task Six_WithExplicitNullOrOmittedParameter_ShouldMatchTheTypeOrDeclaredDefault()
		{
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
				null).Returns(1);
			sut.Mock.Setup.Six(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(2);
			sut.Mock.Setup.Six(1, 2, 3, 4, 5).Returns(3);

			int[] results =
			[
				sut.Six(1, 2, 3, 4, 0, null), sut.Six(1, 2, 3, 4, 0), sut.Six(1, 2, 3, 4, 5), sut.Six(1, 2, 3, 4, 5, null),
			];

			await That(results).IsEqualTo([1, 2, 3, 1,]);
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
			IOptionalMethodParameters sut = IOptionalMethodParameters.CreateMock();
			sut.Mock.Setup.WithSibling(It.IsAny<int>(), null).Returns(1);

			int[] results = [sut.WithSibling(1, 5), sut.WithSibling(1, 0),];

			await That(results).IsEqualTo([1, 0,])
				.Because("next to a same-named method with fewer parameters the optional parameter keeps its = null default");
		}
	}
}
