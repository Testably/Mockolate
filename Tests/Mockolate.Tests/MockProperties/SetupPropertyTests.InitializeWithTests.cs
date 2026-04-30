using System.Collections.Generic;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class InitializeWithTests
	{
		[Test]
		public async Task Returns_PredicateIsFalse_ShouldUseInitializedDefaultValue()
		{
			List<int> results = [];
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.InitializeWith(2)
				.Returns(() => 4).When(i => i > 3);

			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			sut.MyProperty = -3;
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);

			await That(results).IsEqualTo([2, 2, -3, -3, 4, 4, 4,]);
		}

		[Test]
		public async Task WhenRead_ShouldReturnInitializedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.InitializeWith(42);

			int result = sut.MyProperty;

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task WhenSet_ShouldUpdateValue_ShouldReturnInitializedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.InitializeWith(42);

			int result1 = sut.MyProperty;
			sut.MyProperty = 100;
			int result2 = sut.MyProperty;

			await That(result1).IsEqualTo(42);
			await That(result2).IsEqualTo(100);
		}

		[Test]
		public async Task WithNull_ShouldReturnNull()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyStringProperty.InitializeWith(null!);

			string? result = sut.MyStringProperty;

			await That(result).IsNull();
		}
	}
}
