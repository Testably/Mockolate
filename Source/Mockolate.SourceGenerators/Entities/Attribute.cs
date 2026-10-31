using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

internal record Attribute
{
	public Attribute(AttributeData attributeData)
	{
		Name = ToName(attributeData.AttributeClass?.ToQualifiedDisplayString(Helpers.TypeDisplayFormat) ?? "");
		if (attributeData.ConstructorArguments.Length > 0)
		{
			Parameters = new EquatableArray<AttributeParameter>(attributeData.ConstructorArguments
				.Select(arg => new AttributeParameter(arg)).ToArray());
		}

		if (attributeData.NamedArguments.Length > 0)
		{
			NamedArguments = new EquatableArray<(string, AttributeParameter)>(attributeData.NamedArguments
				.Select(arg => (arg.Key, new AttributeParameter(arg.Value))).ToArray());
		}
	}

	public string Name { get; }

	public EquatableArray<AttributeParameter>? Parameters { get; }

	public EquatableArray<(string Name, AttributeParameter Parameter)>? NamedArguments { get; }

	private static string ToName(string className)
		=> className.EndsWith("Attribute")
			? className.Substring(0, className.Length - "Attribute".Length)
			: className;
}
