using System.Threading.Tasks;

namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Generic methods of every arity, which <c>SetupGeneric</c> and <c>VerifyGeneric</c> offer for any type argument:
///     a parameter that depends on a type argument is matched as <see langword="object" />, any other keeps its type, and
///     constraints do not apply. Above four parameters the setups are generated, also the setup without a return value
///     for a method that returns one. Overloads whose counterparts would share a signature are left out.
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
}
