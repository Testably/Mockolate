using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class MembersHidingOtherKindTests
{
	private static readonly LanguageVersion[] LanguageVersions =
	[
		LanguageVersion.CSharp14,
#if NET11_0_OR_GREATER
		LanguageVersion.CSharp15,
#endif
	];

	[Test]
	[Arguments("void Run();", "new int Run { get; }", "int Run", "void global::MyCode.IFoo.Run()")]
	[Arguments("void Run(int value);", "new int Run { get; set; }", "int Run", "void global::MyCode.IFoo.Run(int value)")]
	[Arguments("int Run { get; }", "new event Action Run;", "event global::System.Action Run", "int global::MyCode.IFoo.Run")]
	[Arguments("void Run();", "new event Action Run;", "event global::System.Action Run", "void global::MyCode.IFoo.Run()")]
	[Arguments("int Run { get; }", "new void Run();", "void Run()", "int global::MyCode.IFoo.Run")]
	[Arguments("event Action Run;", "new void Run(int value);", "void Run(int value)", "event global::System.Action global::MyCode.IFoo.Run")]
	[Arguments("event Action Run;", "new int Run { get; }", "int Run", "event global::System.Action global::MyCode.IFoo.Run")]
	[Arguments("int Run { get; }", "new void Run<T>(T value);", "void Run<T>(T value)", "int global::MyCode.IFoo.Run")]
	[Arguments("string ToString();", "new int ToString { get; }", "int ToString", "string global::MyCode.IFoo.ToString()")]
	[Arguments("int GetHashCode();", "new event Action GetHashCode;", "event global::System.Action GetHashCode", "int global::MyCode.IFoo.GetHashCode()")]
	[Arguments("int ToString { get; }", "new string ToString();", "public override string ToString()", "int global::MyCode.IFoo.ToString")]
	public async Task MemberHidingInheritedMemberOfOtherKind_ShouldImplementHiddenMemberExplicitly(
		string hiddenMember, string hidingMember, string expectedHiding, string expectedHidden)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generator.Run($$"""
			                                         #nullable enable
			                                         using System;
			                                         using Mockolate;

			                                         namespace MyCode;

			                                         public class Program
			                                         {
			                                             public static void Main(string[] args)
			                                             {
			                                                 _ = IBar.CreateMock();
			                                             }
			                                         }

			                                         public interface IFoo
			                                         {
			                                             {{hiddenMember}}
			                                         }

			                                         public interface IBar : IFoo
			                                         {
			                                             {{hidingMember}}
			                                         }
			                                         """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty()
				.Because($"the hidden member must not clash with the hiding member in {languageVersion}");
			await That(result.Sources).ContainsKey("Mock.IBar.g.cs").WhoseValue
				.Contains(expectedHiding).And
				.Contains(expectedHidden)
				.Because($"the hidden member is implemented explicitly in {languageVersion}");
		}
	}

	[Test]
	[Arguments("public virtual void Run() { }", "public new virtual int Run => 0;", "public override int Run", "void Run()")]
	[Arguments("public virtual int Run { get; set; }", "public new virtual void Run() { }", "public override void Run()", "int Run")]
	[Arguments("public virtual event Action? Run { add { } remove { } }", "public new virtual int Run => 0;", "public override int Run", "event global::System.Action? Run")]
	[Arguments("public virtual void Run(int value) { }", "public new virtual event Action? Run { add { } remove { } }", "public override event global::System.Action? Run", "void Run(int value)")]
	[Arguments("public override string ToString() => string.Empty;", "public new virtual int ToString => 0;", "public override int ToString", "string ToString()")]
	public async Task ClassMemberHidingInheritedMemberOfOtherKind_ShouldNotOverrideHiddenMember(
		string hiddenMember, string hidingMember, string expectedHiding, string unexpectedHidden)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generator.Run($$"""
			                                         #nullable enable
			                                         using System;
			                                         using Mockolate;

			                                         namespace MyCode;

			                                         public class Program
			                                         {
			                                             public static void Main(string[] args)
			                                             {
			                                                 _ = Bar.CreateMock();
			                                             }
			                                         }

			                                         public class Foo
			                                         {
			                                             {{hiddenMember}}
			                                         }

			                                         public class Bar : Foo
			                                         {
			                                             {{hidingMember}}
			                                         }
			                                         """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty()
				.Because($"the hidden member cannot be overridden in {languageVersion}");
			await That(result.Sources).ContainsKey("Mock.Bar.g.cs").WhoseValue
				.Contains(expectedHiding).And
				.DoesNotContain($"override {unexpectedHidden}");
		}
	}

	[Test]
	[Arguments("public new int Run => 0;", false)]
	[Arguments("public void Run(int value) { }", true)]
	[Arguments("private new int Run => 0;", true)]
	public async Task ClassMemberOfOtherKind_ShouldOnlyPreventOverrideWhenItHidesTheMemberFromTheMock(
		string hidingMember, bool expectOverride)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generator.Run($$"""
			                                         #nullable enable
			                                         using System;
			                                         using Mockolate;

			                                         namespace MyCode;

			                                         public class Program
			                                         {
			                                             public static void Main(string[] args)
			                                             {
			                                                 _ = Bar.CreateMock();
			                                             }
			                                         }

			                                         public class Foo
			                                         {
			                                             public virtual void Run() { }
			                                         }

			                                         public class Bar : Foo
			                                         {
			                                             {{hidingMember}}
			                                         }
			                                         """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources["Mock.Bar.g.cs"].Contains("public override void Run()")).IsEqualTo(expectOverride)
				.Because($"only a member of another kind that the mock sees hides the method in {languageVersion}");
		}
	}

	[Test]
	public async Task SiblingMembersOfOtherKind_ShouldImplementSecondMemberExplicitly()
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generator.Run("""
			                                       #nullable enable
			                                       using System;
			                                       using Mockolate;

			                                       namespace MyCode;

			                                       public class Program
			                                       {
			                                           public static void Main(string[] args)
			                                           {
			                                               _ = IBar.CreateMock();
			                                           }
			                                       }

			                                       public interface IFoo
			                                       {
			                                           void Run();
			                                       }

			                                       public interface IOther
			                                       {
			                                           int Run { get; }
			                                       }

			                                       public interface IBar : IFoo, IOther
			                                       {
			                                       }
			                                       """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty()
				.Because($"the members of both interfaces must not clash in {languageVersion}");
			await That(result.Sources["Mock.IBar.g.cs"])
				.Contains("public void Run()").And
				.Contains("int global::MyCode.IOther.Run");
		}
	}
}
