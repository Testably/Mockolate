using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class GenericMethodClosedOverObjectTests
{
	private const string Declarations = """
	                                    public interface IGen
	                                    {
	                                        int Plain<T>(T value);
	                                        int Two<T>(int level, T value);
	                                        int Dep<T, TBase>(T? value, TBase? other) where T : TBase where TBase : class;
	                                        int Cls<T>(T value) where T : class;
	                                        int Five<T>(int a, int b, int c, int d, T value);
	                                        int Strct<T>(T value) where T : struct;
	                                        int Cmp<T>(T value) where T : IComparable;
	                                        int Obj(object? value);
	                                        int TwoObj(int level, object? value);
	                                    }
	                                    """;

	private static readonly OverloadMatrix.Member Plain = new("IGen", "Plain",
	[
		("o:a", "sut.Plain<object>(\"a\")"), ("o:1", "sut.Plain<object>(1)"), ("o:null", "sut.Plain<object>(null!)"),
		("s:a", "sut.Plain<string>(\"a\")"), ("s:null", "sut.Plain<string>(null!)"), ("i:0", "sut.Plain<int>(0)"),
		("i:1", "sut.Plain<int>(1)"),
	]);

	private static readonly OverloadMatrix.Member Two = new("IGen", "Two",
	[
		("1,o:a", "sut.Two<object>(1, \"a\")"), ("1,o:null", "sut.Two<object>(1, null!)"),
		("2,o:a", "sut.Two<object>(2, \"a\")"), ("0,o:null", "sut.Two<object>(0, null!)"),
		("1,s:a", "sut.Two<string>(1, \"a\")"), ("1,s:null", "sut.Two<string>(1, null!)"),
		("0,i:0", "sut.Two<int>(0, 0)"), ("1,i:2", "sut.Two<int>(1, 2)"),
	]);

	private static readonly OverloadMatrix.Member Dep = new("IGen", "Dep",
	[
		("b,5", "sut.Dep<string, object>(\"b\", 5)"), ("b,null", "sut.Dep<string, object>(\"b\", null)"),
		("c,5", "sut.Dep<string, object>(\"c\", 5)"), ("s:b,s:c", "sut.Dep<string, string>(\"b\", \"c\")"),
		("s:b,s:null", "sut.Dep<string, string>(\"b\", null)"),
	]);

	private static readonly OverloadMatrix.Member Cls = new("IGen", "Cls",
	[
		("o:a", "sut.Cls<object>(\"a\")"), ("o:1", "sut.Cls<object>(1)"), ("s:a", "sut.Cls<string>(\"a\")"),
		("s:null", "sut.Cls<string>(null!)"),
	]);

	private static readonly OverloadMatrix.Member Five = new("IGen", "Five",
	[
		("1234,o:a", "sut.Five<object>(1, 2, 3, 4, \"a\")"), ("1234,o:null", "sut.Five<object>(1, 2, 3, 4, null!)"),
		("0000,o:a", "sut.Five<object>(0, 0, 0, 0, \"a\")"), ("0000,o:null", "sut.Five<object>(0, 0, 0, 0, null!)"),
		("1234,s:a", "sut.Five<string>(1, 2, 3, 4, \"a\")"), ("1234,s:null", "sut.Five<string>(1, 2, 3, 4, null!)"),
	]);

	private static readonly OverloadMatrix.Member Strct = new("IGen", "Strct",
	[
		("0", "sut.Strct<int>(0)"), ("1", "sut.Strct<int>(1)"),
	]);

	private static readonly OverloadMatrix.Member Cmp = new("IGen", "Cmp",
	[
		("s:a", "sut.Cmp<string>(\"a\")"), ("s:null", "sut.Cmp<string>(null!)"), ("i:0", "sut.Cmp<int>(0)"),
	]);

	private static readonly OverloadMatrix.Member Obj = new("IGen", "Obj",
	[
		("a", "sut.Obj(\"a\")"), ("1", "sut.Obj(1)"), ("null", "sut.Obj(null)"),
	]);

	private static readonly OverloadMatrix.Member TwoObj = new("IGen", "TwoObj",
	[
		("1,a", "sut.TwoObj(1, \"a\")"), ("1,null", "sut.TwoObj(1, null)"), ("2,a", "sut.TwoObj(2, \"a\")"),
	]);

	private static readonly Group[] Groups =
	[
		new(Plain, "a matcher still binds the overload that takes values, but matches as a matcher", """
			<object>(It.IsAny<object?>()) | (object) matches o:a o:1 o:null
			<object>(It.IsAny<object>()) | (object) matches o:a o:1 o:null
			<object>(It.IsAny<string>()) | (object) matches o:a o:null
			<object>(It.Is<object>("a")) | (object) matches o:a
			<object>(It.Satisfies<object>(x => x is string)) | (object) matches o:a
			<object>(value: It.IsAny<object>()) | (object) matches o:a o:1 o:null
			<object?>(It.IsAny<object?>()) | (object?) matches o:a o:1 o:null
			<object>(It.IsAny<object>()).AnyParameters() | (object) matches o:a o:1 o:null
			"""),
		new(Plain, "a parameters match for the only parameter matches like the IParameters overload", """
			<object>(Match.AnyParameters()) | (object) matches o:a o:1 o:null
			<object>(Match.Parameters(p => p[0] is string)) | (object) matches o:a
			"""),
		new(Plain, "a matcher that does not convert to the parameter type is still a value, as for an object parameter", """
			<object>(It.IsAny<int>()) | (object) matches nothing
			"""),
		new(Plain, "values and null or default literals bind and match as before", """
			<object>("a") | (object) matches o:a
			<object>(null) | (object) matches o:null
			<object>(default) | (object) matches o:null
			<object>("a").AnyParameters() | (object) matches o:a o:1 o:null
			<object>(null).AnyParameters() | (object) matches o:a o:1 o:null
			<string>("a") | (string) matches s:a
			<string>(null) | (string) matches s:null
			<string>(default) | (string) matches s:null
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			<string>(Match.AnyParameters()) | (IParameters) matches s:a s:null
			<string>("a").AnyParameters() | (string) matches s:a s:null
			<string>(null).AnyParameters() | (string) matches s:a s:null
			<int>(1) | (int) matches i:1
			<int>(default) | (int) matches i:0
			<int>(null) | (IParameters) matches i:0 i:1
			<int>(It.IsAny<int>()) | (IParameter<int>?) matches i:0 i:1
			<int>(Match.AnyParameters()) | (IParameters) matches i:0 i:1
			<int>(1).AnyParameters() | (int) matches i:0 i:1
			<int>(default).AnyParameters() | (int) matches i:0 i:1
			"""),
		new(Two, "a matcher next to a value matches as a matcher, and other calls bind and match as before", """
			<object>(1, It.IsAny<object>()) | (int, object) matches 1,o:a 1,o:null
			<object>(default, It.Is<object>(null)) | (int, object) matches 0,o:null
			<object>(1, "a") | (int, object) matches 1,o:a
			<object>(1, null) | (int, object) matches 1,o:null
			<object>(1, default) | (int, object) matches 1,o:null
			<object>(default, default) | (int, object) matches 0,o:null
			<object>(It.IsAny<int>(), "a") | (IParameter<int>?, object) matches 1,o:a 2,o:a
			<object>(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, IParameter<object>?) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<object>(Match.AnyParameters()) | (IParameters) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<object>(1, "a").AnyParameters() | (int, object) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<string>(1, "a") | (int, string) matches 1,s:a
			<string>(1, null) | (int, string) matches 1,s:null
			<string>(1, It.IsAny<string>()) | (int, IParameter<string>?) matches 1,s:a 1,s:null
			<string>(1, null).AnyParameters() | (int, string) matches 1,s:a 1,s:null
			<int>(default, default) | (int, int) matches 0,i:0
			<int>(default, default).AnyParameters() | (int, int) matches 0,i:0 1,i:2
			<int>(1, It.IsAny<int>()) | (int, IParameter<int>?) matches 1,i:2
			"""),
		new(Dep, "only the type parameter that is closed over object takes a matcher as a value", """
			<string, object>("b", It.IsAny<object?>()) | (string?, object?) matches b,5 b,null
			<string, object>("b", 5) | (string?, object?) matches b,5
			<string, object>("b", null) | (string?, object?) matches b,null
			<string, object>(It.IsAny<string?>(), It.IsAny<object?>()) | (IParameter<string?>?, IParameter<object?>?) matches b,5 b,null c,5
			<string, object>(It.IsAny<string?>(), 5) | (IParameter<string?>?, object?) matches b,5 c,5
			<string, object>(Match.AnyParameters()) | (IParameters) matches b,5 b,null c,5
			<string, string>("b", "c") | (string?, string?) matches s:b,s:c
			<string, string>("b", null) | (string?, string?) matches s:b,s:null
			<string, string>("b", It.IsAny<string?>()) | (string?, IParameter<string?>?) matches s:b,s:c s:b,s:null
			"""),
		new(Cls, "a class constraint can be closed over object", """
			<object>(It.IsAny<object>()) | (object) matches o:a o:1
			<object>(Match.AnyParameters()) | (object) matches o:a o:1
			<object>("a") | (object) matches o:a
			<string>("a") | (string) matches s:a
			<string>(null) | (string) matches s:null
			<string>(null).AnyParameters() | (string) matches s:a s:null
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			"""),
		new(Five, "above four parameters a matcher next to values matches as a matcher, and other calls bind and match as before", """
			<object>(1, 2, 3, 4, It.IsAny<object>()) | (int, int, int, int, object) matches 1234,o:a 1234,o:null
			<object>(1, 2, 3, 4, "a") | (int, int, int, int, object) matches 1234,o:a
			<object>(1, 2, 3, 4, null) | (int, int, int, int, object) matches 1234,o:null
			<object>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<object>?) matches 1234,o:a 1234,o:null 0000,o:a 0000,o:null
			<object>(Match.AnyParameters()) | (IParameters) matches 1234,o:a 1234,o:null 0000,o:a 0000,o:null
			<object>(default, default, default, default, default) | (int, int, int, int, object) matches 0000,o:null
			<object>(default, default, default, default, default).AnyParameters() | (int, int, int, int, object) matches 1234,o:a 1234,o:null 0000,o:a 0000,o:null
			<string>(1, 2, 3, 4, null) | (int, int, int, int, string) matches 1234,s:null
			<string>(1, 2, 3, 4, "a").AnyParameters() | (int, int, int, int, string) matches 1234,s:a 1234,s:null
			"""),
		new(Strct, "a struct constraint cannot be closed over object and did not change", """
			<int>(1) | (int) matches 1
			<int>(default) | (int) matches 0
			<int>(null) | (IParameters) matches everything
			<int>(It.IsAny<int>()) | (IParameter<int>?) matches everything
			<int>(default).AnyParameters() | (int) matches everything
			"""),
		new(Cmp, "an interface constraint cannot be closed over object and did not change", """
			<string>("a") | (string) matches s:a
			<string>(null) | (string) matches s:null
			<string>(null).AnyParameters() | (string) matches s:a s:null
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			<int>(default) | (int) matches i:0
			"""),
	];

	private static readonly Group[] ClassicObjectGroups =
	[
		new(Obj, "a declared object parameter did not change", """
			("a") | (object?) matches a
			(null) | (IParameters?) matches null
			(default) | (IParameters?) matches null
			(It.IsAny<object?>()) | (IParameter<object?>?) matches everything
			(It.IsAny<string>()) | (IParameter<object?>?) matches a null
			(It.IsAny<int>()) | (object?) matches nothing
			(Match.AnyParameters()) | (IParameters?) matches everything
			("a").AnyParameters() | (object?) matches everything
			"""),
		new(TwoObj, "a declared object parameter next to another parameter did not change", """
			(1, "a") | (int, object?) matches 1,a
			(1, null) | (int, IParameter<object?>?) matches 1,null
			(1, It.IsAny<object?>()) | (int, IParameter<object?>?) matches 1,a 1,null
			(It.IsAny<int>(), "a") | (IParameter<int>?, object?) matches 1,a 2,a
			(1, "a").AnyParameters() | (int, object?) matches everything
			"""),
	];

	private static readonly Group[] UnionObjectGroups =
	[
		new(Obj, "a declared object parameter did not change", """
			("a") | (ParameterArg<object?>?) matches a
			(null) | (IParameters?) matches null
			(default) | (IParameters?) matches null
			(It.IsAny<object?>()) | (ParameterArg<object?>?) matches everything
			(It.IsAny<string>()) | (ParameterArg<object?>?) matches a null
			(It.IsAny<int>()) | (ParameterArg<object?>?) matches nothing
			(Match.AnyParameters()) | (IParameters?) matches everything
			("a").AnyParameters() | (ParameterArg<object?>?) matches everything
			"""),
		new(TwoObj, "a declared object parameter next to another parameter did not change", """
			(1, "a") | (ParameterArg<int>?, ParameterArg<object?>?) matches 1,a
			(1, null) | (ParameterArg<int>?, ParameterArg<object?>?) matches 1,null
			(1, It.IsAny<object?>()) | (ParameterArg<int>?, ParameterArg<object?>?) matches 1,a 1,null
			(It.IsAny<int>(), "a") | (ParameterArg<int>?, ParameterArg<object?>?) matches 1,a 2,a
			(1, "a").AnyParameters() | (ParameterArg<int>?, ParameterArg<object?>?) matches everything
			"""),
	];

	[Test]
	public async Task GenericMethodClosedOverObject_InClassicMode_ShouldBindAndMatchAsExpected()
		=> await VerifyGroups([..Groups, ..ClassicObjectGroups,], LanguageVersion.CSharp14);

#if NET11_0_OR_GREATER
	[Test]
	public async Task GenericMethodClosedOverObject_InUnionMode_ShouldBindAndMatchAsExpected()
		=> await VerifyGroups([..Groups, ..UnionObjectGroups,], LanguageVersion.CSharp15);
#endif

	private static async Task VerifyGroups(Group[] expectedGroups, LanguageVersion languageVersion)
	{
		(Group Group, string[] Expected)[] groups = expectedGroups
			.Select(group => (group, Lines(group.Expected))).ToArray();
		string[] results = OverloadMatrix.Run(Declarations, groups
			.SelectMany(x => x.Expected.Select(line => (x.Group.Member, line.Split(" | ")[0]))).ToArray(), languageVersion);

		int index = 0;
		foreach ((Group group, string[] expected) in groups)
		{
			string[] actual = expected.Select(line => $"{line.Split(" | ")[0]} | {results[index++]}").ToArray();

			await That(actual).IsEqualTo(expected)
				.Because($"on {group.Member.Name}, {group.Because}");
		}
	}

	private static string[] Lines(string text)
		=> text.Split('\n').Select(line => line.Trim()).ToArray();

	private sealed record Group(OverloadMatrix.Member Member, string Because, string Expected);
}
