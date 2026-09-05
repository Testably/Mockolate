using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	public static string MockDelegate(string name, MockClass @class, Method delegateMethod,
		bool useUnionOverloads = false)
	{
		string mockRegistryName = @class.GetUniqueName("MockRegistry", "MockolateMockRegistry");
		string escapedClassName = @class.ClassFullName.EscapeForXmlDoc();
		MemberIdTable memberIds = new(new MemberAliases());
		memberIds.AddMethod(delegateMethod);
		string memberIdPrefix = $"global::Mockolate.Mock.{name}.";
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable annotations").AppendLine();
		sb.Append("namespace Mockolate;").AppendLine();
		sb.AppendLine();

		#region Mock

		sb.Append("internal static partial class Mock").AppendLine();
		sb.Append("{").AppendLine();
		
		#region MockForXXX

		sb.Append("\t/// <summary>").AppendLine();
		sb.Append("\t///     A mock implementation for <see cref=\"").Append(escapedClassName).Append("\" />.").AppendLine();
		sb.Append("\t/// </summary>").AppendLine();
		sb.Append("\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tinternal class ").Append(name).Append(" :").AppendLine();
		sb.Append("\t\tIMockFor").Append(name).Append(',').AppendLine();
		sb.Append("\t\tglobal::Mockolate.IMock").AppendLine();
		sb.Append("\t{").AppendLine();
		memberIds.Emit(sb, "\t\t");
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tglobal::Mockolate.MockRegistry global::Mockolate.IMock.MockRegistry => this.").Append(mockRegistryName).Append(';').AppendLine();
		sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
		sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.MockRegistry mockRegistry)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Returns the actual delegate with the mock as target.");
		sb.Append("\t\tpublic ").Append(@class.ClassFullName).Append(" Object => new(Invoke);").AppendLine();
		sb.Append("\t\tprivate ").Append(delegateMethod.ReturnType == Type.Void ? "void" : delegateMethod.ReturnType.Fullname).Append(" Invoke(")
			.Append(string.Join(", ", delegateMethod.Parameters.Select(p => $"{p.RefKind.GetString()}{p.Type.Fullname} {p.Name}"))).Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		string methodSetup = Helpers.GetUniqueLocalVariableName("methodSetup", delegateMethod.Parameters);
		string methodSetupType = (delegateMethod.ReturnType == Type.Void, delegateMethod.Parameters.Count) switch
		{
			(true, 0) => "global::Mockolate.Setup.VoidMethodSetup",
			(true, _) => $"global::Mockolate.Setup.VoidMethodSetup<{string.Join(", ", delegateMethod.Parameters.Select(p => p.ToTypeOrWrapper()))}>",
			(_, 0) => $"global::Mockolate.Setup.ReturnMethodSetup<{delegateMethod.ReturnType.ToTypeOrWrapper()}>",
			(_, _) => $"global::Mockolate.Setup.ReturnMethodSetup<{delegateMethod.ReturnType.ToTypeOrWrapper()}, {string.Join(", ", delegateMethod.Parameters.Select(p => p.ToTypeOrWrapper()))}>",
		};
		bool hasOutParams = delegateMethod.Parameters.Any(p => p.RefKind is RefKind.Out);
		bool hasRefParams = delegateMethod.Parameters.Any(p => p.RefKind is RefKind.Ref);
		string wpc = Helpers.GetUniqueLocalVariableName("wpc", delegateMethod.Parameters);

		StringBuilder sb2 = new();
		int i = 0;
		foreach (MethodParameter p in delegateMethod.Parameters)
		{
			if (i++ > 0)
			{
				sb2.Append(", ");
			}

			if (p.RefKind == RefKind.Ref)
			{
				string paramRef = Helpers.GetUniqueLocalVariableName($"ref_{p.Name}", delegateMethod.Parameters);

				sb.Append("\t\t\tvar ").Append(paramRef).Append(" = ").Append(p.Name).Append(';').AppendLine();
				sb2.Append(paramRef);
			}
			else if (p.Type.SpecialGenericType == SpecialGenericType.Span ||
			         p.Type.SpecialGenericType == SpecialGenericType.ReadOnlySpan)
			{
				string paramRef = Helpers.GetUniqueLocalVariableName($"ref_{p.Name}", delegateMethod.Parameters);

				sb.Append("\t\t\tvar ").Append(paramRef).Append(" = ").Append(p.ToNameOrWrapper()).Append(';').AppendLine();
				sb2.Append(paramRef);
			}
			else
			{
				sb2.Append(p.RefKind switch
					{
						RefKind.Out => "default",
						_ => p.ToNameOrWrapper(),
					});
			}
		}

		string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(delegateMethod);
		bool isGeneric = delegateMethod.GenericParameters is not null &&
		                 delegateMethod.GenericParameters.Value.Count > 0;
		EmitFastMethodSetupLookup(sb, "\t\t\t", $"this.{mockRegistryName}", methodSetup, methodSetupType,
			memberIdRef, delegateMethod.GetUniqueNameString(), sb2.ToString(), isGeneric);

		if (hasOutParams)
		{
			foreach (MethodParameter parameter in delegateMethod.Parameters.Where(p => p.RefKind == RefKind.Out))
			{
				sb.Append("\t\t\t").Append(parameter.Name).Append(" = default!;").AppendLine();
			}
		}

		if (hasOutParams || hasRefParams)
		{
			string outParamBase = Helpers.GetUniqueIndexedLocalVariableBase("outParam", delegateMethod.Parameters);
			string refParamBase = Helpers.GetUniqueIndexedLocalVariableBase("refParam", delegateMethod.Parameters);
			sb.Append("\t\t\tif (").Append(methodSetup).Append(" is ").Append(methodSetupType).Append(".WithParameterCollection ").Append(wpc).Append(')').AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			int parameterIndex = 0;
			foreach (MethodParameter parameter in delegateMethod.Parameters)
			{
				parameterIndex++;
				if (parameter.RefKind == RefKind.Out)
				{
					sb.Append("\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
						.Append(" is not global::Mockolate.Parameters.IOutParameter<")
						.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(outParamBase)
						.Append(parameterIndex)
						.Append(" || !").Append(outParamBase).Append(parameterIndex).Append(".TryGetValue(out ")
						.Append(parameter.Name).Append("))").AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t").Append(parameter.Name).Append(" = ").AppendDefaultValueGeneratorFor(parameter.Type, $"this.{mockRegistryName}.Behavior.DefaultValue").Append(';').AppendLine();
					sb.Append("\t\t\t\t}").AppendLine();
				}
				else if (parameter.RefKind == RefKind.Ref)
				{
					sb.Append("\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex).Append(" is global::Mockolate.Parameters.IRefParameter<").Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(refParamBase).Append(parameterIndex).Append(")").AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t").Append(parameter.Name).Append(" = ").Append(refParamBase).Append(parameterIndex).Append(".GetValue(").Append(parameter.Name).Append(");").AppendLine();
					sb.Append("\t\t\t\t}").AppendLine();
				}
			}

			sb.Append("\t\t\t}").AppendLine();
		}

		sb.Append("\t\t\tif (").Append(mockRegistryName).Append(".Behavior.SkipInteractionRecording == false)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t").Append(mockRegistryName).Append(".RegisterInteraction(new global::Mockolate.Interactions.MethodInvocation");
		if (delegateMethod.Parameters.Count > 0)
		{
			sb.Append('<').Append(string.Join(", ", delegateMethod.Parameters.Select(p => p.ToTypeOrWrapper()))).Append('>');
		}

		sb.Append("(").Append(delegateMethod.GetUniqueNameString());
		if (delegateMethod.Parameters.Count > 0)
		{
			sb.Append(", ").Append(string.Join(", ", delegateMethod.Parameters.Select(p => p.ToNameOrWrapper())));
		}

		sb.Append("));").AppendLine();
		sb.Append("\t\t\t}").AppendLine();

		string displayDelegateName =
			$"{delegateMethod.ContainingType}.{delegateMethod.Name}({string.Join(", ", delegateMethod.Parameters.Select(p => p.Type.DisplayName))})";
		sb.Append("\t\t\tif (").Append(methodSetup).Append(" is null && this.").Append(mockRegistryName).Append(".Behavior.ThrowWhenNotSetup)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockNotSetupException(\"The method '").Append(displayDelegateName).Append("' was invoked without prior setup.\");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();

		AppendTriggerCallbacks(sb, "\t\t\t", methodSetup, delegateMethod.Parameters);

		if (delegateMethod.ReturnType != Type.Void)
		{
			string returnValue = Helpers.GetUniqueLocalVariableName("returnValue", delegateMethod.Parameters);
			sb.Append("\t\t\treturn ").Append(methodSetup).Append("?.TryGetReturnValue(");
			if (delegateMethod.Parameters.Count > 0)
			{
				sb.Append(string.Join(", ", delegateMethod.Parameters.Select(p => p.ToNameOrWrapper()))).Append(", ");
			}

			sb.Append("out var ").Append(returnValue).Append(") == true ? ").Append(returnValue).Append(" : ")
				.AppendDefaultValueGeneratorFor(delegateMethod.ReturnType, $"this.{mockRegistryName}.Behavior.DefaultValue")
				.Append(';').AppendLine();
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.IMock.ToString()").AppendLine();
		sb.Append("\t\t\t=> \"").Append(@class.DisplayString).Append(" mock\";").AppendLine();
		sb.AppendLine();

		if (UseUnionOverloads(delegateMethod, true, useUnionOverloads))
		{
			AppendMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", true, memberIds, memberIdPrefix, "Setup");
			foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(delegateMethod.Parameters))
			{
				AppendUnionMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", memberIds, memberIdPrefix, slots, "Setup");
			}
		}
		else
		{
			AppendMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", false, memberIds, memberIdPrefix, "Setup");

			if (delegateMethod.Parameters.Count > 0)
			{
				AppendMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", true, memberIds, memberIdPrefix, "Setup");
			}

			if (delegateMethod.Parameters.Count is > 0 and <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(delegateMethod.Parameters))
				{
					AppendMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", false, memberIds, memberIdPrefix, "Setup", valueFlags);
				}
			}
			else if (delegateMethod.Parameters.Count > MaxExplicitParameters)
			{
				bool[] allValueFlags = delegateMethod.Parameters.Select(p => p.CanUseNullableParameterOverload()).ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendMethodSetupImplementation(sb, delegateMethod, mockRegistryName, $"IMockSetupFor{name}", false, memberIds, memberIdPrefix, "Setup", allValueFlags);
				}
			}
		}

		// Delegate mocks use a plain MockRegistry (no FastMockInteractions), so emit the slow Verify path.
		if (UseUnionOverloads(delegateMethod, true, useUnionOverloads))
		{
			AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", true, memberIds, memberIdPrefix, false, "Verify");
			foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(delegateMethod.Parameters))
			{
				AppendUnionMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", memberIds, memberIdPrefix, false, slots, "Verify");
			}
		}
		else
		{
			AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false, memberIds, memberIdPrefix, false, "Verify");
			if (delegateMethod.Parameters.Count > 0)
			{
				AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", true, memberIds, memberIdPrefix, false, "Verify");
			}

			if (delegateMethod.Parameters.Count is > 0 and <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(delegateMethod.Parameters))
				{
					AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false, memberIds, memberIdPrefix, false, "Verify", valueFlags);
				}
			}
			else if (delegateMethod.Parameters.Count > MaxExplicitParameters)
			{
				bool[] allValueFlags = delegateMethod.Parameters.Select(p => p.CanUseNullableParameterOverload()).ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false,
						memberIds, memberIdPrefix, false, "Verify", allValueFlags);
				}
			}
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> IMockFor").Append(name).Append(".VerifySetup(global::Mockolate.Setup.IMethodSetup setup)").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".Method<IMockVerifyFor").Append(name).Append(">(this, setup);").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tbool IMockFor").Append(name).Append(".VerifyThatAllInteractionsAreVerified()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".Interactions.GetUnverifiedInteractions().Count == 0;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tbool IMockFor").Append(name).Append(".VerifyThatAllSetupsAreUsed()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".GetUnusedSetups(this.").Append(mockRegistryName).Append(".Interactions).Count == 0;").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tvoid IMockFor").Append(name).Append(".ClearAllInteractions()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".ClearAllInteractions();").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append("> IMockFor").Append(name).Append(".Monitor()").AppendLine();
		sb.Append("\t\t\t=> new global::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append(">(this.")
			.Append(mockRegistryName).Append(".Interactions, interactions => new VerifyMonitor").Append(name)
			.Append("(new global::Mockolate.MockRegistry(this.").Append(mockRegistryName).Append(", interactions)));").AppendLine();
		sb.AppendLine("\t}");
		sb.AppendLine();
		
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tprivate sealed class VerifyMonitor").Append(name).Append("(global::Mockolate.MockRegistry mockRegistry) : global::Mockolate.Mock.IMockVerifyFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; } = mockRegistry;").AppendLine();
		sb.AppendLine();
		
		sb.Append("\t\t#region IMockVerifyFor").Append(name).AppendLine();
		sb.AppendLine();
		
		// Delegate mocks use a plain MockRegistry (no FastMockInteractions), so emit the slow Verify path.
		if (UseUnionOverloads(delegateMethod, true, useUnionOverloads))
		{
			AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", true, memberIds, memberIdPrefix, false, "Verify");
			foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(delegateMethod.Parameters))
			{
				AppendUnionMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", memberIds, memberIdPrefix, false, slots, "Verify");
			}
		}
		else
		{
			AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false, memberIds, memberIdPrefix, false, "Verify");

			if (delegateMethod.Parameters.Count > 0)
			{
				AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", true, memberIds, memberIdPrefix, false, "Verify");
			}

			if (delegateMethod.Parameters.Count is > 0 and <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(delegateMethod.Parameters))
				{
					AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false, memberIds, memberIdPrefix, false, "Verify", valueFlags);
				}
			}
			else if (delegateMethod.Parameters.Count > MaxExplicitParameters)
			{
				bool[] allValueFlags = delegateMethod.Parameters.Select(p => p.CanUseNullableParameterOverload()).ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendMethodVerifyImplementation(sb, delegateMethod, mockRegistryName, $"IMockVerifyFor{name}", false, memberIds, memberIdPrefix, false, "Verify", allValueFlags);
				}
			}
		}

		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion MockForXXX

		#region Mock Interfaces

		#region IMockForXXX

		sb.AppendXmlSummary($"Accesses the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockFor").Append(name).Append(" :").AppendLine();
		sb.Append("\t\t IMockSetupFor").Append(name).Append(", IMockVerifyFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary("Verifies the method invocations for the <paramref name=\"setup\" /> on the mock.");
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> VerifySetup(global::Mockolate.Setup.IMethodSetup setup);").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Gets a value indicating whether all expected interactions have been verified.");
		sb.Append("\t\tbool VerifyThatAllInteractionsAreVerified();").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Gets a value indicating whether all registered setups were used.");
		sb.Append("\t\tbool VerifyThatAllSetupsAreUsed();").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Clears all interactions recorded by the mock object.");
		sb.Append("\t\tvoid ClearAllInteractions();").AppendLine();
		sb.AppendLine();
		sb.AppendXmlSummary("Provides monitoring capabilities for a mocked instance of the specified type, allowing inspection of accessed properties, invoked methods, and event subscriptions.");
		sb.Append("\t\tglobal::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append("> Monitor();").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockForXXX

		#region IMockSetupForXXX

		sb.AppendXmlSummary($"Set up the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockSetupFor").Append(name).Append(" : global::Mockolate.Setup.IMockSetup<").Append(@class.ClassFullName).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		
		if (UseUnionOverloads(delegateMethod, true, useUnionOverloads))
		{
			// Without the priority (guaranteed in union mode) the IParameters overload loses to the union overloads.
			AppendMethodSetupDefinition(sb, @class, delegateMethod, true, "Setup",
				hasOverloadResolutionPriority: true);
			foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(delegateMethod.Parameters))
			{
				AppendUnionMethodSetupDefinition(sb, @class, delegateMethod, slots, "Setup");
			}
		}
		else
		{
			AppendMethodSetupDefinition(sb, @class, delegateMethod, false, "Setup");

			if (delegateMethod.Parameters.Count > 0)
			{
				AppendMethodSetupDefinition(sb, @class, delegateMethod, true, "Setup");
			}

			if (delegateMethod.Parameters.Count is > 0 and <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(delegateMethod.Parameters))
				{
					AppendMethodSetupDefinition(sb, @class, delegateMethod, false, "Setup", valueFlags);
				}
			}
			else if (delegateMethod.Parameters.Count > MaxExplicitParameters)
			{
				bool[] allValueFlags = delegateMethod.Parameters.Select(p => p.CanUseNullableParameterOverload()).ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendMethodSetupDefinition(sb, @class, delegateMethod, false, "Setup", allValueFlags);
				}
			}
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockSetupForXXX

		#region IMockVerifyForXXX

		sb.AppendXmlSummary($"Verify interactions with the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockVerifyFor").Append(name).Append(" : global::Mockolate.Verify.IMockVerify<").Append(@class.ClassFullName).Append(">").AppendLine();
		sb.Append("\t{").AppendLine();
		
		if (UseUnionOverloads(delegateMethod, true, useUnionOverloads))
		{
			// Without the priority (guaranteed in union mode) the IParameters overload loses to the union overloads.
			AppendMethodVerifyDefinition(sb, delegateMethod, $"IMockVerifyFor{name}", true, "Verify",
				hasOverloadResolutionPriority: true);
			foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(delegateMethod.Parameters))
			{
				AppendUnionMethodVerifyDefinition(sb, @class, delegateMethod, $"IMockVerifyFor{name}", slots, "Verify");
			}
		}
		else
		{
			AppendMethodVerifyDefinition(sb, delegateMethod, $"IMockVerifyFor{name}", false, "Verify");

			if (delegateMethod.Parameters.Count > 0)
			{
				AppendMethodVerifyDefinition(sb, delegateMethod, $"IMockVerifyFor{name}", true, "Verify");
			}

			if (delegateMethod.Parameters.Count is > 0 and <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(delegateMethod.Parameters))
				{
					AppendMethodVerifyDefinition(sb, delegateMethod, $"IMockVerifyFor{name}", false, "Verify", valueFlags);
				}
			}
			else if (delegateMethod.Parameters.Count > MaxExplicitParameters)
			{
				bool[] allValueFlags = delegateMethod.Parameters.Select(p => p.CanUseNullableParameterOverload()).ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendMethodVerifyDefinition(sb, delegateMethod, $"IMockVerifyFor{name}", false, "Verify",
						allValueFlags);
				}
			}
		}

		sb.Append("\t}").AppendLine();

		#endregion IMockVerifyForXXX

		#endregion Mock Interfaces

		sb.Append("}").AppendLine();
		sb.AppendLine();
		
		#endregion Mock

		#region MockForXXXExtensions

		sb.AppendXmlSummary($"Mock extensions for <see cref=\"{escapedClassName}\" />.", "");
