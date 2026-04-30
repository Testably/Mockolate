using System.Threading.Tasks;
using Verifier = Mockolate.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<Mockolate.Analyzers.MockabilityAnalyzer>;

namespace Mockolate.Analyzers.Tests;

public class MockabilityAnalyzerAccessibilityTests
{
	[Test]
	[Arguments("internal abstract void MyMember();", "Ext.MyExternalType.MyMember()")]
	[Arguments("internal abstract int MyMember { get; set; }", "Ext.MyExternalType.MyMember.get")]
	[Arguments("internal abstract event System.EventHandler MyMember;", "Ext.MyExternalType.MyMember")]
	[Arguments("public abstract string MyMember { get; internal set; }", "Ext.MyExternalType.MyMember.set")]
	public async Task WhenAbstractMemberOfReferencedAssemblyIsNotAccessible_ShouldBeFlagged(
		string member, string reportedMember) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", member),
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					$"the member '{reportedMember}' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	public async Task WhenInterfaceAccessorOfReferencedAssemblyIsNotAccessible_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("interface", "string MyMember { get; internal set; }"),
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					"the member 'Ext.MyExternalType.MyMember.set' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	public async Task WhenInaccessibleAbstractMemberIsInheritedFromABaseType_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			"""
			namespace Ext
			{
				public abstract class MyBaseType
				{
					internal abstract void MyMember();
				}

				public abstract class MyExternalType : MyBaseType
				{
				}
			}
			""",
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					"the member 'Ext.MyBaseType.MyMember()' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	[Arguments("internal abstract void MyMember();", "internal override void MyMember() { }")]
	[Arguments("internal abstract int MyMember { get; set; }", "internal override int MyMember { get; set; }")]
	[Arguments("internal abstract event System.EventHandler MyMember;",
		"internal override event System.EventHandler MyMember;")]
	[Arguments("public abstract string MyMember { get; internal set; }",
		"public override string MyMember { get => null!; internal set { } }")]
	[Arguments("public abstract string MyMember { internal get; set; }",
		"public override string MyMember { internal get => null!; set { } }")]
	[Arguments("private protected abstract int MyMember { get; set; }",
		"private protected override int MyMember { get; set; }")]
	public async Task WhenInaccessibleAbstractMemberIsAlreadyOverridden_ShouldNotBeFlagged(
		string baseMember, string derivedOverride) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			$$"""
			  namespace Ext
			  {
			  	public abstract class MyBaseType
			  	{
			  		{{baseMember}}
			  	}

			  	public abstract class MyExternalType : MyBaseType
			  	{
			  		{{derivedOverride}}
			  	}
			  }
			  """);

	[Test]
	public async Task WhenInaccessibleInterfaceMemberHasADefaultImplementation_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			"""
			namespace Ext
			{
				public interface IMyBaseType
				{
					internal void MyMember();
				}

				public interface MyExternalType : IMyBaseType
				{
					void IMyBaseType.MyMember() { }
				}
			}
			""");

	[Test]
	public async Task WhenInaccessibleAbstractMemberIsReDeclaredAsAbstract_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			"""
			namespace Ext
			{
				public abstract class MyBaseType
				{
					internal abstract void MyMember();
				}

				public abstract class MyExternalType : MyBaseType
				{
					internal abstract override void MyMember();
				}
			}
			""",
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					"the member 'Ext.MyExternalType.MyMember()' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	[Arguments("internal abstract void MyMember();")]
	[Arguments("public abstract string MyMember { get; internal set; }")]
	public async Task WhenInternalsAreVisibleToTheMockAssembly_ShouldNotBeFlagged(string member) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", member, internalsVisibleToTestProject: true));

	[Test]
	[Arguments("internal virtual void MyMember() { }")]
	[Arguments("internal virtual int MyMember { get; set; }")]
	public async Task WhenInaccessibleMemberIsOnlyVirtual_ShouldNotBeFlagged(string member) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("class", member));

	[Test]
	public async Task WhenProtectedInternalAbstractMember_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", "protected internal abstract void MyMember();"));

	[Test]
	[Arguments("protected abstract void MyMember(MyConfiguration configuration);",
		"Ext.MyExternalType.MyMember(Ext.MyExternalType.MyConfiguration)", "Ext.MyExternalType.MyConfiguration")]
	[Arguments("protected abstract MyConfiguration MyMember();", "Ext.MyExternalType.MyMember()",
		"Ext.MyExternalType.MyConfiguration")]
	[Arguments("protected abstract MyConfiguration MyMember { get; set; }", "Ext.MyExternalType.MyMember",
		"Ext.MyExternalType.MyConfiguration")]
	[Arguments("protected abstract void MyMember<T>() where T : MyConfiguration;",
		"Ext.MyExternalType.MyMember<T>()", "Ext.MyExternalType.MyConfiguration")]
	[Arguments("protected abstract System.Collections.Generic.List<MyConfiguration> MyMember();",
		"Ext.MyExternalType.MyMember()", "System.Collections.Generic.List<Ext.MyExternalType.MyConfiguration>")]
	public async Task WhenAbstractMemberSignatureUsesAnInaccessibleType_ShouldBeFlagged(
		string member, string reportedMember, string reportedType) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", $$"""
			                                 {{member}}
			                                 		protected internal class MyConfiguration { }
			                                 """),
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					$"the member '{reportedMember}' must be implemented, but its signature uses the type '{reportedType}', which is not accessible from this assembly")
		);

	[Test]
	public async Task WhenAbstractMemberSignatureUsesATypeVisibleToTheMockAssembly_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", """
			                               protected abstract void MyMember(MyConfiguration configuration);
			                               		protected internal class MyConfiguration { }
			                               """, internalsVisibleToTestProject: true));

	[Test]
	[Arguments("protected abstract void MyMember(MyConfiguration configuration);",
		"protected override void MyMember(MyConfiguration configuration) { }")]
	[Arguments("protected abstract MyConfiguration MyMember { get; set; }",
		"protected override MyConfiguration MyMember { get; set; }")]
	public async Task WhenAbstractMemberWithInaccessibleSignatureIsAlreadyOverridden_ShouldNotBeFlagged(
		string baseMember, string derivedOverride) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			$$"""
			  namespace Ext
			  {
			  	public abstract class MyBaseType
			  	{
			  		{{baseMember}}
			  		protected internal class MyConfiguration { }
			  	}

