using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockabilityAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockabilityAnalyzerSpellingTests
{
	private const string Prefix = """
	                              #nullable enable
	                              using System.Collections.Generic;
	                              using Mockolate;

	                              namespace Mockolate
	                              {
	                              	internal static class MockExtensions
	                              	{
	                              		extension<T>(T mock) where T : class
	                              		{
	                              			public static T CreateMock() => default!;
	                              			public T Implementing<TInterface>() => default!;
	                              		}
	                              	}
	                              }

	                              namespace MyCode
	                              {
	                              	public interface IRepo<T> { T Get(T key); }
	                              	public interface IOther { }
	                              }

	                              """;

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
	[Arguments(LanguageVersion.Preview)]
	public async Task WhenTypeIsMockedWithDifferentTupleElementNames_ShouldFlagEverySpelling(
		LanguageVersion languageVersion) => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			{|#0:IRepo<(int a, int b)>|}.CreateMock();
			         			{|#1:IRepo<(int, int)>|}.CreateMock();
			         		}
			         	}
			         }
			         """,
			languageVersion,
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(0)
				.WithArguments("MyCode.IRepo<(int a, int b)>", "'MyCode.IRepo<(int, int)>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(1)
				.WithArguments("MyCode.IRepo<(int, int)>", "'MyCode.IRepo<(int a, int b)>'")
		);

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
	[Arguments(LanguageVersion.Preview)]
	public async Task WhenTypeIsMockedWithDifferentNullableAnnotations_ShouldFlagEverySpelling(
		LanguageVersion languageVersion) => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			{|#0:IRepo<string?>|}.CreateMock();
			         			{|#1:IRepo<string>|}.CreateMock();
			         		}
			         	}
			         }
			         """,
			languageVersion,
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(0)
				.WithArguments("MyCode.IRepo<string?>", "'MyCode.IRepo<string>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(1)
				.WithArguments("MyCode.IRepo<string>", "'MyCode.IRepo<string?>'")
		);

	[Test]
	[Arguments(LanguageVersion.CSharp14)]
	[Arguments(LanguageVersion.Preview)]
	public async Task WhenNestedSpellingsDifferInImplementing_ShouldFlagEverySpelling(
		LanguageVersion languageVersion) => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			IOther.CreateMock().Implementing<{|#0:IRepo<List<(int a, int b)>>|}>();
			         			{|#1:IRepo<List<(int, int)>>|}.CreateMock();
			         			IOther.CreateMock().Implementing<{|#2:IRepo<List<string?>>|}>();
			         			IOther.CreateMock().Implementing<{|#3:IRepo<List<string>>|}>();
			         		}
			         	}
			         }
			         """,
			languageVersion,
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(0)
				.WithArguments("MyCode.IRepo<System.Collections.Generic.List<(int a, int b)>>",
					"'MyCode.IRepo<System.Collections.Generic.List<(int, int)>>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(1)
				.WithArguments("MyCode.IRepo<System.Collections.Generic.List<(int, int)>>",
					"'MyCode.IRepo<System.Collections.Generic.List<(int a, int b)>>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(2)
				.WithArguments("MyCode.IRepo<System.Collections.Generic.List<string?>>",
					"'MyCode.IRepo<System.Collections.Generic.List<string>>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(3)
				.WithArguments("MyCode.IRepo<System.Collections.Generic.List<string>>",
					"'MyCode.IRepo<System.Collections.Generic.List<string?>>'")
		);

	[Test]
	public async Task WhenTypeIsMockedInMoreThanTwoSpellings_ShouldListAllOtherSpellings() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			{|#0:IRepo<(int x, int y)>|}.CreateMock();
			         			{|#1:IRepo<(int a, int b)>|}.CreateMock();
			         			{|#2:IRepo<(int a, int b)>|}.CreateMock();
			         			{|#3:IRepo<(int, int)>|}.CreateMock();
			         		}
			         	}
			         }
			         """,
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(0)
				.WithArguments("MyCode.IRepo<(int x, int y)>",
					"'MyCode.IRepo<(int a, int b)>', 'MyCode.IRepo<(int, int)>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(1)
				.WithArguments("MyCode.IRepo<(int a, int b)>",
					"'MyCode.IRepo<(int x, int y)>', 'MyCode.IRepo<(int, int)>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(2)
				.WithArguments("MyCode.IRepo<(int a, int b)>",
					"'MyCode.IRepo<(int x, int y)>', 'MyCode.IRepo<(int, int)>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(3)
				.WithArguments("MyCode.IRepo<(int, int)>",
					"'MyCode.IRepo<(int a, int b)>', 'MyCode.IRepo<(int x, int y)>'")
		);

	[Test]
	public async Task WhenTheGeneratedMocksMakeTheCallsAmbiguous_ShouldFlagEverySpelling() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			namespace Mockolate
			{
				internal static class MockExtensionsForIRepo_string
				{
					extension(MyCode.IRepo<string> mock)
					{
						public static MyCode.IRepo<string> CreateMock() => default!;
						public MyCode.IRepo<string> Implementing<TInterface>() => default!;
					}
				}

				internal static class MockExtensionsForIRepo_string_1
				{
					extension(MyCode.IRepo<string?> mock)
					{
						public static MyCode.IRepo<string?> CreateMock() => default!;
						public MyCode.IRepo<string?> Implementing<TInterface>() => default!;
					}
				}
			}

			namespace MyCode
			{
				public interface IRepo<T> { T Get(T key); }

				public class MyClass
				{
					public void MyTest()
					{
						{|#0:IRepo<string>|}.CreateMock().Implementing<{|#1:IRepo<string?>|}>();
					}
				}
			}
			""",
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(0)
				.WithArguments("MyCode.IRepo<string>", "'MyCode.IRepo<string?>'"),
			new DiagnosticResult("Mockolate0004", DiagnosticSeverity.Error)
				.WithLocation(1)
				.WithArguments("MyCode.IRepo<string?>", "'MyCode.IRepo<string>'")
		);

	[Test]
	[Arguments("IRepo<(int a, int b)>")]
	[Arguments("IRepo<string?>")]
	[Arguments("IRepo<List<(int a, int b)>>")]
	public async Task WhenTypeIsMockedInASingleSpelling_ShouldNotBeFlagged(string type) => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + $$"""
			           namespace MyCode
			           {
			           	public class MyClass
			           	{
			           		public void MyTest()
			           		{
			           			{{type}}.CreateMock();
			           			{{type}}.CreateMock();
			           			IOther.CreateMock().Implementing<{{type}}>();
			           		}
			           	}
			           }
			           """
		);

	[Test]
	public async Task WhenTypeIsMockedWithDynamicAndObject_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			IRepo<dynamic>.CreateMock();
			         			IRepo<object>.CreateMock();
			         			IOther.CreateMock().Implementing<IRepo<List<dynamic>>>();
			         			IRepo<List<object>>.CreateMock();
			         		}
			         	}
			         }
			         """
		);

	[Test]
	public async Task WhenTypeArgumentsAreNullableAndNonNullableValueTypes_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			IRepo<int?>.CreateMock();
			         			IRepo<int>.CreateMock();
			         		}
			         	}
			         }
			         """
		);

	[Test]
	public async Task WhenTypeIsMockedInNullableEnabledAndDisabledContext_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			Prefix + """
			         namespace MyCode
			         {
			         	public class MyClass
			         	{
			         		public void MyTest()
			         		{
			         			IRepo<string>.CreateMock();
			         #nullable disable
			         			IRepo<string>.CreateMock();
			         #nullable restore
			         		}
			         	}
			         }
			         """
		);
}
