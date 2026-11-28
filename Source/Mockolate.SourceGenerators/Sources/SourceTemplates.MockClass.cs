using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using Mockolate.SourceGenerators.Internals;
using GenericParameter = Mockolate.SourceGenerators.Entities.GenericParameter;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class SourceTemplates
{
	private const int MaxExplicitParameters = 4;

	public static string MockClass(
		string name,
		Class @class,
		AutoMockingOptions? autoMocking,
		bool hasOverloadResolutionPriority = false,
		(string Name, Class Class)[]? hiddenBaseInterfaces = null,
		bool useUnionOverloads = false)
	{
		hiddenBaseInterfaces ??= [];
		EquatableArray<Method>? constructors = (@class as MockClass)?.Constructors;
		bool hasParameterizedConstructor = !@class.IsInterface &&
		                                   constructors?.Any(m => m.Parameters.Count > 0) == true;
		bool hasConstructorArguments = HasConstructorArguments(@class, constructors);
		string classReference = @class.XmlDocReference;
		bool hasEvents = @class.AllEvents().Any(x => !x.IsStatic);
		bool hasStaticEvents = @class.IsInterface && @class.AllEvents().Any(@event => @event.IsStatic);
		bool hasStaticMembers = @class.IsInterface && (@class.AllMethods().Any(method => method.IsStatic) || @class.AllProperties().Any(property => property.IsStatic));
		bool hasProtectedEvents = !@class.IsInterface && @class.AllEvents().Any(@event => @event.IsProtected);
		bool hasProtectedMembers = !@class.IsInterface && (@class.AllMethods().Any(method => method.IsProtected) || @class.AllProperties().Any(property => property.IsProtected));
		bool hasGenericMethods = HasGenericFacadeMethods(@class, MemberType.Public);
		bool hasProtectedGenericMethods = HasGenericFacadeMethods(@class, MemberType.Protected);
		string setupType = hasProtectedMembers ? $"IMockSetupInitializationFor{name}" : $"global::Mockolate.Mock.IMockSetupFor{name}";
		string mockRegistryName = @class.GetUniqueName("MockRegistry", "MockolateMockRegistry");
		Class[] memberIdClasses = new Class[1 + hiddenBaseInterfaces.Length];
		memberIdClasses[0] = @class;
		for (int i = 0; i < hiddenBaseInterfaces.Length; i++)
		{
			memberIdClasses[i + 1] = hiddenBaseInterfaces[i].Class;
		}

		MemberIdTable memberIds = ComputeMemberIds(memberIdClasses);
		string memberIdPrefix = $"global::Mockolate.Mock.{name}.";
		
		bool overridesEquals = OverridesEquals(@class);
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable annotations").AppendLine();
		if (overridesEquals)
		{
			sb.Append(DisableEqualsWithoutGetHashCodeWarning).AppendLine();
		}

		sb.Append("namespace Mockolate;").AppendLine();
		sb.AppendLine();

		#region Mock

		sb.Append("internal static partial class Mock").AppendLine();
		sb.Append('{').AppendLine();
		
		#region MockForXXX
		
		sb.AppendXmlSummary($"A mock implementation for {classReference}.", "\t");
		sb.Append("\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
		AppendDebuggerNonUserCode(sb, "\t");
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tinternal class ").Append(name).Append(" :").AppendLine();
		sb.Append("\t\t").Append(@class.ClassFullName).Append(", IMockFor").Append(name).Append(", IMockSetupFor").Append(name);
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

		AppendGenericFacadeInterfaces(sb, @class, name);

		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.Append(", IMockProtectedVerifyFor").Append(name);
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.Append(", IMockStaticVerifyFor").Append(name);
		}

		sb.Append(',').AppendLine();

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			bool hbHasStaticMembers = hiddenBase.Class.AllMethods().Any(x => x.IsStatic) ||
			                          hiddenBase.Class.AllProperties().Any(x => x.IsStatic);
			bool hbHasStaticEvents = hiddenBase.Class.AllEvents().Any(x => x.IsStatic);
			bool hbHasInstanceEvents = hiddenBase.Class.AllEvents().Any(x => !x.IsStatic);
			sb.Append("\t\tIMockFor").Append(hiddenBase.Name).Append(", IMockSetupFor").Append(hiddenBase.Name);
			if (hbHasStaticMembers)
			{
				sb.Append(", IMockStaticSetupFor").Append(hiddenBase.Name);
			}

			if (hbHasInstanceEvents)
			{
				sb.Append(", IMockRaiseOn").Append(hiddenBase.Name);
			}

			if (hbHasStaticEvents)
			{
				sb.Append(", IMockStaticRaiseOn").Append(hiddenBase.Name);
			}

			sb.Append(", IMockVerifyFor").Append(hiddenBase.Name);
			AppendGenericFacadeInterfaces(sb, hiddenBase.Class, hiddenBase.Name);
			if (hbHasStaticMembers || hbHasStaticEvents)
			{
				sb.Append(", IMockStaticVerifyFor").Append(hiddenBase.Name);
			}

			sb.Append(',').AppendLine();
		}

		sb.Append("\t\tglobal::Mockolate.IMock");
		AppendAdapterAccessDeclaration(sb, [@class,]);
		sb.AppendLine();
		sb.Append("\t{").AppendLine();

		memberIds.Emit(sb, "\t\t", @class is { ClassFullName: "global::System.Net.Http.HttpClient", } ? "global::Mockolate.Mock.HttpMessageHandler.MemberCount" : null);
		sb.AppendLine();

		AppendCreateInteractions(sb, "\t\t");
		sb.AppendLine();

		bool hasMockRegistryProvider = constructors?.Count > 0 || (@class.IsInterface && (hasStaticMembers || hasStaticEvents));
		AppendCreateRegistryFromBehavior(sb, "\t\t", hasMockRegistryProvider, autoMocking);
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Internals.MockRegistry global::Mockolate.IMock.MockRegistry => this.").Append(mockRegistryName).Append(';').AppendLine();
		if (constructors?.Count > 0)
		{
			sb.Append("\t\tprivate global::Mockolate.Internals.MockRegistry ").Append(mockRegistryName).AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget => field ?? MockRegistryProvider.Value;").AppendLine();
			sb.Append("\t\t\tset;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tinternal static readonly global::System.Threading.AsyncLocal<global::Mockolate.Internals.MockRegistry> MockRegistryProvider = new global::System.Threading.AsyncLocal<global::Mockolate.Internals.MockRegistry>();").AppendLine();
		}
		else
		{
			sb.Append("\t\tprivate global::Mockolate.Internals.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
			if (hasStaticMembers || hasStaticEvents)
			{
				sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
				sb.Append("\t\tinternal static readonly global::System.Threading.AsyncLocal<global::Mockolate.Internals.MockRegistry> MockRegistryProvider = new global::System.Threading.AsyncLocal<global::Mockolate.Internals.MockRegistry>();").AppendLine();
			}
		}

		string? constructorArgumentsName = hasConstructorArguments ? AppendConstructorArgumentsField(sb, @class) : null;

		sb.AppendLine();


		sb.AppendLine();

		string[]? adapterIgnoredNames = GetAdapterIgnoredNames([@class,]);
		string? adapterClearHook = GetAdapterClearHook([@class,]);
		ImplementMockForInterface(sb, mockRegistryName, name, hasEvents, hasProtectedMembers, hasProtectedEvents, hasStaticMembers, hasStaticEvents, adapterIgnoredNames, adapterClearHook, constructorArgumentsName, @class.DisplayString, hasGenericMethods, hasProtectedGenericMethods);

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			ImplementMockForInterface(sb, mockRegistryName, hiddenBase.Name,
				hiddenBase.Class.AllEvents().Any(x => !x.IsStatic),
hasProtectedMembers: false /* interfaces have no protected members */,
hasProtectedEvents: false /* interfaces have no protected events */,
				hiddenBase.Class.AllMethods().Any(x => x.IsStatic) || hiddenBase.Class.AllProperties().Any(x => x.IsStatic),
				hiddenBase.Class.AllEvents().Any(x => x.IsStatic),
				adapterIgnoredNames, adapterClearHook, hasGenericMethods: HasGenericFacadeMethods(hiddenBase.Class, MemberType.Public));
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.IMock.ToString()").AppendLine();
		sb.Append("\t\t\t=> \"").Append(@class.DisplayString).Append(" mock\";").AppendLine();
		// A class named ToString cannot have a member of its name (CS0542).
		if (@class.IsInterface && !string.Equals(name, "ToString", StringComparison.Ordinal) && !DeclaresToString(@class))
		{
			AppendObjectToString(sb, $"{@class.DisplayString} mock");
		}

		AppendAdapterAccessImplementation(sb, [@class,], mockRegistryName, "\"\"", memberIds, memberIdPrefix);
		sb.AppendLine();

		if (@class.IsInterface)
		{
			sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
			sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.Internals.MockRegistry mockRegistry)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
			if (hasStaticMembers || hasStaticEvents)
			{
				sb.Append("\t\t\tMockRegistryProvider.Value = mockRegistry;").AppendLine();
			}

			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
			
			AppendMockSubject_BehaviorConstructor(sb, name);
		}
		else if (constructors is not null)
		{
			int constructorIndex = 0;
			foreach (Method constructor in constructors)
			{
				AppendMockSubject_BaseClassConstructor(sb, mockRegistryName, name, constructor, @class,
					recording: GetConstructorRecording(@class, constructorArgumentsName, constructor, constructorIndex++));
				AppendMockSubject_BehaviorBaseClassConstructor(sb, name, constructor, @class.HasRequiredMembers);
			}
		}

		AppendMockSubject_ImplementClass(sb, @class, mockRegistryName, mockClass: null, memberIds, memberIdPrefix, autoMocking);
		
		sb.AppendLine();

		#region Mock.Setup

		sb.Append("\t\t#region IMockSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockSetupFor{name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
		sb.Append("\t\t#endregion IMockSetupFor").Append(name).AppendLine();

		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockProtectedSetupFor{name}", MemberType.Protected, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockProtectedSetupFor").Append(name).AppendLine();
		}

		if (hasStaticMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockStaticSetupFor{name}", MemberType.Static, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockStaticSetupFor").Append(name).AppendLine();
		}

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockSetupFor").Append(hiddenBase.Name).AppendLine();
			sb.AppendLine();
			ImplementSetupInterface(sb, hiddenBase.Class, mockRegistryName, $"IMockSetupFor{hiddenBase.Name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockSetupFor").Append(hiddenBase.Name).AppendLine();
		}

		#endregion Mock.Setup

		#region Mock.Raise
		
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
		
		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			if (hiddenBase.Class.AllEvents().Any(x => !x.IsStatic))
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockRaiseOn").Append(hiddenBase.Name).AppendLine();
				sb.AppendLine();
				ImplementRaiseInterface(sb, hiddenBase.Class, mockRegistryName, $"IMockRaiseOn{hiddenBase.Name}", MemberType.Public);
				sb.Append("\t\t#endregion IMockRaiseOn").Append(hiddenBase.Name).AppendLine();
			}
		}

		#endregion Mock.Raise

		#region Mock.Verify

		sb.AppendLine();
		sb.Append("\t\t#region IMockVerifyFor").Append(name).AppendLine();
		sb.AppendLine();
		ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockVerifyFor{name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();

		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockProtectedVerifyFor{name}", MemberType.Protected, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockProtectedVerifyFor").Append(name).AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockStaticVerifyFor{name}", MemberType.Static, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockStaticVerifyFor").Append(name).AppendLine();
		}

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockVerifyFor").Append(hiddenBase.Name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, hiddenBase.Class, mockRegistryName, $"IMockVerifyFor{hiddenBase.Name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useTypedRecords: false, useUnionOverloads);
			sb.Append("\t\t#endregion IMockVerifyFor").Append(hiddenBase.Name).AppendLine();
		}

		ImplementGenericFacade(sb, @class, name, mockRegistryName, memberIds, memberIdPrefix);
		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			ImplementGenericFacade(sb, hiddenBase.Class, hiddenBase.Name, mockRegistryName, memberIds, memberIdPrefix);
		}

		#endregion Mock.Verify

		sb.AppendLine("\t}");
		sb.AppendLine();

		#endregion MockForXXX

		#region VerifyMonitor
		
		AppendDebuggerNonUserCode(sb, "\t");
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tprivate sealed class VerifyMonitor").Append(name).Append("(global::Mockolate.Internals.MockRegistry mockRegistry) : global::Mockolate.Mock.IMockVerifyFor").Append(name);
		if (hasGenericMethods)
		{
			sb.Append(", ").Append(GetGenericVerifyFacadeName(MemberType.Public, name));
		}

		AppendAdapterAccessDeclaration(sb, [@class,]);
		sb.AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.Internals.MockRegistry ").Append(mockRegistryName).Append(" { get; } = mockRegistry;").AppendLine();
		AppendAdapterAccessImplementation(sb, [@class,], mockRegistryName, "\"\"", memberIds, memberIdPrefix);
		sb.AppendLine();
		sb.Append("\t\t#region IMockVerifyFor").Append(name).AppendLine();
		sb.AppendLine();

		ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockVerifyFor{name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);

		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();
		ImplementGenericVerifyFacade(sb, @class, name, MemberType.Public, mockRegistryName);
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion VerifyMonitor

		#region MockInScenarioForXXX
		
		AppendDebuggerNonUserCode(sb, "\t");
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tprivate sealed class MockInScenarioFor").Append(name).Append(" : global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(", global::Mockolate.Mock.IMockSetupFor").Append(name);
		if (hasProtectedMembers)
		{
			sb.Append(", global::Mockolate.Mock.IMockProtectedSetupFor").Append(name);
		}

		foreach (MemberType memberType in new[] { MemberType.Public, MemberType.Protected, })
		{
			if (HasGenericFacadeMethods(@class, memberType))
			{
				sb.Append(", ").Append(GetGenericSetupFacadeName(memberType, name));
			}
		}

		AppendAdapterAccessDeclaration(sb, [@class,]);
		sb.AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.Internals.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
		sb.Append("\t\tprivate string _scenarioName;").AppendLine();
		sb.AppendLine();
		sb.Append("\t\tpublic MockInScenarioFor").Append(name).Append("(global::Mockolate.Internals.MockRegistry mockRegistry, string scenario)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
		sb.Append("\t\t\t_scenarioName = scenario;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		AppendAdapterAccessImplementation(sb, [@class,], mockRegistryName, "this._scenarioName", memberIds, memberIdPrefix);
		sb.AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Mock.IMockSetupFor").Append(name).Append(" global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(".Setup").AppendLine();
		sb.Append("\t\t\t=> this;").AppendLine();
		sb.AppendLine();
		if (hasProtectedMembers)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(" global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(".SetupProtected").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
			sb.AppendLine();
		}

		if (hasGenericMethods)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Mock.").Append(GetGenericSetupFacadeName(MemberType.Public, name)).Append(" global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(".SetupGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
			sb.AppendLine();
		}

		if (hasProtectedGenericMethods)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tglobal::Mockolate.Mock.").Append(GetGenericSetupFacadeName(MemberType.Protected, name)).Append(" global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(".SetupProtectedGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t#region IMockSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		
		ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockSetupFor{name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, "this._scenarioName", useUnionOverloads);
		
		sb.Append("\t\t#endregion IMockSetupFor").Append(name).AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockProtectedSetupFor{name}", MemberType.Protected, memberIds, memberIdPrefix, hasOverloadResolutionPriority, "this._scenarioName", useUnionOverloads);

			sb.Append("\t\t#endregion IMockProtectedSetupFor").Append(name).AppendLine();
		}

		ImplementGenericSetupFacade(sb, @class, name, mockRegistryName, memberIds, memberIdPrefix, "this._scenarioName");
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion MockInScenarioForXXX

		#region Mock Interfaces
		
		#region IMockForXXX

		sb.AppendXmlSummary($"The Mockolate accessor for a mock of {classReference}, reached through <c>.Mock</c> on the mocked instance.", "\t");
		sb.AppendXmlRemarks(["Groups every operation that acts on the mock rather than on the mocked subject: setups, verifications, event raising, scenarios and monitoring.",], "\t");
		sb.Append("\tinternal interface IMockFor").Append(name).Append(" : global::Mockolate.IMock").AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary($"Configures how members of the mock of {classReference} respond when invoked.");
		sb.AppendXmlRemarks(
		[
			"Each mocked member is available as a strongly-typed entry on this surface. Chain <c>Returns</c>, <c>ReturnsAsync</c>, <c>Throws</c>, <c>ThrowsAsync</c> or <c>Do</c> to control the response; chain <c>InitializeWith</c>/<c>Register</c> to initialize properties and indexers; chain multiple returns/throws to define a sequence; use <c>.For(n)</c>, <c>.Only(n)</c>, <c>.Forever()</c>, <c>.When(predicate)</c> to control when a callback runs.",
			"When two setups overlap, the most recently defined one wins.",
		]);
		sb.Append("\t\tIMockSetupFor").Append(name).Append(" Setup { get; }").AppendLine();
		sb.AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendXmlSummary($"Configures how <see langword=\"protected\" /> virtual members of the mock of {classReference} respond when invoked.");
			sb.AppendXmlRemarks(["Only members declared as <see langword=\"protected\" /> (or <see langword=\"protected\" /> <see langword=\"internal\" />) on the mocked class appear here. All setup chain operators (<c>Returns</c>, <c>Throws</c>, <c>Do</c>, sequences, <c>.For</c>/<c>.Only</c>/<c>.Forever</c>, ...) work identically to <see cref=\"Setup\" />.",]);
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" SetupProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticMembers)
		{
			sb.AppendXmlSummary($"Configures how <see langword=\"static\" /> members declared on {classReference} respond when invoked.");
			sb.AppendXmlRemarks(["Static members are scoped per async/execution flow while the mock is alive; invocations from other flows are not intercepted.",]);
			sb.Append("\t\tIMockStaticSetupFor").Append(name).Append(" SetupStatic { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasGenericMethods)
		{
			sb.AppendXmlSummary($"Configures how generic methods of the mock of {classReference} respond when invoked with any type argument.");
			sb.AppendXmlRemarks(["Parameters whose type depends on a type argument are matched as <see langword=\"object\" /> against the runtime value. A setup of a specific type argument and a setup for any type argument both apply; the most recently defined matching one wins.",]);
			sb.Append("\t\tIMockGenericSetupFor").Append(name).Append(" SetupGeneric { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasProtectedGenericMethods)
		{
			sb.AppendXmlSummary($"Configures how <see langword=\"protected\" /> generic methods of the mock of {classReference} respond when invoked with any type argument.");
			sb.AppendXmlRemarks([$"Same semantics as {(hasGenericMethods ? "<see cref=\"SetupGeneric\" />" : "<c>SetupGeneric</c>")}, for the generic methods that <see cref=\"SetupProtected\" /> offers.",]);
			sb.Append("\t\t").Append(GetGenericSetupFacadeName(MemberType.Protected, name)).Append(" SetupProtectedGeneric { get; }").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary($"Opens a named scenario scope on the mock of {classReference} so that additional setups can be registered for that scenario.");
		sb.AppendXmlRemarks(["Scenarios let you define per-state behavior. Setups registered inside the returned <c>IMockInScenarioFor...</c> scope only apply while the mock's current scenario matches <paramref name=\"scenario\" />; switch scenarios with <see cref=\"TransitionTo\" />.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to enter. Any non-null string acts as a key; the mock starts in an unnamed default scenario.");
		sb.AppendXmlReturns("A scoped accessor whose <c>Setup</c> (and <c>SetupProtected</c>, where applicable) register scenario-specific setups.");
		sb.Append("\t\tIMockInScenarioFor").Append(name).Append(" InScenario(string scenario);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Opens a named scenario scope on the mock of {classReference} and immediately invokes <paramref name=\"setup\" /> to register scenario-specific setups.");
		sb.AppendXmlRemarks(["Equivalent to <c>InScenario(scenario)</c> followed by the setup callback, but returns the original <c>IMockFor...</c> accessor so it chains nicely at mock-creation time.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to enter.");
		sb.AppendXmlParam("setup", "Callback that receives the scenario-scoped setup surface and registers scenario-specific setups.");
		sb.AppendXmlReturns("This accessor, to allow chaining.");
		sb.Append("\t\tIMockFor").Append(name).Append(" InScenario(string scenario, global::System.Action<IMockInScenarioFor").Append(name).Append("> setup);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Switches the active scenario of the mock of {classReference} to <paramref name=\"scenario\" />.");
		sb.AppendXmlRemarks(["After the transition, setups registered via <see cref=\"InScenario(string)\" /> under that scenario take effect. Scenarios that have no matching setup for a given member fall back to the default (un-scoped) setups.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to transition to.");
		sb.AppendXmlReturns("This accessor, to allow chaining.");
		sb.Append("\t\tIMockFor").Append(name).Append(" TransitionTo(string scenario);").AppendLine();
		sb.AppendLine();

		if (hasEvents)
		{
			sb.AppendXmlSummary($"Triggers events declared on {classReference} so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["One entry per event is generated; the signature matches the event's delegate. Only handlers that are subscribed at the moment of the <c>Raise</c> call are invoked - handlers subscribed later (or already removed) are skipped.",]);
			sb.Append("\t\tIMockRaiseOn").Append(name).Append(" Raise { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasProtectedEvents)
		{
			sb.AppendXmlSummary($"Triggers <see langword=\"protected\" /> events declared on {classReference} so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["Same semantics as <see cref=\"Raise\" /> but for events whose accessibility prevents external subscription from outside the class. Useful when testing code that subclasses the mocked type.",]);
			sb.Append("\t\tIMockProtectedRaiseOn").Append(name).Append(" RaiseProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticEvents)
		{
			sb.AppendXmlSummary($"Triggers <see langword=\"static\" /> events declared on {classReference} so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["Static events are scoped per async/execution flow while the mock is alive.",]);
			sb.Append("\t\tIMockStaticRaiseOn").Append(name).Append(" RaiseStatic { get; }").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary($"Asserts how often, and in which order, members of the mock of {classReference} were invoked.");
		sb.AppendXmlRemarks(
		[
			"Each call to a member here returns a <c>VerificationResult</c> that you terminate with a count assertion: <c>Never()</c>, <c>Once()</c>, <c>Twice()</c>, <c>Exactly(n)</c>, <c>AtLeast(n)</c>/<c>AtLeastOnce()</c>/<c>AtLeastTwice()</c>, <c>AtMost(n)</c>/<c>AtMostOnce()</c>/<c>AtMostTwice()</c>, <c>Between(min, max)</c> or <c>Times(predicate)</c>.",
			"Use <c>Within(TimeSpan)</c> / <c>WithCancellation(CancellationToken)</c> before the terminator to wait for expected interactions that happen on background threads.",
			"Chain <c>Then(...)</c> to assert an ordered sequence of calls. A failing assertion throws a <see cref=\"global::Mockolate.Exceptions.MockVerificationException\" />.",
		]);
		sb.Append("\t\tIMockVerifyFor").Append(name).Append(" Verify { get; }").AppendLine();
		sb.AppendLine();
		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.AppendXmlSummary($"Asserts how often, and in which order, <see langword=\"protected\" /> members of the mock of {classReference} were invoked.");
			sb.AppendXmlRemarks(["Same terminators and modifiers as <see cref=\"Verify\" /> (<c>Once()</c>, <c>Exactly(n)</c>, <c>Within(...)</c>, <c>Then(...)</c>, ...); applies to <see langword=\"protected\" /> members and events instead of public ones.",]);
			sb.Append("\t\tIMockProtectedVerifyFor").Append(name).Append(" VerifyProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendXmlSummary($"Asserts how often, and in which order, <see langword=\"static\" /> members declared on {classReference} were invoked.");
			sb.AppendXmlRemarks(["Same terminators and modifiers as <see cref=\"Verify\" />; scoped per async/execution flow in the same way as <see cref=\"SetupStatic\" />.",]);
			sb.Append("\t\tIMockStaticVerifyFor").Append(name).Append(" VerifyStatic { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasGenericMethods)
		{
			sb.AppendXmlSummary($"Asserts how often generic methods of the mock of {classReference} were invoked with any type argument.");
			sb.AppendXmlRemarks(["Same terminators and modifiers as <see cref=\"Verify\" />; parameters whose type depends on a type argument are matched as <see langword=\"object\" /> against the runtime value.",]);
			sb.Append("\t\tIMockGenericVerifyFor").Append(name).Append(" VerifyGeneric { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasProtectedGenericMethods)
		{
			sb.AppendXmlSummary($"Asserts how often <see langword=\"protected\" /> generic methods of the mock of {classReference} were invoked with any type argument.");
			sb.AppendXmlRemarks([$"Same semantics as {(hasGenericMethods ? "<see cref=\"VerifyGeneric\" />" : "<c>VerifyGeneric</c>")}, for the generic methods that <see cref=\"VerifyProtected\" /> offers.",]);
			sb.Append("\t\t").Append(GetGenericVerifyFacadeName(MemberType.Protected, name)).Append(" VerifyProtectedGeneric { get; }").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary("Verifies how often a specific method setup was matched by actual invocations.");
		sb.AppendXmlRemarks(["Useful when you want to verify &quot;this <em>particular</em> setup was hit N times&quot; without re-stating the matchers. Chain the usual count terminators (<c>Once()</c>, <c>AtLeastOnce()</c>, <c>Exactly(n)</c>, ...) on the returned result.",]);
		sb.AppendXmlParam("setup", "The setup previously registered through <see cref=\"Setup\" /> (typically returned from a <c>Returns(...)</c>/<c>Throws(...)</c> call).");
		sb.AppendXmlReturns("A <c>VerificationResult</c> that counts invocations matching the given setup.");
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> VerifySetup(global::Mockolate.Setup.IMethodSetup setup);").AppendLine();
		sb.AppendLine();
		
		sb.AppendXmlSummary("Checks whether every recorded interaction on this mock has been observed by at least one <c>Verify</c> call.");
		sb.AppendXmlRemarks(["Useful in test teardown to catch unexpected interactions (&quot;strict verification&quot;): if any recorded call has never been matched by a verification, the method returns <see langword=\"false\" />.",]);
		sb.AppendXmlReturns("<see langword=\"true\" /> if every recorded interaction was verified at least once; otherwise <see langword=\"false\" />.");
		sb.Append("\t\tbool VerifyThatAllInteractionsAreVerified();").AppendLine();
		sb.AppendLine();
		
		sb.AppendXmlSummary("Checks whether every registered setup on this mock was matched by at least one actual invocation.");
		sb.AppendXmlRemarks(["Useful to catch unused setups that silently rot as the test subject evolves.",]);
		sb.AppendXmlReturns("<see langword=\"true\" /> if every registered setup was used at least once; otherwise <see langword=\"false\" />.");
		sb.Append("\t\tbool VerifyThatAllSetupsAreUsed();").AppendLine();
		sb.AppendLine();
		
		sb.AppendXmlSummary("Removes every recorded interaction from this mock while keeping all registered setups intact.");
		sb.AppendXmlRemarks(["Handy when a single test exercises multiple logical phases and you only want to verify the interactions of the latest phase.",]);
		sb.Append("\t\tvoid ClearAllInteractions();").AppendLine();
		sb.AppendLine();
		
		sb.AppendXmlSummary("Creates a monitor whose <c>Verify</c> surface is scoped to interactions produced between <c>monitor.Run()</c> and the disposal of its <see cref=\"global::System.IDisposable\" /> scope.");
		sb.AppendXmlRemarks(["The underlying mock keeps recording all interactions as usual - only the monitor's <c>Verify</c> view is scoped. Useful to verify only the interactions produced by a specific block of test code without resetting the mock.",]);
		sb.AppendXmlReturns("A <see cref=\"global::Mockolate.Monitor.MockMonitor{T}\" /> that exposes <c>Verify</c> over the monitored interactions and a <c>Run()</c> method that opens the recording scope.");
		sb.Append("\t\tglobal::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append("> Monitor();").AppendLine();
		sb.AppendLine();
		AppendTrackCallOrderDeclaration(sb);
		if (hasConstructorArguments)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Gets the only argument of type <typeparamref name=\"T\" /> that the constructor of the mock of {classReference} received.");
			sb.AppendXmlRemarks(["Use it to set up and verify the mocks that <c>CreateMock()</c> passed to the constructor. When the constructor received several arguments of the type, pass the name of the parameter.",]);
			sb.AppendXmlReturns("The argument of type <typeparamref name=\"T\" />.");
			sb.AppendXmlException("global::Mockolate.Exceptions.MockException", "The constructor received no or several arguments of type <typeparamref name=\"T\" />.");
			sb.Append("\t\tT ConstructorArgument<T>();").AppendLine();
			sb.AppendLine();
			sb.AppendXmlSummary($"Gets the argument of the parameter <paramref name=\"parameterName\" /> that the constructor of the mock of {classReference} received.");
			sb.AppendXmlParam("parameterName", "The name of the constructor parameter.");
			sb.AppendXmlReturns("The argument of the parameter <paramref name=\"parameterName\" />.");
			sb.AppendXmlException("global::Mockolate.Exceptions.MockException", "The constructor has no parameter <paramref name=\"parameterName\" />, or its argument is no <typeparamref name=\"T\" />.");
			sb.Append("\t\tT ConstructorArgument<T>(string parameterName);").AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockForXXX

		#region IMockInScenarioForXXX

		sb.AppendXmlSummary($"Scoped access to setups for a scenario on the mock of {classReference}.", "\t");
		sb.Append("\tinternal interface IMockInScenarioFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary($"Set up the mock of {classReference} within the scenario scope.");
		sb.Append("\t\tIMockSetupFor").Append(name).Append(" Setup { get; }").AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Set up protected members of the mock of {classReference} within the scenario scope.");
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" SetupProtected { get; }").AppendLine();
		}

		if (hasGenericMethods)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Set up generic methods of the mock of {classReference} for any type argument within the scenario scope.");
			sb.Append("\t\t").Append(GetGenericSetupFacadeName(MemberType.Public, name)).Append(" SetupGeneric { get; }").AppendLine();
		}

		if (hasProtectedGenericMethods)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Set up protected generic methods of the mock of {classReference} for any type argument within the scenario scope.");
			sb.Append("\t\t").Append(GetGenericSetupFacadeName(MemberType.Protected, name)).Append(" SetupProtectedGeneric { get; }").AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockInScenarioForXXX

		#region IMockSetupForXXX

		sb.AppendXmlSummary($"Set up the mock of {classReference}.", "\t");
		sb.Append("\tinternal interface IMockSetupFor").Append(name);
		if (!hasStaticMembers && !hasStaticEvents)
		{
			sb.Append(" : global::Mockolate.Setup.IMockSetup<").Append(@class.ClassFullName).Append('>').AppendLine();
		}
		else
		{
			sb.AppendLine();
		}

		sb.Append("\t{").AppendLine();
		
		DefineSetupInterface(sb, @class, MemberType.Public, hasOverloadResolutionPriority, useUnionOverloads);
		
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockSetupForXXX

		#region IMockProtectedSetupForXXX

		if (hasProtectedMembers)
		{
			sb.AppendXmlSummary($"Set up protected members for the mock of {classReference}.", "\t");
			sb.Append("\tinternal interface IMockProtectedSetupFor").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineSetupInterface(sb, @class, MemberType.Protected, hasOverloadResolutionPriority, useUnionOverloads);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion IMockProtectedSetupForXXX

		#region IMockStaticSetupForXXX
		
		if (hasStaticMembers)
		{
			sb.AppendXmlSummary($"Set up static members for the mock of {classReference}.", "\t");
			sb.Append("\tinternal interface IMockStaticSetupFor").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineSetupInterface(sb, @class, MemberType.Static, hasOverloadResolutionPriority, useUnionOverloads);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion IMockStaticSetupForXXX

		#region IMockRaiseOnXXX
		
		if (hasEvents)
		{
			sb.AppendXmlSummary($"Raise events on the mock of {classReference}.", "\t");
			sb.Append("\tinternal interface IMockRaiseOn").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineRaiseInterface(sb, @class, MemberType.Public);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}
		
		#endregion IMockRaiseOnXXX

		#region IMockProtectedRaiseOnXXX

		if (hasProtectedEvents)
		{
			sb.AppendXmlSummary($"Raise protected events on the mock of {classReference}.", "\t");
			sb.Append("\tinternal interface IMockProtectedRaiseOn").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineRaiseInterface(sb, @class, MemberType.Protected);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion IMockProtectedRaiseOnXXX

		#region IMockStaticRaiseOnXXX

		if (hasStaticEvents)
		{
			sb.AppendXmlSummary($"Raise static events on the mock of {classReference}.", "\t");
			sb.Append("\tinternal interface IMockStaticRaiseOn").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineRaiseInterface(sb, @class, MemberType.Static);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion IMockStaticRaiseOnXXX

		#region IMockVerifyForXXX

		sb.AppendXmlSummary($"Verify interactions with the mock of {classReference}.", "\t");
		sb.Append("\tinternal interface IMockVerifyFor").Append(name);
		if (!hasStaticMembers && !hasStaticEvents)
		{
			sb.Append(" : global::Mockolate.Verify.IMockVerify<").Append(@class.ClassFullName).Append('>').AppendLine();
		}
		else
		{
			sb.AppendLine();
		}

		sb.Append("\t{").AppendLine();
		
		DefineVerifyInterface(sb, @class, $"IMockVerifyFor{name}", MemberType.Public, hasOverloadResolutionPriority, useUnionOverloads);
		
		sb.Append("\t}").AppendLine();

		#endregion IMockVerifyForXXX

		#region IMockProtectedVerifyForXXX

		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Verify protected interactions with the mock of {classReference}.",
				"\t");
			sb.Append("\tinternal interface IMockProtectedVerifyFor").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			DefineVerifyInterface(sb, @class, $"IMockProtectedVerifyFor{name}", MemberType.Protected,
				hasOverloadResolutionPriority, useUnionOverloads);
			sb.Append("\t}").AppendLine();
		}

		#endregion IMockProtectedVerifyForXXX

		#region IMockStaticVerifyForXXX

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Verify static interactions with the mock of {classReference}.",
				"\t");
			sb.Append("\tinternal interface IMockStaticVerifyFor").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			DefineVerifyInterface(sb, @class, $"IMockStaticVerifyFor{name}", MemberType.Static,
				hasOverloadResolutionPriority, useUnionOverloads);
			sb.Append("\t}").AppendLine();
		}

		#endregion IMockStaticVerifyForXXX

		DefineGenericFacadeInterfaces(sb, @class, name, classReference, hasOverloadResolutionPriority);

		#endregion Mock Interfaces

		sb.Append('}').AppendLine();
		
		#endregion Mock
		
		#region MockForXXXExtensions

		sb.AppendXmlSummary($"Mock extensions for {classReference}.", "");
		AppendDebuggerNonUserCode(sb, "");
		sb.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("internal static partial class MockExtensionsFor").Append(name).AppendLine();
		sb.Append('{').AppendLine();

		#region Mock Type extensions

		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		if (@class is MockClass { IsDiscovered: true, })
		{
			// A discovered mock is generated in one spelling, so its receiver is nullable-oblivious to accept every
			// spelling that differs only in nullable annotations without a warning.
			sb.Append("#nullable disable annotations").AppendLine();
			sb.Append("\textension(").Append(@class.ClassFullName).Append(" mock)").AppendLine();
			sb.Append("#nullable enable annotations").AppendLine();
		}
		else
		{
			sb.Append("\textension(").Append(@class.ClassFullName).Append(" mock)").AppendLine();
		}

		sb.Append("\t{").AppendLine();

		#region Mock Property

		string mockPropertyName = CreateUniquePropertyName(@class, "Mock");

		List<string> mockPropertyRemarks =
		[
			$"The accessor is the bridge between the strongly-typed instance of {classReference} returned by <c>CreateMock(...)</c> and the underlying mock registry where setups and recorded interactions live.",
			"Through it you can:",
			"<list type=\"bullet\">",
			"  <item><description><c>Setup</c> - configure how members respond when invoked (<c>Returns</c>, <c>Throws</c>, <c>Do</c>, <c>InitializeWith</c>, ...).</description></item>",
			"  <item><description><c>Verify</c> - assert how often (and in which order) members were invoked.</description></item>",
		];
		if (hasEvents)
		{
			mockPropertyRemarks.Add("  <item><description><c>Raise</c> - trigger events declared on the mocked type.</description></item>");
		}

		if (hasProtectedMembers || hasProtectedEvents)
		{
			mockPropertyRemarks.Add("  <item><description><c>SetupProtected</c> / <c>VerifyProtected</c> / <c>RaiseProtected</c> - target <see langword=\"protected\" /> members on class mocks.</description></item>");
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			mockPropertyRemarks.Add("  <item><description><c>SetupStatic</c> / <c>VerifyStatic</c> / <c>RaiseStatic</c> - target <see langword=\"static\" /> members on interface mocks.</description></item>");
		}

		if (hasGenericMethods || hasProtectedGenericMethods)
		{
			mockPropertyRemarks.Add("  <item><description><c>SetupGeneric</c> / <c>VerifyGeneric</c> (and <c>SetupProtectedGeneric</c> / <c>VerifyProtectedGeneric</c>) - target generic methods for any type argument.</description></item>");
		}

		mockPropertyRemarks.Add("  <item><description><c>InScenario</c> / <c>TransitionTo</c> - scope setups and behavior to a named scenario and switch between scenarios.</description></item>");
		mockPropertyRemarks.Add("  <item><description><c>Monitor</c>, <c>TrackCallOrder</c>, <c>ClearAllInteractions</c>, <c>VerifyThatAllInteractionsAreVerified</c>, <c>VerifyThatAllSetupsAreUsed</c> - manage recorded interactions.</description></item>");
		mockPropertyRemarks.Add("  <item><description><c>VerifySetup</c> - verify how often a specific setup matched.</description></item>");
		mockPropertyRemarks.Add("</list>");

		sb.AppendXmlSummary($"Gets the mock accessor for {classReference} - the entry point for configuring setups, verifying interactions and raising events.");
		sb.AppendXmlRemarks(mockPropertyRemarks.ToArray());
		sb.AppendXmlException("global::Mockolate.Exceptions.MockException", $"The instance is not a Mockolate-generated mock of {classReference}.");
		sb.Append("\t\tpublic global::Mockolate.Mock.IMockFor").Append(name).Append(' ').Append(mockPropertyName).AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tif (mock is global::Mockolate.Mock.IMockFor").Append(name).Append(" mockInterface)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\treturn mockInterface;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The subject is no mock.\");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		#endregion Mock Property

		#region CreateMock

		string createMockReturns = $"A new mock instance of {classReference}.";
		List<string> createMockRemarks =
		[
			$"The returned instance is a strongly-typed mock generated at compile time - it implements {classReference} and exposes the Mockolate surface through <c>.Mock</c>:",
			"<list type=\"bullet\">",
			"  <item><description><c>.Mock.Setup</c> configures how members respond (<c>Returns</c>, <c>Throws</c>, <c>Do</c>, <c>InitializeWith</c>, sequences, callbacks).</description></item>",
			"  <item><description><c>.Mock.Verify</c> asserts how often and in which order members were invoked.</description></item>",
		];
		if (hasEvents)
		{
			createMockRemarks.Add("  <item><description><c>.Mock.Raise</c> triggers events declared on the mocked type.</description></item>");
		}

		createMockRemarks.Add("</list>");
		createMockRemarks.Add("With the default behavior, un-configured members return <c>default</c> values (empty collections / strings, completed tasks, <see langword=\"null\" /> otherwise) and base-class implementations are invoked for class mocks. Use one of the overloads that accepts a <see cref=\"global::Mockolate.MockBehavior\" /> to customize this (for example to make un-configured calls throw or to skip the base class).");
		createMockRemarks.Add("Overloads allow you to additionally pass constructor parameters (for class mocks), apply an initial <c>setup</c> callback before the instance is returned, or combine both.");

		sb.AppendXmlSummary($"Creates a new mock of {classReference} with the default <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlRemarks(createMockRemarks.ToArray());
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock()").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, null, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of {classReference} with the default <see cref=\"global::Mockolate.MockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::System.Action<").Append(setupType).Append("> setup)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, setup, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of {classReference} with the given <paramref name=\"mockBehavior\" />.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(mockBehavior, null, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of {classReference} with the given <paramref name=\"mockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior, global::System.Action<").Append(setupType).Append("> setup)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(mockBehavior, setup, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		if (hasParameterizedConstructor)
		{
			sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given <paramref name=\"constructorParameters\" /> to invoke the base-class constructor.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Without them, a constructor that can be called without arguments is used, or else the constructor with the most parameters is filled with mocks and default values.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(null, null, constructorParameters);").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given <paramref name=\"mockBehavior\" /> and <paramref name=\"constructorParameters\" />.");
			sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Without them, a constructor that can be called without arguments is used, or else the constructor with the most parameters is filled with mocks and default values.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior, object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(mockBehavior, null, constructorParameters);").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Creates a new mock of {classReference} applying the given <paramref name=\"setup\" /> immediately, using the given <paramref name=\"constructorParameters\" />.");
			sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
			sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Without them, a constructor that can be called without arguments is used, or else the constructor with the most parameters is filled with mocks and default values.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::System.Action<").Append(setupType).Append("> setup, object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(null, setup, constructorParameters);").AppendLine();
			sb.AppendLine();

			AppendTypedCreateMockOverloads(sb, @class, constructors!.Value, setupType, classReference, createMockReturns);
		}

		sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given <paramref name=\"mockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately, using the given <paramref name=\"constructorParameters\" />.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup, or <see langword=\"null\" /> for <c>MockBehavior.Default</c>.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned, or <see langword=\"null\" /> to skip.");
		sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor, or <see langword=\"null\" /> to use a constructor that can be called without arguments.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\t").Append(hasParameterizedConstructor ? "public" : "private").Append(" static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior? mockBehavior, global::System.Action<").Append(setupType).Append(">? setup, object?[]? constructorParameters)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (mockBehavior is not null)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tIMockBehaviorAccess mockBehaviorAccess = (global::Mockolate.IMockBehaviorAccess)mockBehavior;").AppendLine();
		sb.Append("\t\t\t\tif (mockBehaviorAccess.TryGet<global::System.Action<").Append(setupType).Append(">?>(out var additionalSetup))").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tif (setup is null)").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\tsetup = additionalSetup;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\t\telse").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\tvar originalSetup = setup;").AppendLine();
		sb.Append("\t\t\t\t\t\tsetup = s => { additionalSetup.Invoke(s); originalSetup.Invoke(s); };").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		if (!@class.IsInterface && !hasStaticMembers)
		{
			sb.Append("\t\t\t\tif (constructorParameters is null && mockBehaviorAccess.TryGetConstructorParameters<").Append(@class.ClassFullName).Append(">(out object?[]? parameters))").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\tconstructorParameters = parameters;").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();
		sb.AppendLine();

		if (@class is { ClassFullName: "global::System.Net.Http.HttpClient", })
		{
			sb.Append("\t\t\tglobal::Mockolate.MockBehavior effectiveBehavior = mockBehavior ?? global::Mockolate.MockBehavior.Default;").AppendLine();
			AppendUseAutoMockFactory(sb, "\t\t\t", "effectiveBehavior", autoMocking);
			sb.Append("\t\t\tglobal::Mockolate.Internals.MockRegistry mockRegistry = new global::Mockolate.Internals.MockRegistry(effectiveBehavior, global::Mockolate.Mock.").Append(name).Append(".CreateInteractions(effectiveBehavior), constructorParameters);").AppendLine();
			sb.Append("\t\t\tif (constructorParameters is null)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tconstructorParameters = [new global::Mockolate.Mock.HttpMessageHandler(mockRegistry),];").AppendLine();
			sb.Append("\t\t\t\tmockRegistry = new global::Mockolate.Internals.MockRegistry(mockRegistry, constructorParameters);").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\telse if (constructorParameters.Length > 0 && constructorParameters[0] is global::Mockolate.Mock.HttpMessageHandler && constructorParameters[0] is global::Mockolate.IMock httpMessageHandlerMock)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (mockBehavior is not null && httpMessageHandlerMock.MockRegistry.Behavior != mockBehavior)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\tthrow new global::Mockolate.Exceptions.MockException($\"Mock of type 'System.Net.Http.HttpClient' cannot be created with behavior '{mockBehavior}' because it shares its mock registry with a mock of type 'System.Net.Http.HttpMessageHandler' that has behavior '{httpMessageHandlerMock.MockRegistry.Behavior}'.\");").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\tmockRegistry = new global::Mockolate.Internals.MockRegistry(httpMessageHandlerMock.MockRegistry, constructorParameters);").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		}
		else
		{
			sb.Append("\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
			AppendUseAutoMockFactory(sb, "\t\t\t", "mockBehavior", autoMocking);
			sb.Append("\t\t\tglobal::Mockolate.Internals.MockRegistry mockRegistry = new global::Mockolate.Internals.MockRegistry(mockBehavior, global::Mockolate.Mock.").Append(name).Append(".MemberCount, constructorParameters);").AppendLine();
		}

		sb.Append("\t\t\treturn CreateMockInstance(mockRegistry, constructorParameters, setup);").AppendLine();
		sb.Append("\t\t}").AppendLine();

		sb.AppendLine();
		sb.Append("\t\tprivate static ").Append(@class.ClassFullName).Append(" CreateMockInstance(global::Mockolate.Internals.MockRegistry mockRegistry, object?[]? constructorParameters, global::System.Action<").Append(setupType).Append(">? setup)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		if (!@class.IsInterface && constructors?.Count > 0)
		{
			sb.Append("\t\t\tif (constructorParameters is null || constructorParameters.Length == 0)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			if (@class is MockClass { ConstructorWithoutArguments: { } constructorWithoutArguments, })
			{
				sb.Append("\t\t\t\tglobal::Mockolate.Mock.").Append(name).Append(".MockRegistryProvider.Value = mockRegistry;").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.MockExtensionsFor").Append(name).Append(".MockSetup? setupTarget = null;").AppendLine();
				sb.Append("\t\t\t\tif (setup is not null)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tsetupTarget ??= new(mockRegistry);").AppendLine();
				sb.Append("\t\t\t\t\tsetup.Invoke(setupTarget);").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				string arguments = AppendArgumentsWithoutConstructorParameters(sb, "\t\t\t\t", constructors.Value, constructorWithoutArguments);
				sb.Append("\t\t\t\treturn new global::Mockolate.Mock.").Append(name).Append("(mockRegistry").Append(arguments).Append(");").AppendLine();
			}
			else if (@class is MockClass { ConstructorWithDependencies: { } constructorWithDependencies, ConstructorDependencies: { } dependencies, })
			{
				string arguments = AppendConstructorDependencies(sb, "\t\t\t\t",
					constructors.Value.AsArray()[constructorWithDependencies], dependencies, @class.DisplayString);
				sb.Append("\t\t\t\tmockRegistry = new global::Mockolate.Internals.MockRegistry(mockRegistry, new object?[] { ").Append(arguments).Append(", });").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.Mock.").Append(name).Append(".MockRegistryProvider.Value = mockRegistry;").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.MockExtensionsFor").Append(name).Append(".MockSetup? setupTarget = null;").AppendLine();
				sb.Append("\t\t\t\tif (setup is not null)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tsetupTarget ??= new(mockRegistry);").AppendLine();
				sb.Append("\t\t\t\t\tsetup.Invoke(setupTarget);").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				sb.Append("\t\t\t\treturn new global::Mockolate.Mock.").Append(name).Append("(mockRegistry, ").Append(arguments).Append(");").AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"No parameterless constructor found for '").Append(@class.DisplayString).Append("'. Please provide constructor parameters.\");").AppendLine();
			}

			sb.Append("\t\t\t}").AppendLine();
			int constructorIndex = 0;
			bool useTryCast = false;
			bool useTryCastWithDefaultValue = false;
			foreach (EquatableArray<MethodParameter> constructorParameters in constructors.Value.Select(constructor => constructor.Parameters))
			{
				constructorIndex++;
				// A ref struct cannot be passed in the object array of constructor parameters.
				if (constructorParameters.Any(parameter => parameter.Type.IsRefStruct))
				{
					continue;
				}

				int requiredParameters = constructorParameters.Count(c => !c.HasExplicitDefaultValue);
				if (requiredParameters < constructorParameters.Count)
				{
					sb.Append("\t\t\telse if (constructorParameters.Length >= ").Append(requiredParameters).Append(" && constructorParameters.Length <= ").Append(constructorParameters.Count);
				}
				else
				{
					sb.Append("\t\t\telse if (constructorParameters.Length == ").Append(constructorParameters.Count);
				}

				int constructorParameterIndex = 0;
				foreach (MethodParameter parameter in constructorParameters)
				{
					useTryCast = useTryCast || !parameter.HasExplicitDefaultValue;
					useTryCastWithDefaultValue = useTryCastWithDefaultValue || parameter.HasExplicitDefaultValue;
					sb.AppendLine().Append("\t\t\t    && ")
						.Append(parameter.HasExplicitDefaultValue ? $"TryCastWithDefaultValue<{parameter.Type.Fullname}>" : "TryCast")
						.Append("(constructorParameters, ")
						.Append(constructorParameterIndex++)
						.Append(parameter.HasExplicitDefaultValue ? $", {parameter.ExplicitDefaultValue}" : "")
						.Append(", mockRegistry.Behavior, out ").Append(parameter.Type.Fullname).Append(" c")
						.Append(constructorIndex)
						.Append('p')
						.Append(constructorParameterIndex).Append(')');
				}

				sb.Append(')').AppendLine();
				sb.Append("\t\t\t{").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.Mock.").Append(name).Append(".MockRegistryProvider.Value = mockRegistry;").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.MockExtensionsFor").Append(name).Append(".MockSetup? setupTarget = null;").AppendLine();
				sb.Append("\t\t\t\tif (setup is not null)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tsetupTarget ??= new(mockRegistry);").AppendLine();
				sb.Append("\t\t\t\t\tsetup.Invoke(setupTarget);").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				sb.Append("\t\t\t\treturn new global::Mockolate.Mock.").Append(name).Append("(mockRegistry");
				for (int i = 1; i <= constructorParameters.Count; i++)
				{
					sb.Append(", ").Append('c').Append(constructorIndex).Append('p').Append(i);
				}

				sb.Append(");").AppendLine();
				sb.Append("\t\t\t}").AppendLine();
			}

			sb.Append("\t\t\telse").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockException($\"Could not find any constructor for '").Append(@class.DisplayString).Append("' that matches the {constructorParameters.Length} given parameters ({string.Join(\", \", constructorParameters)}).\");").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
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
		}
		else
		{
			sb.Append("\t\t\tvar value = new global::Mockolate.Mock.").Append(name).Append("(mockRegistry);").AppendLine();
			sb.Append("\t\t\tif (setup is not null)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tsetup.Invoke(value);").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\treturn value;").AppendLine();
		}

		sb.Append("\t\t}").AppendLine();

		#endregion CreateMock
		
		#region Wrapping

		sb.AppendXmlSummary("Creates a mock that wraps the given <paramref name=\"instance\" />.");
		sb.AppendXmlRemarks("Public members on the mock forward to <paramref name=\"instance\" /> unless overridden by a setup; protected members still go through the base-class implementation. All forwarded interactions are recorded and can be verified the same as on a plain mock.");
		sb.AppendXmlParam("instance", "The real object whose calls should be forwarded. Must not be <see langword=\"null\" />.");
		sb.AppendXmlReturns($"A new mock of {classReference} that delegates to <paramref name=\"instance\" />.");
		sb.Append("\t\tpublic ").Append(@class.ClassFullName).Append(" Wrapping(").Append(@class.ClassFullName).Append(" instance)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (mock is global::Mockolate.IMock mockInterface)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tglobal::Mockolate.Internals.MockRegistry wrappingRegistry = new global::Mockolate.Internals.MockRegistry(mockInterface.MockRegistry, instance);").AppendLine();
		sb.Append("\t\t\t\twrappingRegistry = new global::Mockolate.Internals.MockRegistry(wrappingRegistry, global::Mockolate.Mock.").Append(name).Append(".CreateInteractions(wrappingRegistry.Behavior));").AppendLine();
		sb.Append("\t\t\t\treturn CreateMockInstance(wrappingRegistry, mockInterface.MockRegistry.ConstructorParameters, null);").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The subject is no mock.\");").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
		
		#endregion Wrapping

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		if (hasGenericMethods)
		{
			sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
			sb.Append("\textension(global::Mockolate.Monitor.MockMonitor<global::Mockolate.Mock.IMockVerifyFor").Append(name).Append("> monitor)").AppendLine();
			sb.Append("\t{").AppendLine();
			sb.AppendXmlSummary($"Asserts how often generic methods of the mock of {classReference} were invoked with any type argument while the monitor was running.");
			sb.Append("\t\tpublic global::Mockolate.Mock.").Append(GetGenericVerifyFacadeName(MemberType.Public, name)).Append(" VerifyGeneric").AppendLine();
			sb.Append("\t\t\t=> (global::Mockolate.Mock.").Append(GetGenericVerifyFacadeName(MemberType.Public, name)).Append(")monitor.Verify;").AppendLine();
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion Mock Type extensions

		#region MockBehavior extensions

		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t{").AppendLine();

		#region Initialize
		
		sb.AppendXmlSummary("Initializes mocks of type <typeparamref name=\"T\" /> with the given <paramref name=\"setup\" />.");
		sb.AppendXmlRemarks("The <paramref name=\"setup\" /> is applied to the mock before the constructor is executed. Calling <c>Initialize</c> again overlays additional setups on top of any previously registered ones.");
		sb.AppendXmlTypeParam("T", $"The mockable type derived from {classReference} that this setup should apply to.");
		sb.AppendXmlParam("setup", "Callback invoked when a new mock of <typeparamref name=\"T\" /> is created.");
		sb.AppendXmlReturns("A new <see cref=\"global::Mockolate.MockBehavior\" /> with the registered initializer. The original instance is unchanged.");
		sb.Append("\t\tpublic global::Mockolate.MockBehavior Initialize<T>(global::System.Action<").Append(setupType).Append("> setup)").AppendLine();
		sb.Append("\t\t\twhere T : ").Append(@class.ClassFullName).AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tvar behaviorAccess = (global::Mockolate.IMockBehaviorAccess)behavior;").AppendLine();
		sb.Append("\t\t\treturn behaviorAccess.Set(setup);").AppendLine();
		sb.Append("\t\t}").AppendLine();

		#endregion Initialize

		sb.Append("\t}").AppendLine();
		AppendLoggerMockExtensions(sb, @class, name);

		#endregion MockBehavior extensions

		#region Setup helpers

		if (!@class.IsInterface && constructors?.Count > 0)
		{
			#region IMockSetupInitializationForXXX
			string protectedName = @class.GetUniqueName("Protected", "SetupProtected");
			if (hasProtectedMembers)
			{
				sb.Append("\tinternal interface IMockSetupInitializationFor").Append(name).Append(" : global::Mockolate.Mock.IMockSetupFor").Append(name).AppendLine();
				sb.Append("\t{").AppendLine();
				sb.AppendXmlSummary("Setup protected members");
				sb.Append("\t\tglobal::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(' ').Append(protectedName).Append(" { get; }").AppendLine();
				sb.Append("\t}").AppendLine();
			}
			#endregion IMockSetupInitializationForXXX

			sb.AppendLine();
			
			#region MockSetup
			AppendDebuggerNonUserCode(sb, "\t");
			sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
			sb.Append("\tinternal sealed class MockSetup(global::Mockolate.Internals.MockRegistry mockRegistry) : global::Mockolate.Mock.IMockSetupFor").Append(name);
			if (hasProtectedMembers)
			{
				sb.Append(", global::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(", IMockSetupInitializationFor").Append(name);
			}

			AppendAdapterAccessDeclaration(sb, [@class,]);
			sb.AppendLine();
			sb.Append("\t{").AppendLine();
			if (hasProtectedMembers)
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\tglobal::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(" IMockSetupInitializationFor").Append(name).Append('.').Append(protectedName).Append(" => this;").AppendLine();
			}

			sb.Append("\t\tprivate global::Mockolate.Internals.MockRegistry ").Append(mockRegistryName).Append(" { get; } = mockRegistry;").AppendLine();
			AppendAdapterAccessImplementation(sb, [@class,], mockRegistryName, "\"\"", memberIds, memberIdPrefix);
			sb.AppendLine();
			sb.Append("\t\t#region IMockSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockSetupFor{name}", MemberType.Public, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockSetupFor").Append(name).AppendLine();
			if (hasProtectedMembers)
			{
				sb.AppendLine();
				sb.Append("\t\t#region IMockProtectedSetupFor").Append(name).AppendLine();
				sb.AppendLine();
				
				ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockProtectedSetupFor{name}", MemberType.Protected, memberIds, memberIdPrefix, hasOverloadResolutionPriority, useUnionOverloads: useUnionOverloads);
				
				sb.Append("\t\t#endregion IMockProtectedSetupFor").Append(name).AppendLine();
			}

			sb.Append("\t}").AppendLine();
			#endregion MockSetup
		}

		#endregion Setup helpers

		sb.Append('}').AppendLine();

		#endregion MockForXXXExtensions
		
		sb.AppendLine();
		if (overridesEquals)
		{
			sb.AppendLine(RestoreEqualsWithoutGetHashCodeWarning);
		}

		sb.AppendLine("#nullable disable annotations");
		return sb.ToString();
	}

	private const string DisableEqualsWithoutGetHashCodeWarning =
		"#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()";

	private const string RestoreEqualsWithoutGetHashCodeWarning = "#pragma warning restore CS0659";

	/// <summary>
	///     True when the mock overrides <c>Equals(object)</c> for a mocked member, for which the compiler can report
	///     CS0659 on the mock. The warning is suppressed instead of adding an override of <c>GetHashCode()</c>, because
	///     the mocked class can seal it, and even an emitted <c>GetHashCode()</c> override can target a member that hides
	///     <see cref="object.GetHashCode()" />.
	/// </summary>
	private static bool OverridesEquals(Class @class)
		=> @class.AllMethods().Any(method => method.ExplicitImplementation is null && method.IsEquals());

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementMockForInterface(StringBuilder sb, string mockRegistryName, string name,
		bool hasEvents, bool hasProtectedMembers, bool hasProtectedEvents, bool hasStaticMembers, bool hasStaticEvents,
		string[]? adapterIgnoredNames, string? adapterClearHook, string? constructorArgumentsName = null,
		string? classDisplay = null, bool hasGenericMethods = false, bool hasProtectedGenericMethods = false)
	{
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tIMockSetupFor").Append(name).Append(" IMockFor").Append(name).Append(".Setup").AppendLine();
		sb.Append("\t\t\t=> this;").AppendLine();

		if (hasProtectedMembers)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" IMockFor").Append(name).Append(".SetupProtected").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();

			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" global::Mockolate.MockExtensionsFor").Append(name).Append(".IMockSetupInitializationFor").Append(name).Append(".Protected").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasStaticMembers)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockStaticSetupFor").Append(name).Append(" IMockFor").Append(name).Append(".SetupStatic").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasGenericMethods)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockGenericSetupFor").Append(name).Append(" IMockFor").Append(name).Append(".SetupGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockGenericVerifyFor").Append(name).Append(" IMockFor").Append(name).Append(".VerifyGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasProtectedGenericMethods)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\t").Append(GetGenericSetupFacadeName(MemberType.Protected, name)).Append(" IMockFor").Append(name).Append(".SetupProtectedGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\t").Append(GetGenericVerifyFacadeName(MemberType.Protected, name)).Append(" IMockFor").Append(name).Append(".VerifyProtectedGeneric").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tIMockInScenarioFor").Append(name).Append(" IMockFor").Append(name).Append(".InScenario(string scenario)").AppendLine();
		sb.Append("\t\t\t=> new MockInScenarioFor").Append(name).Append("(this.").Append(mockRegistryName).Append(", scenario);").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tIMockFor").Append(name).Append(" IMockFor").Append(name).Append(".InScenario(string scenario, global::System.Action<IMockInScenarioFor").Append(name).Append("> setup)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tsetup.Invoke(new MockInScenarioFor").Append(name).Append("(this.").Append(mockRegistryName).Append(", scenario));").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tIMockFor").Append(name).Append(" IMockFor").Append(name).Append(".TransitionTo(string scenario)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".TransitionTo(scenario);").AppendLine();
		sb.Append("\t\t\treturn this;").AppendLine();
		sb.Append("\t\t}").AppendLine();

		if (hasEvents)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockRaiseOn").Append(name).Append(" IMockFor").Append(name).Append(".Raise").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasProtectedEvents)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockProtectedRaiseOn").Append(name).Append(" IMockFor").Append(name).Append(".RaiseProtected").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasStaticEvents)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockStaticRaiseOn").Append(name).Append(" IMockFor").Append(name).Append(".RaiseStatic").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tIMockVerifyFor").Append(name).Append(" IMockFor").Append(name).Append(".Verify").AppendLine();
		sb.Append("\t\t\t=> this;").AppendLine();

		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockProtectedVerifyFor").Append(name).Append(" IMockFor").Append(name).Append(".VerifyProtected").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tIMockStaticVerifyFor").Append(name).Append(" IMockFor").Append(name).Append(".VerifyStatic").AppendLine();
			sb.Append("\t\t\t=> this;").AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<IMockVerifyFor").Append(name).Append("> IMockFor").Append(name).Append(".VerifySetup(global::Mockolate.Setup.IMethodSetup setup)").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".Method<IMockVerifyFor").Append(name).Append(">(this, setup);").AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tbool IMockFor").Append(name).Append(".VerifyThatAllInteractionsAreVerified()").AppendLine();
		if (adapterIgnoredNames is not null)
		{
			sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".AreAllInteractionsVerified(");
			for (int i = 0; i < adapterIgnoredNames.Length; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				sb.Append('"').Append(adapterIgnoredNames[i]).Append('"');
			}

			sb.Append(");").AppendLine();
		}
		else
		{
			sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".Interactions.GetUnverifiedInteractions().Count == 0;").AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tbool IMockFor").Append(name).Append(".VerifyThatAllSetupsAreUsed()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".GetUnusedSetups(this.").Append(mockRegistryName).Append(".Interactions).Count == 0;").AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tvoid IMockFor").Append(name).Append(".ClearAllInteractions()").AppendLine();
		if (adapterClearHook is not null)
		{
			sb.Append("\t\t\t=> ").Append(adapterClearHook).Append("(this.").Append(mockRegistryName).Append(");").AppendLine();
		}
		else
		{
			sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".ClearAllInteractions();").AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append("> IMockFor").Append(name).Append(".Monitor()").AppendLine();
		sb.Append("\t\t\t=> new global::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append(">(this.")
			.Append(mockRegistryName).Append(".Interactions, interactions => new VerifyMonitor").Append(name)
			.Append("(new global::Mockolate.Internals.MockRegistry(this.").Append(mockRegistryName).Append(", interactions)));")
			.AppendLine();
		AppendTrackCallOrderImplementation(sb, name, mockRegistryName);
		if (constructorArgumentsName is not null)
		{
			string recorded = $"(this.{constructorArgumentsName} ?? throw new global::Mockolate.Exceptions.MockException(\"The mock of {classDisplay} was created without constructor arguments.\"))";
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tT IMockFor").Append(name).Append(".ConstructorArgument<T>()").AppendLine();
			sb.Append("\t\t\t=> ").Append(recorded).Append(".Get<T>();").AppendLine();
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tT IMockFor").Append(name).Append(".ConstructorArgument<T>(string parameterName)").AppendLine();
			sb.Append("\t\t\t=> ").Append(recorded).Append(".Get<T>(parameterName);").AppendLine();
		}

		sb.AppendLine();
	}
#pragma warning restore S107 // Methods should not have too many parameters

	#region Mock Helpers

	private static void AppendTrackCallOrderDeclaration(StringBuilder sb)
	{
		sb.AppendXmlSummary("Tracks the order of the calls across this mock and the <paramref name=\"other\" /> mocks until the returned scope is disposed, so that <c>Then</c> can verify the order of calls on different mocks.");
		sb.AppendXmlRemarks(["Only the calls while the scope is not disposed are compared, e.g. <c>using (sut.Mock.TrackCallOrder(other.Mock)) { ... }</c>.",]);
		sb.AppendXmlParam("other", "The <c>Mock</c> property of another mock.");
		sb.AppendXmlParam("more", "The <c>Mock</c> properties of further mocks.");
		sb.AppendXmlReturns("A scope that ends the tracking when it is disposed.");
		sb.AppendXmlException("global::Mockolate.Exceptions.MockException", "The call order of one of the mocks cannot be tracked or is already tracked.");
		sb.Append("\t\tglobal::System.IDisposable TrackCallOrder(global::Mockolate.IMock other, params global::Mockolate.IMock[] more);").AppendLine();
	}

	private static void AppendTrackCallOrderImplementation(StringBuilder sb, string name, string mockRegistryName)
	{
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::System.IDisposable IMockFor").Append(name).Append(".TrackCallOrder(global::Mockolate.IMock other, params global::Mockolate.IMock[] more)").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".TrackCallOrder(this, other, more);").AppendLine();
	}

	/// <summary>
	///     Declares the field that holds the <c>ConstructorArguments</c> of a class mock and returns its name.
	/// </summary>
	private static string AppendConstructorArgumentsField(StringBuilder sb, Class @class)
	{
		string constructorArgumentsName = @class.GetUniqueName("MockolateConstructorArguments");
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tprivate readonly global::Mockolate.ConstructorArguments? ").Append(constructorArgumentsName).Append(';').AppendLine();
		return constructorArgumentsName;
	}

	/// <summary>
	///     Overrides <see cref="object.ToString()" /> of a mock that does not mock it, so that tools which format a mock,
	///     like the display names of a test framework, do not read its mocked properties instead.
	/// </summary>
	private static void AppendObjectToString(StringBuilder sb, string description)
	{
		sb.AppendLine();
		sb.Append("\t\t/// <inheritdoc cref=\"object.ToString()\" />").AppendLine();
		sb.Append("\t\tpublic override string ToString()").AppendLine();
		sb.Append("\t\t\t=> \"").Append(description).Append("\";").AppendLine();
	}

	/// <summary>
	///     Whether the type declares a member that a <see cref="AppendObjectToString" /> override would clash with.
	/// </summary>
	private static bool DeclaresToString(Class @class)
		=> @class.AllMethods().Any(method => method is { Name: "ToString", Parameters.Count: 0, }) ||
		   @class.AllProperties().Any(property => string.Equals(property.Name, "ToString", StringComparison.Ordinal)) ||
		   @class.AllEvents().Any(@event => string.Equals(@event.Name, "ToString", StringComparison.Ordinal));

	/// <summary>
	///     Whether the mock of the class records the arguments of at least one of its <paramref name="constructors" />,
	///     which <c>ConstructorArgument</c> returns.
	/// </summary>
	private static bool HasConstructorArguments(Class @class, EquatableArray<Method>? constructors)
		=> !@class.IsInterface && constructors?.Any(CanRecordConstructorArguments) == true;

	/// <summary>
	///     The arguments of a constructor whose parameters can all be boxed into an <see langword="object" />.
	/// </summary>
	private static bool CanRecordConstructorArguments(Method constructor)
		=> constructor.Parameters.Count > 0 &&
		   constructor.Parameters.All(parameter => !parameter.Type.IsRefStruct && parameter.RefKind != RefKind.Out);

	/// <summary>
	///     The names of the generated field that records the arguments of the constructors and of the static field that
	///     describes the parameters of the <paramref name="constructor" /> with the <paramref name="index" />.
	/// </summary>
	/// <remarks>
	///     The constructor uses the static field without qualification, so its name must differ from the members of the
	///     class and from the parameters of the constructor.
	/// </remarks>
	private static (string Field, string Signature)? GetConstructorRecording(Class @class, string? constructorArgumentsName,
		Method constructor, int index)
	{
		if (constructorArgumentsName is null)
		{
			return null;
		}

		string signature = FormattableString.Invariant($"MockolateConstructorSignature{index}");
		int suffix = 0;
		while (@class.HasReservedName(signature) ||
		       constructor.Parameters.Any(parameter => string.Equals(parameter.Name.TrimStart('@'), signature, StringComparison.Ordinal)))
		{
			signature = FormattableString.Invariant($"MockolateConstructorSignature{index}_{++suffix}");
		}

		return (constructorArgumentsName, signature);
	}

	private static void AppendMockSubject_BaseClassConstructor(StringBuilder sb, string mockRegistryName, string name,
		Method constructor, Class @class, bool setsMockRegistryProvider = false,
		(string Field, string Signature)? recording = null)
	{
		string mockRegistry = CreateUniqueParameterName(constructor.Parameters, "mockRegistry");
		bool records = recording is not null && CanRecordConstructorArguments(constructor);
		if (records)
		{
			// The parameters are described once, so that a construction only allocates the values.
			sb.Append("\t\tprivate static readonly global::Mockolate.Internals.ConstructorSignature ")
				.Append(recording!.Value.Signature).Append(" = new global::Mockolate.Internals.ConstructorSignature(\"")
				.Append(@class.DisplayString).Append('(')
				.Append(string.Join(", ", constructor.Parameters.Select(parameter => parameter.Type.DisplayName)))
				.Append(")\", new string[] { ")
				.Append(string.Join(", ", constructor.Parameters.Select(parameter => $"\"{parameter.Name.TrimStart('@')}\"")))
				.Append(", }, new global::System.Type[] { ")
				.Append(string.Join(", ", constructor.Parameters.Select(parameter =>
					$"typeof({(parameter.Type.IsValueType ? parameter.Type.Fullname : parameter.Type.Fullname.TrimEnd('?'))})")))
				.Append(", });").AppendLine();
			sb.AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
		sb.Append(constructor.Attributes, "\t\t");
		if (@class.HasRequiredMembers && constructor.Attributes?.Any(a => string.Equals(a.Name, "global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers", StringComparison.Ordinal)) != true)
		{
			sb.Append("\t\t[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]").AppendLine();
		}

		sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.Internals.MockRegistry ").Append(mockRegistry);
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			sb.Append(", ");
			if (parameter.IsParams)
			{
				sb.Append("params ");
			}

			sb.Append(parameter.Type.Fullname).Append(' ').Append(parameter.Name);
			if (parameter.HasExplicitDefaultValue)
			{
				sb.Append(" = ").Append(parameter.ExplicitDefaultValue);
			}
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t: base(");
		int index = 0;
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			if (index++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.Name);
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = ").Append(mockRegistry).Append(';').AppendLine();
		if (setsMockRegistryProvider)
		{
			sb.Append("\t\t\tMockRegistryProvider.Value = ").Append(mockRegistry).Append(';').AppendLine();
		}

		if (records)
		{
			sb.Append("\t\t\tthis.").Append(recording!.Value.Field).Append(" = ").Append(recording.Value.Signature)
				.Append(".Record(new object?[] { ")
				.Append(string.Join(", ", constructor.Parameters.Select(parameter => parameter.Name)))
				.Append(", });").AppendLine();
		}

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits a static helper that creates a <c>MockRegistry</c> for the members of the mock from a
	///     <c>MockBehavior</c>. Used by the generator-emitted <c>(MockBehavior)</c> constructors so
	///     subclasses of the generated mock can construct from a behavior without bypassing the MockInteractions
	///     sizing. When <paramref name="setsMockRegistryProvider" /> is true, the helper also primes the
	///     <c>MockRegistryProvider</c> AsyncLocal so virtual calls during base-class construction can resolve
	///     the registry before the chained constructor body has run.
	/// </summary>
	private static void AppendCreateRegistryFromBehavior(StringBuilder sb, string indent,
		bool setsMockRegistryProvider, AutoMockingOptions? autoMocking)
	{
		sb.Append(indent).Append("/// <summary>").AppendLine();
		sb.Append(indent).Append("///     Builds a <see cref=\"global::Mockolate.Internals.MockRegistry\" /> backed by a ").Append("<see cref=\"global::Mockolate.Internals.MockInteractions\" /> for the members of the mock, from <paramref name=\"behavior\" />.").AppendLine();
		sb.Append(indent).Append("/// </summary>").AppendLine();
		sb.Append(indent).Append("private static global::Mockolate.Internals.MockRegistry MockolateCreateRegistryFromBehavior(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		AppendUseAutoMockFactory(sb, indent + "\t", "behavior", autoMocking);
		sb.Append(indent).Append("\tglobal::Mockolate.Internals.MockRegistry registry = new global::Mockolate.Internals.MockRegistry(behavior, MemberCount);").AppendLine();
		if (setsMockRegistryProvider)
		{
			sb.Append(indent).Append("\tMockRegistryProvider.Value = registry;").AppendLine();
		}

		sb.Append(indent).Append("\treturn registry;").AppendLine();
		sb.Append(indent).Append('}').AppendLine();
	}

	/// <summary>
	///     Emits a public <c>(MockBehavior)</c> constructor on the generated mock for interface targets and class
	///     targets without detected base-class constructors. Chains via <c>: this(...)</c> to the existing
	///     <c>(MockRegistry)</c> constructor with a properly-sized registry.
	/// </summary>
	private static void AppendMockSubject_BehaviorConstructor(StringBuilder sb, string name)
	{
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
		sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t\t\t: this(MockolateCreateRegistryFromBehavior(behavior))").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits a public <c>(MockBehavior, ...baseCtorParams)</c> constructor sibling for each detected base-class
	///     constructor. The chained <c>(MockRegistry, ...baseCtorParams)</c> constructor handles base invocation;
	///     <c>MockolateCreateRegistryFromBehavior</c> primes the <c>MockRegistryProvider</c> AsyncLocal before that
	///     chain so virtual calls during base construction resolve the registry.
	/// </summary>
	private static void AppendMockSubject_BehaviorBaseClassConstructor(StringBuilder sb, string name,
		Method constructor, bool hasRequiredMembers)
	{
		string behavior = CreateUniqueParameterName(constructor.Parameters, "behavior");
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
		sb.Append(constructor.Attributes, "\t\t");
		if (hasRequiredMembers && constructor.Attributes?.Any(a => string.Equals(a.Name, "global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers", StringComparison.Ordinal)) != true)
		{
			sb.Append("\t\t[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]").AppendLine();
		}

		sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.MockBehavior ").Append(behavior);
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			sb.Append(", ");
			if (parameter.IsParams)
			{
				sb.Append("params ");
			}

			sb.Append(parameter.Type.Fullname).Append(' ').Append(parameter.Name);
			if (parameter.HasExplicitDefaultValue)
			{
				sb.Append(" = ").Append(parameter.ExplicitDefaultValue);
			}
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t: this(MockolateCreateRegistryFromBehavior(").Append(behavior).Append(')');
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			sb.Append(", ").Append(parameter.Name);
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass(StringBuilder sb, Class @class, string mockRegistryName,
		MockClass? mockClass, MemberIdTable memberIds, string memberIdPrefix, AutoMockingOptions? autoMocking,
		Dictionary<string, int>? signatureIndicesOverride = null,
		int[]? nextSignatureIndexRef = null,
		bool useTypedRecords = true,
		IReadOnlyCollection<Class>? implementedInterfaces = null)
#pragma warning restore S107
	{
		string className = @class.ClassFullName;
		sb.Append("\t\t#region ").Append(@class.DisplayString).AppendLine();
		sb.AppendLine();

		// An aliased member is served by the mocked class' own override: re-implementing it here would
		// re-map the interface slot away from that override and fork the member in two. A member that an
		// interface implemented before already inherits has its explicit implementation already.
		implementedInterfaces ??= [];
		List<Event>? mockEvents = mockClass?.AllEvents()
			.Concat(implementedInterfaces.SelectMany(implemented => implemented.AllEvents())).ToList();
		foreach (Event @event in @class.AllEvents())
		{
			if (!memberIds.IsAlias(@event) &&
			    mockEvents?.TrueForAll(e => !Event.EqualityComparer.Equals(@event, e)) != false)
			{
				AppendMockSubject_ImplementClass_AddEvent(sb, @event, mockRegistryName, className,
					mockClass is not null,
					@class.IsInterface, memberIds, memberIdPrefix, useTypedRecords);
				sb.AppendLine();
			}
		}

		List<Property>? mockProperties = mockClass?.AllProperties()
			.Concat(implementedInterfaces.SelectMany(implemented => implemented.AllProperties())).ToList();
		Dictionary<string, int> signatureIndices = signatureIndicesOverride ?? new Dictionary<string, int>();
		int[] nextSignatureIndex = nextSignatureIndexRef ?? [0,];
		foreach (Property property in @class.AllProperties())
		{
			if (!memberIds.IsAlias(property) &&
			    mockProperties?.TrueForAll(p => !Property.EqualityComparer.Equals(property, p)) != false)
			{
				int signatureIndex = -1;
				if (property is { IsIndexer: true, IndexerParameters: not null, })
				{
					string signatureKey = property.ContainingType + "::" +
					                      (property.ExplicitImplementation ?? "") + "::" +
					                      property.Type.Fullname + "->|" +
					                      string.Join("|",
						                      property.IndexerParameters.Value.Select(p
							                      => p.RefKind + " " + p.Type.Fullname));
					if (!signatureIndices.TryGetValue(signatureKey, out signatureIndex))
					{
						signatureIndex = nextSignatureIndex[0]++;
						signatureIndices[signatureKey] = signatureIndex;
					}
				}

				AppendMockSubject_ImplementClass_AddProperty(sb, property, mockRegistryName, className,
					mockClass is not null,
					@class.IsInterface, signatureIndex, memberIds, memberIdPrefix, autoMocking, useTypedRecords);
				sb.AppendLine();
			}
		}

		List<Method>? mockMethods = mockClass?.AllMethods()
			.Concat(implementedInterfaces.SelectMany(implemented => implemented.AllMethods())).ToList();
		foreach (Method method in @class.AllMethods())
		{
			if (!memberIds.IsAlias(method) &&
			    mockMethods?.TrueForAll(m => !Method.EqualityComparer.Equals(method, m)) != false)
			{
				AppendMockSubject_ImplementClass_AddMethod(sb, method, mockRegistryName, className,
					mockClass is not null,
					@class.IsInterface, @class, memberIds, memberIdPrefix, autoMocking, useTypedRecords);
				sb.AppendLine();
			}
		}

		sb.Append("\t\t#endregion ").Append(@class.DisplayString).AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass_AddEvent(StringBuilder sb, Event @event,
		string mockRegistryName, string className,
		bool explicitInterfaceImplementation, bool isClassInterface,
		MemberIdTable memberIds, string memberIdPrefix, bool useTypedRecords)
#pragma warning restore S107
	{
		string mockRegistry = @event.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		bool useTypedRecordsForEvent = useTypedRecords && IsTypedRecordingEligibleEvent(@event);
		string subscribeIdRef = memberIdPrefix + memberIds.GetEventSubscribeIdentifier(@event);
		string unsubscribeIdRef = memberIdPrefix + memberIds.GetEventUnsubscribeIdentifier(@event);
		string addCall = useTypedRecordsForEvent
			? $".AddEvent({subscribeIdRef}, "
			: ".AddEvent(";
		string removeCall = useTypedRecordsForEvent
			? $".RemoveEvent({unsubscribeIdRef}, "
			: ".RemoveEvent(";
		string backingFieldName = @event.GetBackingFieldName();
		string backingFieldAccess;
		if (@event.IsStatic)
		{
			sb.Append("\t\tprivate static readonly global::System.Threading.AsyncLocal<")
				.Append(@event.Type.Fullname.TrimEnd('?'))
				.Append("?> ").Append(backingFieldName).Append(" = new global::System.Threading.AsyncLocal<")
				.Append(@event.Type.Fullname.TrimEnd('?')).Append("?>();").AppendLine();
			backingFieldAccess = $"{backingFieldName}.Value";
		}
		else
		{
			sb.Append("\t\tprivate ").Append(@event.Type.Fullname.TrimEnd('?'))
				.Append("? ").Append(backingFieldName).Append(';').AppendLine();
			backingFieldAccess = $"this.{backingFieldName}";
		}

		sb.Append("\t\t/// <inheritdoc cref=\"").Append(@event.Cref).AppendLine("\" />");
		sb.Append(@event.Attributes, "\t\t");
		if (explicitInterfaceImplementation)
		{
			sb.Append(@event.IsStatic ? "\t\tstatic event " : "\t\tevent ").Append(@event.Type.Fullname).Append(' ')
				.Append(@event.ContainingType).Append('.').Append(@event.Name).AppendLine();
		}
		else
		{
			if (@event.ExplicitImplementation is null)
			{
				sb.Append("\t\t").Append(@event.OverrideAccessibility).Append(' ');
				if (@event.IsStatic)
				{
					sb.Append("static ");
				}

				if (!isClassInterface && @event.UseOverride)
				{
					sb.Append("override ");
				}
				else if (isClassInterface && Helpers.IsObjectMethodName(@event.Name))
				{
					sb.Append("new ");
				}

				sb.Append("event ").Append(@event.Type.Fullname).Append(' ');
			}
			else
			{
				sb.Append(@event.IsStatic ? "\t\tstatic event " : "\t\tevent ").Append(@event.Type.Fullname).Append(' ')
					.Append(@event.ExplicitImplementation).Append('.');
			}

			sb.Append(@event.Name).AppendLine();
		}

		sb.AppendLine("\t\t{");
		bool supportsWrapping = @event is { IsStatic: false, IsProtected: false, } &&
		                        !explicitInterfaceImplementation;
		// Interface members are forwarded via their declaring interface: an explicit implementation (hidden
		// via `new`, or a name collision between sibling interfaces) has a different delegate type than the
		// one reachable through the mocked interface, and an implicit member declared by several sibling
		// interfaces is ambiguous on the mocked interface (CS0229).
		string wrapsType = isClassInterface ? @event.ContainingType : className;
		bool supportsBaseForwarding = supportsWrapping && !isClassInterface && @event.UseOverride && !@event.IsAbstract;
		if (supportsWrapping)
		{
			sb.Append("\t\t\tadd").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (value is not null)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\t").Append(mockRegistry).Append(addCall).Append(@event.GetUniqueNameString()).Append(", value.Target, value.Method);").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t").Append(backingFieldAccess).Append(" += value;").AppendLine();
			sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType).Append(" wraps)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\twraps.").Append(@event.Name).Append(" += value;").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			if (supportsBaseForwarding)
			{
				sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".Behavior.SkipBaseClass)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tbase.").Append(@event.Name).Append(" += value;").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
			}

			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\tremove").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (value is not null)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\t").Append(mockRegistry).Append(removeCall).Append(@event.GetUniqueNameString()).Append(", value.Target, value.Method);").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t").Append(backingFieldAccess).Append(" -= value;").AppendLine();
			sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType).Append(" wraps)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\twraps.").Append(@event.Name).Append(" -= value;").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			if (supportsBaseForwarding)
			{
				sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".Behavior.SkipBaseClass)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tbase.").Append(@event.Name).Append(" -= value;").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
			}

			sb.Append("\t\t\t}").AppendLine();
		}
		else
		{
			sb.Append("\t\t\tadd").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (value is not null)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\t").Append(mockRegistry).Append(addCall).Append(@event.GetUniqueNameString()).Append(", value.Target, value.Method);").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t").Append(backingFieldAccess).Append(" += value;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\tremove").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (value is not null)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\t").Append(mockRegistry).Append(removeCall).Append(@event.GetUniqueNameString()).Append(", value.Target, value.Method);").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t").Append(backingFieldAccess).Append(" -= value;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
		}

		sb.AppendLine("\t\t}");
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass_AddProperty(StringBuilder sb, Property property,
		string mockRegistryName,
		string className, bool explicitInterfaceImplementation, bool isClassInterface, int signatureIndex,
		MemberIdTable memberIds, string memberIdPrefix, AutoMockingOptions? autoMocking, bool useTypedRecords)
#pragma warning restore S107
	{
		string mockRegistry = property.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		// Interface members are forwarded via their declaring interface: an explicit implementation (hidden
		// via `new`, or a name collision between sibling interfaces) has a different property type than the
		// one reachable through the mocked interface (CS0266), and an implicit member declared by several
		// sibling interfaces is ambiguous on the mocked interface (CS0229/CS0121).
		string wrapsType = isClassInterface ? property.ContainingType : className;
		bool useTypedRecordsForProperty = useTypedRecords && !property.IsIndexer && IsTypedRecordingEligibleProperty(property);
		bool useTypedRecordsForIndexer = useTypedRecords && property.IsIndexer && IsTypedRecordingEligibleIndexer(property);
		string indexerGetIdRef = property.IsIndexer
			? memberIdPrefix + memberIds.GetIndexerGetIdentifier(property)
			: string.Empty;
		string indexerSetIdRef = property.IsIndexer
			? memberIdPrefix + memberIds.GetIndexerSetIdentifier(property)
			: string.Empty;
		bool typedIndexerGet = property.IndexerParameters is not null &&
		                       UsesTypedIndexerDispatch(property.IndexerParameters.Value, useTypedRecordsForIndexer);
		bool typedIndexerSet = property.IndexerParameters is not null &&
		                       UsesTypedIndexerDispatch(property.IndexerParameters.Value, useTypedRecordsForIndexer);

		void AppendIndexerGetReturn(string indent, string accessVarName, string setupVarName)
		{
			// The registry replaces a null value with an auto-mock, but not the elements of a tuple.
			string defaultValueGenerator = new StringBuilder("static b => ")
				.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue",
					autoMock: IsTuple(property.Type) ? PropertyAutoMock(autoMocking, "b") : null).ToString();
			if (typedIndexerGet)
			{
				sb.Append(indent).Append("return ");
				AppendTypedIndexerCall(sb, mockRegistry, "GetIndexer", setupVarName, property.Type,
					property.IndexerParameters!.Value, defaultValueGenerator, signatureIndex);
				sb.Append(';').AppendLine();
				return;
			}

			sb.Append(indent).Append("return ").Append(setupVarName).Append(" is null").AppendLine();
			sb.Append(indent).Append("\t? ").Append(mockRegistry).Append(".GetIndexerFallback<")
				.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
				.Append(defaultValueGenerator).Append(", ").Append(signatureIndex).Append(')')
				.AppendLine();
			sb.Append(indent).Append("\t: ").Append(mockRegistry).Append(".ApplyIndexerSetup<")
				.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
				.Append(setupVarName).Append(", ").Append(defaultValueGenerator).Append(", ")
				.Append(signatureIndex).Append(");").AppendLine();
		}

		void AppendIndexerGetWithBaseReturn(string indent, string accessVarName, string setupVarName,
			string baseResultVarName)
		{
			sb.Append(indent).Append("return ");
			if (typedIndexerGet)
			{
				AppendTypedIndexerCall(sb, mockRegistry, "GetIndexer", setupVarName, property.Type,
					property.IndexerParameters!.Value, baseResultVarName, signatureIndex);
			}
			else
			{
				sb.Append(mockRegistry).Append(".ApplyIndexerGetter(")
					.Append(accessVarName).Append(", ").Append(setupVarName).Append(", ")
					.Append(baseResultVarName).Append(", ").Append(signatureIndex).Append(')');
			}

			sb.Append(';').AppendLine();
		}

		void AppendIndexerSetCall(string prefix, string suffix, string accessVarName, string setupVarName)
		{
			sb.Append(prefix);
			if (typedIndexerSet)
			{
				AppendTypedIndexerCall(sb, mockRegistry, "SetIndexer", setupVarName, property.Type,
					property.IndexerParameters!.Value, "value", signatureIndex);
			}
			else
			{
				sb.Append(mockRegistry).Append(".ApplyIndexerSetter<")
					.AppendTypeOrWrapper(property.Type).Append(">(")
					.Append(accessVarName).Append(", ").Append(setupVarName).Append(", value, ")
					.Append(signatureIndex).Append(')');
			}

			sb.Append(suffix).AppendLine();
		}

		string propertyGetMemberArg = useTypedRecordsForProperty
			? memberIdPrefix + memberIds.GetPropertyGetIdentifier(property) + ", "
			: string.Empty;
		string propertySetMemberArg = useTypedRecordsForProperty
			? memberIdPrefix + memberIds.GetPropertySetIdentifier(property) + ", "
			: string.Empty;
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(property.Cref).AppendLine("\" />");
		sb.Append(property.Attributes, "\t\t");
		if (explicitInterfaceImplementation)
		{
			sb.Append(property.IsStatic ? "\t\tstatic " : "\t\t").Append(property.Type.Fullname)
				.Append(' ').Append(property.ContainingType).Append('.').Append(property.IndexerParameters is not null
					? property.Name.Replace("[]",
						$"[{string.Join(", ", property.IndexerParameters.Value.Select(p => $"{p.Type.Fullname} {p.Name}"))}]")
					: property.Name).AppendLine();
		}
		else
		{
			if (property.ExplicitImplementation is null)
			{
				sb.Append("\t\t").Append(property.OverrideAccessibility).Append(' ');
				if (property.IsStatic)
				{
					sb.Append("static ");
				}

				if (!isClassInterface && property.UseOverride)
				{
					sb.Append("override ");
				}
				else if (isClassInterface && Helpers.IsObjectMethodName(property.Name))
				{
					sb.Append("new ");
				}

				sb.Append(property.Type.Fullname).Append(' ');
			}
			else
			{
				sb.Append("\t\t");

				if (property.IsStatic)
				{
					sb.Append("static ");
				}

				sb.Append(property.Type.Fullname).Append(' ').Append(property.ExplicitImplementation)
					.Append('.');
			}

			sb.Append(property.IndexerParameters is not null
				? property.Name.Replace("[]",
					$"[{string.Join(", ", property.IndexerParameters.Value.Select(p => $"{p.Type.Fullname} {p.Name}"))}]")
				: property.Name).AppendLine();
		}

		// Ref-struct-keyed indexer: the key cannot flow through IndexerGetterAccess /
		// IndexerSetterAccess (classes with field slots), so both accessors dispatch through the
		// RefStructIndexerGetterSetup / RefStructIndexerSetterSetup pipeline instead. That pipeline
		// is #if NET9_0_OR_GREATER-gated; below it the emitted body is an #error and the analyzer
		// reports the declaration.
		bool isRefStructIndexer = property is { IsIndexer: true, IndexerParameters: not null, } &&
		                          property.IndexerParameters.Value.Any(p => p.NeedsRefStructPipeline());

		// A non-span ref-struct property or indexer value has no setup surface either (see
		// Helpers.NeedsRefStructPipeline). A virtual class member still has a real implementation
		// behind it, so the accessor forwards to the wrapped instance or to base rather than
		// breaking a member that would otherwise work; interface and abstract members have nothing
		// to forward to and throw. As on the method side, the forward honours none of the behaviour
		// flags (see AppendUnsupportedRefStructReturnBody) because there is no setup to honour them
		// against.
		bool isUnsupportedRefStructValue = property.Type.NeedsRefStructPipeline();
		bool forwardsUnsupportedRefStructValue = isUnsupportedRefStructValue && !isClassInterface &&
		                                         property is { IsAbstract: false, IsStatic: false, };
		string unsupportedWrapsVarName = property.IndexerParameters is null
			? "wraps"
			: Helpers.GetUniqueLocalVariableName("wraps", property.IndexerParameters.Value);
		string unsupportedMemberAccess = property.IndexerParameters is null
			? "." + property.Name
			: "[" + FormatIndexerParametersAsNames(property.IndexerParameters.Value) + "]";

		void AppendUnsupportedRefStructValueThrow()
			=> sb.Append("\t\t\t\tthrow new global::System.NotSupportedException(\"Mockolate: ")
				.Append(property.IsIndexer
					? "indexers returning a non-span ref struct are not supported. Indexer '"
					: "properties of a non-span ref struct type are not supported. Property '")
				.Append(property.ContainingType).Append('.').Append(property.Name).Append("'.\");")
				.AppendLine();

		void AppendUnsupportedRefStructValueGetter()
		{
			if (!forwardsUnsupportedRefStructValue)
			{
				AppendUnsupportedRefStructValueThrow();
			}
			else if (property.Getter?.IsProtected == true)
			{
				sb.Append("\t\t\t\treturn base").Append(unsupportedMemberAccess).Append(';').AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType).Append(' ')
					.Append(unsupportedWrapsVarName).Append(" ? ").Append(unsupportedWrapsVarName)
					.Append(unsupportedMemberAccess).Append(" : base").Append(unsupportedMemberAccess).Append(';')
					.AppendLine();
			}
		}

		void AppendUnsupportedRefStructValueSetter()
		{
			// An init-only accessor cannot assign through `wraps`, and the surrounding pipeline
			// excludes it from wrapping for the same reason, so it keeps the throw.
			if (!forwardsUnsupportedRefStructValue || property.Setter?.IsInitOnly == true)
			{
				AppendUnsupportedRefStructValueThrow();
			}
			else if (property.Setter?.IsProtected == true)
			{
				sb.Append("\t\t\t\tbase").Append(unsupportedMemberAccess).Append(" = value;").AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType).Append(' ')
					.Append(unsupportedWrapsVarName).Append(')').AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\t").Append(unsupportedWrapsVarName).Append(unsupportedMemberAccess)
					.Append(" = value;").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				sb.Append("\t\t\t\telse").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tbase").Append(unsupportedMemberAccess).Append(" = value;").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
			}
		}

		sb.AppendLine("\t\t{");
		if (property.Getter != null)
		{
			sb.Append("\t\t\t");
			if (property.Getter.Accessibility != property.Accessibility)
			{
				sb.Append(property.Getter.OverrideAccessibility).Append(' ');
			}

			sb.AppendLine("get");
			sb.AppendLine("\t\t\t{");

			if (isUnsupportedRefStructValue)
			{
				AppendUnsupportedRefStructValueGetter();
			}
			else if (isRefStructIndexer)
			{
				AppendRefStructIndexerGetterBody(sb, property, mockRegistry);
			}
			else if (isClassInterface && !explicitInterfaceImplementation)
			{
				if (property is { IsIndexer: true, IndexerParameters: not null, })
				{
					string accessVarName =
						Helpers.GetUniqueLocalVariableName("access", property.IndexerParameters.Value);
					string setupVarName =
						Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
					string baseResultVarName =
						Helpers.GetUniqueLocalVariableName("baseResult", property.IndexerParameters.Value);
					string wrapsVarName =
						Helpers.GetUniqueLocalVariableName("wraps", property.IndexerParameters.Value);

					EmitIndexerGetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
						property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
						useTypedRecordsForIndexer ? indexerGetIdRef : null);
					sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is not ").Append(wrapsType)
						.Append(' ').Append(wrapsVarName).Append(')').AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					AppendIndexerGetReturn("\t\t\t\t\t", accessVarName, setupVarName);
					sb.Append("\t\t\t\t}").AppendLine();
					sb.Append("\t\t\t\t").AppendTypeOrWrapper(property.Type).Append(' ').Append(baseResultVarName)
						.Append(" = ").Append(wrapsVarName).Append('[')
						.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value)).Append("];")
						.AppendLine();
					AppendIndexerGetWithBaseReturn("\t\t\t\t", accessVarName, setupVarName, baseResultVarName);
				}
				else
				{
					string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
					if (useTypedRecordsForProperty)
					{
						void AppendGetPropertyCall(string indent, string? baseValueAccessor)
						{
							sb.Append(indent).Append("return ").Append(mockRegistry).Append(".GetProperty<")
								.AppendTypeOrWrapper(property.Type).Append(">(")
								.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
								.Append(", ").Append(accessFieldRef).Append(", static b => ")
								.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue",
									autoMock: PropertyAutoMock(autoMocking, "b"));
							if (baseValueAccessor is not null)
							{
								sb.Append(", ").Append(baseValueAccessor);
							}

							sb.Append(");").AppendLine();
						}

						if (!property.IsStatic)
						{
							// A local declared inside the block keeps the closure allocation off the non-wrapping path.
							sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
								.Append(')').AppendLine();
							sb.Append("\t\t\t\t{").AppendLine();
							sb.Append("\t\t\t\t\t").Append(wrapsType).Append(" wraps = (").Append(wrapsType)
								.Append(')').Append(mockRegistry).Append(".Wraps;").AppendLine();
							AppendGetPropertyCall("\t\t\t\t\t", "() => wraps." + property.Name);
							sb.Append("\t\t\t\t}").AppendLine();
							sb.AppendLine();
						}

						AppendGetPropertyCall("\t\t\t\t", baseValueAccessor: null);
					}
					else
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertyGetMemberArg).Append(accessFieldRef).Append(", () => ")
							.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue",
								autoMock: PropertyAutoMock(autoMocking, $"{mockRegistry}.Behavior"));
						if (!property.IsStatic)
						{
							sb.Append(", ").Append(mockRegistry).Append(".Wraps is not ").Append(wrapsType)
								.Append(" wraps ? null : () => wraps.").Append(property.Name);
						}
						else
						{
							sb.Append(", null");
						}

						sb.Append(");").AppendLine();
					}
				}
			}
			else if (!isClassInterface && !property.IsAbstract)
			{
				if (property is { IsIndexer: true, IndexerParameters: not null, })
				{
					string accessVarName =
						Helpers.GetUniqueLocalVariableName("access", property.IndexerParameters.Value);
					string setupVarName =
						Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
					string baseResultVarName =
						Helpers.GetUniqueLocalVariableName("baseResult", property.IndexerParameters.Value);
					string wrapsVarName =
						Helpers.GetUniqueLocalVariableName("wraps", property.IndexerParameters.Value);

					EmitIndexerGetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
						property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
						useTypedRecordsForIndexer ? indexerGetIdRef : null);
					sb.Append("\t\t\t\tif (!(").Append(setupVarName).Append("?.SkipBaseClass() ?? ")
						.Append(mockRegistry).Append(".Behavior.SkipBaseClass))").AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					if (property.Getter?.IsProtected != true)
					{
						sb.Append("\t\t\t\t\t").AppendTypeOrWrapper(property.Type).Append(' ')
							.Append(baseResultVarName).Append(" = this.")
							.Append(mockRegistryName)
							.Append(".Wraps is ").Append(wrapsType).Append(' ').Append(wrapsVarName).Append(" ? ")
							.Append(wrapsVarName).Append('[')
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value))
							.Append("] : base[")
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value)).Append("];")
							.AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\t\t").AppendTypeOrWrapper(property.Type).Append(' ')
							.Append(baseResultVarName).Append(" = base[")
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value)).Append("];")
							.AppendLine();
					}

					AppendIndexerGetWithBaseReturn("\t\t\t\t\t", accessVarName, setupVarName, baseResultVarName);
					sb.Append("\t\t\t\t}").AppendLine();
					AppendIndexerGetReturn("\t\t\t\t", accessVarName, setupVarName);
				}
				else
				{
					string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
					if (useTypedRecordsForProperty)
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
							.Append(", ").Append(accessFieldRef).Append(", static b => ")
							.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue",
								autoMock: PropertyAutoMock(autoMocking, "b"));
						if (property is { IsStatic: false, } && property.Getter?.IsProtected != true)
						{
							sb.Append(", ").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
								.Append(" wraps ? () => wraps.").Append(property.Name).Append(" : () => base.")
								.Append(property.Name);
						}
						else
						{
							sb.Append(", () => base.").Append(property.Name);
						}

						sb.Append(");").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertyGetMemberArg).Append(accessFieldRef).Append(", () => ")
							.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue",
								autoMock: PropertyAutoMock(autoMocking, $"{mockRegistry}.Behavior"));
						if (property is { IsStatic: false, } && property.Getter?.IsProtected != true)
						{
							sb.Append(", ").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
								.Append(" wraps ? () => wraps.").Append(property.Name).Append(" : () => base.")
								.Append(property.Name);
						}
						else
						{
							sb.Append(", () => base.").Append(property.Name);
						}

						sb.Append(");").AppendLine();
					}
				}
			}
			else if (property is { IsIndexer: true, IndexerParameters: not null, })
			{
				string accessVarName =
					Helpers.GetUniqueLocalVariableName("access", property.IndexerParameters.Value);
				string setupVarName =
					Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);

				EmitIndexerGetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
					property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
					useTypedRecordsForIndexer ? indexerGetIdRef : null);
				AppendIndexerGetReturn("\t\t\t\t", accessVarName, setupVarName);
			}
			else
			{
				string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
				if (useTypedRecordsForProperty)
				{
					sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
						.AppendTypeOrWrapper(property.Type).Append(">(")
						.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
						.Append(", ").Append(accessFieldRef)
						.Append(", static b => ")
						.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue",
							autoMock: PropertyAutoMock(autoMocking, "b"))
						.Append(");").AppendLine();
				}
				else
				{
					sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
						.AppendTypeOrWrapper(property.Type).Append(">(")
						.Append(propertyGetMemberArg).Append(accessFieldRef)
						.Append(", () => ")
						.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue",
							autoMock: PropertyAutoMock(autoMocking, $"{mockRegistry}.Behavior"))
						.Append(", null);").AppendLine();
				}
			}

			sb.AppendLine("\t\t\t}");
		}

		if (property.Setter != null)
		{
			sb.Append("\t\t\t");
			if (property.Setter.Accessibility != property.Accessibility)
			{
				sb.Append(property.Setter.OverrideAccessibility).Append(' ');
			}

			sb.AppendLine(property.Setter.IsInitOnly ? "init" : "set");
			sb.AppendLine("\t\t\t{");

			if (isUnsupportedRefStructValue)
			{
				AppendUnsupportedRefStructValueSetter();
			}
			// Ref-struct-keyed indexer setter: dispatches through
			// RefStructIndexerSetterSetup<TValue, T1..Tn>. Mirrors the getter pipeline.
			else if (isRefStructIndexer)
			{
				AppendRefStructIndexerSetterBody(sb, property, mockRegistry);
			}
			else if (isClassInterface && !explicitInterfaceImplementation)
			{
				if (property is { IsIndexer: true, IndexerParameters: not null, })
				{
					string accessVarName =
						Helpers.GetUniqueLocalVariableName("access", property.IndexerParameters.Value);
					string setupVarName =
						Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
					string wrapsVarName =
						Helpers.GetUniqueLocalVariableName("wraps", property.IndexerParameters.Value);

					EmitIndexerSetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
						property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
						useTypedRecordsForIndexer ? indexerSetIdRef : null,
						useTypedRecordsForIndexer ? indexerGetIdRef : null);
					AppendIndexerSetCall("\t\t\t\t", ";", accessVarName, setupVarName);

					sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
						.Append(' ').Append(wrapsVarName).Append(')').AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t").Append(wrapsVarName).Append('[')
						.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value))
						.AppendLine("] = value;");
					sb.Append("\t\t\t\t}").AppendLine();
				}
				else
				{
					if (useTypedRecordsForProperty)
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property)).Append(", ")
							.Append(memberIdPrefix).Append(memberIds.GetPropertySetIdentifier(property)).Append(", ")
							.Append(property.GetUniqueNameString()).Append(", value);").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertySetMemberArg).Append(property.GetUniqueNameString())
							.Append(", value);").AppendLine();
					}

					if (!property.IsStatic && !property.Setter.IsInitOnly)
					{
						sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
							.Append(" wraps)").AppendLine();
						sb.Append("\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\twraps.").Append(property.Name).Append(" = value;").AppendLine();
						sb.Append("\t\t\t\t}").AppendLine();
					}
				}
			}
			else if (property is { IsIndexer: true, IndexerParameters: not null, })
			{
				string accessVarName =
					Helpers.GetUniqueLocalVariableName("access", property.IndexerParameters.Value);
				string setupVarName =
					Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
				string wrapsVarName =
					Helpers.GetUniqueLocalVariableName("wraps", property.IndexerParameters.Value);

				if (!isClassInterface && !property.IsAbstract)
				{
					EmitIndexerSetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
						property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
						useTypedRecordsForIndexer ? indexerSetIdRef : null,
						useTypedRecordsForIndexer ? indexerGetIdRef : null);
					AppendIndexerSetCall("\t\t\t\tif (!", ")", accessVarName, setupVarName);
					sb.Append("\t\t\t\t{").AppendLine();
					if (property.Setter?.IsProtected != true)
					{
						sb.Append("\t\t\t\t\tif (this.").Append(mockRegistryName).Append(".Wraps is ").Append(wrapsType)
							.Append(' ').Append(wrapsVarName).Append(')').AppendLine();
						sb.Append("\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\t").Append(wrapsVarName).Append('[')
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value))
							.AppendLine("] = value;");
						sb.Append("\t\t\t\t\t}").AppendLine();
						sb.Append("\t\t\t\t\telse").AppendLine();
						sb.Append("\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\tbase[")
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value))
							.AppendLine("] = value;");
						sb.Append("\t\t\t\t\t}").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\t\tbase[")
							.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value))
							.AppendLine("] = value;");
					}

					sb.Append("\t\t\t\t}").AppendLine();
				}
				else
				{
					EmitIndexerSetterAccessAndSetup(sb, "\t\t\t\t", mockRegistry, accessVarName, setupVarName,
						property.Type, property.IndexerParameters.Value, useTypedRecordsForIndexer,
						useTypedRecordsForIndexer ? indexerSetIdRef : null,
						useTypedRecordsForIndexer ? indexerGetIdRef : null);
					AppendIndexerSetCall("\t\t\t\t", ";", accessVarName, setupVarName);
				}
			}
			else
			{
				if (!isClassInterface && !property.IsAbstract)
				{
					if (useTypedRecordsForProperty)
					{
						sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property)).Append(", ")
							.Append(memberIdPrefix).Append(memberIds.GetPropertySetIdentifier(property)).Append(", ")
							.Append(property.GetUniqueNameString()).Append(", value))").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertySetMemberArg).Append(property.GetUniqueNameString())
							.Append(", value))").AppendLine();
					}

					sb.Append("\t\t\t\t{").AppendLine();
					if (property is { IsStatic: false, Setter: { IsProtected: false, IsInitOnly: false, }, })
					{
						sb.Append("\t\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
							.Append(" wraps)").AppendLine();
						sb.Append("\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\twraps.").Append(property.Name).Append(" = value;").AppendLine();
						sb.Append("\t\t\t\t\t}").AppendLine();
						sb.Append("\t\t\t\t\telse").AppendLine();
						sb.Append("\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\tbase.").Append(property.Name).Append(" = value;").AppendLine();
						sb.Append("\t\t\t\t\t}").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\t\tbase.").Append(property.Name).Append(" = value;").AppendLine();
					}

					sb.Append("\t\t\t\t}").AppendLine();
				}
				else
				{
					if (useTypedRecordsForProperty)
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property)).Append(", ")
							.Append(memberIdPrefix).Append(memberIds.GetPropertySetIdentifier(property)).Append(", ")
							.Append(property.GetUniqueNameString()).AppendLine(", value);");
					}
					else
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertySetMemberArg).Append(property.GetUniqueNameString())
							.AppendLine(", value);");
					}
				}
			}

			sb.AppendLine("\t\t\t}");
		}

		sb.AppendLine("\t\t}");
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass_AddMethod(StringBuilder sb, Method method,
		string mockRegistryName, string className,
		bool explicitInterfaceImplementation, bool isClassInterface, Class @class,
		MemberIdTable memberIds, string memberIdPrefix, AutoMockingOptions? autoMocking, bool useTypedRecords = true)
