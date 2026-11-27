using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
#if NETSTANDARD2_0
using Mockolate.Internals.Polyfills;
#endif

namespace Mockolate.Internals;

#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal static class TypeFormatter
{
	private static readonly Dictionary<Type, string> Aliases = new()
	{
		{
			typeof(byte), "byte"
		},
		{
			typeof(sbyte), "sbyte"
		},
		{
			typeof(short), "short"
		},
		{
			typeof(ushort), "ushort"
		},
		{
			typeof(int), "int"
		},
		{
			typeof(uint), "uint"
		},
		{
			typeof(long), "long"
		},
		{
			typeof(ulong), "ulong"
		},
		{
			typeof(float), "float"
		},
		{
			typeof(double), "double"
		},
		{
			typeof(decimal), "decimal"
		},
		{
			typeof(object), "object"
		},
		{
			typeof(bool), "bool"
		},
		{
			typeof(char), "char"
		},
		{
			typeof(string), "string"
		},
		{
			typeof(void), "void"
		},
		{
			typeof(nint), "nint"
		},
		{
			typeof(nuint), "nuint"
		},
	};

	private static readonly Dictionary<string, string> AliasesByFullName = Aliases.ToDictionary(
		alias => alias.Key.FullName!, alias => alias.Value, StringComparer.Ordinal);

	internal static string FormatTypeNames(string typeNames)
	{
		int position = 0;
		return string.Join(", ", ParseTypeNames(typeNames, ref position));
	}

	private static List<string> ParseTypeNames(string text, ref int position)
	{
		List<string> types = [];
		while (position < text.Length && text[position] != ']')
		{
			if (text[position] is ',' or ' ')
			{
				position++;
				continue;
			}

			types.Add(ParseTypeName(text, ref position));
		}

		return types;
	}

	private static string ParseTypeName(string text, ref int position)
	{
		int start = position;
		while (position < text.Length && text[position] is not ('[' or ',' or ']'))
		{
			position++;
		}

		string name = text.Substring(start, position - start);
		List<string>? arguments = null;
		if (position + 1 < text.Length && text[position] == '[' && text[position + 1] is not (']' or ','))
		{
			position++;
			arguments = ParseTypeNames(text, ref position);
			position++;
		}

		StringBuilder stringBuilder = new(FormatTypeName(name, arguments));
		while (position < text.Length && text[position] == '[')
		{
			do
			{
				stringBuilder.Append(text[position]);
			} while (text[position++] != ']');
		}

		return stringBuilder.ToString();
	}

	private static string FormatTypeName(string name, List<string>? arguments)
	{
		if (arguments is null && AliasesByFullName.TryGetValue(name, out string? alias))
		{
			return alias;
		}

		if (arguments is { Count: 1, } && string.Equals(name, "System.Nullable`1", StringComparison.Ordinal))
		{
			return arguments[0] + "?";
		}

		int nestedStart = name.IndexOf('+', StringComparison.Ordinal);
		int namespaceEnd = name.AsSpan(0, nestedStart < 0 ? name.Length : nestedStart).LastIndexOf('.');
		StringBuilder stringBuilder = new();
		int argumentIndex = 0;
		foreach (string segment in name.Substring(namespaceEnd + 1).Split('+'))
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append('.');
			}

			int arityStart = segment.IndexOf('`', StringComparison.Ordinal);
			if (arityStart < 0 || arguments is null)
			{
				stringBuilder.Append(segment);
				continue;
			}

			int arity = int.Parse(segment.Substring(arityStart + 1), CultureInfo.InvariantCulture);
			stringBuilder.Append(segment, 0, arityStart).Append('<')
				.Append(string.Join(", ", arguments.Skip(argumentIndex).Take(arity))).Append('>');
			argumentIndex += arity;
		}

		return stringBuilder.ToString();
	}

	internal static string FormatType(this Type value)
	{
		StringBuilder stringBuilder = new();
		FormatType(value, stringBuilder);
		return stringBuilder.ToString();
	}

	private static void FormatType(
		Type value,
		StringBuilder stringBuilder)
	{
		if (value.IsGenericParameter)
		{
			stringBuilder.Append(value.Name);
		}
		else if (value.IsArray)
		{
			FormatType(value.GetElementType()!, stringBuilder);
			stringBuilder.Append("[]");
		}
		else if (!AppendedPrimitiveAlias(value, stringBuilder))
		{
			// Handle nested types
			if (value.DeclaringType is not null)
			{
				FormatType(value.DeclaringType, stringBuilder);
				stringBuilder.Append('.');
			}

			if (value.IsGenericType)
			{
				Type genericTypeDefinition = value.GetGenericTypeDefinition();
				stringBuilder.Append(genericTypeDefinition.Name.SubstringUntilFirst('`'));
				stringBuilder.Append('<');
				bool isFirstArgument = true;
				foreach (Type argument in value.GetGenericArguments())
				{
					if (!isFirstArgument)
					{
						stringBuilder.Append(", ");
					}

					isFirstArgument = false;
					if (!argument.ContainsGenericParameters)
					{
						FormatType(argument, stringBuilder);
					}
				}

				stringBuilder.Append('>');
			}
			else
			{
				stringBuilder.Append(value.Name);
			}
		}
	}

	private static bool AppendedPrimitiveAlias(Type value, StringBuilder stringBuilder)
	{
		if (Aliases.TryGetValue(value, out string? typeAlias))
		{
			stringBuilder.Append(typeAlias);
			return true;
		}

		Type? underlyingType = Nullable.GetUnderlyingType(value);

		if (underlyingType != null)
		{
			if (Aliases.TryGetValue(underlyingType, out string? underlyingAlias))
			{
				stringBuilder.Append(underlyingAlias).Append('?');
				return true;
			}

			FormatType(underlyingType, stringBuilder);
			stringBuilder.Append('?');
			return true;
		}

		return false;
	}
}
