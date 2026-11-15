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
}
