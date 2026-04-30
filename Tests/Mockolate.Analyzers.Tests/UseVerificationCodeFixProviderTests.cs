using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<Mockolate.Analyzers.UseVerificationAnalyzer,
	Mockolate.Analyzers.CodeFixers.UseVerificationCodeFixProvider>;

namespace Mockolate.Analyzers.Tests;

public class UseVerificationCodeFixProviderTests
{
	[Test]
	public async Task ShouldApplyCodeFix() => await Verifier.VerifyCodeFixAsync(
		"""
		using Mockolate;
		using Mockolate.Verify;

		public class MyClass
		{
		    public void MyTest()
		    {
		        [|VerifySomething()|];
		    }

			public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
				=> null!;
		}
		""",
		"""
		using Mockolate;
		using Mockolate.Verify;

		public class MyClass
		{
		    public void MyTest()
		    {
		        VerifySomething().AtLeastOnce();
		    }

			public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
				=> null!;
		}
		""");
}
