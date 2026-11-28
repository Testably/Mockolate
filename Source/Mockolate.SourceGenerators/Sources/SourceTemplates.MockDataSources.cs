using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	/// <summary>
	///     A class parameter is the class under test, so its mock always forwards to the real implementation; only its
	///     dependencies follow a behavior that skips the base class.
	/// </summary>
	private const string ForwardingBehavior =
		"(behavior ?? global::Mockolate.MockBehavior.Default).SkippingBaseClass(false)";

	/// <summary>
	///     Creates the <c>MockDataSourceFactory</c> for the <paramref name="classes" />, the
	///     <paramref name="mockedTypes" />, the <paramref name="valueOrMockTypes" /> and the
	///     <paramref name="valueTypes" />, and the one for the <c>Func&lt;T&gt;</c> of the <paramref name="deferredTypes" />,
	///     which a module initializer registers. The module initializer also keeps the fields, properties and methods of the
	///     <paramref name="valueFromClasses" /> (test classes and their base classes) from being trimmed, because a <c>[ValueFrom]</c> parameter reads them through
	///     reflection.
	/// </summary>
	/// <remarks>
	///     The factory lives in a file-local class, so that neither its registration nor its name reach the users of
	///     <c>Mock</c>.
	/// </remarks>
	public static string MockDataSources(EquatableArray<DataSourceClass> classes, EquatableArray<string> mockedTypes,
		EquatableArray<string> valueOrMockTypes, EquatableArray<string> valueTypes, EquatableArray<string> deferredTypes,
		EquatableArray<string> valueFromClasses, bool emitModuleInitializerPolyfill)
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
		AppendDebuggerNonUserCode(sb, "\t");
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tfile static class MockDataSourceRegistration").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("#pragma warning disable CA2255, CS0436 // Module initializers in libraries; the polyfill next to an imported attribute").AppendLine();
		sb.Append("\t\t[global::System.Runtime.CompilerServices.ModuleInitializer]").AppendLine();
		if (valueFromClasses.Count > 0)
		{
			sb.Append("#if NET5_0_OR_GREATER").AppendLine();
			foreach (string valueFromClass in valueFromClasses)
			{
				sb.Append("\t\t[global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods, ")
					.Append(valueFromClass).Append(")]").AppendLine();
			}

			sb.Append("#endif").AppendLine();
		}

		sb.Append("\t\tinternal static void Register()").AppendLine();
		sb.Append("\t\t\t=> global::Mockolate.Internals.MockDataSourceRegistry.Register(typeof(MockDataSourceRegistration).Assembly, Create")
			.Append(deferredTypes.Count > 0 ? ", CreateDeferred" : "").Append(");").AppendLine();
		sb.Append("#pragma warning restore CA2255, CS0436").AppendLine();
		sb.AppendLine();
		sb.Append("\t\tprivate static bool Create(global::System.Type type, global::Mockolate.MockBehavior? behavior, global::Mockolate.Internals.MockArgumentResolver resolve, out object? value)")
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
		if (deferredTypes.Count > 0)
		{
			AppendDeferredFactory(sb, deferredTypes);
		}

		for (int i = 0; i < classArray.Length; i++)
		{
			DataSourceClass dataSourceClass = classArray[i];
			string className = dataSourceClass.ClassFullName.Replace("global::", "").Replace("@", "");
			bool closesCycle = cyclicDependencies.Any(dependency => dependency.Item1 == i);
			sb.AppendLine();
			sb.Append("\t\tprivate static ").Append(dataSourceClass.ClassFullName).Append(" Create").Append(i)
				.Append("(global::Mockolate.MockBehavior? behavior, global::Mockolate.Internals.MockArgumentResolver resolve, CreationPath? path)")
				.AppendLine();
			sb.Append("\t\t{").AppendLine();
			if (closesCycle)
			{
				sb.Append("\t\t\tCreationPath self = new(").Append(i).Append(", path);").AppendLine();
			}

			if (!dataSourceClass.IsMock)
			{
				sb.Append("\t\t\treturn new ").Append(dataSourceClass.ClassFullName).Append('(')
					.Append(string.Join(", ", dataSourceClass.Parameters.AsArray().Select((parameter, index) =>
						Argument(parameter, index, className, i))))
					.Append(");").AppendLine();
			}
			else if (dataSourceClass.Parameters.Count == 0)
			{
				sb.Append("\t\t\treturn ").Append(dataSourceClass.ClassFullName)
					.Append(".CreateMock(").Append(ForwardingBehavior).Append(");").AppendLine();
			}
			else
			{
				// The typed locals give a collection expression its type, and the object array reaches only the
				// constructor whose arguments it holds.
				DataSourceParameter[] parameters = dataSourceClass.Parameters.AsArray();
				for (int index = 0; index < parameters.Length; index++)
				{
					sb.Append("\t\t\tvar a").Append(index).Append(" = ").Append(Argument(parameters[index], index, className, i))
						.Append(';').AppendLine();
				}

				sb.Append("\t\t\treturn ").Append(dataSourceClass.ClassFullName)
					.Append(".CreateMock(").Append(ForwardingBehavior).Append(", new object?[] { ")
					.Append(string.Join(", ", parameters.Select((_, index) => FormattableString.Invariant($"a{index}")))).Append(", });").AppendLine();
			}

			sb.Append("\t\t}").AppendLine();
		}

		sb.AppendLine();
		// The typed overloads of Generate return an empty collection without asking the generator, so a configured
		// value is looked up first. The typed default only runs without one, so that a configured factory is called once.
		sb.Append("\t\tprivate static bool IsValue<T>(global::Mockolate.MockBehavior behavior, global::System.Func<global::Mockolate.MockBehavior, T> typedDefault, out T result)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (behavior.DefaultValue.GenerateValue(typeof(T)) is T configured)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tresult = configured;").AppendLine();
		sb.Append("\t\t\t\treturn true;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tresult = typedDefault(behavior);").AppendLine();
		sb.Append("\t\t\treturn result is not null || typeof(T).IsValueType;").AppendLine();
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
					? FormattableString.Invariant($"self.Contains({dependency}) ? {NotCreated(parameter, index, className)} : Create{dependency}(behavior, resolve, self)")
					: FormattableString.Invariant($"Create{dependency}(behavior, resolve, null)");
			}

			return FormattableString.Invariant($"resolve(typeof({parameter.TypeFullName}), \"{parameter.Name}\", out object? v{index}) ? global::Mockolate.Internals.MockDataSourceRegistry.Cast<{parameter.TypeFullName}>(v{index}, \"{parameter.Name}\", \"{className}\") : {fallback}");
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
			return FormattableString.Invariant($"{TryCreateValue(parameter.TypeFullName, FormattableString.Invariant($"d{index}"))} ? d{index} : {lastResort}");
		}

		static string TryCreateValue(string type, string variable)
			=> $"IsValue(behavior ?? global::Mockolate.MockBehavior.Default, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default({type})!), out {type} {variable})";
	}

	/// <summary>
	///     The factory of the <c>Func&lt;T&gt;</c> of each of the <paramref name="deferredTypes" />, whose calls create a
	///     new <c>T</c> with the behavior and the arguments of the test case.
	/// </summary>
	private static void AppendDeferredFactory(StringBuilder sb, EquatableArray<string> deferredTypes)
	{
		sb.AppendLine();
		sb.Append("\t\tprivate static bool CreateDeferred(global::System.Type type, global::Mockolate.MockBehavior? behavior, global::Mockolate.Internals.MockArgumentResolver resolve, out object? value)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		foreach (string deferredType in deferredTypes)
		{
			sb.Append("\t\t\tif (type == typeof(global::System.Func<").Append(deferredType).Append(">))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = new global::System.Func<").Append(deferredType).Append(">(() => CreateNew<")
				.Append(deferredType).Append(">(behavior, resolve));").AppendLine();
			sb.Append("\t\t\t\treturn true;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t\tvalue = null;").AppendLine();
		sb.Append("\t\t\treturn false;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\tprivate static T CreateNew<T>(global::Mockolate.MockBehavior? behavior, global::Mockolate.Internals.MockArgumentResolver resolve)")
			.AppendLine();
		sb.Append("\t\t\t=> Create(typeof(T), behavior, resolve, out object? value)").AppendLine();
		sb.Append("\t\t\t\t? (T)value!").AppendLine();
		sb.Append("\t\t\t\t: throw new global::Mockolate.Exceptions.MockException($\"Mockolate cannot create a value of type '{typeof(T)}'.\");")
			.AppendLine();
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
		// Tarjan's algorithm: a dependency can lead back exactly when both classes are in one strongly connected
		// component.
		int[] order = Enumerable.Repeat(-1, classes.Length).ToArray();
		int[] lowLink = new int[classes.Length];
		int[] component = Enumerable.Repeat(-1, classes.Length).ToArray();
		Stack<int> open = new();
		int nextOrder = 0;
		int nextComponent = 0;
		for (int index = 0; index < classes.Length; index++)
		{
			if (order[index] < 0)
			{
				Visit(index);
			}
		}

		HashSet<(int, int)> cyclic = [];
		for (int from = 0; from < classes.Length; from++)
		{
			foreach (int to in dependencies[from].Where(to => component[to] == component[from]))
			{
				cyclic.Add((from, to));
			}
		}

		return cyclic;

		void Visit(int current)
		{
			order[current] = lowLink[current] = nextOrder++;
			open.Push(current);
			foreach (int next in dependencies[current])
			{
				if (order[next] < 0)
				{
					Visit(next);
					lowLink[current] = Math.Min(lowLink[current], lowLink[next]);
				}
				else if (component[next] < 0)
				{
					lowLink[current] = Math.Min(lowLink[current], order[next]);
				}
			}

			if (lowLink[current] == order[current])
			{
				int member;
				do
				{
					member = open.Pop();
					component[member] = nextComponent;
				} while (member != current);

				nextComponent++;
			}
		}
	}
}
