using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class GenericParameterDefaultValueTests
{
	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task OptionalParameterOfTypeParameterType_ShouldSetupAndVerifyWithTheArgumentOmitted(
		LanguageVersion languageVersion)
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IFoo
		                                             {
		                                                 int Unconstrained<T>(T value = default!);
		                                                 int Annotated<T>(T? value = default);
		                                                 int Struct<T>(T value = default) where T : struct;
		                                                 int Class<T>(T value = default!) where T : class;
		                                                 int NotNull<T>(T value = default!) where T : notnull;
		                                                 int Interface<T>(T value = default!) where T : IComparable;
		                                                 int Trailing<T>(int a, T value = default!);
		                                             }

		                                             public interface IBar<T>
		                                             {
		                                                 int M(T value = default!);
		                                             }

		                                             public abstract class Base
		                                             {
		                                                 public virtual int Virtual<T>(T value = default!) => -1;
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IFoo sut = IFoo.CreateMock();
		                                                     sut.Mock.Setup.Unconstrained<int>().Returns(1);
		                                                     sut.Mock.Setup.Annotated<int>().Returns(2);
		                                                     sut.Mock.Setup.Struct<int>().Returns(3);
		                                                     sut.Mock.Setup.Class<string>().Returns(4);
		                                                     sut.Mock.Setup.NotNull<int>().Returns(5);
		                                                     sut.Mock.Setup.Interface<int>().Returns(6);
		                                                     sut.Mock.Setup.Trailing<int>(It.IsAny<int>()).Returns(7);
		                                                     IBar<int> bar = IBar<int>.CreateMock();
		                                                     bar.Mock.Setup.M().Returns(8);
		                                                     IBar<string> stringBar = IBar<string>.CreateMock();
		                                                     stringBar.Mock.Setup.M().Returns(9);
		                                                     Base @base = Base.CreateMock();
		                                                     @base.Mock.Setup.Virtual<int>().Returns(10);

		                                                     string results = string.Join(", ",
		                                                         sut.Unconstrained<int>(), sut.Unconstrained(1),
		                                                         sut.Annotated<int>(),
		                                                         sut.Struct<int>(),
		                                                         sut.Class<string>(),
		                                                         sut.NotNull<int>(),
		                                                         sut.Interface<int>(),
		                                                         sut.Trailing<int>(0),
		                                                         bar.M(), stringBar.M(),
		                                                         @base.Virtual<int>(), @base.Virtual(1));

		                                                     return
		                                                     [
		                                                         results,
		                                                         Capture(() => sut.Mock.Verify.Unconstrained<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Annotated<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Struct<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Class<string>().Once()),
		                                                         Capture(() => sut.Mock.Verify.NotNull<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Interface<int>().Once()),
		                                                         Capture(() => sut.Mock.Verify.Trailing<int>(It.IsAny<int>()).Once()),
		                                                         Capture(() => bar.Mock.Verify.M().Once()),
		                                                         Capture(() => stringBar.Mock.Verify.M().Once()),
		                                                         Capture(() => @base.Mock.Verify.Virtual<int>().Once()),
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
		                                             """, languageVersion);

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1",
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
		]).Because("the declared default of a type parameter that is not known to be a reference type is default, not null");
	}
}
