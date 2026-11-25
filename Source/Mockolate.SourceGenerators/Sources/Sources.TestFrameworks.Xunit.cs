namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for xUnit v2, which a project that also references xUnit v3 does not get.
	/// </summary>
	public static string AutoMockArgumentsForXunit2()
		=> AutoMockArgumentsAttribute("MOCKOLATE_XUNIT2 && !MOCKOLATE_XUNIT3", "Xunit", "xUnit can create the arguments during discovery, on another thread,",
			"global::Xunit.Sdk.DataAttribute", ParameterInfoArguments + """
			                                                            		/// <inheritdoc />
			                                                            		public override global::System.Collections.Generic.IEnumerable<object[]> GetData(
			                                                            			global::System.Reflection.MethodInfo testMethod)
			                                                            		{
			                                                            			yield return CreateArguments(testMethod.GetParameters(), Arguments)!;
			                                                            		}
			                                                            """);

	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for xUnit v3.
	/// </summary>
	public static string AutoMockArgumentsForXunit3()
		=> AutoMockArgumentsAttribute("MOCKOLATE_XUNIT3", "Xunit", "xUnit creates the arguments before it runs the test, possibly on another thread,",
			"global::Xunit.v3.DataAttribute", ParameterInfoArguments + """
			                                                           		/// <inheritdoc />
			                                                           		public override global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IReadOnlyCollection<global::Xunit.ITheoryDataRow>> GetData(
			                                                           			global::System.Reflection.MethodInfo testMethod, global::Xunit.Sdk.DisposalTracker disposalTracker)
			                                                           		{
			                                                           			// Like xUnit's InlineData, the row takes every setting of the attribute that the version of xUnit knows.
			                                                           			return new global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.IReadOnlyCollection<global::Xunit.ITheoryDataRow>>(
			                                                           				new global::Xunit.ITheoryDataRow[] { ConvertDataRow(CreateArguments(testMethod.GetParameters(), Arguments)), });
			                                                           		}

			                                                           		/// <summary>
			                                                           		///     Mocks cannot be serialized, so the arguments are created when the test runs.
			                                                           		/// </summary>
			                                                           		public override bool SupportsDiscoveryEnumeration()
			                                                           			=> false;
			                                                           """);
}
