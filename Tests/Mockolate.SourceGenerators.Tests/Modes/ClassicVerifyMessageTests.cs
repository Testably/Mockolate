using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.Modes;

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
		                                                 int this[string? key] { get; }
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify[(string?)null].Got().AtLeastOnce()),
		                                                         Capture(() => sut.Mock.Verify[(IParameter<string>?)null].Got().AtLeastOnce()),
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
			"Expected that mock got indexer [null] at least once, but it never did.",
			"Expected that mock got indexer [null] at least once, but it never did.",
			"Expected that mock invoked method P1(null) at least once, but it never did.",
			"Expected that mock invoked method P1(null) at least once, but it never did.",
			"Expected that mock invoked method P3(1, null, 3) at least once, but it never did.",
			"Expected that mock invoked method P3(It.IsAny<int>(), null, It.IsAny<int>()) at least once, but it never did.",
			"Expected that mock invoked method P4(1, 2, 3, 4) at least once, but it never did.",
			"Expected that mock invoked method P4(It.IsAny<int>(), null, It.IsAny<int>(), It.IsAny<int>()) at least once, but it never did.",
		]).Because("classic mode prints a null literal and a null matcher as null, like union mode does");
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
			"Expected that mock invoked method One(null) at least once, but it never did.",
			"Expected that mock invoked method Optional(null, 5) at least once, but it never did.",
			"Expected that mock invoked method OptionalWithNonNullDefault(null, 5) at least once, but it never did.",
			"Expected that mock invoked method WithParams(null, []) at least once, but it never did.",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
		]).Because("a lone null literal means the call with a null first argument and the declared defaults, also where it binds to the IParameters overload");
	}

	[Test]
	public async Task OptionalParameter_InClassicMode_WhenOmittedOrNullMatcher_ShouldVerifyLikeSetup()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public enum Flavor { Dark, Milk, White, }

		                                             public interface IOpt
		                                             {
		                                                 int Opt(string? s, int number = 5);
		                                                 int Many(int a, string? text = "x", Flavor flavor = Flavor.Milk, decimal price = 2m);
		                                                 int Defaults(int a, string? text = null, TimeSpan time = default, int? count = null, DateTime date = default);
		                                                 int Nullable(int a, int? count = 7);
		                                                 int Generic<T>(T value, int number = 5);
		                                                 int Five(int a, int b, int c, int d, int e = 5);
		                                                 int GenericClass<T>(int a, T value = null!) where T : class;
		                                                 int GenericStruct<T>(int a, T value = default, T? other = null) where T : struct;
		                                             }

		                                             public class Base
		                                             {
		                                                 public virtual int Virtual(int a, string text = "x", object? o = null, in int number = 5) => 0;
		                                             }

		                                             public delegate int Del(string? s, int number = 5);

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IOpt sut = IOpt.CreateMock();
		                                                     Del del = Del.CreateMock();
		                                                     sut.Mock.Setup.Opt(It.IsAny<string?>()).Returns(1);
		                                                     sut.Mock.Setup.Many(It.IsAny<int>()).Returns(1);
		                                                     sut.Mock.Setup.Defaults(It.IsAny<int>()).Returns(1);
		                                                     sut.Mock.Setup.Nullable(It.IsAny<int>()).Returns(1);
		                                                     sut.Mock.Setup.Generic<string>(It.IsAny<string>()).Returns(1);
		                                                     sut.Mock.Setup.Five(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(1);
		                                                     del.Mock.Setup(It.IsAny<string?>()).Returns(1);
		                                                     int[] results =
		                                                     [
		                                                         sut.Opt("a"), sut.Opt("b", 0),
		                                                         sut.Many(1), sut.Many(2, null, Flavor.Dark, 0m),
		                                                         sut.Defaults(1), sut.Defaults(2, "t", TimeSpan.FromSeconds(1), 3, new DateTime(2000, 1, 1)),
		                                                         sut.Nullable(1), sut.Nullable(2, null),
		                                                         sut.Generic("g"), sut.Generic("h", 0),
		                                                         sut.Five(1, 2, 3, 4), sut.Five(1, 2, 3, 4, 0),
		                                                         del("a"), del("b", 0),
		                                                     ];
		                                                     Base baseSut = Base.CreateMock();
		                                                     sut.GenericClass<string>(1);
		                                                     sut.GenericClass<string>(1, "c");
		                                                     sut.GenericStruct<int>(1, 2, 3);
		                                                     sut.GenericStruct<int>(1);
		                                                     baseSut.Virtual(1);
		                                                     baseSut.Virtual(2, "y", 1, 6);
		                                                     return
		                                                     [
		                                                         string.Join(", ", results),
		                                                         Capture(() => sut.Mock.Verify.Opt(It.IsAny<string?>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Opt(It.IsAny<string?>(), null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Opt("a").Once()),
		                                                         Capture(() => sut.Mock.Verify.Opt("b").Never()),
		                                                         Capture(() => sut.Mock.Verify.Many(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Many(It.IsAny<int>(), null, null, null).Once()),
		                                                         Capture(() => sut.Mock.Verify.Many(1).Once()),
		                                                         Capture(() => sut.Mock.Verify.Many(It.IsAny<int>(), It.IsAny<string?>(), Flavor.Dark).Never()),
		                                                         Capture(() => sut.Mock.Verify.Defaults(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Nullable(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Nullable(1).Once()),
		                                                         Capture(() => sut.Mock.Verify.Generic<string>(It.IsAny<string>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Generic<string>("g").Once()),
		                                                         Capture(() => sut.Mock.Verify.Five(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Five(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null).Once()),
		                                                         Capture(() => del.Mock.Verify(It.IsAny<string?>()).Once()),
		                                                         Capture(() => del.Mock.Verify("a").Once()),
		                                                         Capture(() => sut.Mock.Verify.GenericClass<string>(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.GenericStruct<int>(It.IsAny<int>()).Once()),
		                                                         Capture(() => baseSut.Mock.Verify.Virtual(It.IsAny<int>()).Once()),
		                                                         Capture(() => baseSut.Mock.Verify.Virtual(1).Once()),
		                                                         Capture(() => sut.Mock.Verify.Opt(It.IsAny<string?>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Opt("a").Never()),
		                                                         Capture(() => sut.Mock.Verify.Many(It.IsAny<int>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Many(1, It.IsAny<string?>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Defaults(It.IsAny<int>(), null, null, null, It.IsAny<DateTime>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Nullable(It.IsAny<int>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Generic<string>(It.IsAny<string>()).Never()),
		                                                         Capture(() => sut.Mock.Verify.Five(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Never()),
		                                                         Capture(() => del.Mock.Verify(It.IsAny<string?>()).Never()),
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
			"1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock never invoked method Opt(It.IsAny<string>(), 5), but it did once.",
			"Expected that mock never invoked method Opt(\"a\", 5), but it did once.",
			"Expected that mock never invoked method Many(It.IsAny<int>(), \"x\", Milk, 2), but it did once.",
			"Expected that mock never invoked method Many(1, It.IsAny<string>(), Milk, 2), but it did once.",
			"Expected that mock never invoked method Defaults(It.IsAny<int>(), null, 00:00:00, null, It.IsAny<DateTime>()), but it did once.",
			"Expected that mock never invoked method Nullable(It.IsAny<int>(), 7), but it did once.",
			"Expected that mock never invoked method Generic<T>(It.IsAny<string>(), 5), but it did once.",
			"Expected that mock never invoked method Five(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 5), but it did once.",
			"Expected that mock never invoked method Invoke(It.IsAny<string>(), 5), but it did once.",
		]).Because("an omitted optional parameter stands for the declared default value in setup and verify, and so does a null matcher where several optional parameters keep their = null defaults");
	}

	[Test]
	public async Task SeveralOptionalParameters_InClassicMode_WhenSkippedByName_ShouldUseTheDeclaredDefault()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 int Several(int key, int count = 5, string? name = "x");
		                                             }

		                                             public delegate int Del(int key, int count = 5, string? name = "x");

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     Del del = Del.CreateMock();
		                                                     sut.Mock.Setup.Several(key: It.IsAny<int>(), name: It.IsAny<string?>()).Returns(1);
		                                                     del.Mock.Setup(key: It.IsAny<int>(), name: It.IsAny<string?>()).Returns(1);
		                                                     int[] results = [sut.Several(1, name: "y"), sut.Several(1, 0, "y"), del(1, name: "y"), del(1, 0, "y"),];
		                                                     return
		                                                     [
		                                                         string.Join(", ", results),
		                                                         Capture(() => sut.Mock.Verify.Several(key: It.IsAny<int>(), name: It.IsAny<string?>()).Once()),
		                                                         Capture(() => del.Mock.Verify(key: It.IsAny<int>(), name: It.IsAny<string?>()).Once()),
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
			"1, 0, 1, 0",
			"no exception",
			"no exception",
		]).Because("with several optional parameters a call can skip an earlier one by name, which keeps the = null defaults");
	}

	[Test]
	public async Task NullLiteral_InClassicMode_OnDelegates_ShouldMatchOnlyNullArguments()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Parameters;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public delegate int Two(string? value, int number);
		                                             public delegate int Optional(object? value, int number = 5);
		                                             public delegate int Five(string? a, int b, int c, int d, string? e);
		                                             public delegate int TwoObjects(object? a, object? b);

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     Func<object?, int> one = Func<object?, int>.CreateMock();
		                                                     Func<string?, int> oneString = Func<string?, int>.CreateMock();
		                                                     Action<int?> oneNullableInt = Action<int?>.CreateMock();
		                                                     Two two = Two.CreateMock();
		                                                     Optional optional = Optional.CreateMock();
		                                                     Five five = Five.CreateMock();
		                                                     TwoObjects twoObjects = TwoObjects.CreateMock();
		                                                     Action<int> oneInt = Action<int>.CreateMock();
		                                                     one.Mock.Setup(null).Returns(1);
		                                                     oneString.Mock.Setup(null).Returns(1);
		                                                     two.Mock.Setup(null, 1).Returns(1);
		                                                     optional.Mock.Setup(null).Returns(1);
		                                                     five.Mock.Setup(null, 1, 2, 3, null).Returns(1);
		                                                     twoObjects.Mock.Setup(null, null).Returns(1);
		                                                     oneNullableInt(1);
		                                                     oneInt(1);
		                                                     int[] results = [one("a"), oneString("a"), two("a", 1), two(null, 2), optional("a"), optional(null, 6), five("a", 1, 2, 3, null), twoObjects("a", null),];
		                                                     string[] beforeNullCalls =
		                                                     [
		                                                         Capture(() => one.Mock.Verify(null).AtLeastOnce()),
		                                                         Capture(() => oneString.Mock.Verify(null).AtLeastOnce()),
		                                                         Capture(() => oneNullableInt.Mock.Verify(null).AtLeastOnce()),
		                                                         Capture(() => two.Mock.Verify(null, 1).AtLeastOnce()),
		                                                         Capture(() => optional.Mock.Verify(null).AtLeastOnce()),
		                                                         Capture(() => five.Mock.Verify(null, 1, 2, 3, null).AtLeastOnce()),
		                                                         Capture(() => twoObjects.Mock.Verify(null, null).AtLeastOnce()),
		                                                     ];
		                                                     oneNullableInt(null);
		                                                     int[] nullResults = [one(null), oneString(null), two(null, 1), optional(null), five(null, 1, 2, 3, null), twoObjects(null, null),];
		                                                     return
		                                                     [
		                                                         string.Join(", ", results),
		                                                         string.Join(", ", nullResults),
		                                                         ..beforeNullCalls,
		                                                         Capture(() => one.Mock.Verify(null).Once()),
		                                                         Capture(() => oneString.Mock.Verify(null).Once()),
		                                                         Capture(() => oneString.Mock.Verify(default).Once()),
		                                                         Capture(() => oneNullableInt.Mock.Verify(null).Once()),
		                                                         Capture(() => two.Mock.Verify(null, 1).Once()),
		                                                         Capture(() => optional.Mock.Verify(null).Once()),
		                                                         Capture(() => five.Mock.Verify(null, 1, 2, 3, null).Once()),
		                                                         Capture(() => twoObjects.Mock.Verify(null, null).Once()),
		                                                         Capture(() => oneInt.Mock.Verify(null).Once()),
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
			"0, 0, 0, 0, 0, 0, 0, 0",
			"1, 1, 1, 1, 1, 1",
			"Expected that mock invoked method Invoke(null) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null, 1) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null, 5) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null, 1, 2, 3, null) at least once, but it never did.",
			"Expected that mock invoked method Invoke(null, null) at least once, but it never did.",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
		]).Because("a null literal on a delegate means that the argument was null, as for a method, and a lone null for a non-nullable value type keeps matching any call");
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
			"Expected that mock invoked method P2(\"b\", It.IsAny<int>()) exactly once, but it never did.",
			"Expected that mock invoked method P2(It.IsAny<string>(), 3) exactly once, but it never did.",
			"Expected that mock invoked method P2(null, 1) exactly once, but it never did.",
			"Expected that mock invoked method P2(null, 1) exactly once, but it never did.",
			"Expected that mock invoked method P3(It.IsAny<int>(), \"y\", 3) exactly once, but it never did.",
			"Expected that mock invoked method P4(It.IsAny<int>(), 5, It.IsAny<int>(), 4) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method P3(It.IsAny<int>(), \"x\", 3), then invoked method P2(\"a\", It.IsAny<int>()) in order, but it invoked method P2(\"a\", It.IsAny<int>()) too early.",
			"Expected that mock invoked method P2(\"b\", It.IsAny<int>()) exactly once, but it timed out after 00:00:00.0100000.",
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
			"Expected that mock invoked method Q3(null, It.IsAny<int>(), \"zz\") exactly once, but it never did.",
			"Expected that mock invoked method Q4(It.IsAny<int>(), null, 9, It.IsAny<string>()) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method Money(4.75, It.IsAny<double>(), \"n\") exactly once, but it never did.",
			"Expected that mock invoked method Money(It.IsAny<decimal>(), 0.5, It.IsAny<string>()) exactly once, but it never did.",
			"no exception",
			"Expected that mock invoked method Q3(\"never\", It.IsAny<int>(), It.IsAny<string>()) exactly once, but it timed out.",
			"no exception",
			"False",
			"no exception",
			"True",
		]).Because("the mixed overloads must keep the predicate path's matching, cancellation and unverified-interaction tracking, and format the arguments independent of the current culture");
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
			"Expected that mock invoked method Dyn2(\"b\", It.IsAny<int>()) exactly once, but it never did.",
			"Expected that mock invoked method Dyn5(\"a\", 2, 3, 4, 6) exactly once, but it never did.",
			"Expected that mock got indexer [\"x\"] exactly once, but it never did.",
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

	[Test]
	public async Task VerifySet_InClassicMode_WithMatcherForObjectValue_ShouldUseTheMatcher()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 object Value { get; set; }
		                                                 dynamic Dynamic { get; set; }
		                                                 object WriteOnly { set; }
		                                                 object this[int key] { get; set; }
		                                                 object this[int key1, int key2, int key3, int key4, int key5] { get; set; }
		                                             }

		                                             public interface IWriteOnly
		                                             {
		                                                 object this[int key] { set; }
		                                                 object this[int key1, int key2, int key3, int key4, int key5] { set; }
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut.Value = 5;
		                                                     sut.Dynamic = 5;
		                                                     sut.WriteOnly = 5;
		                                                     sut[1] = 5;
		                                                     sut[1, 2, 3, 4, 5] = 5;
		                                                     IWriteOnly writeOnly = IWriteOnly.CreateMock();
		                                                     writeOnly[1] = 5;
		                                                     writeOnly[1, 2, 3, 4, 5] = 5;
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify.Value.Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Set(It.Is<object>(5)).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Set(5).Once()),
		                                                         Capture(() => sut.Mock.Verify.Dynamic.Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.WriteOnly.Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => sut.Mock.Verify[1].Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => sut.Mock.Verify[1].Set(It.Is<object>(5)).Once()),
		                                                         Capture(() => sut.Mock.Verify[1].Set(5).Once()),
		                                                         Capture(() => sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1].Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<object>()).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(5)).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1, 2, 3, 4, 5].Set(5).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Set(It.Is<object>(6)).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(6)).Once()),
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
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock set property Value to 6 exactly once, but it never did.",
			"Expected that mock set indexer [1, 2, 3, 4, 5] to 6 exactly once, but it never did.",
		]).Because("a matcher also converts to object, but must not be compared as a value");
	}

	[Test]
	public async Task VerifySet_InClassicMode_WithMatcherForDerivedType_ShouldUseTheMatcher()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public class BaseValue;

		                                             public class DerivedValue : BaseValue;

		                                             public interface IFoo
		                                             {
		                                                 BaseValue this[int key] { get; set; }
		                                                 BaseValue this[int key1, int key2, int key3, int key4, int key5] { get; set; }
		                                             }

		                                             public interface IWriteOnly
		                                             {
		                                                 BaseValue this[int key] { set; }
		                                                 BaseValue this[int key1, int key2, int key3, int key4, int key5] { set; }
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut[1] = new BaseValue();
		                                                     sut[1] = new DerivedValue();
		                                                     sut[1, 2, 3, 4, 5] = new BaseValue();
		                                                     sut[1, 2, 3, 4, 5] = new DerivedValue();
		                                                     IWriteOnly writeOnly = IWriteOnly.CreateMock();
		                                                     writeOnly[1] = new BaseValue();
		                                                     writeOnly[1] = new DerivedValue();
		                                                     writeOnly[1, 2, 3, 4, 5] = new BaseValue();
		                                                     writeOnly[1, 2, 3, 4, 5] = new DerivedValue();
		                                                     return
		                                                     [
		                                                         Capture(() => sut.Mock.Verify[1].Set(It.IsAny<DerivedValue>()).Once()),
		                                                         Capture(() => sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<DerivedValue>()).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1].Set(It.IsAny<DerivedValue>()).Once()),
		                                                         Capture(() => writeOnly.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<DerivedValue>()).Once()),
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
		]).Because("a matcher for a derived type is a matcher for the value type, although it only implements the typed match for the derived type");
	}

	[Test]
	public async Task Verify_InClassicMode_WithTypesClosedOverDynamic_ShouldCompileMatchAndDescribeArguments()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using System.Collections.Generic;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IRepo<T>
		                                             {
		                                                 T Value { get; set; }
		                                                 T this[T key] { get; }
		                                                 event EventHandler<T>? Changed;
		                                                 T Get(T key, List<T> values);
		                                                 void Add(T item);
		                                             }

		                                             public abstract class RepoBase<T>
		                                             {
		                                                 public virtual T Echo(T value) => value;
		                                             }

		                                             public class DerivedRepo : RepoBase<dynamic>
		                                             {
		                                             }

		                                             public class Repo<T> : IRepo<T>
		                                             {
		                                                 public virtual T Value { get; set; } = default!;
		                                                 public virtual T this[T key] => key;
		                                                 public virtual event EventHandler<T>? Changed;
		                                                 public virtual T Get(T key, List<T> values) => key;
		                                                 public virtual void Add(T item) => Changed?.Invoke(this, item);
		                                             }

		                                             public delegate T Factory<T>(T value);

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IRepo<dynamic> sut = IRepo<dynamic>.CreateMock();
		                                                     IRepo<object> objectSut = IRepo<object>.CreateMock();
		                                                     sut.Mock.Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("value");
		                                                     sut.Mock.Setup["k"].Returns("indexed");
		                                                     object? raised = null;
		                                                     sut.Changed += (_, e) => raised = e;
		                                                     object value = sut.Get("key", new List<dynamic>());
		                                                     object indexed = sut["k"];
		                                                     sut.Value = 5;
		                                                     sut.Add(42);
		                                                     sut.Mock.Raise.Changed(null, "event");

		                                                     IRepo<List<dynamic>> nested = IRepo<List<dynamic>>.CreateMock();
		                                                     nested.Add(new List<dynamic>());
		                                                     RepoBase<dynamic> repoBase = RepoBase<dynamic>.CreateMock();
		                                                     object echoed = repoBase.Echo("base");
		                                                     DerivedRepo derived = DerivedRepo.CreateMock();
		                                                     object derivedEchoed = derived.Echo("derived");
		                                                     Repo<dynamic> repo = Repo<dynamic>.CreateMock();
		                                                     object repoValue = repo.Get("repo", new List<dynamic>());
		                                                     Factory<dynamic> factory = Factory<dynamic>.CreateMock();
		                                                     factory.Mock.Setup("a").Returns("created");
		                                                     object created = factory("a");
		                                                     IRepo<int> combined = IRepo<int>.CreateMock().Implementing<IRepo<dynamic>>();
		                                                     ((IRepo<dynamic>)combined).Add("combined");
		                                                     IRepo<int> objectCombined = IRepo<int>.CreateMock().Implementing<IRepo<object>>();
		                                                     return
		                                                     [
		                                                         value.ToString()!,
		                                                         indexed.ToString()!,
		                                                         raised!.ToString()!,
		                                                         echoed.ToString()!,
		                                                         derivedEchoed.ToString()!,
		                                                         repoValue.ToString()!,
		                                                         created.ToString()!,
		                                                         (sut.GetType() == objectSut.GetType()).ToString(),
		                                                         (combined.GetType() == objectCombined.GetType()).ToString(),
		                                                         Capture(() => sut.Mock.Verify.Get("key", It.IsAny<List<dynamic>>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Set(5).Once()),
		                                                         Capture(() => sut.Mock.Verify.Add(42).Once()),
		                                                         Capture(() => nested.Mock.Verify.Add(It.IsAny<List<dynamic>>()).Once()),
		                                                         Capture(() => derived.Mock.Verify.Echo("derived").Once()),
		                                                         Capture(() => repo.Mock.Verify.Get("repo", It.IsAny<List<dynamic>>()).Once()),
		                                                         Capture(() => factory.Mock.Verify("a").Once()),
		                                                         Capture(() => combined.Mock.As<IRepo<dynamic>>().Verify.Add("combined").Once()),
		                                                         Capture(() => sut.Mock.Verify.Add(43).Once()),
		                                                         Capture(() => sut.Mock.Verify.Get("other", It.IsAny<List<dynamic>>()).Once()),
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
			"value",
			"indexed",
			"event",
			"base",
			"derived",
			"repo",
			"created",
			"True",
			"True",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method Add(43) exactly once, but it never did.",
			"Expected that mock invoked method Get(\"other\", It.IsAny<List<object>>()) exactly once, but it never did.",
		]).Because("a type that is constructed with dynamic cannot be implemented, so the mock names the identical type that is constructed with object");
	}
}
