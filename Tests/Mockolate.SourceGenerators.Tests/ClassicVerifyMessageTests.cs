using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ClassicVerifyMessageTests
{
	[Test]
	public async Task FailedVerify_InClassicMode_ShouldDescribeArgumentsAsBefore()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 void P1(string? value);
		                                                 void P3(int a, string? b, int c);
		                                                 void P4(int a, int b, int c, int d);
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.P1((IParameter<string>?)null).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.P1((string?)null).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.P3(1, null, 3).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.P3(It.IsAny<int>(), null, It.IsAny<int>()).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.P4(1, 2, 3, 4).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.P4(It.IsAny<int>(), null, It.IsAny<int>(), It.IsAny<int>()).AtLeastOnce()),
		                                                     ];
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
		                                             }
		                                             """, LanguageVersion.CSharp14);

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"Expected that mock invoked method P1() at least once, but it never did.",
			"Expected that mock invoked method P1() at least once, but it never did.",
			"Expected that mock invoked method P3(1, , 3) at least once, but it never did.",
			"Expected that mock invoked method P3(It.IsAny<int>(), , It.IsAny<int>()) at least once, but it never did.",
			"Expected that mock invoked method P4(1, 2, 3, 4) at least once, but it never did.",
			"Expected that mock invoked method P4(It.IsAny<int>(), , It.IsAny<int>(), It.IsAny<int>()) at least once, but it never did.",
		]).Because("classic mode prints a null literal and a null matcher as an empty string");
	}

	[Test]
	public async Task Verify_InClassicMode_WithMixedLiteralsAndMatchers_ShouldMatchAndDescribeArgumentsAsBefore()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 void P2(string? a, int b);
		                                                 void P3(int a, string? b, int c);
		                                                 void P4(int a, int b, int c, int d);
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut.P2("a", 1);
		                                                     sut.P2(null, 2);
		                                                     sut.P3(1, "x", 3);
		                                                     sut.P4(1, 2, 3, 4);
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.P2("a", It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2(It.IsAny<string>(), 2).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2((string?)null, It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2((IParameter<string>?)null, 2).Once()),
		                                                         Capture(() => sut.Mock.Verify.P3(It.IsAny<int>(), "x", It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.P4(1, It.IsAny<int>(), 3, It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2("b", It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2(It.IsAny<string>(), 3).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2((string?)null, It.Is(1)).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2((IParameter<string>?)null, 1).Once()),
		                                                         Capture(() => sut.Mock.Verify.P3(It.IsAny<int>(), "y", 3).Once()),
		                                                         Capture(() => sut.Mock.Verify.P4(It.IsAny<int>(), 5, It.IsAny<int>(), 4).Once()),
		                                                         Capture(() => sut.Mock.Verify.P2("a", It.IsAny<int>()).Then(m => m.P3(1, It.IsAny<string>(), 3))),
		                                                         Capture(() => sut.Mock.Verify.P3(It.IsAny<int>(), "x", 3).Then(m => m.P2("a", It.IsAny<int>()))),
		                                                         Capture(() => sut.Mock.Verify.P2("b", It.IsAny<int>()).Within(TimeSpan.FromMilliseconds(10)).Once()),
		                                                     ];
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
		                                             }
		                                             """, LanguageVersion.CSharp14);

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method P2(b, It.IsAny<int>()) exactly once, but it never did.",
			"Expected that mock invoked method P2(It.IsAny<string>(), 3) exactly once, but it never did.",
			"Expected that mock invoked method P2(, 1) exactly once, but it never did.",
			"Expected that mock invoked method P2(, 1) exactly once, but it never did.",
			"Expected that mock invoked method P3(It.IsAny<int>(), y, 3) exactly once, but it never did.",
			"Expected that mock invoked method P4(It.IsAny<int>(), 5, It.IsAny<int>(), 4) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method P3(It.IsAny<int>(), x, 3), then invoked method P2(a, It.IsAny<int>()) in order, but it invoked method P2(a, It.IsAny<int>()) too early.",
			"Expected that mock invoked method P2(b, It.IsAny<int>()) exactly once, but it timed out after 00:00:00.0100000.",
		]).Because("the mixed overloads must count and describe their arguments exactly like the classic predicate path did");
	}

	[Test]
	public async Task Verify_InClassicMode_WithMixedLiteralsAndMatchers_ShouldSupportNullCultureCancellationAndUnverifiedTracking()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using System.Globalization;
		                                             using System.Threading;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IBaz
		                                             {
		                                                 void Q2(int a, string? b);
		                                                 void Q3(string? a, int b, string? c);
		                                                 void Q4(int a, string? b, int c, string? d);
		                                                 void Money(decimal amount, double rate, string? note);
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     CultureInfo previousCulture = CultureInfo.CurrentCulture;
		                                                     try
		                                                     {
		                                                         CultureInfo.CurrentCulture = new CultureInfo("de-AT");
		                                                         return RunScenarios();
		                                                     }
		                                                     finally
		                                                     {
		                                                         CultureInfo.CurrentCulture = previousCulture;
		                                                     }
		                                                 }

		                                                 private static string[] RunScenarios()
		                                                 {
		                                                     IBaz sut = IBaz.CreateMock();
		                                                     sut.Q3(null, 1, null);
		                                                     sut.Q3("a", 2, "b");
		                                                     sut.Q4(1, null, 3, "d");
		                                                     sut.Q4(2, "x", 4, null);
		                                                     sut.Money(4.5m, 0.25, "n");
		                                                     using CancellationTokenSource running = new();
		                                                     using CancellationTokenSource expiring = new(TimeSpan.FromMilliseconds(10));

		                                                     IBaz other = IBaz.CreateMock();
		                                                     other.Q2(1, "a");
		                                                     other.Q2(2, "b");

		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.Q3((string?)null, It.IsAny<int>(), (string?)null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q3(It.IsAny<string>(), 1, (string?)null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q4(1, (string?)null, It.IsAny<int>(), It.IsAny<string>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q4(It.IsAny<int>(), "x", 4, (string?)null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q3((string?)null, It.IsAny<int>(), "zz").Once()),
		                                                         Capture(() => sut.Mock.Verify.Q4(It.IsAny<int>(), (string?)null, 9, It.IsAny<string>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Money(4.5m, It.IsAny<double>(), It.IsAny<string>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Money(4.75m, It.IsAny<double>(), "n").Once()),
		                                                         Capture(() => sut.Mock.Verify.Money(It.IsAny<decimal>(), 0.5, It.IsAny<string>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q3("a", It.IsAny<int>(), "b").WithCancellation(running.Token).Once()),
		                                                         Capture(() => sut.Mock.Verify.Q3("never", It.IsAny<int>(), It.IsAny<string>()).WithCancellation(expiring.Token).Once()),
		                                                         Capture(() => other.Mock.Verify.Q2(1, It.IsAny<string>()).Once()),
		                                                         other.Mock.VerifyThatAllInteractionsAreVerified().ToString(),
		                                                         Capture(() => other.Mock.Verify.Q2(It.IsAny<int>(), "b").Once()),
		                                                         other.Mock.VerifyThatAllInteractionsAreVerified().ToString(),
		                                                     ];
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
		                                             }
		                                             """, LanguageVersion.CSharp14);

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method Q3(, It.IsAny<int>(), zz) exactly once, but it never did.",
			"Expected that mock invoked method Q4(It.IsAny<int>(), , 9, It.IsAny<string>()) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method Money(4,75, It.IsAny<double>(), n) exactly once, but it never did.",
			"Expected that mock invoked method Money(It.IsAny<decimal>(), 0,5, It.IsAny<string>()) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method Q3(never, It.IsAny<int>(), It.IsAny<string>()) exactly once, but it timed out.",
			"no exception",
			"False",
			"no exception",
			"True",
		]).Because("the mixed overloads must keep the predicate path's matching, culture-dependent formatting, cancellation and unverified-interaction tracking");
	}
}
