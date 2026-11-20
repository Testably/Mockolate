using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Mockolate.SourceGenerators.Tests.TestHelpers;

public static class ExternalAssembly
{
	/// <summary>
	///     Compiles the <paramref name="source" /> into an in-memory library named
	///     <paramref name="assemblyName" /> and returns a reference to it.
	/// </summary>
	public static MetadataReference Compile([StringSyntax("c#-test")] string source,
		string assemblyName = "ExternalAssembly", bool allowUnsafe = false)
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName,
			[CSharpSyntaxTree.ParseText(source, parseOptions),],
			Generator.GetReferences([]),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe));

		using MemoryStream stream = new();
		EmitResult emitResult = compilation.Emit(stream);
		if (!emitResult.Success)
		{
			throw new InvalidOperationException(
				$"Could not compile the external assembly '{assemblyName}':{Environment.NewLine}" +
				string.Join(Environment.NewLine, emitResult.Diagnostics
					.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.Select(diagnostic => diagnostic.ToString())));
		}

		stream.Position = 0;
		return MetadataReference.CreateFromStream(stream);
	}
}
