using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Internals;

/// <summary>
///     Keeps the discovered auto-mocks of a project from one compilation to the next, so that an edit that cannot
///     change them, e.g. typing in a test method, does not discover and build them again.
/// </summary>
/// <remarks>
///     The discovery binds symbols of the current compilation, so the incremental pipeline cannot cache it. The result
///     is reused only when the settings, the mocked types, the compilation options apart from their file resolvers and
///     signing services, and the references are the same,
///     no edited, added or removed file declares a type whose name is one of the <see cref="AutoMockDependencies" />
///     or changes a global using or an assembly or module attribute, and no namespace whose last name is one of them
///     starts or stops to exist. The files and
///     references are only held weakly, and the models hold no symbols, so the cache keeps no compilation alive. It
///     keeps one entry per assembly name and set of preprocessor symbols, so that the target frameworks of a project
///     do not replace each other.
/// </remarks>
internal static class AutoMockDiscoveryCache
{
	private static readonly ConcurrentDictionary<string, State> States = new(StringComparer.Ordinal);

	/// <summary>
	///     Returns the auto-mocks of the previous compilation of the project when the edits cannot change them, or
	///     discovers them with <paramref name="discover" />.
	/// </summary>
	/// <param name="compilation">The current compilation.</param>
	/// <param name="settings">The settings and mocked types that the result depends on.</param>
	/// <param name="discover">Discovers the auto-mocks and adds their dependencies to the given collector.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static EquatableArray<MockClass> GetOrDiscover(Compilation compilation, string settings,
		Func<AutoMockDependencies, EquatableArray<MockClass>> discover, CancellationToken cancellationToken)
	{
		string key = (compilation.AssemblyName ?? "") + "\n" +
		             string.Join(";", compilation.SyntaxTrees.FirstOrDefault()?.Options.PreprocessorSymbolNames ?? []);
		if (States.TryGetValue(key, out State? state) &&
		    state.TryReuse(compilation, settings, cancellationToken) is { } reused)
		{
			States[key] = reused;
			return reused.Result;
		}

		AutoMockDependencies dependencies = new();
		EquatableArray<MockClass> result = discover(dependencies);
		if (State.Create(compilation, settings, dependencies.Names, result, state, cancellationToken) is { } created)
		{
			States[key] = created;
		}
		else
		{
			States.TryRemove(key, out _);
		}

		return result;
	}

	private sealed class State(
		string settings,
		CompilationOptions options,
		WeakReference<MetadataReference>[] references,
		Dictionary<string, FileState> files,
		Dictionary<string, int> namespaceCounts,
		HashSet<string> dependencies,
		EquatableArray<MockClass> result)
	{
		public EquatableArray<MockClass> Result { get; } = result;

		private Dictionary<string, FileState> Files { get; } = files;

		/// <summary>
		///     The number of files that declare each namespace.
		/// </summary>
		private Dictionary<string, int> NamespaceCounts { get; } = namespaceCounts;

		/// <summary>
		///     Returns <see langword="null" /> when two files share a path, so that the edited file cannot be found.
		/// </summary>
		/// <remarks>
		///     Only the files that changed since the <paramref name="previous" /> state are read again.
		/// </remarks>
		public static State? Create(Compilation compilation, string settings, HashSet<string> dependencies,
			EquatableArray<MockClass> result, State? previous, CancellationToken cancellationToken)
		{
			Dictionary<string, FileState> files = new(StringComparer.Ordinal);
			Dictionary<string, int> namespaceCounts = new(StringComparer.Ordinal);
			foreach (SyntaxTree tree in compilation.SyntaxTrees)
			{
				if (files.ContainsKey(tree.FilePath))
				{
					return null;
				}

				FileState file = previous is not null &&
				                 previous.Files.TryGetValue(tree.FilePath, out FileState? unchanged) &&
				                 unchanged.Is(tree)
					? unchanged
					: FileState.Read(tree, cancellationToken);
				files[tree.FilePath] = file;
				Count(namespaceCounts, null, file.Namespaces, 1);
			}

			return new State(settings, WithoutServices(compilation.Options),
				compilation.References.Select(reference => new WeakReference<MetadataReference>(reference)).ToArray(),
				files, namespaceCounts, dependencies, result);
		}

		/// <summary>
		///     Returns the state for the <paramref name="compilation" /> with the same result, or
		///     <see langword="null" /> when the edits since the previous compilation may change the result.
		/// </summary>
		public State? TryReuse(Compilation compilation, string currentSettings, CancellationToken cancellationToken)
		{
			if (!string.Equals(currentSettings, settings, StringComparison.Ordinal) ||!WithoutServices(compilation.Options).Equals(options) ||
			    !HasSameReferences(compilation))
			{
				return null;
			}

			Dictionary<string, FileState> currentFiles = new(StringComparer.Ordinal);
			Dictionary<string, int> currentNamespaceCounts = new(NamespaceCounts, StringComparer.Ordinal);
			HashSet<string> changedNamespaces = new(StringComparer.Ordinal);
			foreach (SyntaxTree tree in compilation.SyntaxTrees)
			{
				if (currentFiles.ContainsKey(tree.FilePath))
				{
					return null;
				}

				if (Files.TryGetValue(tree.FilePath, out FileState? previous) && previous.Is(tree))
				{
					currentFiles[tree.FilePath] = previous;
					continue;
				}

				FileState current = FileState.Read(tree, cancellationToken);
				if (current.DeclaresAny(dependencies) || previous?.DeclaresAny(dependencies) == true ||
				    !string.Equals(current.Fingerprint, previous?.Fingerprint ?? "", StringComparison.Ordinal))
				{
					return null;
				}

				Count(currentNamespaceCounts, changedNamespaces, previous?.Namespaces, -1);
				Count(currentNamespaceCounts, changedNamespaces, current.Namespaces, 1);
				currentFiles[tree.FilePath] = current;
			}

			foreach (KeyValuePair<string, FileState> removed in Files)
			{
				if (currentFiles.ContainsKey(removed.Key))
				{
					continue;
				}

				if (removed.Value.DeclaresAny(dependencies) || removed.Value.Fingerprint.Length > 0)
				{
					return null;
				}

				Count(currentNamespaceCounts, changedNamespaces, removed.Value.Namespaces, -1);
			}

			// A namespace can only change the binding of a name when it starts or stops to exist.
			foreach (string @namespace in changedNamespaces)
			{
				bool existed = NamespaceCounts.TryGetValue(@namespace, out int before) && before > 0;
				bool exists = currentNamespaceCounts.TryGetValue(@namespace, out int after) && after > 0;
				if (existed != exists && dependencies.Contains(@namespace.Substring(@namespace.LastIndexOf('.') + 1)))
				{
					return null;
				}
			}

			return new State(settings, options, references, currentFiles, currentNamespaceCounts, dependencies,
				Result);
		}

		/// <summary>
		///     The <paramref name="options" /> without the services that cannot change the discovery: the command-line
		///     compiler creates new file resolvers for every compilation, whose <see cref="object.Equals(object)" /> throws.
		/// </summary>
		private static CompilationOptions WithoutServices(CompilationOptions options)
			=> options
				.WithMetadataReferenceResolver(null)
				.WithSourceReferenceResolver(null)
				.WithXmlReferenceResolver(null)
				.WithStrongNameProvider(null)
				.WithSyntaxTreeOptionsProvider(null);

		private static void Count(Dictionary<string, int> counts, HashSet<string>? changed,
			string[]? namespaces, int delta)
		{
			foreach (string @namespace in namespaces ?? [])
			{
				counts[@namespace] = (counts.TryGetValue(@namespace, out int count) ? count : 0) + delta;
				changed?.Add(@namespace);
			}
		}

		private bool HasSameReferences(Compilation compilation)
		{
			if (compilation.References.Take(references.Length + 1).Count() != references.Length)
			{
				return false;
			}

			int index = 0;
			foreach (MetadataReference reference in compilation.References)
			{
				if (!references[index++].TryGetTarget(out MetadataReference? previous) ||
				    !ReferenceEquals(previous, reference))
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	///     The types and namespaces that a file declares and its text that applies to other files.
	/// </summary>
	private sealed class FileState(
		WeakReference<SyntaxTree> tree,
		string[] typeNames,
		string[] namespaces,
		string fingerprint)
	{
		/// <summary>
		///     The global usings and the assembly and module attributes of the file.
		/// </summary>
		public string Fingerprint { get; } = fingerprint;

		/// <summary>
		///     The full names of the namespaces that the file declares, including the namespaces that contain them.
		/// </summary>
		public string[] Namespaces { get; } = namespaces;

		public bool Is(SyntaxTree other)
			=> tree.TryGetTarget(out SyntaxTree? target) && ReferenceEquals(target, other);

		public bool DeclaresAny(HashSet<string> dependencies)
			=> typeNames.Any(dependencies.Contains);

		public static FileState Read(SyntaxTree tree, CancellationToken cancellationToken)
		{
			List<string> declaredNames = [];
			HashSet<string> namespaces = new(StringComparer.Ordinal);
			List<string> fingerprint = [];
			foreach (SyntaxNode node in tree.GetRoot(cancellationToken).DescendantNodes(node =>
				         node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax))
			{
				switch (node)
				{
					case BaseTypeDeclarationSyntax type:
						declaredNames.Add(type.Identifier.ValueText);
						break;
					case DelegateDeclarationSyntax @delegate:
						declaredNames.Add(@delegate.Identifier.ValueText);
						break;
					case BaseNamespaceDeclarationSyntax @namespace:
						string fullName = "";
						foreach (SimpleNameSyntax segment in @namespace.AncestorsAndSelf()
							         .OfType<BaseNamespaceDeclarationSyntax>()
							         .Reverse()
							         .SelectMany(declaration => declaration.Name.DescendantNodesAndSelf()
								         .OfType<SimpleNameSyntax>()))
						{
							fullName = fullName.Length == 0
								? segment.Identifier.ValueText
								: fullName + "." + segment.Identifier.ValueText;
							namespaces.Add(fullName);
						}

						break;
					case UsingDirectiveSyntax { GlobalKeyword.RawKind: not 0, } globalUsing:
						fingerprint.Add(globalUsing.ToString());
						break;
					case AttributeListSyntax { Parent: CompilationUnitSyntax, } attributes:
						fingerprint.Add(attributes.ToString());
						break;
				}
			}

			return new FileState(new WeakReference<SyntaxTree>(tree), declaredNames.ToArray(), namespaces.ToArray(),
				string.Join("\n", fingerprint));
		}
	}
}
