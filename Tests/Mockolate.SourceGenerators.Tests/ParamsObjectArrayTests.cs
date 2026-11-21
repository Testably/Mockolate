using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Mockolate.SourceGenerators.Tests;

public sealed class ParamsObjectArrayTests
{
	private const string Declarations = """
	                                    public interface ILog
	                                    {
	                                        int Log(int level, params object[] args);
	                                        int Only(params object[] args);
	                                        int Five(int a, int b, int c, int d, params object[] args);
	                                        int Opt(int a, int b = 5, params object[] args);
	                                        int Ov(object value);
	                                        int Ov(params object[] args);
	                                        int GenOv<T>(T value);
	                                        int GenOv(params object[] args);
	                                        int Mix(int a, int b);
	                                        int Mix(params object[] args);
	                                        int Pair(string text);
	                                        int Pair(int level, params object[] args);
	                                        int Tri(object value);
	                                        int Tri(int a, int b);
	                                        int Tri(params object[] args);
	                                        int Nullable(int level, params object?[]? args);
	                                        int OnlyNullable(params object?[]? args);
	                                        int Dyn(int level, params dynamic[] args);
	                                        int OnlyDyn(params dynamic[] args);
	                                        int Gen<T>(int level, params T[] args);
	                                        int OnlyGen<T>(params T[] args);
	                                        int OvGen<T>(params T[] args);
	                                        int OvGen<T>(int a);
	                                        int Str(int level, params string[] args);
	                                        int OnlyStr(params string[] args);
	                                    }

	                                    public interface IG<T>
	                                    {
	                                        int LogG(int level, params T[] args);
	                                        int OnlyG(params T[] args);
	                                    }

	                                    public delegate int LogDelegate(int level, params object[] args);

	                                    public delegate int OnlyDelegate(params object[] args);

	                                    public delegate int StrDelegate(int level, params string[] args);

	                                    public delegate int OnlyStrDelegate(params string[] args);
	                                    """;

	private static readonly (string, string)[] LevelAndArgs =
	[
		("0:[]", "(0)"), ("1:[]", "(1)"), ("0:[a]", "(0, \"a\")"), ("1:[a]", "(1, \"a\")"), ("1:[b]", "(1, \"b\")"),
		("1:[a,2]", "(1, \"a\", 2)"), ("1:[a,3]", "(1, \"a\", 3)"), ("0:null", "(0, null!)"), ("1:null", "(1, null!)"),
		("1:[null]", "(1, (object)null!)"), ("1:[null,null]", "(1, null!, null!)"),
	];

	private static readonly (string, string)[] OnlyArgs =
	[
		("[]", "()"), ("[a]", "(\"a\")"), ("[b]", "(\"b\")"), ("[a,2]", "(\"a\", 2)"), ("null", "(null!)"),
		("[null]", "((object)null!)"), ("[null,null]", "(null!, null!)"), ("[null,a]", "(null!, \"a\")"),
	];

	private static readonly OverloadMatrix.Member Log = new("ILog", "Log", Probes("sut.Log", LevelAndArgs));
	private static readonly OverloadMatrix.Member Only = new("ILog", "Only", Probes("sut.Only", OnlyArgs));

	private static readonly OverloadMatrix.Member Five = new("ILog", "Five",
	[
		("0000:[]", "sut.Five(0, 0, 0, 0)"), ("1234:[]", "sut.Five(1, 2, 3, 4)"),
		("0000:[a]", "sut.Five(0, 0, 0, 0, \"a\")"), ("1234:[a]", "sut.Five(1, 2, 3, 4, \"a\")"),
		("1234:[a,2]", "sut.Five(1, 2, 3, 4, \"a\", 2)"), ("1234:null", "sut.Five(1, 2, 3, 4, null!)"),
		("1234:[null]", "sut.Five(1, 2, 3, 4, (object)null!)"),
	]);

	private static readonly OverloadMatrix.Member Opt = new("ILog", "Opt",
	[
		("1,5:[]", "sut.Opt(1)"), ("1,0:[]", "sut.Opt(1, 0)"), ("1,2:[]", "sut.Opt(1, 2)"),
		("1,5:[a]", "sut.Opt(1, 5, \"a\")"), ("1,0:[a]", "sut.Opt(1, 0, \"a\")"), ("1,0:null", "sut.Opt(1, 0, null!)"),
		("0,5:[]", "sut.Opt(0)"), ("0,0:[]", "sut.Opt(0, 0)"),
	]);

