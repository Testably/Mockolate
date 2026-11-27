using System.Reflection;

namespace Mockolate;

/// <summary>
///     Provides the value of a test parameter of an <c>[AutoMockArguments]</c> test, when an attribute that implements it
///     is applied to the parameter.
/// </summary>
/// <remarks>
///     Mockolate calls <see cref="ProvideValue(ParameterInfo)" /> once per test case and shares the value with the
///     classes under test like a created parameter. Mark the attribute with <see cref="ProvidesValuesForAttribute" />, so
///     that the analyzer reports parameters of other types.
/// </remarks>
public interface IParameterValueProvider
{
	/// <summary>
	///     Provides the value of the <paramref name="parameter" />.
	/// </summary>
	/// <param name="parameter">The test parameter, whose <see cref="ParameterInfo.ParameterType" /> the value must be assignable to.</param>
	/// <returns>The value of the test parameter.</returns>
	object? ProvideValue(ParameterInfo parameter);
}