#pragma warning restore S107
	{
		string mockRegistry = method.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		string returnRefPrefix = (method.IsRefReturn, method.IsRefReadonlyReturn) switch
		{
			(true, _) => "ref ",
			(false, true) => "ref readonly ",
			(_, _) => "",
		};
		// The shapes that degrade to a NotSupportedException stub below never touch the storage, and
		// declaring it would break the whole mock over a member that is already given up on: a field
		// of a non-span ref-struct type does not compile (CS8345). The `allows ref struct` clause
		// guards the same hole (CS9244) and cannot be reached today only because such a method's
		// verify surface is emitted unguarded and fails first.
		bool hasRefReturnStorage = (method.IsRefReturn || method.IsRefReadonlyReturn) &&
		                           !method.ReturnType.NeedsRefStructPipeline() &&
		                           !method.HasUnsupportedAllowsRefStructTypeParameter;
		string refReturnStorageField = hasRefReturnStorage
			? "_refReturnStorage_" + new string((method.ContainingType + "_" + method.Name)
				.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray())
			: "";
		if (hasRefReturnStorage)
		{
			sb.Append("\t\tprivate ").Append(method.ReturnType.Fullname).Append(' ')
				.Append(refReturnStorageField).Append(';').AppendLine();
		}

		// An implicit implementation would have to restate a constraint that C# accepts only when inherited (CS0425).
		bool isExplicitForInheritedOnlyConstraint = !explicitInterfaceImplementation && isClassInterface &&
		                                            method.ExplicitImplementation is null &&
		                                            method.HasInheritedOnlyConstraint;
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(method.Cref).AppendLine("\" />");
		sb.Append(method.Attributes, "\t\t");
		if (explicitInterfaceImplementation || isExplicitForInheritedOnlyConstraint)
		{
			sb.Append("\t\t");
			if (method.IsStatic)
			{
				sb.Append("static ");
			}

			sb.Append(returnRefPrefix).Append(method.ReturnType.Fullname).Append(' ')
				.Append(method.ContainingType).Append('.').Append(method.Name)
				.Append('(');
		}
		else
		{
			sb.Append("\t\t");
			if (method.ExplicitImplementation is null)
			{
				sb.Append(method.OverrideAccessibility).Append(' ');
				if (method.IsStatic)
				{
					sb.Append("static ");
				}

				if ((!isClassInterface && method.UseOverride) || method.IsEquals() || method.IsGetHashCode() ||
				    method.IsToString())
				{
					sb.Append("override ");
				}
				else if (isClassInterface && method.HidesObjectMember())
				{
					sb.Append("new ");
				}

				sb.Append(returnRefPrefix).Append(method.ReturnType.Fullname).Append(' ')
					.Append(method.Name).Append('(');
			}
			else
			{
				if (method.IsStatic)
				{
					sb.Append("static ");
				}

				sb.Append(returnRefPrefix).Append(method.ReturnType.Fullname).Append(' ')
					.Append(method.ExplicitImplementation).Append('.').Append(method.Name).Append('(');
			}
		}

		int index = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			if (index++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.RefKind.GetString());
			if (parameter.IsParams)
			{
				sb.Append("params ");
			}

			sb.Append(parameter.Type.Fullname).Append(' ').Append(parameter.Name);
			if (!explicitInterfaceImplementation && !isExplicitForInheritedOnlyConstraint &&
			    parameter.HasExplicitDefaultValue)
			{
				sb.Append(" = ").Append(parameter.ExplicitDefaultValue);
			}
		}

		sb.Append(')');
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			bool isOverride = !isClassInterface && method.UseOverride;
			bool isExplicitImplementation = explicitInterfaceImplementation ||
			                                isExplicitForInheritedOnlyConstraint ||
			                                method.ExplicitImplementation is not null;
			bool inheritsConstraints = isExplicitImplementation || isOverride || method.IsEquals() ||
			                           method.IsGetHashCode() || method.IsToString();
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", inheritsConstraints, isExplicitImplementation || isOverride);
			}
		}

		sb.AppendLine();
		sb.AppendLine("\t\t{");

		(MockAdapterBinding Binding, MockAdapterMember Member)? adapterMember = FindAdapterMember(@class, method);
		if (adapterMember is { Member.Kind: MockAdapterMemberKind.Intercepted, } interceptedMember)
		{
			AppendAdapterInterception(sb, method, interceptedMember.Binding, interceptedMember.Member, mockRegistry,
				memberIdPrefix + memberIds.GetMethodIdentifier(method), !isClassInterface && !method.IsAbstract);
			sb.AppendLine("\t\t}");
			return;
		}

		// Methods that use a generic type parameter declaring `allows ref struct` in their
		// signature cannot route through the regular setup pipeline: ReturnMethodSetup<T> /
		// IReturnMethodSetup<T> do not declare the same anti-constraint, so referencing them
		// with such a T fails with CS9244. Mirroring the carve-out for unsupported ref-struct
		// shapes, the body throws NotSupportedException so the rest of the type still compiles.
		if (method.HasUnsupportedAllowsRefStructTypeParameter)
		{
			AppendUnsupportedMethodThrow(sb, method,
				"methods with a generic type parameter declaring 'allows ref struct' are not supported");
			sb.AppendLine("\t\t}");
			return;
		}

		// Checked ahead of the ref-struct-parameter branch below: a non-span ref-struct return has no
		// setup surface on any target (see Helpers.NeedsRefStructPipeline), so routing such a method into
		// the ref-struct pipeline would trade a working stub for that pipeline's pre-.NET 9 #error and
		// break the whole compilation over a member that can never be supported anyway.
		if (method.ReturnType.NeedsRefStructPipeline())
		{
			AppendUnsupportedRefStructReturnBody(sb, method, mockRegistry, className, isClassInterface,
				explicitInterfaceImplementation);
			sb.AppendLine("\t\t}");
			return;
		}

		// Methods with at least one ref-struct parameter (outside the Span/ReadOnlySpan wrapper
		// carve-out) route through the ref-struct setup pipeline. The ref-struct value cannot
		// be captured in a closure, so we emit a synchronous, stack-bound match/invoke loop.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			AppendMockSubject_ImplementClass_AddRefStructMethodBody(sb, method, mockRegistry,
				memberIdPrefix + memberIds.GetMethodIdentifier(method), autoMocking);
			sb.AppendLine("\t\t}");
			return;
		}

		string methodSetup = Helpers.GetUniqueLocalVariableName("methodSetup", method.Parameters);
		string methodSetupType = (method.ReturnType == Type.Void, method.Parameters.Count) switch
		{
			(true, 0) => "global::Mockolate.Setup.VoidMethodSetup",
			(true, _) =>
				$"global::Mockolate.Setup.VoidMethodSetup<{string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper()))}>",
			(_, 0) => $"global::Mockolate.Setup.ReturnMethodSetup<{method.ReturnType.ToTypeOrWrapper()}>",
			(_, _) =>
				$"global::Mockolate.Setup.ReturnMethodSetup<{method.ReturnType.ToTypeOrWrapper()}, {string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper()))}>",
		};
		bool hasOutParams = method.Parameters.Any(p => p.RefKind is RefKind.Out);
		bool hasRefParams = method.Parameters.Any(p => p.RefKind is RefKind.Ref);
		string hasWrappedResult = Helpers.GetUniqueLocalVariableName("hasWrappedResult", method.Parameters);
		string wrappedResult = Helpers.GetUniqueLocalVariableName("wrappedResult", method.Parameters);
		string wpc = Helpers.GetUniqueLocalVariableName("wpc", method.Parameters);
		string wraps = Helpers.GetUniqueLocalVariableName("wraps", method.Parameters);
		bool supportsWrapping = !explicitInterfaceImplementation && method is { IsStatic: false, IsProtected: false, };
		// Interface members are forwarded via their declaring interface: an explicit implementation (hidden
		// via `new`, or a name collision between sibling interfaces) has a different signature than the one
		// reachable through the mocked interface, and an implicit member declared by several sibling
		// interfaces is ambiguous on the mocked interface (CS0121).
		string wrapsType = isClassInterface ? method.ContainingType : className;
		bool isAbstractOrInterface = isClassInterface || method.IsAbstract;

		StringBuilder sb2 = new();
		int i = 0;
		foreach (MethodParameter p in method.Parameters)
		{
			if (i++ > 0)
			{
				sb2.Append(", ");
			}

			if (p.RefKind == RefKind.Ref || p.RefKind == RefKind.In || p.RefKind == RefKind.RefReadOnlyParameter)
			{
				string paramRef = Helpers.GetUniqueLocalVariableName($"ref_{p.Name}", method.Parameters);

				sb.Append("\t\t\tvar ").Append(paramRef).Append(" = ").Append(p.Name).Append(';').AppendLine();
				sb2.Append(paramRef);
			}
			else if (p.RefKind != RefKind.Out &&
			         (p.Type.SpecialGenericType == SpecialGenericType.Span ||
			          p.Type.SpecialGenericType == SpecialGenericType.ReadOnlySpan))
			{
				string paramRef = Helpers.GetUniqueLocalVariableName($"ref_{p.Name}", method.Parameters);

				sb.Append("\t\t\tvar ").Append(paramRef).Append(" = ").Append(p.ToNameOrWrapper()).Append(';')
					.AppendLine();
				sb2.Append(paramRef);
			}
			else
			{
				sb2.Append(
					p.RefKind switch
					{
						RefKind.Out => "default",
						_ => p.ToNameOrWrapper(),
					});
			}
		}

		string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
		bool isGeneric = method.GenericParameters is not null && method.GenericParameters.Value.Count > 0;
		(string Type, string MatchArgs)? genericDispatch = isGeneric ? GetGenericDispatch(method) : null;
		string? genericSetup = genericDispatch is null
			? null
			: Helpers.GetUniqueLocalVariableName("genericMethodSetup", method.Parameters);
		if (genericDispatch is { } dispatch)
		{
			EmitMethodSetupLookupWithGeneric(sb, "\t\t\t", mockRegistry, methodSetup, methodSetupType, memberIdRef,
				method.GetUniqueNameString(), sb2.ToString(), genericSetup!, dispatch.Type, GetGenericSetupName(method),
				dispatch.MatchArgs, method.Parameters);
		}
		else
		{
			EmitMethodSetupLookup(sb, "\t\t\t", mockRegistry, methodSetup, methodSetupType, memberIdRef,
				method.GetUniqueNameString(), sb2.ToString(), isGeneric, method.Parameters);
		}
		sb.Append("\t\t\tbool ").Append(hasWrappedResult).Append(" = false;").AppendLine();
		if (method.ReturnType != Type.Void)
		{
			sb.Append("\t\t\t").Append(method.ReturnType.Fullname).Append(' ').Append(wrappedResult)
				.Append(" = default!;")
				.AppendLine();
		}

		if (hasOutParams)
		{
			foreach (MethodParameter parameter in method.Parameters.Where(p => p.RefKind == RefKind.Out))
			{
				sb.Append("\t\t\t").Append(parameter.Name).Append(" = default!;").AppendLine();
			}
		}

		sb.Append("\t\t\tif (").Append(mockRegistry).Append(".Behavior.SkipInteractionRecording == false)")
			.AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		if (useTypedRecords && IsTypedRecordingEligibleMethod(method))
		{
			int arity = method.Parameters.Count;
			string typeArgs = arity == 0
				? string.Empty
				: "<" + string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper())) + ">";
			string recordsType = $"global::Mockolate.Internals.MethodRecords{typeArgs}";

			sb.Append("\t\t\t\t").Append(recordsType)
				.Append(".Append((global::Mockolate.Internals.MockInteractions)")
				.Append(mockRegistry).Append(".Interactions, ").Append(memberIdRef).Append(", ")
				.Append(method.GetUniqueNameString());
			if (arity > 0)
			{
				sb.Append(", ").Append(string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper())));
			}

			sb.Append(");").AppendLine();
		}
		else
		{
			sb.Append("\t\t\t\t").Append(mockRegistry)
				.Append(".RegisterInteraction(new global::Mockolate.Interactions.MethodInvocation");
			if (method.Parameters.Count > 0)
			{
				sb.Append('<').Append(string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper())))
					.Append('>');
			}

			sb.Append('(').Append(method.GetUniqueNameString());
			if (method.Parameters.Count > 0)
			{
				sb.Append(", ").Append(string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper())));
			}

			sb.Append("));").AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();

		sb.Append("\t\t\ttry").AppendLine();
		sb.Append("\t\t\t{").AppendLine();

		if (supportsWrapping)
		{
			sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is ").Append(wrapsType)
				.Append(' ').Append(wraps).Append(')').AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			if (method.ReturnType != Type.Void)
			{
				sb.Append("\t\t\t\t\t").Append(wrappedResult).Append(" = ").Append(wraps).Append('.')
					.Append(method.Name).Append('(')
					.Append(FormatMethodParametersWithRefKind(method.Parameters))
					.Append(");").AppendLine();
				sb.Append("\t\t\t\t\t").Append(hasWrappedResult).Append(" = true;").AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\t\t").Append(wraps).Append('.')
					.Append(method.Name).Append('(')
					.Append(FormatMethodParametersWithRefKind(method.Parameters))
					.Append(");").AppendLine();
				sb.Append("\t\t\t\t\t").Append(hasWrappedResult).Append(" = true;").AppendLine();
			}

			sb.Append("\t\t\t\t}").AppendLine();
		}

		if (!isAbstractOrInterface)
		{
			if (method.Name.StartsWith("Send", StringComparison.Ordinal) &&
			    @class is { ClassFullName: "global::System.Net.Http.HttpClient", })
			{
				sb.Append("\t\t\t\t#if NETFRAMEWORK").AppendLine();
				sb.Append(
						"\t\t\t\t// Persist the HttpContent, because it gets automatically disposed on .NET Framework")
					.AppendLine();
				sb.Append("\t\t\t\tif (request.Content != null)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append(
						"\t\t\t\t\tvar stream = request.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();")
					.AppendLine();
				sb.Append("\t\t\t\t\tusing global::System.IO.MemoryStream ms = new();").AppendLine();
				sb.Append("\t\t\t\t\tstream.CopyTo(ms);").AppendLine();
				sb.Append("\t\t\t\t\tbyte[] bytes = ms.ToArray();").AppendLine();
				sb.Append("\t\t\t\t\tstream.Position = 0L;").AppendLine();
				sb.Append("\t\t\t\t\trequest.Properties.Add(\"Mockolate:HttpContent\", bytes);").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				sb.Append("\t\t\t\t#endif").AppendLine();
			}

			sb.Append("\t\t\t\tif (!(").Append(methodSetup).Append("?.SkipBaseClass(").Append(mockRegistry)
				.Append(".Behavior) ?? ");
			if (genericSetup is not null)
			{
				sb.Append(genericSetup).Append("?.SkipBaseClass(").Append(mockRegistry).Append(".Behavior) ?? ");
			}

			sb.Append(mockRegistry).Append(".Behavior.SkipBaseClass)");
			if (supportsWrapping)
			{
				sb.Append(" && !").Append(hasWrappedResult);
			}

			sb.Append(')').AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\t");
			if (method.ReturnType != Type.Void)
			{
				sb.Append(wrappedResult).Append(" = ");
			}

			sb.Append("base.").Append(method.Name).Append('(')
				.Append(FormatMethodParametersWithRefKind(method.Parameters))
				.Append(");").AppendLine();
			sb.Append("\t\t\t\t\t").Append(hasWrappedResult).Append(" = true;").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
		}

		if (hasOutParams || hasRefParams)
		{
			string outParamBase = Helpers.GetUniqueIndexedLocalVariableBase("outParam", method.Parameters);
			string refParamBase = Helpers.GetUniqueIndexedLocalVariableBase("refParam", method.Parameters);
			string outTempBase = Helpers.GetUniqueIndexedLocalVariableBase("outTemp", method.Parameters);
			sb.Append("\t\t\t\tif (!").Append(hasWrappedResult).Append(" || ").Append(methodSetup).Append(" is ")
				.Append(methodSetupType)
				.Append(".WithParameterCollection)")
				.AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\tif (").Append(methodSetup).Append(" is ").Append(methodSetupType)
				.Append(".WithParameterCollection ").Append(wpc).Append(')').AppendLine();
			sb.Append("\t\t\t\t\t{").AppendLine();
			int parameterIndex = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				parameterIndex++;
				if (parameter.RefKind == RefKind.Out)
				{
					// The temp local keeps the value of the wrapped instance or the base class when the setup has
					// none, and C# does not insert the implicit conversion of a span wrapper across `out`.
					sb.Append("\t\t\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
						.Append(" is global::Mockolate.Parameters.IOutParameter<")
						.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(outParamBase)
						.Append(parameterIndex)
						.Append(" && ").Append(outParamBase).Append(parameterIndex)
						.Append(".TryGetValue(out ").Append(parameter.Type.ToTypeOrWrapper())
						.Append(' ').Append(outTempBase).Append(parameterIndex).Append("))").AppendLine();
					sb.Append("\t\t\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ").Append(outTempBase)
						.Append(parameterIndex).Append(';').AppendLine();
					sb.Append("\t\t\t\t\t\t}").AppendLine();
					sb.Append("\t\t\t\t\t\telse if (!").Append(hasWrappedResult).Append(')').AppendLine();
					sb.Append("\t\t\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ")
						.AppendDefaultValueGeneratorFor(parameter.Type, $"{mockRegistry}.Behavior.DefaultValue")
						.Append(';').AppendLine();
					sb.Append("\t\t\t\t\t\t}").AppendLine();
				}
				else if (parameter.RefKind == RefKind.Ref)
				{
					sb.Append("\t\t\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
						.Append(" is global::Mockolate.Parameters.IRefParameter<")
						.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(refParamBase)
						.Append(parameterIndex)
						.Append(')').AppendLine();
					sb.Append("\t\t\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ").Append(refParamBase)
						.Append(parameterIndex)
						.Append(".GetValue(").Append(parameter.Name).Append(");").AppendLine();
					sb.Append("\t\t\t\t\t\t}").AppendLine();
				}
			}

			sb.Append("\t\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t\telse").AppendLine();
			sb.Append("\t\t\t\t\t{").AppendLine();
			foreach (MethodParameter parameter in method.Parameters.Where(p => p.RefKind == RefKind.Out))
			{
				sb.Append("\t\t\t\t\t\t").Append(parameter.Name).Append(" = ")
					.AppendDefaultValueGeneratorFor(parameter.Type, $"{mockRegistry}.Behavior.DefaultValue").Append(';')
					.AppendLine();
			}

			sb.Append("\t\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
		}

		(MockAdapterBinding Binding, MockAdapterMember Member)? answeringMember =
			adapterMember is { Member.Kind: MockAdapterMemberKind.AnsweredWithoutSetup, } ? adapterMember : null;
		if ((hasOutParams || hasRefParams) && method.ReturnType != Type.Void &&
		    answeringMember is { Member.Hook: { } byRefHook, } byRefAnswering)
		{
			sb.Append("\t\t\t\tif (!").Append(hasWrappedResult).Append(" && ").Append(methodSetup)
				.Append("?.HasReturnCallbacks != true)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\treturn ");
			AppendAnsweringHookCall(sb, byRefAnswering.Binding, byRefAnswering.Member, byRefHook, mockRegistry,
				methodSetup, method);
			sb.Append(';').AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\tfinally").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		AppendTriggerCallbacks(sb, "\t\t\t\t", methodSetup, method.Parameters);
		if (genericDispatch is { } triggeredDispatch)
		{
			sb.Append("\t\t\t\t").Append(genericSetup).Append("?.TriggerCallbacks(").Append(triggeredDispatch.MatchArgs)
				.Append(");").AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();

		if (answeringMember is null)
		{
			string displayMethodName =
				$"{method.ContainingType}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type.DisplayName))})";
			sb.Append("\t\t\tif (").Append(methodSetup).Append(" is null && ");
			if (genericSetup is not null)
			{
				sb.Append(genericSetup).Append(" is null && ");
			}

			sb.Append('!').Append(hasWrappedResult).Append(" && ")
				.Append(mockRegistry).Append(".Behavior.ThrowWhenNotSetup)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockNotSetupException(\"The method '")
				.Append(displayMethodName).Append("' was invoked without prior setup.\");").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
		}
		else if (method.ReturnType == Type.Void && answeringMember is { Member.Hook: { } voidHook, } voidAnswering)
		{
			sb.Append("\t\t\tif (!").Append(hasWrappedResult).Append(')').AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t");
			AppendAnsweringHookCall(sb, voidAnswering.Binding, voidAnswering.Member, voidHook, mockRegistry, methodSetup,
				method);
			sb.Append(';').AppendLine();
			sb.Append("\t\t\t}").AppendLine();
		}

		if (method.ReturnType != Type.Void)
		{
			bool isRefReturn = method.IsRefReturn || method.IsRefReadonlyReturn;
			string returnValue = Helpers.GetUniqueLocalVariableName("returnValue", method.Parameters);
			sb.Append("\t\t\tif (").Append(methodSetup).Append("?.HasReturnCallbacks != true && ")
				.Append(hasWrappedResult).Append(')').AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			if (isRefReturn)
			{
				sb.Append("\t\t\t\tthis.").Append(refReturnStorageField).Append(" = ")
					.Append(wrappedResult).Append(';').AppendLine();
				sb.Append("\t\t\t\treturn ref this.").Append(refReturnStorageField).Append(';').AppendLine();
			}
			else if (answeringMember is { Member.WrappedResultHook: { } wrappedResultHook, } wrappedAnswering)
			{
				sb.Append("\t\t\t\treturn ").Append(wrappedAnswering.Binding.Adapter.HookClass).Append('.')
					.Append(wrappedResultHook).Append('(').Append(mockRegistry).Append(", ").Append(wrappedResult)
					.Append(");").AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\treturn ").Append(wrappedResult).Append(';').AppendLine();
			}

			sb.Append("\t\t\t}").AppendLine();
			if (isRefReturn)
			{
				sb.Append("\t\t\tthis.").Append(refReturnStorageField).Append(" = ")
					.Append(methodSetup).Append("?.TryGetReturnValue(");
			}
			else
			{
				sb.Append("\t\t\treturn ").Append(methodSetup).Append("?.TryGetReturnValue(");
			}

			string defaultValueGeneratorSuffix = "";
			string? taskSuffix = null;
			if (method.Parameters.Count > 0)
			{
				sb.Append(string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper()))).Append(", ");
				defaultValueGeneratorSuffix = string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper()));
				MethodParameter[] tokenCandidates =
					method.Parameters.Where(p => p.Type.MayHoldCancellationToken).ToArray();
				if (tokenCandidates.Length == 0)
				{
					taskSuffix = "";
				}
				else if (tokenCandidates.Length == 1 && tokenCandidates[0].Type.IsCancellationToken)
				{
					taskSuffix = tokenCandidates[0].ToNameOrWrapper();
				}
			}

			sb.Append("out var ").Append(returnValue).Append(") == true ? ").Append(returnValue).Append(" : ");
			if (answeringMember is { Member.Hook: { } hook, } answering)
			{
				AppendAnsweringHookCall(sb, answering.Binding, answering.Member, hook, mockRegistry, methodSetup,
					method);
			}
			else if (method.IsEquals())
			{
				sb.Append("global::System.Object.ReferenceEquals(this, ")
					.Append(method.Parameters.Single().ToNameOrWrapper()).Append(')');
			}
			else
			{
				sb.AppendDefaultValueGeneratorFor(method.ReturnType, $"{mockRegistry}.Behavior.DefaultValue",
					defaultValueGeneratorSuffix, taskSuffix,
					MethodAutoMock(autoMocking, mockRegistry, memberIdRef));
			}

			sb.Append(';').AppendLine();
			if (isRefReturn)
			{
				sb.Append("\t\t\treturn ref this.").Append(refReturnStorageField).Append(';').AppendLine();
			}
		}

		sb.AppendLine("\t\t}");
	}

	/// <summary>
	///     Emits the body of a mock method whose signature contains at least one ref-struct
	///     parameter (outside the Span/ReadOnlySpan wrapper carve-out). Uses
	///     <c>RefStructMethodInvocation</c> for recording and
	///     <c>RefStruct{Void,Return}MethodSetup&lt;T1..Tn&gt;</c> for dispatch. All matching runs
	///     synchronously on the caller's stack — no closure capture.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The full setup-builder pipeline (<c>Do(Action&lt;T&gt;)</c>, <c>Callbacks&lt;T&gt;</c>
	///         sequencing, <c>TransitionTo</c>, <c>When</c>/<c>Only</c>, etc.) is intentionally NOT
	///         supported here: all of it requires <c>T</c> to flow through delegate type parameters,
	///         which is illegal for a ref-struct <c>T</c>. Only the narrow <c>Throws</c> /
	///         <c>Returns(value)</c> / <c>DoesNotThrow</c> / <c>SkippingBaseClass</c> surface is
	///         available — see <c>IRefStructVoidMethodSetup</c> / <c>IRefStructReturnMethodSetup</c>.
	///     </para>
	///     <para>
	///         The emitted code is guarded by <c>#if NET9_0_OR_GREATER</c>. Older TFMs get a
	///         <c>#error</c> because the <c>IParameter&lt;T&gt;</c> anti-constraint is a C# 13
	///         feature and the ref-struct setup types only compile on net9.0+.
	///     </para>
	///     <para>
	///         Every ref kind is in scope: by value, <see langword="out" />, <see langword="ref" /> and <c>ref readonly</c>, the
	///         by-ref ones writing back through <c>IOutRefStructParameter</c> /
	///         <c>IRefRefStructParameter</c> below. There is no arity ceiling either - arities 1-4 are
	///         hand-written in <c>Source/Mockolate/Setup/</c>, 5+ are generator-emitted into
	///         <c>RefStructMethodSetups.g.cs</c>. A non-span ref-struct return never reaches here: the
	///         caller stubs it out first, so it degrades on every target instead of hitting the
	///         <c>#error</c> above. Analyzer-level diagnostics live in <c>MockabilityAnalyzer</c>.
	///     </para>
	/// </remarks>
	private static void AppendMockSubject_ImplementClass_AddRefStructMethodBody(
		StringBuilder sb, Method method, string mockRegistry, string memberIdRef, AutoMockingOptions? autoMocking)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();

		string typeParams = string.Join(", ", method.Parameters.Select(p => p.Type.Fullname));
		string setupType = method.ReturnType == Type.Void
			? $"global::Mockolate.Setup.RefStructVoidMethodSetup<{typeParams}>"
			: $"global::Mockolate.Setup.RefStructReturnMethodSetup<{method.ReturnType.Fullname}, {typeParams}>";

		// Record the invocation — names only, no values. The ref-struct values stay on the stack.
		sb.Append("\t\t\t").Append(mockRegistry)
			.Append(".RegisterInteraction(new global::Mockolate.Interactions.RefStructMethodInvocation(")
			.Append(method.GetUniqueNameString());
		foreach (MethodParameter p in method.Parameters)
		{
			sb.Append(", \"").Append(p.Name).Append('"');
		}

		sb.Append("));").AppendLine();

		// Pre-default any `out` ref-struct slot. The MockBehavior.DefaultValue generator returns
		// object? and cannot produce ref-struct values, so we assign default! directly. If a
		// matching setup later supplies a value via IOutRefStructParameter<T>.TryGetValue, that
		// value overwrites this; otherwise the default! sticks.
		foreach (MethodParameter outParameter in method.Parameters.Where(p => p.RefKind == RefKind.Out))
		{
			sb.Append("\t\t\t").Append(outParameter.Name).Append(" = default!;").AppendLine();
		}

		// Iterate setups in latest-registered-first order (scenario-scoped first, default-scope
		// after — GetMethodSetups<T> preserves that ordering). Stop on the first matcher that
		// accepts every positional argument. The matching runs synchronously on the stack so
		// ref-struct values are not captured in a closure.
		string paramNames = string.Join(", ", method.Parameters.Select(p => p.Name));
		string setupVar = Helpers.GetUniqueLocalVariableName("setup", method.Parameters);
		string matchedVar = Helpers.GetUniqueLocalVariableName("matched", method.Parameters);

		sb.Append("\t\t\tbool ").Append(matchedVar).Append(" = false;").AppendLine();
		sb.Append("\t\t\tforeach (").Append(setupType).Append(' ').Append(setupVar).Append(" in ").Append(mockRegistry)
			.Append(".GetMethodSetups<").Append(setupType).Append(">(").Append(method.GetUniqueNameString())
			.Append("))").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tif (!").Append(setupVar).Append(".Matches(").Append(paramNames).Append("))").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tcontinue;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\t\t").Append(matchedVar).Append(" = true;").AppendLine();

		// Per-slot write-back for out/ref ref-struct parameters. ref-readonly is read-only by
		// definition and needs no write-back.
		int refStructSlotIndex = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			refStructSlotIndex++;
			if (parameter.RefKind == RefKind.Out)
			{
				string outVar = Helpers.GetUniqueLocalVariableName(FormattableString.Invariant($"outParam{refStructSlotIndex}"), method.Parameters);
				sb.Append("\t\t\t\tif (").Append(setupVar).Append(".GetMatcher").Append(refStructSlotIndex)
					.Append("() is global::Mockolate.Parameters.IOutRefStructParameter<")
					.Append(parameter.Type.Fullname).Append("> ").Append(outVar).Append(" && ").Append(outVar)
					.Append(".TryGetValue(out ").Append(parameter.Name).Append(")) { }").AppendLine();
				sb.Append("\t\t\t\telse").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\t").Append(parameter.Name).Append(" = default!;").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
			}
			else if (parameter.RefKind == RefKind.Ref)
			{
				string refVar = Helpers.GetUniqueLocalVariableName(FormattableString.Invariant($"refParam{refStructSlotIndex}"), method.Parameters);
				sb.Append("\t\t\t\tif (").Append(setupVar).Append(".GetMatcher").Append(refStructSlotIndex)
					.Append("() is global::Mockolate.Parameters.IRefRefStructParameter<")
					.Append(parameter.Type.Fullname).Append("> ").Append(refVar).Append(')').AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\t").Append(parameter.Name).Append(" = ").Append(refVar).Append(".GetValue(")
					.Append(parameter.Name).Append(");").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
			}
		}

		if (method.ReturnType == Type.Void)
		{
			sb.Append("\t\t\t\t").Append(setupVar).Append(".Invoke(").Append(paramNames).Append(");").AppendLine();
			sb.Append("\t\t\t\treturn;").AppendLine();
		}
		else
		{
			// Return-side: the Invoke overload takes an optional defaultFactory for missing
			// return configuration. When the setup has a return configured it wins; otherwise
			// we still run Invoke for any Throws/DoesNotThrow side effect then break out of the
			// search (a matching-but-unconfigured setup shadows later setups).
			string comma = method.Parameters.Count > 0 ? ", " : "";
			sb.Append("\t\t\t\tif (").Append(setupVar).Append(".HasReturnValue)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\treturn ").Append(setupVar).Append(".Invoke(").Append(paramNames).Append(comma)
				.Append("() => default!);").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.AppendLine();
			sb.Append("\t\t\t\t").Append(setupVar).Append(".Invoke(").Append(paramNames).Append(comma)
				.Append("() => default!);").AppendLine();
			sb.Append("\t\t\t\tbreak;").AppendLine();
		}

		sb.Append("\t\t\t}").AppendLine();

		// Not-matched path: respect Behavior.ThrowWhenNotSetup, else return the framework default.
		string displayMethodName =
			$"{method.ContainingType}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type.DisplayName))})";

		sb.Append("\t\t\tif (!").Append(matchedVar).Append(" && ").Append(mockRegistry)
			.Append(".Behavior.ThrowWhenNotSetup)")
			.AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockNotSetupException(\"The method '")
			.Append(displayMethodName).Append("' was invoked without prior setup.\");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();

		if (method.ReturnType != Type.Void)
		{
			// Either the setup matched but had no return value configured, or no setup matched and
			// the behavior doesn't throw. Either way, surface the framework default for this return
			// type — matches the non-ref-struct pipeline's behavior when there is no wrapped base
			// and no configured return.
			sb.Append("\t\t\treturn ")
				.AppendDefaultValueGeneratorFor(method.ReturnType, $"{mockRegistry}.Behavior.DefaultValue",
					autoMock: MethodAutoMock(autoMocking, mockRegistry, memberIdRef))
				.Append(';').AppendLine();
		}

		sb.Append("#else").AppendLine();
		sb.Append(
				"#error Mockolate: methods with ref-struct parameters require .NET 9 or later (uses the 'allows ref struct' anti-constraint).")
			.AppendLine();
		sb.Append("\t\t\tthrow new global::System.NotSupportedException();").AppendLine();
		sb.Append("#endif").AppendLine();
	}

	/// <summary>
	///     Emits the body of an indexer getter whose key list contains at least one ref-struct
	///     parameter (outside the Span/ReadOnlySpan wrapper carve-out). Mirrors
	///     <see cref="AppendMockSubject_ImplementClass_AddRefStructMethodBody" /> but uses
	///     <c>RefStructIndexerGetterSetup&lt;TValue, T1..Tn&gt;</c> and the CLR accessor name
	///     <c>get_Item</c>.
	/// </summary>
	/// <remarks>
	///     Semantically equivalent to a method <c>TValue get_Item(T1 k1, ..., Tn kn)</c> — the
	///     setup dispatch, match-then-invoke loop, and fallthrough-to-default behavior all follow
	///     the return-method pipeline. No value is captured on the recorded interaction (ref-struct
	///     keys stay on the stack); verify-side matching over keys is not available, same rationale
	///     as the method-side pipeline.
	/// </remarks>
	private static void AppendRefStructIndexerGetterBody(StringBuilder sb, Property property, string mockRegistry)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();

		string typeParams = string.Join(", ",
			property.IndexerParameters!.Value.Select(p => p.Type.Fullname));
		string setupType =
			$"global::Mockolate.Setup.RefStructIndexerGetterSetup<{property.Type.Fullname}, {typeParams}>";
		string displayName =
			$"{property.ContainingType}.this[{string.Join(", ", property.IndexerParameters!.Value.Select(p => p.Type.DisplayName))}]";
		string indexerName = $"\"{property.ContainingType}.get_Item\"";

		// Record the invocation — names only, no values. The ref-struct keys stay on the stack.
		sb.Append("\t\t\t\t").Append(mockRegistry)
			.Append(".RegisterInteraction(new global::Mockolate.Interactions.RefStructMethodInvocation(")
			.Append(indexerName);
		foreach (MethodParameter p in property.IndexerParameters.Value)
		{
			sb.Append(", \"").Append(p.Name).Append('"');
		}

		sb.Append("));").AppendLine();

		// Iterate setups in latest-registered-first order; stop on the first matching setup.
		string paramNames = string.Join(", ", property.IndexerParameters.Value.Select(p => p.Name));
		string setupVar = Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
		sb.Append("\t\t\t\tforeach (").Append(setupType).Append(' ').Append(setupVar).Append(" in ")
			.Append(mockRegistry)
			.Append(".GetMethodSetups<").Append(setupType).Append(">(").Append(indexerName).Append("))")
			.AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tif (!").Append(setupVar).Append(".Matches(").Append(paramNames).Append("))").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\tcontinue;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.AppendLine();

		// Build a comma-separated raw-key list for arity 2+ that pre-boxes non-ref-struct slots
		// (ref-struct slots receive null and use their projection). At arity 1 no raw keys are
		// passed — the arity-1 setup owns its own projection dispatch via the matcher.
		string rawKeysArgs = property.IndexerParameters!.Value.Count >= 2
			? ", " + string.Join(", ",
				property.IndexerParameters!.Value.Select(p =>
					p.NeedsRefStructPipeline() ? "null" : $"(object){p.Name}"))
			: string.Empty;

		sb.Append("\t\t\t\t\tif (").Append(setupVar).Append(".HasReturnValue)").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\treturn ").Append(setupVar).Append(".Invoke(").Append(paramNames).Append(rawKeysArgs)
			.Append(", () => default!);").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.AppendLine();

		// Matching-but-unconfigured setup still invokes (for Throws side effects) and then shadows
		// later setups in the iteration.
		sb.Append("\t\t\t\t\t").Append(setupVar).Append(".Invoke(").Append(paramNames).Append(rawKeysArgs)
			.Append(", () => default!);").AppendLine();
		sb.Append("\t\t\t\t\tbreak;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();

		// Not-matched path: respect Behavior.ThrowWhenNotSetup, else the framework default.
		sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Behavior.ThrowWhenNotSetup)").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append(
				"\t\t\t\t\tthrow new global::Mockolate.Exceptions.MockNotSetupException(\"The indexer '")
			.Append(displayName).Append("' was invoked without prior setup.\");").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();

		sb.Append("\t\t\t\treturn ")
			.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue")
			.Append(';').AppendLine();

		sb.Append("#else").AppendLine();
		sb.Append(
				"#error Mockolate: indexer getters with ref-struct keys require .NET 9 or later (uses the 'allows ref struct' anti-constraint).")
			.AppendLine();
		sb.Append("\t\t\t\tthrow new global::System.NotSupportedException();").AppendLine();
		sb.Append("#endif").AppendLine();
	}

	/// <summary>
	///     Emits the body of an indexer setter whose key list contains at least one ref-struct
	///     parameter. Mirrors <see cref="AppendRefStructIndexerGetterBody" /> but uses
	///     <c>RefStructIndexerSetterSetup&lt;TValue, T1..Tn&gt;</c> and the CLR accessor name
	///     <c>set_Item</c>. Records the value parameter's name alongside the ref-struct keys on
	///     <c>RefStructMethodInvocation</c> but does not carry the value.
	/// </summary>
	private static void AppendRefStructIndexerSetterBody(StringBuilder sb, Property property, string mockRegistry)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();

		string typeParams = string.Join(", ",
			property.IndexerParameters!.Value.Select(p => p.Type.Fullname));
		string setupType =
			$"global::Mockolate.Setup.RefStructIndexerSetterSetup<{property.Type.Fullname}, {typeParams}>";
		string indexerName = $"\"{property.ContainingType}.set_Item\"";

		// Record the invocation — names only, no ref-struct key values (stays on the stack). The
		// "value" parameter name is appended for parity with the getter's recorded parameter names.
		sb.Append("\t\t\t\t").Append(mockRegistry)
			.Append(".RegisterInteraction(new global::Mockolate.Interactions.RefStructMethodInvocation(")
			.Append(indexerName);
		foreach (MethodParameter p in property.IndexerParameters.Value)
		{
			sb.Append(", \"").Append(p.Name).Append('"');
		}

		sb.Append(", \"value\"));").AppendLine();

		// Iterate setter setups latest-first; first match wins. The setter's Invoke handles the
		// OnSet callback and any configured throw, plus projection-keyed storage forwarding to
		// the companion getter (when both accessors form a combined setup).
		string keyNames = string.Join(", ", property.IndexerParameters.Value.Select(p => p.Name));

		// Build a comma-separated raw-key list for arity 2+ that pre-boxes non-ref-struct slots.
		// At arity 1 no raw keys are passed — the arity-1 setup owns its own projection dispatch.
		string rawKeysArgs = property.IndexerParameters.Value.Count >= 2
			? ", " + string.Join(", ",
				property.IndexerParameters.Value.Select(p =>
					p.NeedsRefStructPipeline() ? "null" : $"(object){p.Name}"))
			: string.Empty;

		string setupVar = Helpers.GetUniqueLocalVariableName("setup", property.IndexerParameters.Value);
		sb.Append("\t\t\t\tforeach (").Append(setupType).Append(' ').Append(setupVar).Append(" in ")
			.Append(mockRegistry)
			.Append(".GetMethodSetups<").Append(setupType).Append(">(").Append(indexerName).Append("))")
			.AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tif (!").Append(setupVar).Append(".Matches(").Append(keyNames).Append("))").AppendLine();
		sb.Append("\t\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\t\tcontinue;").AppendLine();
		sb.Append("\t\t\t\t\t}").AppendLine();
		sb.AppendLine();
		sb.Append("\t\t\t\t\t").Append(setupVar).Append(".Invoke(").Append(keyNames).Append(", value")
			.Append(rawKeysArgs).Append(");").AppendLine();
		sb.Append("\t\t\t\t\treturn;").AppendLine();
		sb.Append("\t\t\t\t}").AppendLine();

		sb.Append("#else").AppendLine();
		sb.Append(
				"#error Mockolate: indexer setters with ref-struct keys require .NET 9 or later (uses the 'allows ref struct' anti-constraint).")
			.AppendLine();
		sb.Append("\t\t\t\tthrow new global::System.NotSupportedException();").AppendLine();
		sb.Append("#endif").AppendLine();
	}

	/// <summary>
	///     Emits the <c>NotSupportedException</c> body for a method-shaped member that has no setup
	///     surface, so every carve-out - including the delegate ones in <c>SourceTemplates.MockDelegate</c> -
	///     reports <paramref name="reason" /> in the same shape the analyzer uses.
	/// </summary>
	private static void AppendUnsupportedMethodThrow(StringBuilder sb, Method method, string reason)
		=> sb.Append("\t\t\tthrow new global::System.NotSupportedException(\"Mockolate: ").Append(reason)
			.Append(". Method '").Append(method.ContainingType).Append('.').Append(method.Name)
			.Append("'.\");").AppendLine();

	/// <summary>
	///     Emits the body of a method whose non-span ref-struct return leaves it without a setup surface.
	///     A virtual class method still has a real implementation behind it, so it forwards to the wrapped
	///     instance or to <see langword="base" /> rather than breaking a member that would otherwise work. Interface,
	///     abstract, static and <see langword="ref" />-returning methods have nothing to forward to and throw.
	/// </summary>
	/// <remarks>
	///     The forward ignores the behaviour flags that a normal member honours, because there is no
	///     setup to honour them against: <c>MockBehavior.SkipBaseClass</c> has no configured value to
	///     return in the base call's place, <c>MockBehavior.ThrowWhenNotSetup</c> would reject a member
	///     that can never be set up in the first place, and a ref-struct return cannot be recorded, so
	///     <c>SkipInteractionRecording</c> is moot too.
	/// </remarks>
	private static void AppendUnsupportedRefStructReturnBody(StringBuilder sb, Method method, string mockRegistry,
		string className, bool isClassInterface, bool explicitInterfaceImplementation)
	{
		const string reason = "methods returning a non-span ref struct are not supported";
		if (isClassInterface || method.IsAbstract || method.IsStatic || method.IsRefReturn ||
		    method.IsRefReadonlyReturn)
		{
			AppendUnsupportedMethodThrow(sb, method, reason);
			return;
		}

		// Method.Name already carries the type-parameter list for generic methods.
		string call = method.Name + "(" + FormatMethodParametersWithRefKind(method.Parameters) + ")";
		if (explicitInterfaceImplementation || method.IsProtected)
		{
			sb.Append("\t\t\treturn base.").Append(call).Append(';').AppendLine();
			return;
		}

		string wraps = Helpers.GetUniqueLocalVariableName("wraps", method.Parameters);
		sb.Append("\t\t\treturn ").Append(mockRegistry).Append(".Wraps is ").Append(className).Append(' ')
			.Append(wraps).Append(" ? ").Append(wraps).Append('.').Append(call).Append(" : base.").Append(call)
			.Append(';').AppendLine();
	}

	#endregion Mock Helpers

	#region Setup Helpers

	private static IEnumerable<bool[]> GenerateValueFlagCombinations(EquatableArray<MethodParameter> parameters)
	{
		int[] valueableIndices = parameters
			.Select((p, i) => (p, i))
			.Where(x => x.p.CanUseNullableParameterOverload())
			.Select(x => x.i)
			.ToArray();
		int valueableCount = valueableIndices.Length;
		int totalCombos = 1 << valueableCount;
		for (int combo = 1; combo < totalCombos; combo++)
		{
			bool[] flags = new bool[parameters.Count];
			for (int bit = 0; bit < valueableCount; bit++)
			{
				if ((combo & (1 << bit)) != 0)
				{
					flags[valueableIndices[bit]] = true;
				}
			}

			yield return flags;
		}
	}

	/// <summary>
	///     The value flags of the overloads that combine values and matchers above <see cref="MaxExplicitParameters" />:
	///     values wherever possible, and also with matchers for every combination of the parameters typed with a method
	///     type parameter (<see cref="MayHoldMatcher" />), so that such a matcher next to values, also next to a value
	///     for another type parameter, binds as a matcher with the type argument it infers. With more such parameters
	///     than <see cref="MaxExplicitParameters" />, only the overload with matchers for all of them is emitted, so that
	///     the count of overloads stays bounded.
	/// </summary>
	private static IEnumerable<bool[]> GenerateValueFlagsAboveMaxExplicitParameters(
		IEnumerable<MethodParameter> parameters)
	{
		MethodParameter[] array = parameters.ToArray();
		bool[] allValueFlags = array.Select(p => p.CanUseNullableParameterOverload()).ToArray();
		if (!allValueFlags.Any(f => f))
		{
			yield break;
		}

		yield return allValueFlags;
		int[] matcherSlots = Enumerable.Range(0, array.Length)
			.Where(i => allValueFlags[i] && MayHoldMatcher(array[i]))
			.ToArray();
		int allSlots = (1 << matcherSlots.Length) - 1;
		int firstCombination = matcherSlots.Length > MaxExplicitParameters ? allSlots : 1;
		for (int combination = firstCombination; combination <= allSlots; combination++)
		{
			bool[] valueFlags = (bool[])allValueFlags.Clone();
			for (int j = 0; j < matcherSlots.Length; j++)
			{
				if ((combination & (1 << j)) != 0)
				{
					valueFlags[matcherSlots[j]] = false;
				}
			}

			if (valueFlags.Any(f => f))
			{
				yield return valueFlags;
			}
		}
	}

	/// <summary>
	///     Computes which parameters should receive a <c>= null</c> default in the generated signature.
	///     Only non-value parameters in the uninterrupted trailing suffix of optional parameters get a default;
	///     a value parameter (explicit-value overload) breaks the suffix, preventing earlier parameters from
	///     becoming optional and causing a CS1737 error.
	/// </summary>
	private static bool[] ComputeTrailingDefaults(ReadOnlySpan<MethodParameter> parameters, bool[]? valueFlags)
	{
		bool[] result = new bool[parameters.Length];
		bool inTrailingSuffix = true;
		for (int k = parameters.Length - 1; k >= 0; k--)
		{
			bool isValueParam = valueFlags?[k] == true;
			if (!isValueParam && parameters[k].HasExplicitDefaultValue && inTrailingSuffix)
			{
				result[k] = true;
			}
			else
			{
				inTrailingSuffix = false;
			}
		}

		return result;
	}

	/// <summary>
	///     The index of the first key that a setup or verification of <paramref name="indexer" /> can leave out, or the
	///     key count when none can. Only trailing optional keys can be left out, never the first one (an indexer access
	///     needs a key), and nothing when another indexer in the same scope has fewer keys: next to <c>this[int]</c>,
	///     <c>Setup[5]</c> must keep binding to that indexer instead of to <c>this[int, int = 5]</c>, as the call on the
	///     mock does, and in union mode the trailing argument expression of its predicate overload would capture a
	///     string argument meant for a shorter overload of the larger indexer. Ref-struct keys have a single setup
	///     overload without a verification, so their indexers leave out nothing either.
	/// </summary>
	private static int FirstOmittableIndexerKey(Class @class, Property indexer)
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		if (parameters.Any(p => p.NeedsRefStructPipeline()) ||
		    @class.AllProperties().Any(p => p is { IsIndexer: true, ExplicitImplementation: null, } &&
		                                    p.MemberType == indexer.MemberType &&
		                                    p.IndexerParameters?.Count < parameters.Length))
		{
			return parameters.Length;
		}

		int first = parameters.Length;
		while (first > 1 && parameters[first - 1] is { HasExplicitDefaultValue: true, } parameter &&
		       parameter.CanUseNullableParameterOverload())
		{
			first--;
		}

		return first;
	}

	/// <summary>
	///     The declared default value of <paramref name="parameter" />, typed so that it also stands alone as an argument.
	/// </summary>
	private static string DeclaredDefaultValue(MethodParameter parameter)
		=> $"({parameter.ToNullableType()})({parameter.ExplicitDefaultValue})";

	/// <summary>
	///     The overloads of the classic setup and verify indexers: the matcher and value combinations of the keys, repeated
	///     for every count of left-out trailing keys from <paramref name="firstOmittable" /> on. As in union mode, a
	///     left-out key gets its own overload instead of a <c>= null</c> default, so that an explicit <see langword="null" />
	///     keeps matching <see langword="null" />. The value flags cover every key; a left-out key is not a value.
	/// </summary>
	private static IEnumerable<(int KeptCount, bool[]? ValueFlags)> GenerateIndexerOverloads(Property indexer,
		int firstOmittable)
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		for (int keptCount = parameters.Length; keptCount >= firstOmittable; keptCount--)
		{
			yield return (keptCount, null);
			if (parameters.Length <= MaxExplicitParameters)
			{
				EquatableArray<MethodParameter> kept = new(parameters.Take(keptCount).ToArray());
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(kept))
				{
					yield return (keptCount, [..valueFlags, ..new bool[parameters.Length - keptCount],]);
				}
			}
			else
			{
				bool[] allValueFlags = parameters
					.Select((p, i) => i < keptCount && p.CanUseNullableParameterOverload())
					.ToArray();
				if (allValueFlags.Any(f => f))
				{
					yield return (keptCount, allValueFlags);
				}
			}
		}
	}

	/// <summary>
	///     The count of trailing optional parameters that the classic setup and verify overloads of
	///     <paramref name="method" /> leave out through overloads of their own instead of <c>= null</c> defaults, as in union
	///     mode and for indexers, so that an explicit <see langword="null" /> keeps standing for <c>default(T)</c>. Only a
	///     single optional parameter qualifies: with more, a call can skip an earlier one by name, which needs its
	///     <c>= null</c> default. Without overload priorities the shorter overloads would make a lone <see langword="null" />
	///     ambiguous with the <c>IParameters</c> overload, and a same-named sibling (<paramref name="class" /> is
	///     <see langword="null" /> for a delegate, which has none) could repeat their signatures. All of them keep the
	///     <c>= null</c> defaults that stand for the declared default value.
	/// </summary>
	private static int OmittableMethodParameterCount(Class? @class, Method method, bool hasOverloadResolutionPriority)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		if (!hasOverloadResolutionPriority || parameters.Where(p => p.HasExplicitDefaultValue).Take(2).Count() != 1 ||
		    parameters[parameters.Length - 1] is not { HasExplicitDefaultValue: true, } last ||
		    !last.CanUseNullableParameterOverload() || parameters.Any(p => p.NeedsRefStructPipeline()) ||
		    (@class is not null && !HasUniqueMethodName(@class, method)))
		{
			return 0;
		}

		return 1;
	}

	/// <summary>
	///     The classic setup and verify overloads that leave out trailing optional parameters: the matcher and value
	///     combinations of the kept parameters, for every count of left-out parameters up to <paramref name="omittable" />.
	///     The value flags cover every parameter and mark a left-out one as a value unless the overload takes only
	///     matchers, so that they name the full overload that the shorter one forwards to with the declared default values.
	///     Above <see cref="MaxExplicitParameters" /> they are the combinations of the full overloads with values for the
	///     left-out parameters, so that each one has its full overload.
	/// </summary>
	private static IEnumerable<(int KeptCount, bool[]? ValueFlags)> GenerateOmittedMethodOverloads(Method method,
		int omittable)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		for (int keptCount = parameters.Length - 1; keptCount >= parameters.Length - omittable; keptCount--)
		{
			yield return (keptCount, null);
			bool[] omitted = Enumerable.Repeat(element: true, parameters.Length - keptCount).ToArray();
			if (parameters.Length <= MaxExplicitParameters)
			{
				EquatableArray<MethodParameter> kept = new(parameters.Take(keptCount).ToArray());
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(kept))
				{
					yield return (keptCount, [..valueFlags, ..omitted,]);
				}
			}
			else
			{
				foreach (bool[] valueFlags in GenerateValueFlagsAboveMaxExplicitParameters(parameters))
				{
					if (valueFlags.Skip(keptCount).All(f => f) && valueFlags.Take(keptCount).Any(f => f))
					{
						yield return (keptCount, valueFlags);
					}
				}
			}
		}
	}

	/// <summary>
	///     The <c>= null</c> defaults of a classic method overload. With left-out parameters only a slot that cannot leave
	///     out its parameter keeps one, as in union mode, so that an explicit <see langword="null" /> never stands for the
	///     declared default value.
	/// </summary>
	private static bool[] ComputeMethodTrailingDefaults(Method method, bool[]? valueFlags, int omittable, int keptCount)
	{
		if (omittable == 0)
		{
			return ComputeTrailingDefaults(method.Parameters.AsSpan(), valueFlags);
		}

		bool[] breaksDefaults = method.Parameters
			.Select((p, i) => i < keptCount && (valueFlags?[i] == true || p.CanUseNullableParameterOverload()))
			.ToArray();
		return ComputeTrailingDefaults(method.Parameters.AsSpan(), breaksDefaults);
	}

	// The overloads that leave out parameters rank like the full ones among themselves and below every full one, as in
	// union mode; fewer left-out parameters rank higher.
	private static string OmittedMethodOverloadPriority(Method method, int keptCount, bool[]? valueFlags)
	{
		int priority;
		if (valueFlags is null)
		{
			priority = keptCount;
		}
		else if (valueFlags.All(x => x))
		{
			bool[] keptValueFlags = valueFlags.Select((flag, i) => flag && i < keptCount).ToArray();
			priority = ParametersBlockAllValuesPromotion(method.Parameters, keptValueFlags) ? 0 : keptCount + 1;
		}
		else
		{
			priority = valueFlags.Take(keptCount).Count(x => !x);
		}

		int parameterCount = method.Parameters.Count;
		return (priority - (parameterCount + 1) * (parameterCount - keptCount)).ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>
	///     The arguments with which an overload that leaves out parameters forwards to the full overload: the kept
	///     parameters and the declared default values, as matchers when the overload takes only matchers.
	/// </summary>
	private static string OmittedMethodForwardingArguments(Method method, int keptCount, bool[]? valueFlags)
	{
		return string.Join(", ", method.Parameters.Select((p, i) => i < keptCount ? p.Name : OmittedArgument(p)));

		string OmittedArgument(MethodParameter parameter)
		{
			if (valueFlags is null)
			{
				return $"new global::Mockolate.Internals.LiteralParameterMatch<{parameter.ToNullableType()}>({DeclaredDefaultValue(parameter)})";
			}

			return DeclaredDefaultValue(parameter);
		}
	}

	private static string OmittedParametersRemark(Method method, int keptCount)
		=> $" It uses the declared default value for {string.Join(", ", method.Parameters.Skip(keptCount).Select(p => $"<c>{p.Name}</c>"))}.";

	// Priority hierarchy among same-arity overloads emitted for one method:
	//   all-values            : int.MaxValue       (so `Method(default, …)` binds here and exposes `.AnyParameters()`)
	//   IParameters           : int.MaxValue - 1
	//   pure IParameter<T>?   : parameterCount     (just below all-values, above any mixed combo)
	//   mixed                 : count of non-value (matcher) params, in [1, parameterCount - 1]
	// parameterCount == 0 returns int.MaxValue so the lone zero-arg overload keeps its historical priority.
	// The all-values overload is not promoted for an `object` parameter or one typed with a method type parameter
	// (ParametersBlockAllValuesPromotion), nor next to a sibling with such a parameter (HasSiblingThatMayHoldMatcher),
	// nor for a `params` array of a type parameter of a method without overloads (HasParamsMatchers), whose pure
	// IParameter<T>? overload moves to parameterCount + 1, above the per-element overload at parameterCount, so that a
	// `null` for the array binds the matcher for the whole array.
	// A trailing `params object[]` (RanksArrayMatcherFirst) moves the overloads that differ from all-values only by
	// matchers for that parameter above it:
	//   values + IParameter<object[]>?           : int.MaxValue
	//   with other parameters                    : all-values int.MaxValue - 1, IParameters int.MaxValue - 2
	//   alone                                    : IParameters int.MaxValue - 1, params IParameter<object>[]
	//                                              int.MaxValue - 2, all-values int.MaxValue - 3
	//   alone, next to an `object` overload      : params IParameter<object>[] 1, all-values not promoted
	private static string ComputeMethodOverloadPriority(Class @class, Method method, bool useParameters,
		bool[]? valueFlags, bool perElementParams)
	{
		int parameterCount = method.Parameters.Count;
		if (parameterCount == 0)
		{
			return "int.MaxValue";
		}

		bool arrayMatcherFirst = RanksArrayMatcherFirst(method.Parameters);
		if (useParameters)
		{
			return arrayMatcherFirst && parameterCount > 1 ? "int.MaxValue - 2" : "int.MaxValue - 1";
		}

		if (valueFlags is null)
		{
			// A `null` for a `params` array of a type parameter converts to the array matcher and to an element matcher,
			// neither of which converts to the other, so the array matcher must win to stand for a `null` array.
			if (!perElementParams && HasParamsMatchers(@class, method))
			{
				return (parameterCount + 1).ToString(CultureInfo.InvariantCulture);
			}

			if (!arrayMatcherFirst || parameterCount > 1 ||
			    (perElementParams && HasSiblingWithObjectParameter(@class, method)))
			{
				return parameterCount.ToString(CultureInfo.InvariantCulture);
			}

			return perElementParams ? "int.MaxValue - 2" : "int.MaxValue";
		}

		int matcherCount = valueFlags.Count(x => !x);
		return arrayMatcherFirst && matcherCount == 1 && !valueFlags[parameterCount - 1]
			? "int.MaxValue"
			: matcherCount.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>
	///     The priority of the all-values overload, or <see langword="null" /> when it is not promoted. The overload
	///     without parameters for an empty <c>params object[]</c> (<paramref name="emptyParams" />) is the only one
	///     for a call without arguments that keeps <c>AnyParameters()</c>, so it comes first.
	/// </summary>
	private static string? ComputeAllValuesOverloadPriority(Class @class, Method method, bool[] valueFlags,
		bool emptyParams)
	{
		if (emptyParams)
		{
			return "int.MaxValue";
		}

		if (ParametersBlockAllValuesPromotion(method.Parameters, valueFlags) || HasParamsMatchers(@class, method) ||
		    HasSiblingThatMayHoldMatcher(@class, method))
		{
			return null;
		}

		if (!RanksArrayMatcherFirst(method.Parameters))
		{
			return "int.MaxValue";
		}

		if (method.Parameters.Count > 1)
		{
			return "int.MaxValue - 1";
		}

		return HasSiblingWithObjectParameter(@class, method) ? null : "int.MaxValue - 3";
	}

	/// <summary>
	///     Whether an overload of <paramref name="method" /> has an <see langword="object" /> parameter or one typed with a
	///     method type parameter (<see cref="MayHoldMatcher" />), so that its all-values overload is not promoted. The
	///     all-values overload of a lone <c>params object[]</c> then stays unpromoted as well: at equal priority
	///     <c>Setup.M("a")</c> prefers <c>M(object)</c> or <c>M&lt;string&gt;(string)</c> over the expanded form of
	///     <c>M(params object[])</c>, as the call on the mock does.
	/// </summary>
	private static bool HasSiblingWithObjectParameter(Class @class, Method method)
	{
		string bareName = BareMethodName(method);
		return @class.AllMethods().Any(m => m.ExplicitImplementation is null &&
		                                    m.MemberType == method.MemberType &&
		                                    string.Equals(BareMethodName(m), bareName, StringComparison.Ordinal) &&
		                                    m.Parameters.Any(p => (p.Type.SpecialType == SpecialType.System_Object ||
		                                                           MayHoldMatcher(p)) &&
		                                                          p.CanUseNullableParameterOverload()));
	}

	/// <summary>
	///     Whether an overload of <paramref name="method" /> with as many parameters has one typed with a method type
	///     parameter (<see cref="MayHoldMatcher" />), whose all-values overload is not promoted. The all-values overload
	///     of <paramref name="method" /> then stays unpromoted as well: at equal priority <c>Setup.M("a")</c> binds
	///     <c>M&lt;string&gt;(string)</c> over <c>M(IComparable)</c>, as the call on the mock does.
	/// </summary>
	private static bool HasSiblingThatMayHoldMatcher(Class @class, Method method)
	{
		string bareName = BareMethodName(method);
		return @class.AllMethods().Any(m => m.ExplicitImplementation is null &&
		                                    m.MemberType == method.MemberType &&
		                                    string.Equals(BareMethodName(m), bareName, StringComparison.Ordinal) &&
		                                    m.Parameters.Count == method.Parameters.Count &&
		                                    m.Parameters.Any(p => MayHoldMatcher(p) &&
		                                                          p.CanUseNullableParameterOverload()));
	}

	// Object-typed parameters defeat the all-values bump: every reference type — including the
	// IParameter<T>? matcher itself — implicitly converts to object, so promoting `Method(object?)` over
	// `Method(IParameter<object?>?)` would silently capture matcher instances as raw values. Detect that case
	// here and skip the priority bump so callers keep using Match.AnyParameters() / It.IsAny<object?>().
	// A parameter typed with a method type parameter (MayHoldMatcher) blocks it as well: without explicit type
	// arguments, the call infers the type parameter as the matcher type, so `Method(It.IsAny<int>())` would bind
	// `Method<IParameterWithCallback<int>>(value)` instead of `Method<int>(matcher)`.
	private static bool ParametersBlockAllValuesPromotion(EquatableArray<MethodParameter> parameters,
		bool[] valueFlags)
	{
		ReadOnlySpan<MethodParameter> span = parameters.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			if (valueFlags[i] && (span[i].Type.SpecialType == SpecialType.System_Object || MayHoldMatcher(span[i])))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Whether the value for <paramref name="parameter" /> can be a matcher: every matcher converts to a type
	///     parameter that the call infers from it or closes over <see langword="object" />. The overload that takes it as
	///     a value is therefore not promoted (<see cref="ParametersBlockAllValuesPromotion" />), and a matcher that still
	///     binds to it as a value, e.g. one typed as <see langword="object" />, decides at runtime
	///     (<c>Matchers.IsValueOrMatcher</c>).
	/// </summary>
	private static bool MayHoldMatcher(MethodParameter parameter)
		=> parameter.RefKind is RefKind.None or RefKind.In && !parameter.IsParams &&
		   parameter.Type.IsTypeParameterThatCanBeObject;

	private static void AppendValueOrMatcher(StringBuilder sb, MethodParameter parameter)
		=> sb.Append("global::Mockolate.Internals.Matchers.IsValueOrMatcher<").Append(parameter.ToTypeOrWrapper())
			.Append(">(").Append(parameter.Name).Append(')');

	/// <summary>
	///     Whether the values for the <see langword="params" /> <paramref name="parameter" /> can be matchers, for the same reason as
	///     in <see cref="MayHoldMatcher" />: its element type is a type parameter that the call infers from a matcher or
	///     closes over <see langword="object" />. Matchers that bind to the overload that takes them as values decide at
	///     runtime (<c>Matchers.AsParamsMatcher</c>).
	/// </summary>
	private static bool MayHoldParamsMatchers(MethodParameter parameter)
		=> parameter.IsParams && parameter.Type.ElementType is { IsTypeParameterThatCanBeObject: true, };

	/// <summary>
	///     Whether the trailing <see langword="params" /> array of <paramref name="method" /> can hold matchers
	///     (<see cref="MayHoldParamsMatchers" />), no other method shares its name and no parameter is optional. Its
	///     all-values overload is then not promoted, so that matchers bind the overloads for matchers, it keeps the
	///     overload without arguments, and a <see langword="null" /> stands for a <see langword="null" /> array. A
	///     same-named overload competes with each of these choices in ways that the call on the mock does not, and an
	///     optional parameter before the array would take a <see langword="null" /> as its declared default value in an
	///     overload for matchers, so both keep promoting the all-values overload.
	/// </summary>
	private static bool HasParamsMatchers(Class @class, Method method)
		=> method.Parameters.Count > 0 &&
		   MayHoldParamsMatchers(method.Parameters.AsArray()[method.Parameters.Count - 1]) &&
		   !method.Parameters.Any(p => p.HasExplicitDefaultValue) &&
		   HasUniqueMethodName(@class, method);

	private static void AppendParamsMatcher(StringBuilder sb, MethodParameter parameter)
		=> sb.Append("global::Mockolate.Internals.Matchers.AsParamsMatcher<").Append(parameter.Type.ElementType!.Fullname)
			.Append(">(").Append(parameter.Name).Append(')');

	/// <summary>
	///     Matches the <see langword="params" /> values element by element, or a <see langword="null" /> array when the
	///     argument is <see langword="null" />, which is what the same argument passes to the mocked method. Without
	///     <paramref name="matchesNull" /> the caller has already handled a <see langword="null" /> array.
	/// </summary>
	private static void AppendParamsValuesMatcher(StringBuilder sb, MethodParameter parameter, bool matchesNull = true)
	{
		sb.Append("global::Mockolate.CovariantParameterAdapter<").Append(parameter.Type.Fullname).Append(">.Wrap(");
		if (matchesNull)
		{
			sb.Append(parameter.Name).Append(" is null ? global::Mockolate.Internals.Matchers.IsNull<")
				.Append(parameter.Type.Fullname).Append(">() : ");
		}

		sb.Append("global::Mockolate.It.SequenceEquals<").Append(parameter.Type.ElementType!.Fullname).Append(">(")
			.Append(parameter.Name).Append("))");
	}

	/// <summary>
	///     Whether a matcher for the whole array of a trailing <c>params object[]</c> must outrank the all-values
	///     overload, which accepts any argument list in its expanded form and would capture the matcher as one element.
	///     Only the all-values overload moves below it: the overloads that replace other values by matchers stay
	///     below all-values, so a <see langword="default" /> literal for such a parameter keeps meaning the value.
	///     An <see langword="object" /> parameter or one typed with a method type parameter already keeps all-values from
	///     being promoted, and a matcher for it must not bind as its value next to the array matcher. Above
	///     <see cref="MaxExplicitParameters" /> no overload combines values with a matcher for the array.
	/// </summary>
	private static bool RanksArrayMatcherFirst(EquatableArray<MethodParameter> parameters)
	{
		ReadOnlySpan<MethodParameter> span = parameters.AsSpan();
		if (span.Length == 0 || span.Length > MaxExplicitParameters || !IsObjectParams(span[span.Length - 1]))
		{
			return false;
		}

		for (int i = 0; i < span.Length; i++)
		{
			if (!span[i].CanUseNullableParameterOverload() || span[i].Type.SpecialType == SpecialType.System_Object ||
			    MayHoldMatcher(span[i]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsObjectParams(MethodParameter parameter)
		=> parameter.IsParams && parameter.Type.ElementType?.SpecialType == SpecialType.System_Object;

	/// <summary>
	///     The end of the parameter type of the per-element overload. For a lone <c>params object[]</c> that overload
	///     outranks all-values, so a <see langword="null" /> literal binds to it as a <see langword="null" /> element
	///     and the elements are nullable. The elements of a <see langword="params" /> array of a type parameter
	///     (<see cref="MayHoldParamsMatchers" />) are nullable as well, because a <see langword="null" /> element next to a
	///     matcher binds this overload when the type argument is nullable.
	/// </summary>
	private static string PerElementArraySuffix(Method method)
		=> (method.Parameters.Count == 1 && RanksArrayMatcherFirst(method.Parameters)) ||
		   MayHoldParamsMatchers(method.Parameters.AsArray()[method.Parameters.Count - 1])
			? ">?[] "
			: ">[] ";

	/// <summary>
	///     Declares the empty <see langword="params" /> array of the overload that leaves it out as a local, so that the body of
	///     the overload that takes it as values follows unchanged. Forwarding to that overload instead would go through overload
	///     resolution, where a sibling that accepts an array could win.
	/// </summary>
	private static void AppendEmptyParamsLocal(StringBuilder sb, Method method)
	{
		MethodParameter parameter = method.Parameters.AsArray()[method.Parameters.Count - 1];
		sb.Append("\t\t\t").Append(parameter.Type.Fullname).Append(' ').Append(parameter.Name)
			.Append(" = global::System.Array.Empty<").Append(parameter.Type.ElementType!.Fullname).Append(">();")
			.AppendLine();
	}

	private static string EmptyParamsSummary(Method method)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		return parameters.Length == 1
			? " without arguments"
			: " with the given " + string.Join(", ", parameters.Take(parameters.Length - 1)
				  .Select(p => $"<paramref name=\"{p.Name.TrimStart('@')}\"/>")) +
			  " and no <paramref name=\"" + parameters[parameters.Length - 1].Name.TrimStart('@') + "\"/>";
	}

	private static bool[] EmptyParamsValueFlags(Method method)
		=> method.Parameters.Select(p => p.IsParams).ToArray();

	/// <summary>
	///     Whether <paramref name="method" /> gets an overload that leaves out its <see langword="params" /> parameter and matches an
	///     empty array. For a lone <c>params object[]</c> and a lone <see langword="params" /> array of a type parameter
	///     (<see cref="HasParamsMatchers" />) the all-values overload ranks below the per-element overload, so a call
	///     without arguments would bind to the latter and lose <c>AnyParameters()</c>. Without
	///     <c>OverloadResolutionPriorityAttribute</c> (<paramref name="hasOverloadResolutionPriority" />) a call without
	///     <see langword="params" /> arguments is ambiguous between the per-element overload and the one that takes the array as
	///     values, so every <c>params T[]</c> gets it; an overload in normal form wins over both expanded forms.
	///     A sibling that could already own that signature suppresses it; a delegate has no siblings.
	/// </summary>
	private static bool HasEmptyParamsOverload(Class @class, Method method, bool hasOverloadResolutionPriority)
	{
		if (!TryGetPerElementParamsParameter(method) ||
		    method.HasUnsupportedAllowsRefStructTypeParameter || method.ReturnType.NeedsRefStructPipeline())
		{
			return false;
		}

		string bareName = BareMethodName(method);
		bool loneParams = method.Parameters.Count == 1 &&
		                  ((RanksArrayMatcherFirst(method.Parameters) &&
		                    !@class.AllMethods().Any(m => m.ExplicitImplementation is null &&
		                                                  m.MemberType == method.MemberType &&
		                                                  m.Parameters.Count == 0 &&
		                                                  string.Equals(BareMethodName(m), bareName, StringComparison.Ordinal))) ||
		                   HasParamsMatchers(@class, method));
		if (loneParams || hasOverloadResolutionPriority)
		{
			return loneParams;
		}

		string[] leading = LeadingParameterTypes(method);
		return !@class.AllMethods().Any(m => m.ExplicitImplementation is null &&
		                                     m.MemberType == method.MemberType &&
		                                     !m.Equals(method) &&
		                                     string.Equals(BareMethodName(m), bareName, StringComparison.Ordinal) &&
		                                     (m.Parameters.Count == leading.Length ||
		                                      (m.Parameters.Count == leading.Length + 1 &&
		                                       m.Parameters.AsArray()[leading.Length].IsParams)) &&
		                                     m.Parameters.Take(leading.Length)
			                                     .Select(p => p.Type.Fullname.Replace("?", ""))
			                                     .SequenceEqual(leading));

		static string[] LeadingParameterTypes(Method method)
			=> method.Parameters.Take(method.Parameters.Count - 1).Select(p => p.Type.Fullname.Replace("?", ""))
				.ToArray();
	}

	/// <summary>
	///     Whether a lone <see langword="null" /> argument binds to the <c>IParameters</c> overload although it means the
	///     literal <see langword="null" /> for the first parameter: the <c>IParameters</c> overload outranks the overload
	///     for a single <see langword="object" /> parameter or one typed with a method type parameter
	///     (<see cref="MayHoldMatcher" />) and the overloads that leave out optional parameters or an empty
	///     <see langword="params" /> array. Its <see langword="null" /> then forwards to the call with every argument given
	///     (<see cref="LoneNullArguments" />). A same-named sibling could capture that call (<paramref name="class" /> is
	///     <see langword="null" /> for a delegate, which has none), and a classic <see langword="null" /> for a
	///     non-nullable value type is a matcher rather than a value, so both keep the plain <c>IParameters</c> overload.
	///     A <see langword="params" /> array of a type parameter (<see cref="HasParamsMatchers" />) keeps its all-values overload
	///     unpromoted, so the <c>IParameters</c> overload would take a lone <see langword="null" /> for it, where it stands
	///     for a <see langword="null" /> array, or a lone <see langword="null" /> or <see langword="default" /> for the
	///     parameters before it, even of a value type.
	///     A lone <see langword="null" /> for a generic method needs explicit type arguments, and so does the forwarded
	///     call, so only a sibling with as many type parameters could capture them. Next to a generic sibling that keeps
	///     the all-values overload from being promoted (<see cref="HasSiblingThatMayHoldMatcher" />), the
	///     <c>IParameters</c> overload of a non-generic method outranks it as well, and its forwarded call binds the
	///     method itself, as a non-generic method wins a tie with a generic one; so only a non-generic sibling counts.
	///     Only a forwarding overload declares its parameter as <c>IParameters?</c>: the analyzer Mockolate0006 reports a
	///     lone <see langword="null" /> for a non-nullable <c>IParameters</c> parameter and relies on this annotation.
	/// </summary>
	private static bool ForwardsLoneNull(Class? @class, Method method, bool useUnionOverloads)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		bool isGeneric = method.GenericParameters is not null && method.GenericParameters.Value.Count > 0;
		bool besideGenericSibling = !isGeneric && @class is not null && HasSiblingThatMayHoldMatcher(@class, method);
		bool paramsMatchers = isGeneric && @class is not null && HasParamsMatchers(@class, method);
		if (parameters.Length == 0 ||
		    (isGeneric && !MayHoldMatcher(parameters[0]) && !paramsMatchers) ||
		    (@class is not null && !HasUniqueMethodName(@class, method, isGeneric || besideGenericSibling)))
		{
			return false;
		}

		MethodParameter first = parameters[0];
		int optionalCount = parameters.Length > 1 && parameters[parameters.Length - 1].IsParams
			? parameters.Length - 2
			: parameters.Length - 1;
		return (!first.IsParams || paramsMatchers) &&
		       first.CanUseNullableParameterOverload() &&
		       (useUnionOverloads || !first.Type.IsValueType || first.Type.CanBeNullable || paramsMatchers) &&
		       (parameters.Length > 1 || first.Type.SpecialType == SpecialType.System_Object ||
		        MayHoldMatcher(first) || paramsMatchers || besideGenericSibling) &&
		       parameters.Skip(1).Take(optionalCount)
			       .All(p => p.HasExplicitDefaultValue && p.CanUseNullableParameterOverload());
	}

	/// <summary>
	///     <c>default(T)</c> for the first parameter and the declared default value for the optional ones, each typed, so
	///     that the call binds the all-values overload without relying on overload priorities.
	/// </summary>
	private static string LoneNullArguments(Method method)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		StringBuilder arguments = new();
		arguments.Append("default(").Append(parameters[0].ToNullableType()).Append(')');
		foreach (MethodParameter parameter in parameters.Skip(1).Where(p => !p.IsParams))
		{
			arguments.Append(", (").Append(parameter.ToNullableType()).Append(")(")
				.Append(parameter.ExplicitDefaultValue).Append(')');
		}

		return arguments.ToString();
	}

	/// <summary>
	///     Which accessors of <paramref name="property" /> the mock actually intercepts, and therefore
	///     which setup/verify facade the generated surface exposes. The setup type, the verify type and the
	///     verify constructor arguments all derive from this one classification so they cannot disagree.
	/// </summary>
	/// <remarks>
	///     The classification follows the accessors the mock emits a body for, which is exactly what
	///     <see cref="Property.Getter" />/<see cref="Property.Setter" /> being non-null means: the property
	///     declares the accessor, and it is not an <see langword="internal" />/<c>private protected</c> one hidden from
	///     the mock's assembly. Other accessibilities are not considered here, so a
	///     <c>{ get; private set; }</c> property still classifies as <see cref="PropertyAccessors.Both" />.
	///     The <see cref="PropertyAccessors.Both" /> arm therefore also absorbs degenerate shapes that
	///     cannot produce compiling output regardless of the facade chosen.
	/// </remarks>
	private static PropertyAccessors GetInterceptedAccessors(Property property)
		=> (property.Getter, property.Setter) switch
		{
			(not null, null) => PropertyAccessors.GetOnly,
			(null, not null) => PropertyAccessors.SetOnly,
			_ => PropertyAccessors.Both,
		};

	/// <summary>
	///     The setup facade emitted for <paramref name="property" />, narrowed to the accessors classified
	///     by <see cref="GetInterceptedAccessors" />.
	/// </summary>
	private static string GetPropertySetupType(Property property)
		=> GetInterceptedAccessors(property) switch
		{
			PropertyAccessors.GetOnly =>
				$"global::Mockolate.Setup.IPropertyGetterOnlySetup<{property.Type.ToTypeOrWrapper()}>",
			PropertyAccessors.SetOnly =>
				$"global::Mockolate.Setup.IPropertySetterOnlySetup<{property.Type.ToTypeOrWrapper()}>",
			_ => $"global::Mockolate.Setup.PropertySetup<{property.Type.ToTypeOrWrapper()}>",
		};

	/// <summary>
	///     The verify facade emitted for <paramref name="property" />, narrowed to the accessors classified
	///     by <see cref="GetInterceptedAccessors" />.
	/// </summary>
	private static string GetPropertyVerifyType(Property property, string verifyName)
		=> GetInterceptedAccessors(property) switch
		{
			PropertyAccessors.GetOnly =>
				$"global::Mockolate.Verify.VerificationPropertyGetterResult<{verifyName}>",
			PropertyAccessors.SetOnly =>
				$"global::Mockolate.Verify.VerificationPropertySetterResult<{verifyName}, {property.Type.ToTypeOrWrapper()}>",
			_ => $"global::Mockolate.Verify.VerificationPropertyResult<{verifyName}, {property.Type.ToTypeOrWrapper()}>",
		};

	/// <summary>
	///     The open-generic setup facade emitted for the <paramref name="indexer" />, narrowed to the accessors
	///     classified by <see cref="GetInterceptedAccessors" />. The narrowed types are shipped for up to
	///     <see cref="MaxExplicitParameters" /> keys and generated per-compilation for more. The caller appends
	///     the value and key type arguments and the closing angle bracket.
	/// </summary>
	private static string GetIndexerSetupType(Property indexer)
		=> GetInterceptedAccessors(indexer) switch
		{
			PropertyAccessors.GetOnly => "global::Mockolate.Setup.IIndexerGetterOnlySetup<",
			PropertyAccessors.SetOnly => "global::Mockolate.Setup.IIndexerSetterOnlySetup<",
			_ => "global::Mockolate.Setup.IndexerSetup<",
		};

	/// <summary>
	///     Appends the verify facade emitted for the <paramref name="indexer" />, narrowed to the accessors
	///     classified by <see cref="GetInterceptedAccessors" />. The narrowed results are keyed by the
	///     indexer parameters (the setter result additionally by the value type), the full
	///     <c>VerificationIndexerResult</c> only by the value type.
	/// </summary>
	private static void AppendIndexerVerifyType(StringBuilder sb, Property indexer, string verifyName)
	{
		switch (GetInterceptedAccessors(indexer))
		{
			case PropertyAccessors.GetOnly:
				sb.Append("global::Mockolate.Verify.VerificationIndexerGetterResult<").Append(verifyName);
				foreach (MethodParameter parameter in indexer.IndexerParameters!.Value)
				{
					sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
				}

				sb.Append('>');
				break;
			case PropertyAccessors.SetOnly:
				sb.Append("global::Mockolate.Verify.VerificationIndexerSetterResult<").Append(verifyName);
				foreach (MethodParameter parameter in indexer.IndexerParameters!.Value)
				{
					sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
				}

				sb.Append(", ").AppendTypeOrWrapper(indexer.Type).Append('>');
				break;
			default:
				sb.Append("global::Mockolate.Verify.VerificationIndexerResult<").Append(verifyName).Append(", ")
					.AppendTypeOrWrapper(indexer.Type).Append('>');
				break;
		}
	}

	private static void DefineSetupInterface(StringBuilder sb, Class @class, MemberType memberType,
		bool hasOverloadResolutionPriority, bool useUnionOverloads = false)
	{
		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType && !property.Type.NeedsRefStructPipeline();
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			sb.AppendXmlSummary(
				$"Setup for the {property.Type.Fullname.EscapeForXmlDoc()} property {property.XmlDocReference}.");
			sb.Append("\t\t").Append(GetPropertySetupType(property)).Append(' ')
				.Append(property.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Events

		Func<Event, bool> eventPredicate = @event
			=> @event.ExplicitImplementation is null && @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(eventPredicate))
		{
			sb.AppendXmlSummary(
				$"Setup for the event {@event.XmlDocReference}.");
			sb.Append("\t\tglobal::Mockolate.Setup.EventSetup ").Append(@event.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate =
			indexer => indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, } &&
			           indexer.MemberType == memberType && !indexer.Type.NeedsRefStructPipeline();
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			int firstOmittable = FirstOmittableIndexerKey(@class, indexer);
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionIndexerSlotCombinations(indexer, firstOmittable))
				{
					AppendUnionIndexerSetupDefinition(sb, indexer, slots);
				}

				continue;
			}

			foreach ((int keptCount, bool[]? valueFlags) in GenerateIndexerOverloads(indexer, firstOmittable))
			{
				AppendIndexerSetupDefinition(sb, indexer, keptCount, valueFlags, hasOverloadResolutionPriority);
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(GetParametersOverloadKey).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Take(2).Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodSetupDefinition(sb, @class, method, useParameters: true,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority,
						forwardsLoneNull: ForwardsLoneNull(@class, method, UseUnionOverloads(@class, method, useUnionOverloads)));
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodSetupDefinition(sb, @class, method, useParameters: false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters, allowOmission: true))
					{
						AppendUnionMethodSetupDefinition(sb, @class, method, slots);
					}
				}
				else
				{
					int omittable = OmittableMethodParameterCount(@class, method, hasOverloadResolutionPriority);
					AppendMethodSetupDefinition(sb, @class, method, useParameters: false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodSetupDefinition(sb, @class, method, useParameters: false,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, perElementParams: true);
					}

					if (HasEmptyParamsOverload(@class, method, hasOverloadResolutionPriority))
					{
						AppendMethodSetupDefinition(sb, @class, method, useParameters: false, valueFlags: EmptyParamsValueFlags(method),
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, emptyParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodSetupDefinition(sb, @class, method, useParameters: false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
						}
					}
					else
					{
						foreach (bool[] valueFlags in GenerateValueFlagsAboveMaxExplicitParameters(method.Parameters))
						{
							AppendMethodSetupDefinition(sb, @class, method, useParameters: false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
						}
					}

					foreach ((int keptCount, bool[]? valueFlags) in GenerateOmittedMethodOverloads(method, omittable))
					{
						AppendMethodSetupDefinition(sb, @class, method, useParameters: false, valueFlags: valueFlags,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable,
							keptCount: keptCount);
					}
				}
			}
		}

		#endregion
	}

	private static void AppendMethodOverloadRemark(StringBuilder sb, Method method, bool useParameters,
		bool[]? valueFlags, bool isVerify, int keptCount)
	{
		if (keptCount == method.Parameters.Count)
		{
			AppendOverloadDifferentiatorRemark(sb, method.Parameters.Select(p => p.Name).ToArray(), useParameters,
				valueFlags, isVerify);
			return;
		}

		string omission = OmittedParametersRemark(method, keptCount);
		if (keptCount == 0)
		{
			sb.AppendXmlRemarks(omission.TrimStart());
			return;
		}

		AppendOverloadDifferentiatorRemark(sb, method.Parameters.Take(keptCount).Select(p => p.Name).ToArray(),
			useParameters, valueFlags?.Take(keptCount).ToArray(), isVerify, omission);
	}

	private static void AppendOverloadDifferentiatorRemark(StringBuilder sb,
		IReadOnlyList<string> parameterNames, bool useParameters, bool[]? valueFlags,
		bool isVerify = false, string omission = "")
	{
		if (parameterNames.Count == 0)
		{
			return;
		}

		string text;
		if (useParameters)
		{
			text = isVerify
				? "This overload matches invocations via a custom <see cref=\"global::Mockolate.Match\" /> predicate (for example <see cref=\"global::Mockolate.Match.AnyParameters()\" /> or <see cref=\"global::Mockolate.Match.Parameters(global::System.Func{object?[], bool}, string)\" />) rather than per-parameter matchers."
				: "This overload configures the setup via a custom <see cref=\"global::Mockolate.Match\" /> predicate (for example <see cref=\"global::Mockolate.Match.AnyParameters()\" /> or <see cref=\"global::Mockolate.Match.Parameters(global::System.Func{object?[], bool}, string)\" />) rather than per-parameter matchers.";
		}
		else if (valueFlags is null)
		{
			text =
				"This overload takes <see cref=\"global::Mockolate.It\" /> argument matchers (e.g. <c>It.IsAny&lt;T&gt;()</c>, <c>It.Is&lt;T&gt;(value)</c>) for every parameter.";
		}
		else if (valueFlags.All(x => x))
		{
			text = isVerify
				? "This overload accepts direct values for every parameter and returns a <see cref=\"global::Mockolate.Verify.VerificationResult{TVerify}.IgnoreParameters\" /> whose <see cref=\"global::Mockolate.Verify.VerificationResult{TVerify}.IgnoreParameters.AnyParameters()\" /> drops per-parameter matching entirely."
				: "This overload accepts direct values for every parameter; each is treated as <c>It.Is&lt;T&gt;(value)</c>.";
		}
		else
		{
			List<string> valueParams = [];
			List<string> matcherParams = [];
			for (int i = 0; i < parameterNames.Count && i < valueFlags.Length; i++)
			{
				if (valueFlags[i])
				{
					valueParams.Add($"<paramref name=\"{parameterNames[i].TrimStart('@')}\" />");
				}
				else
				{
					matcherParams.Add($"<paramref name=\"{parameterNames[i].TrimStart('@')}\" />");
				}
			}

			string directPart = string.Join(", ", valueParams);
			string matcherPart = string.Join(", ", matcherParams);
			text =
				$"This overload accepts a direct value for {directPart} (equivalent to <c>It.Is&lt;T&gt;(value)</c>) and an <see cref=\"global::Mockolate.It\" /> matcher for {matcherPart}.";
		}

		sb.AppendXmlRemarks(text + omission);
	}

	/// <summary>
	///     Detects whether <paramref name="method" /> ends in a <c>params T[]</c> parameter that can carry a
	///     per-element matcher overload. The element type must flow through the regular <c>IParameter&lt;T&gt;</c>
	///     pipeline; ref-struct element types are intentionally not supported, as they cannot satisfy
	///     <c>IParameter&lt;T&gt;</c>.
	/// </summary>
	private static bool TryGetPerElementParamsParameter(Method method)
	{
		if (method.Parameters.Count == 0)
		{
			return false;
		}

		MethodParameter last = method.Parameters.AsArray()[method.Parameters.Count - 1];
		if (!last.IsParams || last.Type.ElementType is null || last.NeedsRefStructPipeline())
		{
			return false;
		}

		return true;
	}

	/// <summary>
	///     True when <paramref name="valueFlags" /> marks the trailing <c>params T[]</c> parameter as a
	///     literal value. Such overloads render the parameter as <c>params T[]</c> and match it element-wise
	///     by value (via <c>It.SequenceEquals</c>) rather than by whole-array reference equality.
	/// </summary>
	private static bool HasParamsValueParameter(Method method, bool[]? valueFlags)
	{
		if (valueFlags is null || method.Parameters.Count == 0)
		{
			return false;
		}

		int last = method.Parameters.Count - 1;
		MethodParameter parameter = method.Parameters.AsArray()[last];
		return valueFlags[last] && parameter.IsParams && parameter.Type.ElementType is not null &&
		       !parameter.NeedsRefStructPipeline();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	/// <summary>
	///     Groups the methods whose <c>IParameters</c> overloads would share a signature: the same name and number of type
	///     parameters, whatever the names of the type parameters (e.g. <c>Send&lt;TResponse&gt;</c> and
	///     <c>Send&lt;TRequest&gt;</c>).
	/// </summary>
	private static string GetParametersOverloadKey(Method method)
	{
		int index = method.Name.IndexOf('<');
		return index < 0 ? method.Name : FormattableString.Invariant($"{method.Name.Substring(0, index)}`{method.GenericParameters?.Count}");
	}

	private static void AppendMethodSetupDefinition(StringBuilder sb, Class @class, Method method,
		bool useParameters, string? methodNameOverride = null, bool[]? valueFlags = null,
		bool hasOverloadResolutionPriority = false, bool perElementParams = false, bool forwardsLoneNull = false,
		bool emptyParams = false, int omittable = 0, int? keptCount = null)
#pragma warning restore S107
	{
		// Three shapes cannot expose a setup surface, all for the same reason: IReturnMethodSetup<T> /
		// IVoidMethodSetup<T> do not carry the `allows ref struct` anti-constraint. A generic type
		// parameter that declares it, a non-span ref-struct return, and a ref-struct parameter on a
		// delegate (which has no ref-struct pipeline to route into) each make the override body a
		// NotSupportedException stub, so no setup is needed.
		if (method.HasUnsupportedAllowsRefStructTypeParameter ||
		    method.ReturnType.NeedsRefStructPipeline() ||
		    method.IsDelegateWithUnsupportedRefStructParameter)
		{
			return;
		}

		// Ref-struct pipeline: emit only the narrow IRefStruct*Setup declaration. We skip the
		// value-flag overloads entirely because an explicit ref-struct value cannot be captured
		// via `It.Is<T>(value)` (the static value would need to live in a class field). We also
		// skip the useParameters=true variant (IParameters overload) — ref-struct values cannot
		// flow through the existing IParameters collection. Users pass matchers via
		// `It.IsAnyRefStruct<T>()` / `It.IsRefStruct<T>(predicate)` through the single surviving
		// overload.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			if (useParameters || valueFlags is not null)
			{
				return;
			}

			AppendRefStructMethodSetupDefinition(sb, method, methodNameOverride);
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		int kept = keptCount ?? method.Parameters.Count;
		sb.Append("\t\t/// <summary>").AppendLine();
		if (methodNameOverride is null)
		{
			sb.Append("\t\t///     Setup for the method ").Append(method.XmlDocReference);
		}
		else
		{
			sb.Append("\t\t///     Setup for the delegate ").Append(@class.XmlDocReference);
		}

		if (useParameters)
		{
			sb.Append(" with the given <paramref name=\"parameters\" />");
		}
		else if (emptyParams)
		{
			sb.Append(EmptyParamsSummary(method));
		}
		else if (kept > 0)
		{
			sb.Append(" with the given ")
				.Append(string.Join(", ", method.Parameters.Take(kept).Select(p => $"<paramref name=\"{p.Name.TrimStart('@')}\"/>")));
		}

		sb.Append('.').AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		if (!emptyParams)
		{
			AppendMethodOverloadRemark(sb, method, useParameters, valueFlags, isVerify: false, kept);
		}

		string? omittedPriority = keptCount is not null ? OmittedMethodOverloadPriority(method, kept, valueFlags) : null;
		string? allValuesPriority = valueFlags?.All(x => x) == true && hasOverloadResolutionPriority
			? omittedPriority ?? ComputeAllValuesOverloadPriority(@class, method, valueFlags, emptyParams)
			: null;
		if (method.ReturnType != Type.Void)
		{
			if (valueFlags?.All(x => x) == true)
			{
				if (allValuesPriority is not null)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(allValuesPriority).Append(")]").AppendLine();
				}

				sb.Append("\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer");
			}
			else
			{
				if (hasOverloadResolutionPriority)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(omittedPriority ?? ComputeMethodOverloadPriority(@class, method, useParameters, valueFlags,
							perElementParams))
						.Append(")]").AppendLine();
				}

				sb.Append(method.Parameters.Count > 0
					? "\t\tglobal::Mockolate.Setup.IReturnMethodSetupWithCallback"
					: "\t\tglobal::Mockolate.Setup.IReturnMethodSetup");
			}

			sb.Append('<');
			AppendSetupReturnType(sb, method);
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			sb.Append("> ");
			sb.Append(methodName).Append('(');
		}
		else
		{
			if (valueFlags?.All(x => x) == true)
			{
				if (allValuesPriority is not null)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(allValuesPriority).Append(")]").AppendLine();
				}

				sb.Append("\t\tglobal::Mockolate.Setup.IVoidMethodSetupParameterIgnorer");
			}
			else
			{
				if (hasOverloadResolutionPriority)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(omittedPriority ?? ComputeMethodOverloadPriority(@class, method, useParameters, valueFlags,
							perElementParams))
						.Append(")]").AppendLine();
				}

				sb.Append(method.Parameters.Count > 0
					? "\t\tglobal::Mockolate.Setup.IVoidMethodSetupWithCallback"
					: "\t\tglobal::Mockolate.Setup.IVoidMethodSetup");
			}

			if (method.Parameters.Count > 0)
			{
				sb.Append('<');
				int index = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					if (index++ > 0)
					{
						sb.Append(", ");
					}

					sb.AppendTypeOrWrapper(parameter.Type);
				}

				sb.Append('>');
			}

			sb.Append(' ').Append(methodName).Append('(');
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters").Append(forwardsLoneNull ? "? " : " ").Append("parameters)");
		}
		else
		{
			bool[] hasTrailingDefault = ComputeMethodTrailingDefaults(method, valueFlags, omittable, kept);

			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if ((emptyParams && parameter.IsParams) || i >= kept)
				{
					break;
				}

				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(PerElementArraySuffix(method))
						.Append(parameter.Name);
				}
				else if (isValueParam)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						sb.Append("params ");
					}

					sb.Append(parameter.ToNullableType()).Append(' ').Append(parameter.Name);
				}
				else
				{
					sb.Append(parameter.ToParameter());
					if (parameter.CanUseNullableParameterOverload())
					{
						sb.Append('?');
					}

					sb.Append(' ').Append(parameter.Name);
					if (hasTrailingDefault[i])
					{
						sb.Append(" = null");
					}
				}

				i++;
			}

			sb.Append(')');
		}

		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t");
			}
		}

		sb.Append(';').AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementSetupInterface(StringBuilder sb, Class @class, string mockRegistryName,
		string setupName, MemberType memberType, MemberIdTable memberIds, string memberIdPrefix, bool hasOverloadResolutionPriority,
		string? scopeExpression = null, bool useUnionOverloads = false)
