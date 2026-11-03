using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Mockolate.Analyzers.CodeFixers;

namespace Mockolate.Analyzers.Tests;

// The pinned test compiler predates C# unions, so the stand-in ParameterArg<T> below takes values through a plain
// implicit operator. The errors it produces for these arguments match the union-typed overloads: neither passes the
// parameter type on to target-typed expressions or chains a user-defined conversion.
public class UnionArgumentCodeFixProviderTests
{
	private const string Declarations = """
	                                    #nullable enable
	                                    using System;

	                                    namespace Mockolate
	                                    {
	                                        public readonly struct ParameterArg<T>
	                                        {
	                                            public ParameterArg(T value) { }
	                                            public static implicit operator ParameterArg<T>(T value) => new(value);
	                                        }
	                                    }

	                                    public readonly record struct Key(string Value)
	                                    {
	                                        public static implicit operator Key(string value) => new(value);
	                                    }

	                                    public interface ISetup
	                                    {
	                                        int Write(Mockolate.ParameterArg<ReadOnlyMemory<byte>>? data, Mockolate.ParameterArg<int>? count);
	                                        int Wait(Mockolate.ParameterArg<TimeSpan>? timeout);
	                                        int CountAll(Mockolate.ParameterArg<int[]>? values);
	                                        int Lookup(Mockolate.ParameterArg<Key>? key);
	                                        int Plain(int value);
	                                        string this[Mockolate.ParameterArg<Key>? key] { get; }
	                                    }

	                                    """;

	[Test]
	public async Task UserDefinedConversion_ShouldAddACast() => await VerifyAsync(
		"setup.Write({|CS1503:bytes|}, 1);",
		"setup.Write((ReadOnlyMemory<byte>)bytes, 1);");

	[Test]
	public async Task TargetTypedNew_ShouldAddTheType() => await VerifyAsync(
		"setup.Wait({|CS1729:new(0, 0, 5)|});",
		"setup.Wait(new TimeSpan(0, 0, 5));");

	[Test]
	public async Task CollectionExpression_ShouldAddACast() => await VerifyAsync(
		"setup.CountAll({|CS9174:[1, 2,]|});",
		"setup.CountAll((int[])[1, 2,]);");

	[Test]
	public async Task NamedArgument_ShouldAddACast() => await VerifyAsync(
		"setup.Lookup(key: {|CS1503:\"k\"|});",
		"setup.Lookup(key: (Key)\"k\");");

	[Test]
	public async Task IndexerKey_ShouldAddACast() => await VerifyAsync(
		"_ = setup[{|CS1503:\"k\"|}];",
		"_ = setup[(Key)\"k\"];");

	[Test]
	public async Task CompoundExpression_ShouldBeParenthesized() => await VerifyAsync(
		"setup.Lookup({|CS1503:\"a\" + \"b\"|});",
		"setup.Lookup((Key)(\"a\" + \"b\"));");

	[Test]
	public async Task ArgumentWithoutConversionToTheCaseType_ShouldNotBeFixed() => await VerifyAsync(
		"setup.Wait({|CS1503:\"5s\"|});",
		"setup.Wait({|CS1503:\"5s\"|});");

	[Test]
	public async Task NonUnionParameter_ShouldNotBeFixed() => await VerifyAsync(
		"setup.Plain({|CS1503:\"1\"|});",
		"setup.Plain({|CS1503:\"1\"|});");

	private static async Task VerifyAsync(string statement, string fixedStatement)
	{
		CSharpCodeFixTest<EmptyDiagnosticAnalyzer, UnionArgumentCodeFixProvider, DefaultVerifier> test = new()
		{
			TestCode = Wrap(statement),
			FixedCode = Wrap(fixedStatement),
			ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
		};
		await test.RunAsync(CancellationToken.None);

		static string Wrap(string body)
			=> Declarations + $$"""
			                    public class MyClass
			                    {
			                        public void MyTest(ISetup setup, byte[] bytes)
			                        {
			                            {{body}}
			                        }
			                    }
			                    """;
	}
}
