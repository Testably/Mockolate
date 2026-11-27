using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	/// <summary>
	///     Creates the <c>CreateMock(out ConstructorArguments ...)</c> overload of every sealed class or record, which
	///     returns a real instance whose constructor received mocks.
	/// </summary>
	public static string SealedClasses(EquatableArray<DataSourceClass> classes)
	{
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable").AppendLine();
		// A dependency that spells a type argument with other nullable annotations than the CreateMock call shares its
		// mock, whose extension then differs only in nullability.
		sb.Append("#pragma warning disable CS8619, CS8620").AppendLine();
		sb.AppendLine();
		sb.Append("namespace Mockolate").AppendLine();
		sb.Append('{').AppendLine();
		int index = 0;
		foreach (DataSourceClass sealedClass in classes)
		{
			if (index > 0)
			{
				sb.AppendLine();
			}

			// Static extension members of different receivers in one class must not have the same parameters, so every
			// sealed class gets a class of its own.
			string container = $"MockolateSealedCreation{index++}";
			sb.Append("\t/// <summary>").AppendLine();
			sb.Append("\t///     The <c>CreateMock(out ...)</c> overload of <see cref=\"").Append(sealedClass.ClassFullName)
				.Append("\" />.").AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
#if !DEBUG
			sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
			sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
			sb.Append("\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
			sb.Append("\tinternal static class ").Append(container).AppendLine();
			sb.Append("\t{").AppendLine();
			AppendSealedCreateMock(sb, sealedClass, container);
			sb.Append("\t}").AppendLine();
		}

		sb.Append('}').AppendLine();
		sb.Append("#nullable disable").AppendLine();
		return sb.ToString();
	}

	private static void AppendSealedCreateMock(StringBuilder sb, DataSourceClass sealedClass, string container)
	{
		DataSourceParameter[] parameters = sealedClass.Parameters.AsArray();
		string className = sealedClass.ClassFullName.Replace("global::", "").Replace("@", "");
		string arguments = UniqueParameterName(parameters, "constructorArguments");
		string behavior = UniqueParameterName(parameters, "mockBehavior");
		string local = UniqueParameterName(parameters, "argument");
		string value = UniqueParameterName(parameters, "value");
		string created = UniqueParameterName(parameters, "created");

		// The parameters are described once, so that a creation only allocates the values.
		sb.Append("\t\tprivate static readonly global::Mockolate.Internals.ConstructorSignature Signature = new global::Mockolate.Internals.ConstructorSignature(\"")
			.Append(className).Append('(')
			.Append(string.Join(", ", parameters.Select(parameter => parameter.TypeFullName.Replace("global::", ""))))
			.Append(")\", new string[] { ")
			.Append(string.Join(", ", parameters.Select(parameter => $"\"{parameter.Name}\"")))
			.Append(" }, new global::System.Type[] { ")
			.Append(string.Join(", ", parameters.Select(parameter => $"typeof({parameter.TypeFullName})")))
			.Append(" });").AppendLine();
		sb.AppendLine();
		sb.Append("\t\textension(").Append(sealedClass.ClassFullName).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t/// <summary>").AppendLine();
		sb.Append("\t\t\t///     Creates a real instance of <see cref=\"").Append(sealedClass.ClassFullName)
			.Append("\" />, which cannot be mocked, with a new mock for every mockable constructor parameter without an argument.")
			.AppendLine();
		sb.Append("\t\t\t/// </summary>").AppendLine();
		sb.Append("\t\t\t/// <remarks>").AppendLine();
		sb.Append("\t\t\t///     The <paramref name=\"").Append(arguments)
			.Append("\" /> return the arguments, so that you can set up and verify the mocks. Other parameters get their default value, or a value from the default values of the behavior.")
			.AppendLine();
		sb.Append("\t\t\t/// </remarks>").AppendLine();
		sb.Append("\t\t\tpublic static ").Append(sealedClass.ClassFullName).Append(" CreateMock(out global::Mockolate.ConstructorArguments ")
			.Append(arguments).Append(", global::Mockolate.MockBehavior? ").Append(behavior).Append(" = null");
		foreach (DataSourceParameter parameter in parameters)
		{
			sb.Append(", ");
			if (parameter is { IsMockable: false, IsParams: false, DefaultValue: { } defaultValue, } && !string.Equals(defaultValue, "null", StringComparison.Ordinal))
			{
				sb.Append(parameter.IsNullable ? NullableName(parameter.TypeFullName) : parameter.TypeFullName).Append(' ')
					.Append(Helpers.EscapeIfKeyword(parameter.Name)).Append(" = ").Append(defaultValue);
			}
			else
			{
				sb.Append(NullableName(parameter.TypeFullName)).Append(' ').Append(Helpers.EscapeIfKeyword(parameter.Name))
					.Append(" = null");
			}
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t").Append(behavior).Append(" ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		for (int index = 0; index < parameters.Length; index++)
		{
			DataSourceParameter parameter = parameters[index];
			string name = Helpers.EscapeIfKeyword(parameter.Name);
			string type = parameter.IsNullable && !parameter.IsMockable && !parameter.IsParams
				? NullableName(parameter.TypeFullName)
				: parameter.TypeFullName;
			sb.Append("\t\t\t\t").Append(type).Append(' ').Append(local).Append(index).Append(" = ")
				.Append(SealedArgument(parameter, name, index, behavior, value, created, className)).Append(';').AppendLine();
		}

		string locals = string.Join(", ", parameters.Select((_, index) => $"{local}{index}"));
		// Qualified, because a constructor parameter can be named like the field.
		sb.Append("\t\t\t\t").Append(arguments).Append(" = global::Mockolate.").Append(container)
			.Append(".Signature.Record(new object?[] { ").Append(locals).Append(" });").AppendLine();
		sb.Append("\t\t\t\treturn new ").Append(sealedClass.ClassFullName).Append('(').Append(locals).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
	}

	private static string SealedArgument(DataSourceParameter parameter, string name, int index, string behavior,
		string value, string created, string className)
	{
		if (parameter.IsMockable)
		{
			return $"{name} ?? {parameter.TypeFullName}.CreateMock({behavior})";
		}

		if (parameter.IsParams)
		{
			return $"{name} is {{ }} {value}{index} ? {value}{index} : []";
		}

		if (string.Equals(parameter.DefaultValue, "null", StringComparison.Ordinal))
		{
			return parameter.IsNullable ? name : $"{name}!";
		}

		if (parameter.DefaultValue is not null || parameter.IsNullable)
		{
			return name;
		}

		string lastResort = parameter.IsMockedLast
			? $"{parameter.TypeFullName}.CreateMock({behavior})"
			: $"throw new global::Mockolate.Exceptions.MockException(\"There is no argument for the constructor parameter '{parameter.Name}' of {className}. Pass it, or register a default value for its type with WithDefaultValueFor.\")";
		string generated = $"global::Mockolate.Internals.ConstructorDefaultValue.Get({behavior}, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default({parameter.TypeFullName})!))";
		return $"{name} is {{ }} {value}{index} ? {value}{index} : {generated} is {{ }} {created}{index} ? {created}{index} : {lastResort}";
	}

	private static string NullableName(string typeName)
		=> typeName.TrimEnd('?') + "?";

	private static string UniqueParameterName(DataSourceParameter[] parameters, string name)
	{
		string unique = name;
		int suffix = 1;
		// The name is used as it is and with an index appended, so it must not match a parameter in either form.
		while (parameters.Any(parameter => parameter.Name.StartsWith(unique, StringComparison.Ordinal) &&
		                                   parameter.Name.Skip(unique.Length).All(char.IsDigit)))
		{
			unique = $"{name}_{suffix++}";
		}

		return unique;
	}
}
