using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ObjectLookAlikeTests
{
	private static readonly LanguageVersion[] LanguageVersions =
	[
		LanguageVersion.CSharp14,
#if NET11_0_OR_GREATER
		LanguageVersion.CSharp15,
#endif
	];

	[Test]
	[Arguments("void Equals(object other);", "public new void Equals(object other)")]
	[Arguments("int Equals(object other);", "public new int Equals(object other)")]
	[Arguments("Task<bool> Equals(object other);",
		"public new global::System.Threading.Tasks.Task<bool> Equals(object other)")]
	[Arguments("static abstract bool Equals(object other);", "public static new bool Equals(object other)")]
	[Arguments("static virtual bool Equals(object other) => true;", "public static new bool Equals(object other)")]
	[Arguments("string GetHashCode();", "public new string GetHashCode()")]
	[Arguments("void GetHashCode();", "public new void GetHashCode()")]
	[Arguments("int? GetHashCode();", "public new int? GetHashCode()")]
	[Arguments("bool Equals(object? obj); string GetHashCode();", "public new string GetHashCode()")]
	[Arguments("ref int GetHashCode();", "public new ref int GetHashCode()")]
	[Arguments("ref readonly int GetHashCode();", "public new ref readonly int GetHashCode()")]
	[Arguments("static abstract int GetHashCode();", "public static new int GetHashCode()")]
	[Arguments("static virtual int GetHashCode() => 1;", "public static new int GetHashCode()")]
	[Arguments("object ToString();", "public new object ToString()")]
	[Arguments("static abstract string ToString();", "public static new string ToString()")]
	[Arguments("static virtual string ToString() => \"x\";", "public static new string ToString()")]
	[Arguments("bool Equals(object a, object b);", "public new bool Equals(object a, object b)")]
	[Arguments("static abstract bool Equals(object a, object b);", "public static new bool Equals(object a, object b)")]
	[Arguments("bool ReferenceEquals(object a, object b);", "public new bool ReferenceEquals(object a, object b)")]
	[Arguments("Type GetType();", "public new global::System.Type GetType()")]
	[Arguments("object MemberwiseClone();", "public new object MemberwiseClone()")]
	[Arguments("int ToString { get; set; }", "public new int ToString")]
	[Arguments("static abstract int Equals { get; }", "public static new int Equals")]
	[Arguments("event Action GetHashCode;", "public new event global::System.Action GetHashCode")]
	[Arguments("static abstract event Action GetType;", "public static new event global::System.Action GetType")]
	public async Task MemberThatHidesAnObjectMember_ShouldBeDeclaredWithNew(string member, string expectedDeclaration)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generate(member, languageVersion);

			await That(result.Diagnostics).IsEmpty()
				.Because($"a member that cannot override the object member must hide it in {languageVersion}");
			await That(result.Sources).ContainsKey("Mock.IFoo.g.cs").WhoseValue
				.Contains(expectedDeclaration);
		}
	}

	[Test]
	[Arguments("bool Equals(in object other);", "public bool Equals(in object other)")]
	[Arguments("bool Equals(ref object other);", "public bool Equals(ref object other)")]
	[Arguments("bool Equals(out object other);", "public bool Equals(out object other)")]
	[Arguments("bool Equals<T>(object other);", "public bool Equals<T>(object other)")]
	[Arguments("string ToString(int format);", "public string ToString(int format)")]
	public async Task MemberWithOtherSignature_ShouldNeitherOverrideNorHide(string member, string expectedDeclaration)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generate(member, languageVersion);

			await That(result.Diagnostics).IsEmpty()
				.Because($"a member with another signature is an overload of the object member in {languageVersion}");
			await That(result.Sources).ContainsKey("Mock.IFoo.g.cs").WhoseValue
				.Contains(expectedDeclaration);
		}
	}

	[Test]
	[Arguments("bool Equals(object? obj);", "public override bool Equals(object? obj)")]
	[Arguments("bool Equals(dynamic obj);", "public override bool Equals(object obj)")]
	[Arguments("int GetHashCode();", "public override int GetHashCode()")]
	[Arguments("string? ToString();", "public override string? ToString()")]
	public async Task ObjectMember_ShouldBeOverridden(string member, string expectedDeclaration)
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generate(member, languageVersion);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.IFoo.g.cs").WhoseValue
				.Contains(expectedDeclaration);
		}
	}

	[Test]
	public async Task LookAlikeOfTypeParameterType_ShouldOnlyOverrideWhenClosedOverTheObjectMemberType()
	{
		foreach (LanguageVersion languageVersion in LanguageVersions)
		{
			GeneratorResult result = Generator.Run("""
			                                       #nullable enable
			                                       using Mockolate;

			                                       namespace MyCode;

			                                       public class Program
			                                       {
			                                           public static void Main(string[] args)
			                                           {
			                                               _ = IFoo<int>.CreateMock();
			                                               _ = IFoo<string>.CreateMock();
			                                           }

			                                           public static void Open<T>()
			                                           {
			                                               _ = IFoo<T>.CreateMock();
			                                           }
			                                       }

			                                       public interface IFoo<T>
			                                       {
			                                           T GetHashCode();
			                                       }
			                                       """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty();
			await That(result.Sources).ContainsKey("Mock.IFoo_int.g.cs").WhoseValue
				.Contains("public override int GetHashCode()");
			await That(result.Sources).ContainsKey("Mock.IFoo_string.g.cs").WhoseValue
				.Contains("public new string GetHashCode()");
		}
	}

	[Test]
	public async Task LookAlikes_InCombinationAndInheritedMocks_ShouldCompileWithoutDiagnostics()
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
			                                               _ = IFoo.CreateMock();
			                                               _ = IFooDerived.CreateMock();
			                                               _ = IFoo.CreateMock().Implementing<IOther>();
			                                               _ = IOther.CreateMock().Implementing<IFoo>();
			                                           }
			                                       }

			                                       public interface IFoo
			                                       {
			                                           void Equals(object? other);
			                                           bool Equals(object? a, object? b);
			                                           string GetHashCode();
			                                           object ToString();
			                                           Type GetType();
			                                       }

			                                       public interface IFooDerived : IFoo
			                                       {
			                                           new bool Equals(object? other);
			                                           new int GetHashCode();
			                                       }

			                                       public interface IOther
			                                       {
			                                           string? ToString();
			                                           int ReferenceEquals { get; }
			                                       }
			                                       """, languageVersion, null);

			await That(result.Diagnostics).IsEmpty()
				.Because($"additional interfaces are implemented explicitly and cannot hide object members in {languageVersion}");
		}
	}

	private static GeneratorResult Generate(string member, LanguageVersion languageVersion)
		=> Generator.Run($$"""
		                   #nullable enable
		                   using System;
		                   using System.Threading.Tasks;
		                   using Mockolate;

		                   namespace MyCode;

		                   public class Program
		                   {
		                       public static void Main(string[] args)
		                       {
		                           _ = IFoo.CreateMock();
		                           _ = IFooDerived.CreateMock();
		                       }
		                   }

		                   public interface IFoo
		                   {
		                       {{member}}
		                   }

		                   public interface IFooDerived : IFoo
		                   {
		                   }
		                   """, languageVersion, null);
}
