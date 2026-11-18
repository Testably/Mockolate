using System.Reflection;
using System.Runtime.CompilerServices;
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
	public async Task NullLiteral_InClassicMode_ShouldMatchOnlyNullArguments()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 int One(object? value);
		                                                 int Optional(string? value, int number = 5);
		                                                 int OptionalWithNonNullDefault(string? value = "x", int number = 5);
		                                                 int WithParams(object? value, params int[] rest);
		                                                 int Sibling(object? value);
		                                                 int Sibling<T>(T value);
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut.Mock.Setup.One(null).Returns(1);
		                                                     sut.Mock.Setup.Optional(null).Returns(1);
		                                                     sut.Mock.Setup.OptionalWithNonNullDefault(null).Returns(1);
		                                                     sut.Mock.Setup.WithParams(null).Returns(1);
		                                                     sut.Mock.Setup.Sibling(null!).Returns(1);
		                                                     int[] results = [sut.One("a"), sut.Optional("a"), sut.Optional(null, 6), sut.OptionalWithNonNullDefault(), sut.WithParams("a"), sut.WithParams(null, 1),];
		                                                     string[] beforeNullCalls =
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.One(null).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.Optional(null).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.OptionalWithNonNullDefault(null).AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify.WithParams(null).AtLeastOnce()),
		                                                     ];
		                                                     int[] nullResults = [sut.One(null), sut.Optional(null), sut.OptionalWithNonNullDefault(null), sut.WithParams(null), sut.Sibling((object?)null),];
		                                                     IParameters? nullParameters = null;
		                                                     return
		                                                     [
		                                                         string.Join(", ", results),
		                                                         string.Join(", ", nullResults),
		                                                         ..beforeNullCalls,
		                                                         Capture(() => sut.Mock.Verify.One(null).Once()),
		                                                         Capture(() => sut.Mock.Verify.One(nullParameters).Once()),
		                                                         Capture(() => sut.Mock.Verify.Optional(null).Once()),
		                                                         Capture(() => sut.Mock.Verify.OptionalWithNonNullDefault(null).Once()),
		                                                         Capture(() => sut.Mock.Verify.WithParams(null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Sibling(null!).Once()),
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
			"0, 0, 0, 0, 0, 0",
			"1, 1, 1, 1, 1",
			"Expected that mock invoked method One() at least once, but it never did.",
			"Expected that mock invoked method Optional(, 5) at least once, but it never did.",
			"Expected that mock invoked method OptionalWithNonNullDefault(, 5) at least once, but it never did.",
			"Expected that mock invoked method WithParams(, System.Int32[]) at least once, but it never did.",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
		]).Because("a lone null literal means the call with a null first argument and the declared defaults, also where it binds to the IParameters overload");
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

	[Test]
	public async Task Verify_InClassicMode_WithDynamicMembers_ShouldCompileMatchAndDescribeArguments()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IDyn
		                                             {
		                                                 dynamic Value { get; set; }
		                                                 dynamic this[dynamic key] { get; set; }
		                                                 void Dyn1(dynamic d);
		                                                 void Dyn2(dynamic d, int x);
		                                                 dynamic Dyn4(dynamic a, dynamic b, int c, string d);
		                                                 void Dyn5(dynamic a, int b, int c, int d, dynamic e);
		                                             }

		                                             public abstract class DynBase
		                                             {
		                                                 public virtual dynamic Combine(dynamic a, int b) => a;
		                                                 public virtual dynamic this[dynamic key] => key;
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IDyn sut = IDyn.CreateMock();
		                                                     sut.Mock.Setup.Dyn4(It.IsAny<object>(), 2, It.IsAny<int>(), It.IsAny<string>()).Returns("four");
		                                                     sut.Mock.Setup["k"].Returns("v");
		                                                     sut.Dyn1(42);
		                                                     sut.Dyn2("a", 1);
		                                                     sut.Dyn5("a", 2, 3, 4, 5);
		                                                     object four = sut.Dyn4(1, 2, 3, "d");
		                                                     object indexed = sut["k"];
		                                                     sut.Value = 5;
		                                                     DynBase dynBase = DynBase.CreateMock();
		                                                     object combined = dynBase.Combine("base", 1);
		                                                     object baseIndexed = dynBase["key"];
		                                                     return
		                                                     [
		                                                         four.ToString()!,
		                                                         indexed.ToString()!,
		                                                         combined.ToString()!,
		                                                         baseIndexed.ToString()!,
		                                                         Capture(() => sut.Mock.Verify.Dyn1(42).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dyn2(It.IsAny<object>(), 1).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dyn5(It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<object>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Set(5).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dyn1(43).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dyn2("b", It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dyn5("a", 2, 3, 4, 6).Once()),
		                                                         Capture(() => sut.Mock.Verify["x"].Got().Once()),
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
		                                             """, LanguageVersion.CSharp14, typeof(DynamicAttribute));

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"four",
			"v",
			"base",
			"key",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method Dyn1(43) exactly once, but it never did.",
			"Expected that mock invoked method Dyn2(b, It.IsAny<int>()) exactly once, but it never did.",
			"Expected that mock invoked method Dyn5(a, 2, 3, 4, 6) exactly once, but it never did.",
			"Expected that mock got indexer [x] exactly once, but it never did.",
		]).Because("classic mode must compile dynamic members without dynamic binding and treat them like object");
	}

	[Test]
	public async Task Verify_InClassicMode_WithAnyParameters_ShouldDescribeAnyParameters()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 void P0();
		                                                 void P1(string? value);
		                                                 void P2(string? a, decimal b);
		                                                 void P3(int a, string? b, int c);
		                                                 void P4(int a, int b, int c, int d);
		                                                 void P5(int a, int b, int c, int d, int e);
		                                                 T Generic<T>(T value);
		                                             }

		                                             public delegate int Calculate(int a, int b);

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut.P0();
		                                                     sut.P1("a");
		                                                     sut.P2("Bob", 1m);
		                                                     sut.P3(1, "x", 3);
		                                                     sut.P4(1, 2, 3, 4);
		                                                     sut.P5(1, 2, 3, 4, 5);
		                                                     sut.Generic(1);
		                                                     Calculate calculate = Calculate.CreateMock();
		                                                     calculate(1, 2);
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.P0().AnyParameters().Never()),
		                                                         Capture(() => sut.Mock.Verify.P1("b").AnyParameters().Never()),
		                                                         Capture(() => sut.Mock.Verify.P2("Alice", 4.5m).AnyParameters().Never()),
		                                                         Capture(() => sut.Mock.Verify.P3(9, "y", 9).AnyParameters().Twice()),
		                                                         Capture(() => sut.Mock.Verify.P4(9, 9, 9, 9).AnyParameters().AtLeast(2)),
		                                                         Capture(() => sut.Mock.Verify.P5(9, 9, 9, 9, 9).AnyParameters().Exactly(3)),
		                                                         Capture(() => sut.Mock.Verify.Generic(9).AnyParameters().Never()),
		                                                         Capture(() => calculate.Mock.Verify(9, 9).AnyParameters().Never()),
		                                                         Capture(() => sut.Mock.Verify.P2("Alice", 4.5m).AnyParameters().Within(TimeSpan.FromMilliseconds(10)).Twice()),
		                                                         Capture(() => sut.Mock.Verify.P3(9, "y", 9).AnyParameters().Then(m => m.P2("Alice", 4.5m).AnyParameters())),
		                                                         Capture(() => sut.Mock.Verify.P2("Alice", 4.5m).AnyParameters().Then(m => m.P3(9, "y", 9).AnyParameters())),
		                                                         Capture(() => sut.Mock.Verify.P4(9, 9, 9, 9).Once()),
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
			"Expected that mock never invoked method P0(Match.AnyParameters()), but it did once.",
			"Expected that mock never invoked method P1(Match.AnyParameters()), but it did once.",
			"Expected that mock never invoked method P2(Match.AnyParameters()), but it did once.",
			"Expected that mock invoked method P3(Match.AnyParameters()) exactly twice, but it did once.",
			"Expected that mock invoked method P4(Match.AnyParameters()) at least twice, but it did once.",
			"Expected that mock invoked method P5(Match.AnyParameters()) exactly 3 times, but it did once.",
			"Expected that mock never invoked method Generic<T>(Match.AnyParameters()), but it did once.",
			"Expected that mock never invoked method Invoke(Match.AnyParameters()), but it did once.",
			"Expected that mock invoked method P2(Match.AnyParameters()) exactly twice, but it timed out after 00:00:00.0100000.",
			"Expected that mock invoked method P3(Match.AnyParameters()), then invoked method P2(Match.AnyParameters()) in order, but it invoked method P2(Match.AnyParameters()) too early.",
			"no exception",
			"Expected that mock invoked method P4(9, 9, 9, 9) exactly once, but it never did.",
		]).Because("after AnyParameters the verification counts the calls with any arguments, so the message must not name the original ones, while a verification without it keeps them");
	}
}
