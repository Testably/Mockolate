namespace Mockolate;

/// <summary>
///     Marks an attribute of a test framework that fills parameters through <see cref="MockDataSource" />.
/// </summary>
/// <remarks>
///     The Mockolate source generator emits the creation code for the parameter types of every method and constructor
///     with such an attribute, and of the constructors of every type with it: a mock for an interface, a delegate or an
///     abstract class, an instance with mocked constructor dependencies for a concrete class, and for the other types,
///     including those of the <c>System</c>, <c>Microsoft</c> and <c>Windows</c> namespaces, a value from the default
///     values of the behavior. Mockolate0010 reports a parameter of an interface, delegate or class outside these
///     namespaces that cannot be mocked or created.
/// </remarks>
public interface IMockDataSource;
