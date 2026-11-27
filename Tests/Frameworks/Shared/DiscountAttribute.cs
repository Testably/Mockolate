using System;
using System.Reflection;

namespace Mockolate.Frameworks.TestHelpers.Domain;

[ProvidesValuesFor(typeof(DiscountRule))]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DiscountAttribute(int percentage) : Attribute, IParameterValueProvider
{
	public object? ProvideValue(ParameterInfo parameter) => new DiscountRule
	{
		ProductCode = parameter.Name!,
		Percentage = percentage,
	};
}