#pragma warning restore S107
	{
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";

		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType && !property.Type.NeedsRefStructPipeline();
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append(
					"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\t").Append(GetPropertySetupType(property))
				.Append(" global::Mockolate.Mock.").Append(setupName).Append('.').Append(property.Name).AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvar propertySetup = new global::Mockolate.Setup.PropertySetup<")
				.AppendTypeOrWrapper(property.Type).Append(">(").Append(mockRegistryName).Append(", ")
				.Append(property.GetUniqueNameString()).Append(");")
				.AppendLine();
			sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupProperty(")
				.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property)).Append(", ")
				.Append(scopePrefix).Append("propertySetup);").AppendLine();
			sb.Append("\t\t\t\treturn propertySetup;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Events

		Func<Event, bool> eventSetupPredicate = @event
			=> @event.ExplicitImplementation is null && @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(eventSetupPredicate))
		{
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append(
					"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\tglobal::Mockolate.Setup.EventSetup global::Mockolate.Mock.")
				.Append(setupName).Append('.').Append(@event.Name).AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tglobal::Mockolate.Setup.EventSetup eventSetup = new global::Mockolate.Setup.EventSetup(")
				.Append(mockRegistryName).Append(", ").Append(@event.GetUniqueNameString()).Append(");").AppendLine();
			sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupEvent(")
				.Append(memberIdPrefix).Append(memberIds.GetEventSubscribeIdentifier(@event)).Append(", ")
				.Append(scopePrefix).Append("eventSetup);").AppendLine();
			sb.Append("\t\t\t\treturn eventSetup;").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate =
			indexer => indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, } &&
			           indexer.MemberType == memberType && !indexer.Type.NeedsRefStructPipeline();
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			int firstOmittable = FirstOmittableIndexerKey(@class, indexer);
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionIndexerSlotCombinations(indexer, firstOmittable))
				{
					AppendUnionIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds,
						memberIdPrefix, slots, scopeExpression);
				}

				continue;
			}

			foreach ((int keptCount, bool[]? valueFlags) in GenerateIndexerOverloads(indexer, firstOmittable))
			{
				AppendIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds, memberIdPrefix,
					keptCount, valueFlags, scopeExpression);
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(GetParametersOverloadKey).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Take(2).Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: true,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression,
						forwardsLoneNull: ForwardsLoneNull(@class, method, UseUnionOverloads(@class, method, useUnionOverloads)));
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters, allowOmission: true))
					{
						AppendUnionMethodSetupImplementation(sb, method, mockRegistryName, setupName, memberIds,
							memberIdPrefix, slots, scopeExpression: scopeExpression);
					}
				}
				else
				{
					int omittable = OmittableMethodParameterCount(@class, method, hasOverloadResolutionPriority);
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression, omittable: omittable);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
							memberIds, memberIdPrefix, scopeExpression: scopeExpression, perElementParams: true);
					}

					if (HasEmptyParamsOverload(@class, method, hasOverloadResolutionPriority))
					{
						AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
							memberIds, memberIdPrefix, valueFlags: EmptyParamsValueFlags(method), scopeExpression: scopeExpression,
							emptyParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
								memberIds, memberIdPrefix, valueFlags: valueFlags, scopeExpression: scopeExpression,
								omittable: omittable);
						}
					}
					else
					{
						foreach (bool[] valueFlags in GenerateValueFlagsAboveMaxExplicitParameters(method.Parameters))
						{
							AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
								memberIds, memberIdPrefix, valueFlags: valueFlags, scopeExpression: scopeExpression,
								omittable: omittable);
						}
					}

					foreach ((int keptCount, bool[]? valueFlags) in GenerateOmittedMethodOverloads(method, omittable))
					{
						AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, useParameters: false,
							memberIds, memberIdPrefix, valueFlags: valueFlags, scopeExpression: scopeExpression,
							omittable: omittable, keptCount: keptCount);
					}
				}
			}
		}

		#endregion
	}
