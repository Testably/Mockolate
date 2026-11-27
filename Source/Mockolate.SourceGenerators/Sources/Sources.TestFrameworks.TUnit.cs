namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for TUnit.
	/// </summary>
	public static string AutoMockArgumentsForTUnit()
		=> AutoMockArgumentsAttribute("MOCKOLATE_TUNIT", "TUnit", "TUnit creates the arguments before it runs any hook, and on another thread,",
			"global::TUnit.Core.UntypedDataSourceGeneratorAttribute", """
			                                                          		/// <inheritdoc />
			                                                          		protected override global::System.Collections.Generic.IEnumerable<global::System.Func<object?[]?>> GenerateDataSources(
			                                                          			global::TUnit.Core.DataGeneratorMetadata dataGeneratorMetadata)
			                                                          		{
			                                                          			// The attribute only applies to methods, so every member is a parameter.
			                                                          			global::TUnit.Core.ParameterMetadata[] parameters = global::System.Array.ConvertAll(
			                                                          				dataGeneratorMetadata.MembersToGenerate, member => (global::TUnit.Core.ParameterMetadata)member);
			                                                          			global::System.Type[] types = global::System.Array.ConvertAll(parameters, parameter => parameter.Type);
			                                                          			string[] names = global::System.Array.ConvertAll(parameters, parameter => parameter.Name);
			                                                          			// The attribute is generated into every test assembly, next to the code that creates the parameters.
			                                                          			global::System.Reflection.Assembly assembly = typeof(AutoMockArgumentsAttribute).Assembly;
			                                                          			// Only a parameter that could be shared needs its reflection info, which older versions of TUnit can lack.
			                                                          			yield return () => new global::Mockolate.Integrations.AutoMockRow(assembly, types, names,
			                                                          				index => parameters[index].ReflectionInfo is { } reflectionInfo &&
			                                                          				         global::System.Reflection.CustomAttributeExtensions.IsDefined(reflectionInfo,
			                                                          					         typeof(global::Mockolate.Integrations.NotSharedParameterAttribute), false),
			                                                          				index => parameters[index].ReflectionInfo is { } reflectionInfo &&
			                                                          				         global::System.Reflection.CustomAttributeExtensions.IsDefined(reflectionInfo,
			                                                          					         typeof(global::Mockolate.Integrations.DeferredParameterAttribute), false),
			                                                          				index => parameters[index].ReflectionInfo is { } reflectionInfo
			                                                          					? global::Mockolate.Integrations.AutoMockRow.GetAsDefaultTypes(reflectionInfo)
			                                                          					: null,
			                                                          				index => parameters[index].ReflectionInfo is { } reflectionInfo
			                                                          					? global::Mockolate.Integrations.AutoMockRow.GetValueFrom(reflectionInfo)
			                                                          					: null,
			                                                          				Arguments, DefaultBehavior?.Invoke()).Create();
			                                                          		}
			                                                          """);
}