	private static readonly OverloadMatrix.Member Ov = new("ILog", "Ov",
	[
		("object:a", "sut.Ov(\"a\")"), ("object:null", "sut.Ov((object)null!)"), ("array:[]", "sut.Ov()"),
		("array:[a]", "sut.Ov(new object[] { \"a\", })"), ("array:[a,b]", "sut.Ov(\"a\", \"b\")"),
		("array:null", "sut.Ov((object[])null!)"),
	]);

	private static readonly OverloadMatrix.Member GenOv = new("ILog", "GenOv",
	[
		("t:a", "sut.GenOv(\"a\")"), ("t:1", "sut.GenOv(1)"), ("array:[]", "sut.GenOv()"),
		("array:[a,b]", "sut.GenOv(\"a\", \"b\")"),
	]);

	private static readonly OverloadMatrix.Member Mix = new("ILog", "Mix",
	[
		("ints:1,2", "sut.Mix(1, 2)"), ("array:[]", "sut.Mix()"), ("array:[a,1]", "sut.Mix(\"a\", 1)"),
		("array:[null,1]", "sut.Mix(null!, 1)"),
	]);

	private static readonly OverloadMatrix.Member Pair = new("ILog", "Pair",
	[
		("text:a", "sut.Pair(\"a\")"), ("0:[a]", "sut.Pair(0, \"a\")"), ("1:[a]", "sut.Pair(1, \"a\")"),
		("1:[a,2]", "sut.Pair(1, \"a\", 2)"), ("1:null", "sut.Pair(1, null!)"),
	]);

	private static readonly OverloadMatrix.Member Tri = new("ILog", "Tri",
	[
		("object:a", "sut.Tri(\"a\")"), ("ints:1,2", "sut.Tri(1, 2)"), ("array:[a,b]", "sut.Tri(\"a\", \"b\")"),
		("array:[null,1]", "sut.Tri(null!, 1)"),
	]);

	private static readonly OverloadMatrix.Member Nullable =
		new("ILog", "Nullable", Probes("sut.Nullable", LevelAndArgs));

	private static readonly OverloadMatrix.Member OnlyNullable =
		new("ILog", "OnlyNullable", Probes("sut.OnlyNullable", OnlyArgs));

	private static readonly OverloadMatrix.Member Dyn = new("ILog", "Dyn", Probes("sut.Dyn", LevelAndArgs));
	private static readonly OverloadMatrix.Member OnlyDyn = new("ILog", "OnlyDyn", Probes("sut.OnlyDyn", OnlyArgs));
	private static readonly OverloadMatrix.Member Gen = new("ILog", "Gen", Probes("sut.Gen<object>", LevelAndArgs));
	private static readonly OverloadMatrix.Member OnlyGen = new("ILog", "OnlyGen", Probes("sut.OnlyGen<object>", OnlyArgs));
	private static readonly OverloadMatrix.Member OvGen = new("ILog", "OvGen", Probes("sut.OvGen<object>", OnlyArgs));

	private static readonly (string, string)[] LevelAndStrings =
	[
		("0:[]", "(0)"), ("1:[]", "(1)"), ("0:[a]", "(0, \"a\")"), ("1:[a]", "(1, \"a\")"), ("1:[a,b]", "(1, \"a\", \"b\")"),
		("1:null", "(1, null!)"),
	];

	private static readonly (string, string)[] OnlyStrings =
	[
		("[]", "()"), ("[a]", "(\"a\")"), ("[a,b]", "(\"a\", \"b\")"), ("null", "(null!)"), ("[null,null]", "(null!, null!)"),
	];

	private static readonly OverloadMatrix.Member Str = new("ILog", "Str", Probes("sut.Str", LevelAndStrings));
	private static readonly OverloadMatrix.Member OnlyStr = new("ILog", "OnlyStr", Probes("sut.OnlyStr", OnlyStrings));

	private static readonly OverloadMatrix.Member LogG = new("IG<object>", "LogG", Probes("sut.LogG", LevelAndArgs));
	private static readonly OverloadMatrix.Member OnlyG = new("IG<object>", "OnlyG", Probes("sut.OnlyG", OnlyArgs));