#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMethodSetupImplementation(StringBuilder sb, Method method, string mockRegistryName,
		string setupName,
		bool useParameters, MemberIdTable memberIds, string memberIdPrefix,
		string? methodNameOverride = null, bool[]? valueFlags = null,
		string? scopeExpression = null, bool perElementParams = false, bool forwardsLoneNull = false,
		bool emptyParams = false, int omittable = 0, int? keptCount = null)
	{
		// Setup-side carve-out: methods using a generic type parameter that declares
		// `allows ref struct`, methods returning a non-span ref struct, and delegates with a ref-struct
		// parameter have no setup interface declaration (see AppendMethodSetupDefinition), so no
		// explicit implementation is emitted either.
		if (method.HasUnsupportedAllowsRefStructTypeParameter ||
		    method.ReturnType.NeedsRefStructPipeline() ||
		    method.IsDelegateWithUnsupportedRefStructParameter)
		{
			return;
		}

		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			// Emit exactly once: skip the useParameters=true variant (IParameters collection
			// overload is not offered for ref-struct methods) and every value-flag variant
			// (explicit-value overloads are incompatible with ref-struct T).
			if (useParameters || valueFlags is not null)
			{
				return;
			}

			AppendRefStructMethodSetupImplementation(sb, method, mockRegistryName, setupName, memberIds,
				memberIdPrefix, methodNameOverride, scopeExpression);
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		if (method.ReturnType != Type.Void)
		{
			if (valueFlags?.All(x => x) == true)
			{
				sb.Append("\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer");
			}
			else
			{
				sb.Append(method.Parameters.Count > 0
					? "\t\tglobal::Mockolate.Setup.IReturnMethodSetupWithCallback"
					: "\t\tglobal::Mockolate.Setup.IReturnMethodSetup");
			}

			sb.Append('<');
			AppendSetupReturnType(sb, method);
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			sb.Append("> global::Mockolate.Mock.").Append(setupName).Append('.');
			sb.Append(methodName).Append('(');
		}
		else
		{
			if (valueFlags?.All(x => x) == true)
			{
				sb.Append("\t\tglobal::Mockolate.Setup.IVoidMethodSetupParameterIgnorer");
			}
			else
			{
				sb.Append(method.Parameters.Count > 0
					? "\t\tglobal::Mockolate.Setup.IVoidMethodSetupWithCallback"
					: "\t\tglobal::Mockolate.Setup.IVoidMethodSetup");
			}

			if (method.Parameters.Count > 0)
			{
				sb.Append('<');
				int index = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					if (index++ > 0)
					{
						sb.Append(", ");
					}

					sb.AppendTypeOrWrapper(parameter.Type);
				}

				sb.Append('>');
			}

			sb.Append(" global::Mockolate.Mock.").Append(setupName).Append('.').Append(methodName).Append('(');
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters").Append(forwardsLoneNull ? "? " : " ").Append("parameters)");
		}
		else
		{
			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if ((emptyParams && parameter.IsParams) || i >= (keptCount ?? method.Parameters.Count))
				{
					break;
				}

				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(PerElementArraySuffix(method))
						.Append(parameter.Name);
				}
				else if (isValueParam)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						sb.Append("params ");
					}

					sb.Append(parameter.ToNullableType()).Append(' ').Append(parameter.Name);
				}
				else
				{
					sb.Append(parameter.ToParameter());
					if (parameter.CanUseNullableParameterOverload())
					{
						sb.Append('?');
					}

					sb.Append(' ').Append(parameter.Name);
				}

				i++;
			}

			sb.Append(')');
		}

		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", inheritsConstraints: true, isExplicitInterfaceImpl: true);
			}
		}

		sb.AppendLine();
		if (keptCount is not null)
		{
			sb.Append("\t\t\t=> ((global::Mockolate.Mock.").Append(setupName).Append(")this).").Append(methodName)
				.Append('(').Append(OmittedMethodForwardingArguments(method, keptCount.Value, valueFlags)).Append(");")
				.AppendLine();
			sb.AppendLine();
			return;
		}

		sb.AppendLine("\t\t{");
		if (emptyParams)
		{
			AppendEmptyParamsLocal(sb, method);
		}

		if (useParameters && forwardsLoneNull)
		{
			sb.Append("\t\t\tif (parameters is null)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\treturn ((global::Mockolate.Mock.").Append(setupName).Append(")this).").Append(methodName)
				.Append('(').Append(LoneNullArguments(method)).Append(");").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		// Fused literal path: when the call site is the bare-value overload (every parameter is a
		// literal value) we can store the values directly on the setup via WithLiteralValues and
		// skip the per-parameter IParameterMatch<T> allocations that WithParameterCollection
		// would otherwise force via Matchers.IsValue<T>(...). Gated to 1..4 parameters because that is
		// the arity range covered by the WithLiteralValues nested types.
		// A params value parameter matches element-wise by value (It.SequenceEquals), which the
		// reference-equality WithLiteralValues fast path cannot express — force the collection path.
		bool useLiteralValues = !useParameters &&
		                        valueFlags is { Length: > 0 and <= MaxExplicitParameters, } &&
		                        valueFlags.All(x => x) &&
		                        !HasParamsValueParameter(method, valueFlags) &&
		                        !method.Parameters.Any(p => p.RefKind == RefKind.Out ||
		                                                    p.RefKind == RefKind.Ref ||
		                                                    p.RefKind == RefKind.RefReadOnlyParameter);
		string setupType = GetMethodSetupType(method);
		// Look up the method's memberId from the table; all other fluent overloads for the same
		// method share the same id since they register setups for the same target method overload.
		string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
		MethodParameter[] matcherCandidates = useLiteralValues
			? method.Parameters.Where(MayHoldMatcher).ToArray()
			: [];
		if (matcherCandidates.Length > 0)
		{
			// Only a value that is a matcher needs the collection path; plain values keep the literal fast path.
			string matcherSetupVar = Helpers.GetUniqueLocalVariableName("matcherSetup", method.Parameters);
			sb.Append("\t\t\tif (")
				.Append(string.Join(" || ",
					matcherCandidates.Select(p => $"{p.Name} is global::Mockolate.Parameters.IParameter<{p.ToTypeOrWrapper()}>")))
				.Append(')').AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tvar ").Append(matcherSetupVar).Append(" = new ").Append(setupType);
			AppendParameterCollection();
			sb.Append(");").AppendLine();
			sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
				.Append(memberIdRef).Append(", ").Append(scopePrefix).Append(matcherSetupVar).Append(");").AppendLine();
			sb.Append("\t\t\t\treturn ").Append(matcherSetupVar).Append(';').AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.AppendLine();
		}

		string methodSetupVar = Helpers.GetUniqueLocalVariableName("methodSetup", method.Parameters);
		sb.Append("\t\t\tvar ").Append(methodSetupVar).Append(" = new ").Append(setupType);
		if (useParameters)
		{
			sb.Append(".WithParameters(this.").Append(mockRegistryName).Append(", ").Append(method.GetUniqueNameString())
				.Append(", parameters");
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", \"").Append(parameter.Name).Append('"');
			}

			sb.Append(");").AppendLine();
			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
				.Append(memberIdRef).Append(", ").Append(scopePrefix).Append(methodSetupVar).Append(");").AppendLine();
			sb.Append("\t\t\treturn ").Append(methodSetupVar).Append(';').AppendLine();
		}
		else
		{
			if (useLiteralValues)
			{
				sb.Append(".WithLiteralValues(this.").Append(mockRegistryName).Append(", ")
					.Append(method.GetUniqueNameString());
				foreach (MethodParameter parameter in method.Parameters)
				{
					sb.Append(", ").Append(parameter.Name);
				}

				sb.Append(");").AppendLine();
			}
			else
			{
				AppendParameterCollection();
				sb.Append(");").AppendLine();
			}

			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
				.Append(memberIdRef).Append(", ").Append(scopePrefix).Append(methodSetupVar).Append(");").AppendLine();
			sb.Append("\t\t\treturn ").Append(methodSetupVar).Append(';').AppendLine();
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();

		void AppendParameterCollection()
		{
			sb.Append(".WithParameterCollection(this.").Append(mockRegistryName).Append(", ")
				.Append(method.GetUniqueNameString());
			int j = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ");
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("new global::Mockolate.Internals.ParamsArrayParameterMatch<")
						.Append(parameter.Type.ElementType.Fullname).Append(">(").Append(parameter.Name)
						.Append(')');
				}
				else if (valueFlags?[j] == true)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						bool asParamsMatcher = !emptyParams && MayHoldParamsMatchers(parameter);
						if (asParamsMatcher)
						{
							AppendParamsMatcher(sb, parameter);
							sb.Append(" ?? ");
						}

						AppendParamsValuesMatcher(sb, parameter, matchesNull: !asParamsMatcher);
					}
					else if (MayHoldMatcher(parameter))
					{
						AppendValueOrMatcher(sb, parameter);
					}
					else
					{
						AppendNamedValueParameter(sb, parameter);
					}
				}
				else
				{
					AppendNamedParameter(sb, parameter, nullIsDeclaredDefault: omittable == 0);
				}

				j++;
			}
		}
	}
