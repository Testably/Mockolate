using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Mockolate.SourceGenerators.Entities;

internal record AttributeParameter
{
	public AttributeParameter(TypedConstant value)
	{
		Value = Serialize(value);
	}

	public string? Value { get; }

	private static string Serialize(TypedConstant value)
		=> value.Kind switch
		{
			TypedConstantKind.Array when value.IsNull => "null",
			TypedConstantKind.Array =>
				$"new {value.Type?.ToQualifiedDisplayString(Helpers.TypeDisplayFormat)}{{{string.Join(", ", value.Values.Select(Serialize))}}}",
			TypedConstantKind.Type when value.Value is ITypeSymbol type =>
				$"typeof({type.ToQualifiedDisplayString(Helpers.TypeDisplayFormat)})",
			TypedConstantKind.Enum =>
				$"({value.Type?.ToQualifiedDisplayString(Helpers.TypeDisplayFormat)}){value.Value}",
			_ => SerializeConstantValue(value),
		};

	private static string SerializeConstantValue(TypedConstant value)
	{
		if (value.Value is null)
		{
			return "null";
		}

		return value.Type?.SpecialType switch
		{
			SpecialType.System_Char => $"'{value.Value}'",
			SpecialType.System_String => $"\"{value.Value.ToString().Replace("\"", "\\\"")}\"",
			SpecialType.System_Boolean => value.Value.ToString().ToLowerInvariant(),
			SpecialType.System_Double => ((double)value.Value).ToString(CultureInfo.InvariantCulture),
			SpecialType.System_Single => $"{((float)value.Value).ToString(CultureInfo.InvariantCulture)}F",
			SpecialType.System_Byte => $"(byte){value.Value}",
			SpecialType.System_SByte => $"(sbyte){value.Value}",
			SpecialType.System_Int16 => $"(short){value.Value}",
			SpecialType.System_UInt16 => $"(ushort){value.Value}",
			SpecialType.System_Int64 => $"{value.Value}L",
			SpecialType.System_UInt64 => $"{value.Value}uL",
			SpecialType.System_UInt32 => $"{value.Value}u",
			_ => value.Value.ToString(),
		};
	}
}
