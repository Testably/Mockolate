using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests.SpecialCases;

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
	                                        int Mix<T>(T value, params int[] rest);
	                                        int MixObj(object? value, params int[] rest);
	                                        int In<T>(in T value);
	                                        int NotNull<T>(T value) where T : notnull;
	                                        int New<T>(T value) where T : new();
	                                        int Unmanaged<T>(T value) where T : unmanaged;
	                                    }

	                                    public interface IOuter<TC>
	                                    {
	                                        int G<T>(T value) where T : TC;
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

	private static readonly OverloadMatrix.Member Mix = new("IGen", "Mix",
	[
		("o:a", "sut.Mix<object>(\"a\")"), ("o:a,1", "sut.Mix<object>(\"a\", 1)"), ("o:b", "sut.Mix<object>(\"b\")"),
		("s:a", "sut.Mix<string>(\"a\")"),
	]);

	private static readonly OverloadMatrix.Member MixObj = new("IGen", "MixObj",
	[
		("a", "sut.MixObj(\"a\")"), ("a,1", "sut.MixObj(\"a\", 1)"), ("b", "sut.MixObj(\"b\")"),
	]);

	private static readonly OverloadMatrix.Member In = new("IGen", "In",
	[
		("o:a", "sut.In<object>(\"a\")"), ("o:1", "sut.In<object>(1)"), ("s:a", "sut.In<string>(\"a\")"),
	]);

	private static readonly OverloadMatrix.Member NotNull = new("IGen", "NotNull",
	[
		("o:a", "sut.NotNull<object>(\"a\")"), ("o:1", "sut.NotNull<object>(1)"), ("s:a", "sut.NotNull<string>(\"a\")"),
	]);

	private static readonly OverloadMatrix.Member New = new("IGen", "New",
	[
		("o:x", "sut.New<object>(\"x\")"), ("o:1", "sut.New<object>(1)"), ("i:1", "sut.New<int>(1)"),
	]);

	private static readonly OverloadMatrix.Member Unmanaged = new("IGen", "Unmanaged",
	[
		("0", "sut.Unmanaged<int>(0)"), ("1", "sut.Unmanaged<int>(1)"),
	]);

	private static readonly OverloadMatrix.Member OuterObject = new("IOuter<object>", "G",
	[
		("o:a", "sut.G<object>(\"a\")"), ("o:1", "sut.G<object>(1)"), ("s:a", "sut.G<string>(\"a\")"),
	]);

	private static readonly OverloadMatrix.Member OuterString = new("IOuter<string>", "G",
	[
		("s:a", "sut.G<string>(\"a\")"), ("s:b", "sut.G<string>(\"b\")"),
	]);

	private static readonly Group[] Groups =
	[
		new(Plain, "without type arguments a matcher binds the matcher overload with the type argument it infers", """
			(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			(It.IsAny<int>()) | (IParameter<int>?) matches i:0 i:1
			(It.Is("a")) | (IParameter<string>?) matches s:a
			("a") | (string) matches s:a
			(1) | (int) matches i:1
			"""),
		new(Two, "without type arguments a matcher next to a value binds the matcher overload with the type argument it infers", """
			(1, It.IsAny<string>()) | (int, IParameter<string>?) matches 1,s:a 1,s:null
			(It.IsAny<int>(), It.IsAny<string>()) | (IParameter<int>?, IParameter<string>?) matches 1,s:a 1,s:null
			(It.IsAny<int>(), "a") | (IParameter<int>?, string) matches 1,s:a
			(1, "a") | (int, string) matches 1,s:a
			"""),
		new(Five, "without type arguments a matcher next to values binds the matcher overload above four parameters as well", """
			(1, 2, 3, 4, It.IsAny<string>()) | (int, int, int, int, IParameter<string>?) matches 1234,s:a 1234,s:null
			(1, 2, 3, 4, "a") | (int, int, int, int, string) matches 1234,s:a
			(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<string>?) matches 1234,s:a 1234,s:null
			"""),
		new(Mix, "without type arguments a matcher next to an empty params array binds the matcher overload", """
			(It.IsAny<string>()) | (IParameter<string>?, params IParameter<int>[]) matches s:a
			("a") | (string, params int[]) matches s:a
			"""),
		new(Dep, "without type arguments a matcher for a dependent type parameter binds the matcher overload", """
			("b", It.IsAny<object>()) | (string?, IParameter<object?>?) matches b,5 b,null
			"""),
		new(In, "without type arguments a matcher for an in parameter binds the matcher overload", """
			(It.IsAny<string>()) | (IParameter<string>?) matches s:a
			"""),
		new(Cls, "without type arguments a matcher for a class constraint binds the matcher overload", """
			(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			"""),
		new(NotNull, "without type arguments a matcher for a notnull constraint binds the matcher overload", """
			(It.IsAny<string>()) | (IParameter<string>?) matches s:a
			"""),
		new(Plain, "a matcher that converts to the type argument binds the matcher overload, as for an object parameter", """
			<object>(It.IsAny<object?>()) | (IParameter<object>?) matches o:a o:1 o:null
			<object>(It.IsAny<object>()) | (IParameter<object>?) matches o:a o:1 o:null
			<object>(It.IsAny<string>()) | (IParameter<object>?) matches o:a o:null
			<object>(It.Is<object>("a")) | (IParameter<object>?) matches o:a
			<object>(It.Satisfies<object>(x => x is string)) | (IParameter<object>?) matches o:a
			<object>(value: It.IsAny<object>()) | (IParameter<object>?) matches o:a o:1 o:null
			<object?>(It.IsAny<object?>()) | (IParameter<object?>?) matches o:a o:1 o:null
			<object>(It.IsAny<object>()).AnyParameters() | CS1061
			"""),
		new(Plain, "a parameters match for the only parameter binds the IParameters overload, as for an object parameter", """
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:1 o:null
			<object>(Match.Parameters(p => p[0] is string)) | (IParameters?) matches o:a
			"""),
		new(Plain, "a matcher that does not convert to the parameter type is still a value, as for an object parameter", """
			<object>(It.IsAny<int>()) | (object) matches nothing
			"""),
		new(Plain, "values match as before, and a lone null or default literal stands for that argument, as for an object parameter", """
			<object>("a") | (object) matches o:a
			<object>(null) | (IParameters?) matches o:null
			<object>(default) | (IParameters?) matches o:null
			<object>("a").AnyParameters() | (object) matches o:a o:1 o:null
			<object>(null).AnyParameters() | CS1061
			<string>("a") | (string) matches s:a
			<string>(null) | (IParameters?) matches s:null
			<string>(default) | (IParameters?) matches s:null
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			<string>(Match.AnyParameters()) | (IParameters?) matches s:a s:null
			<string>("a").AnyParameters() | (string) matches s:a s:null
			<string>(null).AnyParameters() | CS1061
			<int>(1) | (int) matches i:1
			<int>(default) | (IParameters?) matches i:0
			<int>(null) | (IParameters?) matches i:0
			<int>(It.IsAny<int>()) | (IParameter<int>?) matches i:0 i:1
			<int>(Match.AnyParameters()) | (IParameters?) matches i:0 i:1
			<int>(1).AnyParameters() | (int) matches i:0 i:1
			<int>(default).AnyParameters() | CS1061
			"""),
		new(Two, "a matcher or a null or default literal for the type parameter binds the matcher overload, and values match as before", """
			<object>(1, It.IsAny<object>()) | (int, IParameter<object>?) matches 1,o:a 1,o:null
			<object>(default, It.Is<object>(null)) | (IParameter<int>?, IParameter<object>?) matches 0,o:null
			<object>(1, "a") | (int, object) matches 1,o:a
			<object>(1, null) | (int, IParameter<object>?) matches 1,o:null
			<object>(1, default) | (int, IParameter<object>?) matches 1,o:null
			<object>(default, default) | (IParameter<int>?, IParameter<object>?) matches 0,o:null
			<object>(It.IsAny<int>(), "a") | (IParameter<int>?, object) matches 1,o:a 2,o:a
			<object>(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, IParameter<object>?) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<object>(Match.AnyParameters()) | (IParameters) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<object>(1, "a").AnyParameters() | (int, object) matches 1,o:a 1,o:null 2,o:a 0,o:null
			<string>(1, "a") | (int, string) matches 1,s:a
			<string>(1, null) | (int, IParameter<string>?) matches 1,s:null
			<string>(1, It.IsAny<string>()) | (int, IParameter<string>?) matches 1,s:a 1,s:null
			<string>(1, null).AnyParameters() | CS1061
			<int>(default, default) | (IParameter<int>?, IParameter<int>?) matches 0,i:0
			<int>(default, default).AnyParameters() | CS1061
			<int>(1, It.IsAny<int>()) | (int, IParameter<int>?) matches 1,i:2
			"""),
		new(Dep, "a matcher or a null literal for a type parameter binds the matcher overload", """
			<string, object>("b", It.IsAny<object?>()) | (string?, IParameter<object?>?) matches b,5 b,null
			<string, object>("b", 5) | (string?, object?) matches b,5
			<string, object>("b", null) | (string?, IParameter<object?>?) matches b,null
			<string, object>(It.IsAny<string?>(), It.IsAny<object?>()) | (IParameter<string?>?, IParameter<object?>?) matches b,5 b,null c,5
			<string, object>(It.IsAny<string?>(), 5) | (IParameter<string?>?, object?) matches b,5 c,5
			<string, object>(Match.AnyParameters()) | (IParameters) matches b,5 b,null c,5
			<string, string>("b", "c") | (string?, string?) matches s:b,s:c
			<string, string>("b", null) | (string?, IParameter<string?>?) matches s:b,s:null
			<string, string>("b", It.IsAny<string?>()) | (string?, IParameter<string?>?) matches s:b,s:c s:b,s:null
			"""),
		new(Cls, "a class constraint can be closed over object", """
			<object>(It.IsAny<object>()) | (IParameter<object>?) matches o:a o:1
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:1
			<object>("a") | (object) matches o:a
			<string>("a") | (string) matches s:a
			<string>(null) | (IParameters?) matches s:null
			<string>(null).AnyParameters() | CS1061
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a s:null
			"""),
		new(Five, "above four parameters a matcher or a null or default literal next to values binds the matcher overload, and values match as before", """
			<object>(1, 2, 3, 4, It.IsAny<object>()) | (int, int, int, int, IParameter<object>?) matches 1234,o:a 1234,o:null
			<object>(1, 2, 3, 4, "a") | (int, int, int, int, object) matches 1234,o:a
			<object>(1, 2, 3, 4, null) | (int, int, int, int, IParameter<object>?) matches 1234,o:null
			<object>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<object>?) matches 1234,o:a 1234,o:null 0000,o:a 0000,o:null
			<object>(Match.AnyParameters()) | (IParameters) matches 1234,o:a 1234,o:null 0000,o:a 0000,o:null
			<object>(default, default, default, default, default) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<object>?) matches 0000,o:null
			<object>(default, default, default, default, default).AnyParameters() | CS1061
			<string>(1, 2, 3, 4, null) | (int, int, int, int, IParameter<string>?) matches 1234,s:null
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
		new(Mix, "a parameters match with an empty trailing params array binds the IParameters overload", """
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:a,1 o:b
			<object>(Match.Parameters(p => p[0] is "a")) | (IParameters?) matches o:a o:a,1
			<object>(Match.Parameters(p => p[1] is int[] { Length: 1, })) | (IParameters?) matches o:a,1
			<object>(Match.AnyParameters()).AnyParameters() | CS1061
			<string>(Match.AnyParameters()) | (IParameters?) matches s:a
			"""),
		new(Mix, "a parameters match next to elements of the params array is a value, as for an object parameter", """
			<object>(Match.AnyParameters(), 1) | (object, params int[]) matches nothing
			"""),
		new(Mix, "matchers and values next to a params array match as for the other methods", """
			<object>(It.IsAny<string>()) | (IParameter<object>?, params IParameter<int>[]) matches o:a o:b
			<object>(It.Is<object>("a"), 1) | (IParameter<object>?, params int[]) matches o:a,1
			<object>("a") | (object, params int[]) matches o:a
			<object>("a", 1) | (object, params int[]) matches o:a,1
			<object>("a").AnyParameters() | (object, params int[]) matches o:a o:a,1 o:b
			"""),
		new(MixObj, "a declared object parameter next to a params array binds the IParameters overload", """
			(Match.AnyParameters()) | (IParameters?) matches everything
			(Match.AnyParameters(), 1) | (object?, params int[]) matches nothing
			("a") | (object?, params int[]) matches a
			"""),
		new(In, "an in parameter takes a matcher like a value parameter", """
			<object>(It.IsAny<string>()) | (IParameter<object>?) matches o:a
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:1
			<object>("a") | (object) matches o:a
			<string>("a") | (string) matches s:a
			"""),
		new(NotNull, "a notnull constraint can be closed over object", """
			<object>(It.IsAny<string>()) | (IParameter<object>?) matches o:a
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:1
			<object>(1) | (object) matches o:1
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a
			"""),
		new(New, "a new() constraint can be closed over object", """
			<object>(It.IsAny<int>()) | (object) matches nothing
			<object>(It.Satisfies<object>(x => x is int)) | (IParameter<object>?) matches o:1
			<object>(Match.AnyParameters()) | (IParameters?) matches o:x o:1
			<int>(1) | (int) matches i:1
			"""),
		new(Unmanaged, "an unmanaged constraint cannot be closed over object and did not change", """
			<int>(1) | (int) matches 1
			<int>(It.IsAny<int>()) | (IParameter<int>?) matches everything
			<int>(Match.AnyParameters()) | (IParameters) matches everything
			"""),
		new(OuterObject, "a constraint to a type parameter of the mocked type that is closed over object can be object", """
			<object>(It.IsAny<string>()) | (IParameter<object>?) matches o:a
			<object>(Match.AnyParameters()) | (IParameters?) matches o:a o:1
			<object>(1) | (object) matches o:1
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches s:a
			"""),
		new(OuterString, "a constraint to a type parameter of the mocked type that is closed over a sealed class cannot be object", """
			<string>("a") | (string) matches s:a
			<string>(It.IsAny<string>()) | (IParameter<string>?) matches everything
			<string>(Match.AnyParameters()) | (IParameters) matches everything
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
	public async Task WithClassicOverloads_ShouldBindAndMatchAsExpected()
		=> await VerifyGroups([..Groups, ..ClassicObjectGroups,], LanguageVersion.CSharp14);

#if NET11_0_OR_GREATER
	[Test]
	public async Task WithUnionOverloads_ShouldBindAndMatchAsExpected()
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