#pragma warning restore S107 // Methods should not have too many parameters

	private static string GetMethodSetupType(Method method)
	{
		StringBuilder sb = new();
		if (method.ReturnType != Type.Void)
		{
			sb.Append("global::Mockolate.Setup.ReturnMethodSetup<");
			AppendSetupReturnType(sb, method);

			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			sb.Append('>');
		}
		else
		{
			sb.Append("global::Mockolate.Setup.VoidMethodSetup");

			if (method.Parameters.Count > 0)
			{
				sb.Append('<');
				int index = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					if (index++ > 0)
					{
						sb.Append(", ");
					}

					sb.AppendTypeOrWrapper(parameter.Type);
				}

				sb.Append('>');
			}
		}

		return sb.ToString();
	}
#pragma warning restore S107 // Methods should not have too many parameters

	/// <summary>
	///     Emits the setup-interface declaration for a method that contains at least one
	///     ref-struct parameter. Gated on <c>NET9_0_OR_GREATER</c> because the declaration
	///     references <c>IRefStructVoidMethodSetup</c> / <c>IRefStructReturnMethodSetup</c>
	///     which themselves only compile on net9.0+.
	/// </summary>
	private static void AppendRefStructMethodSetupDefinition(StringBuilder sb, Method method,
		string? methodNameOverride)
	{
		// Out/ref/ref-readonly slots that don't go through the ref-struct setup pipeline have no setup
		// surface today. That includes regular non-ref-struct types and out/ref Span/ReadOnlySpan
		// (which route through the SpanWrapper/ReadOnlySpanWrapper carve-out, not IParameterMatch<T>).
		// The ref-struct narrow setup uses IParameterMatch<T> slots; mixing in a wrapper-based or
		// non-ref-struct IOutParameter<T> would require a wider pipeline. Continue to skip those.
		bool unsupported = method.Parameters.Any(p =>
			                   (p.RefKind == RefKind.Out || p.RefKind == RefKind.Ref ||
			                    p.RefKind == RefKind.RefReadOnlyParameter)
			                   && !p.NeedsRefStructPipeline()) ||
		                   (method.ReturnType.IsRefStruct && method.ReturnType.SpecialGenericType is not
			                   (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan));
		if (unsupported)
		{
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		string typeParams = string.Join(", ", method.Parameters.Select(p => p.Type.Fullname));
		string iface = method.ReturnType == Type.Void
			? $"global::Mockolate.Setup.IRefStructVoidMethodSetup<{typeParams}>"
			: $"global::Mockolate.Setup.IRefStructReturnMethodSetup<{method.ReturnType.Fullname}, {typeParams}>";

		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.AppendXmlSummary(
			$"Setup for the method {method.XmlDocReference}" +
			" — ref-struct parameter pipeline (narrow setup surface).");
		sb.Append("\t\t").Append(iface).Append(' ').Append(methodName).Append('(');
		int i = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.ToParameter()).Append("? ").Append(parameter.Name);
		}

		sb.Append(");").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the setup-interface implementation for a method that contains at least one
	///     ref-struct parameter. Constructs the concrete
	///     <c>RefStruct{Void,Return}MethodSetup&lt;T1..Tn&gt;</c>, registers it via
	///     <c>SetupMethod</c>, and returns it as its narrow interface.
	/// </summary>
