namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     An indexer with trailing optional keys, which setups and verifications can leave out: a nullable key with a
///     non-null default and a reference-type key with a <see langword="null" /> default.
/// </summary>
public interface IOptionalIndexerKeys
{
	string this[int key, int? count = 5, string? name = null] { get; set; }
}

/// <summary>
///     A getter-only indexer with a trailing optional key.
/// </summary>
public interface IOptionalIndexerKeysGetterOnly
{
	long this[byte a, byte b = 2] { get; }
}

/// <summary>
///     A setter-only indexer with a trailing optional key.
/// </summary>
public interface IOptionalIndexerKeysSetterOnly
{
	long this[short a, short b, short c, short d = 4] { set; }
}

/// <summary>
///     An indexer with six keys, two of them optional, for the setup and verify types the generator emits and the
///     predicate-based verify path.
/// </summary>
public interface IOptionalIndexerKeysWithSixKeys
{
	string this[int a, int b, int c, int d, int e = 5, string f = "f"] { get; set; }
}

/// <summary>
///     An indexer with optional keys next to one with fewer keys: setups and verifications cannot leave out its keys,
///     so that <c>Setup[x]</c> keeps meaning <c>this[int]</c>, as <c>sut[x]</c> does.
/// </summary>
public interface IOptionalIndexerKeysWithSibling
{
	string this[int key] { get; set; }
	string this[int key, int b = 2, int c = 3] { get; set; }
}
