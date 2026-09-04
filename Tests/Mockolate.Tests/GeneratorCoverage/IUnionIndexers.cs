namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Indexer shapes for the union-mode snapshot: one indexer per key count so that every one of them qualifies for
///     union-typed keys (a getter/setter pair, a getter-only and a setter-only indexer, a delegate-typed key, and a
///     five-key indexer that exercises the predicate-based verify path).
/// </summary>
public interface IUnionIndexers
{
	string this[int key] { get; set; }
	long this[byte a, byte b] { get; }
	long this[short a, short b, short c] { set; }
	string this[System.Func<int, bool> selector, int key, string name, bool flag] { get; }
	string this[int a, int b, int c, int d, int e] { get; set; }
}
