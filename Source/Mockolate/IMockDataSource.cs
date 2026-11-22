namespace Mockolate;

/// <summary>
///     Marks an attribute of a test framework that fills the parameters of a test method through
///     <see cref="MockDataSources" />.
/// </summary>
/// <remarks>
///     The Mockolate source generator emits the construction code for the parameter types of every method with such an
///     attribute: a mock for a mockable type, and the class with mocked constructor dependencies for a concrete class.
/// </remarks>
public interface IMockDataSource;