#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendRefStructMethodSetupImplementation(StringBuilder sb, Method method,
		string mockRegistryName, string setupName, MemberIdTable memberIds, string memberIdPrefix,
		string? methodNameOverride, string? scopeExpression)
#pragma warning restore S107
	{
		bool unsupported = method.Parameters.Any(p =>
			                   (p.RefKind == RefKind.Out || p.RefKind == RefKind.Ref ||
			                    p.RefKind == RefKind.RefReadOnlyParameter)
			                   && !p.NeedsRefStructPipeline()) ||
		                   (method.ReturnType.IsRefStruct && method.ReturnType.SpecialGenericType is not
			                   (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan));
		if (unsupported)
		{
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		string typeParams = string.Join(", ", method.Parameters.Select(p => p.Type.Fullname));
		string iface = method.ReturnType == Type.Void
			? $"global::Mockolate.Setup.IRefStructVoidMethodSetup<{typeParams}>"
			: $"global::Mockolate.Setup.IRefStructReturnMethodSetup<{method.ReturnType.Fullname}, {typeParams}>";
		string concrete = method.ReturnType == Type.Void
			? $"global::Mockolate.Setup.RefStructVoidMethodSetup<{typeParams}>"
			: $"global::Mockolate.Setup.RefStructReturnMethodSetup<{method.ReturnType.Fullname}, {typeParams}>";

		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" global::Mockolate.Mock.").Append(setupName).Append('.')
			.Append(methodName).Append('(');
		int i = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.ToParameter()).Append("? ").Append(parameter.Name);
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t{").AppendLine();
		// The concrete setup constructor takes (string name, IParameterMatch<T1>? matcher1, ...).
		// All framework-provided matchers (It.IsAnyRefStruct<T>, It.IsRefStruct<T>, It.IsAny<T>, ...)
		// implement both IParameter<T> and IParameterMatch<T>, so a direct cast succeeds.
		sb.Append("\t\t\tvar methodSetup = new ").Append(concrete).Append('(')
			.Append(method.GetUniqueNameString());
		foreach (MethodParameter parameter in method.Parameters)
		{
			sb.Append(", (global::Mockolate.Parameters.IParameterMatch<").Append(parameter.Type.Fullname)
				.Append(">?)").Append(parameter.Name);
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
			.Append(memberIdPrefix).Append(memberIds.GetMethodIdentifier(method)).Append(", ")
			.Append(scopePrefix).Append("methodSetup);").AppendLine();
		sb.Append("\t\t\treturn methodSetup;").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	private static void AppendIndexerSetupDefinition(StringBuilder sb, Property indexer, int keptCount,
		bool[]? valueFlags = null, bool hasOverloadResolutionPriority = false)
	{
		// Ref-struct-keyed indexers go through the narrow RefStruct pipeline. Based on the
		// getter/setter combination we expose the appropriate facade type:
		//   - getter only  -> IRefStructIndexerGetterSetup<TValue, T1..Tn>
		//   - setter only  -> IRefStructIndexerSetterSetup<TValue, T1..Tn>
		//   - get + set    -> IRefStructIndexerSetup<TValue, T1..Tn> (combined)
		// Ref-struct setups take IParameter<T>? for every key (no by-value overloads), so only
		// the valueFlags==null caller emits; subsequent variant-flag calls are skipped.
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			if (valueFlags is not null)
			{
				return;
			}

			if (indexer.Getter is not null && indexer.Setter is null)
			{
				AppendRefStructIndexerGetterSetupDefinition(sb, indexer);
			}
			else if (indexer.Setter is not null && indexer.Getter is null)
			{
				AppendRefStructIndexerSetterSetupDefinition(sb, indexer);
			}
			else if (indexer.Getter is not null && indexer.Setter is not null)
			{
				AppendRefStructIndexerSetupDefinition(sb, indexer);
			}

			return;
		}

		sb.AppendXmlSummary(
			$"Setup for the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer {indexer.XmlDocReference}");
		string[] indexerNames = Enumerable.Range(1, indexer.IndexerParameters!.Value.Count)
			.Select(i => FormattableString.Invariant($"parameter{i}")).ToArray();
		AppendIndexerOverloadRemark(sb, indexerNames, keptCount, valueFlags, isVerify: false);
		if (hasOverloadResolutionPriority)
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
				.Append(IndexerOverloadPriority(keptCount, valueFlags)).Append(")]").AppendLine();
		}

		sb.Append("\t\t").Append(GetIndexerSetupType(indexer)).AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in indexer.IndexerParameters!)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append("> this[");
		AppendIndexerKeyParameters(sb, indexer, indexerNames, keptCount, valueFlags);
		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     The all-matcher overload ranks first and the others by their count of matcher keys. Overloads with different
	///     key counts never compete, so the left-out keys do not count.
	/// </summary>
	private static string IndexerOverloadPriority(int keptCount, bool[]? valueFlags)
		=> valueFlags?.Take(keptCount).Count(x => !x).ToString() ?? "int.MaxValue";

	/// <summary>
	///     The overload remark of a classic indexer overload, which names the keys that the overload leaves out.
	/// </summary>
	private static void AppendIndexerOverloadRemark(StringBuilder sb, string[] names, int keptCount,
		bool[]? valueFlags, bool isVerify)
	{
		string omission = keptCount < names.Length
			? $" It uses the declared default value for {string.Join(", ", names.Skip(keptCount).Select(name => $"<c>{name}</c>"))}."
			: "";
		AppendOverloadDifferentiatorRemark(sb, names.Take(keptCount).ToArray(), useParameters: false,
			valueFlags?.Take(keptCount).ToArray(), isVerify, omission);
	}

	/// <summary>
	///     The key parameters of a classic indexer overload: a value or a matcher for each of the first
	///     <paramref name="keptCount" /> keys.
	/// </summary>
	private static void AppendIndexerKeyParameters(StringBuilder sb, Property indexer, string[] names, int keptCount,
		bool[]? valueFlags)
	{
		MethodParameter[] parameters = indexer.IndexerParameters!.Value.AsArray();
		for (int i = 0; i < keptCount; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			MethodParameter parameter = parameters[i];
			if (valueFlags?[i] == true)
			{
				sb.Append(parameter.ToNullableType());
			}
			else
			{
				sb.Append(parameter.ToParameter());
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append('?');
				}
			}

			sb.Append(' ').Append(names[i]);
		}
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendIndexerSetupImplementation(StringBuilder sb, Property indexer, string mockRegistryName,
		string setupName, MemberIdTable memberIds, string memberIdPrefix, int keptCount,
		bool[]? valueFlags = null, string? scopeExpression = null)
