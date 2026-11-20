using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests.MockDelegates;

public partial class DelegateTests
{
	public sealed class ParamsTests
	{
		[Test]
		public async Task ElementMatchers_OnParamsArray_ShouldMatchElementByElement()
		{
			FlagsDelegate sut = FlagsDelegate.CreateMock();
			sut.Mock.Setup(It.IsAny<int>(), It.IsAny<bool>(), It.IsFalse()).Returns(true);

			bool[] results = [sut(5), sut(5, true), sut(5, true, false), sut(5, true, true), sut(5, true, false, true),];

			await That(results).IsEqualTo([false, false, true, false, false,]);
		}

		[Test]
		public async Task ElementMatchers_OnParamsArray_ShouldSupportVerify()
		{
			FlagsDelegate sut = FlagsDelegate.CreateMock();

			_ = sut(5);
			_ = sut(5, true);
			_ = sut(5, true, false);
			_ = sut(5, false, false);
			_ = sut(5, true, false, true);

			await That(sut.Mock.Verify(It.IsAny<int>(), It.IsAny<bool>(), It.IsFalse())).Twice();
			await That(sut.Mock.Verify(It.IsAny<int>(), It.IsTrue())).Once();
			await That(sut.Mock.Verify(It.IsAny<int>())).Once()
				.Because("no element matcher matches only the empty params array");
		}

		[Test]
		public async Task ElementMatchers_OnParamsObjectArray_ShouldMatchElementByElement()
		{
			LogDelegate sut = LogDelegate.CreateMock();
			sut.Mock.Setup(It.Is(4), It.IsAny<object>(), It.Is<object>(2)).Returns(1);

			int[] results = [sut(4, "a", 2), sut(4, "b", 2), sut(4, "a", 3), sut(3, "a", 2), sut(4, "a"),];

			await That(results).IsEqualTo([1, 1, 0, 0, 0,])
				.Because("matchers for the element type must not be captured as elements of the params array");
		}

		[Test]
		public async Task ElementMatchers_OnParamsObjectArray_ShouldSupportVerify()
		{
			LogDelegate sut = LogDelegate.CreateMock();

			_ = sut(4, "a", 2);
			_ = sut(4, "b", 3);

			await That(sut.Mock.Verify(It.Is(4), It.IsAny<object>(), It.Is<object>(2))).Once();
			await That(sut.Mock.Verify(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>())).Twice();
			await That(sut.Mock.Verify(It.IsAny<int>(), It.IsAny<object>())).Never();
		}

