using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     Creates the <c>MockDataSourceFactory</c> for the <paramref name="classes" />, the
	///     <paramref name="mockedTypes" />, the <paramref name="valueOrMockTypes" /> and the
	///     <paramref name="valueTypes" />, which a module initializer registers.
	/// </summary>
	/// <remarks>
	///     The factory lives in a file-local class, so that neither its registration nor its name reach the users of
	///     <c>Mock</c>.
	/// </remarks>
	public static string MockDataSources(EquatableArray<DataSourceClass> classes, EquatableArray<string> mockedTypes,
		EquatableArray<string> valueOrMockTypes, EquatableArray<string> valueTypes, bool emitModuleInitializerPolyfill)
	{
		DataSourceClass[] classArray = classes.AsArray();
		Dictionary<string, int> classIndices = new(StringComparer.Ordinal);
		for (int i = 0; i < classArray.Length; i++)
		{
			classIndices[classArray[i].ClassFullName] = i;
		}

		HashSet<(int, int)> cyclicDependencies = GetCyclicDependencies(classArray, classIndices);

		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable").AppendLine();
		// A dependency that spells a type argument with other nullable annotations than the CreateMock call shares its
		// mock, whose extension then differs only in nullability.
		sb.Append("#pragma warning disable CS8619, CS8620").AppendLine();
		sb.AppendLine();
		if (emitModuleInitializerPolyfill)
		{
			sb.Append("""
			          namespace System.Runtime.CompilerServices
			          {
			          	/// <summary>
			          	///     Polyfill for the module initializer attribute; the runtime ships it starting with .NET 5.
			          	/// </summary>
			          	[global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
			          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
			          	internal sealed class ModuleInitializerAttribute : global::System.Attribute
			          	{
			          	}
			          }

			          """).AppendLine();
		}

		sb.Append("namespace Mockolate").AppendLine();
		sb.Append('{').AppendLine();
		#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tfile static class MockDataSourceRegistration").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("#pragma warning disable CA2255, CS0436 // Module initializers in libraries; the polyfill next to an imported attribute").AppendLine();
		sb.Append("\t\t[global::System.Runtime.CompilerServices.ModuleInitializer]").AppendLine();
		sb.Append("\t\tinternal static void Register()").AppendLine();
		sb.Append("\t\t\t=> global::Mockolate.Internals.MockDataSourceRegistry.Register(typeof(MockDataSourceRegistration).Assembly, Create);")
			.AppendLine();
		sb.Append("#pragma warning restore CA2255, CS0436").AppendLine();
		sb.AppendLine();
		sb.Append("\t\tprivate static bool Create(global::System.Type type, global::Mockolate.MockBehavior? behavior, global::Mockolate.MockArgumentResolver resolve, out object? value)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 0; i < classArray.Length; i++)
		{
			sb.Append("\t\t\tif (type == typeof(").Append(classArray[i].ClassFullName).Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = Create").Append(i).Append("(behavior, resolve, null);").AppendLine();
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

		foreach (string valueOrMockType in valueOrMockTypes)
		{
			sb.Append("\t\t\tif (type == typeof(").Append(valueOrMockType).Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = ").Append(TryCreateValue(valueOrMockType, "createdValue")).Append(" ? createdValue : ")
				.Append(valueOrMockType).Append(".CreateMock(behavior ?? global::Mockolate.MockBehavior.Default);")
				.AppendLine();
			sb.Append("\t\t\t\treturn true;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		foreach (string valueType in valueTypes)
		{
			sb.Append("\t\t\tif (type == typeof(").Append(valueType).Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tbool created = ").Append(TryCreateValue(valueType, "createdValue")).Append(';').AppendLine();
			sb.Append("\t\t\t\tvalue = createdValue;").AppendLine();
			sb.Append("\t\t\t\treturn created;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t\tvalue = null;").AppendLine();
		sb.Append("\t\t\treturn false;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		for (int i = 0; i < classArray.Length; i++)
		{
			DataSourceClass dataSourceClass = classArray[i];
			string className = dataSourceClass.ClassFullName.Replace("global::", "").Replace("@", "");
			bool closesCycle = cyclicDependencies.Any(dependency => dependency.Item1 == i);
			sb.AppendLine();
			sb.Append("\t\tprivate static ").Append(dataSourceClass.ClassFullName).Append(" Create").Append(i)
				.Append("(global::Mockolate.MockBehavior? behavior, global::Mockolate.MockArgumentResolver resolve, CreationPath? path)")
				.AppendLine();
			sb.Append("\t\t{").AppendLine();
			if (closesCycle)
			{
				sb.Append("\t\t\tCreationPath self = new(").Append(i).Append(", path);").AppendLine();
			}

			sb.Append("\t\t\treturn new ").Append(dataSourceClass.ClassFullName).Append('(')
				.Append(string.Join(", ", dataSourceClass.Parameters.AsArray().Select((parameter, index) =>
					Argument(parameter, index, className, i))))
				.Append(");").AppendLine();
			sb.Append("\t\t}").AppendLine();
		}

		sb.AppendLine();
		sb.Append("\t\tprivate static bool IsValue<T>(T value, out T result)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tresult = value;").AppendLine();
		sb.Append("\t\t\treturn value is not null || typeof(T).IsValueType;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t/// <summary>").AppendLine();
		sb.Append("\t\t///     The classes that are being created, so that a dependency that depends on one of them again is not created.").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		sb.Append("\t\tprivate sealed class CreationPath").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tprivate readonly int _index;").AppendLine();
		sb.Append("\t\t\tprivate readonly CreationPath? _parent;").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tpublic CreationPath(int index, CreationPath? parent)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t_index = index;").AppendLine();
		sb.Append("\t\t\t\t_parent = parent;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tpublic bool Contains(int index)").AppendLine();
		sb.Append("\t\t\t\t=> _index == index || (_parent?.Contains(index) ?? false);").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.Append('}').AppendLine();
		sb.Append("#nullable disable").AppendLine();
		return sb.ToString();

		string Argument(DataSourceParameter parameter, int index, string className, int classIndex)
		{
			string fallback = parameter switch
			{
				{ IsMockable: true, } => $"{parameter.TypeFullName}.CreateMock(behavior ?? global::Mockolate.MockBehavior.Default)",
				{ IsParams: true, } => "[]",
				// A non-nullable parameter can declare a null! default.
				{ DefaultValue: "null", } => "default!",
				{ DefaultValue: { } defaultValue, } => $"({parameter.TypeFullName})({defaultValue})",
				_ => NotCreated(parameter, index, className),
			};
			if (parameter is { IsMockable: false, IsParams: false, DefaultValue: null, IsCreatable: true, } &&
			    classIndices.TryGetValue(parameter.TypeFullName, out int dependency))
			{
				// Only a dependency that can lead back to this class passes the path on: below any other one, no class
				// of the path can be created again.
				fallback = cyclicDependencies.Contains((classIndex, dependency))
					? $"self.Contains({dependency}) ? {NotCreated(parameter, index, className)} : Create{dependency}(behavior, resolve, self)"
					: $"Create{dependency}(behavior, resolve, null)";
			}

			return $"resolve(typeof({parameter.TypeFullName}), \"{parameter.Name}\", out object? v{index}) ? global::Mockolate.Internals.MockDataSourceRegistry.Cast<{parameter.TypeFullName}>(v{index}, \"{parameter.Name}\", \"{className}\") : {fallback}";
		}

		// A value from the default values of the behavior is the last resort before a mock of a framework interface or
		// the failure.
		static string NotCreated(DataSourceParameter parameter, int index, string className)
		{
			if (parameter.IsNullable)
			{
				return "default";
			}

			string lastResort = parameter.IsMockedLast
				? $"{parameter.TypeFullName}.CreateMock(behavior ?? global::Mockolate.MockBehavior.Default)"
				: $"throw new global::Mockolate.Exceptions.MockException(\"There is no argument for the constructor parameter '{parameter.Name}' of {className}.\")";
			return $"{TryCreateValue(parameter.TypeFullName, $"d{index}")} ? d{index} : {lastResort}";
		}
		// The typed overloads of Generate return what an un-configured mock member returns, e.g. an empty collection.
		static string TryCreateValue(string type, string variable)
			=> $"IsValue((behavior ?? global::Mockolate.MockBehavior.Default).DefaultValue.Generate(default({type})!), out {type} {variable})";
	}

	/// <summary>
	///     The dependencies from a class to a class that can depend on it again, which check the creation path, so
	///     that the created classes do not recurse without end.
	/// </summary>
	private static HashSet<(int, int)> GetCyclicDependencies(DataSourceClass[] classes,
		Dictionary<string, int> classIndices)
	{
		List<int>[] dependencies = Array.ConvertAll(classes, dataSourceClass => dataSourceClass.Parameters.AsArray()
			.Where(parameter => parameter.IsCreatable)
			.Select(parameter => classIndices.TryGetValue(parameter.TypeFullName, out int index) ? index : -1)
			.Where(index => index >= 0)
			.ToList());
		HashSet<(int, int)> cyclic = [];
		for (int from = 0; from < classes.Length; from++)
		{
			foreach (int to in dependencies[from].Where(to => Reaches(to, from)))
			{
				cyclic.Add((from, to));
			}
		}

		return cyclic;

		bool Reaches(int start, int target)
		{
			HashSet<int> visited = [];
			Stack<int> pending = new([start,]);
			while (pending.Count > 0)
			{
				int current = pending.Pop();
				if (current == target)
				{
					return true;
				}

				if (visited.Add(current))
				{
					foreach (int next in dependencies[current])
					{
						pending.Push(next);
					}
				}
			}

			return false;
		}
	}
}
