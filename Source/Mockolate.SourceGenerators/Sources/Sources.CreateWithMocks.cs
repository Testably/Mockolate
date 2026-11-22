using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     Creates the <c>CreateWithMocks</c> extension and the container of the dependencies for every class in
	///     <paramref name="suts" />, and the <c>MockDataSourceFactory</c> that a module initializer registers for the
	///     <paramref name="suts" /> and the <paramref name="mockedTypes" />.
	/// </summary>
	public static string CreateWithMocks(EquatableArray<SutClass> suts, EquatableArray<string> mockedTypes,
		bool emitModuleInitializerPolyfill)
	{
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable").AppendLine();
		sb.Append("#pragma warning disable CA2255 // The ModuleInitializer attribute should not be used in libraries").AppendLine();
		sb.AppendLine();
		if (emitModuleInitializerPolyfill)
		{
			sb.Append("""
			          namespace System.Runtime.CompilerServices
			          {
			          	[global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
			          	internal sealed class ModuleInitializerAttribute : global::System.Attribute;
			          }

			          """).AppendLine();
		}

		sb.Append("namespace Mockolate").AppendLine();
		sb.Append('{').AppendLine();
		sb.Append("\tinternal static partial class Mock").AppendLine();
		sb.Append("\t{").AppendLine();

		Dictionary<string, int> nameCounts = suts
			.GroupBy(sut => sut.Name)
			.ToDictionary(group => group.Key, group => group.Count());
		foreach (SutClass sut in suts)
		{
			string containerName = (nameCounts[sut.Name] > 1 ? ToIdentifier(sut.ClassFullName) : sut.Name) + "Mocks";
			AppendSut(sb, sut, containerName);
		}

		AppendFactory(sb, suts, mockedTypes);
		sb.Append("\t}").AppendLine();
		sb.Append('}').AppendLine();
		sb.Append("#nullable disable").AppendLine();
		return sb.ToString();
	}

	private static void AppendSut(StringBuilder sb, SutClass sut, string containerName)
	{
		SutParameter[] parameters = sut.Parameters.AsArray();

		sb.Append("\t\t/// <summary>").AppendLine();
		sb.Append("\t\t///     The constructor arguments of the <see cref=\"").Append(sut.ClassFullName)
			.Append("\" /> that <c>CreateWithMocks</c> created.").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		sb.Append("\t\tinternal sealed class ").Append(containerName).AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tinternal ").Append(containerName).Append('(')
			.Append(string.Join(", ", parameters.Select(p => p.TypeFullName + " " + Helpers.EscapeIfKeyword(p.Name))))
			.Append(')').AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		foreach (SutParameter parameter in parameters)
		{
			sb.Append("\t\t\t\t").Append(ToPropertyName(parameter.Name)).Append(" = ")
				.Append(Helpers.EscapeIfKeyword(parameter.Name)).Append(';').AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();
		foreach (SutParameter parameter in parameters)
		{
			sb.AppendLine();
			sb.Append("\t\t\t/// <summary>").AppendLine();
			sb.Append("\t\t\t///     The argument of the constructor parameter <c>").Append(parameter.Name).Append("</c>.")
				.AppendLine();
			sb.Append("\t\t\t/// </summary>").AppendLine();
			sb.Append("\t\t\tpublic ").Append(parameter.TypeFullName).Append(' ').Append(ToPropertyName(parameter.Name))
				.Append(" { get; }").AppendLine();
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\textension(").Append(sut.ClassFullName).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t/// <summary>").AppendLine();
		sb.Append("\t\t\t///     Creates a <see cref=\"").Append(sut.ClassFullName)
			.Append("\" /> with a new mock for every mockable constructor parameter without an argument.").AppendLine();
		sb.Append("\t\t\t/// </summary>").AppendLine();
		sb.Append("\t\t\tpublic static ").Append(sut.ClassFullName).Append(" CreateWithMocks(out ").Append(containerName)
			.Append(" mocks");
		foreach (SutParameter parameter in parameters.Where(p => !p.IsMockable))
		{
			sb.Append(", ").Append(parameter.TypeFullName).Append(' ').Append(Helpers.EscapeIfKeyword(parameter.Name));
		}

		sb.Append(", global::Mockolate.MockBehavior? mockBehavior = null");
		foreach (SutParameter parameter in parameters.Where(p => p.IsMockable))
		{
			sb.Append(", ").Append(parameter.TypeFullName).Append("? ").Append(Helpers.EscapeIfKeyword(parameter.Name))
				.Append(" = null");
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		foreach (SutParameter parameter in parameters.Where(p => p.IsMockable))
		{
			sb.Append("\t\t\t\t").Append(Helpers.EscapeIfKeyword(parameter.Name)).Append(" ??= ")
				.Append(parameter.TypeFullName).Append(".CreateMock(mockBehavior);").AppendLine();
		}

		string arguments = string.Join(", ", parameters.Select(p => Helpers.EscapeIfKeyword(p.Name)));
		sb.Append("\t\t\t\tmocks = new ").Append(containerName).Append('(').Append(arguments).Append(");").AppendLine();
		sb.Append("\t\t\t\treturn new ").Append(sut.ClassFullName).Append('(').Append(arguments).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	private static void AppendFactory(StringBuilder sb, EquatableArray<SutClass> suts,
		EquatableArray<string> mockedTypes)
	{
		sb.Append("\t\t[global::System.Runtime.CompilerServices.ModuleInitializer]").AppendLine();
		sb.Append("\t\tinternal static void RegisterMockDataSources()").AppendLine();
		sb.Append("\t\t\t=> global::Mockolate.MockDataSources.Register(typeof(Mock).Assembly, CreateFromMockDataSource);")
			.AppendLine();
		sb.AppendLine();
		sb.Append("\t\tprivate static bool CreateFromMockDataSource(global::System.Type type, global::Mockolate.MockBehavior? behavior, global::Mockolate.DependencyResolver resolve, out object? value)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		foreach (SutClass sut in suts)
		{
			SutParameter[] parameters = sut.Parameters.AsArray();
			sb.Append("\t\t\tif (type == typeof(").Append(sut.ClassFullName).Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = ").Append(sut.ClassFullName).Append(".CreateWithMocks(out _");
			foreach (int index in Enumerable.Range(0, parameters.Length).Where(i => !parameters[i].IsMockable))
			{
				sb.Append(", ").Append(Resolve(parameters[index], index));
			}

			sb.Append(", behavior");
			foreach (int index in Enumerable.Range(0, parameters.Length).Where(i => parameters[i].IsMockable))
			{
				sb.Append(", ").Append(Resolve(parameters[index], index));
			}

			sb.Append(");").AppendLine();
			sb.Append("\t\t\t\treturn true;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		foreach (string mockedType in mockedTypes)
		{
			sb.Append("\t\t\tif (type == typeof(").Append(mockedType).Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = ").Append(mockedType)
				.Append(".CreateMock(behavior ?? global::Mockolate.MockBehavior.Default);").AppendLine();
			sb.Append("\t\t\t\treturn true;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t\tvalue = null;").AppendLine();
		sb.Append("\t\t\treturn false;").AppendLine();
		sb.Append("\t\t}").AppendLine();

		static string Resolve(SutParameter parameter, int index)
			=> $"resolve(typeof({parameter.TypeFullName}), \"{parameter.Name}\") is {{ }} v{index} ? ({parameter.TypeFullName})v{index} : default!";
	}

	private static string ToPropertyName(string parameterName)
		=> char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);

	private static string ToIdentifier(string fullName)
		=> new(fullName.Replace("global::", "").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}
