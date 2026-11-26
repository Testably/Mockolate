using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods of every arity, which <c>SetupGeneric</c> and <c>VerifyGeneric</c> offer for any type argument:
///     a parameter that depends on a type argument is matched as <see langword="object" />, any other keeps its type, and
///     constraints do not apply. Above four parameters the setups are generated, also the setup without a return value
///     for a method that returns one. Overloads whose counterparts would share a signature are offered once, but not those
///     that differ in a nullable value type, and calls of overloads with another number of type parameters are told
///     apart, as are overloads with as many type parameters and parameters, by the types of their parameters. Overloads
///     whose counterparts differ only in the names of tuple elements or in <see langword="dynamic" /> are left out.
///     Parameter names do not clash with the names of the generated locals, nor, when they differ only in case,
///     with each other or with other parameters in the type parameters of the overload that takes matchers of any type.
/// </summary>
public interface IGenericMethodAnyTypeArgument
{
	void Plain<T>(T value);
	T Deserialize<T>(string json);
	Task<TResponse> Send<TResponse>(IAnyTypeRequest<TResponse> request);
	int Struct<T>(T value) where T : struct;
	int Constrained<T>(T value) where T : class, IAnyTypeRequest<int>, new();
	void Overloaded<T>(T value);
	void Overloaded<T>(object value);
	void Sibling<T>(T value);
	void Sibling(int value);
	T Create<T>() where T : new();
	void Pair<TKey, TValue>(TKey key, TValue value);
	void Two<T>(T value, int other);
	int Three<T>(T first, string second, int third);
	void Four<T>(T first, T second, T third, T fourth);
	int Five<T>(T first, int second, int third, int fourth, string fifth);
	void Six<T>(T first, T second, T third, T fourth, T fifth, T sixth);
	void Collide<T>(T methodSetup, T invocation, T match1);
	void Arity<T>(T value);
	void Arity<T1, T2>(int value);
	void Nullable<T>(int value);
	void Nullable<T>(int? value);
	void Cased<T>(T value, T Value);
	void Prefixed<T>(T foo, int TFoo);
	void SameShape<T>(T value);
	void SameShape<T>(int value);
	void WithParams<T>(T value);
	void WithParams<T>(params T[] values);
	void TupleNames<T>((int First, int Second) value);
	void TupleNames<T1, T2>((int Left, int Right) value);
	void NestedDynamic<T>(List<object> value);
	void NestedDynamic<T1, T2>(List<dynamic> value);
}
