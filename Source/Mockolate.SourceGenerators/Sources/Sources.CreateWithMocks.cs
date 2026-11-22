using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     Creates the <c>CreateWithMocks</c> extension and the container of the constructor arguments for every class
	///     in <paramref name="suts" />.
	/// </summary>
	public static string CreateWithMocks(EquatableArray<SutClass> suts)
	{
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable").AppendLine();
		// A dependency that spells a type argument with other nullable annotations than the CreateMock call shares its
		// mock, whose extension then differs only in nullability.
		sb.Append("#pragma warning disable CS8619, CS8620").AppendLine();
		sb.AppendLine();
		foreach (SutClass sut in suts)
		{
			AppendContainer(sb, sut);
		}

		sb.Append("namespace Mockolate").AppendLine();
		sb.Append('{').AppendLine();
		sb.Append("\tinternal static partial class Mock").AppendLine();
		sb.Append("\t{").AppendLine();
		for (int i = 0; i < suts.Count; i++)
		{
			if (i > 0)
			{
				sb.AppendLine();
			}

			SutClass sut = suts.AsArray()[i];
			AppendCreateWithMocks(sb, sut, ContainerFullName(sut));
		}

		sb.Append("\t}").AppendLine();
		sb.Append('}').AppendLine();
		return sb.ToString();
	}

	private static string ContainerFullName(SutClass sut)
		=> sut.Namespace is null ? $"global::{sut.ContainerName}" : $"global::{sut.Namespace}.{sut.ContainerName}";

	private static void AppendContainer(StringBuilder sb, SutClass sut)
	{
		string containerName = sut.ContainerName;
		SutParameter[] parameters = sut.Parameters.AsArray();
		string[] propertyNames = GetPropertyNames(parameters, containerName);
		string indent = sut.Namespace is null ? "" : "\t";
		if (sut.Namespace is not null)
		{
			sb.Append("namespace ").Append(sut.Namespace).AppendLine();
			sb.Append('{').AppendLine();
		}

		sb.Append(indent).Append("/// <summary>").AppendLine();
		sb.Append(indent).Append("///     The constructor arguments of the <see cref=\"").Append(sut.ClassFullName)
			.Append("\" /> that <c>CreateWithMocks</c> created.").AppendLine();
		sb.Append(indent).Append("/// </summary>").AppendLine();
		sb.Append(indent).Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append(indent).Append("internal sealed class ").Append(containerName).AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		sb.Append(indent).Append("\tinternal ").Append(containerName).Append('(')
			.Append(string.Join(", ", parameters.Select((p, i) => $"{p.TypeFullName} arg{i}")))
			.Append(')').AppendLine();
		sb.Append(indent).Append("\t{").AppendLine();
		for (int i = 0; i < parameters.Length; i++)
		{
			sb.Append(indent).Append("\t\t").Append(propertyNames[i]).Append(" = arg").Append(i).Append(';').AppendLine();
		}

		sb.Append(indent).Append("\t}").AppendLine();
		for (int i = 0; i < parameters.Length; i++)
		{
			sb.AppendLine();
			sb.Append(indent).Append("\t/// <summary>").AppendLine();
			sb.Append(indent).Append("\t///     The argument of the constructor parameter <c>").Append(parameters[i].Name)
				.Append("</c>.").AppendLine();
			sb.Append(indent).Append("\t/// </summary>").AppendLine();
			sb.Append(indent).Append("\tpublic ").Append(parameters[i].TypeFullName).Append(' ').Append(propertyNames[i])
				.Append(" { get; }").AppendLine();
		}

		sb.Append(indent).Append('}').AppendLine();
		if (sut.Namespace is not null)
		{
			sb.Append('}').AppendLine();
		}

		sb.AppendLine();
	}

	/// <summary>
	///     PascalCase names that neither hide a member of <see cref="object" /> (CS0108), nor repeat the container
	///     name (CS0542), nor each other.
	/// </summary>
	private static string[] GetPropertyNames(SutParameter[] parameters, string containerName)
	{
		HashSet<string> used = new(StringComparer.Ordinal) { containerName, };
		string[] names = new string[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			string baseName = char.ToUpperInvariant(parameters[i].Name[0]) + parameters[i].Name.Substring(1);
			string name = baseName;
			for (int index = 1; Helpers.IsObjectMethodName(name) || !used.Add(name); index++)
			{
				name = $"{baseName}Argument{(index == 1 ? "" : index.ToString())}";
			}

			names[i] = name;
		}

		return names;
	}

	private static string CreateUniqueName(SutParameter[] parameters, string name)
	{
		string usedName = name;
#pragma warning disable S1994 // "for" loop increment clauses should modify the loops' counters
		for (int index = 1; parameters.Any(x => x.Name == usedName); index++)
		{
			usedName = $"{name}_{index}";
		}
#pragma warning restore S1994

		return usedName;
	}

	private static void AppendCreateWithMocks(StringBuilder sb, SutClass sut, string containerFullName)
	{
		SutParameter[] parameters = sut.Parameters.AsArray();
		string mocks = CreateUniqueName(parameters, "mocks");
		string mockBehavior = CreateUniqueName(parameters, "mockBehavior");

		sb.Append("\t\textension(").Append(sut.ClassFullName).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t/// <summary>").AppendLine();
		sb.Append("\t\t\t///     Creates a <see cref=\"").Append(sut.ClassFullName)
			.Append("\" /> with a new mock for every mockable constructor parameter without an argument.").AppendLine();
		sb.Append("\t\t\t/// </summary>").AppendLine();
		sb.Append("\t\t\t/// <remarks>").AppendLine();
		sb.Append("\t\t\t///     A <see langword=\"null\" /> argument for a mockable parameter also gets a new mock.").AppendLine();
		sb.Append("\t\t\t/// </remarks>").AppendLine();
		sb.Append("\t\t\tpublic static ").Append(sut.ClassFullName).Append(" CreateWithMocks(out ")
			.Append(containerFullName).Append(' ').Append(mocks);
		foreach (SutParameter parameter in parameters.Where(p => !p.IsOptional))
		{
			sb.Append(", ").Append(parameter.TypeFullName).Append(' ').Append(Helpers.EscapeIfKeyword(parameter.Name));
		}

		sb.Append(", global::Mockolate.MockBehavior? ").Append(mockBehavior).Append(" = null");
		foreach (SutParameter parameter in parameters.Where(p => p.IsOptional))
		{
			sb.Append(", ");
			if (parameter.CallerInfoAttribute is not null)
			{
				sb.Append('[').Append(parameter.CallerInfoAttribute).Append("] ");
			}

			if (parameter.IsMockable || parameter.IsParams || parameter.IsNullForgiving)
			{
				sb.Append(parameter.TypeFullName.TrimEnd('?')).Append("? ").Append(Helpers.EscapeIfKeyword(parameter.Name))
					.Append(" = null");
			}
			else
			{
				sb.Append(parameter.TypeFullName).Append(' ').Append(Helpers.EscapeIfKeyword(parameter.Name))
					.Append(" = ").Append(parameter.DefaultValue);
			}
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t").Append(mockBehavior).Append(" ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		foreach (SutParameter parameter in parameters)
		{
			if (parameter.IsMockable)
			{
				sb.Append("\t\t\t\t").Append(Helpers.EscapeIfKeyword(parameter.Name)).Append(" ??= ")
					.Append(parameter.TypeFullName).Append(".CreateMock(").Append(mockBehavior).Append(");").AppendLine();
			}
			else if (parameter.IsParams)
			{
				sb.Append("\t\t\t\t").Append(Helpers.EscapeIfKeyword(parameter.Name)).Append(" ??= [];").AppendLine();
			}
		}

		string arguments = string.Join(", ",
			parameters.Select(p => Helpers.EscapeIfKeyword(p.Name) + (p.IsNullForgiving ? "!" : "")));
		sb.Append("\t\t\t\t").Append(mocks).Append(" = new ").Append(containerFullName).Append('(').Append(arguments)
			.Append(");").AppendLine();
		sb.Append("\t\t\t\treturn new ").Append(sut.ClassFullName).Append('(').Append(arguments).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
	}
}
