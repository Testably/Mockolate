using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	public static string IndexerSetups(
		IEnumerable<IndexerSetupKey> indexerSetups,
		bool hasOverloadResolutionPriority)
	{
		StringBuilder sb = InitializeBuilder();

		sb.Append("""
		          #nullable enable

		          namespace Mockolate.Setup
		          {
		          """);
		foreach (IndexerSetupKey item in indexerSetups)
		{
			sb.AppendLine();
			AppendIndexerSetup(sb, item.Arity, item.NeedsGetterOnly, item.NeedsSetterOnly);
			if (item.NeedsGetterOnly)
			{
				AppendGetterOnlyIndexerInterfaces(sb, item.Arity);
			}

			if (item.NeedsSetterOnly)
			{
				AppendSetterOnlyIndexerInterfaces(sb, item.Arity);
			}
		}

		sb.AppendLine();
		sb.Append("""
		          }

		          namespace Mockolate
		          {
		          """);
#if !DEBUG
		sb.Append("[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.AppendLine("""
		              	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		              	internal static class IndexerSetupExtensions
		              	{
		              """).AppendLine();
		foreach (IndexerSetupKey setup in indexerSetups)
		{
			int item = setup.Arity;
			string types = GetGenericTypeParameters(item);
			sb.Append($$"""
			            		/// <summary>
			            		///     Extensions for indexer getter callback setups with {{item}} parameters.
			            		/// </summary>
			            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerGetterSetupCallbackWhenBuilder<TValue, {{types}}> setup)
			            		{
			            			/// <summary>
			            			///     Executes the callback only once.
			            			/// </summary>
			            			public global::Mockolate.Setup.IIndexerSetup<TValue, {{types}}> OnlyOnce()
			            				=> setup.Only(1);
			            		}

			            		/// <summary>
			            		///     Extensions for indexer setter callback setups with {{item}} parameters.
			            		/// </summary>
			            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerSetterSetupCallbackWhenBuilder<TValue, {{types}}> setup)
			            		{
			            			/// <summary>
			            			///     Executes the callback only once.
			            			/// </summary>
			            			public global::Mockolate.Setup.IIndexerSetup<TValue, {{types}}> OnlyOnce()
			            				=> setup.Only(1);
			            		}

			            		/// <summary>
			            		///     Extensions for indexer setups with {{item}} parameters.
			            		/// </summary>
			            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerSetupReturnWhenBuilder<TValue, {{types}}> setup)
			            		{
			            			/// <summary>
			            			///     Returns/throws forever.
			            			/// </summary>
			            			public void Forever()
			            			{
			            				setup.For(int.MaxValue);
			            			}

			            			/// <summary>
			            			///     Uses the return value only once.
			            			/// </summary>
			            			public global::Mockolate.Setup.IIndexerSetup<TValue, {{types}}> OnlyOnce()
			            				=> setup.Only(1);
			            		}
			            """).AppendLine();

			if (setup.NeedsGetterOnly)
			{
				sb.AppendLine();
				sb.Append($$"""
				            		/// <summary>
				            		///     Extensions for setups of get-only indexers with {{item}} parameters.
				            		/// </summary>
				            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{types}}> setup)
				            		{
				            			/// <summary>
				            			///     Returns/throws forever.
				            			/// </summary>
				            			public void Forever()
				            			{
				            				setup.For(int.MaxValue);
				            			}

				            			/// <summary>
				            			///     Uses the return value only once.
				            			/// </summary>
				            			public global::Mockolate.Setup.IIndexerGetterOnlySetup<TValue, {{types}}> OnlyOnce()
				            				=> setup.Only(1);
				            		}

				            		/// <summary>
				            		///     Extensions for getter callback setups of get-only indexers with {{item}} parameters.
				            		/// </summary>
				            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{types}}> setup)
				            		{
				            			/// <summary>
				            			///     Executes the callback only once.
				            			/// </summary>
				            			public global::Mockolate.Setup.IIndexerGetterOnlySetup<TValue, {{types}}> OnlyOnce()
				            				=> setup.Only(1);
				            		}
				            """).AppendLine();
			}

			if (setup.NeedsSetterOnly)
			{
				sb.AppendLine();
				sb.Append($$"""
				            		/// <summary>
				            		///     Extensions for setter callback setups of set-only indexers with {{item}} parameters.
				            		/// </summary>
				            		extension<TValue, {{types}}>(Mockolate.Setup.IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{types}}> setup)
				            		{
				            			/// <summary>
				            			///     Executes the callback only once.
				            			/// </summary>
				            			public global::Mockolate.Setup.IIndexerSetterOnlySetup<TValue, {{types}}> OnlyOnce()
				            				=> setup.Only(1);
				            		}
				            """).AppendLine();
			}
		}

		sb.Append("""
		          	}
		          }
		          """).AppendLine();
		sb.Append("namespace Mockolate.Interactions").AppendLine();
		sb.Append("{").AppendLine();
		foreach (IndexerSetupKey key in indexerSetups)
		{
			AppendIndexerGetterAccess(sb, key.Arity);
			AppendIndexerSetterAccess(sb, key.Arity);
		}

		sb.Append("}").AppendLine();
		sb.AppendLine();

		if (indexerSetups.Any(v => v.NeedsGetterOnly || v.NeedsSetterOnly))
		{
			sb.Append("namespace Mockolate.Verify").AppendLine();
			sb.Append("{").AppendLine();
			foreach (IndexerSetupKey item in indexerSetups)
			{
				if (item.NeedsGetterOnly)
				{
					AppendIndexerVerifyGetterResult(sb, item.Arity);
				}

				if (item.NeedsSetterOnly)
				{
					AppendIndexerVerifySetterResult(sb, item.Arity, hasOverloadResolutionPriority);
				}
			}

			sb.Append("}").AppendLine();
			sb.AppendLine();
		}

		sb.AppendLine("#nullable disable");
		return sb.ToString();
	}

	private static void AppendIndexerGetterAccess(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		sb.AppendXmlSummary(
			$"An access of an indexer getter with {numberOfParameters} typed parameters.", "\t");
		sb.Append("\t[global::System.Diagnostics.DebuggerDisplay(\"{ToString()}\")]").AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\tinternal class IndexerGetterAccess<").Append(typeParams).Append(">(")
			.Append(string.Join(", ",
				Enumerable.Range(1, numberOfParameters).Select(x => $"T{x} parameter{x}")))
			.Append(")").AppendLine();
		sb.Append("\t\t: global::Mockolate.Interactions.IndexerAccess").AppendLine();
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.AppendXmlSummary($"The value of parameter {i}.");
			sb.Append("\t\tpublic T").Append(i).Append(" Parameter").Append(i).Append(" { get; } = parameter")
				.Append(i).Append(";").AppendLine();
		}

		AppendParameterHooks(sb, numberOfParameters);

		sb.Append("\t\t/// <inheritdoc cref=\"object.ToString()\" />").AppendLine();
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"get indexer [").Append(string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"{{Parameter{i}?.ToString() ?? \"null\"}}"))).Append("]\";").AppendLine();
		sb.Append("\t}").AppendLine();
	}

	private static void AppendIndexerSetterAccess(StringBuilder sb, int numberOfParameters)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		sb.AppendXmlSummary(
			$"An access of an indexer setter with {numberOfParameters} typed parameters.", "\t");
		sb.Append("\t[global::System.Diagnostics.DebuggerDisplay(\"{ToString()}\")]").AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\tinternal class IndexerSetterAccess<").Append(typeParams).Append(", TValue>(")
			.Append(string.Join(", ",
				Enumerable.Range(1, numberOfParameters).Select(x => $"T{x} parameter{x}")))
			.Append(", TValue value)").AppendLine();
		sb.Append("\t\t: global::Mockolate.Interactions.IndexerAccess").AppendLine();
		sb.Append("\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.AppendXmlSummary($"The value of parameter {i}.");
			sb.Append("\t\tpublic T").Append(i).Append(" Parameter").Append(i).Append(" { get; } = parameter")
				.Append(i).Append(";").AppendLine();
		}

		sb.AppendXmlSummary("The typed value the indexer was being set to.");
		sb.Append("\t\tpublic TValue TypedValue { get; } = value;").AppendLine();

		AppendParameterHooks(sb, numberOfParameters);

		sb.Append("\t\t/// <inheritdoc cref=\"object.ToString()\" />").AppendLine();
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"set indexer [").Append(string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"{{Parameter{i}?.ToString() ?? \"null\"}}"))).Append("] to {TypedValue?.ToString() ?? \"null\"}\";").AppendLine();
		sb.Append("\t}").AppendLine();
	}

	private static void AppendParameterHooks(StringBuilder sb, int numberOfParameters)
	{
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Interactions.IndexerAccess.ParameterCount\" />").AppendLine();
		sb.Append("\t\tpublic override int ParameterCount => ").Append(numberOfParameters).Append(";").AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Interactions.IndexerAccess.GetParameterValueAt(int)\" />").AppendLine();
		sb.Append("\t\tpublic override object? GetParameterValueAt(int index)").AppendLine();
		sb.Append("\t\t\t=> index switch").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t\t").Append(i - 1).Append(" => Parameter").Append(i).Append(",").AppendLine();
		}

		sb.Append("\t\t\t\t_ => null,").AppendLine();
		sb.Append("\t\t\t};").AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Interactions.IndexerAccess.TraverseStorage(global::Mockolate.Internals.IndexerValueStorage?, bool)\" />").AppendLine();
		sb.Append("\t\tprotected override global::Mockolate.Internals.IndexerValueStorage? TraverseStorage(global::Mockolate.Internals.IndexerValueStorage? storage, bool createMissing)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tglobal::Mockolate.Internals.IndexerValueStorage? s = storage;").AppendLine();
		sb.Append("\t\t\tif (s is null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn null;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		for (int i = 1; i < numberOfParameters; i++)
		{
			sb.Append("\t\t\ts = createMissing ? s.GetOrAddChildDispatch(Parameter").Append(i)
				.Append(") : s.GetChildDispatch(Parameter").Append(i).Append(");").AppendLine();
			sb.Append("\t\t\tif (s is null)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\treturn null;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
		}

		sb.Append("\t\t\treturn createMissing ? s.GetOrAddChildDispatch(Parameter").Append(numberOfParameters)
			.Append(") : s.GetChildDispatch(Parameter").Append(numberOfParameters).Append(");").AppendLine();
		sb.Append("\t\t}").AppendLine();
	}

	private static void AppendIndexerSetup(StringBuilder sb, int numberOfParameters,
		bool needsGetterOnly, bool needsSetterOnly)
	{
		string typeParams = GetGenericTypeParameters(numberOfParameters);
		string outTypeParams = GetOutGenericTypeParameters(numberOfParameters);
		string parameters = string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"p{i}"));
		string stateParameters = string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"state.p{i}"));
		string discards = string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(_ => "_"));

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer getter for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
		sb.Append("\tinternal interface IIndexerGetterSetup<TValue, ").Append(outTypeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's getter is accessed.");
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action callback);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Transitions the scenario to the given <paramref name=\"scenario\" /> whenever the indexer is read.");
		sb.Append("\t\tIIndexerGetterSetupParallelCallbackBuilder<TValue, ").Append(typeParams).Append("> TransitionTo(string scenario);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer getter for {GetTypeParametersDescription(numberOfParameters)} with callback support for the parameters.", "\t");
		sb.Append("\tinternal interface IIndexerGetterSetupWithCallback<TValue, ").Append(outTypeParams)
			.Append("> : global::Mockolate.Setup.IIndexerGetterSetup<TValue, ").Append(typeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's getter is accessed.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer.");
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action<")
			.Append(typeParams)
			.Append("> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's getter is accessed.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer and the value of the indexer as last parameter.");
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action<")
			.Append(typeParams)
			.Append(", TValue> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's getter is accessed.");
		sb.AppendXmlRemarks("The callback receives an incrementing access counter as first parameter, the parameters of the indexer and the value of the indexer as last parameter.");
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action<int, ")
			.Append(typeParams)
			.Append(", TValue> callback);").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer setter for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
		sb.Append("\tinternal interface IIndexerSetterSetup<TValue, ").Append(outTypeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's setter is accessed.");
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action callback);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's setter is accessed.");
		sb.AppendXmlRemarks("The callback receives the value the indexer is set to as single parameter.");
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> Do(global::System.Action<TValue> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Transitions the scenario to the given <paramref name=\"scenario\" /> whenever the indexer is written to.");
		sb.Append("\t\tIIndexerSetterSetupParallelCallbackBuilder<TValue, ").Append(typeParams).Append("> TransitionTo(string scenario);")
			.AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer setter for {GetTypeParametersDescription(numberOfParameters)} with callback support for the parameters.", "\t");
		sb.Append("\tinternal interface IIndexerSetterSetupWithCallback<TValue, ").Append(outTypeParams)
			.Append("> : global::Mockolate.Setup.IIndexerSetterSetup<TValue, ").Append(typeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's setter is accessed.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer and the value the indexer is set to as last parameter.");
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action<")
			.Append(typeParams).Append(", TValue> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\"/> to be invoked whenever the indexer's setter is accessed.");
		sb.AppendXmlRemarks("The callback receives an incrementing access counter as first parameter, the parameters of the indexer and the value the indexer is set to as last parameter.");
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append("> Do(global::System.Action<int, ")
			.Append(typeParams).Append(", TValue> callback);").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
		sb.Append("\tinternal interface IIndexerSetup<TValue, ").Append(outTypeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Sets up callbacks on the getter.");
		sb.Append("\t\tIIndexerGetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append("> OnGet { get; }").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Sets up callbacks on the setter.");
		sb.Append("\t\tIIndexerSetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append("> OnSet { get; }").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Overrides <see cref=\"global::Mockolate.MockBehavior.SkipBaseClass\" /> for this indexer only.");
		sb.AppendXmlRemarks("If not specified, use <see cref=\"global::Mockolate.MockBehavior.SkipBaseClass\" />.");
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams)
			.Append("> SkippingBaseClass(bool skipBaseClass = true);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Initializes the indexer with the given <paramref name=\"value\" />.");
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> InitializeWith(TValue value);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers the <paramref name=\"returnValue\" /> for this indexer.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Returns(TValue returnValue);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> to setup the return value for this indexer.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Returns(global::System.Func<TValue> callback);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers an <typeparamref name=\"TException\" /> to throw when the indexer is read.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws<TException>()")
			.AppendLine();
		sb.Append("\t\t\twhere TException : global::System.Exception, new();").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers an <paramref name=\"exception\" /> to throw when the indexer is read.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws(global::System.Exception exception);")
			.AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> that will calculate the exception to throw when the indexer is read.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Throws(global::System.Func<global::System.Exception> callback);")
			.AppendLine();

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)} with callback support for the parameters.", "\t");
		sb.Append("\tinternal interface IIndexerSetupWithCallback<TValue, ").Append(outTypeParams)
			.Append("> : global::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();

		sb.AppendXmlSummary("Initializes the indexer according to the given <paramref name=\"valueGenerator\" />.");
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> InitializeWith(global::System.Func<")
			.Append(typeParams).Append(", TValue> valueGenerator);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> to setup the return value for this indexer.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Returns(global::System.Func<")
			.Append(typeParams)
			.Append(", TValue> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> to setup the return value for this indexer.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer and the value of the indexer as last parameter.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Returns(global::System.Func<")
			.Append(typeParams).Append(", TValue, TValue> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> that will calculate the exception to throw when the indexer is read.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws(global::System.Func<")
			.Append(typeParams)
			.Append(", global::System.Exception> callback);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary("Registers a <paramref name=\"callback\" /> that will calculate the exception to throw when the indexer is read.");
		sb.AppendXmlRemarks("The callback receives the parameters of the indexer and the value of the indexer as last parameter.");
		sb.Append("\t\tIIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws(global::System.Func<")
			.Append(typeParams).Append(", TValue, global::System.Exception> callback);").AppendLine();

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		foreach (string side in new[]
		         {
			         "Getter", "Setter",
		         })
		{
			sb.AppendXmlSummary($"Sets up a {side.ToLowerInvariant()} callback for a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
			sb.Append("\tinternal interface IIndexer").Append(side).Append("SetupCallbackBuilder<TValue, ").Append(outTypeParams)
				.Append("> : IIndexer").Append(side).Append("SetupParallelCallbackBuilder<TValue, ").Append(typeParams).Append(">").AppendLine();
			sb.Append("\t{").AppendLine();
			sb.AppendXmlSummary("Runs the callback in parallel to the other callbacks.");
			sb.Append("\t\tIIndexer").Append(side).Append("SetupParallelCallbackBuilder<TValue, ").Append(typeParams).Append("> InParallel();")
				.AppendLine();
			sb.Append("\t}").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Sets up a parallel {side.ToLowerInvariant()} callback for a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
			sb.Append("\tinternal interface IIndexer").Append(side).Append("SetupParallelCallbackBuilder<TValue, ").Append(outTypeParams)
				.Append("> : IIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(typeParams).Append(">").AppendLine();
			sb.Append("\t{").AppendLine();
			sb.AppendXmlSummary("Limits the callback to only execute for indexer accesses where the predicate returns true.");
			sb.AppendXmlRemarks("Provides a zero-based counter indicating how many times the indexer has been accessed so far.");
			sb.Append("\t\tIIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(typeParams)
				.Append("> When(global::System.Func<int, bool> predicate);").AppendLine();
			sb.Append("\t}").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Sets up a when {side.ToLowerInvariant()} callback for a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
			sb.Append("\tinternal interface IIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(outTypeParams)
				.Append("> : global::Mockolate.Setup.IIndexerSetupWithCallback<TValue, ").Append(typeParams).Append(">").AppendLine();
			sb.Append("\t{").AppendLine();

			sb.AppendXmlSummary("Repeats the callback for the given number of <paramref name=\"times\" />.");
			sb.AppendXmlRemarks($"The number of times is only counted for actual executions (<see cref=\"IIndexer{side}SetupParallelCallbackBuilder{{TValue, {typeParams}}}.When(global::System.Func{{int, bool}})\" /> evaluates to <see langword=\"true\" />).");
			sb.Append("\t\tIIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(typeParams).Append("> For(int times);")
				.AppendLine();
			sb.AppendLine();
			sb.AppendXmlSummary("Deactivates the callback after the given number of <paramref name=\"times\" />.");
			sb.AppendXmlRemarks($"The number of times is only counted for actual executions (<see cref=\"IIndexer{side}SetupParallelCallbackBuilder{{TValue, {typeParams}}}.When(global::System.Func{{int, bool}})\" /> evaluates to <see langword=\"true\" />).");
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> Only(int times);").AppendLine();
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary($"Sets up a return/throw callback for a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
		sb.Append("\tinternal interface IIndexerSetupReturnBuilder<TValue, ").Append(outTypeParams)
			.Append("> : IIndexerSetupReturnWhenBuilder<TValue, ").Append(typeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Limits the return/throw callback to only execute for indexer accesses where the predicate returns true.");
		sb.AppendXmlRemarks("Provides a zero-based counter indicating how many times the indexer has been accessed so far.");
		sb.Append("\t\tIIndexerSetupReturnWhenBuilder<TValue, ").Append(typeParams)
			.Append("> When(global::System.Func<int, bool> predicate);").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a when return/throw callback for a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
		sb.Append("\tinternal interface IIndexerSetupReturnWhenBuilder<TValue, ").Append(outTypeParams)
			.Append("> : global::Mockolate.Setup.IIndexerSetupWithCallback<TValue, ").Append(typeParams).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Repeats the return/throw callback for the given number of <paramref name=\"times\" />.");
		sb.AppendXmlRemarks($"The number of times is only counted for actual executions (<see cref=\"IIndexerSetupReturnBuilder{{TValue, {typeParams}}}.When(global::System.Func{{int, bool}})\" /> evaluates to <see langword=\"true\" />).");
		sb.Append("\t\tIIndexerSetupReturnWhenBuilder<TValue, ").Append(typeParams).Append("> For(int times);")
			.AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Deactivates the return/throw after the given number of <paramref name=\"times\" />.");
		sb.AppendXmlRemarks($"The number of times is only counted for actual executions (<see cref=\"IIndexerSetupReturnBuilder{{TValue, {typeParams}}}.When(global::System.Func{{int, bool}})\" /> evaluates to <see langword=\"true\" />).");
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> Only(int times);").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Sets up a <typeparamref name=\"TValue\"/> indexer for {GetTypeParametersDescription(numberOfParameters)}.", "\t");
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tinternal class IndexerSetup<TValue, ").Append(typeParams).Append(">(")
			.Append("global::Mockolate.Internals.MockRegistry mockRegistry")
			.Append(", ")
			.Append(
				string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"global::Mockolate.Parameters.IParameterMatch<T{i}> parameter{i}")))
			.Append(") : global::Mockolate.Setup.IndexerSetup(mockRegistry),")
			.AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetupWithCallback<TValue, ").Append(typeParams).Append(">,").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append(">,").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams).Append(">,").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append(">,").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterSetupWithCallback<TValue, ").Append(typeParams).Append(">,").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetterSetupWithCallback<TValue, ").Append(typeParams).Append(">");
		if (needsGetterOnly)
		{
			sb.Append(",").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterOnlySetup<TValue, ").Append(typeParams).Append(">,").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterOnlyGetterSetup<TValue, ").Append(typeParams).Append(">,").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterOnlySetupCallbackBuilder<TValue, ").Append(typeParams).Append(">,").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerGetterOnlySetupReturnBuilder<TValue, ").Append(typeParams).Append(">");
		}

		if (needsSetterOnly)
		{
			sb.Append(",").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetterOnlySetup<TValue, ").Append(typeParams).Append(">,").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetterOnlySetterSetup<TValue, ").Append(typeParams).Append(">,").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetterOnlySetupCallbackBuilder<TValue, ").Append(typeParams).Append(">");
		}

		sb.AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.Internals.Callbacks<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? _getterCallbacks;")
			.AppendLine();
		sb.Append("\t\tprivate global::Mockolate.Internals.Callbacks<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? _setterCallbacks;")
			.AppendLine();
		sb.Append("\t\tprivate global::Mockolate.Internals.Callbacks<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>? _returnCallbacks;")
			.AppendLine();
		sb.Append("\t\tprivate bool? _skipBaseClass;").AppendLine();
		sb.Append("\t\tprivate global::System.Func<").Append(typeParams).Append(", TValue>? _initialization;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.SkippingBaseClass(bool)\" />").AppendLine();
		sb.Append("\t\tpublic global::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams)
			.Append("> SkippingBaseClass(bool skipBaseClass = true)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_skipBaseClass = skipBaseClass;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.InitializeWith(TValue)\" />").AppendLine();
		sb.Append("\t\tpublic global::Mockolate.Setup.IIndexerSetupWithCallback<TValue, ").Append(typeParams).Append("> InitializeWith(TValue value)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_initialization is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append(
				"\t\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The indexer is already initialized. You cannot initialize it twice.\");")
			.AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\t_initialization = (").Append(discards).Append(") => value;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams)
			.Append("> global::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams)
			.Append(">.InitializeWith(TValue value)").AppendLine();
		sb.Append("\t\t\t=> InitializeWith(value);").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetupWithCallback{TValue, ").Append(typeParams)
			.Append("}.InitializeWith(global::System.Func{").Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tpublic global::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> InitializeWith(global::System.Func<")
			.Append(typeParams).Append(", TValue> valueGenerator)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_initialization is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append(
				"\t\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The indexer is already initialized. You cannot initialize it twice.\");")
			.AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\t_initialization = valueGenerator;").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.OnGet\" />")
			.AppendLine();
		sb.Append("\t\tpublic IIndexerGetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append("> OnGet").AppendLine();
		sb.Append("\t\t\t=> this;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerGetterSetup{TValue, ").Append(typeParams)
			.Append("}.Do(global::System.Action)\" />").AppendLine();
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerGetterSetup<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, ")
			.Append(discards).Append(", _) => callback());").AppendLine();
		sb.Append("\t\t\t_getterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_getterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerGetterSetupWithCallback{TValue, ").Append(typeParams).Append("}.Do(global::System.Action{")
			.Append(typeParams).Append("})\" />").AppendLine();
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerGetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<")
			.Append(typeParams)
			.Append("> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, ")
			.Append(parameters).Append(", _) => callback(").Append(parameters).Append("));").AppendLine();
		sb.Append("\t\t\t_getterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_getterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerGetterSetupWithCallback{TValue, ").Append(typeParams).Append("}.Do(global::System.Action{")
			.Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerGetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<")
			.Append(typeParams)
			.Append(", TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, ")
			.Append(parameters).Append(", v) => callback(").Append(parameters).Append(", v));").AppendLine();
		sb.Append("\t\t\t_getterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_getterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerGetterSetupWithCallback{TValue, ").Append(typeParams)
			.Append("}.Do(global::System.Action{int, ").Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tIIndexerGetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerGetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<int, ")
			.Append(typeParams)
			.Append(", TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? currentCallback = new(callback);")
			.AppendLine();
		sb.Append("\t\t\t_getterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_getterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\tIIndexerGetterSetupParallelCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerGetterSetup<TValue, ").Append(typeParams)
			.Append(">.TransitionTo(string scenario)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? currentCallback = new((_, ")
			.Append(discards).Append(", _) => TransitionScenario(scenario));").AppendLine();
		sb.Append("\t\t\tcurrentCallback.InParallel();").AppendLine();
		sb.Append("\t\t\t_getterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_getterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.OnSet\" />")
			.AppendLine();
		sb.Append("\t\tpublic IIndexerSetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append("> OnSet").AppendLine();
		sb.Append("\t\t\t=> this;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetterSetup{TValue, ").Append(typeParams)
			.Append("}.Do(global::System.Action)\" />").AppendLine();
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetterSetup<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, _, ")
			.Append(discards).Append(") => callback());").AppendLine();
		sb.Append("\t\t\t_setterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_setterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetterSetup{TValue, ").Append(typeParams)
			.Append("}.Do(global::System.Action{TValue})\" />").AppendLine();
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetterSetup<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<TValue> callback)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, ")
			.Append(discards).Append(", v) => callback(v));").AppendLine();
		sb.Append("\t\t\t_setterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_setterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetterSetupWithCallback{TValue, ").Append(typeParams).Append("}.Do(global::System.Action{")
			.Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<")
			.Append(typeParams).Append(", TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>>? currentCallback = new((_, ")
			.Append(parameters).Append(", v) => callback(").Append(parameters).Append(", v));").AppendLine();
		sb.Append("\t\t\t_setterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_setterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetterSetupWithCallback{TValue, ").Append(typeParams)
			.Append("}.Do(global::System.Action{int, ").Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tIIndexerSetterSetupCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetterSetupWithCallback<TValue, ").Append(typeParams)
			.Append(">.Do(global::System.Action<int, ")
			.Append(typeParams).Append(", TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? currentCallback = new(callback);")
			.AppendLine();
		sb.Append("\t\t\t_setterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_setterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\tIIndexerSetterSetupParallelCallbackBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetterSetup<TValue, ").Append(typeParams)
			.Append(">.TransitionTo(string scenario)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tCallback<global::System.Action<int, ").Append(typeParams)
			.Append(", TValue>>? currentCallback = new((_, ")
			.Append(discards).Append(", _) => TransitionScenario(scenario));").AppendLine();
		sb.Append("\t\t\tcurrentCallback.InParallel();").AppendLine();
		sb.Append("\t\t\t_setterCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_setterCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.Returns(TValue)\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Returns(TValue returnValue)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(discards).Append(", _) => returnValue);").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.Returns(global::System.Func{TValue})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Returns(global::System.Func<TValue> callback)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(discards).Append(", _) => callback());").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.Returns(global::System.Func{")
			.Append(typeParams).Append(", TValue})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Returns(global::System.Func<")
			.Append(typeParams)
			.Append(", TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(parameters).Append(", _) => callback(").Append(parameters)
			.Append("));")
			.AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.Returns(global::System.Func{")
			.Append(typeParams).Append(", TValue, TValue})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Returns(global::System.Func<")
			.Append(typeParams).Append(", TValue, TValue> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, v, ").Append(parameters).Append(") => callback(v, ").Append(parameters)
			.Append("));").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.Throws{TException}()\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws<TException>()")
			.AppendLine();
		sb.Append("\t\t\twhere TException : global::System.Exception, new()").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(discards).Append(", _) => throw new TException());").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.Throws(global::System.Exception)\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Throws(global::System.Exception exception)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(discards).Append(", _) => throw exception);").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams)
			.Append("}.Throws(global::System.Func{global::System.Exception})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append("> Throws(global::System.Func<global::System.Exception> callback)")
			.AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(discards).Append(", _) => throw callback());").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.Throws(global::System.Func{")
			.Append(typeParams).Append(", global::System.Exception})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws(global::System.Func<")
			.Append(typeParams)
			.Append(", global::System.Exception> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(parameters).Append(", _) => throw callback(").Append(parameters)
			.Append("));").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetup{TValue, ").Append(typeParams).Append("}.Throws(global::System.Func{")
			.Append(typeParams).Append(", TValue, global::System.Exception})\" />").AppendLine();
		sb.Append("\t\tpublic IIndexerSetupReturnBuilder<TValue, ").Append(typeParams).Append("> Throws(global::System.Func<")
			.Append(typeParams).Append(", TValue, global::System.Exception> callback)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar currentCallback = new Callback<global::System.Func<int, ").Append(typeParams)
			.Append(", TValue, TValue>>((_, ").Append(parameters).Append(", v) => throw callback(").Append(parameters)
			.Append(", v));").AppendLine();
		sb.Append("\t\t\t_returnCallbacks = global::Mockolate.Internals.CallbacksExtensions.Register(_returnCallbacks, currentCallback);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		foreach ((string side, string fieldName) in new[]
		         {
			         ("Getter", "_getterCallbacks"), ("Setter", "_setterCallbacks"),
		         })
		{
			sb.Append("\t\t/// <inheritdoc cref=\"IIndexer").Append(side).Append("SetupParallelCallbackBuilder{TValue, ").Append(typeParams)
				.Append("}.When(global::System.Func{int, bool})\" />").AppendLine();
			sb.Append("\t\tIIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(typeParams)
				.Append("> IIndexer").Append(side).Append("SetupParallelCallbackBuilder<TValue, ").Append(typeParams)
				.Append(">.When(global::System.Func<int, bool> predicate)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\t").Append(fieldName).Append("?.Active?.When(predicate);").AppendLine();
			sb.Append("\t\t\treturn this;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();

			sb.Append("\t\t/// <inheritdoc cref=\"IIndexer").Append(side).Append("SetupCallbackBuilder{TValue, ").Append(typeParams)
				.Append("}.InParallel()\" />").AppendLine();
			sb.Append("\t\tIIndexer").Append(side).Append("SetupParallelCallbackBuilder<TValue, ").Append(typeParams)
				.Append("> IIndexer").Append(side).Append("SetupCallbackBuilder<TValue, ").Append(typeParams)
				.Append(">.InParallel()").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\t").Append(fieldName).Append("?.Active?.InParallel();").AppendLine();
			sb.Append("\t\t\treturn this;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();

			sb.Append("\t\t/// <inheritdoc cref=\"IIndexer").Append(side).Append("SetupCallbackWhenBuilder{TValue, ").Append(typeParams)
				.Append("}.For(int)\" />").AppendLine();
			sb.Append("\t\tIIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ").Append(typeParams)
				.Append("> IIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ")
				.Append(typeParams)
				.Append(">.For(int times)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\t").Append(fieldName).Append("?.Active?.For(times);").AppendLine();
			sb.Append("\t\t\treturn this;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();

			sb.Append("\t\t/// <inheritdoc cref=\"IIndexer").Append(side).Append("SetupCallbackWhenBuilder{TValue, ").Append(typeParams)
				.Append("}.Only(int)\" />").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> IIndexer").Append(side).Append("SetupCallbackWhenBuilder<TValue, ")
				.Append(typeParams)
				.Append(">.Only(int times)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\t").Append(fieldName).Append("?.Active?.Only(times);").AppendLine();
			sb.Append("\t\t\treturn this;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetupReturnBuilder{TValue, ").Append(typeParams)
			.Append("}.When(global::System.Func{int, bool})\" />").AppendLine();
		sb.Append("\t\tIIndexerSetupReturnWhenBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetupReturnBuilder<TValue, ").Append(typeParams)
			.Append(">.When(global::System.Func<int, bool> predicate)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnCallbacks?.Active?.When(predicate);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetupReturnWhenBuilder{TValue, ").Append(typeParams)
			.Append("}.For(int)\" />").AppendLine();
		sb.Append("\t\tIIndexerSetupReturnWhenBuilder<TValue, ").Append(typeParams)
			.Append("> IIndexerSetupReturnWhenBuilder<TValue, ")
			.Append(typeParams)
			.Append(">.For(int times)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnCallbacks?.Active?.For(times);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"IIndexerSetupReturnWhenBuilder{TValue, ").Append(typeParams)
			.Append("}.Only(int)\" />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Setup.IIndexerSetup<TValue, ").Append(typeParams).Append("> IIndexerSetupReturnWhenBuilder<TValue, ")
			.Append(typeParams)
			.Append(">.Only(int times)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\t_returnCallbacks?.Active?.Only(times);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// Matches(T1..TN)
		sb.Append("\t\t/// <summary>").AppendLine();
		sb.Append("\t\t///     Check if the setup matches the specified parameter values.").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		sb.Append("\t\tpublic virtual bool Matches(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} p{i}"))).Append(")").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (");
		sb.Append(string.Join(" || ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"!parameter{i}.Matches(p{i})")));
		sb.Append(")").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn false;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\tparameter").Append(i).Append(".InvokeCallbacks(p").Append(i).Append(");").AppendLine();
		}

		sb.Append("\t\t\treturn true;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// Matches(T1..TN, TValue)
		sb.Append("\t\t/// <summary>").AppendLine();
		sb.Append("\t\t///     Check if the setup matches the specified parameter values.").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		sb.Append("\t\tpublic virtual bool Matches(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} p{i}"))).Append(", TValue value)").AppendLine();
		sb.Append("\t\t\t=> Matches(").Append(parameters).Append(");").AppendLine();
		sb.AppendLine();

		// MatchesAccess override
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.MatchesAccess(global::Mockolate.Interactions.IndexerAccess)\" />").AppendLine();
		sb.Append("\t\tprotected override bool MatchesAccess(global::Mockolate.Interactions.IndexerAccess access)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (access is global::Mockolate.Interactions.IndexerGetterAccess<").Append(typeParams).Append("> getter)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn Matches(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"getter.Parameter{i}"))).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (access is global::Mockolate.Interactions.IndexerSetterAccess<").Append(typeParams).Append(", TValue> setter)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn Matches(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"setter.Parameter{i}"))).Append(", setter.TypedValue);").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn false;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// SkipBaseClass
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.SkipBaseClass()\" />").AppendLine();
		sb.Append("\t\tpublic override bool? SkipBaseClass()").AppendLine();
		sb.Append("\t\t\t=> _skipBaseClass;").AppendLine();
		sb.AppendLine();

		// GetResult(TResult baseValue)
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.GetResult{TResult}(global::Mockolate.Interactions.IndexerAccess, global::Mockolate.MockBehavior, TResult)\" />").AppendLine();
		sb.Append("\t\tpublic override TResult GetResult<TResult>(global::Mockolate.Interactions.IndexerAccess access, global::Mockolate.MockBehavior behavior, TResult baseValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (!TryExtractParameters(access");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", out T").Append(i).Append(" p").Append(i);
		}

		sb.Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn baseValue;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tTValue currentValue = TryCast(baseValue, out TValue casted, behavior) ? casted : default!;").AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteGetterCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteReturnCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\taccess.StoreValue(currentValue);").AppendLine();
		sb.Append("\t\t\treturn TryCast(currentValue, out TResult result, behavior) ? result : baseValue;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// GetResult(behavior) - no-closure entry point used by the generated mock indexer body
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.GetResult{TResult}(global::Mockolate.Interactions.IndexerAccess, global::Mockolate.MockBehavior)\" />").AppendLine();
		sb.Append("\t\tpublic override TResult GetResult<TResult>(global::Mockolate.Interactions.IndexerAccess access, global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (!TryExtractParameters(access");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", out T").Append(i).Append(" p").Append(i);
		}

		sb.Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn behavior.DefaultValue.Generate(default(TResult)!);").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tTValue currentValue;").AppendLine();
		sb.Append("\t\t\tif (access.TryFindStoredValue(out TValue existing))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = existing;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\telse if (_initialization is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = _initialization.Invoke(").Append(parameters).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\telse").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = TryCast(behavior.DefaultValue.Generate(default(TValue)!), out TValue casted, behavior) ? casted : default!;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteGetterCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteReturnCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\taccess.StoreValue(currentValue);").AppendLine();
		sb.Append("\t\t\treturn TryCast(currentValue, out TResult result, behavior) ? result : behavior.DefaultValue.Generate(default(TResult)!);").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// GetResult(Func<TResult> defaultValueGenerator)
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.GetResult{TResult}(global::Mockolate.Interactions.IndexerAccess, global::Mockolate.MockBehavior, global::System.Func{TResult})\" />").AppendLine();
		sb.Append("\t\tpublic override TResult GetResult<TResult>(global::Mockolate.Interactions.IndexerAccess access, global::Mockolate.MockBehavior behavior, global::System.Func<TResult> defaultValueGenerator)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (!TryExtractParameters(access");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", out T").Append(i).Append(" p").Append(i);
		}

		sb.Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn defaultValueGenerator();").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tTValue currentValue;").AppendLine();
		sb.Append("\t\t\tif (access.TryFindStoredValue(out TValue existing))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = existing;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\telse if (_initialization is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = _initialization.Invoke(").Append(parameters).Append(");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\telse").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tcurrentValue = TryCast(defaultValueGenerator(), out TValue casted, behavior) ? casted : default!;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteGetterCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\tcurrentValue = ExecuteReturnCallbacks(").Append(parameters).Append(", currentValue);").AppendLine();
		sb.Append("\t\t\taccess.StoreValue(currentValue);").AppendLine();
		sb.Append("\t\t\treturn TryCast(currentValue, out TResult result, behavior) ? result : defaultValueGenerator();").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// SetResult
		sb.Append("\t\t/// <inheritdoc cref=\"global::Mockolate.Setup.IndexerSetup.SetResult{TResult}(global::Mockolate.Interactions.IndexerAccess, global::Mockolate.MockBehavior, TResult)\" />").AppendLine();
		sb.Append("\t\tpublic override void SetResult<TResult>(global::Mockolate.Interactions.IndexerAccess access, global::Mockolate.MockBehavior behavior, TResult value)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\taccess.StoreValue(value);").AppendLine();
		sb.Append("\t\t\tif (!TryExtractParameters(access");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", out T").Append(i).Append(" p").Append(i);
		}

		sb.Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (!TryCast(value, out TValue resultValue, behavior))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\treturn;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (_setterCallbacks is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tbool wasInvoked = false;").AppendLine();
		sb.Append("\t\t\t\tint currentSetterCallbacksIndex = _setterCallbacks.CurrentIndex;").AppendLine();
		sb.Append("\t\t\t\tfor (int i = 0; i < _setterCallbacks.Count; i++)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>> setterCallback =").AppendLine();
		sb.Append("\t\t\t\t\t\t_setterCallbacks[(currentSetterCallbacksIndex + i) % _setterCallbacks.Count];").AppendLine();
		sb.Append("\t\t\t\t\tif (setterCallback.Invoke(wasInvoked, ref _setterCallbacks.CurrentIndex, (").Append(parameters).Append(", resultValue),").AppendLine();
		sb.Append("\t\t\t\t\t\tstatic (count, @delegate, state) => @delegate(count, ").Append(stateParameters).Append(", state.resultValue)))").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\twasInvoked = true;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// ExecuteGetterCallbacks (private)
		sb.Append("\t\tprivate TValue ExecuteGetterCallbacks(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} p{i}"))).Append(", TValue currentValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_getterCallbacks is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tbool wasInvoked = false;").AppendLine();
		sb.Append("\t\t\t\tint currentGetterCallbacksIndex = _getterCallbacks.CurrentIndex;").AppendLine();
		sb.Append("\t\t\t\tfor (int i = 0; i < _getterCallbacks.Count; i++)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tCallback<global::System.Action<int, ").Append(typeParams).Append(", TValue>> getterCallback =").AppendLine();
		sb.Append("\t\t\t\t\t\t_getterCallbacks[(currentGetterCallbacksIndex + i) % _getterCallbacks.Count];").AppendLine();
		sb.Append("\t\t\t\t\tif (getterCallback.Invoke(wasInvoked, ref _getterCallbacks.CurrentIndex, (").Append(parameters).Append(", currentValue),").AppendLine();
		sb.Append("\t\t\t\t\t\tstatic (count, @delegate, state) => @delegate(count, ").Append(stateParameters).Append(", state.currentValue)))").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\twasInvoked = true;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn currentValue;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// ExecuteReturnCallbacks (private)
		sb.Append("\t\tprivate TValue ExecuteReturnCallbacks(").Append(
			string.Join(", ", Enumerable.Range(1, numberOfParameters).Select(i => $"T{i} p{i}"))).Append(", TValue currentValue)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (_returnCallbacks is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tforeach (Callback<global::System.Func<int, ").Append(typeParams).Append(", TValue, TValue>> _ in _returnCallbacks)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tCallback<global::System.Func<int, ").Append(typeParams).Append(", TValue, TValue>> returnCallback =").AppendLine();
		sb.Append("\t\t\t\t\t\t_returnCallbacks[_returnCallbacks.CurrentIndex % _returnCallbacks.Count];").AppendLine();
		sb.Append("\t\t\t\t\tif (returnCallback.Invoke(ref _returnCallbacks.CurrentIndex, (").Append(parameters).Append(", currentValue),").AppendLine();
		sb.Append("\t\t\t\t\t\tstatic (count, @delegate, state) => @delegate(count, ").Append(stateParameters).Append(", state.currentValue),").AppendLine();
		sb.Append("\t\t\t\t\t\tout TValue? newValue))").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\treturn newValue!;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\treturn currentValue;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// TryExtractParameters (private static)
		sb.Append("\t\tprivate static bool TryExtractParameters(global::Mockolate.Interactions.IndexerAccess access");
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append(", out T").Append(i).Append(" p").Append(i);
		}

		sb.Append(")").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (access is global::Mockolate.Interactions.IndexerGetterAccess<").Append(typeParams).Append("> getter)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t\tp").Append(i).Append(" = getter.Parameter").Append(i).Append(";").AppendLine();
		}

		sb.Append("\t\t\t\treturn true;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\tif (access is global::Mockolate.Interactions.IndexerSetterAccess<").Append(typeParams).Append(", TValue> setter)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\t\tp").Append(i).Append(" = setter.Parameter").Append(i).Append(";").AppendLine();
		}

		sb.Append("\t\t\t\treturn true;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();
		for (int i = 1; i <= numberOfParameters; i++)
		{
			sb.Append("\t\t\tp").Append(i).Append(" = default!;").AppendLine();
		}

		sb.Append("\t\t\treturn false;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		// ToString
		sb.Append("\t\t/// <inheritdoc cref=\"object.ToString()\" />").AppendLine();
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> $\"{FormatType(typeof(TValue))} this[").Append(string.Join(", ",
			Enumerable.Range(1, numberOfParameters).Select(i => $"{{parameter{i}}}"))).Append("]\";").AppendLine();
		sb.AppendLine();

		if (needsGetterOnly)
		{
			AppendGetterOnlyIndexerImplementation(sb, numberOfParameters);
		}

		if (needsSetterOnly)
		{
			AppendSetterOnlyIndexerImplementation(sb, numberOfParameters);
		}

		sb.Append("\t}").AppendLine();
	}

	private static void AppendGetterOnlyIndexerInterfaces(StringBuilder sb, int numberOfParameters)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);
		string outTp = GetOutGenericTypeParameters(numberOfParameters);
		string description = GetTypeParametersDescription(numberOfParameters);

		sb.AppendLine();
		sb.Append($$"""
		            	/// <summary>
		            	///     Setup for a mocked <typeparamref name="TValue"/> indexer for {{description}} that the mock only reads.
		            	/// </summary>
		            	/// <remarks>
		            	///     Used instead of <see cref="IIndexerSetup{TValue, {{tp}}}" /> when the mock has no setter to intercept, either
		            	///     because the indexer is declared without one or because its setter is not accessible from the mock's assembly.
		            	///     Writes then never reach the mock, so <see cref="IIndexerSetup{TValue, {{tp}}}.OnSet" /> is not offered.
		            	/// </remarks>
		            	internal interface IIndexerGetterOnlySetup<TValue, {{outTp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.OnGet" />
		            		IIndexerGetterOnlyGetterSetup<TValue, {{tp}}> OnGet { get; }

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.SkippingBaseClass(bool)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> SkippingBaseClass(bool skipBaseClass = true);

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.InitializeWith(TValue)" />
		            		/// <remarks>
		            		///     Seeds the value that reads return. Unlike a read-write indexer there is no setter to update the
		            		///     slot afterwards, so it stays at <paramref name="value" /> unless a <c>Returns</c> entry applies.
		            		/// </remarks>
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> InitializeWith(TValue value);

		            		/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, {{tp}}}.InitializeWith(global::System.Func{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> InitializeWith(global::System.Func<{{tp}}, TValue> valueGenerator);

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.Returns(TValue)" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Returns(TValue returnValue);

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.Returns(global::System.Func{TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Returns(global::System.Func<TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, {{tp}}}.Returns(global::System.Func{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Returns(global::System.Func<{{tp}}, TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, {{tp}}}.Returns(global::System.Func{{{tp}}, TValue, TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Returns(global::System.Func<{{tp}}, TValue, TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.Throws{TException}()" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Throws<TException>()
		            			where TException : global::System.Exception, new();

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.Throws(global::System.Exception)" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Throws(global::System.Exception exception);

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.Throws(global::System.Func{global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Throws(global::System.Func<global::System.Exception> callback);

		            		/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, {{tp}}}.Throws(global::System.Func{{{tp}}, global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Throws(global::System.Func<{{tp}}, global::System.Exception> callback);

		            		/// <inheritdoc cref="IIndexerSetupWithCallback{TValue, {{tp}}}.Throws(global::System.Func{{{tp}}, TValue, global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> Throws(global::System.Func<{{tp}}, TValue, global::System.Exception> callback);
		            	}

		            	/// <summary>
		            	///     Setup for attaching side-effects to the getter of a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	/// <remarks>
		            	///     The counterpart of <see cref="IIndexerGetterSetupWithCallback{TValue, {{tp}}}" /> for
		            	///     <see cref="IIndexerGetterOnlySetup{TValue, {{tp}}}" />: the returned builders stay on the getter-only surface,
		            	///     so chaining can never reach <see cref="IIndexerSetup{TValue, {{tp}}}.OnSet" />.
		            	/// </remarks>
		            	internal interface IIndexerGetterOnlyGetterSetup<TValue, {{outTp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerGetterSetup{TValue, {{tp}}}.Do(global::System.Action)" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action callback);

		            		/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, {{tp}}}.Do(global::System.Action{{{tp}}})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<{{tp}}> callback);

		            		/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, {{tp}}}.Do(global::System.Action{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<{{tp}}, TValue> callback);

		            		/// <inheritdoc cref="IIndexerGetterSetupWithCallback{TValue, {{tp}}}.Do(global::System.Action{int, {{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<int, {{tp}}, TValue> callback);

		            		/// <inheritdoc cref="IIndexerGetterSetup{TValue, {{tp}}}.TransitionTo(string)" />
		            		IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> TransitionTo(string scenario);
		            	}

		            	/// <summary>
		            	///     Sets up a callback for a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerGetterOnlySetupCallbackBuilder<TValue, {{outTp}}>
		            		: IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerGetterSetupCallbackBuilder{TValue, {{tp}}}.InParallel()" />
		            		IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> InParallel();
		            	}

		            	/// <summary>
		            	///     Sets up a parallel callback for a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{outTp}}>
		            		: IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerGetterSetupParallelCallbackBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> When(global::System.Func<int, bool> predicate);
		            	}

		            	/// <summary>
		            	///     Sets up a when callback for a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{outTp}}>
		            		: IIndexerGetterOnlySetup<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> For(int times);

		            		/// <inheritdoc cref="IIndexerGetterSetupCallbackWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> Only(int times);
		            	}

		            	/// <summary>
		            	///     Sets up a return/throw builder for a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerGetterOnlySetupReturnBuilder<TValue, {{outTp}}>
		            		: IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetupReturnBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}> When(global::System.Func<int, bool> predicate);
		            	}

		            	/// <summary>
		            	///     Sets up a when builder for returns/throws for a get-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{outTp}}>
		            		: IIndexerGetterOnlySetup<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}> For(int times);

		            		/// <inheritdoc cref="IIndexerSetupReturnWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> Only(int times);
		            	}
		            """).AppendLine();
	}

	private static void AppendSetterOnlyIndexerInterfaces(StringBuilder sb, int numberOfParameters)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);
		string outTp = GetOutGenericTypeParameters(numberOfParameters);
		string description = GetTypeParametersDescription(numberOfParameters);

		sb.AppendLine();
		sb.Append($$"""
		            	/// <summary>
		            	///     Setup for a mocked <typeparamref name="TValue"/> indexer for {{description}} that the mock only writes.
		            	/// </summary>
		            	/// <remarks>
		            	///     The write-only counterpart of <see cref="IIndexerGetterOnlySetup{TValue, {{tp}}}" />: the mock has no getter to
		            	///     intercept, so <see cref="IIndexerSetup{TValue, {{tp}}}.OnGet" />, <c>InitializeWith</c> and the
		            	///     <c>Returns</c>/<c>Throws</c> read-sequence are not offered.
		            	/// </remarks>
		            	internal interface IIndexerSetterOnlySetup<TValue, {{outTp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.OnSet" />
		            		IIndexerSetterOnlySetterSetup<TValue, {{tp}}> OnSet { get; }

		            		/// <inheritdoc cref="IIndexerSetup{TValue, {{tp}}}.SkippingBaseClass(bool)" />
		            		IIndexerSetterOnlySetup<TValue, {{tp}}> SkippingBaseClass(bool skipBaseClass = true);
		            	}

		            	/// <summary>
		            	///     Setup for attaching side-effects to the setter of a set-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	/// <remarks>
		            	///     The counterpart of <see cref="IIndexerSetterSetupWithCallback{TValue, {{tp}}}" /> for
		            	///     <see cref="IIndexerSetterOnlySetup{TValue, {{tp}}}" />: the returned builders stay on the setter-only surface,
		            	///     so chaining can never reach <see cref="IIndexerSetup{TValue, {{tp}}}.OnGet" /> or the
		            	///     <c>Returns</c>/<c>Throws</c> read-sequence.
		            	/// </remarks>
		            	internal interface IIndexerSetterOnlySetterSetup<TValue, {{outTp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetterSetup{TValue, {{tp}}}.Do(global::System.Action)" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action callback);

		            		/// <inheritdoc cref="IIndexerSetterSetup{TValue, {{tp}}}.Do(global::System.Action{TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, {{tp}}}.Do(global::System.Action{{{tp}}, TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<{{tp}}, TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetterSetupWithCallback{TValue, {{tp}}}.Do(global::System.Action{int, {{tp}}, TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> Do(global::System.Action<int, {{tp}}, TValue> callback);

		            		/// <inheritdoc cref="IIndexerSetterSetup{TValue, {{tp}}}.TransitionTo(string)" />
		            		IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> TransitionTo(string scenario);
		            	}

		            	/// <summary>
		            	///     Sets up a setter callback for a set-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerSetterOnlySetupCallbackBuilder<TValue, {{outTp}}>
		            		: IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetterSetupCallbackBuilder{TValue, {{tp}}}.InParallel()" />
		            		IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> InParallel();
		            	}

		            	/// <summary>
		            	///     Sets up a parallel setter callback for a set-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{outTp}}>
		            		: IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetterSetupParallelCallbackBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> When(global::System.Func<int, bool> predicate);
		            	}

		            	/// <summary>
		            	///     Sets up a when setter callback for a set-only <typeparamref name="TValue"/> indexer for {{description}}.
		            	/// </summary>
		            	internal interface IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{outTp}}>
		            		: IIndexerSetterOnlySetup<TValue, {{tp}}>
		            	{
		            		/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> For(int times);

		            		/// <inheritdoc cref="IIndexerSetterSetupCallbackWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerSetterOnlySetup<TValue, {{tp}}> Only(int times);
		            	}
		            """).AppendLine();
	}

	private static void AppendGetterOnlyIndexerImplementation(StringBuilder sb, int numberOfParameters)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);

		sb.Append($$"""
		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.SkippingBaseClass(bool)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.SkippingBaseClass(bool skipBaseClass)
		            		{
		            			SkippingBaseClass(skipBaseClass);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.InitializeWith(TValue)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.InitializeWith(TValue value)
		            		{
		            			InitializeWith(value);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.InitializeWith(global::System.Func{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.InitializeWith(global::System.Func<{{tp}}, TValue> valueGenerator)
		            		{
		            			InitializeWith(valueGenerator);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.OnGet" />
		            		IIndexerGetterOnlyGetterSetup<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.OnGet
		            			=> this;

		            		/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, {{tp}}}.Do(global::System.Action)" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlyGetterSetup<TValue, {{tp}}>.Do(global::System.Action callback)
		            		{
		            			((IIndexerGetterSetup<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, {{tp}}}.Do(global::System.Action{{{tp}}})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlyGetterSetup<TValue, {{tp}}>.Do(global::System.Action<{{tp}}> callback)
		            		{
		            			((IIndexerGetterSetupWithCallback<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, {{tp}}}.Do(global::System.Action{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlyGetterSetup<TValue, {{tp}}>.Do(global::System.Action<{{tp}}, TValue> callback)
		            		{
		            			((IIndexerGetterSetupWithCallback<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, {{tp}}}.Do(global::System.Action{int, {{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlyGetterSetup<TValue, {{tp}}>.Do(global::System.Action<int, {{tp}}, TValue> callback)
		            		{
		            			((IIndexerGetterSetupWithCallback<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlyGetterSetup{TValue, {{tp}}}.TransitionTo(string)" />
		            		IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlyGetterSetup<TValue, {{tp}}>.TransitionTo(string scenario)
		            		{
		            			((IIndexerGetterSetup<TValue, {{tp}}>)this).TransitionTo(scenario);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackBuilder{TValue, {{tp}}}.InParallel()" />
		            		IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> IIndexerGetterOnlySetupCallbackBuilder<TValue, {{tp}}>.InParallel()
		            		{
		            			((IIndexerGetterSetupCallbackBuilder<TValue, {{tp}}>)this).InParallel();
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupParallelCallbackBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> IIndexerGetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}>.When(global::System.Func<int, bool> predicate)
		            		{
		            			((IIndexerGetterSetupParallelCallbackBuilder<TValue, {{tp}}>)this).When(predicate);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>.For(int times)
		            		{
		            			((IIndexerGetterSetupCallbackWhenBuilder<TValue, {{tp}}>)this).For(times);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupCallbackWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>.Only(int times)
		            		{
		            			((IIndexerGetterSetupCallbackWhenBuilder<TValue, {{tp}}>)this).Only(times);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Returns(TValue)" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Returns(TValue returnValue)
		            		{
		            			Returns(returnValue);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Returns(global::System.Func{TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Returns(global::System.Func<TValue> callback)
		            		{
		            			Returns(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Returns(global::System.Func{{{tp}}, TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Returns(global::System.Func<{{tp}}, TValue> callback)
		            		{
		            			Returns(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Returns(global::System.Func{{{tp}}, TValue, TValue})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Returns(global::System.Func<{{tp}}, TValue, TValue> callback)
		            		{
		            			Returns(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Throws{TException}()" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Throws<TException>()
		            		{
		            			Throws<TException>();
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Throws(global::System.Exception)" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Throws(global::System.Exception exception)
		            		{
		            			Throws(exception);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Throws(global::System.Func{global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Throws(global::System.Func<global::System.Exception> callback)
		            		{
		            			Throws(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Throws(global::System.Func{{{tp}}, global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Throws(global::System.Func<{{tp}}, global::System.Exception> callback)
		            		{
		            			Throws(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetup{TValue, {{tp}}}.Throws(global::System.Func{{{tp}}, TValue, global::System.Exception})" />
		            		IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}> IIndexerGetterOnlySetup<TValue, {{tp}}>.Throws(global::System.Func<{{tp}}, TValue, global::System.Exception> callback)
		            		{
		            			Throws(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupReturnBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}> IIndexerGetterOnlySetupReturnBuilder<TValue, {{tp}}>.When(global::System.Func<int, bool> predicate)
		            		{
		            			((IIndexerSetupReturnBuilder<TValue, {{tp}}>)this).When(predicate);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}>.For(int times)
		            		{
		            			((IIndexerSetupReturnWhenBuilder<TValue, {{tp}}>)this).For(times);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerGetterOnlySetupReturnWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerGetterOnlySetup<TValue, {{tp}}> IIndexerGetterOnlySetupReturnWhenBuilder<TValue, {{tp}}>.Only(int times)
		            		{
		            			((IIndexerSetupReturnWhenBuilder<TValue, {{tp}}>)this).Only(times);
		            			return this;
		            		}

		            """).AppendLine();
	}

	private static void AppendSetterOnlyIndexerImplementation(StringBuilder sb, int numberOfParameters)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);

		sb.Append($$"""
		            		/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, {{tp}}}.SkippingBaseClass(bool)" />
		            		IIndexerSetterOnlySetup<TValue, {{tp}}> IIndexerSetterOnlySetup<TValue, {{tp}}>.SkippingBaseClass(bool skipBaseClass)
		            		{
		            			SkippingBaseClass(skipBaseClass);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetup{TValue, {{tp}}}.OnSet" />
		            		IIndexerSetterOnlySetterSetup<TValue, {{tp}}> IIndexerSetterOnlySetup<TValue, {{tp}}>.OnSet
		            			=> this;

		            		/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, {{tp}}}.Do(global::System.Action)" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetterSetup<TValue, {{tp}}>.Do(global::System.Action callback)
		            		{
		            			((IIndexerSetterSetup<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, {{tp}}}.Do(global::System.Action{TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetterSetup<TValue, {{tp}}>.Do(global::System.Action<TValue> callback)
		            		{
		            			((IIndexerSetterSetup<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, {{tp}}}.Do(global::System.Action{{{tp}}, TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetterSetup<TValue, {{tp}}>.Do(global::System.Action<{{tp}}, TValue> callback)
		            		{
		            			((IIndexerSetterSetupWithCallback<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, {{tp}}}.Do(global::System.Action{int, {{tp}}, TValue})" />
		            		IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetterSetup<TValue, {{tp}}>.Do(global::System.Action<int, {{tp}}, TValue> callback)
		            		{
		            			((IIndexerSetterSetupWithCallback<TValue, {{tp}}>)this).Do(callback);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetterSetup{TValue, {{tp}}}.TransitionTo(string)" />
		            		IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetterSetup<TValue, {{tp}}>.TransitionTo(string scenario)
		            		{
		            			((IIndexerSetterSetup<TValue, {{tp}}>)this).TransitionTo(scenario);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackBuilder{TValue, {{tp}}}.InParallel()" />
		            		IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}> IIndexerSetterOnlySetupCallbackBuilder<TValue, {{tp}}>.InParallel()
		            		{
		            			((IIndexerSetterSetupCallbackBuilder<TValue, {{tp}}>)this).InParallel();
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetupParallelCallbackBuilder{TValue, {{tp}}}.When(global::System.Func{int, bool})" />
		            		IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> IIndexerSetterOnlySetupParallelCallbackBuilder<TValue, {{tp}}>.When(global::System.Func<int, bool> predicate)
		            		{
		            			((IIndexerSetterSetupParallelCallbackBuilder<TValue, {{tp}}>)this).When(predicate);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, {{tp}}}.For(int)" />
		            		IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>.For(int times)
		            		{
		            			((IIndexerSetterSetupCallbackWhenBuilder<TValue, {{tp}}>)this).For(times);
		            			return this;
		            		}

		            		/// <inheritdoc cref="IIndexerSetterOnlySetupCallbackWhenBuilder{TValue, {{tp}}}.Only(int)" />
		            		IIndexerSetterOnlySetup<TValue, {{tp}}> IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, {{tp}}>.Only(int times)
		            		{
		            			((IIndexerSetterSetupCallbackWhenBuilder<TValue, {{tp}}>)this).Only(times);
		            			return this;
		            		}

		            """).AppendLine();
	}

	private static void AppendIndexerVerifyGetterResult(StringBuilder sb, int numberOfParameters)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);
		string description = GetTypeParametersDescription(numberOfParameters);

		sb.Append($$"""
		            	/// <summary>
		            	///     Verifications on a {{numberOfParameters}}-key indexer for {{description}} that the mock only reads.
		            	/// </summary>
		            	/// <remarks>
		            	///     Used instead of <see cref="global::Mockolate.Verify.VerificationIndexerResult{TSubject, TParameter}" /> when the
		            	///     mock has no setter to intercept. Writes then never reach the mock, so offering <c>Set(...)</c> here would
		            	///     always report zero interactions.
		            	/// </remarks>
		            """).AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append($$"""
		            	internal class VerificationIndexerGetterResult<TSubject, {{tp}}>(
		            		TSubject subject,
		            		global::Mockolate.Internals.MockRegistry mockRegistry,
		            		int getMemberId,
		            		global::System.Func<global::Mockolate.Interactions.IInteraction, bool> gotPredicate,
		            		global::System.Func<string> parametersDescription)
		            	{
		            		/// <inheritdoc cref="global::Mockolate.Verify.VerificationIndexerResult{TSubject, TParameter}.Got()" />
		            		public global::Mockolate.Verify.VerificationResult<TSubject> Got()
		            			=> mockRegistry.IndexerGot(subject, getMemberId, gotPredicate, parametersDescription);
		            	}
		            """).AppendLine();
	}

	private static void AppendIndexerVerifySetterResult(StringBuilder sb, int numberOfParameters,
		bool hasOverloadResolutionPriority)
	{
		string tp = GetGenericTypeParameters(numberOfParameters);
		string description = GetTypeParametersDescription(numberOfParameters);

		sb.Append($$"""
		            	/// <summary>
		            	///     Verifications on a {{numberOfParameters}}-key indexer of type <typeparamref name="TParameter" /> for {{description}} that the mock only writes.
		            	/// </summary>
		            	/// <remarks>
		            	///     Used instead of <see cref="global::Mockolate.Verify.VerificationIndexerResult{TSubject, TParameter}" /> when the
		            	///     mock has no getter to intercept, so <c>Got()</c> is not offered.
		            	/// </remarks>
		            """).AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append($$"""
		            	internal class VerificationIndexerSetterResult<TSubject, {{tp}}, TParameter>(
		            		TSubject subject,
		            		global::Mockolate.Internals.MockRegistry mockRegistry,
		            		int setMemberId,
		            		global::System.Func<global::Mockolate.Interactions.IInteraction, global::Mockolate.Parameters.IParameterMatch<TParameter>, bool> setPredicate,
		            		global::System.Func<string> parametersDescription)
		            	{
		            		/// <inheritdoc cref="global::Mockolate.Verify.VerificationIndexerResult{TSubject, TParameter}.Set(global::Mockolate.Parameters.IParameter{TParameter})" />
		            		public global::Mockolate.Verify.VerificationResult<TSubject> Set(global::Mockolate.Parameters.IParameter<TParameter> value)
		            			=> mockRegistry.IndexerSet(subject, setMemberId, setPredicate,
		            				(global::Mockolate.Parameters.IParameterMatch<TParameter>)value, parametersDescription);

		            """).AppendLine();
		if (hasOverloadResolutionPriority)
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(1)]").AppendLine();
		}

		// Matchers.IsValue instead of It.Is: this file is emitted into the consumer's compilation, which
		// cannot see the internal CallerArgumentExpression polyfill the shipped result relies on.
		// IsValue formats the value lazily and invariantly, so a literal renders exactly as it does
		// on the shipped surface, and no description is built on the passing path.
		sb.Append($$"""
		            		/// <summary>
		            		///     Verifies the indexer write access on the mock with the given <paramref name="value" />.
		            		/// </summary>
		            		public global::Mockolate.Verify.VerificationResult<TSubject> Set(TParameter value)
		            			=> mockRegistry.IndexerSet(subject, setMemberId, setPredicate,
		            				(global::Mockolate.Parameters.IParameterMatch<TParameter>)global::Mockolate.Internals.Matchers.IsValue<TParameter>(value), parametersDescription);
		            	}
		            """).AppendLine();
	}
}