#pragma warning restore S107
	{
		// Mirror AppendIndexerSetupDefinition: dispatch to the appropriate ref-struct facade
		// implementation depending on whether the indexer has a getter, a setter, or both.
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			if (valueFlags is not null)
			{
				return;
			}

			if (indexer.Getter is not null && indexer.Setter is null)
			{
				AppendRefStructIndexerGetterSetupImplementation(sb, indexer, mockRegistryName, setupName,
					memberIds, memberIdPrefix, scopeExpression);
			}
			else if (indexer.Setter is not null && indexer.Getter is null)
			{
				AppendRefStructIndexerSetterSetupImplementation(sb, indexer, mockRegistryName, setupName,
					memberIds, memberIdPrefix, scopeExpression);
			}
			else if (indexer.Getter is not null && indexer.Setter is not null)
			{
				AppendRefStructIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName,
					memberIds, memberIdPrefix, scopeExpression);
			}

			return;
		}

		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append(
				"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
			.AppendLine();
		sb.Append("\t\t").Append(GetIndexerSetupType(indexer)).AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in indexer.IndexerParameters!)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append("> global::Mockolate.Mock.").Append(setupName).Append(".this[");
		string[] indexerNames = Enumerable.Range(1, indexer.IndexerParameters!.Value.Count)
			.Select(i => FormattableString.Invariant($"parameter{i}")).ToArray();
		AppendIndexerKeyParameters(sb, indexer, indexerNames, keptCount, valueFlags);
		sb.Append(']').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar indexerSetup = new global::Mockolate.Setup.IndexerSetup<")
			.AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in indexer.IndexerParameters!)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append(">(").Append(mockRegistryName);
		int j = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			sb.Append(", ");

			bool isValueParam = valueFlags?[j] == true;
			string paramRef = indexerNames[j];
			if (j >= keptCount)
			{
				AppendNamedValueParameter(sb, parameter, DeclaredDefaultValue(parameter));
			}
			else if (isValueParam)
			{
				AppendNamedValueParameter(sb, parameter, paramRef);
			}
			else
			{
				sb.Append("global::Mockolate.CovariantParameterAdapter<")
					.AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(")
					.Append(paramRef);
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append(" ?? global::Mockolate.Internals.Matchers.IsNull<").Append(parameter.ToNullableType()).Append(">()");
				}

				sb.Append(')');
			}

			j++;
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupIndexer(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerGetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup);").AppendLine();
		sb.Append("\t\t\t\treturn indexerSetup;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the setup-interface declaration for a ref-struct-keyed getter-only indexer.
	///     Shape: <c>IRefStructIndexerGetterSetup&lt;TValue, T1..Tn&gt; this[IParameter&lt;T1&gt;?, ...] { get; }</c>.
	/// </summary>
	private static void AppendRefStructIndexerGetterSetupDefinition(StringBuilder sb, Property indexer)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.AppendXmlSummary(
			$"Setup for the ref-struct-keyed getter-only indexer {indexer.XmlDocReference}" +
			" — narrow setup surface (Returns / Throws / SkippingBaseClass).");
		sb.Append("\t\tglobal::Mockolate.Setup.IRefStructIndexerGetterSetup<")
			.Append(indexer.Type.Fullname);
		foreach (MethodParameter p in indexer.IndexerParameters!.Value)
		{
			sb.Append(", ").Append(p.Type.Fullname);
		}

		sb.Append("> this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? ").Append(FormattableString.Invariant($"parameter{i}"));
		}

		sb.Append("] { get; }").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the setup-interface implementation for a ref-struct-keyed getter-only indexer.
	///     Constructs the concrete <c>RefStructIndexerGetterSetup&lt;TValue, T1..Tn&gt;</c>,
	///     registers it via <c>SetupMethod</c>, and returns it as its narrow interface.
	/// </summary>
	private static void AppendRefStructIndexerGetterSetupImplementation(StringBuilder sb, Property indexer,
		string mockRegistryName, string setupName, MemberIdTable memberIds, string memberIdPrefix,
		string? scopeExpression)
	{
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		string typeParams = string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname));
		string iface =
			$"global::Mockolate.Setup.IRefStructIndexerGetterSetup<{indexer.Type.Fullname}, {typeParams}>";
		string concrete =
			$"global::Mockolate.Setup.RefStructIndexerGetterSetup<{indexer.Type.Fullname}, {typeParams}>";
		string indexerName = $"\"{indexer.ContainingType}.get_Item\"";

		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" global::Mockolate.Mock.").Append(setupName).Append(".this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? parameter").Append(i);
		}

		sb.Append(']').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar indexerSetup = new ").Append(concrete).Append('(').Append(indexerName);
		int k = 0;
		foreach (MethodParameter keyParam in indexer.IndexerParameters.Value)
		{
			k++;
			sb.Append(", (global::Mockolate.Parameters.IParameterMatch<")
				.Append(keyParam.Type.Fullname).Append(">?)parameter").Append(k);
		}

		sb.Append(");").AppendLine();
		// Re-use the generic SetupMethod slot. The setup's MatchesInteraction filter on its name
		// ensures it only participates in get_Item lookups.
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerGetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup);").AppendLine();
		sb.Append("\t\t\t\treturn indexerSetup;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the setup-interface declaration for a ref-struct-keyed setter-only indexer.
	///     Shape: <c>IRefStructIndexerSetterSetup&lt;TValue, T1..Tn&gt; this[IParameter&lt;T1&gt;?, ...] { get; }</c>.
	/// </summary>
	private static void AppendRefStructIndexerSetterSetupDefinition(StringBuilder sb, Property indexer)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.AppendXmlSummary(
			$"Setup for the ref-struct-keyed setter-only indexer {indexer.XmlDocReference}" +
			" — narrow setter surface (OnSet / Throws / SkippingBaseClass).");
		sb.Append("\t\tglobal::Mockolate.Setup.IRefStructIndexerSetterSetup<")
			.Append(indexer.Type.Fullname);
		foreach (MethodParameter p in indexer.IndexerParameters!.Value)
		{
			sb.Append(", ").Append(p.Type.Fullname);
		}

		sb.Append("> this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? parameter").Append(i);
		}

		sb.Append("] { get; }").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	private static void AppendRefStructIndexerSetterSetupImplementation(StringBuilder sb, Property indexer,
		string mockRegistryName, string setupName, MemberIdTable memberIds, string memberIdPrefix,
		string? scopeExpression)
	{
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		string typeParams = string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname));
		string iface =
			$"global::Mockolate.Setup.IRefStructIndexerSetterSetup<{indexer.Type.Fullname}, {typeParams}>";
		string concrete =
			$"global::Mockolate.Setup.RefStructIndexerSetterSetup<{indexer.Type.Fullname}, {typeParams}>";
		string indexerName = $"\"{indexer.ContainingType}.set_Item\"";

		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" global::Mockolate.Mock.").Append(setupName).Append(".this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? parameter").Append(i);
		}

		sb.Append(']').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar indexerSetup = new ").Append(concrete).Append('(').Append(indexerName);
		int k = 0;
		foreach (MethodParameter keyParam in indexer.IndexerParameters.Value)
		{
			k++;
			sb.Append(", (global::Mockolate.Parameters.IParameterMatch<")
				.Append(keyParam.Type.Fullname).Append(">?)parameter").Append(k);
		}

		sb.Append(");").AppendLine();
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerSetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup);").AppendLine();
		sb.Append("\t\t\t\treturn indexerSetup;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits the setup-interface declaration for a ref-struct-keyed get+set indexer.
	///     Shape: <c>IRefStructIndexerSetup&lt;TValue, T1..Tn&gt; this[IParameter&lt;T1&gt;?, ...] { get; }</c>.
	/// </summary>
	private static void AppendRefStructIndexerSetupDefinition(StringBuilder sb, Property indexer)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.AppendXmlSummary(
			$"Setup for the ref-struct-keyed get+set indexer {indexer.XmlDocReference}" +
			" — combined getter/setter facade.");
		sb.Append("\t\tglobal::Mockolate.Setup.IRefStructIndexerSetup<")
			.Append(indexer.Type.Fullname);
		foreach (MethodParameter p in indexer.IndexerParameters!.Value)
		{
			sb.Append(", ").Append(p.Type.Fullname);
		}

		sb.Append("> this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? parameter").Append(i);
		}

		sb.Append("] { get; }").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	private static void AppendRefStructIndexerSetupImplementation(StringBuilder sb, Property indexer,
		string mockRegistryName, string setupName, MemberIdTable memberIds, string memberIdPrefix,
		string? scopeExpression)
	{
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";
		string typeParams = string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname));
		string iface =
			$"global::Mockolate.Setup.IRefStructIndexerSetup<{indexer.Type.Fullname}, {typeParams}>";
		string concrete =
			$"global::Mockolate.Setup.RefStructIndexerSetup<{indexer.Type.Fullname}, {typeParams}>";
		string getterName = $"\"{indexer.ContainingType}.get_Item\"";
		string setterName = $"\"{indexer.ContainingType}.set_Item\"";

		sb.Append("#if NET9_0_OR_GREATER").AppendLine();
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t").Append(iface).Append(" global::Mockolate.Mock.").Append(setupName).Append(".this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append("global::Mockolate.Parameters.IParameter<").Append(parameter.Type.Fullname)
				.Append(">? parameter").Append(i);
		}

		sb.Append(']').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tvar indexerSetup = new ").Append(concrete).Append('(').Append(getterName)
			.Append(", ").Append(setterName);
		int k = 0;
		foreach (MethodParameter keyParam in indexer.IndexerParameters.Value)
		{
			k++;
			sb.Append(", (global::Mockolate.Parameters.IParameterMatch<")
				.Append(keyParam.Type.Fullname).Append(">?)parameter").Append(k);
		}

		sb.Append(");").AppendLine();
		// Register both the inner getter and setter. Each has its own MatchesInteraction name
		// filter so they participate only in their own accessor's dispatch loop.
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerGetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup.Getter);").AppendLine();
		sb.Append("\t\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
			.Append(memberIdPrefix).Append(memberIds.GetIndexerSetIdentifier(indexer)).Append(", ")
			.Append(scopePrefix).Append("indexerSetup.Setter);").AppendLine();
		sb.Append("\t\t\t\treturn indexerSetup;").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.Append("#endif").AppendLine();
		sb.AppendLine();
	}

	private static void AppendIndexerVerifyDefinition(StringBuilder sb, Property indexer, string verifyName,
		int keptCount, bool[]? valueFlags = null, bool hasOverloadResolutionPriority = false)
	{
		// Ref-struct-keyed indexers: no Verify surface — same rationale as the setup path.
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		sb.AppendXmlSummary(
			$"Verify interactions with the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer {indexer.XmlDocReference}.");
		string[] names = indexer.IndexerParameters!.Value.Select(p => p.Name).ToArray();
		AppendIndexerOverloadRemark(sb, names, keptCount, valueFlags, isVerify: true);
		if (hasOverloadResolutionPriority)
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
				.Append(IndexerOverloadPriority(keptCount, valueFlags)).Append(")]").AppendLine();
		}

		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(" this[");
		AppendIndexerKeyParameters(sb, indexer, names, keptCount, valueFlags);
		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendIndexerVerifyImplementation(StringBuilder sb, Property indexer, string mockRegistryName,
		string verifyName, MemberIdTable memberIds, string memberIdPrefix, bool useTypedRecords, int keptCount,
		bool[]? valueFlags = null)
#pragma warning restore S107
	{
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		bool useTypedRecordsForIndexer = useTypedRecords && IsTypedRecordingEligibleIndexer(indexer);
		string indexerGetMemberId = useTypedRecordsForIndexer
			? memberIdPrefix + memberIds.GetIndexerGetIdentifier(indexer)
			: "-1";
		string indexerSetMemberId = useTypedRecordsForIndexer
			? memberIdPrefix + memberIds.GetIndexerSetIdentifier(indexer)
			: "-1";

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append(
				"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
			.AppendLine();
		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(' ').Append(verifyName).Append(".this[");
		MethodParameter[] keys = indexer.IndexerParameters!.Value.AsArray();
		AppendIndexerKeyParameters(sb, indexer, keys.Select(p => p.Name).ToArray(), keptCount, valueFlags);
		sb.Append(']').AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		// A left-out key becomes a local matcher of its declared default value, so that the matching and the failure
		// message treat it like a matcher argument.
		for (int keyIndex = keptCount; keyIndex < keys.Length; keyIndex++)
		{
			sb.Append("\t\t\t\t").Append(keys[keyIndex].ToParameter()).Append(' ').Append(keys[keyIndex].Name)
				.Append(" = new global::Mockolate.Internals.LiteralParameterMatch<")
				.Append(keys[keyIndex].ToNullableType()).Append(">(").Append(keys[keyIndex].ExplicitDefaultValue)
				.Append(");").AppendLine();
		}

		bool useTypedVerify = indexer.IndexerParameters.Value.Count is >= 1 and <= 4;
		if (useTypedVerify)
		{
			PropertyAccessors interceptedAccessors = GetInterceptedAccessors(indexer);
			string typedVerifyType = interceptedAccessors switch
			{
				PropertyAccessors.GetOnly => "VerificationIndexerGetterResult",
				PropertyAccessors.SetOnly => "VerificationIndexerSetterResult",
				_ => "VerificationIndexerResult",
			};
			// The narrow views take only the member id of the accessor they expose.
			string verifyMemberIds = interceptedAccessors switch
			{
				PropertyAccessors.GetOnly => indexerGetMemberId,
				PropertyAccessors.SetOnly => indexerSetMemberId,
				_ => $"{indexerGetMemberId}, {indexerSetMemberId}",
			};
			sb.Append("\t\t\t\treturn new global::Mockolate.Verify.").Append(typedVerifyType).Append('<')
				.Append(verifyName);
			foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			// Only Set(...) needs the value type, so the getter-only result is keyed by the parameters alone.
			if (interceptedAccessors != PropertyAccessors.GetOnly)
			{
				sb.Append(", ").AppendTypeOrWrapper(indexer.Type);
			}

			sb.Append(">(this, this.").Append(mockRegistryName)
				.Append(", ").Append(verifyMemberIds).Append(',').AppendLine();

			AppendIndexerVerifyTypedMatches(sb, indexer.IndexerParameters.Value, keptCount, valueFlags);
		}
		else
		{
			PropertyAccessors interceptedAccessors = GetInterceptedAccessors(indexer);
			// The narrow views take only the member id and predicate of the accessor they expose.
			switch (interceptedAccessors)
			{
				case PropertyAccessors.GetOnly:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerGetterResult<")
						.Append(verifyName);
					foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
					{
						sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
					}

					sb.Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerGetMemberId).Append(',').AppendLine();
					break;
				case PropertyAccessors.SetOnly:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerSetterResult<")
						.Append(verifyName);
					foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
					{
						sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
					}

					sb.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerSetMemberId).Append(',').AppendLine();
					break;
				default:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerResult<").Append(verifyName)
						.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerGetMemberId).Append(", ").Append(indexerSetMemberId).Append(',').AppendLine();
					break;
			}

			if (interceptedAccessors != PropertyAccessors.SetOnly)
			{
				sb.Append("\t\t\t\t\tinteraction => interaction is global::Mockolate.Interactions.IndexerGetterAccess<");
				int ti = 0;
				foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
				{
					if (ti > 0)
					{
						sb.Append(", ");
					}

					sb.AppendTypeOrWrapper(parameter.Type);
					ti++;
				}

				sb.Append("> g");
				AppendIndexerVerifyParameterMatches(sb, indexer.IndexerParameters.Value, keptCount, valueFlags, "g");
				sb.Append(',').AppendLine();
			}

			if (interceptedAccessors != PropertyAccessors.GetOnly)
			{
				sb.Append(
					"\t\t\t\t\t(interaction, value) => interaction is global::Mockolate.Interactions.IndexerSetterAccess<");
				foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
				{
					sb.AppendTypeOrWrapper(parameter.Type).Append(", ");
				}

				sb.AppendTypeOrWrapper(indexer.Type).Append("> s");
				AppendIndexerVerifyParameterMatches(sb, indexer.IndexerParameters.Value, keptCount, valueFlags, "s");
				sb.Append(" && value.Matches(s.TypedValue),").AppendLine();
			}
		}

		sb.Append("\t\t\t\t\t() => global::System.String.Format(\"[");

		for (int k = 0; k < indexer.IndexerParameters.Value.Count; k++)
		{
			if (k > 0)
			{
				sb.Append(", ");
			}

			sb.Append('{').Append(k).Append('}');
		}

		sb.Append("]\"");
		foreach (string? parameterName in indexer.IndexerParameters.Value.Select(p => p.Name))
		{
			sb.Append(", global::Mockolate.Internals.ValueFormatter.Format((object?)").Append(parameterName).Append(')');
		}

		sb.Append("));").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits one comma-terminated <c>IParameterMatch&lt;T&gt;</c> argument per indexer key for the typed
	///     <c>VerificationIndexerResult&lt;TSubject, T1, ..., TParameter&gt;</c> family. Each match is either
	///     built via <c>CovariantParameterAdapter</c> (for the matcher overload) or via <c>It.Is</c> (for the
	///     value overload).
	/// </summary>
	private static void AppendIndexerVerifyTypedMatches(StringBuilder sb,
		EquatableArray<MethodParameter> parameters, int keptCount, bool[]? valueFlags)
	{
		int j = 0;
		foreach (MethodParameter parameter in parameters)
		{
			sb.Append("\t\t\t\t\t");
			bool isValueParam = valueFlags?[j] == true;
			if (isValueParam)
			{
				sb.Append("(global::Mockolate.Parameters.IParameterMatch<").AppendTypeOrWrapper(parameter.Type)
					.Append(">)global::Mockolate.It.Is<").AppendTypeOrWrapper(parameter.Type).Append(">(")
					.Append(parameter.Name).Append(", \"").Append(parameter.Name).Append("\")");
			}
			else
			{
				sb.Append("global::Mockolate.CovariantParameterAdapter<").AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(").Append(parameter.Name);
				if (j < keptCount && parameter.CanUseNullableParameterOverload())
				{
					sb.Append(" ?? global::Mockolate.Internals.Matchers.IsNull<").Append(parameter.ToNullableType())
						.Append(">()");
				}

				sb.Append(')');
			}

			sb.Append(',').AppendLine();
			j++;
		}
	}

	private static void AppendIndexerVerifyParameterMatches(StringBuilder sb,
		EquatableArray<MethodParameter> parameters, int keptCount, bool[]? valueFlags, string interactionVar)
	{
		int j = 0;
		foreach (MethodParameter parameter in parameters)
		{
			sb.Append(" && ");
			bool isValueParam = valueFlags?[j] == true;
			if (isValueParam)
			{
				sb.Append("global::System.Collections.Generic.EqualityComparer<")
					.AppendTypeOrWrapper(parameter.Type).Append(">.Default.Equals(").Append(parameter.Name)
					.Append(", ").Append(interactionVar).Append(".Parameter").Append(j + 1).Append(')');
			}
			else
			{
				sb.Append("global::Mockolate.CovariantParameterAdapter<").AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(").Append(parameter.Name);
				if (j < keptCount && parameter.CanUseNullableParameterOverload())
				{
					sb.Append(" ?? global::Mockolate.Internals.Matchers.IsNull<").Append(parameter.ToNullableType())
						.Append(">()");
				}

				sb.Append(").Matches(").Append(interactionVar).Append(".Parameter").Append(j + 1).Append(')');
			}

			j++;
		}
	}

	#endregion Setup Helpers

	#region Raise Helpers

	/// <summary>
	///     Whether the <c>IDefaultEventParameters</c> overload can be emitted for <paramref name="event" />.
	/// </summary>
	/// <remarks>
	///     The overload fills every delegate parameter through <c>Generate&lt;T&gt;</c>, which carries no
	///     <c>allows ref struct</c> anti-constraint (its body type-tests an <see langword="object" /> against <c>T</c>),
	///     so a ref-struct argument - <c>Span&lt;T&gt;</c> included, as the event path has no wrapper
	///     carve-out - would fail with CS9244. Such events keep the strongly-typed <c>Raise</c> overload,
	///     which passes the argument straight to the backing delegate.
	/// </remarks>
	private static bool HasDefaultParametersRaiseOverload(Event @event)
		=> @event.Delegate.Parameters.Count > 0 &&
		   !@event.Delegate.Parameters.Any(parameter => parameter.Type.IsRefStruct);

	private static void DefineRaiseInterface(StringBuilder sb, Class @class, MemberType memberType)
	{
		Func<Event, bool> predicate = @event => @event.ExplicitImplementation is null &&
		                                        @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(predicate))
		{
			sb.AppendXmlSummary(
				$"Raise the {@event.XmlDocReference} event.");
			sb.Append("\t\tvoid ").Append(@event.Name).Append('(')
				.Append(FormatParametersWithTypeAndName(@event.Delegate.Parameters)).Append(");").AppendLine();
			sb.AppendLine();
		}

		foreach (Event @event in @class.AllEvents()
			         .Where(predicate)
			         .GroupBy(m => m.Name)
			         .Where(g => g.Take(2).Count() == 1)
			         .Select(g => g.Single())
			         .Where(HasDefaultParametersRaiseOverload))
		{
			sb.AppendXmlSummary(
				$"Raise the {@event.XmlDocReference} event.");
			sb.Append("\t\tvoid ").Append(@event.Name)
				.Append("(global::Mockolate.Parameters.IDefaultEventParameters parameters);").AppendLine();
			sb.AppendLine();
		}
	}

	private static void ImplementRaiseInterface(StringBuilder sb, Class @class, string mockRegistryName,
		string raiseOnName, MemberType memberType)
	{
		string mockRegistry =
			memberType == MemberType.Static ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		Func<Event, bool> predicate = @event => @event.ExplicitImplementation is null &&
		                                        @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(predicate))
		{
			string backingFieldAccess = @event.IsStatic
				? $"{@event.GetBackingFieldName()}.Value"
				: $"this.{@event.GetBackingFieldName()}";
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tvoid ").Append(raiseOnName).Append('.').Append(@event.Name).Append('(')
				.Append(FormatParametersWithTypeAndName(@event.Delegate.Parameters))
				.Append(')').AppendLine();
			sb.AppendLine("\t\t{");
			// Pre-assign declared `out` parameters: when no subscriber exists the conditional
			// Invoke is skipped, so the compiler-required definite assignment must come from us.
			foreach (MethodParameter p in @event.Delegate.Parameters)
			{
				if (p.RefKind == RefKind.Out)
				{
					sb.Append("\t\t\t").Append(p.Name).Append(" = default!;").AppendLine();
				}
			}

			sb.Append("\t\t\t").Append(backingFieldAccess).Append("?.Invoke(");
			if (@event.Delegate.Parameters.Count > 0)
			{
				sb.Append(FormatParametersAsNames(@event.Delegate.Parameters));
			}

			sb.Append(");").AppendLine();
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}

		foreach (Event @event in @class.AllEvents()
			         .Where(predicate)
			         .GroupBy(m => m.Name)
			         .Where(g => g.Take(2).Count() == 1)
			         .Select(g => g.Single())
			         .Where(HasDefaultParametersRaiseOverload))
		{
			string backingFieldAccess = @event.IsStatic
				? $"{@event.GetBackingFieldName()}.Value"
				: $"this.{@event.GetBackingFieldName()}";
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append("\t\tvoid ").Append(raiseOnName).Append('.').Append(@event.Name)
				.Append("(global::Mockolate.Parameters.IDefaultEventParameters parameters)")
				.AppendLine();
			sb.AppendLine("\t\t{");
			sb.Append("\t\t\tglobal::Mockolate.MockBehavior mockBehavior = ").Append(mockRegistry).Append(".Behavior;")
				.AppendLine();
			// ref/out delegate parameters can't accept arbitrary expressions — bind each generated
			// default to a local so it has an addressable storage location for the Invoke call.
			bool hasByRef = @event.Delegate.Parameters.Any(p
				=> p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out ||
				   p.RefKind == RefKind.In || p.RefKind == RefKind.RefReadOnlyParameter);

			List<string> argNames = new();
			int idx = 0;
			foreach (MethodParameter p in @event.Delegate.Parameters)
			{
				idx++;
				string defaultExpr = $"mockBehavior.DefaultValue.Generate(default({p.Type.Fullname.TrimEnd('?')}))";
				if (hasByRef)
				{
					string local = FormattableString.Invariant($"__arg{idx}");
					sb.Append("\t\t\t").Append(p.Type.Fullname).Append(' ').Append(local).Append(" = ")
						.Append(defaultExpr).Append(';').AppendLine();
					argNames.Add($"{p.RefKind.GetString()}{local}");
				}
				else
				{
					argNames.Add(defaultExpr);
				}
			}

			sb.Append("\t\t\t").Append(backingFieldAccess).Append("?.Invoke(");

			if (@event.Delegate.Parameters.Count > 0)
			{
				sb.Append(string.Join(", ", argNames));
			}

			sb.Append(");").AppendLine();
			sb.AppendLine("\t\t}");
			sb.AppendLine();
		}
	}

	#endregion Raise Helpers

	#region Verify Helpers

	private static void DefineVerifyInterface(StringBuilder sb, Class @class, string verifyName, MemberType memberType,
		bool hasOverloadResolutionPriority, bool useUnionOverloads = false)
	{
		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType && !property.Type.NeedsRefStructPipeline();
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			sb.AppendXmlSummary(
				$"Verify interactions with the {property.Type.Fullname.EscapeForXmlDoc()} property {property.XmlDocReference}.");
			sb.Append("\t\t").Append(GetPropertyVerifyType(property, verifyName)).Append(' ')
				.Append(property.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate =
			indexer => indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, } &&
			           indexer.MemberType == memberType && !indexer.Type.NeedsRefStructPipeline();
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			int firstOmittable = FirstOmittableIndexerKey(@class, indexer);
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionIndexerSlotCombinations(indexer, firstOmittable))
				{
					AppendUnionIndexerVerifyDefinition(sb, indexer, verifyName, slots);
				}

				continue;
			}

			foreach ((int keptCount, bool[]? valueFlags) in GenerateIndexerOverloads(indexer, firstOmittable))
			{
				AppendIndexerVerifyDefinition(sb, indexer, verifyName, keptCount, valueFlags,
					hasOverloadResolutionPriority);
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(GetParametersOverloadKey).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Take(2).Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: true,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority,
						forwardsLoneNull: ForwardsLoneNull(@class, method, UseUnionOverloads(@class, method, useUnionOverloads)));
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters, allowOmission: true))
					{
						AppendUnionMethodVerifyDefinition(sb, @class, method, verifyName, slots);
					}
				}
				else
				{
					int omittable = OmittableMethodParameterCount(@class, method, hasOverloadResolutionPriority);
					AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, perElementParams: true);
					}

					if (HasEmptyParamsOverload(@class, method, hasOverloadResolutionPriority))
					{
						AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false, valueFlags: EmptyParamsValueFlags(method),
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, emptyParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
						}
					}
					else
					{
						foreach (bool[] valueFlags in GenerateValueFlagsAboveMaxExplicitParameters(method.Parameters))
						{
							AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable);
						}
					}

					foreach ((int keptCount, bool[]? valueFlags) in GenerateOmittedMethodOverloads(method, omittable))
					{
						AppendMethodVerifyDefinition(sb, @class, method, verifyName, useParameters: false, valueFlags: valueFlags,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, omittable: omittable,
							keptCount: keptCount);
					}
				}
			}
		}

		#endregion

		#region Events

		Func<Event, bool> eventPredicate = @event => @event.ExplicitImplementation is null &&
		                                             @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(eventPredicate))
		{
			sb.AppendXmlSummary(
				$"Verify subscriptions on the {@event.Name} event of {@event.XmlDocReference}.");
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationEventResult<").Append(verifyName).Append("> ")
				.Append(@event.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMethodVerifyDefinition(StringBuilder sb, Class @class, Method method, string verifyName,
		bool useParameters, string? methodNameOverride = null, bool[]? valueFlags = null,
		bool hasOverloadResolutionPriority = false, bool perElementParams = false, bool forwardsLoneNull = false,
		bool emptyParams = false, int omittable = 0, int? keptCount = null)
#pragma warning restore S107
	{
		// For methods with ref-struct parameters, skip Verify emission entirely. The
		// VerificationResult pipeline takes IParameter<T>? matchers that then feed into
		// closures over the captured call values — incompatible with a ref-struct T. Callers
		// fall back to reading `mock.Mock.Registry.Interactions.OfType<RefStructMethodInvocation>()`
		// for count-based verification. See brief's "verify count" guidance.
		//
		// A non-span ref-struct return is skipped for a different reason: the body is a
		// NotSupportedException stub (or, for a virtual class method, a passthrough to base), so the
		// call never reaches interaction recording and the surface could only ever report zero.
		// Matches the property/indexer carve-out.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()) || method.ReturnType.NeedsRefStructPipeline())
		{
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		int kept = keptCount ?? method.Parameters.Count;
		sb.Append("\t\t/// <summary>").AppendLine();
		if (methodNameOverride is null)
		{
			sb.Append("\t\t///     Verify invocations for the method ").Append(method.XmlDocReference);
		}
		else
		{
			sb.Append("\t\t///     Verify invocations for the delegate ").Append(@class.XmlDocReference);
		}

		if (emptyParams)
		{
			sb.Append(EmptyParamsSummary(method));
		}
		else if (useParameters)
		{
			sb.Append(" with the given <paramref name=\"parameters\"/>");
		}
		else if (kept > 0)
		{
			sb.Append(" with the given ")
				.Append(string.Join(", ", method.Parameters.Take(kept).Select(p => $"<paramref name=\"{p.Name.TrimStart('@')}\"/>")));
		}

		sb.Append('.').AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		if (!emptyParams)
		{
			AppendMethodOverloadRemark(sb, method, useParameters, valueFlags, isVerify: true, kept);
		}

		string? omittedPriority = keptCount is not null ? OmittedMethodOverloadPriority(method, kept, valueFlags) : null;
		if (valueFlags?.All(x => x) == true || (method.Parameters.Count == 0 && !useParameters))
		{
			string? allValuesPriority = hasOverloadResolutionPriority && method.Parameters.Count > 0 &&
			                            valueFlags is not null
				? omittedPriority ?? ComputeAllValuesOverloadPriority(@class, method, valueFlags, emptyParams)
				: null;
			if (allValuesPriority is not null)
			{
				sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
					.Append(allValuesPriority).Append(")]").AppendLine();
			}

			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName)
				.Append(">.IgnoreParameters ").Append(methodName).Append('(');
		}
		else
		{
			if (hasOverloadResolutionPriority)
			{
				sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
					.Append(omittedPriority ?? ComputeMethodOverloadPriority(@class, method, useParameters, valueFlags,
						perElementParams))
					.Append(")]").AppendLine();
			}

			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName).Append("> ")
				.Append(methodName).Append('(');
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters").Append(forwardsLoneNull ? "? " : " ").Append("parameters");
		}
		else
		{
			bool[] hasTrailingDefault = ComputeMethodTrailingDefaults(method, valueFlags, omittable, kept);

			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if ((emptyParams && parameter.IsParams) || i >= kept)
				{
					break;
				}

				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(PerElementArraySuffix(method))
						.Append(parameter.Name);
				}
				else if (isValueParam)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						sb.Append("params ");
					}

					sb.Append(parameter.ToNullableType()).Append(' ').Append(parameter.Name);
				}
				else
				{
					sb.AppendVerifyParameter(parameter);
					if (parameter.CanUseNullableParameterOverload())
					{
						sb.Append('?');
					}

					sb.Append(' ').Append(parameter.Name);
					if (hasTrailingDefault[i])
					{
						sb.Append(" = null");
					}
				}

				i++;
			}
		}

		sb.Append(')');
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t");
			}
		}

		sb.Append(';').AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementVerifyInterface(StringBuilder sb, Class @class, string mockRegistryName,
		string verifyName, MemberType memberType, MemberIdTable memberIds, string memberIdPrefix, bool hasOverloadResolutionPriority,
		bool useTypedRecords = true, bool useUnionOverloads = false)
