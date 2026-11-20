using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.NullParametersAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class NullParametersAnalyzerTests
{
	[Test]
	[Arguments("null")]
	[Arguments("default")]
	[Arguments("null!")]
	[Arguments("(null)")]
	public async Task WhenNullLiteralIsPassedToNonNullableParameters_ShouldBeFlagged(string literal) => await Verifier
		.VerifyAnalyzerAsync(
			$$"""
			  #nullable enable
			  using Mockolate.Parameters;

			  public class MyClass
			  {
			      public void MyTest()
			      {
			          Bar({|#0:{{literal}}|});
			      }

			      public static void Bar(IParameters parameters) { }
			  }
			  """,
			Verifier.Diagnostic(NullParametersAnalyzer.NullParametersRule)
				.WithLocation(0)
				.WithArguments(literal)
		);

	[Test]
	public async Task WhenNullLiteralIsPassedAsNamedArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest()
			    {
			        Bar(parameters: {|#0:null|});
			    }

			    public static void Bar(IParameters parameters) { }
			}
			""",
			Verifier.Diagnostic(NullParametersAnalyzer.NullParametersRule)
				.WithLocation(0)
				.WithArguments("null")
		);

	[Test]
	public async Task WhenNullLiteralIsPassedToNullableParameters_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest()
			    {
			        Bar(null);
			    }

			    public static void Bar(IParameters? parameters) { }
			}
			"""
		);

	[Test]
	public async Task WhenParametersAreNullableOblivious_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable disable
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest()
			    {
			        Bar(null);
			    }

			    public static void Bar(IParameters parameters) { }
			}
			"""
		);

	[Test]
	public async Task WhenNullLiteralIsPassedToOtherParameter_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest()
			    {
			        Bar(null);
			    }

			    public static void Bar(IParameter<int>? value) { }
			}
			"""
		);

	[Test]
	public async Task WhenAnyParametersIsPassed_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			using Mockolate;
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest()
			    {
			        Bar(Match.AnyParameters());
			    }

			    public static void Bar(IParameters parameters) { }
			}
			"""
		);

	[Test]
	public async Task WhenVariableIsPassed_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			#nullable enable
			using Mockolate.Parameters;

			public class MyClass
			{
			    public void MyTest(IParameters parameters)
			    {
			        Bar(parameters);
			    }

			    public static void Bar(IParameters parameters) { }
			}
			"""
		);
}