	private static readonly Group[] Groups =
	[
		new(GenOv, "an overload with a type parameter decides like the call on the mock does", """
			("a") | (string) matches t:a
			(1) | (int) matches t:1
			() | () matches array:[]
			("a", "b") | (params object[]) matches array:[a,b]
			(It.IsAny<string>()) | (IParameter<string>?) matches t:a
			"""),
		new(Log, "values and arrays bind the all-values overload and match by value", """
			(1) | (int, params object[]) matches 1:[]
			(1, "a") | (int, params object[]) matches 1:[a]
			(1, "a", 2) | (int, params object[]) matches 1:[a,2]
			(1, new object[] { "a", 2, }) | (int, params object[]) matches 1:[a,2]
			(1, new string[] { "a", }) | (int, params object[]) matches 1:[a]
			(1, (object)null!) | (int, params object[]) matches 1:[null]
			(level: 1) | (int, params object[]) matches 1:[]
			(1).AnyParameters() | (int, params object[]) matches everything
			(1, "a").AnyParameters() | (int, params object[]) matches everything
			"""),
		new(Log, "a matcher for the array type matches the whole array instead of one element", """
			(1, It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(1, It.Satisfies<object[]>(a => a?.Length == 2)) | (int, IParameter<object[]>?) matches 1:[a,2] 1:[a,3] 1:[null,null]
			(level: 1, args: It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(default, It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 0:[] 0:[a] 0:null
			(It.IsAny<int>(), It.IsAny<object[]>()) | (IParameter<int>?, IParameter<object[]>?) matches everything
			(1, It.IsAny<string[]>()) | (int, IParameter<object[]>?) matches 1:null
			"""),
		new(Log, "a default literal for another parameter keeps meaning its value", """
			(default) | (int, params object[]) matches 0:[]
			(default, "a") | (int, params object[]) matches 0:[a]
			(default, "a").AnyParameters() | (int, params object[]) matches everything
			"""),
		new(Log, "a null literal for the array means a null array", """
			(1, null) | (int, IParameter<object[]>?) matches 1:null
			(1, default) | (int, IParameter<object[]>?) matches 1:null
			(default, default) | (int, IParameter<object[]>?) matches 0:null
			(level: 1, args: null) | (int, IParameter<object[]>?) matches 1:null
			(args: null, level: 1) | (int, IParameter<object[]>?) matches 1:null
			"""),
		new(Log, "the overloads that take matchers for the other parameters bind as before", """
			(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, params IParameter<object>[]) matches 0:[a] 1:[a] 1:[b] 1:[null]
			(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(2)) | (IParameter<int>?, params IParameter<object>[]) matches 1:[a,2]
			(It.IsAny<int>(), null, null) | (IParameter<int>?, params IParameter<object>[]) matches 1:[null,null]
			(It.IsAny<int>(), "a") | (IParameter<int>?, params object[]) matches 0:[a] 1:[a]
			(It.IsAny<int>(), "a", 2) | (IParameter<int>?, params object[]) matches 1:[a,2]
			(It.IsAny<int>()) | (IParameter<int>?, params IParameter<object>[]) matches 0:[] 1:[]
			(null) | (IParameters) matches everything
			(null, "a") | (IParameter<int>?, params object[]) matches 0:[a]
			(Match.AnyParameters()) | (IParameters) matches everything
			"""),
		new(Log, "an element matcher next to values and a null array typed as such are known gaps that did not change", """
			(1, It.IsAny<object>()) | (int, params object[]) matches nothing
			(1, "a", It.IsAny<object>()) | (int, params object[]) matches nothing
			(1, It.IsAny<string>()) | (int, params object[]) matches nothing
			(1, (object[])null!) | (int, params object[]) throws ArgumentNullException
			"""),
		new(Log, "these calls do not compile: two equally good matcher overloads, or AnyParameters after a matcher", """
			(It.IsAny<int>(), null) | CS0121
			(null, null) | CS0121
			(1, It.IsAny<object[]>()).AnyParameters() | CS1061
			"""),
		new(Only, "values and arrays bind the all-values overload, and no arguments the overload without parameters", """
			() | () matches []
			("a") | (params object[]) matches [a]
			("a", 2) | (params object[]) matches [a,2]
			(new object[] { "a", 2, }) | (params object[]) matches [a,2]
			(new string[] { "a", }) | (params object[]) matches [a]
			((object)null!) | (params object[]) matches [null]
			(null, "a") | (params object[]) matches [null,a]
			().AnyParameters() | () matches everything
			("a").AnyParameters() | (params object[]) matches everything
			"""),
		new(Only, "a matcher for the array type matches the whole array instead of one element", """
			(It.IsAny<object[]>()) | (IParameter<object[]>?) matches everything
			(It.Satisfies<object[]>(a => a?.Length == 2)) | (IParameter<object[]>?) matches [a,2] [null,null] [null,a]
			(args: It.IsAny<object[]>()) | (IParameter<object[]>?) matches everything
			(It.IsAny<string[]>()) | (IParameter<object[]>?) matches null
			"""),
		new(Only, "matchers for the element type match element by element", """
			(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a] [b] [null]
			(It.Is<object>("a")) | (params IParameter<object>?[]) matches [a]
			(It.IsAny<object>(), It.IsAny<object>()) | (params IParameter<object>?[]) matches [a,2] [null,null] [null,a]
			(It.IsAny<string>()) | (params IParameter<object>?[]) matches [a] [b] [null]
			"""),
		new(Only, "a lone null literal means a null array and several null literals mean null elements", """
			(null) | (IParameter<object[]>?) matches null
			(default) | (IParameter<object[]>?) matches null
			(args: null) | (IParameter<object[]>?) matches null
			(null, null) | (params IParameter<object>?[]) matches [null,null]
			(default, default) | (params IParameter<object>?[]) matches [null,null]
			"""),
		new(Only, "the IParameters overload takes a parameters match, and a null one matches any call", """
			(Match.AnyParameters()) | (IParameters) matches everything
			(parameters: Match.AnyParameters()) | (IParameters) matches everything
			(parameters: null) | (IParameters) matches everything
			"""),
		new(Only, "an element matcher next to values and a null array typed as such are known gaps that did not change", """
			("a", It.IsAny<object>()) | (params object[]) matches nothing
			((object[])null!) | (params object[]) throws ArgumentNullException
			"""),
		new(Only, "AnyParameters after a matcher does not compile", """
			(It.IsAny<object[]>()).AnyParameters() | CS1061
			"""),
		new(Five, "above four parameters the overloads and their priorities did not change", """
			(1, 2, 3, 4) | (int, int, int, int, params object[]) matches 1234:[]
			(1, 2, 3, 4, "a") | (int, int, int, int, params object[]) matches 1234:[a]
			(1, 2, 3, 4, "a", 2) | (int, int, int, int, params object[]) matches 1234:[a,2]
			(1, 2, 3, 4, new object[] { "a", 2, }) | (int, int, int, int, params object[]) matches 1234:[a,2]
			(1, 2, 3, 4, (object)null!) | (int, int, int, int, params object[]) matches 1234:[null]
			(1, 2, 3, 4).AnyParameters() | (int, int, int, int, params object[]) matches everything
			(default, default, default, default) | (int, int, int, int, params object[]) matches 0000:[]
			(default, default, default, default, "a") | (int, int, int, int, params object[]) matches 0000:[a]
			(default, default, default, default).AnyParameters() | (int, int, int, int, params object[]) matches everything
			(It.IsAny<int>(), It.Is(2), It.Is(3), It.Is(4), It.IsAny<object[]>()) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<object[]>?) matches 1234:[] 1234:[a] 1234:[a,2] 1234:null 1234:[null]
			(It.IsAny<int>(), It.Is(2), It.Is(3), It.Is(4), It.IsAny<object>()) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, params IParameter<object>[]) matches 1234:[a] 1234:[null]
			(It.IsAny<int>(), It.Is(2), It.Is(3), It.Is(4)) | (IParameter<int>?, IParameter<int>?, IParameter<int>?, IParameter<int>?, params IParameter<object>[]) matches 1234:[]
			(null) | (IParameters) matches everything
			(Match.AnyParameters()) | (IParameters) matches everything
			"""),
		new(Five, "no overload combines values with a matcher above four parameters, which are known gaps that did not change", """
			(1, 2, 3, 4, It.IsAny<object[]>()) | (int, int, int, int, params object[]) matches nothing
			(1, 2, 3, 4, It.IsAny<object>()) | (int, int, int, int, params object[]) matches nothing
			(1, 2, 3, 4, null) | (int, int, int, int, params object[]) throws ArgumentNullException
			(1, 2, 3, 4, (object[])null!) | (int, int, int, int, params object[]) throws ArgumentNullException
			(default, default, default, default, default) | (int, int, int, int, params object[]) throws ArgumentNullException
			(It.IsAny<int>(), It.Is(2), It.Is(3), It.Is(4), "a") | CS1503
			"""),
		new(Opt, "a default literal for an optional parameter keeps meaning its value instead of the declared default", """
			(1, 2) | (int, int, params object[]) matches 1,2:[]
			(1, 5, "a") | (int, int, params object[]) matches 1,5:[a]
			(1, default) | (int, int, params object[]) matches 1,0:[]
			(1, default, "a") | (int, int, params object[]) matches 1,0:[a]
			(default, default) | (int, int, params object[]) matches 0,0:[]
			(1, default).AnyParameters() | (int, int, params object[]) matches everything
			(default) | (IParameters) matches everything
			(null) | (IParameters) matches everything
			(1, null) | (int, IParameter<int>?, params object[]) matches 1,5:[]
			(1, It.IsAny<int>()) | (int, IParameter<int>?, params object[]) matches 1,5:[] 1,0:[] 1,2:[]
			(Match.AnyParameters()) | (IParameters) matches everything
			(1) | CS1503
			"""),
		new(Opt, "a matcher or a null literal for the array matches the whole array", """
			(1, 0, It.IsAny<object[]>()) | (int, int, IParameter<object[]>?) matches 1,0:[] 1,0:[a] 1,0:null
			(1, It.IsAny<int>(), It.IsAny<object[]>()) | (int, IParameter<int>?, IParameter<object[]>?) matches 1,5:[] 1,0:[] 1,2:[] 1,5:[a] 1,0:[a] 1,0:null
			(1, 0, null) | (int, int, IParameter<object[]>?) matches 1,0:null
			(1, 0, It.IsAny<object>()) | (int, int, params object[]) matches nothing
			"""),
		new(Ov, "an overload with an object parameter decides like the call on the mock does", """
			() | () matches array:[]
			("a") | (object) matches object:a
			("a", "b") | (params object[]) matches array:[a,b]
			(new object[] { "a", }) | (params object[]) matches array:[a]
			(null) | (IParameter<object[]>?) matches array:null
			((object)null!) | (object) matches object:null
			((object[])null!) | (params object[]) throws ArgumentNullException
			(It.IsAny<object>()) | (IParameter<object>?) matches object:a object:null
			(It.IsAny<object[]>()) | (IParameter<object[]>?) matches array:[] array:[a] array:[a,b] array:null
			(It.IsAny<object>(), It.IsAny<object>()) | (params IParameter<object>?[]) matches array:[a,b]
			(Match.AnyParameters()) | (object) matches nothing
			().AnyParameters() | () matches array:[] array:[a] array:[a,b] array:null
			("a").AnyParameters() | (object) matches object:a object:null
			"""),
		new(Mix, "an overload with as many value parameters binds as before, also where a matcher for it is captured", """
			() | () matches array:[]
			(1, 2) | (int, int) matches ints:1,2
			("a", 1) | (params object[]) matches array:[a,1]
			(null, 1) | (params object[]) matches array:[null,1]
			(It.IsAny<int>(), 2) | (params object[]) matches nothing
			(It.IsAny<int>(), It.IsAny<int>()) | (params object[]) matches nothing
			"""),
		new(Pair, "an overload of another shape does not change the rules", """
			("a") | (string) matches text:a
			(1, "a") | (int, params object[]) matches 1:[a]
			(default, "a") | (int, params object[]) matches 0:[a]
			(1, It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 1:[a] 1:[a,2] 1:null
			(1, null) | (int, IParameter<object[]>?) matches 1:null
			(It.IsAny<int>(), "a") | (IParameter<int>?, params object[]) matches 0:[a] 1:[a]
			"""),
		new(Tri, "next to an object overload values compete like the methods do, but a null literal for a value type binds the matcher overload", """
			("a") | (object) matches object:a
			(1, 2) | (int, int) matches ints:1,2
			("a", "b") | (params object[]) matches array:[a,b]
			(null, 1) | (IParameter<int>?, int) matches nothing
			"""),
		new(Nullable, "a nullable array of nullable elements behaves like object[]", """
			(1) | (int, params object?[]?) matches 1:[]
			(1, "a") | (int, params object?[]?) matches 1:[a]
			(1, (object?)null) | (int, params object?[]?) matches 1:[null]
			(1, null) | (int, IParameter<object?[]?>?) matches 1:null
			(1, It.IsAny<object?[]>()) | (int, IParameter<object?[]?>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(1, It.IsAny<object?[]?>()) | (int, IParameter<object?[]?>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(default, "a") | (int, params object?[]?) matches 0:[a]
			(default) | (int, params object?[]?) matches 0:[]
			(1, It.IsAny<object?>()) | (int, params object?[]?) matches nothing
			"""),
		new(OnlyNullable, "a lone nullable array of nullable elements behaves like object[]", """
			() | () matches []
			("a") | (params object?[]?) matches [a]
			(null) | (IParameter<object?[]?>?) matches null
			(null, null) | (params IParameter<object?>?[]) matches [null,null]
			(It.IsAny<object?[]>()) | (IParameter<object?[]?>?) matches everything
			(It.IsAny<object?>()) | (params IParameter<object?>?[]) matches [a] [b] [null]
			(Match.AnyParameters()) | (IParameters) matches everything
			().AnyParameters() | () matches everything
			"""),
		new(Dyn, "dynamic elements behave like object elements", """
			(1) | (int, params dynamic[]) matches 1:[]
			(1, "a") | (int, params dynamic[]) matches 1:[a]
			(1, "a", 2) | (int, params dynamic[]) matches 1:[a,2]
			(1, null) | (int, IParameter<dynamic[]>?) matches 1:null
			(1, It.IsAny<object[]>()) | (int, IParameter<dynamic[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(1, It.IsAny<dynamic[]>()) | (int, IParameter<dynamic[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(It.IsAny<int>(), It.IsAny<object>()) | (IParameter<int>?, params IParameter<object>[]) matches 0:[a] 1:[a] 1:[b] 1:[null]
			(default, "a") | (int, params dynamic[]) matches 0:[a]
			(default) | (int, params dynamic[]) matches 0:[]
			(1, It.IsAny<object>()) | (int, params dynamic[]) matches nothing
			"""),
		new(OnlyDyn, "a lone array of dynamic elements behaves like object[]", """
			() | () matches []
			("a") | (params dynamic[]) matches [a]
			(null) | (IParameter<dynamic[]>?) matches null
			(null, null) | (params IParameter<object>?[]) matches [null,null]
			(It.IsAny<object[]>()) | (IParameter<dynamic[]>?) matches everything
			(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a] [b] [null]
			(Match.AnyParameters()) | (IParameters) matches everything
			().AnyParameters() | () matches everything
			"""),
		new(LogG, "a type parameter of the mocked type that is closed over object behaves like object", """
			(1) | (int, params object[]) matches 1:[]
			(1, "a") | (int, params object[]) matches 1:[a]
			(1, null) | (int, IParameter<object[]>?) matches 1:null
			(1, It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			(default, "a") | (int, params object[]) matches 0:[a]
			(default) | (int, params object[]) matches 0:[]
			(1).AnyParameters() | (int, params object[]) matches everything
			(1, It.IsAny<object>()) | (int, params object[]) matches nothing
			"""),
		new(OnlyG, "a lone array of a type parameter that is closed over object behaves like object[]", """
			() | () matches []
			("a") | (params object[]) matches [a]
			(null) | (IParameter<object[]>?) matches null
			(null, null) | (params IParameter<object>?[]) matches [null,null]
			(It.IsAny<object[]>()) | (IParameter<object[]>?) matches everything
			(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a] [b] [null]
			(Match.AnyParameters()) | (IParameters) matches everything
			().AnyParameters() | () matches everything
			"""),
		new(Gen, "the values of a params array whose type parameter of the method is closed over object can be matchers", """
			<object>(1) | (int, params object[]) matches 1:[]
			<object>(1, "a") | (int, params object[]) matches 1:[a]
			<object>(default, "a") | (IParameter<int>?, params object[]) matches 0:[a]
			<object>(It.IsAny<int>(), It.IsAny<object[]>()) | (IParameter<int>?, IParameter<object[]>?) matches everything
			<object>(1, It.IsAny<object[]>()) | (int, IParameter<object[]>?) matches 1:[] 1:[a] 1:[b] 1:[a,2] 1:[a,3] 1:null 1:[null] 1:[null,null]
			<object>(1, It.IsAny<object>()) | (int, params object[]) matches 1:[a] 1:[b] 1:[null]
			<object>(1, "a", It.IsAny<object>()) | (int, params object[]) matches 1:[a,2] 1:[a,3]
			<object>(1, It.IsAny<object[]>(), 2) | (int, params object[]) matches nothing
			<object>(1, It.IsAny<int>()) | (int, params object[]) matches nothing
			<object>(1, null) | (int, IParameter<object[]>?) matches 1:null
			<object>(default) | (IParameters?) matches 0:[]
			<object>(default, null) | (IParameter<int>?, IParameter<object[]>?) matches 0:null
			"""),
		new(OnlyGen, "a lone params array whose type parameter of the method is closed over object behaves like object[]", """
			<object>() | () matches []
			<object>("a") | (params object[]) matches [a]
			<object>(It.IsAny<object[]>()) | (IParameter<object[]>?) matches everything
			<object>(It.IsAny<object>()) | (params IParameter<object>?[]) matches [a] [b] [null]
			<object>(null, It.IsAny<object>()) | (params IParameter<object>?[]) matches [null,null] [null,a]
			<object>(Match.AnyParameters()) | (IParameters?) matches everything
			<object>("a").AnyParameters() | (params object[]) matches everything
			<object>(It.IsAny<int>()) | (params object[]) matches nothing
			<object>(null) | (IParameters?) matches null
			"""),
		new(OvGen, "a params array of a type parameter beside an overload keeps the overload for values, which recognizes matchers at runtime, and binds as before", """
			<object>() | (params object[]) matches []
			<object>("a") | (params object[]) matches [a]
			<object>(It.IsAny<object[]>()) | (params object[]) matches everything
			<object>(It.IsAny<object>()) | (params object[]) matches [a] [b] [null]
			<object>(null, It.IsAny<object>()) | (params object[]) matches [null,null] [null,a]
			<object>(Match.AnyParameters()) | (params object[]) matches nothing
			<object>("a").AnyParameters() | (params object[]) matches everything
			<object>(null!) | (params object[]) matches null
			"""),
		new(Str, "other element types did not change", """
			(1) | (int, params string[]) matches 1:[]
			(1, "a") | (int, params string[]) matches 1:[a]
			(1, null) | (int, params string[]) throws ArgumentNullException
			(1, It.IsAny<string[]>()) | (int, IParameter<string[]>?) matches 1:[] 1:[a] 1:[a,b] 1:null
			(1, It.IsAny<string>()) | CS1503
			(It.IsAny<int>(), "a") | (IParameter<int>?, params string[]) matches 0:[a] 1:[a]
			(default, "a") | (int, params string[]) matches 0:[a]
			(default) | (int, params string[]) matches 0:[]
			(default, default) | (int, params string[]) throws ArgumentNullException
			(null) | (IParameters) matches everything
			(Match.AnyParameters()) | (IParameters) matches everything
			(1).AnyParameters() | (int, params string[]) matches everything
			"""),
		new(OnlyStr, "a lone array of another element type did not change", """
			() | (params string[]) matches []
			("a") | (params string[]) matches [a]
			(null) | (params string[]) throws ArgumentNullException
			(null, null) | (params string[]) matches [null,null]
			(default, default) | (params string[]) matches [null,null]
			(It.IsAny<string[]>()) | (IParameter<string[]>?) matches everything
			(It.IsAny<string>()) | (params IParameter<string>[]) matches [a]
			(Match.AnyParameters()) | (IParameters) matches everything
			(parameters: null) | (IParameters) matches everything
			().AnyParameters() | (params string[]) matches everything
			"""),
	];

