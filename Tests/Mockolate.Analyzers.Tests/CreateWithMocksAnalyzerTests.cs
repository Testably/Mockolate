using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.CreateWithMocksAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class CreateWithMocksAnalyzerTests
{
	[Test]
	[Arguments("public struct Svc { public Svc(IDep dep) { } }", "it is a struct")]
	[Arguments("public interface ISvc { }", "it is not a class")]
	[Arguments("public abstract class Svc(IDep dep) { }", "it is abstract")]
	[Arguments("public static class Svc { }", "it is static")]
	[Arguments("[System.Obsolete] public class Svc(IDep dep) { }", "it is obsolete or experimental")]
	[Arguments("public class Svc { public Svc(IDep dep) { } public Svc(IDep dep, int value) { } public Svc(IDep dep, string value) { } }",
		"several accessible constructors have the most parameters (2), so none of them is chosen")]
	[Arguments("public class Svc { public Svc(ref int value) { } }",
		"it has no accessible constructor without ref, out, pointer or ref struct parameters that is not obsolete and sets all required members")]
	[Arguments("public class Svc(IDep dep) { public required string Name { get; init; } }",
		"it has no accessible constructor without ref, out, pointer or ref struct parameters that is not obsolete and sets all required members")]
	[Arguments("public class Svc(IDep dep) { } public class SvcMocks { }",
		"the name 'SvcMocks' of its arguments container is already used in its namespace")]
	public async Task WhenTheGeneratorSkipsTheClass_ShouldBeFlagged(string declaration, string reason)
	{
		string typeName = declaration.Contains("ISvc") ? "ISvc" : "Svc";
		await Verifier.VerifyAnalyzerAsync(
			$$"""
			  namespace MyCode
			  {
			  	public interface IDep { }

			  	{{declaration}}

			  	public class Program
			  	{
			  		public void Run()
			  		{
			  			_ = {|#0:{{typeName}}|}.CreateWithMocks(out _);
			  		}
			  	}
			  }
			  """,
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments($"MyCode.{typeName}", reason));
	}

	[Test]
	public async Task WhenTheClassIsGeneric_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public class Svc<T>(T value) { }

				public class Program
				{
					public void Run()
					{
						_ = {|#0:Svc<int>|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("MyCode.Svc<int>", "it is generic or nested in a generic type"));

	[Test]
	public async Task WhenTheClassIsPrivateNested_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public class Program
				{
					private class Svc { }

					public void Run()
					{
						_ = {|#0:Svc|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("MyCode.Program.Svc",
					"it is not accessible from the generated code, which needs a public or internal class"));

	[Test]
	public async Task WhenTheClassIsInAFrameworkNamespace_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace System.Custom
			{
				public class Svc { }
			}

			namespace MyCode
			{
				public class Program
				{
					public void Run()
					{
						_ = {|#0:System.Custom.Svc|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("System.Custom.Svc", "it is declared in the System, Microsoft or Windows namespace"));

	[Test]
	public async Task WhenTheClassIsSupported_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public interface IDep { }

				public class Svc
				{
					public Svc(IDep dep) { }
					public Svc() { }
				}

				public class Program
				{
					public void Run()
					{
						_ = Svc.CreateWithMocks(out _);
					}
				}
			}
			""");

	[Test]
	public async Task WhenTheClassDeclaresCreateWithMocks_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public abstract class Svc
				{
					public static Svc CreateWithMocks(int value) => null!;
				}

				public class Program
				{
					public void Run()
					{
						_ = Svc.CreateWithMocks(out _);
					}
				}
			}
			""");

	[Test]
	public async Task WhenAClassDependencyHasNoArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.TimeSource clock, string name") + """
			                                                         namespace MyCode
			                                                         {
			                                                         	public class TimeSource { }

			                                                         	public class Program
			                                                         	{
			                                                         		public void Run()
			                                                         		{
			                                                         			_ = Svc.{|#0:CreateWithMocks|}(out _);
			                                                         		}
			                                                         	}
			                                                         }
			                                                         """,
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("clock", "MyCode.TimeSource", "only interfaces are mocked"));

	[Test]
	public async Task WhenAFrameworkInterfaceHasNoArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("System.Collections.Generic.IEnumerable<int> values") + """
				namespace MyCode
				{
					public class Program
					{
						public void Run()
						{
							_ = Svc.{|#0:CreateWithMocks|}(out _);
						}
					}
				}
				""",
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("values", "System.Collections.Generic.IEnumerable<int>",
					"interfaces in the System, Microsoft and Windows namespaces are not mocked, except ILogger, ILogger<T> and ILoggerFactory"));

	[Test]
	public async Task WhenAnInterfaceInTheGlobalNamespaceHasNoArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("IGlobal global") + """
			                                    public interface IGlobal { }

			                                    namespace MyCode
			                                    {
			                                    	public class Program
			                                    	{
			                                    		public void Run()
			                                    		{
			                                    			_ = Svc.{|#0:CreateWithMocks|}(out _);
			                                    		}
			                                    	}
			                                    }
			                                    """,
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("global", "IGlobal", "interfaces in the global namespace are not mocked"));

	[Test]
	public async Task WhenAnUnmockableInterfaceHasNoArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.IObsolete dep") + """
			                                          namespace MyCode
			                                          {
			                                          	public interface IObsolete
			                                          	{
			                                          		[System.Obsolete] void Run();
			                                          	}

			                                          	public class Program
			                                          	{
			                                          		public void Run()
			                                          		{
			                                          			_ = Svc.{|#0:CreateWithMocks|}(out _);
			                                          		}
			                                          	}
			                                          }
			                                          """,
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("dep", "MyCode.IObsolete",
					"its mock would not compile without warnings, for example because a member is obsolete"));

	[Test]
	[Arguments("Svc.CreateWithMocks(out _, new TimeSource())")]
	[Arguments("Svc.CreateWithMocks(out _, clock: new TimeSource())")]
	[Arguments("Svc.CreateWithMocks(out _)")]
	public async Task WhenTheDependencyHasAnArgumentOrOnlyOtherParametersAreMissing_ShouldNotBeFlagged(string call)
		=> await Verifier.VerifyAnalyzerAsync(
			GeneratedPrefix(call.Contains("TimeSource") ? "MyCode.TimeSource clock, string name" : "string name") +
			$$"""
			  namespace MyCode
			  {
			  	public class TimeSource { }

			  	public class Program
			  	{
			  		public void Run()
			  		{
			  			_ = {{call}};
			  		}
			  	}
			  }
			  """);

	private static string GeneratedPrefix(string requiredParameters)
		=> $$"""
		     namespace MyCode
		     {
		     	public interface IDep { }

		     	public class Svc { }
		     }

		     namespace Mockolate
		     {
		     	internal static partial class MockExtensionsForSvc
		     	{
		     		extension(MyCode.Svc)
		     		{
		     			public static MyCode.Svc CreateWithMocks(out object mocks, {{requiredParameters}}, global::Mockolate.MockBehavior? mockBehavior = null, MyCode.IDep? dep = null) => null!;
		     		}
		     	}
		     }

		     """;
}