#pragma warning restore S107
	{
		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType && !property.Type.NeedsRefStructPipeline();
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			bool useTypedRecordsForProperty = useTypedRecords && IsTypedRecordingEligibleProperty(property);
			string propertyGetMemberId = useTypedRecordsForProperty
				? memberIdPrefix + memberIds.GetPropertyGetIdentifier(property)
				: "-1";
			string propertySetMemberId = useTypedRecordsForProperty
				? memberIdPrefix + memberIds.GetPropertySetIdentifier(property)
				: "-1";
			sb.Append("\t\t/// <inheritdoc />").AppendLine();
			sb.Append(
					"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
				.AppendLine();
			string verifyType = GetPropertyVerifyType(property, verifyName);
			// The narrow views take only the member id of the accessor they expose.
			string verifyMemberIds = GetInterceptedAccessors(property) switch
			{
				PropertyAccessors.GetOnly => propertyGetMemberId,
				PropertyAccessors.SetOnly => propertySetMemberId,
				_ => $"{propertyGetMemberId}, {propertySetMemberId}",
			};
			sb.Append("\t\t").Append(verifyType).Append(' ').Append(verifyName).Append('.').Append(property.Name)
				.AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\treturn new ").Append(verifyType).Append("(this, this.").Append(mockRegistryName)
				.Append(", ").Append(verifyMemberIds).Append(", ")
				.Append(property.GetUniqueNameString()).Append(");").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate = indexer
			=> indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, IndexerParameters: not null, } &&
			   indexer.MemberType == memberType && !indexer.Type.NeedsRefStructPipeline();
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			int firstOmittable = FirstOmittableIndexerKey(@class, indexer);
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionIndexerSlotCombinations(indexer, firstOmittable))
				{
					AppendUnionIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds,
						memberIdPrefix, useTypedRecords, slots);
				}

				continue;
			}

			foreach ((int keptCount, bool[]? valueFlags) in GenerateIndexerOverloads(indexer, firstOmittable))
			{
				AppendIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds, memberIdPrefix,
					useTypedRecords, keptCount, valueFlags);
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(GetParametersOverloadKey).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Take(2).Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: true,
						memberIds, memberIdPrefix, useTypedRecords,
						forwardsLoneNull: ForwardsLoneNull(@class, method, UseUnionOverloads(@class, method, useUnionOverloads)));
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
						memberIds, memberIdPrefix, useTypedRecords);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters, allowOmission: true))
					{
						AppendUnionMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, memberIds,
							memberIdPrefix, useTypedRecords, slots);
					}
				}
				else
				{
					int omittable = OmittableMethodParameterCount(@class, method, hasOverloadResolutionPriority);
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
						memberIds, memberIdPrefix, useTypedRecords, omittable: omittable);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
							memberIds, memberIdPrefix, useTypedRecords, perElementParams: true);
					}

					if (HasEmptyParamsOverload(@class, method, hasOverloadResolutionPriority))
					{
						AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
							memberIds, memberIdPrefix, useTypedRecords, valueFlags: EmptyParamsValueFlags(method), emptyParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
								memberIds, memberIdPrefix, useTypedRecords, valueFlags: valueFlags, omittable: omittable);
						}
					}
					else
					{
						foreach (bool[] valueFlags in GenerateValueFlagsAboveMaxExplicitParameters(method.Parameters))
						{
							AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
								memberIds, memberIdPrefix, useTypedRecords, valueFlags: valueFlags, omittable: omittable);
						}
					}

					foreach ((int keptCount, bool[]? valueFlags) in GenerateOmittedMethodOverloads(method, omittable))
					{
						AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, useParameters: false,
							memberIds, memberIdPrefix, useTypedRecords, valueFlags: valueFlags, omittable: omittable,
							keptCount: keptCount);
					}
				}
			}
		}

		#endregion

		#region Events

		Func<Event, bool> eventPredicate = @event => @event.ExplicitImplementation is null &&
		                                             @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(eventPredicate))
		{
			bool useTypedRecordsForEvent = useTypedRecords && IsTypedRecordingEligibleEvent(@event);
			string subMemberId = useTypedRecordsForEvent
				? memberIdPrefix + memberIds.GetEventSubscribeIdentifier(@event)
				: "-1";
			string unsubMemberId = useTypedRecordsForEvent
				? memberIdPrefix + memberIds.GetEventUnsubscribeIdentifier(@event)
				: "-1";
			sb.AppendXmlSummary(
				$"Verify subscriptions on the {@event.Name} event {@event.XmlDocReference}.");
			sb.Append(
					"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
				.AppendLine();
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationEventResult<").Append(verifyName).Append("> ")
				.Append(verifyName).Append('.').Append(@event.Name).AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationEventResult<").Append(verifyName)
				.Append(">(this, this.").Append(mockRegistryName).Append(", ").Append(subMemberId).Append(", ")
				.Append(unsubMemberId).Append(", ").Append(@event.GetUniqueNameString()).Append(");").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMethodVerifyImplementation(StringBuilder sb, Method method, string mockRegistryName,
		string verifyName,
		bool useParameters, MemberIdTable memberIds, string memberIdPrefix, bool useTypedRecords,
		string? methodNameOverride = null, bool[]? valueFlags = null, bool perElementParams = false,
		bool forwardsLoneNull = false, bool emptyParams = false, int omittable = 0, int? keptCount = null)
#pragma warning restore S107
	{
		// Mirror the AppendMethodVerifyDefinition short-circuit for ref-struct signatures.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()) || method.ReturnType.NeedsRefStructPipeline())
		{
			return;
		}

		bool useTypedRecordsForMethod = useTypedRecords && IsTypedRecordingEligibleMethod(method);
		string methodMemberId = useTypedRecordsForMethod
			? memberIdPrefix + memberIds.GetMethodIdentifier(method)
			: "-1";
		string methodName = methodNameOverride ?? method.Name;
		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<");
		sb.Append(verifyName).Append('>');
		if (valueFlags?.All(x => x) == true || (method.Parameters.Count == 0 && !useParameters))
		{
			sb.Append(".IgnoreParameters");
		}

		sb.Append(' ').Append(verifyName).Append('.').Append(methodName).Append('(');
		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters").Append(forwardsLoneNull ? "? " : " ").Append("parameters");
		}
		else
		{
			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if ((emptyParams && parameter.IsParams) || i >= (keptCount ?? method.Parameters.Count))
				{
					break;
				}

				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(PerElementArraySuffix(method))
						.Append(parameter.Name);
				}
				else if (isValueParam)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						sb.Append("params ");
					}

					sb.Append(parameter.ToNullableType()).Append(' ').Append(parameter.Name);
				}
				else
				{
					sb.AppendVerifyParameter(parameter);
					if (parameter.CanUseNullableParameterOverload())
					{
						sb.Append('?');
					}

					sb.Append(' ').Append(parameter.Name);
				}

				i++;
			}
		}

		sb.Append(')');
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", inheritsConstraints: true, isExplicitInterfaceImpl: true);
			}
		}

		sb.AppendLine();
		if (keptCount is not null)
		{
			sb.Append("\t\t\t=> ((").Append(verifyName).Append(")this).").Append(methodName).Append('(')
				.Append(OmittedMethodForwardingArguments(method, keptCount.Value, valueFlags)).Append(");").AppendLine();
			return;
		}

		bool noRefStruct = !method.Parameters.Any(p
			=> p.RefKind == RefKind.Out || p.RefKind == RefKind.Ref ||
			   p.RefKind == RefKind.RefReadOnlyParameter);
		bool baseEligible = !useParameters
		                    && method.Parameters.Count is > 0 and <= 4
		                    && (method.GenericParameters is null || method.GenericParameters.Value.Count == 0)
		                    && noRefStruct;

		bool canUseLiteralVerify = baseEligible &&
		                           valueFlags is { Length: > 0 and <= 4, } &&
		                           valueFlags.All(x => x) &&
		                           !HasParamsValueParameter(method, valueFlags);
		// The per-element params overload passes a `params IParameter<TElement>[]` argument, which neither the
		// literal nor the typed fast path can render (both assume a whole-array IParameter<TElement[]>). Route it
		// through the slow predicate path, where the composite matcher is built explicitly.
		bool canUseTypedVerify = !perElementParams
		                         && useTypedRecordsForMethod
		                         && !useParameters
		                         && method.Parameters.Count <= 4
		                         && (method.GenericParameters is null || method.GenericParameters.Value.Count == 0)
		                         && !HasParamsValueParameter(method, valueFlags)
		                         && noRefStruct;

		if (canUseLiteralVerify)
		{
			// Bare-value verify path: every parameter is a literal value, so we can route through the
			// MockRegistry.VerifyMethod overload that takes the values directly (no IParameterMatch<T>
			// allocation) and uses Method{N}LiteralCountSource for the allocation-free fast path. The
			// memberId may be -1 here when typed recording is off, which the runtime overload handles by
			// dispatching to the slow predicate-based VerifyMethod.
			sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName);
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").Append(parameter.ToTypeOrWrapper());
			}

			sb.Append(">(this, ").Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString());
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").Append(parameter.Name);
			}

			AppendStaticExpectationLambda(sb, method);
			return;
		}

		// Without overloads that leave out optional parameters, an omitted one and a null matcher for it stand for the
		// declared default value, like in the setup, so the matcher is replaced up front and every verify path and the
		// failure message see the value.
		bool[] usesDeclaredDefault = method.Parameters
			.Select((p, i) => !useParameters && omittable == 0 && valueFlags?[i] != true &&
			                  p.HasExplicitDefaultValue && p.CanUseNullableParameterOverload())
			.ToArray();
		bool hasDeclaredDefaults = usesDeclaredDefault.Any(x => x);

		if (canUseTypedVerify)
		{
			// A literal value in a mixed overload becomes a LiteralParameterMatch, which serves both as the matcher
			// and as the failure-message argument.
			MethodParameter[] parameters = method.Parameters.AsArray();
			string literalBase = Helpers.GetUniqueIndexedLocalVariableBase("__literal", method.Parameters);
			string?[] literalNames = parameters
				.Select((_, i) => valueFlags?[i] == true ? FormattableString.Invariant($"{literalBase}{i + 1}") : null)
				.ToArray();
			bool hasBlockBody = hasDeclaredDefaults || literalNames.Any(x => x is not null);
			if (hasBlockBody)
			{
				sb.Append("\t\t{").AppendLine();
				AppendDeclaredDefaultMatchers(sb, method, usesDeclaredDefault);
				for (int i = 0; i < parameters.Length; i++)
				{
					if (literalNames[i] is not null)
					{
						string literalType = $"global::Mockolate.Internals.LiteralParameterMatch<{parameters[i].ToTypeOrWrapper()}>";
						sb.Append("\t\t\t").Append(literalType).Append(' ').Append(literalNames[i]).Append(" = new ")
							.Append(literalType).Append('(').Append(parameters[i].Name).Append(");").AppendLine();
					}
				}
			}

			sb.Append(hasBlockBody ? "\t\t\treturn this." : "\t\t\t=> this.").Append(mockRegistryName)
				.Append(".VerifyMethod<").Append(verifyName);
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").Append(parameter.ToTypeOrWrapper());
			}

			sb.Append(">(this, ").Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString());
			for (int i = 0; i < parameters.Length; i++)
			{
				MethodParameter parameter = parameters[i];
				if (literalNames[i] is not null)
				{
					sb.Append(", ").Append(literalNames[i]);
					continue;
				}

				string paramType = parameter.ToTypeOrWrapper();
				if (usesDeclaredDefault[i])
				{
					sb.Append(", global::Mockolate.CovariantParameterAdapter<").Append(paramType).Append(">.Wrap(")
						.Append(parameter.Name).Append(')');
					continue;
				}

				sb.Append(", ").Append(parameter.Name).Append(" is null ? ")
					.Append("(global::Mockolate.Parameters.IParameterMatch<").Append(paramType)
					.Append(">)global::Mockolate.It.Is<").Append(paramType).Append(">(default!) : ")
					.Append("global::Mockolate.CovariantParameterAdapter<").Append(paramType).Append(">.Wrap(").Append(parameter.Name)
					.Append(')');
			}

			if (method.Parameters.Count == 0)
			{
				sb.Append(", () => $\"").Append(method.Name).Append("()\");").AppendLine();
				return;
			}

			for (int i = 0; i < parameters.Length; i++)
			{
				sb.Append(", ").Append(literalNames[i] ?? parameters[i].Name);
			}

			AppendStaticExpectationLambda(sb, method);
			if (hasBlockBody)
			{
				sb.Append("\t\t}").AppendLine();
			}

			return;
		}

		MethodParameter[] slowParameters = method.Parameters.AsArray();
		string matchBase = Helpers.GetUniqueIndexedLocalVariableBase("__match", method.Parameters);
		string?[] matchNames = slowParameters
			.Select((p, i) => !useParameters && valueFlags?[i] == true &&
			                  (MayHoldMatcher(p) || (!emptyParams && MayHoldParamsMatchers(p)))
				? FormattableString.Invariant($"{matchBase}{i + 1}")
				: null)
			.ToArray();
		bool usesBlockBody = hasDeclaredDefaults || emptyParams || perElementParams || matchNames.Any(x => x is not null);
		if (usesBlockBody)
		{
			sb.Append("\t\t{").AppendLine();
			AppendDeclaredDefaultMatchers(sb, method, usesDeclaredDefault);
			for (int k = 0; k < slowParameters.Length; k++)
			{
				if (matchNames[k] is not null && slowParameters[k].IsParams)
				{
					sb.Append("\t\t\tglobal::Mockolate.Parameters.IParameterMatch<").Append(slowParameters[k].Type.Fullname)
						.Append(">? ").Append(matchNames[k]).Append(" = ");
					AppendParamsMatcher(sb, slowParameters[k]);
					sb.Append(';').AppendLine();
				}
				else if (matchNames[k] is not null)
				{
					sb.Append("\t\t\tglobal::Mockolate.Parameters.IParameterMatch<").Append(slowParameters[k].ToTypeOrWrapper())
						.Append(">? ").Append(matchNames[k]).Append(" = global::Mockolate.Internals.Matchers.AsMatcher<")
						.Append(slowParameters[k].ToTypeOrWrapper()).Append(">(").Append(slowParameters[k].Name).Append(");")
						.AppendLine();
				}
			}
		}

		if (emptyParams)
		{
			AppendEmptyParamsLocal(sb, method);
		}

		string elementMatch = Helpers.GetUniqueLocalVariableName("__elementMatch", method.Parameters);
		if (perElementParams)
		{
			MethodParameter paramsParameter = method.Parameters.AsArray()[method.Parameters.Count - 1];
			string matchType =
				$"global::Mockolate.Internals.ParamsArrayParameterMatch<{paramsParameter.Type.ElementType!.Fullname}>";
			sb.Append("\t\t\t").Append(matchType).Append(' ').Append(elementMatch).Append(" = new ").Append(matchType)
				.Append('(').Append(paramsParameter.Name).Append(");").AppendLine();
		}

		sb.Append(usesBlockBody ? "\t\t\treturn " : "\t\t\t=> ");
		if (useParameters && forwardsLoneNull)
		{
			sb.Append("parameters is null").AppendLine();
			sb.Append("\t\t\t\t? ((").Append(verifyName).Append(")this).").Append(methodName).Append('(')
				.Append(LoneNullArguments(method)).Append(')').AppendLine();
			sb.Append("\t\t\t\t: ");
		}

		sb.Append("this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName)
			.Append(", global::Mockolate.Interactions.MethodInvocation");
		if (method.Parameters.Count > 0)
		{
			sb.Append('<').Append(string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper()))).Append('>');
		}

		sb.Append(">(this, ").Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString());
		string invocation = Helpers.GetUniqueLocalVariableName("__i", method.Parameters);
		if (useParameters)
		{
			sb.Append(", ").Append(invocation).Append(" => ");
			AppendParametersSwitch(sb, method, "parameters", invocation);
		}
		else if (method.Parameters.Count == 0)
		{
			sb.Append(", ").Append(invocation).Append(" => true");
		}
		else
		{
			sb.Append(", ").Append(invocation).Append(" => ");
			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if (i > 0)
				{
					sb.Append(" && ");
				}

				sb.AppendLine().Append("\t\t\t\t");

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append('(').Append(elementMatch).Append(".Matches(").Append(invocation).Append(".Parameter")
						.Append(i + 1).Append("))");
				}
				else if (matchNames[i] is not null && parameter.IsParams)
				{
					sb.Append('(').Append(matchNames[i]).Append("?.Matches(").Append(invocation).Append(".Parameter")
						.Append(i + 1).Append(") ?? global::Mockolate.CovariantParameterAdapter<").Append(parameter.Type.Fullname)
						.Append(">.Wrap(global::Mockolate.It.SequenceEquals<").Append(parameter.Type.ElementType!.Fullname)
						.Append(">(").Append(parameter.Name).Append(")).Matches(").Append(invocation).Append(".Parameter")
						.Append(i + 1).Append("))");
				}
				else if (matchNames[i] is not null)
				{
					// A plain value keeps the allocation-free equality comparison.
					sb.Append('(').Append(matchNames[i]).Append("?.Matches(").Append(invocation).Append(".Parameter")
						.Append(i + 1).Append(") ?? global::System.Collections.Generic.EqualityComparer<")
						.Append(parameter.ToTypeOrWrapper()).Append(">.Default.Equals(").Append(parameter.Name).Append(", ")
						.Append(invocation).Append(".Parameter").Append(i + 1).Append("))");
				}
				else if (isValueParam)
				{
					if (parameter.IsParams && parameter.Type.ElementType is not null)
					{
						sb.Append('(');
						AppendParamsValuesMatcher(sb, parameter);
						sb.Append(".Matches(").Append(invocation).Append(".Parameter").Append(i + 1).Append("))");
					}
					else
					{
						sb.Append(FormattableString.Invariant($"(global::System.Collections.Generic.EqualityComparer<{parameter.ToTypeOrWrapper()}>.Default.Equals({parameter.Name}, {invocation}.Parameter{i + 1}))"));
					}
				}
				else if (usesDeclaredDefault[i])
				{
					sb.Append("(global::Mockolate.CovariantParameterAdapter<").Append(parameter.ToTypeOrWrapper())
						.Append(">.Wrap(").Append(parameter.Name).Append(").Matches(").Append(invocation)
						.Append(".Parameter").Append(i + 1).Append("))");
				}
				else
				{
					AppendSlowVerifyMatcherCondition(sb, parameter, FormattableString.Invariant($"{invocation}.Parameter{i + 1}"));
				}

				i++;
			}
		}

		sb.Append(", () => $\"").Append(method.Name).Append('(')
			.Append(useParameters
				? "{parameters}"
				: string.Join(", ", method.Parameters.Select(ParameterPlaceholder)))
			.Append(")\");").AppendLine();
		if (usesBlockBody)
		{
			sb.Append("\t\t}").AppendLine();
		}

		string ParameterPlaceholder(MethodParameter parameter)
		{
			if (parameter.IsParams && perElementParams)
			{
				return $"{{{elementMatch}}}";
			}

			if (parameter.IsParams && emptyParams)
			{
				return "[]";
			}

			if (parameter.IsParams && matchNames[matchNames.Length - 1] is { } paramsMatch)
			{
				return $"{{({paramsMatch} is null ? global::Mockolate.Internals.ValueFormatter.FormatParams({parameter.Name}) : global::Mockolate.Internals.ValueFormatter.Format((object?){paramsMatch}))}}";
			}

			if (parameter.IsParams && HasParamsValueParameter(method, valueFlags))
			{
				return $"{{(global::Mockolate.Internals.ValueFormatter.FormatParams({parameter.Name}))}}";
			}

			return ExpectationPlaceholder(parameter);
		}
	}

	private static void AppendParametersSwitch(StringBuilder sb, Method method, string parametersName,
		string invocation)
	{
		sb.Append(parametersName).Append(" switch").AppendLine();
		sb.Append("\t\t\t\t{").AppendLine();
		sb.Append("\t\t\t\t\tglobal::Mockolate.Parameters.IParametersMatch m => m.Matches([")
			.Append(string.Join(", ", Enumerable.Range(1, method.Parameters.Count).Select(i => FormattableString.Invariant($"{invocation}.Parameter{i}"))))
			.Append("]),").AppendLine();
		sb.Append("\t\t\t\t\tglobal::Mockolate.Parameters.INamedParametersMatch m => m.Matches([")
			.Append(string.Join(", ", method.Parameters.Select((p, i) => FormattableString.Invariant($"(\"{p.Name}\", {invocation}.Parameter{i + 1})"))))
			.Append("]),").AppendLine();
		sb.Append("\t\t\t\t\t_ => true").AppendLine();
		sb.Append("\t\t\t\t}");
	}

	private static void AppendDeclaredDefaultMatchers(StringBuilder sb, Method method, bool[] usesDeclaredDefault)
	{
		MethodParameter[] parameters = method.Parameters.AsArray();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (usesDeclaredDefault[i])
			{
				sb.Append("\t\t\t").Append(parameters[i].Name)
					.Append(" ??= new global::Mockolate.Internals.LiteralParameterMatch<")
					.Append(parameters[i].ToNullableType()).Append(">(").Append(parameters[i].ExplicitDefaultValue)
					.Append(");").AppendLine();
			}
		}
	}

	private static void AppendStaticExpectationLambda(StringBuilder sb, Method method)
	{
		string parameters = string.Join(", ", method.Parameters.Select(p => p.Name));
		sb.Append(", static (").Append(parameters).Append(") => $\"").Append(method.Name).Append('(')
			.Append(string.Join(", ", method.Parameters.Select(ExpectationPlaceholder))).Append(")\");").AppendLine();
	}

	private static string ExpectationPlaceholder(MethodParameter parameter)
		=> $"{{(global::Mockolate.Internals.ValueFormatter.Format((object?){parameter.Name}))}}";

	/// <summary>
	///     The slow verify path's condition for one classic matcher argument, shared between the classic and the
	///     union-mode emission. out/ref verify parameters don't inherit from <c>IParameter&lt;T&gt;</c>, so they keep
	///     the direct <c>IParameterMatch&lt;T&gt;</c> check instead of the covariance adapter.
	/// </summary>
	private static void AppendSlowVerifyMatcherCondition(StringBuilder sb, MethodParameter parameter,
		string invocationValue)
	{
		string type = parameter.ToTypeOrWrapper();
		if (parameter.RefKind is RefKind.Out or RefKind.Ref or RefKind.RefReadOnlyParameter)
		{
			sb.Append('(').Append(parameter.Name).Append(" is global::Mockolate.Parameters.IParameterMatch<").Append(type).Append("> ")
				.Append(parameter.Name).Append("Match ? ").Append(parameter.Name).Append("Match.Matches(").Append(invocationValue)
				.Append(") : global::System.Collections.Generic.EqualityComparer<").Append(type).Append(">.Default.Equals(")
				.Append(invocationValue).Append(", default(").Append(type).Append(")))");
		}
		else
		{
			sb.Append('(').Append(parameter.Name).Append(" is not null ? global::Mockolate.CovariantParameterAdapter<").Append(type)
				.Append(">.Wrap(").Append(parameter.Name).Append(").Matches(").Append(invocationValue)
				.Append(") : global::System.Collections.Generic.EqualityComparer<").Append(type).Append(">.Default.Equals(")
				.Append(invocationValue).Append(", default(").Append(type).Append(")))");
		}
	}

	#endregion Verify Helpers

	private static void AppendCreateInteractions(StringBuilder sb, string indent)
	{
		sb.Append(indent).Append("/// <summary>").AppendLine();
		sb.Append(indent).Append("///     Creates a <see cref=\"global::Mockolate.Internals.MockInteractions\" /> sized to ").Append("<see cref=\"MemberCount\" /> for use as the mock's interaction store.").AppendLine();
		sb.Append(indent).Append("/// </summary>").AppendLine();
		sb.Append(indent).Append("internal static global::Mockolate.Internals.MockInteractions CreateInteractions(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append(indent).Append("\t=> new global::Mockolate.Internals.MockInteractions(MemberCount, behavior.SkipInteractionRecording);").AppendLine();
	}

	/// <summary>
	///     Properties are recorded typed when they are not static. Static properties record through
	///     <c>RegisterInteraction</c>.
	/// </summary>
	private static bool IsTypedRecordingEligibleProperty(Property property)
		=> !property.IsStatic;

	/// <summary>
	///     Indexers with up to four key parameters and a non-ref-struct signature are recorded typed.
	/// </summary>
	private static bool IsTypedRecordingEligibleIndexer(Property indexer)
	{
		if (indexer.IsStatic ||
		    indexer.IndexerParameters is null ||
		    indexer.IndexerParameters.Value.Count == 0 ||
		    indexer.IndexerParameters.Value.Count > 4 ||
		    indexer.Type.NeedsRefStructPipeline())
		{
			return false;
		}

		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (parameter.NeedsRefStructPipeline())
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	///     Events are recorded typed when they are not static, like properties.
	/// </summary>
	private static bool IsTypedRecordingEligibleEvent(Event @event)
		=> !@event.IsStatic;

	private static bool IsTuple(Type type)
		=> type.TupleTypes is not null || type.SpecialGenericType == SpecialGenericType.Tuple;

	/// <summary>
	///     A property stores its first default value, so the auto-mock needs no cache of its own.
	/// </summary>
	private static Func<Type, (string Prefix, string Suffix)?>? PropertyAutoMock(AutoMockingOptions? autoMocking,
		string behavior)
		=> autoMocking is null
			? null
			: type => autoMocking.CanReturnAutoMock(type)
				? ($"global::Mockolate.Internals.AutoMocks.Or<{type.Fullname}>({behavior}, ", ")")
				: null;

	/// <summary>
	///     A method keeps its auto-mock in the registry, keyed by the member id and, for the elements of a tuple, by the
	///     index of the auto-mock within the return value, so that each element has its own mock.
	/// </summary>
	private static Func<Type, (string Prefix, string Suffix)?>? MethodAutoMock(AutoMockingOptions? autoMocking,
		string mockRegistry, string memberIdRef)
	{
		if (autoMocking is null)
		{
			return null;
		}

		int element = 0;
		return type =>
		{
			if (!autoMocking.CanReturnAutoMock(type))
			{
				return null;
			}

			string elementArgument = element == 0 ? "" : FormattableString.Invariant($"{element}, ");
			element++;
			return ($"{mockRegistry}.AutoMockOr<{type.Fullname}>({memberIdRef}, {elementArgument}", ")");
		};
	}

	/// <summary>
	///     Lets the mock create the auto-mocks of this assembly, so that their <c>Mock</c> property works here, and
	///     without the build property removes the factory that a behavior of another assembly may carry.
	/// </summary>
	private static void AppendUseAutoMockFactory(StringBuilder sb, string indent, string behavior,
		AutoMockingOptions? autoMocking)
	{
		sb.Append(indent).Append(behavior).Append(" = global::Mockolate.Internals.AutoMocks.UseFactory(")
			.Append(behavior).Append(autoMocking is not null ? ", global::Mockolate.Mock.CreateAutoMock);" : ", null);").AppendLine();
	}

	/// <summary>
	///     Methods with non-generic, non-ref-struct signatures are recorded typed; everything else (open
	///     generics, ref-struct params) records through <c>RegisterInteraction</c>. A non-span ref-struct
	///     return is excluded because its body never records: it is a <c>NotSupportedException</c> stub or a
	///     passthrough to base.
	/// </summary>
	private static bool IsTypedRecordingEligibleMethod(Method method)
	{
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			return false;
		}

		if (method.ReturnType.NeedsRefStructPipeline())
		{
			return false;
		}

		foreach (MethodParameter parameter in method.Parameters)
		{
			if (parameter.NeedsRefStructPipeline())
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	///     A <c>T?</c> return where <c>T</c> is one of the method's generic parameters and is
	///     constrained to a reference type (or any other non-value-type constraint such as
	///     <see langword="class" />, <c>class?</c>, an interface, or <c>notnull</c>) cannot be expressed in
	///     the explicit setup-interface implementation: CS0460 forbids restating the inherited
	///     constraint, and <c>where T : default</c> (CS8822) conflicts with those constraints.
	///     Without a constraint clause the compiler resolves the bare <c>T?</c> as
	///     <c>Nullable&lt;T&gt;</c> and reports CS0453/CS9334/CS0738/CS0266.
	///     The fix is to drop the trailing <c>?</c> from the setup-side return type
	///     (<c>IReturnMethodSetup&lt;T&gt;</c> instead of <c>IReturnMethodSetup&lt;T?&gt;</c>) and from
	///     the matching <c>ReturnMethodSetup&lt;T&gt;</c> construction. NRT annotations are erased at
	///     runtime, so the underlying setup object is identical and the fluent API still composes.
	///     The user-facing mock body keeps <c>T?</c> because the constraint is visible there.
	/// </summary>
	private static bool ShouldStripNullableGenericReturnAnnotation(Method method)
	{
		if (method.GenericParameters is null || method.GenericParameters.Value.Count == 0)
		{
			return false;
		}

		string fullname = method.ReturnType.Fullname;
		if (fullname.Length < 2 || fullname[fullname.Length - 1] != '?')
		{
			return false;
		}

		string raw = fullname.Substring(0, fullname.Length - 1);
		foreach (GenericParameter gp in method.GenericParameters.Value)
		{
			if (string.Equals(gp.Name, raw, StringComparison.Ordinal))
			{
				return !gp.IsStruct && !gp.IsUnmanaged;
			}
		}

		return false;
	}

	/// <summary>
	///     Emits the method's return type as it should appear inside the setup-side surface
	///     (the <c>IReturnMethodSetup&lt;...&gt;</c> wrapper on the setup interface, the explicit
	///     impl, and the <c>new ReturnMethodSetup&lt;...&gt;</c> construction). Strips a trailing
	///     <c>?</c> when <see cref="ShouldStripNullableGenericReturnAnnotation" /> applies.
	/// </summary>
	private static void AppendSetupReturnType(StringBuilder sb, Method method)
	{
		if (ShouldStripNullableGenericReturnAnnotation(method))
		{
			string fullname = method.ReturnType.Fullname;
			sb.Append(fullname, 0, fullname.Length - 1);
			return;
		}

		sb.AppendTypeOrWrapper(method.ReturnType);
	}

	/// <summary>
	///     Declares the arguments that <c>CreateMock()</c> passes to the <paramref name="constructor" /> with dependencies
	///     and returns their names: a new mock with the same behavior, an empty collection for <see langword="params" />,
	///     the declared default value, <see langword="null" /> for a nullable parameter, or else a value from the default
	///     values of the behavior.
	/// </summary>
	private static string AppendConstructorDependencies(StringBuilder sb, string indent, Method constructor,
		EquatableArray<ConstructorDependency> dependencies, string classDisplay)
	{
		List<string> names = [];
		ConstructorDependency[] dependencyArray = dependencies.AsArray();
		int index = 0;
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			ConstructorDependency dependency = dependencyArray[index++];
			string name = FormattableString.Invariant($"d{index}");
			names.Add(name);
			sb.Append(indent).Append(parameter.Type.Fullname).Append(' ').Append(name).Append(" = ");
			if (dependency.MockTypeFullName is { } mockType)
			{
				sb.Append(mockType).Append(".CreateMock(mockRegistry.Behavior);").AppendLine();
			}
			else if (parameter.IsParams)
			{
				sb.Append("[];").AppendLine();
			}
			else if (parameter.HasExplicitDefaultValue)
			{
				sb.Append(parameter.ExplicitDefaultValue).Append(';').AppendLine();
			}
			else if (dependency.IsNullable)
			{
				sb.Append("default;").AppendLine();
			}
			else
			{
				sb.Append("global::Mockolate.Internals.ConstructorDefaultValue.Get(mockRegistry.Behavior, static mockolateBehavior => mockolateBehavior.DefaultValue.Generate(default(").Append(parameter.Type.Fullname).Append(")!));").AppendLine();
				if (!parameter.Type.IsValueType)
				{
					sb.Append(indent).Append("if (").Append(name).Append(" is null)").AppendLine();
					sb.Append(indent).Append('{').AppendLine();
					sb.Append(indent).Append("\tthrow new global::Mockolate.Exceptions.MockException(\"There is no argument for the constructor parameter '")
						.Append(parameter.Name.TrimStart('@')).Append("' of ").Append(classDisplay)
						.Append(". Pass the constructor parameters, or register a default value for its type with WithDefaultValueFor.\");").AppendLine();
					sb.Append(indent).Append('}').AppendLine();
				}
			}
		}

		return string.Join(", ", names);
	}

	private static string AppendArgumentsWithoutConstructorParameters(StringBuilder sb, string indent,
		EquatableArray<Method> constructors, int constructorIndex)
	{
		StringBuilder arguments = new();
		int parameterIndex = 0;
		foreach (MethodParameter parameter in constructors.AsArray()[constructorIndex].Parameters)
		{
			string name = FormattableString.Invariant($"c{constructorIndex + 1}p{++parameterIndex}");
			sb.Append(indent).Append(parameter.Type.Fullname).Append(' ').Append(name).Append(" = ")
				.Append(parameter.IsParams ? "[]" : parameter.ExplicitDefaultValue).Append(';').AppendLine();
			arguments.Append(", ").Append(name);
		}

		return arguments.ToString();
	}

	private static void AppendTypedCreateMockOverloads(StringBuilder sb, Class @class,
		EquatableArray<Method> constructors, string setupType, string classReference, string createMockReturns)
	{
		// Seeded signatures track the hand-written CreateMock overloads so typed overloads that
		// would collide with them are skipped. The key order mirrors the emitted C# signature:
		// "mockBehavior? | setup? | ctor-param-types...".
		HashSet<string> emittedSignatures = new(StringComparer.Ordinal)
		{
			string.Empty,
			"global::Mockolate.MockBehavior",
			$"global::System.Action<{setupType}>",
			$"global::Mockolate.MockBehavior|global::System.Action<{setupType}>",
			"object?[]",
			"global::Mockolate.MockBehavior|object?[]",
			$"global::System.Action<{setupType}>|object?[]",
			$"global::Mockolate.MockBehavior|global::System.Action<{setupType}>|object?[]",
		};

		foreach (Method constructor in constructors)
		{
			if (constructor.Parameters.Count == 0)
			{
				continue;
			}

			// A ref struct cannot be passed in the object array of constructor parameters.
			if (constructor.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams || p.Type.IsRefStruct))
			{
				continue;
			}

			string mockBehaviorName = CreateUniqueParameterName(constructor.Parameters, "mockBehavior");
			string setupName = CreateUniqueParameterName(constructor.Parameters, "setup");
			string baseSig = string.Join("|",
				constructor.Parameters.Select(p => p.Type.Fullname));

			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, classReference, createMockReturns,
includeMockBehavior: false, includeSetup: false, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, classReference, createMockReturns,
includeMockBehavior: true, includeSetup: false, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, classReference, createMockReturns,
includeMockBehavior: false, includeSetup: true, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, classReference, createMockReturns,
includeMockBehavior: true, includeSetup: true, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
		}
	}

	/// <summary>
	///     Builds an XML-doc cref string and a matching short display text for the given
	///     <paramref name="constructor" /> on <paramref name="class" />. The cref has the form
	///     <c>{class-cref}({fully-qualified-param-types})</c>; the display has the form
	///     <c>{simple-name}({short-param-types})</c>, intended as the inner text of
	///     <c>&lt;see cref="..."&gt;...&lt;/see&gt;</c> so the rendered prose reads
	///     <c>the MyClass(int) constructor</c>.
	///     Returns <see langword="null" /> when no valid cref can be produced.
	/// </summary>
	/// <remarks>
	///     Generic classes are skipped because the cref type-parameter-list syntax (e.g. <c>{T}</c>)
	///     expects identifier tokens, not the concrete type arguments that closed generics carry —
	///     emitting <c>MyClass{int}(int)</c> would surface CS1584/CS1658 on the consumer side.
	/// </remarks>
	private static (string Cref, string Display)? BuildConstructorCref(Class @class, Method constructor)
	{
		string fullName = @class.ClassFullName;

		if (fullName.IndexOf('<') >= 0)
		{
			return null;
		}

		int lastDot = fullName.LastIndexOf('.');
		string simpleName = lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;

		StringBuilder display = new();
		display.Append(simpleName).Append('(');
		bool first = true;
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			if (!first)
			{
				display.Append(", ");
			}

			first = false;
			// Inner text of <see> is XML content, so escape '<'/'>' as entities
			// (unlike cref attributes, which use the '{...}' shorthand).
			display.Append(parameter.Type.DisplayName.Replace("<", "&lt;").Replace(">", "&gt;"));
		}

		display.Append(')');
		return (constructor.Cref, display.ToString());
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void TryEmitTypedCreateMockOverload(StringBuilder sb, Class @class, Method constructor,
		string setupType, string classReference, string createMockReturns,
		bool includeMockBehavior, bool includeSetup, string mockBehaviorName, string setupName, string baseSig,
		HashSet<string> emittedSignatures)
	{
		// Build the signature key in the same order as the emitted method signature
		// (mockBehavior, setup, then ctor parameters) so it correctly detects collisions against
		// other typed overloads and against the hand-written seeded overloads.
		string sig = baseSig;
		if (includeSetup)
		{
			sig = $"global::System.Action<{setupType}>|{sig}";
		}

		if (includeMockBehavior)
		{
			sig = $"global::Mockolate.MockBehavior|{sig}";
		}

		if (!emittedSignatures.Add(sig))
		{
			return;
		}

		(string Cref, string Display)? constructorCref = BuildConstructorCref(@class, constructor);
		string ctorPhrase = constructorCref is null
			? "the base-class constructor"
			: $"the <see cref=\"{constructorCref.Value.Cref}\">{constructorCref.Value.Display}</see> constructor";

		if (includeMockBehavior && includeSetup)
		{
			sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given <paramref name=\"{mockBehaviorName}\" />, applying the given <paramref name=\"{setupName}\" /> immediately, using the given constructor parameters to invoke {ctorPhrase}.");
			sb.AppendXmlRemarks($"The provided <paramref name=\"{setupName}\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		}
		else if (includeMockBehavior)
		{
			sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given <paramref name=\"{mockBehaviorName}\" /> and the given constructor parameters to invoke {ctorPhrase}.");
		}
		else if (includeSetup)
		{
			sb.AppendXmlSummary($"Creates a new mock of {classReference} applying the given <paramref name=\"{setupName}\" /> immediately, using the given constructor parameters to invoke {ctorPhrase}.");
			sb.AppendXmlRemarks($"The provided <paramref name=\"{setupName}\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		}
		else
		{
			sb.AppendXmlSummary($"Creates a new mock of {classReference} using the given constructor parameters to invoke {ctorPhrase}.");
		}

		if (includeMockBehavior)
		{
			sb.AppendXmlParam(mockBehaviorName, "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
		}

		if (includeSetup)
		{
			sb.AppendXmlParam(setupName, "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
		}

		foreach (MethodParameter parameter in constructor.Parameters)
		{
			sb.AppendXmlParam(parameter.Name.TrimStart('@'), "Value forwarded to the base-class constructor.");
		}

		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(");
		bool needsLeadingComma = false;
		if (includeMockBehavior)
		{
			sb.Append("global::Mockolate.MockBehavior ").Append(mockBehaviorName);
			needsLeadingComma = true;
		}

		if (includeSetup)
		{
			if (needsLeadingComma)
			{
				sb.Append(", ");
			}

			sb.Append("global::System.Action<").Append(setupType).Append("> ").Append(setupName);
			needsLeadingComma = true;
		}

		foreach (MethodParameter parameter in constructor.Parameters)
		{
			if (needsLeadingComma)
			{
				sb.Append(", ");
			}

			needsLeadingComma = true;
			sb.Append(parameter.Type.Fullname).Append(' ').Append(parameter.Name);
			if (parameter.HasExplicitDefaultValue)
			{
				sb.Append(" = ").Append(parameter.ExplicitDefaultValue);
			}
		}

		sb.Append(')').AppendLine();
		sb.Append("\t\t\t=> CreateMock(").Append(includeMockBehavior ? mockBehaviorName : "null").Append(", ")
			.Append(includeSetup ? setupName : "null").Append(", new object?[] { ");
		int argIndex = 0;
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			if (argIndex++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.Name);
		}

		sb.Append(" });").AppendLine();
		sb.AppendLine();
	}
#pragma warning restore S107 // Methods should not have too many parameters
}
