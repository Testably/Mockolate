using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	/// <summary>
	///     Emits the <c>IRefStructVoidMethodSetup&lt;T1..Tn&gt;</c> /
	///     <c>IRefStructReturnMethodSetup&lt;TReturn, T1..Tn&gt;</c> interfaces and their concrete
	///     implementations for arities beyond the hand-written ceiling (arity 5 and above).
	/// </summary>
	/// <remarks>
	///     <para>
	///         The hand-written types in <c>Source/Mockolate/Setup/IRefStructVoidMethodSetup.cs</c>,
	///         <c>IRefStructReturnMethodSetup.cs</c>, <c>RefStructVoidMethodSetup.cs</c>, and
	///         <c>RefStructReturnMethodSetup.cs</c> cover arities 1 through 4. This generator output
	///         mirrors the hand-written shape for arity 5+: the narrow setup surface (<c>Returns</c>,
	///         <c>Throws</c>, <c>DoesNotThrow</c>, <c>SkippingBaseClass</c>) with a single-slot
	///         last-call-wins throw configuration. No <c>Do(Action&lt;T&gt;)</c>, no
	///         <c>Callbacks&lt;T&gt;</c> sequencing — both are illegal for a ref-struct
	///         type parameter.
	///     </para>
	///     <para>
	///         Emission is guarded by <c>#if NET9_0_OR_GREATER</c> because
	///         <c>allows ref struct</c> is a C# 13 / net9.0+ feature.
	///     </para>
	/// </remarks>
	public static string RefStructMethodSetups(HashSet<(int, bool)> refStructMethodSetups,
		Dictionary<int, (bool HasGetter, bool HasSetter)> refStructIndexerArities)
	{
		StringBuilder sb = InitializeBuilder();

		sb.Append("""
		          #nullable enable
		          #if NET9_0_OR_GREATER

		          namespace Mockolate.Setup
		          {
		          """);
		foreach ((int, bool) item in refStructMethodSetups)
		{
			sb.AppendLine();
			if (item.Item2)
			{
				AppendRefStructVoidMethodSetup(sb, item.Item1);
			}
			else
			{
				AppendRefStructReturnMethodSetup(sb, item.Item1);
			}
		}

		foreach (KeyValuePair<int, (bool HasGetter, bool HasSetter)> kvp in refStructIndexerArities)
		{
			int arity = kvp.Key;
			(bool hasGetter, bool hasSetter) = kvp.Value;
			if (hasGetter)
			{
				sb.AppendLine();
				AppendRefStructIndexerGetterSetup(sb, arity);
			}

			if (hasSetter)
			{
				sb.AppendLine();
				AppendRefStructIndexerSetterSetup(sb, arity);
			}

			if (hasGetter && hasSetter)
			{
				sb.AppendLine();
				AppendRefStructIndexerSetup(sb, arity);
			}
		}

		sb.Append("""
		          }
		          #endif
		          """);
		return sb.ToString();
	}

	private static void AppendRefStructVoidMethodSetup(StringBuilder sb, int numberOfParameters)
	{
		const string className = "RefStructVoidMethodSetup";
		const string ifaceDoc = "global::Mockolate.Setup.IRefStructVoidMethodSetup{T}";
		const string classDoc = "global::Mockolate.Setup.RefStructVoidMethodSetup{T}";
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string iface = RefStructSetupInterface(className, typeParams);

		AppendRefStructMethodSetupInterfaceStart(sb, "void", className, typeParams, ifaceDoc, numberOfParameters);
		AppendRefStructInheritdoc(sb, ifaceDoc + ".DoesNotThrow()");
		sb.Append("\t\t").Append(iface).Append(" DoesNotThrow();").AppendLine();
		AppendRefStructMethodSetupInterfaceEnd(sb, className, typeParams, ifaceDoc);

		AppendRefStructMethodSetupClassStart(sb, "void", className, typeParams, classDoc, numberOfParameters,
			"private global::System.Func<global::System.Exception?>? _returnAction;");

		// Invoke.
		AppendRefStructInheritdoc(sb, classDoc + ".Invoke(T)");
		sb.Append("\t\tpublic void Invoke(");
		AppendRefStructValueParameters(sb, numberOfParameters);
		sb.Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		AppendRefStructInvokeCallbacks(sb, numberOfParameters);
		sb.Append("\t\t\tif (_returnAction is null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tglobal::System.Exception? exception = _returnAction();").AppendLine();
		sb.Append("\t\t\tif (exception is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tthrow exception;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		AppendRefStructSkipBaseClass(sb, iface, classDoc);

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".DoesNotThrow()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnAction = static () => null;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		AppendRefStructThrowsImplementations(sb, iface, "_returnAction");
		AppendRefStructMethodSetupClassEnd(sb, "void", numberOfParameters);
	}

	private static void AppendRefStructReturnMethodSetup(StringBuilder sb, int numberOfParameters)
	{
		const string className = "RefStructReturnMethodSetup";
		const string ifaceDoc = "global::Mockolate.Setup.IRefStructReturnMethodSetup{TReturn, T}";
		const string classDoc = "global::Mockolate.Setup.RefStructReturnMethodSetup{TReturn, T}";
		string ifaceGenerics = "TReturn, " + GetGenericTypeParameters(numberOfParameters);
		string iface = RefStructSetupInterface(className, ifaceGenerics);

		AppendRefStructMethodSetupInterfaceStart(sb, "return", className, ifaceGenerics, ifaceDoc, numberOfParameters);
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(TReturn)");
		sb.Append("\t\t").Append(iface).Append(" Returns(TReturn returnValue);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(global::System.Func{TReturn})");
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TReturn> returnFactory);").AppendLine();
		AppendRefStructMethodSetupInterfaceEnd(sb, className, ifaceGenerics, ifaceDoc);

		AppendRefStructMethodSetupClassStart(sb, "return", className, ifaceGenerics, classDoc, numberOfParameters,
			"private global::System.Func<TReturn>? _returnFactory;",
			"private global::System.Func<global::System.Exception?>? _throwAction;");

		// Invoke.
		AppendRefStructInheritdoc(sb, classDoc + ".Invoke(T, global::System.Func{TReturn})");
		sb.Append("\t\tpublic TReturn Invoke(");
		AppendRefStructValueParameters(sb, numberOfParameters);
		sb.Append(", global::System.Func<TReturn>? defaultFactory = null)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		AppendRefStructInvokeCallbacks(sb, numberOfParameters);
		sb.Append("\t\t\tif (_throwAction is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tglobal::System.Exception? exception = _throwAction();").AppendLine();
		sb.Append("\t\t\t\tif (exception is not null)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tthrow exception;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (_returnFactory is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn _returnFactory();").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn defaultFactory is not null ? defaultFactory() : default!;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// HasReturnValue.
		AppendRefStructInheritdoc(sb, classDoc + ".HasReturnValue");
		sb.Append("\t\tpublic bool HasReturnValue => _returnFactory is not null;").AppendLine();
		sb.AppendLine();

		AppendRefStructSkipBaseClass(sb, iface, classDoc);

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Returns(TReturn returnValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnFactory = () => returnValue;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Returns(global::System.Func<TReturn> returnFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnFactory = returnFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		AppendRefStructThrowsImplementations(sb, iface, "_throwAction");
		AppendRefStructMethodSetupClassEnd(sb, "{typeof(TReturn).Name}", numberOfParameters);
	}

	/// <summary>
	///     Emits the interface up to and including <c>SkippingBaseClass</c>; the caller adds the
	///     result-specific members before <see cref="AppendRefStructMethodSetupInterfaceEnd" />.
	/// </summary>
	private static void AppendRefStructMethodSetupInterfaceStart(StringBuilder sb, string kind, string className,
		string generics, string ifaceDoc, int numberOfParameters)
	{
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Ref-struct-compatible ").Append(kind).Append(" setup for arity ")
			.Append(numberOfParameters).Append(". Mirrors the hand-written arity 1-4 surface.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface I").Append(className).Append('<').Append(generics).Append("> : IMethodSetup")
			.AppendLine();
		sb.Append(BuildAllowsRefStructWhereClauses(numberOfParameters));
		sb.Append("\t{").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".SkippingBaseClass(bool)");
		sb.Append("\t\t").Append(RefStructSetupInterface(className, generics))
			.Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
	}

	private static void AppendRefStructMethodSetupInterfaceEnd(StringBuilder sb, string className, string generics,
		string ifaceDoc)
	{
		string iface = RefStructSetupInterface(className, generics);
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws{TException}()");
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Exception)");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Func{global::System.Exception})");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the class declaration, the fields, the constructor, <c>Matches</c> and the
	///     <c>GetMatcherN</c> accessors. <paramref name="fields" /> are emitted between the matcher
	///     fields and <c>_skipBaseClass</c>.
	/// </summary>
	private static void AppendRefStructMethodSetupClassStart(StringBuilder sb, string kind, string className,
		string generics, string classDoc, int numberOfParameters, params string[] fields)
	{
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible ").Append(kind).Append(" setup for arity ")
			.Append(numberOfParameters).Append(". See <see cref=\"").Append(classDoc).Append("\" />.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class ").Append(className).Append('<').Append(generics)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(RefStructSetupInterface(className, generics))
			.AppendLine();
		sb.Append(BuildAllowsRefStructWhereClauses(numberOfParameters));
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? _matcher")
				.Append(i).Append(';').AppendLine();
		}

		foreach (string field in fields)
		{
			sb.Append("\t\t").Append(field).AppendLine();
		}

		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// Constructor.
		AppendRefStructInheritdoc(sb, $"global::Mockolate.Setup.{className}{{{generics}}}");
		sb.Append("\t\tpublic ").Append(className).Append("(string name");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? matcher").Append(i)
				.Append(" = null");
		}

		sb.Append(") : base(name)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append(" = matcher").Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// Matches.
		AppendRefStructInheritdoc(sb, classDoc + ".Matches(T)");
		sb.Append("\t\tpublic bool Matches(");
		AppendRefStructValueParameters(sb, numberOfParameters);
		sb.Append(')').AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(i == 1 ? "\t\t\t=> " : "\t\t\t   && ");
			sb.Append("(_matcher").Append(i).Append(" is null || _matcher").Append(i).Append(".Matches(value")
				.Append(i).Append("))");
			sb.AppendLine();
		}

		// Strip the trailing newline appended by the loop so we can close with `;`. The
		// StringBuilder uses `\r\n` via AppendLine on Windows and `\n` elsewhere; fall back to
		// whichever terminator is present at the end.
		while (sb.Length > 0 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r'))
		{
			sb.Length--;
		}

		sb.Append(';').AppendLine();
		sb.AppendLine();

		// Per-slot matcher accessors. Used by generated mock bodies to access
		// IOutRefStructParameter<T>/IRefRefStructParameter<T> payloads on out/ref slots.
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.AppendXmlSummary(FormattableString.Invariant($"Returns the matcher for the parameter slot {i}."));
			sb.Append(
					"\t\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\tpublic global::Mockolate.Parameters.IParameterMatch<T").Append(i)
				.Append(">? GetMatcher").Append(i).Append("() => _matcher").Append(i).Append(';').AppendLine();
		}

		sb.AppendLine();
	}

	private static string RefStructSetupInterface(string className, string generics)
		=> $"global::Mockolate.Setup.I{className}<{generics}>";

	private static void AppendRefStructValueParameters(StringBuilder sb, int numberOfParameters)
	{
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append('T').Append(i).Append(" value").Append(i);
		}
	}

	private static void AppendRefStructInvokeCallbacks(StringBuilder sb, int numberOfParameters)
	{
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append("?.InvokeCallbacks(value").Append(i).Append(");").AppendLine();
		}
	}

	/// <summary>
	///     Emits <c>SkipBaseClass(MockBehavior)</c> and the explicit <c>SkippingBaseClass</c> implementation.
	/// </summary>
	private static void AppendRefStructSkipBaseClass(StringBuilder sb, string iface, string classDoc)
	{
		AppendRefStructInheritdoc(sb, classDoc + ".SkipBaseClass(global::Mockolate.MockBehavior)");
		sb.Append("\t\tpublic bool SkipBaseClass(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t\t\t=> _skipBaseClass ?? behavior.SkipBaseClass;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".SkippingBaseClass(bool skipBaseClass)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_skipBaseClass = skipBaseClass;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the explicit <c>Throws</c> implementations, which store the exception factory in <paramref name="field" />.
	/// </summary>
	private static void AppendRefStructThrowsImplementations(StringBuilder sb, string iface, string field)
	{
		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Throws<TException>()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t").Append(field).Append(" = static () => new TException();").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Exception exception)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t").Append(field).Append(" = () => exception;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Func<global::System.Exception> exceptionFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t").Append(field).Append(" = exceptionFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits <c>MatchesInteraction</c>, <c>ToString</c> (prefixed with <paramref name="returnType" />) and closes the class.
	/// </summary>
	private static void AppendRefStructMethodSetupClassEnd(StringBuilder sb, string returnType, int numberOfParameters)
	{
		// MatchesInteraction.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.MethodSetup.MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction)");
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
		AppendRefStructInheritdoc(sb, "object.ToString()");
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"").Append(returnType).Append(" {Name}(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("{_matcher").Append(i).Append("?.ToString() ?? $\"<{typeof(T").Append(i)
				.Append(").Name} ref struct>\"}");
		}

		sb.Append(")\";").AppendLine();

		sb.Append("\t}").AppendLine();
	}

	private static void AppendRefStructIndexerGetterSetup(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string ifaceGenerics = "TValue, " + typeParams;
		string iface = $"global::Mockolate.Setup.IRefStructIndexerGetterSetup<{ifaceGenerics}>";
		const string ifaceDoc = "global::Mockolate.Setup.IRefStructIndexerGetterSetup{TValue, T}";
		string whereClauses = BuildAllowsRefStructWhereClauses(numberOfParameters);

		// Interface declaration.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Ref-struct-compatible indexer getter setup for arity ").Append(numberOfParameters)
			.Append(". Mirrors the hand-written arity 1-4 surface.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface IRefStructIndexerGetterSetup<").Append(ifaceGenerics)
			.Append("> : IMethodSetup").AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".SkippingBaseClass(bool)");
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(TValue)");
		sb.Append("\t\t").Append(iface).Append(" Returns(TValue returnValue);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(global::System.Func{TValue})");
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TValue> returnFactory);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws{TException}()");
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Exception)");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Func{global::System.Exception})");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		// Concrete class.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible indexer getter setup for arity ")
			.Append(numberOfParameters)
			.Append(". See <see cref=\"global::Mockolate.Setup.RefStructIndexerGetterSetup{TValue, T}\" />.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class RefStructIndexerGetterSetup<").Append(ifaceGenerics)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(iface).AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? _matcher")
				.Append(i).Append(';').AppendLine();
		}

		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IRefStructProjectionMatch<T").Append(i)
				.Append(">? _projection").Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\t// The concrete leaf is an internal generic — the generator emits against the public base.").AppendLine();
		sb.Append("\t\tprivate readonly global::Mockolate.Internals.IndexerValueStorage? _storage;").AppendLine();
		sb.Append("\t\tprivate global::System.Func<TValue>? _returnFactory;").AppendLine();
		sb.Append("\t\tprivate global::System.Func<global::System.Exception?>? _throwAction;").AppendLine();
		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// Constructor.
		AppendRefStructInheritdoc(sb, $"global::Mockolate.Setup.RefStructIndexerGetterSetup{{{ifaceGenerics}}}");
		sb.Append("\t\tpublic RefStructIndexerGetterSetup(string name");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? matcher").Append(i)
				.Append(" = null");
		}

		sb.Append(") : base(name)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append(" = matcher").Append(i).Append(';').AppendLine();
		}

		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_projection").Append(i).Append(" = matcher").Append(i)
				.Append(" as global::Mockolate.Parameters.IRefStructProjectionMatch<T").Append(i).Append(">;")
				.AppendLine();
		}

		// Storage activates iff every ref-struct slot carries a projection. Non-ref-struct slots
		// contribute their raw value (boxed by the caller) as the dispatch key.
		sb.Append("\t\t\tif (");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(" && ");
			}

			sb.Append("global::Mockolate.Internals.RefStructStorageHelper.IsSlotStorageReady(_projection").Append(i)
				.Append(')');
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t_storage = global::Mockolate.Internals.RefStructStorageHelper.CreateStorage<TValue>();").AppendLine();
		sb.Append("\t\t\t}").AppendLine();

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// Matches.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerGetterSetup{TValue, T}.Matches(T)");
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append('T').Append(i).Append(" value").Append(i);
		}

		sb.Append(')').AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(i == 1 ? "\t\t\t=> " : "\t\t\t   && ");
			sb.Append("(_matcher").Append(i).Append(" is null || _matcher").Append(i).Append(".Matches(value")
				.Append(i).Append("))");
			sb.AppendLine();
		}

		while (sb.Length > 0 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r'))
		{
			sb.Length--;
		}

		sb.Append(';').AppendLine();
		sb.AppendLine();

		// Helpers used by Invoke + StoreValue to build the ref-struct indexer parameter lists.
		string invokeValueParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"T{i} value{i}, ")));
		string invokeValueArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"value{i}")));
		string rawKeyOptionalParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"object? rawKey{i} = null, ")));
		string rawKeyParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"object? rawKey{i}, ")));
		string rawKeyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"rawKey{i}")));

		// TryGetStoredValue — traverses the tree using per-slot resolved keys and returns the
		// leaf value when present.
		sb.Append("\t\tinternal bool TryGetStoredValue(").Append(invokeValueParams).Append(rawKeyParams)
			.Append("out TValue value)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_storage is null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvalue = default!;").AppendLine();
		sb.Append("\t\t\t\treturn false;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\tif (!global::Mockolate.Internals.RefStructStorageHelper.TryResolveKey(_projection").Append(i)
				.Append(", value").Append(i).Append(", rawKey").Append(i).Append(", out object key").Append(i)
				.Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvalue = default!;").AppendLine();
			sb.Append("\t\t\t\treturn false;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t\tglobal::Mockolate.Internals.IndexerValueStorage? node = _storage");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(i == 1 ? "" : "?").Append(".GetChildDispatch(key").Append(i).Append(')');
		}

		sb.Append(';').AppendLine();
		sb.Append("\t\t\treturn global::Mockolate.Internals.RefStructStorageHelper.TryGetLeafValue<TValue>(node, out value);").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// StoreValue — writes value to the leaf indexed by the per-slot resolved keys.
		sb.Append("\t\tinternal void StoreValue(").Append(invokeValueParams).Append("TValue value, ")
			.Append(rawKeyParams.TrimEnd(' ', ',')).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_storage is null || !Matches(").Append(invokeValueArgs).Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\tif (!global::Mockolate.Internals.RefStructStorageHelper.TryResolveKey(_projection").Append(i)
				.Append(", value").Append(i).Append(", rawKey").Append(i).Append(", out object key").Append(i)
				.Append("))").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\treturn;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t\tglobal::Mockolate.Internals.IndexerValueStorage leaf = _storage");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(".GetOrAddChildDispatch(key").Append(i).Append(')');
		}

		sb.Append(';').AppendLine();
		sb.Append("\t\t\tglobal::Mockolate.Internals.RefStructStorageHelper.SetLeafValue<TValue>(leaf, value);").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// Invoke — executes matcher callbacks, applies throw, reads storage (if active), then
		// falls through to the return factory or default.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerGetterSetup{TValue, T}.Invoke(T, global::System.Func{TValue})");
		sb.Append("\t\tpublic TValue Invoke(").Append(invokeValueParams).Append(rawKeyOptionalParams)
			.Append("global::System.Func<TValue>? defaultFactory = null)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append("?.InvokeCallbacks(value").Append(i).Append(");").AppendLine();
		}

		sb.Append("\t\t\tif (_throwAction is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tglobal::System.Exception? exception = _throwAction();").AppendLine();
		sb.Append("\t\t\t\tif (exception is not null)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tthrow exception;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (_storage is not null && TryGetStoredValue(").Append(invokeValueArgs).Append(", ")
			.Append(rawKeyArgs).Append(", out TValue stored))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn stored;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (_returnFactory is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn _returnFactory();").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn defaultFactory is not null ? defaultFactory() : default!;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// HasReturnValue — mirrors the arity 1-4 rule: active storage flips this true so the
		// mock class's generated indexer body takes the return path.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerGetterSetup{TValue, T}.HasReturnValue");
		sb.Append("\t\tpublic bool HasReturnValue => _returnFactory is not null || _storage is not null;").AppendLine();
		sb.AppendLine();

		// SkipBaseClass.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerGetterSetup{TValue, T}.SkipBaseClass(global::Mockolate.MockBehavior)");
		sb.Append("\t\tpublic bool SkipBaseClass(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t\t\t=> _skipBaseClass ?? behavior.SkipBaseClass;").AppendLine();
		sb.AppendLine();

		// Interface methods.
		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".SkippingBaseClass(bool skipBaseClass)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_skipBaseClass = skipBaseClass;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Returns(TValue returnValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnFactory = () => returnValue;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Returns(global::System.Func<TValue> returnFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnFactory = returnFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Throws<TException>()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = static () => new TException();").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Exception exception)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = () => exception;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Func<global::System.Exception> exceptionFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = exceptionFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// MatchesInteraction.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.MethodSetup.MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction)");
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
		AppendRefStructInheritdoc(sb, "object.ToString()");
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"{typeof(TValue).Name} {Name}[");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("{_matcher").Append(i).Append("?.ToString() ?? $\"<{typeof(T").Append(i)
				.Append(").Name} ref struct>\"}");
		}

		sb.Append("]\";").AppendLine();

		sb.Append("\t}").AppendLine();
	}

	private static void AppendRefStructIndexerSetterSetup(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string ifaceGenerics = "TValue, " + typeParams;
		string iface = $"global::Mockolate.Setup.IRefStructIndexerSetterSetup<{ifaceGenerics}>";
		const string ifaceDoc = "global::Mockolate.Setup.IRefStructIndexerSetterSetup{TValue, T}";
		string concreteGetter = $"global::Mockolate.Setup.RefStructIndexerGetterSetup<{ifaceGenerics}>";
		string whereClauses = BuildAllowsRefStructWhereClauses(numberOfParameters);

		// Interface declaration.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Ref-struct-compatible indexer setter setup for arity ").Append(numberOfParameters)
			.Append(". Mirrors the hand-written arity 1-4 surface.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface IRefStructIndexerSetterSetup<").Append(ifaceGenerics)
			.Append("> : IMethodSetup").AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".SkippingBaseClass(bool)");
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".OnSet(global::System.Action{TValue})");
		sb.Append("\t\t").Append(iface).Append(" OnSet(global::System.Action<TValue> callback);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws{TException}()");
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Exception)");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Func{global::System.Exception})");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		// Concrete class.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible indexer setter setup for arity ").Append(numberOfParameters)
			.Append(". See <see cref=\"global::Mockolate.Setup.RefStructIndexerSetterSetup{TValue, T}\" />.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class RefStructIndexerSetterSetup<").Append(ifaceGenerics)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(iface).AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? _matcher")
				.Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\tprivate global::System.Action<TValue>? _onSet;").AppendLine();
		sb.Append("\t\tprivate global::System.Func<global::System.Exception?>? _throwAction;").AppendLine();
		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// Constructor.
		AppendRefStructInheritdoc(sb, $"global::Mockolate.Setup.RefStructIndexerSetterSetup{{{ifaceGenerics}}}");
		sb.Append("\t\tpublic RefStructIndexerSetterSetup(string name");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? matcher").Append(i)
				.Append(" = null");
		}

		sb.Append(") : base(name)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append(" = matcher").Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// BoundGetter — set by the combined setup facade so setter writes forward into the
		// getter's projection-keyed storage.
		sb.Append("\t\tinternal ").Append(concreteGetter).Append("? BoundGetter { get; set; }").AppendLine();
		sb.AppendLine();

		// Matches.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerSetterSetup{TValue, T}.Matches(T)");
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append('T').Append(i).Append(" value").Append(i);
		}

		sb.Append(')').AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(i == 1 ? "\t\t\t=> " : "\t\t\t   && ");
			sb.Append("(_matcher").Append(i).Append(" is null || _matcher").Append(i).Append(".Matches(value")
				.Append(i).Append("))");
			sb.AppendLine();
		}

		while (sb.Length > 0 && (sb[sb.Length - 1] == '\n' || sb[sb.Length - 1] == '\r'))
		{
			sb.Length--;
		}

		sb.Append(';').AppendLine();
		sb.AppendLine();

		string keyParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"T{i} k{i}, ")));
		string keyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"k{i}")));
		string rawKeyOptionalParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"object? rawKey{i} = null, ")));
		string rawKeyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => FormattableString.Invariant($"rawKey{i}")));

		// Invoke.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerSetterSetup{TValue, T}.Invoke(T, TValue)");
		sb.Append("\t\tpublic void Invoke(").Append(keyParams).Append("TValue value, ")
			.Append(rawKeyOptionalParams.TrimEnd(' ', ',')).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append("?.InvokeCallbacks(k").Append(i).Append(");").AppendLine();
		}

		sb.Append("\t\t\t_onSet?.Invoke(value);").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (_throwAction is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tglobal::System.Exception? exception = _throwAction();").AppendLine();
		sb.Append("\t\t\t\tif (exception is not null)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tthrow exception;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tBoundGetter?.StoreValue(").Append(keyArgs).Append(", value, ").Append(rawKeyArgs)
			.Append(");").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// SkipBaseClass.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerSetterSetup{TValue, T}.SkipBaseClass(global::Mockolate.MockBehavior)");
		sb.Append("\t\tpublic bool SkipBaseClass(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t\t\t=> _skipBaseClass ?? behavior.SkipBaseClass;").AppendLine();
		sb.AppendLine();

		// Interface methods.
		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".SkippingBaseClass(bool skipBaseClass)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_skipBaseClass = skipBaseClass;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".OnSet(global::System.Action<TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_onSet = callback;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Throws<TException>()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = static () => new TException();").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Exception exception)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = () => exception;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Func<global::System.Exception> exceptionFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_throwAction = exceptionFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// MatchesInteraction.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.MethodSetup.MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction)");
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
		AppendRefStructInheritdoc(sb, "object.ToString()");
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"{Name}(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("{_matcher").Append(i).Append("?.ToString() ?? $\"<{typeof(T").Append(i)
				.Append(").Name} ref struct>\"}");
		}

		sb.Append(") = {typeof(TValue).Name}\";").AppendLine();

		sb.Append("\t}").AppendLine();
	}

	private static void AppendRefStructIndexerSetup(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string ifaceGenerics = "TValue, " + typeParams;
		string iface = $"global::Mockolate.Setup.IRefStructIndexerSetup<{ifaceGenerics}>";
		const string ifaceDoc = "global::Mockolate.Setup.IRefStructIndexerSetup{TValue, T}";
		string getterIface = $"global::Mockolate.Setup.IRefStructIndexerGetterSetup<{ifaceGenerics}>";
		string setterIface = $"global::Mockolate.Setup.IRefStructIndexerSetterSetup<{ifaceGenerics}>";
		string concreteGetter = $"global::Mockolate.Setup.RefStructIndexerGetterSetup<{ifaceGenerics}>";
		string concreteSetter = $"global::Mockolate.Setup.RefStructIndexerSetterSetup<{ifaceGenerics}>";
		string whereClauses = BuildAllowsRefStructWhereClauses(numberOfParameters);

		// Interface declaration.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Ref-struct-compatible combined indexer setup for arity ").Append(numberOfParameters)
			.Append(". Mirrors the hand-written arity 1-4 surface.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface IRefStructIndexerSetup<").Append(ifaceGenerics).Append("> : IMethodSetup")
			.AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".SkippingBaseClass(bool)");
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(TValue)");
		sb.Append("\t\t").Append(iface).Append(" Returns(TValue returnValue);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Returns(global::System.Func{TValue})");
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TValue> returnFactory);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".OnSet(global::System.Action{TValue})");
		sb.Append("\t\t").Append(iface).Append(" OnSet(global::System.Action<TValue> callback);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws{TException}()");
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Exception)");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		AppendRefStructInheritdoc(sb, ifaceDoc + ".Throws(global::System.Func{global::System.Exception})");
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		// Concrete class.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible combined indexer setup for arity ").Append(numberOfParameters)
			.Append(". See <see cref=\"global::Mockolate.Setup.RefStructIndexerSetup{TValue, T}\" />.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class RefStructIndexerSetup<").Append(ifaceGenerics)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(iface).AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();

		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerSetup{TValue, T}.Getter");
		sb.Append("\t\tpublic ").Append(concreteGetter).Append(" Getter { get; }").AppendLine();
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.RefStructIndexerSetup{TValue, T}.Setter");
		sb.Append("\t\tpublic ").Append(concreteSetter).Append(" Setter { get; }").AppendLine();
		sb.AppendLine();

		// Constructor.
		AppendRefStructInheritdoc(sb, $"global::Mockolate.Setup.RefStructIndexerSetup{{{ifaceGenerics}}}");
		sb.Append("\t\tpublic RefStructIndexerSetup(string getterName, string setterName");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? matcher").Append(i)
				.Append(" = null");
		}

		sb.Append(") : base(getterName)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tGetter = new ").Append(concreteGetter).Append("(getterName");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", matcher").Append(i);
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\tSetter = new ").Append(concreteSetter).Append("(setterName");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", matcher").Append(i);
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\tSetter.BoundGetter = Getter;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// MatchesInteraction — combined setup is not registered directly.
		AppendRefStructInheritdoc(sb, "global::Mockolate.Setup.MethodSetup.MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction)");
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction) => false;")
			.AppendLine();
		sb.AppendLine();

		// Interface methods — delegate to inner setups.
		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".SkippingBaseClass(bool skipBaseClass)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).SkippingBaseClass(skipBaseClass);").AppendLine();
		sb.Append("\t\t\t((").Append(setterIface).Append(")Setter).SkippingBaseClass(skipBaseClass);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Returns(TValue returnValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).Returns(returnValue);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Returns(global::System.Func<TValue> returnFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).Returns(returnFactory);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".OnSet(global::System.Action<TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(setterIface).Append(")Setter).OnSet(callback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Throws<TException>()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).Throws<TException>();").AppendLine();
		sb.Append("\t\t\t((").Append(setterIface).Append(")Setter).Throws<TException>();").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Exception exception)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).Throws(exception);").AppendLine();
		sb.Append("\t\t\t((").Append(setterIface).Append(")Setter).Throws(exception);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Func<global::System.Exception> exceptionFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t((").Append(getterIface).Append(")Getter).Throws(exceptionFactory);").AppendLine();
		sb.Append("\t\t\t((").Append(setterIface).Append(")Setter).Throws(exceptionFactory);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();

		sb.Append("\t}").AppendLine();
	}

	/// <summary>
	///     Appends an <c>&lt;inheritdoc&gt;</c> that points a generated member at its hand-written arity 1
	///     counterpart, like the hand-written arities 2-4 do.
	/// </summary>
	private static void AppendRefStructInheritdoc(StringBuilder sb, string cref)
		=> sb.Append("\t\t/// <inheritdoc cref=\"").Append(cref).Append("\" />").AppendLine();

	private static string BuildAllowsRefStructWhereClauses(int numberOfParameters)
	{
		StringBuilder sb = new();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\twhere T").Append(i).Append(" : allows ref struct").AppendLine();
		}

		return sb.ToString();
	}
}