	private static readonly OverloadMatrix.Member LogDelegate = new("LogDelegate", null, Probes("sut", LevelAndArgs));
	private static readonly OverloadMatrix.Member OnlyDelegate = new("OnlyDelegate", null, Probes("sut", OnlyArgs));
	private static readonly OverloadMatrix.Member StrDelegate = new("StrDelegate", null, Probes("sut", LevelAndStrings));

	private static readonly OverloadMatrix.Member OnlyStrDelegate =
		new("OnlyStrDelegate", null, Probes("sut", OnlyStrings));

	private static readonly (OverloadMatrix.Member Method, OverloadMatrix.Member Delegate, string Calls)[] LikeMethods =
	[
		(Log, LogDelegate, """
			(1)
			(1, "a")
			(1, "a", 2)
			(1, new object[] { "a", 2, })
			(1, (object)null!)
			(1, null)
			(1, default)
			(level: 1, args: null)
			(args: null, level: 1)
			(default)
			(default, "a")
			(default, default)
			(1, It.IsAny<object[]>())
			(1, It.Satisfies<object[]>(a => a?.Length == 2))
			(level: 1, args: It.IsAny<object[]>())
			(default, It.IsAny<object[]>())
			(1, It.IsAny<string[]>())
			(It.IsAny<int>(), It.IsAny<object[]>())
			(It.IsAny<int>(), It.IsAny<object>())
			(It.IsAny<int>(), It.IsAny<object>(), It.Is<object>(2))
			(It.Is(1), It.Is<object>("a"), It.IsAny<object>())
			(It.IsAny<int>(), null, null)
			(It.IsAny<int>())
			(It.IsAny<int>(), "a")
			(It.IsAny<int>(), "a", 2)
			(null)
			(null, "a")
			(Match.AnyParameters())
			(1).AnyParameters()
			(default, "a").AnyParameters()
			(1, It.IsAny<object>())
			(1, "a", It.IsAny<object>())
			(1, (object[])null!)
			(It.IsAny<int>(), null)
			(null, null)
			(1, It.IsAny<object[]>()).AnyParameters()
			"""),
		(Only, OnlyDelegate, """
			()
			("a")
			("a", 2)
			(new object[] { "a", 2, })
			((object)null!)
			(null, "a")
			(null)
			(default)
			(args: null)
			(null, null)
			(default, default)
			(It.IsAny<object[]>())
			(It.Satisfies<object[]>(a => a?.Length == 2))
			(args: It.IsAny<object[]>())
			(It.IsAny<string[]>())
			(It.IsAny<object>())
			(It.Is<object>("a"))
			(It.IsAny<object>(), It.IsAny<object>())
			(It.IsAny<string>())
			(Match.AnyParameters())
			(parameters: null)
			().AnyParameters()
			("a").AnyParameters()
			("a", It.IsAny<object>())
			((object[])null!)
			(It.IsAny<object[]>()).AnyParameters()
			"""),
		(Str, StrDelegate, """
			(1)
			(1, "a")
			(1, "a", "b")
			(1, null)
			(1, It.IsAny<string[]>())
			(It.IsAny<int>(), "a")
			(It.IsAny<int>(), It.IsAny<string>())
			(It.IsAny<int>(), It.Is("a"), It.IsAny<string>())
			(It.IsAny<int>())
			(default, "a")
			(default)
			(default, default)
			(null)
			(Match.AnyParameters())
			(1).AnyParameters()
			(1, It.IsAny<string>())
			(It.IsAny<int>(), null)
			"""),
		(OnlyStr, OnlyStrDelegate, """
			()
			("a")
			("a", "b")
			(null)
			(null, null)
			(default, default)
			(It.IsAny<string[]>())
			(It.IsAny<string>())
			(It.IsAny<string>(), It.Is("b"))
			(Match.AnyParameters())
			(parameters: null)
			().AnyParameters()
			"""),
	];

