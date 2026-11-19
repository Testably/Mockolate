using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.AutoMockingAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class AutoMockingAnalyzerTests
{
	/// <summary>
	///     A stand-in for <c>Mockolate.MockBehavior</c>, because calls into the referenced Mockolate assembly do not
	///     bind in the test compilation (CS1705).
	/// </summary>
	private const string MockBehaviorStub = """
	                                        namespace Mockolate
	                                        {
	                                            public record MockBehavior
	                                            {
	                                                public bool AutoMock { get; init; }
	                                                public MockBehavior AutoMocking(bool autoMock = true) => this;
	                                            }
	                                        }
	                                        """;

	private const string Source = MockBehaviorStub + """

	                                                 public class MyClass
	                                                 {
	                                                     public void MyTest(Mockolate.MockBehavior behavior)
	                                                     {
	                                                         _ = {|#0:behavior.AutoMocking()|};
	                                                     }
	                                                 }
	                                                 """;

	[Test]
	public async Task WhenDisablingAutoMocking_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(MockBehaviorStub + """

		                                        public class MyClass
		                                        {
		                                            public void MyTest(Mockolate.MockBehavior behavior)
		                                            {
		                                                _ = behavior.AutoMocking(false);
		                                            }
		                                        }
		                                        """);

	[Test]
	public async Task AutoMockFalseInInitializer_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(MockBehaviorStub + """

		                                        public class MyClass
		                                        {
		                                            public void MyTest(Mockolate.MockBehavior behavior)
		                                            {
		                                                _ = behavior with { AutoMock = false };
		                                            }
		                                        }
		                                        """);

	[Test]
	public async Task AutoMockInObjectInitializer_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(MockBehaviorStub + """

		                                        public class MyClass
		                                        {
		                                            public void MyTest()
		                                            {
		                                                _ = new Mockolate.MockBehavior { {|#0:AutoMock = true|} };
		                                            }
		                                        }
		                                        """,
			Verifier.Diagnostic("Mockolate0005").WithLocation(0));

	[Test]
	public async Task AutoMockInWithExpression_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(MockBehaviorStub + """

		                                        public class MyClass
		                                        {
		                                            public void MyTest(Mockolate.MockBehavior behavior, bool value)
		                                            {
		                                                _ = behavior with { {|#0:AutoMock = value|} };
		                                            }
		                                        }
		                                        """,
			Verifier.Diagnostic("Mockolate0005").WithLocation(0));

	[Test]
	public async Task InsideMockBehavior_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync("""
		                     namespace Mockolate
		                     {
		                         public record MockBehavior
		                         {
		                             public bool AutoMock { get; init; }
		                             public MockBehavior AutoMocking(bool autoMock = true) => this with { AutoMock = autoMock };
		                         }
		                     }
		                     """);

	[Test]
	public async Task WithBuildProperty_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerWithBuildPropertiesAsync(Source, "build_property.MockolateAutoMocking = true");

	[Test]
	public async Task WithBuildPropertyFalse_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerWithBuildPropertiesAsync(Source, "build_property.MockolateAutoMocking = false",
			Verifier.Diagnostic("Mockolate0005").WithLocation(0));

	[Test]
	public async Task WithOtherMethodNamedAutoMocking_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			public class MyClass
			{
			    public void MyTest()
			    {
			        AutoMocking();
			    }

			    private static void AutoMocking() { }
			}
			"""
		);

	[Test]
	public async Task WithoutBuildProperty_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(Source,
			Verifier.Diagnostic("Mockolate0005").WithLocation(0));
}
