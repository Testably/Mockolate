namespace Mockolate.Tests.GeneratorCoverage;

public class GenericMethodHidingBase
{
	public virtual string Hidden<T>(T value) => "base";
	public virtual string HiddenVirtual<T>(T value) => "base";
	public virtual string HiddenNested<T>(System.Collections.Generic.List<T> values) => "base";
	public virtual string Arity<T>(int value) => "base";
	public virtual string Array<T>(T[] values) => "base";
}

/// <summary>
///     Hides generic methods of its base class with <see langword="new" />. The type parameters of the hiding and the
///     hidden method are different symbols, so they must be matched by position. Only the hiding methods may be
///     overridden, while the overloads with a different type parameter count or parameter type do not hide anything.
/// </summary>
public class GenericMethodHidingDerived : GenericMethodHidingBase
{
#pragma warning disable CA1822 // Mark members as static
	public new string Hidden<T>(T value) => "derived";
#pragma warning restore CA1822
	public new virtual string HiddenVirtual<T>(T value) => "derived";
	public new virtual string HiddenNested<T>(System.Collections.Generic.List<T> values) => "derived";
	public virtual string Arity<T1, T2>(int value) => "derived";
	public virtual string Array<T>(T value) => "derived";
}

public class GenericMethodHidingBase<TItem>
{
	public virtual string Hidden<T>(TItem item, T value) => "base";
}

/// <summary>
///     Hides a generic method of its generic base class, so the hidden method uses the type parameter of the class and
///     of the method.
/// </summary>
public class GenericMethodHidingDerived<TItem> : GenericMethodHidingBase<TItem>
{
	public new virtual string Hidden<T>(TItem item, T value) => "derived";
}

public interface IGenericMethodHidingBase
{
	string Hidden<T>(T value);
}

/// <summary>
///     Hides a generic method of its base interface with <see langword="new" />, so the hidden method must be
///     implemented explicitly.
/// </summary>
public interface IGenericMethodHidingDerived : IGenericMethodHidingBase
{
	new string Hidden<T>(T value);
}
