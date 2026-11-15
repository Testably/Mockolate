using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Verify;

namespace Mockolate.Tests;

public sealed class UnionVerifyMessageTests
{
	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task FailedVerify_ShouldDescribeEverySlotKind(bool invokedBefore)
	{
		IUnionMessageService sut = IUnionMessageService.CreateMock();
		if (invokedBefore)
		{
			sut.One("zz");
			sut.Two(-9, "zz");
			sut.Three(-9, "zz", _ => false);
			sut.Four(-9, "zz", -9, -9);
			sut.Register(_ => false);
			sut.WithDefault(-9, "zz");
			sut.Keywords(-9, "zz");
			sut.Clash(-9, "zz", "zz");
			sut.Fixed("zz", out _);
			sut.Many(-9, -9, -9, -9, "zz");
		}

		Func<string?, bool> isB = s => s == "b";

		string[] messages =
		[
			Capture(() => sut.Mock.Verify.One("a").AtLeastOnce()),
			Capture(() => sut.Mock.Verify.One(null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.One(It.Is<string?>("b")).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.One((IParameter<string?>)null!).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.One(s => s == "b").AtLeastOnce()),
			Capture(() => sut.Mock.Verify.One(isB).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Two(1, null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Two(It.IsAny<int>(), null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Two(x => x > 1, "a").AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Three(1, null, x => x > 0).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Three(It.Is(1), (IParameter<string?>)null!, x => x > 0).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Four(1, null, 3, null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Four(x => x > 0, It.IsAny<string?>(), 3, null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Register(x => x > 0).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Register(null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.WithDefault(1).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.WithDefault(It.IsAny<int>(), null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.WithDefault(x => x > 0).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Keywords(1, null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Keywords(p => p > 0, v => v == null).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Clash(1, "a", "b").AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Clash(v => v > 0, "a", It.IsAny<string?>()).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Fixed(null, It.IsOut<int>()).AtLeastOnce()),
			Capture(() => sut.Mock.Verify.Many(1, 2, 3, 4, null).AtLeastOnce()),
		];

		await That(messages).IsEqualTo([
			"Expected that mock invoked method One(a) at least once, but it never did.",
			"Expected that mock invoked method One(null) at least once, but it never did.",
			"Expected that mock invoked method One(\"b\") at least once, but it never did.",
			"Expected that mock invoked method One(null) at least once, but it never did.",
			"Expected that mock invoked method One(s => s == \"b\") at least once, but it never did.",
			"Expected that mock invoked method One(isB) at least once, but it never did.",
			"Expected that mock invoked method Two(1, null) at least once, but it never did.",
			"Expected that mock invoked method Two(It.IsAny<int>(), null) at least once, but it never did.",
			"Expected that mock invoked method Two(x => x > 1, a) at least once, but it never did.",
			"Expected that mock invoked method Three(1, null, System.Func`2[System.Int32,System.Boolean]) at least once, but it never did.",
			"Expected that mock invoked method Three(1, null, System.Func`2[System.Int32,System.Boolean]) at least once, but it never did.",
			"Expected that mock invoked method Four(1, null, 3, null) at least once, but it never did.",
			"Expected that mock invoked method Four(x => x > 0, It.IsAny<string>(), 3, null) at least once, but it never did.",
			"Expected that mock invoked method Register(System.Func`2[System.Int32,System.Boolean]) at least once, but it never did.",
			"Expected that mock invoked method Register(null) at least once, but it never did.",
			"Expected that mock invoked method WithDefault(1, x) at least once, but it never did.",
			"Expected that mock invoked method WithDefault(It.IsAny<int>(), null) at least once, but it never did.",
			"Expected that mock invoked method WithDefault(x => x > 0, x) at least once, but it never did.",
			"Expected that mock invoked method Keywords(1, null) at least once, but it never did.",
			"Expected that mock invoked method Keywords(p => p > 0, v => v == null) at least once, but it never did.",
			"Expected that mock invoked method Clash(1, a, b) at least once, but it never did.",
			"Expected that mock invoked method Clash(v => v > 0, a, It.IsAny<string>()) at least once, but it never did.",
			"Expected that mock invoked method Fixed(null, It.IsOut<int>()) at least once, but it never did.",
			"Expected that mock invoked method Many(1, 2, 3, 4, null) at least once, but it never did.",
		]).Because("union mode prints a null literal and a null matcher as null and a predicate as its caller expression");
	}

	[Test]
	public async Task FailedNeverVerify_ShouldDescribeTheUnionArguments()
	{
		IUnionMessageService sut = IUnionMessageService.CreateMock();
		sut.Two(1, null);

		void ActLiteral()
			=> sut.Mock.Verify.Two(1, null).Never();

		void ActPredicate()
			=> sut.Mock.Verify.Two(x => x > 0, It.IsAny<string?>()).Never();

		await That(ActLiteral).Throws<MockVerificationException>()
			.WithMessage("Expected that mock never invoked method Two(1, null), but it did once.");
		await That(ActPredicate).Throws<MockVerificationException>()
			.WithMessage("Expected that mock never invoked method Two(x => x > 0, It.IsAny<string>()), but it did once.");
	}

	[Test]
	public async Task FailedThenVerify_WithMixedLiteralAndMatcher_ShouldDescribeTheUnionArguments()
	{
		IUnionMessageService sut = IUnionMessageService.CreateMock();
		sut.Two(1, "a");
		sut.Two(2, "b");

		void Act()
			=> sut.Mock.Verify.Two(2, It.IsAny<string?>())
				.Then(m => m.Two(1, It.IsAny<string?>()));

		await That(Act).Throws<MockVerificationException>()
			.WithMessage(
				"Expected that mock invoked method Two(2, It.IsAny<string>()), then invoked method Two(1, It.IsAny<string>()) in order, but it invoked method Two(1, It.IsAny<string>()) too early.");
	}

	private static string Capture(Action act)
	{
		try
		{
			act();
			return "no exception";
		}
		catch (Exception exception)
		{
			return exception.Message;
		}
	}

	public interface IUnionMessageService
	{
		void One(string? text);
		void Two(int number, string? text);
		void Three(int a, string? b, Func<int, bool> c);
		void Four(int a, string? b, int c, int? d);
		void Register(Func<int, bool>? callback);
		void WithDefault(int number, string? text = "x");
		void Keywords(int @params, string? @void);
		void Clash(int value, string? valueArg, string? valueExpression);
		void Fixed(string? key, out int result);
		void Many(int a, int b, int c, int d, string? e);
	}
}