			  	public class MyExternalType : MyBaseType
			  	{
			  		{{derivedOverride}}
			  	}
			  }
			  """);

	[Test]
	[Arguments("private protected abstract void MyMember();", "Ext.MyExternalType.MyMember()")]
	[Arguments("private protected abstract int MyMember { get; set; }", "Ext.MyExternalType.MyMember.get")]
	[Arguments("private protected abstract event System.EventHandler MyMember;", "Ext.MyExternalType.MyMember")]
	[Arguments("public abstract string MyMember { get; private protected set; }", "Ext.MyExternalType.MyMember.set")]
	public async Task WhenPrivateProtectedAbstractMemberIsNotAccessible_ShouldBeFlagged(
		string member, string reportedMember) => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			MockCreation,
			ExternalType("abstract class", member),
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					$"the member '{reportedMember}' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	[Arguments("private protected abstract void MyMember();")]
	[Arguments("private protected abstract event System.EventHandler MyMember;")]
	[Arguments("public abstract string MyMember { get; private protected set; }")]
	public async Task WhenPrivateProtectedAbstractMemberIsVisibleToTheMockAssembly_ShouldNotBeFlagged(string member)
		=> await Verifier
			.VerifyAnalyzerWithReferencedProjectAsync(
				MockCreation,
				ExternalType("abstract class", member, internalsVisibleToTestProject: true));

	[Test]
	public async Task WhenAdditionalInterfaceHasAnInaccessibleMember_ShouldBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			ImplementingMockCreation,
			ExternalType("interface", "internal void MyMember();"),
			Verifier.Diagnostic(Rules.MockabilityRule)
				.WithLocation(0)
				.WithArguments("Ext.MyExternalType",
					"the member 'Ext.MyExternalType.MyMember()' must be implemented, but it is not accessible from this assembly")
		);

	[Test]
	public async Task WhenAdditionalInterfaceMemberIsVisibleToTheMockAssembly_ShouldNotBeFlagged() => await Verifier
		.VerifyAnalyzerWithReferencedProjectAsync(
			ImplementingMockCreation,
			ExternalType("interface", "internal void MyMember();", internalsVisibleToTestProject: true));

	private const string MockCreation = """
	                                    namespace Mockolate
	                                    {
	                                    	internal static partial class MockExtensionsForMyExternalType
	                                    	{
	                                    		extension(Ext.MyExternalType mock)
	                                    		{
	                                    			public static Ext.MyExternalType CreateMock() => default!;
	                                    		}
	                                    	}
	                                    }

	                                    namespace MyNamespace
	                                    {
	                                    	public class MyClass
	                                    	{
	                                    		public void MyTest()
	                                    		{
	                                    			_ = {|#0:Ext.MyExternalType|}.CreateMock();
	                                    		}
	                                    	}
	                                    }
	                                    """;

	private const string ImplementingMockCreation = """
	                                                namespace Mockolate
	                                                {
	                                                	internal static partial class MockExtensionsForIMyInterface
	                                                	{
	                                                		extension(MyNamespace.IMyInterface mock)
	                                                		{
	                                                			public static MyNamespace.IMyInterface CreateMock() => default!;
	                                                			public MyNamespace.IMyInterface Implementing<TInterface>() => default!;
	                                                		}
	                                                	}
	                                                }

	                                                namespace MyNamespace
	                                                {
	                                                	public interface IMyInterface
	                                                	{
	                                                	}

	                                                	public class MyClass
	                                                	{
	                                                		public void MyTest()
	                                                		{
	                                                			IMyInterface.CreateMock().Implementing<{|#0:Ext.MyExternalType|}>();
	                                                		}
	                                                	}
	                                                }
	                                                """;

	private static string ExternalType(string typeKeyword, string member,
		bool internalsVisibleToTestProject = false)
	{
		string internalsVisibleTo = internalsVisibleToTestProject
			? """[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestProject")]"""
			: "";
		return $$"""
		         {{internalsVisibleTo}}
		         namespace Ext
		         {
		         	public {{typeKeyword}} MyExternalType
		         	{
		         		{{member}}
		         	}
		         }
		         """;
	}
}
