namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Overridable generic methods that use the nullable annotation <c>U?</c> of a method type parameter. The
///     override must state <c>where U : default</c> or <c>where U : class</c>, because <c>U?</c> otherwise means
///     <see cref="System.Nullable{T}" /> there.
/// </summary>
public abstract class GenericMethodOverrideBase
{
	public abstract void AcceptClass<U>(U? value) where U : class;
	public abstract U? GetNotNull<U>() where U : notnull;
	public virtual U? GetComparable<U>(U? value) where U : System.IComparable => value;
	public abstract void Accept<U>(U? value);
	public abstract U? Get<U>();
	public virtual void AcceptVirtual<U>(U? value) { }
	public virtual U? GetVirtual<U>() => default;
	protected abstract U? Combine<U, V>(U? first, V second);
	public U? InvokeCombine<U, V>(U? first, V second) => Combine(first, second);
}
