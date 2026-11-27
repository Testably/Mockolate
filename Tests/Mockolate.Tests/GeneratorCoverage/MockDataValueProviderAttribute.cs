using System;
using System.Reflection;

namespace Mockolate.Tests.GeneratorCoverage;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MockDataValueProviderAttribute : Attribute, IParameterValueProvider
{
	public object? ProvideValue(ParameterInfo parameter) => null;
}
