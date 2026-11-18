namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Members typed as <see langword="dynamic" />: an argument of type <see langword="dynamic" /> would bind every call
///     the generated code passes it to dynamically, so the generated code must forward it as <see langword="object" />.
/// </summary>
public interface IDynamicMembers
{
	dynamic Value { get; set; }
	dynamic this[dynamic key] { get; set; }
	dynamic this[int a, dynamic b, string c, dynamic d, int e] { get; set; }
	void Dyn1(dynamic d);
	void Dyn2(dynamic d, int x);
	int Dyn3(int a, dynamic b, string c);
	dynamic Dyn4(dynamic a, dynamic b, int c, string d);
	void Dyn5(dynamic a, int b, int c, int d, dynamic e);
	dynamic Get();
	void DynParams(int a, params dynamic[] values);
	System.Collections.Generic.List<dynamic> DynList(System.Func<dynamic, bool> filter);
	(dynamic, int) DynTuple(out dynamic o, ref dynamic r);
}

/// <summary>
///     A class with <see langword="dynamic" /> members, so that the generated overrides forward a
///     <see langword="dynamic" /> argument to the base implementation.
/// </summary>
public abstract class DynamicMembersBase
{
	public virtual dynamic Value { get; set; } = 0;
	public virtual dynamic this[dynamic key] => key;
	public virtual dynamic Combine(dynamic a, int b) => a;
	public abstract void Consume(int a, dynamic b);
}

public interface IDynamicOverloadA
{
	void M(dynamic x);
}

public interface IDynamicOverloadB
{
	void M(object x);
}

/// <summary>
///     Inherits <c>M(dynamic)</c> and <c>M(object)</c>, which have the same signature once <see langword="dynamic" />
///     is emitted as <see langword="object" />, so one of them must be implemented explicitly.
/// </summary>
public interface IDynamicOverloads : IDynamicOverloadA, IDynamicOverloadB
{
}

public class DynamicHidingBase
{
	public virtual dynamic M(dynamic x) => x;
}

/// <summary>
///     Hides the <see langword="dynamic" /> member of its base class with an <see langword="object" /> member of the
///     same signature, so only the hiding member may be overridden.
/// </summary>
public class DynamicHidingDerived : DynamicHidingBase
{
	public new virtual object M(object x) => $"derived {x}";
}

/// <summary>
///     Generic types that are mocked closed over <see langword="dynamic" />. A class cannot implement an interface that
///     is constructed with <see langword="dynamic" />, so the generated code must name the identical
///     <see langword="object" /> construction.
/// </summary>
public interface IDynamicRepository<T>
{
	T Value { get; set; }
	T this[T key] { get; set; }
	event System.EventHandler<T>? Changed;
	T Get(T key, System.Collections.Generic.List<T> values);
	void Add(T item);
}

public abstract class DynamicRepositoryBase<T>
{
	protected DynamicRepositoryBase(T initial)
	{
		Initial = initial;
	}

	public T Initial { get; }
	public virtual T Echo(T value) => value;
	public abstract void Consume(T value, System.Collections.Generic.IEnumerable<T> values);
}

public delegate T DynamicFactory<T>(T value, System.Collections.Generic.List<T> values);
