using System.Threading.Tasks;
using Xunit;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.UseVerificationAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class UseVerificationAnalyzerTests
{
	[Fact]
	public async Task WhenIgnoreParametersResultIsNotUsed_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        {|#0:VerifySomething()|};
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>>.IgnoreParameters VerifySomething()
					=> null!;
			}
			""",
			Verifier.Diagnostic(Rules.UseVerificationRule)
				.WithLocation(0)
		);

	[Fact]
	public async Task WhenIgnoreParametersResultIsUsed_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        VerificationResult<MockVerify<int, Mock<int>>>.IgnoreParameters result = VerifySomething();
			        _ = result;
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>>.IgnoreParameters VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenAssigned_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        _ = {|#0:VerifySomething()|};
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenNotUsed_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        {|#0:VerifySomething()|};
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			""",
			Verifier.Diagnostic(Rules.UseVerificationRule)
				.WithLocation(0)
		);

	[Fact]
	public async Task WhenPassedAsArgument_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        ConsumeResult(VerifySomething());
			    }

				public static void ConsumeResult(
					VerificationResult<MockVerify<int, Mock<int>>> result) { }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenReturnedFromMethod_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public static VerificationResult<MockVerify<int, Mock<int>>> MyTest()
			    {
			        return VerifySomething();
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenUsedAsVariableInitializer_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        VerificationResult<MockVerify<int, Mock<int>>> result = VerifySomething();
			        _ = result;
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenUsedInConditional_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest(bool condition)
			    {
			        _ = condition ? VerifySomething() : VerifySomething();
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);

	[Fact]
	public async Task WhenUsedInMethod_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			using Mockolate;
			using Mockolate.Verify;

			public class MyClass
			{
			    public void MyTest()
			    {
			        {|#0:VerifySomething().Never()|};
			    }

				public static VerificationResult<MockVerify<int, Mock<int>>> VerifySomething()
					=> null!;
			}
			"""
		);
}
