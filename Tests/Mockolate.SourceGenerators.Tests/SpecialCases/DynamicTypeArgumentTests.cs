using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.SpecialCases;

public sealed class DynamicTypeArgumentTests
{
	private const string Types = """
	                             #nullable enable
	                             using System;
	                             using System.Collections.Generic;
	                             using Mockolate;
	                             using Mockolate.Verify;

	                             namespace MyCode;

	                             public interface IRepo<T>
	                             {
	                                 event EventHandler<T>? Changed;
	                                 T Get(T key, List<T> values);
	                             }

	                             public interface ISimple<T>
	                             {
	                                 T Get(T key);
	                             }

	                             public interface IOtherA { void Do(); }
	                             public interface IOtherB { void Do(); }
	                             public interface IOtherC { void Do(); }
	                             public interface IOtherD { void Do(); }
	                             public interface IFirst { void First(); }
	                             public interface ISecond { void Second(); }

	                             public class Outer<T>
	                             {
	                                 public interface IInner
	                                 {
	                                     T Get((T Key, int Index) value);
	                                 }
	                             }

	                             public class Repo<T> : IRepo<T>
	                             {
	                                 public virtual event EventHandler<T>? Changed;
	                                 public virtual T Get(T key, List<T> values)
	                                 {
	                                     Changed?.Invoke(this, key);
	                                     return key;
	                                 }
	                             }

	                             public class DerivedRepo : Repo<dynamic>
	                             {
	                             }
	                             """;

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task ClassImplementingTheAdditionalInterface_ShouldShareTheMembersOfBothSpellings(
		LanguageVersion languageVersion)
	{
		Assembly assembly = Generator.CompileAndLoad(Types + """

		                                                     public static class Scenarios
		                                                     {
		                                                         public static string[] Run()
		                                                         {
		                                                             Repo<dynamic> repo = Repo<dynamic>.CreateMock().Implementing<IRepo<object>>();
		                                                             Repo<object> objectRepo = Repo<object>.CreateMock().Implementing<IRepo<dynamic>>();
		                                                             DerivedRepo derived = DerivedRepo.CreateMock().Implementing<IRepo<dynamic>>();
		                                                             DerivedRepo objectDerived = DerivedRepo.CreateMock().Implementing<IRepo<object>>();
		                                                             repo.Mock.Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("repo");
		                                                             derived.Mock.Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("derived");

		                                                             object repoValue = ((IRepo<dynamic>)repo).Get("key", new List<dynamic>());
		                                                             object derivedValue = ((IRepo<object>)derived).Get("key", new List<object>());

		                                                             return
		                                                             [
		                                                                 repoValue.ToString()!,
		                                                                 derivedValue.ToString()!,
		                                                                 (repo.GetType() == objectRepo.GetType()).ToString(),
		                                                                 (derived.GetType() == objectDerived.GetType()).ToString(),
		                                                             ];
		                                                         }
		                                                     }
		                                                     """, languageVersion, typeof(DynamicAttribute));

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo(["repo", "derived", "True", "True",])
			.Because("the class already implements the additional interface in either spelling, so a setup of the class member must also answer the call through the interface");
	}

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task BothSpellings_InDifferentCombinations_ShouldCompileAndShareTheMocks(
		LanguageVersion languageVersion)
	{
		Assembly assembly = Generator.CompileAndLoad(Types + """

		                                                     public static class Scenarios
		                                                     {
		                                                         public static string[] Run()
		                                                         {
		                                                             IOtherA a1 = IOtherA.CreateMock().Implementing<IRepo<dynamic>>();
		                                                             IRepo<object> a2 = IRepo<object>.CreateMock().Implementing<IOtherA>();
		                                                             IOtherB b1 = IOtherB.CreateMock().Implementing<IRepo<object>>();
		                                                             IRepo<dynamic> b2 = IRepo<dynamic>.CreateMock().Implementing<IOtherB>();
		                                                             IFirst c1 = IFirst.CreateMock().Implementing<IRepo<dynamic>>().Implementing<IOtherC>();
		                                                             ISecond c2 = ISecond.CreateMock().Implementing<IRepo<object>>().Implementing<IOtherC>();
		                                                             IOtherD d1 = IOtherD.CreateMock().Implementing<ISimple<dynamic>>();
		                                                             ISimple<object> d2 = ISimple<object>.CreateMock().Implementing<IOtherD>();
		                                                             Outer<dynamic>.IInner inner = Outer<dynamic>.IInner.CreateMock();
		                                                             Outer<object>.IInner objectInner = Outer<object>.IInner.CreateMock();

		                                                             a1.Mock.As<IRepo<dynamic>>().Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("a1");
		                                                             a2.Mock.Setup.Get("key", It.IsAny<List<object>>()).Returns("a2");
		                                                             b1.Mock.As<IRepo<object>>().Setup.Get("key", It.IsAny<List<object>>()).Returns("b1");
		                                                             b2.Mock.Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("b2");
		                                                             c1.Mock.As<IOtherC>().As<IRepo<dynamic>>().Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("c1");
		                                                             c2.Mock.As<IOtherC>().As<IRepo<object>>().Setup.Get("key", It.IsAny<List<dynamic>>()).Returns("c2");
		                                                             d1.Mock.As<ISimple<dynamic>>().Setup.Get("key").Returns("d1");
		                                                             d2.Mock.Setup.Get("key").Returns("d2");
		                                                             inner.Mock.Setup.Get(It.IsAny<(dynamic Key, int Index)>()).Returns("inner");
		                                                             a2.Mock.As<IOtherA>().Setup.Do().Do(() => { });
		                                                             b2.Mock.As<IOtherB>().Setup.Do().Do(() => { });
		                                                             d2.Mock.As<IOtherD>().Setup.Do().Do(() => { });
		                                                             ((IOtherA)a2).Do();
		                                                             ((IOtherB)b2).Do();
		                                                             ((IOtherD)d2).Do();

		                                                             return
		                                                             [
		                                                                 ((object)((IRepo<dynamic>)a1).Get("key", new List<dynamic>())).ToString()!,
		                                                                 a2.Get("key", new List<object>()).ToString()!,
		                                                                 ((object)((IRepo<dynamic>)b1).Get("key", new List<dynamic>())).ToString()!,
		                                                                 ((object)b2.Get("key", new List<dynamic>())).ToString()!,
		                                                                 ((IRepo<object>)c1).Get("key", new List<object>()).ToString()!,
		                                                                 ((object)((IRepo<dynamic>)c2).Get("key", new List<dynamic>())).ToString()!,
		                                                                 ((ISimple<object>)d1).Get("key").ToString()!,
		                                                                 d2.Get("key").ToString()!,
		                                                                 ((object)inner.Get(("key", 1))).ToString()!,
		                                                                 (inner.GetType() == objectInner.GetType()).ToString(),
		                                                                 Capture(() => a1.Mock.As<IRepo<object>>().Verify.Get("key", It.IsAny<List<object>>()).Once()),
		                                                                 Capture(() => a2.Mock.As<IOtherA>().Verify.Do().Once()),
		                                                                 Capture(() => b1.Mock.As<IRepo<dynamic>>().Verify.Get("key", It.IsAny<List<dynamic>>()).Once()),
		                                                                 Capture(() => b2.Mock.As<IOtherB>().Verify.Do().Once()),
		                                                                 Capture(() => c1.Mock.As<IOtherC>().As<IRepo<object>>().Verify.Get("key", It.IsAny<List<object>>()).Once()),
		                                                                 Capture(() => c2.Mock.As<IOtherC>().As<IRepo<dynamic>>().Verify.Get("key", It.IsAny<List<dynamic>>()).Once()),
		                                                                 Capture(() => d1.Mock.As<ISimple<object>>().Verify.Get("key").Once()),
		                                                                 Capture(() => d2.Mock.As<IOtherD>().Verify.Do().Once()),
		                                                                 Capture(() => b2.Mock.Verify.Get("other", It.IsAny<List<dynamic>>()).Once()),
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
		                                                     """, languageVersion, typeof(DynamicAttribute));

		string[] messages = (string[])assembly.GetType("MyCode.Scenarios")!.GetMethod("Run")!.Invoke(null, null)!;

		await That(messages).IsEqualTo([
			"a1",
			"a2",
			"b1",
			"b2",
			"c1",
			"c2",
			"d1",
			"d2",
			"inner",
			"True",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"no exception",
			"Expected that mock invoked method Get(\"other\", It.IsAny<List<object>>()) exactly once, but it never did.",
		]).Because("a type that is constructed with dynamic is the type that is constructed with object, so a mock or an As<T> extension for each spelling would be a duplicate");
	}

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
#if NET11_0_OR_GREATER
	[Arguments(LanguageVersion.CSharp15)]
#endif
	public async Task BothSpellings_ShouldGenerateTheSameSourcesInAnyOrder(LanguageVersion languageVersion)
	{
		string[] mocks =
		[
			"_ = IRepo<dynamic>.CreateMock();",
			"_ = IRepo<object>.CreateMock();",
			"_ = IOtherA.CreateMock().Implementing<IRepo<dynamic>>();",
			"_ = IRepo<object>.CreateMock().Implementing<IOtherA>();",
			"_ = Outer<dynamic>.IInner.CreateMock();",
			"_ = ISimple<(dynamic Key, List<dynamic?>? Values)?[]>.CreateMock();",
			"_ = DerivedRepo.CreateMock().Implementing<IRepo<dynamic>>();",
		];

		GeneratorResult result = Run(mocks);
		GeneratorResult reversedResult = Run(mocks.Reverse().ToArray());
		GeneratorResult objectResult = Run(mocks.Select(mock => mock.Replace("dynamic", "object")).ToArray());

		await That(result.Diagnostics).IsEmpty();
		await That(ToFiles(reversedResult)).IsEqualTo(ToFiles(result))
			.Because("the generated sources must not depend on which spelling comes first");
		await That(ToFiles(objectResult)).IsEqualTo(ToFiles(result))
			.Because("a type that is constructed with dynamic is mocked as the type that is constructed with object");

		GeneratorResult Run(string[] mockCreations)
			=> Generator.Run([
				Types + $$"""

				          public static class Program
				          {
				              public static void Main()
				              {
				                  {{string.Join("\n        ", mockCreations)}}
				              }
				          }
				          """,
			], [], languageVersion, null, typeof(DynamicAttribute));

		static string[] ToFiles(GeneratorResult generatorResult)
			=> generatorResult.Sources.OrderBy(source => source.Key, StringComparer.Ordinal)
				.Select(source => $"{source.Key}:\n{source.Value}").ToArray();
	}

	[Test]
	public async Task TypeNamedDynamic_ShouldBeKeptAsTypeArgument()
	{
		GeneratorResult result = Generator.Run("""
		                                       using Mockolate;

		                                       namespace MyCode
		                                       {
		                                           public class @dynamic { }

		                                           public interface ISimple<T>
		                                           {
		                                               T Get(T key);
		                                           }

		                                           public static class Program
		                                           {
		                                               public static void Main()
		                                               {
		                                                   _ = ISimple<dynamic>.CreateMock();
		                                               }
		                                           }
		                                       }
		                                       """);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources).ContainsKey("Mock.ISimple_dynamic.g.cs").WhoseValue
			.Contains("global::MyCode.ISimple<global::MyCode.dynamic>")
			.Because("only the dynamic type is replaced, not a type that is named dynamic");
	}
}
