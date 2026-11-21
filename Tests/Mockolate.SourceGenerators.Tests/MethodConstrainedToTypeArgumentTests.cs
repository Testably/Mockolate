using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MethodConstrainedToTypeArgumentTests
{
	private const string Types = """
	                             #nullable enable
	                             using System;
	                             using Mockolate;
	                             using Mockolate.Verify;

	                             namespace MyCode;

	                             public class Open { }
	                             public sealed class Sealed : Open { }
	                             public struct Point { public int X; }

	                             public interface IWithBase<T, TBase>
	                             {
	                                 void Accept<U>(U? value) where U : T, TBase;
	                             }

	                             public interface IB<T>
	                             {
	                                 void Accept<U>(U? value) where U : T;
	                                 U Get<U>(int key = 1) where U : T;
	                                 U? GetMany<U>(U? a, U? b, U? c, U? d, U? e) where U : T;
	                                 void AcceptNotNull<U>(U? value) where U : notnull, T;
	                                 void AcceptConstructor<U>(U? value) where U : T, new();
	                                 void AcceptInterface<U>(U? value) where U : T, IComparable;
	                                 void AcceptDependent<U, V>(U? value, V? other) where U : V where V : T;
	                                 void AcceptRefStruct<U>() where U : T, allows ref struct;
	                             }

	                             public abstract class B<T>
	                             {
	                                 public abstract void Accept<U>(U? value) where U : T;
	                                 public virtual U Get<U>(int key = 1) where U : T => default!;
	                                 public abstract void AcceptNotNull<U>(U? value) where U : notnull, T;
	                                 public abstract void AcceptDependent<U, V>(U? value, V? other) where U : V where V : T;
	                             }

	                             public class Outer<T>
	                             {
	                                 public interface IInner
	                                 {
	                                     U? Get<U>(U? value) where U : T;
	                                 }
	                             }

	                             public interface IOther
	                             {
	                             }

	                             public static class Mocks
	                             {
	                                 public static object[] CreateOpen<TOpen>()
	                                     => [IB<TOpen>.CreateMock(), B<TOpen>.CreateMock(),];

	                                 public static object[] Create()
	                                     => [
	                                         IB<int>.CreateMock(), B<int>.CreateMock(),
	                                         IB<int?>.CreateMock(), B<int?>.CreateMock(),
	                                         IB<string>.CreateMock(), B<string>.CreateMock(),
	                                         IB<DayOfWeek>.CreateMock(), B<DayOfWeek>.CreateMock(),
	                                         IB<Point>.CreateMock(), B<Point>.CreateMock(),
	                                         IB<(int, string)>.CreateMock(), B<(int, string)>.CreateMock(),
	                                         IB<Sealed>.CreateMock(), B<Sealed>.CreateMock(),
	                                         IB<int[]>.CreateMock(), B<int[]>.CreateMock(),
	                                         IB<Action>.CreateMock(), B<Action>.CreateMock(),
	                                         IB<Array>.CreateMock(), B<Array>.CreateMock(),
	                                         IB<ValueType>.CreateMock(), B<ValueType>.CreateMock(),
	                                         IB<object>.CreateMock(), B<object>.CreateMock(),
	                                         IWithBase<Sealed, Open>.CreateMock(),
	                                         Outer<int>.IInner.CreateMock(), Outer<string>.IInner.CreateMock(),
	                                         IOther.CreateMock().Implementing<IB<int>>(),
	                                         IOther.CreateMock().Implementing<IB<string>>(),
	                                     ];
	                             }
	                             """;

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task ClosedOverTypesThatCanOnlyBeInheritedAsConstraint_ShouldCompile(
		LanguageVersion languageVersion)
	{
		Assembly assembly = Generator.CompileAndLoad(Types, languageVersion);

		object[] mocks = (object[])assembly.GetType("MyCode.Mocks")!.GetMethod("Create")!.Invoke(null, null)!;

		await That(mocks).All().Satisfy(mock => mock is not null)
			.Because("a constraint that C# accepts only when inherited must not be restated on the generated mock");
	}

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task ClosedOverValueTypeAndSealedClass_ShouldSetupAndVerify(LanguageVersion languageVersion)
	{
		Assembly assembly = Generator.CompileAndLoad(Types + """

		                                                     public static class Scenarios
		                                                     {
		                                                         public static string[] Run()
		                                                         {
		                                                             IB<int> values = IB<int>.CreateMock();
		                                                             values.Mock.Setup.Get<int>(2).Returns(5);
		                                                             int received = 0;
		                                                             values.Mock.Setup.AcceptDependent<int, int>(It.IsAny<int>(), It.Is(1)).Do((value, other) => received = value + other);
		                                                             values.Accept(3);
		                                                             values.AcceptDependent<int, int>(4, 1);

		                                                             B<string> references = B<string>.CreateMock();
		                                                             references.Mock.Setup.Get<string>(It.IsAny<int>()).Returns("a");
		                                                             string? accepted = "unset";
		                                                             references.Mock.Setup.Accept<string>(It.IsAny<string?>()).Do(value => accepted = value);
		                                                             references.Accept<string>(null);
		                                                             references.AcceptNotNull("b");

		                                                             IB<int> combined = (IB<int>)IOther.CreateMock().Implementing<IB<int>>();

		                                                             return
		                                                             [
		                                                                 $"{values.Get<int>(2)}, {values.Get<int>()}, {received}, {references.Get<string>()}, {accepted ?? "null"}, {combined.Get<int>()}",
		                                                                 Capture(() => values.Mock.Verify.Accept<int>(3).Once()),
		                                                                 Capture(() => values.Mock.Verify.Get<int>(It.IsAny<int>()).Twice()),
		                                                                 Capture(() => values.Mock.Verify.AcceptDependent<int, int>(4, 1).Once()),
		                                                                 Capture(() => references.Mock.Verify.Accept<string>(It.IsNull<string?>()).Once()),
		                                                                 Capture(() => references.Mock.Verify.AcceptNotNull<string>("b").Once()),
		                                                                 Capture(() => references.Mock.Verify.AcceptNotNull<string>("c").Once()),
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
			"5, 0, 5, a, null, 0",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method AcceptNotNull<U>(\"c\") exactly once, but it never did.",
		]).Because("the methods are mocked although their constraint can only be inherited");
	}
}
