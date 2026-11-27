using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	public static string ReturnsThrowsAsyncExtensions(int[] numberOfParameters)
	{
		static void AppendSequenceRemark(StringBuilder sb)
		{
			sb.Append("\t///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles").AppendLine();
			sb.Append("\t///     back to the first entry unless the last one is followed by <c>.Forever()</c>.").AppendLine();
		}

		StringBuilder sb = InitializeBuilder();

		sb.Append("""
		          namespace Mockolate;

		          #nullable enable

		          """);
		sb.AppendLine();
		sb.Append("/// <summary>").AppendLine();
		sb.Append("///     Extensions for setting up return values and throwing exceptions for <see langword=\"async\" /> methods.").AppendLine();
		sb.Append("/// </summary>").AppendLine();
#if !DEBUG
		sb.Append("[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("internal static class ReturnsThrowsAsyncExtensions2").AppendLine();
		sb.Append("{").AppendLine();
		foreach (int number in numberOfParameters)
		{
			string types = string.Join(", ", Enumerable.Range(1, number).Select(i => FormattableString.Invariant($"T{i}")));
			string variables = string.Join(", ", Enumerable.Range(1, number).Select(i => FormattableString.Invariant($"v{i}")));
			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends <paramref name=\"returnValue\" /> to the sequence - the next matching invocation returns a completed")
				.AppendLine();
			sb.Append(
					"\t///     <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" /> carrying this value.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, TReturn returnValue)").AppendLine();
			sb.Append("\t\t=> setup.Returns(global::System.Threading.Tasks.Task.FromResult(returnValue));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends a lazy async return to the sequence; <paramref name=\"callback\" /> is invoked on each matching")
				.AppendLine();
			sb.Append(
					"\t///     invocation and its result is wrapped in a completed <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" />.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, global::System.Func<TReturn> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns(() => global::System.Threading.Tasks.Task.FromResult(callback()));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a")
				.AppendLine();
			sb.Append(
					"\t///     completed <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" />.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetupWithCallback<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, global::System.Func<").Append(types).Append(", TReturn> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns((").Append(variables).Append(") => global::System.Threading.Tasks.Task.FromResult(callback(")
				.Append(variables).Append(")));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that faults the returned <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" /> with")
				.AppendLine();
			sb.Append(
					"\t///     <paramref name=\"exception\" /> so awaiting it throws.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, global::System.Exception exception)").AppendLine();
			sb.Append("\t\t=> setup.Returns(global::System.Threading.Tasks.Task.FromException<TReturn>(exception));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that invokes <paramref name=\"callback\" /> to build the exception the returned")
				.AppendLine();
			sb.Append(
					"\t///     <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" /> is faulted with.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, global::System.Func<global::System.Exception> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns(() => global::System.Threading.Tasks.Task.FromException<TReturn>(callback()));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that invokes <paramref name=\"callback\" /> with the method's arguments to build the")
				.AppendLine();
			sb.Append(
					"\t///     exception the returned <see cref=\"global::System.Threading.Tasks.Task{TReturn}\" /> is faulted with.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.Task<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types).Append(">(this global::Mockolate.Setup.IReturnMethodSetupWithCallback<global::System.Threading.Tasks.Task<TReturn>, ")
				.Append(types).Append("> setup, global::System.Func<").Append(types).Append(", global::System.Exception> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns((").Append(variables).Append(") => global::System.Threading.Tasks.Task.FromException<TReturn>(callback(")
				.Append(variables).Append(")));").AppendLine();
			sb.AppendLine();
		}

		sb.AppendLine("#if NET8_0_OR_GREATER");
		sb.AppendLine();
		foreach (int number in numberOfParameters)
		{
			string types = string.Join(", ", Enumerable.Range(1, number).Select(i => FormattableString.Invariant($"T{i}")));
			string variables = string.Join(", ", Enumerable.Range(1, number).Select(i => FormattableString.Invariant($"v{i}")));
			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends <paramref name=\"returnValue\" /> to the sequence - the next matching invocation returns a completed")
				.AppendLine();
			sb.Append(
					"\t///     <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" /> carrying this value.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> setup, TReturn returnValue)").AppendLine();
			sb.Append("\t\t=> setup.Returns(global::System.Threading.Tasks.ValueTask.FromResult(returnValue));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends a lazy async return to the sequence; <paramref name=\"callback\" /> is invoked on each matching")
				.AppendLine();
			sb.Append(
					"\t///     invocation and its result is wrapped in a completed <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" />.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> setup, global::System.Func<TReturn> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns(() => global::System.Threading.Tasks.ValueTask.FromResult(callback()));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a")
				.AppendLine();
			sb.Append(
					"\t///     completed <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" />.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ReturnsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetupWithCallback<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types).Append("> setup, global::System.Func<")
				.Append(types).Append(", TReturn> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns((").Append(variables).Append(") => global::System.Threading.Tasks.ValueTask.FromResult(callback(")
				.Append(variables).Append(")));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that faults the returned <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" /> with")
				.AppendLine();
			sb.Append(
					"\t///     <paramref name=\"exception\" /> so awaiting it throws.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> setup, global::System.Exception exception)").AppendLine();
			sb.Append("\t\t=> setup.Returns(global::System.Threading.Tasks.ValueTask.FromException<TReturn>(exception));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that invokes <paramref name=\"callback\" /> to build the exception the returned")
				.AppendLine();
			sb.Append(
					"\t///     <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" /> is faulted with.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetup<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> setup, global::System.Func<global::System.Exception> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns(() => global::System.Threading.Tasks.ValueTask.FromException<TReturn>(callback()));").AppendLine();
			sb.AppendLine();

			sb.Append("\t/// <summary>").AppendLine();
			sb.Append(
					"\t///     Appends an entry that invokes <paramref name=\"callback\" /> with the method's arguments to build the")
				.AppendLine();
			sb.Append(
					"\t///     exception the returned <see cref=\"global::System.Threading.Tasks.ValueTask{TReturn}\" /> is faulted with.")
				.AppendLine();
			sb.Append("\t/// </summary>").AppendLine();
			sb.Append("\t/// <remarks>").AppendLine();
			AppendSequenceRemark(sb);
			sb.Append("\t/// </remarks>").AppendLine();
			sb.Append("\tpublic static global::Mockolate.Setup.IReturnMethodSetupReturnBuilder<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types)
				.Append("> ThrowsAsync<TReturn, ").Append(types)
				.Append(">(this global::Mockolate.Setup.IReturnMethodSetupWithCallback<global::System.Threading.Tasks.ValueTask<TReturn>, ").Append(types).Append("> setup, global::System.Func<")
				.Append(types).Append(", global::System.Exception> callback)").AppendLine();
			sb.Append("\t\t=> setup.Returns((").Append(variables).Append(") => global::System.Threading.Tasks.ValueTask.FromException<TReturn>(callback(")
				.Append(variables).Append(")));").AppendLine();
			sb.AppendLine();
		}

		sb.AppendLine("#endif");
		sb.Append("}").AppendLine();

		sb.AppendLine("#nullable disable");
		return sb.ToString();
	}
}
