using System.Collections.Generic;
using Mockolate.Setup;

namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	public sealed class ReturnsThrowsTests
	{
		public class ReturnMethodWith0Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method0()
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method0();
				Exception? result2 = Record.Exception(() => sut.Method0());
				string result3 = sut.Method0();

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("d")
					.Returns("c")
					.Returns(() => "b");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method0();
				}

				await That(result).IsEqualTo(["d", "c", "b", "d", "c", "b", "d", "c", "b", "d",]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method0()
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Only_ShouldLimitMatches()
			{
				List<string> results = [];
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("a").When(v => v > 1).Only(2);

				results.Add(sut.Method0());
				results.Add(sut.Method0());
				results.Add(sut.Method0());
				results.Add(sut.Method0());
				results.Add(sut.Method0());

				await That(results).IsEqualTo(["", "", "a", "a", "",]);
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Returns(() => "d");

				string result = sut.Method0();

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method0());
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method0();
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method0());
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Returns("d");

				string result = sut.Method0();

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method0();
				string result2 = sut.Method0();
				string result3 = sut.Method0();

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method0());
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method0();

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method0().Do(() => { });

				string result = sut.Method0();

				await That(result).IsEqualTo("foo");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldReturnSetupValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method0()
					.Returns("bar");

				string result = sut.Method0();

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method0();
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method0();
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws(new Exception("foo"));

				void Act()
				{
					sut.Method0();
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string> setup =
					(IReturnMethodSetupReturnBuilder<string>)sut.Mock.Setup.Method0();

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string> setup =
					(IReturnMethodSetupReturnWhenBuilder<string>)sut.Mock.Setup.Method0();

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

		public class ReturnMethodWith1Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method1(It.IsAny<int>())
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method1(1);
				Exception? result2 = Record.Exception(() => sut.Method1(2));
				string result3 = sut.Method1(3);

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("d")
					.Returns(() => "c")
					.Returns(v => $"foo-{v}");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method1(i);
				}

				await That(result).IsEqualTo(["d", "c", "foo-2", "d", "c", "foo-5", "d", "c", "foo-8", "d",]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method1(It.IsAny<int>())
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>()).Returns(() => "d");

				string result = sut.Method1(3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>()).Returns(x => $"foo-{x}");

				string result = sut.Method1(3);

				await That(result).IsEqualTo("foo-3");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method1(1));
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method1(i);
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method1(1));
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>()).Returns("d");

				string result = sut.Method1(3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method1(1);
				string result2 = sut.Method1(1);
				string result3 = sut.Method1(1);

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method1(1));
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method1(3);

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method1(It.IsAny<int>()).Do(() => { });

				string result = sut.Method1(1);

				await That(result).IsEqualTo("foo-1");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldReturnSetupValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Returns("bar");

				string result = sut.Method1(1);

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(v1 => new Exception("foo-" + v1));

				void Act()
				{
					sut.Method1(42);
				}

				await That(Act).Throws().WithMessage("foo-42");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
			{
				int callCount = 0;
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Do(() => { callCount++; })
					.Returns((string?)null!);

				string result = sut.Method1(1);

				await That(callCount).IsEqualTo(1);
				await That(result).IsNull();
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string, int> setup =
					(IReturnMethodSetupReturnBuilder<string, int>)sut.Mock.Setup.Method1(It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string, int> setup =
					(IReturnMethodSetupReturnWhenBuilder<string, int>)sut.Mock.Setup.Method1(It.IsAny<int>());

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

		public class ReturnMethodWith2Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method2(1, 2);
				Exception? result2 = Record.Exception(() => sut.Method2(2, 3));
				string result3 = sut.Method2(3, 4);

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Returns(() => "c")
					.Returns((v1, v2) => $"foo-{v1}-{v2}");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method2(i, 2 * i);
				}

				await That(result).IsEqualTo(["d", "c", "foo-2-4", "d", "c", "foo-5-10", "d", "c", "foo-8-16", "d",]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).Returns(() => "d");

				string result = sut.Method2(2, 3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).Returns((x, y) => $"foo-{x}-{y}");

				string result = sut.Method2(2, 3);

				await That(result).IsEqualTo("foo-2-3");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method2(1, 2));
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method2(i, 2);
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method2(1, 2));
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).Returns("d");

				string result = sut.Method2(2, 3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method2(1, 2);
				string result2 = sut.Method2(1, 2);
				string result3 = sut.Method2(1, 2);

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method2(1, 2));
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method2(2, 3);

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).Do(() => { });

				string result = sut.Method2(1, 2);

				await That(result).IsEqualTo("foo-1-2");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldReturnSetupValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Returns("bar");

				string result = sut.Method2(1, 2);

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2) => new Exception($"foo-{v1}-{v2}"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo-1-2");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
			{
				int callCount = 0;
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Do(() => { callCount++; })
					.Returns((string?)null!);

				string result = sut.Method2(1, 2);

				await That(callCount).IsEqualTo(1);
				await That(result).IsNull();
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string, int, int> setup =
					(IReturnMethodSetupReturnBuilder<string, int, int>)sut.Mock.Setup.Method2(
						It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string, int, int> setup =
					(IReturnMethodSetupReturnWhenBuilder<string, int, int>)sut.Mock.Setup.Method2(
						It.IsAny<int>(), It.IsAny<int>());

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

		public class ReturnMethodWith3Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method3(1, 2, 3);
				Exception? result2 = Record.Exception(() => sut.Method3(2, 3, 4));
				string result3 = sut.Method3(3, 4, 5);

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Returns(() => "c")
					.Returns((v1, v2, v3) => $"foo-{v1}-{v2}-{v3}");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method3(i, 2 * i, 3 * i);
				}

				await That(result).IsEqualTo(["d", "c", "foo-2-4-6", "d", "c", "foo-5-10-15", "d", "c", "foo-8-16-24", "d",]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns(() => "d");

				string result = sut.Method3(1, 2, 3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns((x, y, z) => $"foo-{x}-{y}-{z}");

				string result = sut.Method3(2, 3, 4);

				await That(result).IsEqualTo("foo-2-3-4");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method3(1, 2, 3));
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method3(i, 2, 3);
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method3(1, 2, 3));
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns("d");

				string result = sut.Method3(1, 2, 3);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method3(1, 2, 3);
				string result2 = sut.Method3(1, 2, 3);
				string result3 = sut.Method3(1, 2, 3);

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method3(1, 2, 3));
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method3(2, 3, 4);

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Do(() => { });

				string result = sut.Method3(1, 2, 3);

				await That(result).IsEqualTo("foo-1-2-3");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldReturnSetupValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("bar");

				string result = sut.Method3(1, 2, 3);

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2, v3) => new Exception($"foo-{v1}-{v2}-{v3}"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
			{
				int callCount = 0;
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Do(() => { callCount++; })
					.Returns((string?)null!);

				string result = sut.Method3(1, 2, 3);

				await That(callCount).IsEqualTo(1);
				await That(result).IsNull();
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string, int, int, int> setup =
					(IReturnMethodSetupReturnBuilder<string, int, int, int>)sut.Mock.Setup.Method3(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string, int, int, int> setup =
					(IReturnMethodSetupReturnWhenBuilder<string, int, int, int>)sut.Mock.Setup.Method3(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

		public class ReturnMethodWith4Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method4(1, 2, 3, 4);
				Exception? result2 = Record.Exception(() => sut.Method4(2, 3, 4, 5));
				string result3 = sut.Method4(3, 4, 5, 6);

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("d")
					.Returns(() => "c")
					.Returns((v1, v2, v3, v4) => $"foo-{v1}-{v2}-{v3}-{v4}");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method4(i, 2 * i, 3 * i, 4 * i);
				}

				await That(result).IsEqualTo([
					"d", "c", "foo-2-4-6-8", "d", "c", "foo-5-10-15-20", "d", "c", "foo-8-16-24-32", "d",
				]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns(() => "d");

				string result = sut.Method4(1, 2, 3, 4);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns((x, y, z, a) => $"foo-{x}-{y}-{z}-{a}");

				string result = sut.Method4(2, 3, 4, 5);

				await That(result).IsEqualTo("foo-2-3-4-5");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method4(1, 2, 3, 4));
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method4(i, 2, 3, 4);
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method4(1, 2, 3, 4));
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("d");

				string result = sut.Method4(1, 2, 3, 4);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method4(1, 2, 3, 4);
				string result2 = sut.Method4(1, 2, 3, 4);
				string result3 = sut.Method4(1, 2, 3, 4);

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method4(1, 2, 3, 4));
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method4(2, 3, 4, 5);

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Do(() => { });

				string result = sut.Method4(1, 2, 3, 4);

				await That(result).IsEqualTo("foo-1-2-3-4");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldIgnoreBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Returns("bar");

				string result = sut.Method4(1, 2, 3, 4);

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2, v3, v4) => new Exception($"foo-{v1}-{v2}-{v3}-{v4}"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3-4");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
			{
				int callCount = 0;
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Do(() => { callCount++; })
					.Returns((string?)null!);

				string result = sut.Method4(1, 2, 3, 4);

				await That(callCount).IsEqualTo(1);
				await That(result).IsNull();
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string, int, int, int, int> setup =
					(IReturnMethodSetupReturnBuilder<string, int, int, int, int>)sut.Mock.Setup.Method4(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string, int, int, int, int> setup =
					(IReturnMethodSetupReturnWhenBuilder<string, int, int, int, int>)sut.Mock.Setup.Method4(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

		public class ReturnMethodWith5Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
							It.IsAny<int>())
						.Returns("").For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixReturnsAndThrows_ShouldIterateThroughBoth()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("d")
					.Throws(new Exception("foo"))
					.Returns(() => "b");

				string result1 = sut.Method5(1, 2, 3, 4, 5);
				Exception? result2 = Record.Exception(() => sut.Method5(2, 3, 4, 5, 6));
				string result3 = sut.Method5(3, 4, 5, 6, 7);

				await That(result1).IsEqualTo("d");
				await That(result2).HasMessage("foo");
				await That(result3).IsEqualTo("b");
			}

			[Fact]
			public async Task MultipleReturns_ShouldIterateThroughAllRegisteredValues()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("d")
					.Returns(() => "c")
					.Returns((v1, v2, v3, v4, v5) => $"foo-{v1}-{v2}-{v3}-{v4}-{v5}");

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method5(i, 2 * i, 3 * i, 4 * i, 5 * i);
				}

				await That(result).IsEqualTo([
					"d", "c", "foo-2-4-6-8-10", "d", "c", "foo-5-10-15-20-25", "d", "c", "foo-8-16-24-32-40", "d",
				]);
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
							It.IsAny<int>())
						.Returns("").Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_Callback_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns(() => "d");

				string result = sut.Method5(1, 2, 3, 4, 5);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_CallbackWithValue_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns((x, y, z, a, b) => $"foo-{x}-{y}-{z}-{a}-{b}");

				string result = sut.Method5(2, 3, 4, 5, 6);

				await That(result).IsEqualTo("foo-2-3-4-5-6");
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("foo").For(2)
					.Returns("bar").For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method5(1, 2, 3, 4, 5));
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("a")
					.Returns("b")
					.Returns("c").Forever();

				string[] result = new string[10];
				for (int i = 0; i < 10; i++)
				{
					result[i] = sut.Method5(i, 2, 3, 4, 5);
				}

				await That(result).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("foo").Only(2)
					.Returns("bar").Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					values.Add(sut.Method5(1, 2, 3, 4, 5));
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_ShouldReturnExpectedValue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("d");

				string result = sut.Method5(1, 2, 3, 4, 5);

				await That(result).IsEqualTo("d");
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("foo").When(i => i > 0);

				string result1 = sut.Method5(1, 2, 3, 4, 5);
				string result2 = sut.Method5(1, 2, 3, 4, 5);
				string result3 = sut.Method5(1, 2, 3, 4, 5);

				await That(result1).IsEqualTo("");
				await That(result2).IsEqualTo("foo");
				await That(result3).IsEqualTo("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("foo").When(i => i > 0).For(2)
					.Returns("baz")
					.Returns("bar").For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					values.Add(sut.Method5(1, 2, 3, 4, 5));
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Returns_WithoutSetup_ShouldReturnDefault()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				string result = sut.Method5(2, 3, 4, 5, 6);

				await That(result).IsEmpty();
			}

			[Fact]
			public async Task SetupWithoutReturn_ShouldUseBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method5(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Do(() => { });

				string result = sut.Method5(1, 2, 3, 4, 5);

				await That(result).IsEqualTo("foo-1-2-3-4-5");
			}

			[Fact]
			public async Task SetupWithReturn_ShouldIgnoreBaseValue()
			{
				ReturnMethodSetupTest sut = ReturnMethodSetupTest.CreateMock();
				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Returns("bar");

				string result = sut.Method5(1, 2, 3, 4, 5);

				await That(result).IsEqualTo("bar");
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws((v1, v2, v3, v4, v5) => new Exception($"foo-{v1}-{v2}-{v3}-{v4}-{v5}"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3-4-5");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WhenSetupWithNull_ShouldReturnDefaultValue()
			{
				int callCount = 0;
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Do(() => { callCount++; })
					.Returns((string?)null!);

				string result = sut.Method5(1, 2, 3, 4, 5);

				await That(callCount).IsEqualTo(1);
				await That(result).IsNull();
			}

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnBuilder<string, int, int, int, int, int> setup =
					(IReturnMethodSetupReturnBuilder<string, int, int, int, int, int>)sut.Mock.Setup.Method5(
						It.IsAny<int>(),
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IReturnMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IReturnMethodSetupTest sut = IReturnMethodSetupTest.CreateMock();
				IReturnMethodSetupReturnWhenBuilder<string, int, int, int, int, int> setup =
					(IReturnMethodSetupReturnWhenBuilder<string, int, int, int, int, int>)sut.Mock.Setup.Method5(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

		public class VoidMethodWith0Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method0()
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method0();
				Exception? result1 = Record.Exception(() => sut.Method0());
				sut.Method0();
				sut.Method0();
				Exception? result2 = Record.Exception(() => sut.Method0());
				sut.Method0();

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method0()
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Only_ShouldLimitMatches()
			{
				List<string> results = [];
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("a")).When(v => v > 1).Only(2);

				for (int i = 0; i < 5; i++)
				{
					try
					{
						sut.Method0();
						results.Add("");
					}
					catch (Exception ex)
					{
						results.Add(ex.Message);
					}
				}

				await That(results).IsEqualTo(["", "", "a", "a", "",]);
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method0();
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method0();
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method0();
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method0()).DoesNotThrow();
				await That(() => sut.Method0()).Throws().WithMessage("foo");
				await That(() => sut.Method0()).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0()
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method0();
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method0();
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method0();
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method0().Throws(new Exception("foo"));

				void Act()
				{
					sut.Method0();
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder setup =
					(IVoidMethodSetupReturnBuilder)sut.Mock.Setup.Method0();

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder setup =
					(IVoidMethodSetupReturnWhenBuilder)sut.Mock.Setup.Method0();

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

		public class VoidMethodWith1Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method1(It.IsAny<int>())
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method1(1);
				Exception? result1 = Record.Exception(() => sut.Method1(2));
				sut.Method1(3);
				sut.Method1(4);
				Exception? result2 = Record.Exception(() => sut.Method1(5));
				sut.Method1(6);

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method1(It.IsAny<int>())
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method1(1);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method1(i);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method1(1);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method1(1)).DoesNotThrow();
				await That(() => sut.Method1(1)).Throws().WithMessage("foo");
				await That(() => sut.Method1(1)).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method1(1);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>()).Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>()).Throws(v1 => new Exception($"foo-{v1}"));

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).Throws().WithMessage("foo-1");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method1(It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method1(1);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder<int> setup =
					(IVoidMethodSetupReturnBuilder<int>)sut.Mock.Setup.Method1(It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder<int> setup =
					(IVoidMethodSetupReturnWhenBuilder<int>)sut.Mock.Setup.Method1(It.IsAny<int>());

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

		public class VoidMethodWith2Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method2(1, 2);
				Exception? result1 = Record.Exception(() => sut.Method2(2, 3));
				sut.Method2(3, 4);
				sut.Method2(4, 5);
				Exception? result2 = Record.Exception(() => sut.Method2(5, 6));
				sut.Method2(6, 7);

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method2(1, 2);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method2(i, 2);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method2(1, 2);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method2(1, 2)).DoesNotThrow();
				await That(() => sut.Method2(1, 2)).Throws().WithMessage("foo");
				await That(() => sut.Method2(1, 2)).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method2(1, 2);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2) => new Exception($"foo-{v1}-{v2}"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo-1-2");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method2(1, 2);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder<int, int> setup =
					(IVoidMethodSetupReturnBuilder<int, int>)sut.Mock.Setup.Method2(
						It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder<int, int> setup =
					(IVoidMethodSetupReturnWhenBuilder<int, int>)sut.Mock.Setup.Method2(
						It.IsAny<int>(), It.IsAny<int>());

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

		public class VoidMethodWith3Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method3(1, 2, 3);
				Exception? result1 = Record.Exception(() => sut.Method3(2, 3, 4));
				sut.Method3(3, 4, 5);
				sut.Method3(4, 5, 6);
				Exception? result2 = Record.Exception(() => sut.Method3(5, 6, 7));
				sut.Method3(6, 7, 8);

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method3(1, 2, 3);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method3(i, 2, 3);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method3(1, 2, 3);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method3(1, 2, 3)).DoesNotThrow();
				await That(() => sut.Method3(1, 2, 3)).Throws().WithMessage("foo");
				await That(() => sut.Method3(1, 2, 3)).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method3(1, 2, 3);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2, v3) => new Exception($"foo-{v1}-{v2}-{v3}"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method3(1, 2, 3);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder<int, int, int> setup =
					(IVoidMethodSetupReturnBuilder<int, int, int>)sut.Mock.Setup.Method3(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder<int, int, int> setup =
					(IVoidMethodSetupReturnWhenBuilder<int, int, int>)sut.Mock.Setup.Method3(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

		public class VoidMethodWith4Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method4(1, 2, 3, 4);
				Exception? result1 = Record.Exception(() => sut.Method4(2, 3, 4, 5));
				sut.Method4(3, 4, 5, 6);
				sut.Method4(4, 5, 6, 7);
				Exception? result2 = Record.Exception(() => sut.Method4(5, 6, 7, 8));
				sut.Method4(6, 7, 8, 9);

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method4(1, 2, 3, 4);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method4(i, 2, 3, 4);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method4(1, 2, 3, 4);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method4(1, 2, 3, 4)).DoesNotThrow();
				await That(() => sut.Method4(1, 2, 3, 4)).Throws().WithMessage("foo");
				await That(() => sut.Method4(1, 2, 3, 4)).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method4(1, 2, 3, 4);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws((v1, v2, v3, v4) => new Exception($"foo-{v1}-{v2}-{v3}-{v4}"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3-4");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method4(1, 2, 3, 4);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder<int, int, int, int> setup =
					(IVoidMethodSetupReturnBuilder<int, int, int, int>)sut.Mock.Setup.Method4(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder<int, int, int, int> setup =
					(IVoidMethodSetupReturnWhenBuilder<int, int, int, int>)sut.Mock.Setup.Method4(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

		public class VoidMethodWith5Parameters
		{
			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task For_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
							It.IsAny<int>())
						.Throws<Exception>().For(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task MixDoesNotThrowAndThrow_ShouldIterateThroughBoth()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.DoesNotThrow()
					.Throws(new Exception("foo"))
					.DoesNotThrow();

				sut.Method5(1, 2, 3, 4, 5);
				Exception? result1 = Record.Exception(() => sut.Method5(2, 3, 4, 5, 6));
				sut.Method5(3, 4, 5, 6, 7);
				sut.Method5(4, 5, 6, 7, 8);
				Exception? result2 = Record.Exception(() => sut.Method5(5, 6, 7, 8, 9));
				sut.Method5(6, 7, 8, 9, 10);

				await That(result1).HasMessage("foo");
				await That(result2).HasMessage("foo");
			}

			[Theory]
			[InlineData(-2)]
			[InlineData(0)]
			public async Task Only_LessThanOne_ShouldThrowArgumentOutOfRangeException(int times)
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				void Act()
				{
					sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
							It.IsAny<int>())
						.Throws<Exception>().Only(times);
				}

				await That(Act).Throws<ArgumentOutOfRangeException>()
					.WithMessage("Times must be greater than zero.").AsPrefix();
			}

			[Fact]
			public async Task Returns_For_ShouldRepeatUsage_ForTheSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo")).For(2)
					.Throws(new Exception("bar")).For(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method5(1, 2, 3, 4, 5);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "foo", "bar", "bar", "bar", "foo", "foo", "bar", "bar", "bar",]);
			}

			[Fact]
			public async Task Returns_Forever_ShouldUseTheLastValueForever()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("a"))
					.Throws(new Exception("b"))
					.Throws(new Exception("c")).Forever();

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method5(i, 2, 3, 4, 5);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["a", "b", "c", "c", "c", "c", "c", "c", "c", "c",]);
			}

			[Fact]
			public async Task Returns_Only_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo")).Only(2)
					.Throws(new Exception("bar")).Only(3);

				List<string> values = [];
				for (int i = 0; i < 10; i++)
				{
					try
					{
						sut.Method5(1, 2, 3, 4, 5);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo(["foo", "bar", "foo", "bar", "bar", "", "", "", "", "",]);
			}

			[Fact]
			public async Task Returns_When_ShouldOnlyUseValueWhenPredicateIsTrue()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0);

				await That(() => sut.Method5(1, 2, 3, 4, 5)).DoesNotThrow();
				await That(() => sut.Method5(1, 2, 3, 4, 5)).Throws().WithMessage("foo");
				await That(() => sut.Method5(1, 2, 3, 4, 5)).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Returns_WhenFor_ShouldLimitUsage_ToSpecifiedNumber()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo")).When(i => i > 0).For(2)
					.Throws(new Exception("baz"))
					.Throws(new Exception("bar")).For(3).OnlyOnce();

				List<string> values = [];
				for (int i = 0; i < 14; i++)
				{
					try
					{
						sut.Method5(1, 2, 3, 4, 5);
						values.Add("");
					}
					catch (Exception ex)
					{
						values.Add(ex.Message);
					}
				}

				await That(values).IsEqualTo([
					"baz", "bar", "bar", "bar", "foo", "foo", "baz", "foo", "foo", "baz", "foo", "foo", "baz", "foo",
				]);
			}

			[Fact]
			public async Task Throws_Callback_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(() => new Exception("foo"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task Throws_CallbackWithValue_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws((v1, v2, v3, v4, v5) => new Exception($"foo-{v1}-{v2}-{v3}-{v4}-{v5}"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo-1-2-3-4-5");
			}

			[Fact]
			public async Task Throws_Generic_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws<ArgumentNullException>();

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).ThrowsExactly<ArgumentNullException>();
			}

			[Fact]
			public async Task Throws_ShouldThrowException()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();

				sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>())
					.Throws(new Exception("foo"));

				void Act()
				{
					sut.Method5(1, 2, 3, 4, 5);
				}

				await That(Act).Throws().WithMessage("foo");
			}

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnBuilder<int, int, int, int, int> setup =
					(IVoidMethodSetupReturnBuilder<int, int, int, int, int>)sut.Mock.Setup.Method5(
						It.IsAny<int>(),
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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

			[Fact]
			public async Task WithoutCallback_IVoidMethodSetupReturnWhenBuilder_ShouldNotThrow()
			{
				IVoidMethodSetupTest sut = IVoidMethodSetupTest.CreateMock();
				IVoidMethodSetupReturnWhenBuilder<int, int, int, int, int> setup =
					(IVoidMethodSetupReturnWhenBuilder<int, int, int, int, int>)sut.Mock.Setup.Method5(
						It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

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
}
