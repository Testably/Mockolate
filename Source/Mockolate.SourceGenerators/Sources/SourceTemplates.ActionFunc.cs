using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	internal static string ActionFunc(IEnumerable<int> parameterCounts)
	{
		StringBuilder sb = InitializeBuilder();

		sb.AppendLine("""
		              namespace System;

		              #nullable enable

		              """);
		foreach (int parameterCount in parameterCounts)
		{
			sb.AppendLine($$"""
			                /// <summary>
			                ///     Encapsulates a method that has {{parameterCount}} parameters and does not return a value.
			                /// </summary>
			                public delegate void Action<{{string.Join(", ", Enumerable.Range(1, parameterCount).Select(i => $"in T{i}"))}}>({{string.Join(", ", Enumerable.Range(1, parameterCount).Select(i => $"T{i} arg{i}"))}});
			                """);
			sb.AppendLine();
			sb.AppendLine($$"""
			                /// <summary>
			                ///     Encapsulates a method that has {{parameterCount}} parameters and returns a value of the type specified by the <typeparamref name="TResult" /> parameter.
			                /// </summary>
			                public delegate TResult Func<{{string.Join(", ", Enumerable.Range(1, parameterCount).Select(i => $"in T{i}"))}}, out TResult>({{string.Join(", ", Enumerable.Range(1, parameterCount).Select(i => $"T{i} arg{i}"))}});
			                """);
			sb.AppendLine();
		}

		sb.AppendLine("#nullable disable");
		return sb.ToString();
	}
}
