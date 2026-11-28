using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.Modes;

public sealed class ClassicGenericMethodOverrideTests
{
	[Test]
	public async Task Method_WithNullableTypeParameter_ShouldSetUpAndVerify()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public abstract class MyBase
		                                             {
		                                                 public abstract void Accept<U>(U? value);
		                                                 public abstract U? Get<U>();
		                                                 public virtual void AcceptVirtual<U>(U? value) { }
		                                                 public virtual U? GetVirtual<U>() => default;
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     MyBase sut = MyBase.CreateMock();
		                                                     sut.Mock.Setup.Get<int>().Returns(5);
		                                                     sut.Mock.Setup.Get<string>().Returns("a");
		                                                     sut.Mock.Setup.GetVirtual<int>().Returns(6);
		                                                     int accepted = 0;
		                                                     sut.Mock.Setup.Accept<int>(It.IsAny<int>()).Do(value => accepted += value);
		                                                     sut.Mock.Setup.AcceptVirtual<int>(It.IsAny<int>()).Do(value => accepted += value);

		                                                     sut.Accept(3);
		                                                     sut.Accept<string>(null);
		                                                     sut.AcceptVirtual(4);

		                                                     return
		                                                     [
		                                                         $"{sut.Get<int>()}, {sut.Get<string>()}, {sut.Get<long>()}, {sut.GetVirtual<int>()}, {sut.GetVirtual<string>() is null}, {accepted}",
		                                                         Capture(() => sut.Mock.Verify.Accept(3).Once()),
		                                                         Capture(() => sut.Mock.Verify.Accept((string?)null).Once()),
		                                                         Capture(() => sut.Mock.Verify.AcceptVirtual<int>(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Get<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Accept(4).Once()),
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
			"5, a, 0, 6, True, 7",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method Accept<U>(4) exactly once, but it never did.",
		]).Because("the overrides repeat the unconstrained type parameter as 'where U : default', so U? is not Nullable<U> there");
	}
}
