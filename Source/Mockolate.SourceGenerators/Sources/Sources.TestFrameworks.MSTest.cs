namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The <c>AutoMockArguments</c> attribute for MSTest, which is used on a <c>[TestMethod]</c> like <c>[DataRow]</c>.
	/// </summary>
	/// <remarks>
	///     MSTest v3 folds the test cases of the attribute, because a runner that saves the test cases would otherwise serialize
	///     their values. MSTest v4 moved this option to <c>TestMethodAttribute.UnfoldingStrategy</c> and
	///     <c>TestDataSourceOptionsAttribute</c>, which a test project sets.
	/// </remarks>
	public static string AutoMockArgumentsForMSTest()
		=> AutoMockArgumentsAttribute("MOCKOLATE_MSTEST", "MSTest", "MSTest can create the arguments during discovery,",
			"""
			global::System.Attribute,
			#if MOCKOLATE_MSTEST3
					global::Microsoft.VisualStudio.TestTools.UnitTesting.ITestDataSourceUnfoldingCapability,
			#endif
					global::Microsoft.VisualStudio.TestTools.UnitTesting.ITestDataSource
			""", ParameterInfoArguments + """
			                         		/// <inheritdoc />
			                         		public global::System.Collections.Generic.IEnumerable<object?[]> GetData(
			                         			global::System.Reflection.MethodInfo methodInfo)
			                         		{
			                         			yield return CreateArguments(methodInfo.GetParameters(), Arguments);
			                         		}

			                         		/// <inheritdoc />
			                         		public string? GetDisplayName(global::System.Reflection.MethodInfo methodInfo, object?[]? data)
			                         			=> global::Mockolate.Integrations.AutoMockRow.GetDisplayName(
			                         				global::System.Reflection.CustomAttributeExtensions
			                         					.GetCustomAttribute<global::Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute>(methodInfo, true)
			                         					?.DisplayName ?? methodInfo.Name,
			                         				Arguments);
			                         #if MOCKOLATE_MSTEST3

			                         		/// <summary>
			                         		///     Mocks cannot be serialized, so the arguments are created when the test runs.
			                         		/// </summary>
			                         		public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestDataSourceUnfoldingStrategy UnfoldingStrategy
			                         			=> global::Microsoft.VisualStudio.TestTools.UnitTesting.TestDataSourceUnfoldingStrategy.Fold;
			                         #endif
			                         """);
}
