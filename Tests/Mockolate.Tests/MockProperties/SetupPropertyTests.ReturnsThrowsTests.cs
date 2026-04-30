using System.Collections.Generic;
using Mockolate.Setup;
using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockProperties;

public sealed partial class SetupPropertyTests
{
	public sealed class ReturnsThrowsTests
	{
		[Test]
		public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(4)
				.Throws(new Exception("foo"))
				.Returns(() => 2);

			int result1 = sut.MyProperty;
			Exception? result2 = Catch.Exception(() => _ = sut.MyProperty);
			int result3 = sut.MyProperty;

			await That(result1).IsEqualTo(4);
			await That(result2).HasMessage("foo");
			await That(result3).IsEqualTo(2);
		}

		[Test]
		public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(4)
				.Returns(() => 3)
				.Returns(v => 10 * v);

			int[] result = new int[10];
			for (int i = 0; i < 10; i++)
			{
				result[i] = sut.MyProperty;
			}

			await That(result).IsEqualTo([4, 3, 30, 4, 3, 30, 4, 3, 30, 4,]);
		}

		[Test]
		public async Task Returns_Callback_ShouldReturnExpectedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(() => 4);

			int result = sut.MyProperty;

			await That(result).IsEqualTo(4);
		}

		[Test]
		public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.InitializeWith(3)
				.Returns(x => 4 * x);

			int result = sut.MyProperty;

			await That(result).IsEqualTo(12);
		}

		[Test]
		public async Task Returns_CallbackWithWhen_ShouldReturnDefaultValueWhenPredicateIsFalse()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(() => 4).When(i => i > 0);

			int result1 = sut.MyProperty;
			int result2 = sut.MyProperty;

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(4);
		}

		[Test]
		public async Task Returns_For_OnlyOnce_ShouldLimitUsage_ToSpecifiedNumber()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyStringProperty
				.Returns("foo").For(2).OnlyOnce()
				.Returns("bar").For(3).OnlyOnce();

			List<string?> values = [];
			for (int i = 0; i < 11; i++)
			{
				sut.MyStringProperty = "-";
				values.Add(sut.MyStringProperty);
			}

			await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "-", "-", "-", "-", "-", "-",]);
		}

		[Test]
		public async Task Returns_For_ShouldRepeatUsage_ToSpecifiedNumber()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyStringProperty
				.Returns("foo").For(2)
				.Returns("bar").For(3);

			List<string?> values = [];
			for (int i = 0; i < 11; i++)
			{
				sut.MyStringProperty = "-";
				values.Add(sut.MyStringProperty);
			}

			await That(values)
				.IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar", "foo",]);
		}

		[Test]
		public async Task Returns_Forever_ShouldUseTheLastValueForever()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(2)
				.Returns(3)
				.Returns(4).Forever();

			int[] result = new int[10];
			for (int i = 0; i < 10; i++)
			{
				result[i] = sut.MyProperty;
			}

			await That(result).IsEqualTo([2, 3, 4, 4, 4, 4, 4, 4, 4, 4,]);
		}

		[Test]
		public async Task Returns_OnlyOnce_ShouldKeepLastUsedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyProperty;
				if (i == 4)
				{
					sut.MyProperty = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task Returns_OnlyOnce_ShouldUseReturnValueOnlyOnce()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut.MyProperty = 0;
				values[i] = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task Returns_PredicateIsFalse_ShouldUseInitializedDefaultValue()
		{
			List<int> results = [];
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(() => 4).When(i => i is > 3 and < 6);

			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			sut.MyProperty = -3;
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);
			results.Add(sut.MyProperty);

			await That(results).IsEqualTo([0, 0, -3, -3, 4, 4, 4,]);
		}

		[Test]
		public async Task Returns_ShouldReturnExpectedValue()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Returns(4);

			int result = sut.MyProperty;

			await That(result).IsEqualTo(4);
		}

		[Test]
		public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyStringProperty
				.Returns("foo").When(i => i > 0);

			string? result1 = sut.MyStringProperty;
			string? result2 = sut.MyStringProperty;
			string? result3 = sut.MyStringProperty;

			await That(result1).IsEqualTo("");
			await That(result2).IsEqualTo("foo");
			await That(result3).IsEqualTo("foo");
		}

		[Test]
		public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyStringProperty
				.Returns("foo").When(i => i > 0).For(2)
				.Returns("baz")
				.Returns("bar").For(3).OnlyOnce();

			List<string?> values = [];
			for (int i = 0; i < 14; i++)
			{
				values.Add(sut.MyStringProperty);
			}

			await That(values).IsEqualTo([
				"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
			]);
		}

		[Test]
		public async Task Returns_WithoutSetup_ShouldReturnDefault()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			int result = sut.MyProperty;

			await That(result).IsEqualTo(0);
		}

		[Test]
		public async Task Throws_Callback_ShouldThrowException()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Throws(() => new Exception("foo"));

			void Act()
			{
				_ = sut.MyProperty;
			}

			await That(Act).Throws().WithMessage("foo");
		}

		[Test]
		public async Task Throws_CallbackWithValue_ShouldThrowException()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.InitializeWith(42)
				.Throws(v => new Exception($"foo-{v}"));

			void Act()
			{
				_ = sut.MyProperty;
			}

			await That(Act).Throws().WithMessage("foo-42");
		}

		[Test]
		public async Task Throws_Generic_ShouldThrowException()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Throws<ArgumentNullException>();

			void Act()
			{
				_ = sut.MyProperty;
			}

			await That(Act).ThrowsExactly<ArgumentNullException>();
		}

		[Test]
		public async Task Throws_ShouldThrowException()
		{
			IPropertyService sut = IPropertyService.CreateMock();

			sut.Mock.Setup.MyProperty
				.Throws(new Exception("foo"));

			void Act()
			{
				_ = sut.MyProperty;
			}

			await That(Act).Throws().WithMessage("foo");
		}

		[Test]
		public async Task WithoutCallback_IPropertySetupReturnBuilder_ShouldNotThrow()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			IPropertySetupReturnBuilder<int> setup =
				sut.Mock.Setup.MyProperty;

			void ActWhen()
			{
				setup.When(_ => true);
			}

			void ActFor()
			{
				setup.For(2);
			}

			await That(ActWhen).DoesNotThrow();
			await That(ActFor).DoesNotThrow();
		}

		[Test]
		public async Task WithoutCallback_IPropertySetupReturnWhenBuilder_ShouldNotThrow()
		{
			IPropertyService sut = IPropertyService.CreateMock();
			IPropertySetupReturnWhenBuilder<int> setup =
				sut.Mock.Setup.MyProperty;

			void ActFor()
			{
				setup.For(2);
			}

			void ActOnly()
			{
				setup.Only(2);
			}

			await That(ActFor).DoesNotThrow();
			await That(ActOnly).DoesNotThrow();
		}
	}
}
