using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Analyzers.NullParametersAnalyzer,
	Mockolate.Analyzers.CodeFixers.NullParametersCodeFixProvider>;

namespace Mockolate.Analyzers.Tests;

public class NullParametersCodeFixProviderTests
{
	[Test]
	[Arguments("null")]
	[Arguments("null!")]
	public async Task ShouldReplaceLiteralWithAnyParameters(string literal) => await Verifier.VerifyCodeFixAsync(
		$$"""
		  #nullable enable
		  using Mockolate;
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
		Verifier.Diagnostic(NullParametersAnalyzer.NullParametersRule).WithLocation(0).WithArguments(literal),
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
		""");

	[Test]
	public async Task WithoutUsing_ShouldFullyQualifyMatch() => await Verifier.VerifyCodeFixAsync(
		"""
		#nullable enable
		public class MyClass
		{
		    public void MyTest()
		    {
		        Bar({|#0:default|});
		    }

		    public static void Bar(Mockolate.Parameters.IParameters parameters) { }
		}
		""",
		Verifier.Diagnostic(NullParametersAnalyzer.NullParametersRule).WithLocation(0).WithArguments("default"),
		"""
		#nullable enable
		public class MyClass
		{
		    public void MyTest()
		    {
		        Bar(global::Mockolate.Match.AnyParameters());
		    }

		    public static void Bar(Mockolate.Parameters.IParameters parameters) { }
		}
		""");
}
