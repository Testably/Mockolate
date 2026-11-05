#if NET10_0_OR_GREATER
namespace Mockolate.Tests.GeneratorCoverage;

/// <summary>
///     Parameters named like identifiers the generator emits into member bodies, so an emitted
///     reference that is neither qualified nor uniquified binds to the parameter instead.
/// </summary>
public interface IParameterNameCollisions
{
	int this[int MockRegistry] { get; set; }

	event ParameterNameCollisionsDelegate Event;

	int Method(int MockRegistry);

	void VoidMethod(int MockRegistry, ref int other, out int another);

	int Five(int MockRegistry, int b, int c, int d, int e);

	T Generic<T>(T MockRegistry);
}

/// <summary>
///     The class-mock counterpart of <see cref="IParameterNameCollisions" />, adding the base-call paths.
/// </summary>
public abstract class ParameterNameCollisionsBase
{
	public virtual int this[int MockRegistry]
	{
		get => 0;
		set { }
	}

	public virtual int Method(int MockRegistry) => 0;

	public virtual void VoidMethod(int MockRegistry, ref int other, out int another) => another = 0;

	protected virtual int ProtectedMethod(int MockRegistry) => 0;
}

/// <summary>
///     The delegate-mock counterpart of <see cref="IParameterNameCollisions" />.
/// </summary>
public delegate int ParameterNameCollisionsDelegate(int MockRegistry);
#endif