		[Test]
		public async Task ElementMatchers_OnLoneParamsObjectArray_ShouldMatchElementByElement()
		{
			OnlyObjectsDelegate sut = OnlyObjectsDelegate.CreateMock();
			sut.Mock.Setup(It.IsAny<object>()).Returns(1);
			sut.Mock.Setup(It.Is<object>("a"), It.IsAny<object>()).Returns(2);

			int[] results = [sut("x"), sut(), sut("a", 2), sut("b", 2),];

			await That(results).IsEqualTo([1, 0, 2, 0,]);
			await That(sut.Mock.Verify(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify(It.IsAny<object>(), It.IsAny<object>())).Twice();
		}

		[Test]
		public async Task ArrayMatcher_OnParamsObjectArray_ShouldMatchTheWholeArray()
		{
			LogDelegate sut = LogDelegate.CreateMock();
			OnlyObjectsDelegate only = OnlyObjectsDelegate.CreateMock();
			sut.Mock.Setup(1, It.IsAny<object[]>()).Returns(1);
			only.Mock.Setup(It.Satisfies<object[]>(args => args.Length == 2)).Returns(2);

			int[] results = [sut(1, "a", 2), sut(1), sut(2, "a"), only("a", 2), only("a"),];

			await That(results).IsEqualTo([1, 1, 0, 2, 0,])
				.Because("a matcher for the array type must not be treated as a matcher for one element");
			await That(sut.Mock.Verify(1, It.IsAny<object[]>())).Twice();
			await That(only.Mock.Verify(It.IsAny<object[]>())).Twice();
		}

		[Test]
		public async Task LiteralElements_ShouldMatchByValue()
		{
			FlagsDelegate flags = FlagsDelegate.CreateMock();
			LogDelegate log = LogDelegate.CreateMock();
			flags.Mock.Setup(It.IsAny<int>(), true, false).Returns(true);
			log.Mock.Setup(1, "a", 2).Returns(1);

			bool[] flagResults = [flags(5, true, false), flags(5, true), flags(5, true, false, true),];
			int[] logResults = [log(1, "a", 2), log(1, "a"), log(1, "a", 3),];

			await That(flagResults).IsEqualTo([true, false, false,]);
			await That(logResults).IsEqualTo([1, 0, 0,]);
			await That(flags.Mock.Verify(It.IsAny<int>(), true, false)).Once();
			await That(log.Mock.Verify(1, "a", 2)).Once();
		}

		[Test]
		public async Task NoParamsArguments_ShouldMatchAnEmptyArray()
		{
			FlagsDelegate flags = FlagsDelegate.CreateMock();
			OnlyObjectsDelegate only = OnlyObjectsDelegate.CreateMock();
			OnlyObjectsDelegate any = OnlyObjectsDelegate.CreateMock();
			flags.Mock.Setup(It.IsAny<int>()).Returns(true);
			only.Mock.Setup().Returns(1);
			any.Mock.Setup().AnyParameters().Returns(2);

			bool[] flagResults = [flags(1), flags(1, true),];
			int[] onlyResults = [only(), only("a"), any(), any("a", 2),];

			await That(flagResults).IsEqualTo([true, false,]);
			await That(onlyResults).IsEqualTo([1, 0, 2, 2,]);
			await That(only.Mock.Verify()).Once();
			await That(only.Mock.Verify().AnyParameters()).Twice();
		}

		[Test]
		public async Task NullLiterals_OnLoneParamsObjectArray_ShouldMeanANullArrayOrNullElements()
		{
			OnlyObjectsDelegate sut = OnlyObjectsDelegate.CreateMock();
			sut.Mock.Setup(null).Returns(1);
			sut.Mock.Setup(null, null).Returns(2);

			int[] results = [sut(null!), sut(null!, null!), sut((object)null!), sut(),];

			await That(results).IsEqualTo([1, 2, 0, 0,]);
			await That(sut.Mock.Verify(null)).Once();
			await That(sut.Mock.Verify(null, null)).Once();
		}

		[Test]
		public async Task ElementMatchers_WithMoreThanFourParameters_ShouldMatchElementByElement()
		{
			FiveDelegate sut = FiveDelegate.CreateMock();
			sut.Mock.Setup(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.Is(5), It.IsAny<int>())
				.Returns(1);

			int[] results = [sut(1, 2, 3, 4, 5, 6), sut(1, 2, 3, 4, 5), sut(1, 2, 3, 4, 6, 6),];

			await That(results).IsEqualTo([1, 0, 0,]);
			await That(sut.Mock.Verify(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
				It.IsAny<int>(), It.IsAny<int>())).Twice();
		}

		[Test]
		public async Task ElementMatchers_OnVoidDelegate_ShouldMatchElementByElement()
		{
			WriteDelegate sut = WriteDelegate.CreateMock();
			int calls = 0;
			sut.Mock.Setup(It.Is("a"), It.IsAny<string>()).Do(() => calls++);

			sut("a", "b");
			sut("a");
			sut("b", "b");

			await That(calls).IsEqualTo(1);
			await That(sut.Mock.Verify(It.IsAny<string>(), It.Is("b"))).Twice();
		}

		[Test]
		public async Task FailedVerify_WithElementMatchers_ShouldListTheElementMatchers()
		{
			LogDelegate sut = LogDelegate.CreateMock();
			_ = sut(4, "a", 3);

			void Act()
			{
				sut.Mock.Verify(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(2)).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Invoke(It.IsAny<int>(), [It.IsAny<object>(), 2]) exactly once, but it never did.");
		}

		[Test]
		public async Task FailedVerify_WithoutParamsArguments_ShouldShowAnEmptyArray()
		{
			OnlyObjectsDelegate sut = OnlyObjectsDelegate.CreateMock();
			_ = sut("a");

			void Act()
			{
				sut.Mock.Verify().Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Invoke([]) exactly once, but it never did.");
		}

		internal delegate bool FlagsDelegate(int level, params bool[] flags);

		internal delegate int LogDelegate(int level, params object[] args);

		internal delegate int OnlyObjectsDelegate(params object[] args);

		internal delegate int FiveDelegate(int a, int b, int c, int d, params int[] values);

		internal delegate void WriteDelegate(params string[] values);
	}
}
