using System.Text;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	public static string MockCombinationClass(
		string fileName,
		string name,
		Class @class,
		(string Name, Class Class)[] additionalInterfaces,
		bool useUnionOverloads = false)
	{
		EquatableArray<Method>? constructors = (@class as MockClass)?.Constructors;
		MemberAliases aliases = new();
		if (@class is MockClass { HasImplementedMembers: true, } implementor)
		{
			additionalInterfaces = additionalInterfaces
				.Select(additional => (additional.Name, additional.Class.RebaseOnto(implementor, aliases)))
				.ToArray();
		}

		string escapedClassName = @class.ClassFullName.EscapeForXmlDoc();
		bool hasEvents = @class.AllEvents().Any(x => !x.IsStatic);
		bool hasStaticEvents = @class.IsInterface && @class.AllEvents().Any(x => x.IsStatic);
		bool hasStaticMembers = @class.IsInterface &&
		                        (@class.AllMethods().Any(x => x.IsStatic) ||
		                         @class.AllProperties().Any(x => x.IsStatic));
		bool hasAnyStaticMembers = hasStaticMembers ||
		                           additionalInterfaces.Any(ai =>
			                           ai.Class.AllMethods().Any(x => x.IsStatic) ||
			                           ai.Class.AllProperties().Any(x => x.IsStatic));
		bool hasAnyStaticEvents = hasStaticEvents ||
		                          additionalInterfaces.Any(ai => ai.Class.AllEvents().Any(x => x.IsStatic));
		bool hasProtectedEvents = !@class.IsInterface && @class.AllEvents().Any(@event => @event.IsProtected);
		bool hasProtectedMembers = !@class.IsInterface &&
		                           (@class.AllMethods().Any(method => method.IsProtected)
		                            || @class.AllProperties().Any(property => property.IsProtected));
		string mockRegistryName = @class.GetUniqueName("MockRegistry", "MockolateMockRegistry");
		Class[] allMemberClasses = new Class[1 + additionalInterfaces.Length];
		allMemberClasses[0] = @class;
		for (int memberClassIndex = 0; memberClassIndex < additionalInterfaces.Length; memberClassIndex++)
		{
			allMemberClasses[memberClassIndex + 1] = additionalInterfaces[memberClassIndex].Class;
		}

		MemberIdTable memberIds = ComputeMemberIds(aliases, allMemberClasses);
		string memberIdPrefix = $"global::Mockolate.Mock.{fileName}.";
		StringBuilder sb = InitializeBuilder();

		sb.Append("#nullable enable annotations").AppendLine();
		sb.Append("namespace Mockolate;").AppendLine();
		sb.AppendLine();

		#region Mock

		sb.Append("internal static partial class Mock").AppendLine();
		sb.Append("{").AppendLine();
		
		#region MockForXXX
		
		sb.AppendXmlSummary($"A mock implementation for <see cref=\"{escapedClassName}\" /> that also implements<br />\n\t///      - {string.Join("<br />\n\t///      - ", additionalInterfaces.Select(x => $"<see cref=\"{x.Class.ClassFullName.EscapeForXmlDoc()}\" />"))}.", "\t");
		sb.Append("\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tinternal class ").Append(fileName).Append(" :").AppendLine();
		sb.Append("\t\t").Append(@class.ClassFullName).Append(", ").Append("IMockFor").Append(name).Append(", IMockSetupFor").Append(name);
		if (hasProtectedMembers)
		{
			sb.Append(", IMockProtectedSetupFor").Append(name);
			sb.Append(", global::Mockolate.MockExtensionsFor").Append(name).Append(".IMockSetupInitializationFor").Append(name);
		}

		if (hasStaticMembers)
		{
			sb.Append(", IMockStaticSetupFor").Append(name);
		}

		if (hasEvents)
		{
			sb.Append(", IMockRaiseOn").Append(name);
		}

		if (hasProtectedEvents)
		{
			sb.Append(", IMockProtectedRaiseOn").Append(name);
		}

		if (hasStaticEvents)
		{
			sb.Append(", IMockStaticRaiseOn").Append(name);
		}

		sb.Append(", IMockVerifyFor").Append(name);
		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.Append(", IMockProtectedVerifyFor").Append(name);
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.Append(", IMockStaticVerifyFor").Append(name);
		}

		sb.Append(',').AppendLine();
		foreach ((string Name, Class Class) additional in additionalInterfaces)
		{
			bool additionalHasStaticMembers = additional.Class.AllMethods().Any(x => x.IsStatic) ||
			                                  additional.Class.AllProperties().Any(x => x.IsStatic);
			bool additionalHasStaticEvents = additional.Class.AllEvents().Any(x => x.IsStatic);
			sb.Append("\t\t").Append(additional.Class.ClassFullName).Append(", ").Append("IMockFor").Append(additional.Name).Append(", IMockSetupFor").Append(additional.Name);
			if (additionalHasStaticMembers)
			{
				sb.Append(", IMockStaticSetupFor").Append(additional.Name);
			}

			if (additional.Class.AllEvents().Any(x => !x.IsStatic))
			{
				sb.Append(", IMockRaiseOn").Append(additional.Name);
			}

			if (additionalHasStaticEvents)
			{
				sb.Append(", IMockStaticRaiseOn").Append(additional.Name);
			}

			sb.Append(", IMockVerifyFor").Append(additional.Name);
			if (additionalHasStaticMembers || additionalHasStaticEvents)
			{
				sb.Append(", IMockStaticVerifyFor").Append(additional.Name);
			}

			sb.Append(",").AppendLine();
		}

		sb.Append("\t\tglobal::Mockolate.IMock").AppendLine();
		sb.Append("\t{").AppendLine();

		memberIds.Emit(sb, "\t\t");
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tglobal::Mockolate.MockRegistry global::Mockolate.IMock.MockRegistry => this.").Append(mockRegistryName).Append(";").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
		if (hasAnyStaticMembers || hasAnyStaticEvents)
		{
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tinternal static readonly global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry> MockRegistryProvider = new global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry>();").AppendLine();
		}

		sb.AppendLine();

		ImplementMockForInterface(sb, mockRegistryName, name, hasEvents, hasProtectedMembers, hasProtectedEvents, hasStaticMembers, hasStaticEvents);
		foreach ((string additionalInterfaceName, Class additionalInterface) in additionalInterfaces)
		{
			ImplementMockForInterface(sb, mockRegistryName, additionalInterfaceName,
				additionalInterface.AllEvents().Any(x => !x.IsStatic),
				false /* Interfaces cannot have protected members */,
				false /* Interfaces cannot have protected events */,
				additionalInterface.AllMethods().Any(x => x.IsStatic) || additionalInterface.AllProperties().Any(x => x.IsStatic),
				additionalInterface.AllEvents().Any(x => x.IsStatic));
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.IMock.ToString()").AppendLine();
		sb.Append("\t\t\t=> \"").Append(@class.DisplayString).Append(" mock that also implements ").Append(string.Join(", ", additionalInterfaces.Select(x => x.Class.DisplayString))).Append("\";").AppendLine();
		sb.AppendLine();

		if (!@class.IsInterface && constructors is not null)
		{
			foreach (Method constructor in constructors)
			{
				AppendMockSubject_BaseClassConstructor(sb, mockRegistryName, fileName, constructor,
					@class.HasRequiredMembers, hasAnyStaticMembers || hasAnyStaticEvents);
			}
		}
		else
		{
			sb.Append("\t\t/// <inheritdoc cref=\"").Append(fileName).Append("\" />").AppendLine();
			sb.Append("\t\tpublic ").Append(fileName).Append("(global::Mockolate.MockRegistry mockRegistry)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
			if (hasAnyStaticMembers || hasAnyStaticEvents)
			{
				sb.Append("\t\t\tMockRegistryProvider.Value = mockRegistry;").AppendLine();
			}

			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		Dictionary<string, int> signatureIndices = new();
		int[] nextSignatureIndex = [0,];
		
		// Combined mocks reuse the base mock's MockRegistry (and thus its FastMockInteractions sized
		// to the base MemberCount). Combined member ids would index past those buffers, so emit the
		// legacy RegisterInteraction path instead — recordings still flow through FastMockInteractions'
		// fallback buffer and remain shared with the base mock.
		AppendMockSubject_ImplementClass(sb, @class, mockRegistryName, null, memberIds, memberIdPrefix, signatureIndices, nextSignatureIndex, false);
		
		foreach ((string Name, Class Class) item in additionalInterfaces)
		{
			sb.AppendLine();
			AppendMockSubject_ImplementClass(sb, item.Class, mockRegistryName, @class as MockClass, memberIds, memberIdPrefix, signatureIndices, nextSignatureIndex, false);
		}

		sb.AppendLine();

		#region Setup implementations

		sb.Append("\t\t#region IMockSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		
		ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockSetupFor{name}", MemberType.Public, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
		
		sb.Append("\t\t#endregion IMockSetupFor").Append(name).AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockProtectedSetupFor{name}", MemberType.Protected, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockProtectedSetupFor").Append(name).AppendLine();
		}

		if (hasStaticMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockStaticSetupFor{name}", MemberType.Static, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockStaticSetupFor").Append(name).AppendLine();
		}

		foreach ((string Name, Class Class) item in additionalInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockSetupFor").Append(item.Name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, item.Class, mockRegistryName, $"IMockSetupFor{item.Name}", MemberType.Public, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockSetupFor").Append(item.Name).AppendLine();

			if (item.Class.AllMethods().Any(x => x.IsStatic) || item.Class.AllProperties().Any(x => x.IsStatic))
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockStaticSetupFor").Append(item.Name).AppendLine();
				sb.AppendLine();
				
				ImplementSetupInterface(sb, item.Class, mockRegistryName, $"IMockStaticSetupFor{item.Name}", MemberType.Static, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
				
				sb.Append("\t\t#endregion IMockStaticSetupFor").Append(item.Name).AppendLine();
			}
		}

		#endregion Setup implementations

		#region Raise implementations

		if (hasEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockRaiseOn").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementRaiseInterface(sb, @class, mockRegistryName, $"IMockRaiseOn{name}", MemberType.Public);
			
			sb.Append("\t\t#endregion IMockRaiseOn").Append(name).AppendLine();
		}

		if (hasProtectedEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedRaiseOn").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementRaiseInterface(sb, @class, mockRegistryName, $"IMockProtectedRaiseOn{name}", MemberType.Protected);
			
			sb.Append("\t\t#endregion IMockProtectedRaiseOn").Append(name).AppendLine();
		}

		if (hasStaticEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticRaiseOn").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementRaiseInterface(sb, @class, mockRegistryName, $"IMockStaticRaiseOn{name}", MemberType.Static);
			
			sb.Append("\t\t#endregion IMockStaticRaiseOn").Append(name).AppendLine();
		}
		
		foreach ((string Name, Class Class) item in additionalInterfaces)
		{
			if (item.Class.AllEvents().Any(x => !x.IsStatic))
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockRaiseOn").Append(item.Name).AppendLine();
				sb.AppendLine();
				
				ImplementRaiseInterface(sb, item.Class, mockRegistryName, $"IMockRaiseOn{item.Name}", MemberType.Public);
				
				sb.Append("\t\t#endregion IMockRaiseOn").Append(item.Name).AppendLine();
			}

			if (item.Class.AllEvents().Any(x => x.IsStatic))
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockStaticRaiseOn").Append(item.Name).AppendLine();
				sb.AppendLine();
				
				ImplementRaiseInterface(sb, item.Class, mockRegistryName, $"IMockStaticRaiseOn{item.Name}", MemberType.Static);
				
				sb.Append("\t\t#endregion IMockStaticRaiseOn").Append(item.Name).AppendLine();
			}
		}

		#endregion Raise implementations

		#region Verify implementation

		sb.AppendLine();
		sb.Append("\t\t#region IMockVerifyFor").Append(name).AppendLine();
		sb.AppendLine();
		// Combined mocks reuse the base mock's MockRegistry whose FastMockInteractions buffers are sized
		// to the base MemberCount and indexed by the base mock's member ids. The combination's own
		// memberIds enumerate a different (typically larger) set, so they cannot be used to fetch the
		// base buffers — emit the slow Verify path here instead. Recordings flow through the
		// FastMockInteractions fallback buffer (see AppendMockSubject_ImplementClass useFastBuffers: false).
		ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockVerifyFor{name}", MemberType.Public, memberIds, memberIdPrefix, false, useUnionOverloads);
		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();
		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockProtectedVerifyFor{name}", MemberType.Protected, memberIds, memberIdPrefix, false, useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockProtectedVerifyFor").Append(name).AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockStaticVerifyFor{name}", MemberType.Static, memberIds, memberIdPrefix, false, useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockStaticVerifyFor").Append(name).AppendLine();
		}

		foreach ((string Name, Class Class) item in additionalInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockVerifyFor").Append(item.Name).AppendLine();
			sb.AppendLine();
			
			ImplementVerifyInterface(sb, item.Class, mockRegistryName, $"IMockVerifyFor{item.Name}", MemberType.Public, memberIds, memberIdPrefix, false, useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockVerifyFor").Append(item.Name).AppendLine();

			if (item.Class.AllMethods().Any(x => x.IsStatic) || item.Class.AllProperties().Any(x => x.IsStatic) ||
			    item.Class.AllEvents().Any(x => x.IsStatic))
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockStaticVerifyFor").Append(item.Name).AppendLine();
				sb.AppendLine();
				
				ImplementVerifyInterface(sb, item.Class, mockRegistryName, $"IMockStaticVerifyFor{item.Name}", MemberType.Static, memberIds, memberIdPrefix, false, useUnionOverloads);
				
				sb.Append("\t\t#endregion IMockStaticVerifyFor").Append(item.Name).AppendLine();
			}
		}

		#endregion Verify implementation

		sb.AppendLine("\t}");
		
		#endregion MockForXXX
		
		sb.Append("}").AppendLine();
		sb.AppendLine();

		#endregion Mock

		#region Extensions

		sb.AppendXmlSummary($"Mock extensions for <see cref=\"{escapedClassName}\" /> that also implements<br />\n///      - {string.Join("<br />\n///      - ", additionalInterfaces.Select(x => $"<see cref=\"{x.Class.ClassFullName.EscapeForXmlDoc()}\" />"))}.", "");
#if !DEBUG
		sb.Append("[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("internal static partial class MockExtensionsFor").Append(fileName).AppendLine();
		sb.Append("{").AppendLine();

		#region Implementing

		(string Name, Class Class) lastInterface = additionalInterfaces[additionalInterfaces.Length - 1];
		sb.AppendXmlSummary($"Extends this mock so the returned instance also implements <see cref=\"{lastInterface.Class.ClassFullName.EscapeForXmlDoc()}\" />.");
		sb.AppendXmlRemarks([$"The returned instance is a brand-new mock that shares the mock registry (recorded interactions, scenario state, setups) of this one. Cast it to <see cref=\"{lastInterface.Class.ClassFullName.EscapeForXmlDoc()}\" /> to exercise the extra surface or use <c>.Mock.As&lt;{lastInterface.Class.ClassName}&gt;()</c> to reach the Setup/Verify surface of the additional interface.",]);
		sb.AppendXmlParam("sut", "The mock instance to extend.");
		sb.AppendXmlParam("setups", "Optional setup callbacks registered on the additional interface before the mock is returned.");
		sb.AppendXmlReturns($"A mock of the original type that additionally implements <see cref=\"{lastInterface.Class.ClassFullName.EscapeForXmlDoc()}\" />.");
		sb.Append("\tpublic static ").Append(@class.ClassFullName).Append(" Implementing<TInterface>(this ").Append(@class.ClassFullName).Append(" sut, params global::System.Action<global::Mockolate.Mock.IMockSetupFor").Append(lastInterface.Name).Append(">[] setups)").AppendLine();
		sb.Append("\t\twhere TInterface : ").Append(lastInterface.Class.ClassFullName).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tif (sut is not global::Mockolate.IMock mock)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The subject is no mock.\");").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Mock.").Append(fileName).Append(" value;").AppendLine();

		bool useTryCast = false;
		bool useTryCastWithDefaultValue = false;
		if (!@class.IsInterface && constructors?.Count > 0)
		{
			sb.Append("\t\tif (mock.MockRegistry.ConstructorParameters is null || mock.MockRegistry.ConstructorParameters.Length == 0)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			if (constructors.Value.Any(m => m.Parameters.Count == 0))
			{
				sb.Append("\t\t\tvalue = new global::Mockolate.Mock.").Append(fileName).Append("(mock.MockRegistry);").AppendLine();
			}
			else
			{
				sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"No parameterless constructor found for '").Append(@class.DisplayString).Append("'. Please provide constructor parameters.\");").AppendLine();
			}

			sb.Append("\t\t}").AppendLine();
			int constructorIndex = 0;
			foreach (EquatableArray<MethodParameter> constructorParameters in constructors.Value
				         .Select(constructor => constructor.Parameters))
			{
				constructorIndex++;
				int requiredParameters = constructorParameters.Count(c => !c.HasExplicitDefaultValue);
				if (requiredParameters < constructorParameters.Count)
				{
					sb.Append("\t\telse if (mock.MockRegistry.ConstructorParameters.Length >= ")
						.Append(requiredParameters).Append(" && mock.MockRegistry.ConstructorParameters.Length <= ")
						.Append(constructorParameters.Count);
				}
				else
				{
					sb.Append("\t\telse if (mock.MockRegistry.ConstructorParameters.Length == ")
						.Append(constructorParameters.Count);
				}

				int constructorParameterIndex = 0;
				foreach (MethodParameter parameter in constructorParameters)
				{
					useTryCast = useTryCast || !parameter.HasExplicitDefaultValue;
					useTryCastWithDefaultValue = useTryCastWithDefaultValue || parameter.HasExplicitDefaultValue;
					sb.AppendLine().Append("\t\t    && ")
						.Append(parameter.HasExplicitDefaultValue ? "TryCastWithDefaultValue" : "TryCast")
						.Append("(mock.MockRegistry.ConstructorParameters, ")
						.Append(constructorParameterIndex++)
						.Append(parameter.HasExplicitDefaultValue ? $", {parameter.ExplicitDefaultValue}" : "")
						.Append(", mock.MockRegistry.Behavior, out ").Append(parameter.Type.Fullname).Append(" c")
						.Append(constructorIndex)
						.Append('p')
						.Append(constructorParameterIndex).Append(")");
				}

				sb.Append(")").AppendLine();
				sb.Append("\t\t{").AppendLine();
				sb.Append("\t\t\tvalue = new global::Mockolate.Mock.").Append(fileName).Append("(mock.MockRegistry");
				for (int j = 1; j <= constructorParameters.Count; j++)
				{
					sb.Append(", ").Append('c').Append(constructorIndex).Append('p').Append(j);
				}

				sb.Append(");").AppendLine();
				sb.Append("\t\t}").AppendLine();
			}

			sb.Append("\t\telse").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException($\"Could not find any constructor for '").Append(@class.DisplayString).Append("' that matches the {mock.MockRegistry.ConstructorParameters.Length} given parameters ({string.Join(\", \", mock.MockRegistry.ConstructorParameters)}).\");").AppendLine();
			sb.Append("\t\t}").AppendLine();
		}
		else
		{
			sb.Append("\t\tvalue = new global::Mockolate.Mock.").Append(fileName).Append("(mock.MockRegistry);").AppendLine();
		}

		sb.Append("\t\tIMockBehaviorAccess mockBehaviorAccess = (global::Mockolate.IMockBehaviorAccess)mock.MockRegistry.Behavior;").AppendLine();
		sb.Append("\t\tif (mockBehaviorAccess.TryGet<global::System.Action<global::Mockolate.Mock.IMockSetupFor").Append(lastInterface.Name).Append(">[]?>(out var additionalSetups))").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (setups.Length > 0)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar concatenatedSetups = new global::System.Action<global::Mockolate.Mock.IMockSetupFor").Append(lastInterface.Name).Append(">[additionalSetups.Length + setups.Length];").AppendLine();
		sb.Append("\t\t\t\tadditionalSetups.CopyTo(concatenatedSetups, 0);").AppendLine();
		sb.Append("\t\t\t\tsetups.CopyTo(concatenatedSetups, additionalSetups.Length);").AppendLine();
		sb.Append("\t\t\t\tsetups = concatenatedSetups;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\telse").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tsetups = additionalSetups;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t\tif (setups.Length > 0)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tforeach (var setup in setups)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tsetup.Invoke(value);").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("\t\treturn value;").AppendLine();
		if (useTryCast)
		{
			sb.Append("""
			          		static bool TryCast<TValue>(object?[] values, int index, global::Mockolate.MockBehavior behavior, out TValue result)
			          		{
			          		    var value = values[index];
			          			if (value is TValue typedValue)
			          			{
			          				result = typedValue;
			          				return true;
			          			}

			          			result = default!;
			          			return value is null;
			          		}
			          """).AppendLine();
		}

		if (useTryCastWithDefaultValue)
		{
			sb.Append("""
			          		static bool TryCastWithDefaultValue<TValue>(object?[] values, int index, TValue defaultValue, global::Mockolate.MockBehavior behavior, out TValue result)
			          		{
			          			if (values.Length > index && values[index] is TValue typedValue)
			          			{
			          				result = typedValue;
			          				return true;
			          			}

			          			result = defaultValue;
			          			return true;
			          		}
			          """).AppendLine();
		}

		sb.Append("\t}").AppendLine();

		#endregion Implementing

		sb.Append("}").AppendLine();
		sb.AppendLine();

		#endregion Extensions

		sb.AppendLine("#nullable disable annotations");
		return sb.ToString();
	}
}
