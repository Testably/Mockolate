using aweXpect.Chronology;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Tests.MockMethods;

public sealed partial class VerifyInvokedTests
{
	/// <summary>
	///     Regression coverage for mixed literal and matcher verifications, in union mode on net11.0 and with the
	///     classic overloads on the other target frameworks.
	/// </summary>
	public sealed class MixedLiteralAndMatcherUnionModeTests
	{
		[Test]
		public async Task With2Parameters_ShouldCountMatchingInvocations()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			sut.Method2("a", 1);
			sut.Method2("a", 2);
			sut.Method2("b", 1);

			await That(sut.Mock.Verify.Method2("a", It.IsAny<int>())).Twice();
			await That(sut.Mock.Verify.Method2(It.IsAny<string>(), 1)).Twice();
			await That(sut.Mock.Verify.Method2("b", It.Is(2))).Never();
			await That(sut.Mock.Verify.Method2(It.Is("b"), 2)).Never();
		}

		[Test]
		public async Task With3Parameters_ShouldCountMatchingInvocations()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			sut.Method3("a", 1, "x");
			sut.Method3("a", 2, "y");
			sut.Method3("b", 1, "x");

			await That(sut.Mock.Verify.Method3("a", It.IsAny<int>(), It.IsAny<string>())).Twice();
			await That(sut.Mock.Verify.Method3(It.IsAny<string>(), 1, "x")).Twice();
			await That(sut.Mock.Verify.Method3("a", It.IsAny<int>(), "y")).Once();
			await That(sut.Mock.Verify.Method3(It.IsAny<string>(), 2, It.Is("x"))).Never();
		}

		[Test]
		public async Task With4Parameters_ShouldCountMatchingInvocations()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			sut.Method4("a", 1, "x", 10);
			sut.Method4("a", 2, "y", 10);
			sut.Method4("b", 1, "x", 20);

			await That(sut.Mock.Verify.Method4("a", It.IsAny<int>(), It.IsAny<string>(), 10)).Twice();
			await That(sut.Mock.Verify.Method4(It.IsAny<string>(), 1, It.IsAny<string>(), It.IsAny<int>())).Twice();
			await That(sut.Mock.Verify.Method4(It.IsAny<string>(), It.IsAny<int>(), "x", 20)).Once();
			await That(sut.Mock.Verify.Method4("b", It.IsAny<int>(), "y", It.IsAny<int>())).Never();
		}

		[Test]
		public async Task WithLiteralFirst_WhenFailing_ShouldDescribeArguments()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();
			sut.Method2("a", 1);

			void Act2()
			{
				sut.Mock.Verify.Method2("b", It.IsAny<int>()).Once();
			}

			void Act3()
			{
				sut.Mock.Verify.Method3("b", It.IsAny<int>(), It.IsAny<string>()).Once();
			}

			void Act4()
			{
				sut.Mock.Verify.Method4("b", It.IsAny<int>(), "c", It.IsAny<int>()).Once();
			}

			await That(Act2).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method2(b, It.IsAny<int>()) exactly once, but it never did.");
			await That(Act3).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method3(b, It.IsAny<int>(), It.IsAny<string>()) exactly once, but it never did.");
			await That(Act4).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method4(b, It.IsAny<int>(), c, It.IsAny<int>()) exactly once, but it never did.");
		}

		[Test]
		public async Task WithMatcherFirst_WhenFailing_ShouldDescribeArguments()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();
			sut.Method2("a", 1);

			void Act2()
			{
				sut.Mock.Verify.Method2(It.IsAny<string>(), 2).Once();
			}

			void Act3()
			{
				sut.Mock.Verify.Method3(It.IsAny<string>(), 2, "c").Once();
			}

			void Act4()
			{
				sut.Mock.Verify.Method4(It.IsAny<string>(), 2, It.IsAny<string>(), 3).Once();
			}

			await That(Act2).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method2(It.IsAny<string>(), 2) exactly once, but it never did.");
			await That(Act3).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method3(It.IsAny<string>(), 2, c) exactly once, but it never did.");
			await That(Act4).Throws<MockVerificationException>()
				.WithMessage("Expected that mock invoked method Method4(It.IsAny<string>(), 2, It.IsAny<string>(), 3) exactly once, but it never did.");
		}

		[Test]
		public async Task WithNullLiteral_ShouldMatchNullArguments()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			sut.Method2(null, 1);
			sut.Method3("a", 1, null);
			sut.Method4(null, 1, null, 2);

			await That(sut.Mock.Verify.Method2((string?)null, It.IsAny<int>())).Once();
			await That(sut.Mock.Verify.Method3(It.IsAny<string>(), 1, (string?)null)).Once();
			await That(sut.Mock.Verify.Method4((string?)null, It.IsAny<int>(), (string?)null, 2)).Once();
			await That(sut.Mock.Verify.Method3((string?)null, It.IsAny<int>(), It.IsAny<string>())).Never();
		}

		[Test]
		public async Task WithNullLiteral_WhenFailing_ShouldDescribeArguments()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			void Act()
			{
				sut.Mock.Verify.Method3((string?)null, It.IsAny<int>(), (string?)null).Once();
			}

			await That(Act).Throws<MockVerificationException>()