#if !DEBUG
		sb.Append("[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("internal static partial class MockExtensionsFor").Append(name).AppendLine();
		sb.Append("{").AppendLine();

		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(").Append(@class.ClassFullName).Append(" mock)").AppendLine();
		sb.Append("\t{").AppendLine();

		#region Mock Property

		sb.AppendXmlSummary($"Get access to the mock of <see cref=\"{escapedClassName}\" />.");
		sb.Append("\t\tpublic global::Mockolate.Mock.IMockFor").Append(name).Append(" Mock").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tif (mock.Target is global::Mockolate.Mock.IMockFor").Append(name).Append(" mockInterface)")
			.AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\treturn mockInterface;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The subject is no mock.\");")
			.AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		#endregion Mock Property

		#region CreateMock

		sb.AppendXmlSummary(
			$"Create a new mock of <see cref=\"{escapedClassName}\" /> with the default <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock()").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, []);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary(
			$"Create a new mock of <see cref=\"{escapedClassName}\" /> with the default <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlRemarks("All provided <paramref name=\"setups\" /> are immediately applied to the mock.");
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName)
			.Append(" CreateMock(params global::System.Action<global::Mockolate.Mock.IMockSetupFor").Append(name)
			.Append(">[] setups)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, setups);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary(
			$"Create a new mock of <see cref=\"{escapedClassName}\" /> with the given <paramref name=\"mockBehavior\" />.");
		sb.AppendXmlRemarks("All provided <paramref name=\"setups\" /> are immediately applied to the mock.");
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName)
			.Append(
				" CreateMock(global::Mockolate.MockBehavior? mockBehavior = null, params global::System.Action<global::Mockolate.Mock.IMockSetupFor")
			.Append(name).Append(">[] setups)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		sb.Append(
				"\t\t\tvar mockRegistry = new global::Mockolate.MockRegistry(mockBehavior, new global::Mockolate.Interactions.FastMockInteractions(0, mockBehavior.SkipInteractionRecording));")
			.AppendLine();
		sb.Append("\t\t\tglobal::Mockolate.Mock.").Append(name).Append(" mockTarget = new global::Mockolate.Mock.")
			.Append(name).Append("(mockRegistry);")
			.AppendLine();
		sb.Append("\t\t\tif (setups.Length > 0)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tforeach (var setup in setups)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tsetup.Invoke(mockTarget);").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\treturn mockTarget.Object;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t}").AppendLine();
		sb.Append("}").AppendLine();

		#endregion MockForXXXExtensions

		#endregion MockForXXXExtensions
		
		sb.AppendLine("#nullable disable annotations");
		return sb.ToString();
	}
}
