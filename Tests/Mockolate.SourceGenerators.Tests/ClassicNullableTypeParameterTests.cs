using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ClassicNullableTypeParameterTests
{
	[Test]
	public async Task NullableMethodTypeParameter_InClassicMode_WithConstraint_ShouldSetupAndVerify()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IBar<T>
		                                             {
		                                                 void AcceptClass<U>(U? value) where U : class;
		                                                 void AcceptNotNull<U>(U? value) where U : notnull;
		                                                 void AcceptInterface<U>(U? value) where U : IComparable;
		                                                 void AcceptMany<U>(U? a, U? b, U? c, U? d, U? e) where U : notnull;
		                                                 U? GetClass<U>(U? fallback) where U : class;
		                                                 U? GetNotNull<U>(U? fallback) where U : notnull;
		                                             }

		                                             public interface IBaz
		                                             {
		                                                 U? FindClass<U>(U? fallback) where U : class;
		                                                 U? FindNotNull<U>(U? fallback) where U : notnull;
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IBar<int> sut = IBar<int>.CreateMock();
		                                                     sut.Mock.Setup.GetClass<string>("a").Returns("b");
		                                                     sut.Mock.Setup.GetNotNull<int>(It.IsAny<int>()).Returns(2);
		                                                     string? received = "unset";
		                                                     sut.Mock.Setup.AcceptClass<string>(It.IsAny<string?>()).Do(value => received = value);
		                                                     sut.AcceptClass<string>(null);
		                                                     sut.AcceptNotNull(1);
		                                                     sut.AcceptInterface(2);
		                                                     sut.AcceptMany(1, 2, 3, 4, 5);

		                                                     IBar<string> other = IBar<string>.CreateMock();
		                                                     other.AcceptClass("x");

		                                                     IBaz combined = (IBaz)IBar<int>.CreateMock().Implementing<IBaz>();

		                                                     return
		                                                     [
		                                                         $"{sut.GetClass("a")}, {sut.GetNotNull(1)}, {received ?? "null"}",
		                                                         $"{combined.FindClass<string>(null)}, {combined.FindNotNull(3)}",
		                                                         Capture(() => sut.Mock.Verify.AcceptClass<string>(It.IsNull<string?>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.AcceptNotNull<int>(1).Once()),
		                                                         Capture(() => sut.Mock.Verify.AcceptInterface<int>(It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.AcceptMany<int>(1, 2, 3, 4, 5).Once()),
		                                                         Capture(() => sut.Mock.Verify.AcceptNotNull<int>(2).Once()),
		                                                         Capture(() => other.Mock.Verify.AcceptClass<string>("x").Once()),
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
			"b, 2, null",
			", 0",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method AcceptNotNull<U>(2) exactly once, but it never did.",
			"no exception",
		]).Because("U? of a constrained method type parameter is a nullable annotation, so the explicit implementations must not bind it to Nullable<U>");
	}


	[Test]
	public async Task NullableTypeParameter_InClassicMode_ClosedOverValueAndReferenceType_ShouldSetupAndVerify()
	{
		Assembly assembly = Generator.CompileAndLoad("""
		                                             using System;
		                                             using Mockolate;
		                                             using Mockolate.Verify;

		                                             namespace MyCode;

		                                             public interface IBar<T>
		                                             {
		                                                 T? Value { get; set; }
		                                                 T? this[T? key] { get; set; }
		                                                 event Action<T?, int>? Changed;
		                                                 T? Get();
		                                                 void G2(T? a, int b);
		                                                 T? Combine(T? a, int b, T? c, int d, T? e);
		                                                 bool TryGet(int key, out T? value);
		                                             }

		                                             public static class Scenarios
		                                             {
		                                                 public static string[] Run()
		                                                 {
		                                                     IBar<int> sut = IBar<int>.CreateMock();
		                                                     sut.Mock.Setup.Get().Returns(5);
		                                                     sut.Mock.Setup.Value.InitializeWith(6);
		                                                     sut.Mock.Setup[It.Is(1)].Returns(7);
		                                                     sut.Mock.Setup.Combine(It.Is(1), It.IsAny<int>(), It.Is(3), It.IsAny<int>(), It.Is(5)).Returns(8);
		                                                     sut.Mock.Setup.TryGet(It.IsAny<int>(), It.IsOut(() => 9)).Returns(true);
		                                                     int raised = 0;
		                                                     sut.Changed += (value, count) => raised = value + count;
		                                                     sut.Mock.Raise.Changed(3, 4);
		                                                     sut.G2(1, 2);
		                                                     sut.G2(1, 3);
		                                                     bool found = sut.TryGet(0, out int outValue);

		                                                     IBar<string> other = IBar<string>.CreateMock();
		                                                     other.Mock.Setup.Get().Returns("a");
		                                                     other.Mock.Setup[(string?)null].Returns("b");
		                                                     other.G2(null, 2);

		                                                     return
		                                                     [
		                                                         $"{sut.Get()}, {sut.Value}, {sut[1]}, {sut.Combine(1, 2, 3, 4, 5)}, {found}, {outValue}, {raised}",
		                                                         Capture(() => sut.Mock.Verify.G2(1, It.IsAny<int>()).Twice()),
		                                                         Capture(() => sut.Mock.Verify.G2(It.IsAny<int>(), 3).Once()),
		                                                         Capture(() => sut.Mock.Verify.G2(1, 2).Once()),
		                                                         Capture(() => sut.Mock.Verify.G2(2, It.IsAny<int>()).Once()),
		                                                         Capture(() => sut.Mock.Verify.Value.Got().Once()),
		                                                         Capture(() => sut.Mock.Verify[It.Is(1)].Got().Once()),
		                                                         $"{other.Get()}, {other[null]}",
		                                                         Capture(() => other.Mock.Verify.G2((string?)null, 2).Once()),
		                                                         Capture(() => other.Mock.Verify.G2("x", It.IsAny<int>()).Once()),
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
			"5, 6, 7, 8, True, 9, 7",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method G2(2, It.IsAny<int>()) exactly once, but it never did.",
			"no exception",
			"no exception",
			"a, b",
			"no exception",
			"Expected that mock invoked method G2(x, It.IsAny<int>()) exactly once, but it never did.",
		]).Because("T? closed over a value type is the plain value type, so the classic setup and verify surface must use int instead of Nullable<int>");
	}
}
