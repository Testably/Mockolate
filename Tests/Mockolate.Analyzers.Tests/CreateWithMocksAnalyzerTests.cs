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
			GeneratedPrefix("MyCode.IObsolete obsolete") + """
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
				.WithArguments("obsolete", "MyCode.IObsolete",
					"the generator cannot mock it without a CreateMock call, for example because a member is obsolete or its type arguments nest too deeply; create the mock with CreateMock and pass it"));

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

	[Test]
	public async Task WhenAPositionalArgumentFollowsANamedArgument_ShouldOnlyReportTheMissingOne() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.TimeSource clock, MyCode.TimeSource store, MyCode.TimeSource extra") + """
				namespace MyCode
				{
					public class TimeSource { }

					public class Program
					{
						public void Run()
						{
							_ = Svc.{|#0:CreateWithMocks|}(out _, clock: new TimeSource(), new TimeSource());
						}
					}
				}
				""",
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("extra", "MyCode.TimeSource", "only interfaces are mocked"));

	[Test]
	public async Task WhenOnlyNamedArgumentsArePassed_ShouldReportTheMissingOne() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.TimeSource clock, System.Action callback") + """
				namespace MyCode
				{
					public class TimeSource { }

					public class Program
					{
						public void Run()
						{
							_ = Svc.{|#0:CreateWithMocks|}(mocks: out _, clock: new TimeSource());
						}
					}
				}
				""",
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("callback", "System.Action", "only interfaces are mocked"));

	[Test]
	public async Task WhenAnInterfaceNestedInAGenericTypeHasNoArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.Outer<int>.IInner inner") + """
				namespace MyCode
				{
					public class Outer<T>
					{
						public interface IInner { }
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
				.WithArguments("inner", "MyCode.Outer<int>.IInner", "it is nested in a generic type"));

	[Test]
	public async Task WhenTheCallBindsToTheGeneratedMethodOfAnotherClass_ShouldReportTheMissingDependency() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.TimeSource clock") + """
				namespace MyCode
				{
					public class TimeSource { }
					public class Other { }

					public class Program
					{
						public void Run()
						{
							_ = Svc.{|#0:CreateWithMocks|}(out _);
						}
					}
				}

				namespace Mockolate
				{
					internal static partial class Mock
					{
						extension(global::MyCode.Other)
						{
							public static global::MyCode.Other CreateWithMocks(out object mocks, global::Mockolate.MockBehavior? mockBehavior = null) { mocks = null!; return null!; }
						}
					}
				}
				""",
			Verifier.Diagnostic("Mockolate0009")
				.WithLocation(0)
				.WithArguments("clock", "MyCode.TimeSource", "only interfaces are mocked"));

	[Test]
	public async Task WhenAPropertyHidesTheClass_ShouldReportTheMissingDependency() => await Verifier
		.VerifyAnalyzerAsync(
			GeneratedPrefix("MyCode.TimeSource clock") + """
				namespace MyCode
				{
					public class TimeSource { }

					public class Program
					{
						public Svc Svc { get; set; } = null!;

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
	public async Task WhenTheClassIsFileLocal_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public interface IDep { }

				file class Svc(IDep dep) { }

				public class Program
				{
					public void Run()
					{
						_ = {|#0:Svc|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("MyCode.Svc", "it is file-local"));

	[Test]
	public async Task WhenTheClassIsNestedInAGenericType_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public interface IDep { }

				public class Outer<T>
				{
					public class Svc(IDep dep) { }
				}

				public class Program
				{
					public void Run()
					{
						_ = {|#0:Outer<int>.Svc|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("MyCode.Outer<int>.Svc", "it is generic or nested in a generic type"));

	[Test]
	public async Task WhenTheOnlyConstructorUsesARefStructTypeArgument_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public ref struct MyRef { }

				public class Svc(System.Func<MyRef, int> callback) { }

				public class Program
				{
					public void Run()
					{
						_ = {|#0:Svc|}.CreateWithMocks(out _);
					}
				}
			}
			""",
			Verifier.Diagnostic("Mockolate0008")
				.WithLocation(0)
				.WithArguments("MyCode.Svc",
					"it has no accessible constructor without ref, out, pointer or ref struct parameters that is not obsolete and sets all required members"));

	[Test]
	public async Task WhenAGenericTypeHasTheContainerName_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerAsync(
			"""
			namespace MyCode
			{
				public interface IDep { }

				public class Svc(IDep dep) { }

				public class SvcMocks<T> { }

				public class Program
				{
					public void Run()
					{
						_ = Svc.CreateWithMocks(out _);
					}
				}
			}
			""");

	private static string GeneratedPrefix(string requiredParameters)
		=> $$"""
		     using Mockolate;

		     namespace MyCode
		     {
		     	public interface IDep { }

		     	public class Svc { }

		     	internal sealed class SvcMocks { }
		     }

		     namespace Mockolate
		     {
		     	internal static partial class Mock
		     	{
		     		extension(global::MyCode.Svc)
		     		{
		     			public static global::MyCode.Svc CreateWithMocks(out global::MyCode.SvcMocks mocks, {{requiredParameters}}, global::Mockolate.MockBehavior? mockBehavior = null, global::MyCode.IDep? dep = null) { mocks = null!; return null!; }
		     		}
		     	}
		     }

		     """;
}
