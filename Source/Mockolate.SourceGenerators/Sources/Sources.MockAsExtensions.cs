using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     Emits the cross-interface <c>As&lt;T&gt;</c> bridge methods for every distinct unordered
	///     pair of mock-extension class names that appears in any combination mock. Splitting these
	///     out of <see cref="MockCombinationClass" /> lets the per-mock incremental output stay
	///     stable when only the set of <c>As&lt;T&gt;</c> pairs changes, and avoids the duplicate
	///     partial-method definitions that would otherwise occur if two combinations referenced the
	///     same pair.
	/// </summary>
	public static string MockAsExtensions(IEnumerable<MockAsExtensionPair> pairs)
	{
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable annotations").AppendLine();
		sb.Append("namespace Mockolate;").AppendLine();
		sb.AppendLine();

		foreach (MockAsExtensionPair pair in pairs)
		{
			AppendAsBridge(sb, pair.SourceName, pair.OtherName, pair.OtherFullName, pair.OtherXmlDocReference);
			AppendAsBridge(sb, pair.OtherName, pair.SourceName, pair.SourceFullName, pair.SourceXmlDocReference);
		}

		return sb.ToString();
	}

	private static void AppendAsBridge(StringBuilder sb, string sourceName, string otherName, string otherFullName,
		string otherReference)
	{
		sb.Append("internal static partial class MockExtensionsFor").Append(otherName).AppendLine();
		sb.Append("{").AppendLine();
		sb.Append("\textension(global::Mockolate.Mock.IMockFor").Append(sourceName).Append(" mock)").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary($"Reinterprets this mock as a mock of {otherReference} to reach its Setup/Verify/Raise surface.");
		sb.AppendXmlRemarks(
			"The returned accessor shares the same mock registry as this one - setups and verifications act on the same mocked instance. Use this when the mock implements multiple interfaces via <c>Implementing&lt;T&gt;()</c> and you need to configure or verify members of a different interface than the one the instance is currently typed as.");
		sb.AppendXmlReturns($"An <c>IMockFor...</c> accessor targeting {otherReference}.");
		sb.AppendXmlException("global::Mockolate.Exceptions.MockException",
			$"The subject does not implement {otherReference}.");
		sb.Append("\t\tpublic global::Mockolate.Mock.IMockFor").Append(otherName).Append(" As<T>() where T : ").Append(otherFullName).AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (mock is global::Mockolate.Mock.IMockFor").Append(otherName).Append(" typed)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn typed;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException($\"The subject does not support type {typeof(T)}.\");").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.Append("}").AppendLine();
		sb.AppendLine();
	}
}