#if NET11_0_OR_GREATER
				.WithMessage("Expected that mock invoked method Method3(null, It.IsAny<int>(), null) exactly once, but it never did.");
#else
				.WithMessage("Expected that mock invoked method Method3(, It.IsAny<int>(), ) exactly once, but it never did.");
#endif
		}

		[Test]
		public async Task WithNullMatcher_ShouldMatchNullArguments()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();

			sut.Method2(null, 1);
			sut.Method2("a", 1);
			sut.Method3("a", 1, null);
			sut.Method3("a", 1, "x");

			await That(sut.Mock.Verify.Method2((IParameter<string?>)null!, 1)).Once();
			await That(sut.Mock.Verify.Method3("a", 1, (IParameter<string?>)null!)).Once();
		}

		[Test]
		public async Task Then_ShouldVerifyInOrder()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();
			sut.Method2("a", 1);
			sut.Method3("b", 2, "x");
			sut.Method4("c", 3, "y", 4);

			sut.Mock.Verify.Method2("a", It.IsAny<int>())
				.Then(m => m.Method3(It.IsAny<string>(), 2, "x"), m => m.Method4("c", It.IsAny<int>(), It.IsAny<string>(), 4));

			void Act()
			{
				sut.Mock.Verify.Method4(It.IsAny<string>(), 3, "y", It.IsAny<int>())
					.Then(m => m.Method2("a", It.IsAny<int>()));
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Method4(It.IsAny<string>(), 3, y, It.IsAny<int>()), then invoked method Method2(a, It.IsAny<int>()) in order, but it invoked method Method2(a, It.IsAny<int>()) too early.");
		}

		[Test]
		public async Task Within_ShouldWaitForMatchingInvocation()
		{
			IMixedVerifyService sut = IMixedVerifyService.CreateMock();
			_ = Task.Run(async () =>
			{
				await Task.Delay(50.Milliseconds());
				sut.Method3("a", 1, "x");
			});

			sut.Mock.Verify.Method3("a", It.IsAny<int>(), "x").Within(30.Seconds()).Once();

			void Act()
			{
				sut.Mock.Verify.Method2(It.IsAny<string>(), 1).Within(20.Milliseconds()).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage(
					"Expected that mock invoked method Method2(It.IsAny<string>(), 1) exactly once, but it timed out after 00:00:00.0200000.");
		}

		public interface IMixedVerifyService
		{
			void Method2(string? a, int b);
			void Method3(string? a, int b, string? c);
			void Method4(string? a, int b, string? c, int d);
		}
	}
}
