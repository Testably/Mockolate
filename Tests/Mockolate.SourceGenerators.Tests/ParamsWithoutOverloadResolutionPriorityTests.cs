using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ParamsWithoutOverloadResolutionPriorityTests
{
	private const string Declarations = """
	                                    public interface IParams
	                                    {
	                                        int Only(params string[] args);
	                                        int Log(int level, params string[] args);
	                                        int Objects(params object[] args);
	                                        int LogObjects(int level, params object[] args);
	                                        int OnlyGen<T>(params T[] args);
	                                        int LogGen<T>(int level, params T[] args);
	                                    }

	                                    public delegate int OnlyDelegate(params string[] args);

	                                    public delegate int IntsDelegate(params int[] values);

	                                    public delegate int LogDelegate(int level, params string[] args);

	                                    public delegate int GenericDelegate<T>(int level, params T[] args);

	                                    public delegate int ObjectsDelegate(params object[] args);

	                                    public delegate int LogObjectsDelegate(int level, params object[] args);
	                                    """;

	/// <summary>
	///     A second public declaration of the attribute makes it ambiguous, so the generator sees a consumer without it,
	///     as on .NET 8 or .NET Framework without a polyfill.
	/// </summary>
	private static readonly MetadataReference AmbiguousOverloadResolutionPriority = CSharpCompilation.Create(
			"AmbiguousOverloadResolutionPriority",
			[
				CSharpSyntaxTree.ParseText("""
				                           namespace System.Runtime.CompilerServices;

				                           public sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
				                           {
				                               public int Priority { get; } = priority;
				                           }
				                           """),
			],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
		.ToMetadataReference();

	private static readonly (string, string)[] OnlyStrings =
	[
		("[]", "()"), ("[a]", "(\"a\")"), ("[a,b]", "(\"a\", \"b\")"),
	];

	private static readonly (string, string)[] OnlyInts = [("[]", "()"), ("[1]", "(1)"), ("[1,2]", "(1, 2)"),];

	private static readonly (string, string)[] LevelAndStrings =
	[
		("1:[]", "(1)"), ("1:[a]", "(1, \"a\")"), ("1:[a,b]", "(1, \"a\", \"b\")"),
	];

	private static readonly Group[] Groups =
	[
		new(new OverloadMatrix.Member("IParams", "Only", Probes("sut.Only", OnlyStrings)), """
			() | () matches []
			().AnyParameters() | () matches everything
			("a") | (params string[]) matches [a]
			(It.IsAny<string>()) | (params IParameter<string>[]) matches [a]
			(It.IsAny<string>(), It.Is("b")) | (params IParameter<string>[]) matches [a,b]
			(It.IsAny<string[]>()) | (IParameter<string[]>?) matches everything
			"""),
		new(new OverloadMatrix.Member("IParams", "Log", Probes("sut.Log", LevelAndStrings)), """
			(1) | (int, params string[]) matches 1:[]
			(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			(It.IsAny<int>(), It.IsAny<string>()) | (IParameter<int>?, params IParameter<string>[]) matches 1:[a]
			(It.IsAny<int>(), "a") | (IParameter<int>?, params string[]) matches 1:[a]
			(1, It.IsAny<string[]>()) | (int, IParameter<string[]>?) matches everything
			"""),
		new(new OverloadMatrix.Member("IParams", "Objects", Probes("sut.Objects", OnlyStrings)), """
			() | () matches []
			().AnyParameters() | () matches everything
			(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a]
			"""),
		new(new OverloadMatrix.Member("IParams", "LogObjects", Probes("sut.LogObjects", LevelAndStrings)), """
			(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, params IParameter<object>[]) matches 1:[a]
			"""),
		new(new OverloadMatrix.Member("IParams", "OnlyGen", Probes("sut.OnlyGen<object>", OnlyStrings)), """
			<object>() | () matches []
			<object>().AnyParameters() | () matches everything
			<object>("a") | (params object[]) matches [a]
			<object>(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a]
			<object>(It.IsAny<object[]>()) | (IParameter<object[]>?) matches everything
			<object>("a", It.IsAny<object>()) | (params object[]) matches [a,b]
			"""),
		new(new OverloadMatrix.Member("IParams", "LogGen", Probes("sut.LogGen<object>", LevelAndStrings)), """
			<object>(1) | (int, params object[]) matches 1:[]
			<object>(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			<object>(1, It.IsAny<object>()) | (int, params object[]) matches 1:[a]
			<object>(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, params IParameter<object>?[]) matches 1:[a]
			"""),
		new(new OverloadMatrix.Member("OnlyDelegate", null, Probes("sut", OnlyStrings)), """
			() | () matches []
			().AnyParameters() | () matches everything
			("a") | (params string[]) matches [a]
			(It.IsAny<string>()) | (params IParameter<string>[]) matches [a]
			(It.IsAny<string>(), It.Is("b")) | (params IParameter<string>[]) matches [a,b]
			(It.IsAny<string[]>()) | (IParameter<string[]>?) matches everything
			"""),
		new(new OverloadMatrix.Member("IntsDelegate", null, Probes("sut", OnlyInts)), """
			() | () matches []
			().AnyParameters() | () matches everything
			(1) | (params int[]) matches [1]
			(It.IsAny<int>(), It.Is(2)) | (params IParameter<int>[]) matches [1,2]
			"""),
		new(new OverloadMatrix.Member("LogDelegate", null, Probes("sut", LevelAndStrings)), """
			(1) | (int, params string[]) matches 1:[]
			(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			(It.IsAny<int>(), It.IsAny<string>()) | (IParameter<int>?, params IParameter<string>[]) matches 1:[a]
			(It.IsAny<int>(), "a") | (IParameter<int>?, params string[]) matches 1:[a]
			"""),
		new(new OverloadMatrix.Member("GenericDelegate<string>", null, Probes("sut", LevelAndStrings)), """
			(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			(It.IsAny<int>(), It.IsAny<string>()) | (IParameter<int>?, params IParameter<string>[]) matches 1:[a]
			"""),
		new(new OverloadMatrix.Member("ObjectsDelegate", null, Probes("sut", OnlyStrings)), """
			() | () matches []
			().AnyParameters() | () matches everything
			(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a]
			"""),
		new(new OverloadMatrix.Member("LogObjectsDelegate", null, Probes("sut", LevelAndStrings)), """
			(It.IsAny<int>()) | (IParameter<int>?) matches 1:[]
			(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, params IParameter<object>[]) matches 1:[a]
			"""),
		new(new OverloadMatrix.Member("OnlyDelegate", null, Probes("sut", OnlyStrings)), """
			(null, null) | CS0121
			(default, default) | CS0121
			""", "null literals for the elements are a known gap: they convert to the elements and to the element matchers"),
		new(new OverloadMatrix.Member("LogDelegate", null, Probes("sut", LevelAndStrings)), """
			(It.IsAny<int>(), null, null) | CS0121
			""", "null literals for the elements are a known gap: they convert to the elements and to the element matchers"),
	];

	[Test]
	public async Task WithoutOverloadResolutionPriority_ShouldNotEmitPriorities()
	{
		GeneratorResult result = Generator.RunWithReferences($$"""
		                                                       using Mockolate;

		                                                       namespace MyCode;

		                                                       {{Declarations}}

		                                                       public class Program
		                                                       {
		                                                           public static void Main()
		                                                           {
		                                                               _ = IParams.CreateMock();
		                                                               _ = LogDelegate.CreateMock();
		                                                           }
		                                                       }
		                                                       """, [AmbiguousOverloadResolutionPriority,]);

		await That(result.Diagnostics).IsEmpty();
		await That(result.Sources["Mock.IParams.g.cs"]).DoesNotContain("OverloadResolutionPriority")
			.Because("the test simulates a consumer without OverloadResolutionPriorityAttribute");
		await That(result.Sources["Mock.LogDelegate.g.cs"]).DoesNotContain("OverloadResolutionPriority");
	}

	[Test]
	public async Task WithoutOverloadResolutionPriority_ParamsCallsWithoutParamsArguments_ShouldNotBeAmbiguous()
	{
		(Group Group, string[] Expected)[] groups = Groups.Select(group => (group, Lines(group.Expected))).ToArray();
		string[] results = OverloadMatrix.Run(Declarations, groups
				.SelectMany(x => x.Expected.Select(line => (x.Group.Member, line.Split(" | ")[0]))).ToArray(),
			LanguageVersion.CSharp14, [AmbiguousOverloadResolutionPriority,]);

		int index = 0;
		foreach ((Group group, string[] expected) in groups)
		{
			string[] actual = expected.Select(line => $"{line.Split(" | ")[0]} | {results[index++]}").ToArray();

			await That(actual).IsEqualTo(expected)
				.Because($"on {group.Member.Name ?? group.Member.Type}, {group.Because}");
		}
	}

	private static string[] Lines(string text)
		=> text.Split('\n').Select(line => line.Trim()).ToArray();

	private static (string, string)[] Probes(string member, (string Label, string Arguments)[] probes)
		=> probes.Select(probe => (probe.Label, member + probe.Arguments)).ToArray();

	private sealed record Group(OverloadMatrix.Member Member, string Expected,
		string Because = "an overload that leaves out the params parameter must take a call without params arguments, which the per-element overload and the one that takes the array as values compete for without priorities");
}
