using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
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
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string iface = $"global::Mockolate.Setup.IRefStructVoidMethodSetup<{typeParams}>";
		string whereClauses = BuildAllowsRefStructWhereClauses(numberOfParameters);

		// Interface declaration.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append(
				"\t///     Ref-struct-compatible void setup for arity ")
			.Append(numberOfParameters).Append(". Mirrors the hand-written arity 1-4 surface.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface IRefStructVoidMethodSetup<").Append(typeParams).Append("> : IMethodSetup")
			.AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" DoesNotThrow();").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		// Concrete class.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible void setup for arity ").Append(numberOfParameters)
			.Append(". See <see cref=\"global::Mockolate.Setup.RefStructVoidMethodSetup{T}\" />.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class RefStructVoidMethodSetup<").Append(typeParams)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(iface).AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? _matcher")
				.Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\tprivate global::System.Func<global::System.Exception?>? _returnAction;").AppendLine();
		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// Constructor.
		sb.Append("\t\tpublic RefStructVoidMethodSetup(string name");
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
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
		}

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
			sb.Append(
					"\t\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\tpublic global::Mockolate.Parameters.IParameterMatch<T").Append(i)
				.Append(">? GetMatcher").Append(i).Append("() => _matcher").Append(i).Append(';').AppendLine();
		}

		sb.AppendLine();

		// Invoke.
		sb.Append("\t\tpublic void Invoke(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t_matcher").Append(i).Append("?.InvokeCallbacks(value").Append(i).Append(");").AppendLine();
		}

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

		// SkipBaseClass.
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

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".DoesNotThrow()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnAction = static () => null;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface).Append(".Throws<TException>()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnAction = static () => new TException();").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Exception exception)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnAction = () => exception;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t").Append(iface).Append(' ').Append(iface)
			.Append(".Throws(global::System.Func<global::System.Exception> exceptionFactory)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnAction = exceptionFactory;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// MatchesInteraction.
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"void {Name}(");
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

	private static void AppendRefStructReturnMethodSetup(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string ifaceGenerics = "TReturn, " + typeParams;
		string iface = $"global::Mockolate.Setup.IRefStructReturnMethodSetup<{ifaceGenerics}>";
		string whereClauses = BuildAllowsRefStructWhereClauses(numberOfParameters);

		// Interface declaration.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append(
				"\t///     Ref-struct-compatible return setup for arity ")
			.Append(numberOfParameters).Append(". Mirrors the hand-written arity 1-4 surface.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\tpublic interface IRefStructReturnMethodSetup<").Append(ifaceGenerics)
			.Append("> : IMethodSetup").AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(TReturn returnValue);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TReturn> returnFactory);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Func<global::System.Exception> exceptionFactory);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		// Concrete class.
		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     Concrete ref-struct-compatible return setup for arity ").Append(numberOfParameters)
			.Append(". See <see cref=\"global::Mockolate.Setup.RefStructReturnMethodSetup{TReturn, T}\" />.")
			.AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("#if !DEBUG").AppendLine();
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.Append("\tpublic sealed class RefStructReturnMethodSetup<").Append(ifaceGenerics)
			.Append("> : global::Mockolate.Setup.MethodSetup, ").Append(iface).AppendLine();
		sb.Append(whereClauses);
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\tprivate readonly global::Mockolate.Parameters.IParameterMatch<T").Append(i).Append(">? _matcher")
				.Append(i).Append(';').AppendLine();
		}

		sb.Append("\t\tprivate global::System.Func<TReturn>? _returnFactory;").AppendLine();
		sb.Append("\t\tprivate global::System.Func<global::System.Exception?>? _throwAction;").AppendLine();
		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// Constructor.
		sb.Append("\t\tpublic RefStructReturnMethodSetup(string name");
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
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
		}

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
			sb.Append(
					"\t\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\tpublic global::Mockolate.Parameters.IParameterMatch<T").Append(i)
				.Append(">? GetMatcher").Append(i).Append("() => _matcher").Append(i).Append(';').AppendLine();
		}

		sb.AppendLine();

		// Invoke.
		sb.Append("\t\tpublic TReturn Invoke(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
		}

		sb.Append(", global::System.Func<TReturn>? defaultFactory = null)").AppendLine();
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
		sb.Append("\t\t\tif (_returnFactory is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn _returnFactory();").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn defaultFactory is not null ? defaultFactory() : default!;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// HasReturnValue.
		sb.Append("\t\tpublic bool HasReturnValue => _returnFactory is not null;").AppendLine();
		sb.AppendLine();

		// SkipBaseClass.
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
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"{typeof(TReturn).Name} {Name}(");
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
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(TValue returnValue);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TValue> returnFactory);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
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
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
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
			Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} value{i}, "));
		string invokeValueArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"value{i}"));
		string rawKeyOptionalParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => $"object? rawKey{i} = null, "));
		string rawKeyParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => $"object? rawKey{i}, "));
		string rawKeyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"rawKey{i}"));

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
		sb.Append("\t\tpublic bool HasReturnValue => _returnFactory is not null || _storage is not null;").AppendLine();
		sb.AppendLine();

		// SkipBaseClass.
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
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
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
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" OnSet(global::System.Action<TValue> callback);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
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
		sb.Append("\t\tpublic bool Matches(");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			if (i > 1)
			{
				sb.Append(", ");
			}

			sb.Append("T").Append(i).Append(" value").Append(i);
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
			Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} k{i}, "));
		string keyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"k{i}"));
		string rawKeyOptionalParams = string.Concat(
			Enumerable.Range(1, numberOfParameters).Select(i => $"object? rawKey{i} = null, "));
		string rawKeyArgs = string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"rawKey{i}"));

		// Invoke.
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
		sb.Append(
				"\t\tprotected override bool MatchesInteraction(global::Mockolate.Interactions.IMethodInteraction interaction)")
			.AppendLine();
		sb.Append(
				"\t\t\t=> interaction is global::Mockolate.Interactions.RefStructMethodInvocation invocation && invocation.Name == Name;")
			.AppendLine();
		sb.AppendLine();

		// ToString.
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
		sb.Append("\t\t").Append(iface).Append(" SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(TValue returnValue);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Returns(global::System.Func<TValue> returnFactory);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" OnSet(global::System.Action<TValue> callback);").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws<TException>() where TException : global::System.Exception, new();")
			.AppendLine();
		sb.Append("\t\t").Append(iface).Append(" Throws(global::System.Exception exception);").AppendLine();
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

		sb.Append("\t\tpublic ").Append(concreteGetter).Append(" Getter { get; }").AppendLine();
		sb.Append("\t\tpublic ").Append(concreteSetter).Append(" Setter { get; }").AppendLine();
		sb.AppendLine();

		// Constructor.
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