	[Test]
	public async Task ParamsObjectArray_InClassicMode_ShouldBindAndMatchAsExpected()
		=> await VerifyGroups(Groups, LanguageVersion.CSharp14);

	[Test]
	public async Task ParamsObjectArray_InClassicMode_OnDelegates_ShouldBindLikeMethods()
		=> await VerifyDelegates(LanguageVersion.CSharp14);

#if NET11_0_OR_GREATER
	[Test]
	public async Task ParamsObjectArray_InUnionMode_ShouldBindAndMatchAsExpected()
		=> await VerifyGroups(Groups, LanguageVersion.CSharp15);

	[Test]
	public async Task ParamsObjectArray_InUnionMode_OnDelegates_ShouldBindLikeMethods()
		=> await VerifyDelegates(LanguageVersion.CSharp15);
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
				.Because($"on {group.Member.Name ?? group.Member.Type}, {group.Because}");
		}
	}

	private static async Task VerifyDelegates(LanguageVersion languageVersion)
	{
		(OverloadMatrix.Member Member, string Call)[] calls = LikeMethods
			.SelectMany(x => new[] { x.Method, x.Delegate, }.SelectMany(member => Lines(x.Calls).Select(call => (member, call))))
			.ToArray();
		string[] results = OverloadMatrix.Run(Declarations, calls, languageVersion);

		int index = 0;
		foreach ((OverloadMatrix.Member method, OverloadMatrix.Member @delegate, string callLines) in LikeMethods)
		{
			string[] callsOfPair = Lines(callLines);
			string[] onMethod = callsOfPair.Select(call => $"{call} | {results[index++]}").ToArray();
			string[] onDelegate = callsOfPair.Select(call => $"{call} | {results[index++]}").ToArray();

			await That(onDelegate).IsEqualTo(onMethod)
				.Because($"{@delegate.Type} carries the overloads and priorities of the method {method.Name}");
		}
	}

	private static string[] Lines(string text)
		=> text.Split('\n').Select(line => line.Trim()).ToArray();

	private static (string, string)[] Probes(string member, (string Label, string Arguments)[] probes)
		=> probes.Select(probe => (probe.Label, member + probe.Arguments)).ToArray();

	private sealed record Group(OverloadMatrix.Member Member, string Because, string Expected);
}
