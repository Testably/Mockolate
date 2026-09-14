using System.Text;
using Microsoft.CodeAnalysis;
using Mockolate.SourceGenerators.Entities;
using GenericParameter = Mockolate.SourceGenerators.Entities.GenericParameter;
using Type = Mockolate.SourceGenerators.Entities.Type;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	private const int MaxExplicitParameters = 4;

	public static string MockClass(
		string name,
		Class @class,
		bool hasOverloadResolutionPriority = false,
		(string Name, Class Class)[]? hiddenBaseInterfaces = null,
		bool useUnionOverloads = false)
	{
		hiddenBaseInterfaces ??= [];
		EquatableArray<Method>? constructors = (@class as MockClass)?.Constructors;
		bool hasParameterizedConstructor = !@class.IsInterface &&
		                                   constructors?.Any(m => m.Parameters.Count > 0) == true;
		string escapedClassName = @class.ClassFullName.EscapeForXmlDoc();
		bool hasEvents = @class.AllEvents().Any(x => !x.IsStatic);
		bool hasStaticEvents = @class.IsInterface && @class.AllEvents().Any(@event => @event.IsStatic);
		bool hasStaticMembers = @class.IsInterface && (@class.AllMethods().Any(method => method.IsStatic) || @class.AllProperties().Any(property => property.IsStatic));
		bool hasProtectedEvents = !@class.IsInterface && @class.AllEvents().Any(@event => @event.IsProtected);
		bool hasProtectedMembers = !@class.IsInterface && (@class.AllMethods().Any(method => method.IsProtected) || @class.AllProperties().Any(property => property.IsProtected));
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
		
		StringBuilder sb = InitializeBuilder();
		sb.Append("#nullable enable annotations").AppendLine();
		sb.Append("namespace Mockolate;").AppendLine();
		sb.AppendLine();

		#region Mock

		sb.Append("internal static partial class Mock").AppendLine();
		sb.Append("{").AppendLine();
		
		#region MockForXXX
		
		sb.AppendXmlSummary($"A mock implementation for <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\t[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").AppendLine();
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
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
			if (hbHasStaticMembers || hbHasStaticEvents)
			{
				sb.Append(", IMockStaticVerifyFor").Append(hiddenBase.Name);
			}

			sb.Append(",").AppendLine();
		}

		sb.Append("\t\tglobal::Mockolate.IMock").AppendLine();
		sb.Append("\t{").AppendLine();

		memberIds.Emit(sb, "\t\t");
		sb.AppendLine();

		AppendCreateFastInteractions(sb, "\t\t");
		sb.AppendLine();

		bool hasMockRegistryProvider = constructors?.Count > 0 || (@class.IsInterface && hasStaticMembers);
		AppendCreateRegistryFromBehavior(sb, "\t\t", hasMockRegistryProvider);
		sb.AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
		sb.Append("\t\tglobal::Mockolate.MockRegistry global::Mockolate.IMock.MockRegistry => this.").Append(mockRegistryName).Append(';').AppendLine();
		if (constructors?.Count > 0)
		{
			sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tget => field ?? MockRegistryProvider.Value;").AppendLine();
			sb.Append("\t\t\tset;").AppendLine();
			sb.Append("\t\t}").AppendLine();
			sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append("\t\tinternal static readonly global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry> MockRegistryProvider = new global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry>();").AppendLine();
		}
		else
		{
			sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
			if (hasStaticMembers)
			{
				sb.Append("\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
				sb.Append("\t\tinternal static readonly global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry> MockRegistryProvider = new global::System.Threading.AsyncLocal<global::Mockolate.MockRegistry>();").AppendLine();
			}
		}

		sb.AppendLine();
		
		AppendCachedFieldDeclarations(sb, "\t\t", @class, memberIds, memberIdPrefix, mockRegistryName);
		
		sb.AppendLine();
		
		ImplementMockForInterface(sb, mockRegistryName, name, hasEvents, hasProtectedMembers, hasProtectedEvents, hasStaticMembers, hasStaticEvents);

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			ImplementMockForInterface(sb, mockRegistryName, hiddenBase.Name,
				hiddenBase.Class.AllEvents().Any(x => !x.IsStatic),
				false /* interfaces have no protected members */,
				false /* interfaces have no protected events */,
				hiddenBase.Class.AllMethods().Any(x => x.IsStatic) || hiddenBase.Class.AllProperties().Any(x => x.IsStatic),
				hiddenBase.Class.AllEvents().Any(x => x.IsStatic));
		}

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tstring global::Mockolate.IMock.ToString()").AppendLine();
		sb.Append("\t\t\t=> \"").Append(@class.DisplayString).Append(" mock\";").AppendLine();
		sb.AppendLine();

		if (@class.IsInterface)
		{
			sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
			sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.MockRegistry mockRegistry)").AppendLine();
			sb.Append("\t\t{").AppendLine();
			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
			if (hasStaticMembers)
			{
				sb.Append("\t\t\tMockRegistryProvider.Value = mockRegistry;").AppendLine();
			}

			sb.Append("\t\t}").AppendLine();
			sb.AppendLine();
			
			AppendMockSubject_BehaviorConstructor(sb, name);
		}
		else if (constructors is not null)
		{
			foreach (Method constructor in constructors)
			{
				AppendMockSubject_BaseClassConstructor(sb, mockRegistryName, name, constructor, @class.HasRequiredMembers);
				AppendMockSubject_BehaviorBaseClassConstructor(sb, name, constructor, @class.HasRequiredMembers);
			}
		}

		AppendMockSubject_ImplementClass(sb, @class, mockRegistryName, null, memberIds, memberIdPrefix);
		
		sb.AppendLine();

		#region Mock.Setup

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

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockSetupFor").Append(hiddenBase.Name).AppendLine();
			sb.AppendLine();
			ImplementSetupInterface(sb, hiddenBase.Class, mockRegistryName, $"IMockSetupFor{hiddenBase.Name}", MemberType.Public, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
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
		ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockVerifyFor{name}", MemberType.Public, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();

		if (hasProtectedMembers || hasProtectedEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockProtectedVerifyFor{name}", MemberType.Protected, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockProtectedVerifyFor").Append(name).AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockStaticVerifyFor").Append(name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockStaticVerifyFor{name}", MemberType.Static, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
			sb.Append("\t\t#endregion IMockStaticVerifyFor").Append(name).AppendLine();
		}

		foreach ((string Name, Class Class) hiddenBase in hiddenBaseInterfaces)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockVerifyFor").Append(hiddenBase.Name).AppendLine();
			sb.AppendLine();
			ImplementVerifyInterface(sb, hiddenBase.Class, mockRegistryName, $"IMockVerifyFor{hiddenBase.Name}", MemberType.Public, memberIds, memberIdPrefix, false, useUnionOverloads);
			sb.Append("\t\t#endregion IMockVerifyFor").Append(hiddenBase.Name).AppendLine();
		}

		#endregion Mock.Verify

		sb.AppendLine("\t}");
		sb.AppendLine();

		#endregion MockForXXX

		#region VerifyMonitor
		
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
		
		ImplementVerifyInterface(sb, @class, mockRegistryName, $"IMockVerifyFor{name}", MemberType.Public, memberIds, memberIdPrefix, useUnionOverloads: useUnionOverloads);
		
		sb.Append("\t\t#endregion IMockVerifyFor").Append(name).AppendLine();
		sb.Append("\t}").AppendLine();
		sb.AppendLine();
		
		#endregion VerifyMonitor

		#region MockInScenarioForXXX
		
#if !DEBUG
		sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("\tprivate sealed class MockInScenarioFor").Append(name).Append(" : global::Mockolate.Mock.IMockInScenarioFor").Append(name).Append(", global::Mockolate.Mock.IMockSetupFor").Append(name);
		if (hasProtectedMembers)
		{
			sb.Append(", global::Mockolate.Mock.IMockProtectedSetupFor").Append(name);
		}

		sb.AppendLine();
		sb.Append("\t{").AppendLine();
		sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; }").AppendLine();
		sb.Append("\t\tprivate string _scenarioName;").AppendLine();
		sb.AppendLine();
		sb.Append("\t\tpublic MockInScenarioFor").Append(name).Append("(global::Mockolate.MockRegistry mockRegistry, string scenario)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(" = mockRegistry;").AppendLine();
		sb.Append("\t\t\t_scenarioName = scenario;").AppendLine();
		sb.Append("\t\t}").AppendLine();
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

		sb.Append("\t\t#region IMockSetupFor").Append(name).AppendLine();
		sb.AppendLine();
		
		ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockSetupFor{name}", MemberType.Public, memberIds, memberIdPrefix, "_scenarioName", useUnionOverloads);
		
		sb.Append("\t\t#endregion IMockSetupFor").Append(name).AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.Append("\t\t#region IMockProtectedSetupFor").Append(name).AppendLine();
			sb.AppendLine();
			
			ImplementSetupInterface(sb, @class, mockRegistryName, $"IMockProtectedSetupFor{name}", MemberType.Protected, memberIds, memberIdPrefix, "_scenarioName", useUnionOverloads);
			
			sb.Append("\t\t#endregion IMockProtectedSetupFor").Append(name).AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();
		
		#endregion MockInScenarioForXXX

		#region Mock Interfaces
		
		#region IMockForXXX

		sb.AppendXmlSummary($"The Mockolate accessor for a mock of <see cref=\"{escapedClassName}\" />, reached through <c>.Mock</c> on the mocked instance.", "\t");
		sb.AppendXmlRemarks(["Groups every operation that acts on the mock rather than on the mocked subject: setups, verifications, event raising, scenarios and monitoring.",], "\t");
		sb.Append("\tinternal interface IMockFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary($"Configures how members of the mock of <see cref=\"{escapedClassName}\" /> respond when invoked.");
		sb.AppendXmlRemarks(
		[
			"Each mocked member is available as a strongly-typed entry on this surface. Chain <c>Returns</c>, <c>ReturnsAsync</c>, <c>Throws</c>, <c>ThrowsAsync</c> or <c>Do</c> to control the response; chain <c>InitializeWith</c>/<c>Register</c> to initialize properties and indexers; chain multiple returns/throws to define a sequence; use <c>.For(n)</c>, <c>.Only(n)</c>, <c>.Forever()</c>, <c>.When(predicate)</c> to control when a callback runs.",
			"When two setups overlap, the most recently defined one wins.",
		]);
		sb.Append("\t\tIMockSetupFor").Append(name).Append(" Setup { get; }").AppendLine();
		sb.AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendXmlSummary($"Configures how <see langword=\"protected\" /> virtual members of the mock of <see cref=\"{escapedClassName}\" /> respond when invoked.");
			sb.AppendXmlRemarks(["Only members declared as <see langword=\"protected\" /> (or <see langword=\"protected\" /> <see langword=\"internal\" />) on the mocked class appear here. All setup chain operators (<c>Returns</c>, <c>Throws</c>, <c>Do</c>, sequences, <c>.For</c>/<c>.Only</c>/<c>.Forever</c>, ...) work identically to <see cref=\"Setup\" />.",]);
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" SetupProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticMembers)
		{
			sb.AppendXmlSummary($"Configures how <see langword=\"static\" /> members declared on <see cref=\"{escapedClassName}\" /> respond when invoked.");
			sb.AppendXmlRemarks(["Static members are scoped per async/execution flow while the mock is alive; invocations from other flows are not intercepted.",]);
			sb.Append("\t\tIMockStaticSetupFor").Append(name).Append(" SetupStatic { get; }").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary($"Opens a named scenario scope on the mock of <see cref=\"{escapedClassName}\" /> so that additional setups can be registered for that scenario.");
		sb.AppendXmlRemarks(["Scenarios let you define per-state behavior. Setups registered inside the returned <c>IMockInScenarioFor...</c> scope only apply while the mock's current scenario matches <paramref name=\"scenario\" />; switch scenarios with <see cref=\"TransitionTo\" />.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to enter. Any non-null string acts as a key; the mock starts in an unnamed default scenario.");
		sb.AppendXmlReturns("A scoped accessor whose <c>Setup</c> (and <c>SetupProtected</c>, where applicable) register scenario-specific setups.");
		sb.Append("\t\tIMockInScenarioFor").Append(name).Append(" InScenario(string scenario);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Opens a named scenario scope on the mock of <see cref=\"{escapedClassName}\" /> and immediately invokes <paramref name=\"setup\" /> to register scenario-specific setups.");
		sb.AppendXmlRemarks(["Equivalent to <c>InScenario(scenario)</c> followed by the setup callback, but returns the original <c>IMockFor...</c> accessor so it chains nicely at mock-creation time.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to enter.");
		sb.AppendXmlParam("setup", "Callback that receives the scenario-scoped setup surface and registers scenario-specific setups.");
		sb.AppendXmlReturns("This accessor, to allow chaining.");
		sb.Append("\t\tIMockFor").Append(name).Append(" InScenario(string scenario, global::System.Action<IMockInScenarioFor").Append(name).Append("> setup);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Switches the active scenario of the mock of <see cref=\"{escapedClassName}\" /> to <paramref name=\"scenario\" />.");
		sb.AppendXmlRemarks(["After the transition, setups registered via <see cref=\"InScenario(string)\" /> under that scenario take effect. Scenarios that have no matching setup for a given member fall back to the default (un-scoped) setups.",]);
		sb.AppendXmlParam("scenario", "Name of the scenario to transition to.");
		sb.AppendXmlReturns("This accessor, to allow chaining.");
		sb.Append("\t\tIMockFor").Append(name).Append(" TransitionTo(string scenario);").AppendLine();
		sb.AppendLine();

		if (hasEvents)
		{
			sb.AppendXmlSummary($"Triggers events declared on <see cref=\"{escapedClassName}\" /> so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["One entry per event is generated; the signature matches the event's delegate. Only handlers that are subscribed at the moment of the <c>Raise</c> call are invoked - handlers subscribed later (or already removed) are skipped.",]);
			sb.Append("\t\tIMockRaiseOn").Append(name).Append(" Raise { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasProtectedEvents)
		{
			sb.AppendXmlSummary($"Triggers <see langword=\"protected\" /> events declared on <see cref=\"{escapedClassName}\" /> so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["Same semantics as <see cref=\"Raise\" /> but for events whose accessibility prevents external subscription from outside the class. Useful when testing code that subclasses the mocked type.",]);
			sb.Append("\t\tIMockProtectedRaiseOn").Append(name).Append(" RaiseProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticEvents)
		{
			sb.AppendXmlSummary($"Triggers <see langword=\"static\" /> events declared on <see cref=\"{escapedClassName}\" /> so that currently subscribed handlers are invoked.");
			sb.AppendXmlRemarks(["Static events are scoped per async/execution flow while the mock is alive.",]);
			sb.Append("\t\tIMockStaticRaiseOn").Append(name).Append(" RaiseStatic { get; }").AppendLine();
			sb.AppendLine();
		}

		sb.AppendXmlSummary($"Asserts how often, and in which order, members of the mock of <see cref=\"{escapedClassName}\" /> were invoked.");
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
			sb.AppendXmlSummary($"Asserts how often, and in which order, <see langword=\"protected\" /> members of the mock of <see cref=\"{escapedClassName}\" /> were invoked.");
			sb.AppendXmlRemarks(["Same terminators and modifiers as <see cref=\"Verify\" /> (<c>Once()</c>, <c>Exactly(n)</c>, <c>Within(...)</c>, <c>Then(...)</c>, ...); applies to <see langword=\"protected\" /> members and events instead of public ones.",]);
			sb.Append("\t\tIMockProtectedVerifyFor").Append(name).Append(" VerifyProtected { get; }").AppendLine();
			sb.AppendLine();
		}

		if (hasStaticMembers || hasStaticEvents)
		{
			sb.AppendXmlSummary($"Asserts how often, and in which order, <see langword=\"static\" /> members declared on <see cref=\"{escapedClassName}\" /> were invoked.");
			sb.AppendXmlRemarks(["Same terminators and modifiers as <see cref=\"Verify\" />; scoped per async/execution flow in the same way as <see cref=\"SetupStatic\" />.",]);
			sb.Append("\t\tIMockStaticVerifyFor").Append(name).Append(" VerifyStatic { get; }").AppendLine();
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
		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockForXXX

		#region IMockInScenarioForXXX

		sb.AppendXmlSummary($"Scoped access to setups for a scenario on the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockInScenarioFor").Append(name).AppendLine();
		sb.Append("\t{").AppendLine();
		sb.AppendXmlSummary($"Set up the mock of <see cref=\"{escapedClassName}\" /> within the scenario scope.");
		sb.Append("\t\tIMockSetupFor").Append(name).Append(" Setup { get; }").AppendLine();
		if (hasProtectedMembers)
		{
			sb.AppendLine();
			sb.AppendXmlSummary($"Set up protected members of the mock of <see cref=\"{escapedClassName}\" /> within the scenario scope.");
			sb.Append("\t\tIMockProtectedSetupFor").Append(name).Append(" SetupProtected { get; }").AppendLine();
		}

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion IMockInScenarioForXXX

		#region IMockSetupForXXX

		sb.AppendXmlSummary($"Set up the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockSetupFor").Append(name);
		if (!hasStaticMembers && !hasStaticEvents)
		{
			sb.Append(" : global::Mockolate.Setup.IMockSetup<").Append(@class.ClassFullName).Append(">").AppendLine();
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
			sb.AppendXmlSummary($"Set up protected members for the mock of <see cref=\"{escapedClassName}\" />.", "\t");
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
			sb.AppendXmlSummary($"Set up static members for the mock of <see cref=\"{escapedClassName}\" />.", "\t");
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
			sb.AppendXmlSummary($"Raise events on the mock of <see cref=\"{escapedClassName}\" />.", "\t");
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
			sb.AppendXmlSummary($"Raise protected events on the mock of <see cref=\"{escapedClassName}\" />.", "\t");
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
			sb.AppendXmlSummary($"Raise static events on the mock of <see cref=\"{escapedClassName}\" />.", "\t");
			sb.Append("\tinternal interface IMockStaticRaiseOn").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			
			DefineRaiseInterface(sb, @class, MemberType.Static);
			
			sb.Append("\t}").AppendLine();
			sb.AppendLine();
		}

		#endregion IMockStaticRaiseOnXXX

		#region IMockVerifyForXXX

		sb.AppendXmlSummary($"Verify interactions with the mock of <see cref=\"{escapedClassName}\" />.", "\t");
		sb.Append("\tinternal interface IMockVerifyFor").Append(name);
		if (!hasStaticMembers && !hasStaticEvents)
		{
			sb.Append(" : global::Mockolate.Verify.IMockVerify<").Append(@class.ClassFullName).Append(">").AppendLine();
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
			sb.AppendXmlSummary($"Verify protected interactions with the mock of <see cref=\"{escapedClassName}\" />.",
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
			sb.AppendXmlSummary($"Verify static interactions with the mock of <see cref=\"{escapedClassName}\" />.",
				"\t");
			sb.Append("\tinternal interface IMockStaticVerifyFor").Append(name).AppendLine();
			sb.Append("\t{").AppendLine();
			DefineVerifyInterface(sb, @class, $"IMockStaticVerifyFor{name}", MemberType.Static,
				hasOverloadResolutionPriority, useUnionOverloads);
			sb.Append("\t}").AppendLine();
		}

		#endregion IMockStaticVerifyForXXX

		#endregion Mock Interfaces

		sb.Append("}").AppendLine();
		
		#endregion Mock
		
		#region MockForXXXExtensions

		sb.AppendXmlSummary($"Mock extensions for <see cref=\"{escapedClassName}\" />.", "");
#if !DEBUG
		sb.Append("[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
		sb.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
		sb.Append("internal static partial class MockExtensionsFor").Append(name).AppendLine();
		sb.Append("{").AppendLine();

		#region Mock Type extensions

		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(").Append(@class.ClassFullName).Append(" mock)").AppendLine();
		sb.Append("\t{").AppendLine();

		#region Mock Property

		string mockPropertyName = CreateUniquePropertyName(@class, "Mock");

		List<string> mockPropertyRemarks =
		[
			$"The accessor is the bridge between the strongly-typed instance of <see cref=\"{escapedClassName}\" /> returned by <c>CreateMock(...)</c> and the underlying mock registry where setups and recorded interactions live.",
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

		mockPropertyRemarks.Add("  <item><description><c>InScenario</c> / <c>TransitionTo</c> - scope setups and behavior to a named scenario and switch between scenarios.</description></item>");
		mockPropertyRemarks.Add("  <item><description><c>Monitor</c>, <c>ClearAllInteractions</c>, <c>VerifyThatAllInteractionsAreVerified</c>, <c>VerifyThatAllSetupsAreUsed</c> - manage recorded interactions.</description></item>");
		mockPropertyRemarks.Add("  <item><description><c>VerifySetup</c> - verify how often a specific setup matched.</description></item>");
		mockPropertyRemarks.Add("</list>");

		sb.AppendXmlSummary($"Gets the mock accessor for <see cref=\"{escapedClassName}\" /> - the entry point for configuring setups, verifying interactions and raising events.");
		sb.AppendXmlRemarks(mockPropertyRemarks.ToArray());
		sb.AppendXmlException("global::Mockolate.Exceptions.MockException", $"The instance is not a Mockolate-generated mock of <see cref=\"{escapedClassName}\" />.");
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

		string createMockReturns = $"A new mock instance of <see cref=\"{escapedClassName}\" />.";
		List<string> createMockRemarks =
		[
			$"The returned instance is a strongly-typed mock generated at compile time - it implements <see cref=\"{escapedClassName}\" /> and exposes the Mockolate surface through <c>.Mock</c>:",
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

		sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> with the default <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlRemarks(createMockRemarks.ToArray());
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock()").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, null, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> with the default <see cref=\"global::Mockolate.MockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::System.Action<").Append(setupType).Append("> setup)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(null, setup, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> with the given <paramref name=\"mockBehavior\" />.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(mockBehavior, null, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> with the given <paramref name=\"mockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
		sb.AppendXmlReturns(createMockReturns);
		sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior, global::System.Action<").Append(setupType).Append("> setup)").AppendLine();
		sb.Append("\t\t\t=> CreateMock(mockBehavior, setup, (object?[]?)null);").AppendLine();
		sb.AppendLine();

		if (hasParameterizedConstructor)
		{
			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given <paramref name=\"constructorParameters\" /> to invoke the base-class constructor.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Required when no parameterless constructor exists.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(null, null, constructorParameters);").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given <paramref name=\"mockBehavior\" /> and <paramref name=\"constructorParameters\" />.");
			sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup; see <see cref=\"global::Mockolate.MockBehavior\" />.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Required when no parameterless constructor exists.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::Mockolate.MockBehavior mockBehavior, object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(mockBehavior, null, constructorParameters);").AppendLine();
			sb.AppendLine();

			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> applying the given <paramref name=\"setup\" /> immediately, using the given <paramref name=\"constructorParameters\" />.");
			sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
			sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned.");
			sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor. Required when no parameterless constructor exists.");
			sb.AppendXmlReturns(createMockReturns);
			sb.Append("\t\tpublic static ").Append(@class.ClassFullName).Append(" CreateMock(global::System.Action<").Append(setupType).Append("> setup, object?[] constructorParameters)").AppendLine();
			sb.Append("\t\t\t=> CreateMock(null, setup, constructorParameters);").AppendLine();
			sb.AppendLine();

			AppendTypedCreateMockOverloads(sb, @class, constructors!.Value, setupType, escapedClassName, createMockReturns);
		}

		sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given <paramref name=\"mockBehavior\" />, applying the given <paramref name=\"setup\" /> immediately, using the given <paramref name=\"constructorParameters\" />.");
		sb.AppendXmlRemarks("The provided <paramref name=\"setup\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		sb.AppendXmlParam("mockBehavior", "Controls how the mock responds when members are invoked without a matching setup, or <see langword=\"null\" /> for <c>MockBehavior.Default</c>.");
		sb.AppendXmlParam("setup", "Callback that receives the mock's setup surface and registers initial setups before the mock is returned, or <see langword=\"null\" /> to skip.");
		sb.AppendXmlParam("constructorParameters", "Values forwarded to a matching base-class constructor, or <see langword=\"null\" /> to use the parameterless constructor.");
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
			sb.Append("\t\t\tglobal::Mockolate.MockRegistry mockRegistry = new global::Mockolate.MockRegistry(effectiveBehavior, global::Mockolate.Mock.").Append(name).Append(".CreateFastInteractions(effectiveBehavior), constructorParameters);").AppendLine();
			sb.Append("\t\t\tif (constructorParameters is null)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tconstructorParameters = [new global::Mockolate.Mock.HttpMessageHandler(mockRegistry),];").AppendLine();
			sb.Append("\t\t\t\tmockRegistry = new global::Mockolate.MockRegistry(mockRegistry, constructorParameters);").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\telse if (constructorParameters.Length > 0 && constructorParameters[0] is global::Mockolate.Mock.HttpMessageHandler && constructorParameters[0] is global::Mockolate.IMock httpMessageHandlerMock)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			sb.Append("\t\t\t\tif (mockBehavior is not null && httpMessageHandlerMock.MockRegistry.Behavior != mockBehavior)").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\tthrow new global::Mockolate.Exceptions.MockException($\"Mock of type 'System.Net.Http.HttpClient' cannot be created with behavior '{mockBehavior}' because it shares its mock registry with a mock of type 'System.Net.Http.HttpMessageHandler' that has behavior '{httpMessageHandlerMock.MockRegistry.Behavior}'.\");").AppendLine();
			sb.Append("\t\t\t\t}").AppendLine();
			sb.Append("\t\t\t\tmockRegistry = new global::Mockolate.MockRegistry(httpMessageHandlerMock.MockRegistry, constructorParameters);").AppendLine();
			sb.Append("\t\t\t}").AppendLine();
			sb.Append("\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
		}
		else
		{
			sb.Append("\t\t\tmockBehavior ??= global::Mockolate.MockBehavior.Default;").AppendLine();
			sb.Append("\t\t\tglobal::Mockolate.MockRegistry mockRegistry = new global::Mockolate.MockRegistry(mockBehavior, global::Mockolate.Mock.").Append(name).Append(".MemberCount, constructorParameters);").AppendLine();
		}

		sb.Append("\t\t\treturn CreateMockInstance(mockRegistry, constructorParameters, setup);").AppendLine();
		sb.Append("\t\t}").AppendLine();

		sb.AppendLine();
		sb.Append("\t\tprivate static ").Append(@class.ClassFullName).Append(" CreateMockInstance(global::Mockolate.MockRegistry mockRegistry, object?[]? constructorParameters, global::System.Action<").Append(setupType).Append(">? setup)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		if (!@class.IsInterface && constructors?.Count > 0)
		{
			sb.Append("\t\t\tif (constructorParameters is null || constructorParameters.Length == 0)").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			if (constructors.Value.Any(m => m.Parameters.Count == 0))
			{
				sb.Append("\t\t\t\tglobal::Mockolate.Mock.").Append(name).Append(".MockRegistryProvider.Value = mockRegistry;").AppendLine();
				sb.Append("\t\t\t\tglobal::Mockolate.MockExtensionsFor").Append(name).Append(".MockSetup? setupTarget = null;").AppendLine();
				sb.Append("\t\t\t\tif (setup is not null)").AppendLine();
				sb.Append("\t\t\t\t{").AppendLine();
				sb.Append("\t\t\t\t\tsetupTarget ??= new(mockRegistry);").AppendLine();
				sb.Append("\t\t\t\t\tsetup.Invoke(setupTarget);").AppendLine();
				sb.Append("\t\t\t\t}").AppendLine();
				sb.Append("\t\t\t\treturn new global::Mockolate.Mock.").Append(name).Append("(mockRegistry);").AppendLine();
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
						.Append(parameter.HasExplicitDefaultValue ? "TryCastWithDefaultValue" : "TryCast")
						.Append("(constructorParameters, ")
						.Append(constructorParameterIndex++)
						.Append(parameter.HasExplicitDefaultValue ? $", {parameter.ExplicitDefaultValue}" : "")
						.Append(", mockRegistry.Behavior, out ").Append(parameter.Type.Fullname).Append(" c")
						.Append(constructorIndex)
						.Append('p')
						.Append(constructorParameterIndex).Append(")");
				}

				sb.Append(")").AppendLine();
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
		sb.AppendXmlReturns($"A new mock of <see cref=\"{escapedClassName}\" /> that delegates to <paramref name=\"instance\" />.");
		sb.Append("\t\tpublic ").Append(@class.ClassFullName).Append(" Wrapping(").Append(@class.ClassFullName).Append(" instance)").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tif (mock is global::Mockolate.IMock mockInterface)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tglobal::Mockolate.MockRegistry wrappingRegistry = new global::Mockolate.MockRegistry(mockInterface.MockRegistry, instance);").AppendLine();
		sb.Append("\t\t\t\twrappingRegistry = new global::Mockolate.MockRegistry(wrappingRegistry, global::Mockolate.Mock.").Append(name).Append(".CreateFastInteractions(wrappingRegistry.Behavior));").AppendLine();
		sb.Append("\t\t\t\treturn CreateMockInstance(wrappingRegistry, mockInterface.MockRegistry.ConstructorParameters, null);").AppendLine();
		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\tthrow new global::Mockolate.Exceptions.MockException(\"The subject is no mock.\");").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
		
		#endregion Wrapping

		sb.Append("\t}").AppendLine();
		sb.AppendLine();

		#endregion Mock Type extensions

		#region MockBehavior extensions

		sb.Append("\t/// <inheritdoc cref=\"MockExtensionsFor").Append(name).Append("\" />").AppendLine();
		sb.Append("\textension(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append("\t{").AppendLine();

		#region Initialize
		
		sb.AppendXmlSummary("Initializes mocks of type <typeparamref name=\"T\" /> with the given <paramref name=\"setup\" />.");
		sb.AppendXmlRemarks("The <paramref name=\"setup\" /> is applied to the mock before the constructor is executed. Calling <c>Initialize</c> again overlays additional setups on top of any previously registered ones.");
		sb.AppendXmlTypeParam("T", $"The mockable type derived from <see cref=\"{escapedClassName}\" /> that this setup should apply to.");
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
#if !DEBUG
			sb.Append("\t[global::System.Diagnostics.DebuggerNonUserCode]").AppendLine();
#endif
			sb.Append("\t[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]").AppendLine();
			sb.Append("\tinternal sealed class MockSetup(global::Mockolate.MockRegistry mockRegistry) : global::Mockolate.Mock.IMockSetupFor").Append(name);
			if (hasProtectedMembers)
			{
				sb.Append(", global::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(", IMockSetupInitializationFor").Append(name);
			}

			sb.AppendLine();
			sb.Append("\t{").AppendLine();
			if (hasProtectedMembers)
			{
				sb.Append("\t\t/// <inheritdoc />").AppendLine();
				sb.Append("\t\tglobal::Mockolate.Mock.IMockProtectedSetupFor").Append(name).Append(" IMockSetupInitializationFor").Append(name).Append('.').Append(protectedName).Append(" => this;").AppendLine();
			}

			sb.Append("\t\tprivate global::Mockolate.MockRegistry ").Append(mockRegistryName).Append(" { get; } = mockRegistry;").AppendLine();
			sb.AppendLine();
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

			sb.Append("\t}").AppendLine();
			#endregion MockSetup
		}

		#endregion Setup helpers

		sb.Append("}").AppendLine();

		#endregion MockForXXXExtensions
		
		sb.AppendLine();
		sb.AppendLine("#nullable disable annotations");
		return sb.ToString();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementMockForInterface(StringBuilder sb, string mockRegistryName, string name,
		bool hasEvents, bool hasProtectedMembers, bool hasProtectedEvents, bool hasStaticMembers, bool hasStaticEvents)
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
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".Interactions.GetUnverifiedInteractions().Count == 0;").AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tbool IMockFor").Append(name).Append(".VerifyThatAllSetupsAreUsed()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".GetUnusedSetups(this.").Append(mockRegistryName).Append(".Interactions).Count == 0;").AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tvoid IMockFor").Append(name).Append(".ClearAllInteractions()").AppendLine();
		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".ClearAllInteractions();").AppendLine();

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append("\t\tglobal::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append("> IMockFor").Append(name).Append(".Monitor()").AppendLine();
		sb.Append("\t\t\t=> new global::Mockolate.Monitor.MockMonitor<IMockVerifyFor").Append(name).Append(">(this.")
			.Append(mockRegistryName).Append(".Interactions, interactions => new VerifyMonitor").Append(name)
			.Append("(new global::Mockolate.MockRegistry(this.").Append(mockRegistryName).Append(", interactions)));")
			.AppendLine();
		sb.AppendLine();
	}
#pragma warning restore S107 // Methods should not have too many parameters

	#region Mock Helpers

	private static void AppendMockSubject_BaseClassConstructor(StringBuilder sb, string mockRegistryName, string name,
		Method constructor, bool hasRequiredMembers, bool setsMockRegistryProvider = false)
	{
		string mockRegistry = CreateUniqueParameterName(constructor.Parameters, "mockRegistry");
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(name).Append("\" />").AppendLine();
		sb.Append(constructor.Attributes, "\t\t");
		if (hasRequiredMembers && constructor.Attributes?.Any(a => a.Name == "global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers") != true)
		{
			sb.Append("\t\t[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]").AppendLine();
		}

		sb.Append("\t\tpublic ").Append(name).Append("(global::Mockolate.MockRegistry ").Append(mockRegistry);
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

		sb.Append(")").AppendLine();
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

		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

	/// <summary>
	///     Emits a static helper that creates a typed-buffer-sized <c>MockRegistry</c> from a
	///     <c>MockBehavior</c>. Used by the generator-emitted <c>(MockBehavior)</c> constructors so
	///     subclasses of the generated mock can construct from a behavior without bypassing the FastMockInteractions
	///     sizing. When <paramref name="setsMockRegistryProvider" /> is true, the helper also primes the
	///     <c>MockRegistryProvider</c> AsyncLocal so virtual calls during base-class construction can resolve
	///     the registry before the chained constructor body has run.
	/// </summary>
	private static void AppendCreateRegistryFromBehavior(StringBuilder sb, string indent,
		bool setsMockRegistryProvider)
	{
		sb.Append(indent).Append("/// <summary>").AppendLine();
		sb.Append(indent).Append("///     Builds a <see cref=\"global::Mockolate.MockRegistry\" /> backed by a typed-buffer-sized ").Append("<see cref=\"global::Mockolate.Interactions.FastMockInteractions\" /> from <paramref name=\"behavior\" />.").AppendLine();
		sb.Append(indent).Append("/// </summary>").AppendLine();
		sb.Append(indent).Append("private static global::Mockolate.MockRegistry MockolateCreateRegistryFromBehavior(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append(indent).Append("{").AppendLine();
		sb.Append(indent).Append("\tglobal::Mockolate.MockRegistry registry = new global::Mockolate.MockRegistry(behavior, MemberCount);").AppendLine();
		if (setsMockRegistryProvider)
		{
			sb.Append(indent).Append("\tMockRegistryProvider.Value = registry;").AppendLine();
		}

		sb.Append(indent).Append("\treturn registry;").AppendLine();
		sb.Append(indent).Append("}").AppendLine();
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
		if (hasRequiredMembers && constructor.Attributes?.Any(a => a.Name == "global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers") != true)
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

		sb.Append(")").AppendLine();
		sb.Append("\t\t\t: this(MockolateCreateRegistryFromBehavior(").Append(behavior).Append(")");
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			sb.Append(", ").Append(parameter.Name);
		}

		sb.Append(")").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t}").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass(StringBuilder sb, Class @class, string mockRegistryName,
		MockClass? mockClass, MemberIdTable memberIds, string memberIdPrefix,
		Dictionary<string, int>? signatureIndicesOverride = null,
		int[]? nextSignatureIndexRef = null,
		bool useFastBuffers = true)
#pragma warning restore S107
	{
		string className = @class.ClassFullName;
		sb.Append("\t\t#region ").Append(@class.DisplayString).AppendLine();
		sb.AppendLine();

		// An aliased member is served by the mocked class' own override: re-implementing it here would
		// re-map the interface slot away from that override and fork the member in two.
		List<Event>? mockEvents = mockClass?.AllEvents().ToList();
		foreach (Event @event in @class.AllEvents())
		{
			if (!memberIds.IsAlias(@event) &&
			    mockEvents?.All(e => !Event.EqualityComparer.Equals(@event, e)) != false)
			{
				AppendMockSubject_ImplementClass_AddEvent(sb, @event, mockRegistryName, className,
					mockClass is not null,
					@class.IsInterface, memberIds, memberIdPrefix, useFastBuffers);
				sb.AppendLine();
			}
		}

		List<Property>? mockProperties = mockClass?.AllProperties().ToList();
		Dictionary<string, int> signatureIndices = signatureIndicesOverride ?? new Dictionary<string, int>();
		int[] nextSignatureIndex = nextSignatureIndexRef ?? [0,];
		foreach (Property property in @class.AllProperties())
		{
			if (!memberIds.IsAlias(property) &&
			    mockProperties?.All(p => !Property.EqualityComparer.Equals(property, p)) != false)
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
					@class.IsInterface, signatureIndex, memberIds, memberIdPrefix, useFastBuffers);
				sb.AppendLine();
			}
		}

		List<Method>? mockMethods = mockClass?.AllMethods().ToList();
		foreach (Method method in @class.AllMethods())
		{
			if (!memberIds.IsAlias(method) &&
			    mockMethods?.All(m => !Method.EqualityComparer.Equals(method, m)) != false)
			{
				AppendMockSubject_ImplementClass_AddMethod(sb, method, mockRegistryName, className,
					mockClass is not null,
					@class.IsInterface, @class, memberIds, memberIdPrefix, useFastBuffers);
				sb.AppendLine();
			}
		}

		sb.Append("\t\t#endregion ").Append(@class.DisplayString).AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendMockSubject_ImplementClass_AddEvent(StringBuilder sb, Event @event,
		string mockRegistryName, string className,
		bool explicitInterfaceImplementation, bool isClassInterface,
		MemberIdTable memberIds, string memberIdPrefix, bool useFastBuffers)
#pragma warning restore S107
	{
		string mockRegistry = @event.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		bool useFast = useFastBuffers && IsFastBufferEligibleEvent(@event);
		string subscribeIdRef = memberIdPrefix + memberIds.GetEventSubscribeIdentifier(@event);
		string unsubscribeIdRef = memberIdPrefix + memberIds.GetEventUnsubscribeIdentifier(@event);
		string addCall = useFast
			? $".AddEvent({subscribeIdRef}, "
			: ".AddEvent(";
		string removeCall = useFast
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

		sb.Append("\t\t/// <inheritdoc cref=\"").Append(@event.ContainingType.EscapeForXmlDoc()).Append('.')
			.Append(@event.Name).AppendLine("\" />");
		sb.Append(@event.Attributes, "\t\t");
		if (explicitInterfaceImplementation)
		{
			sb.Append(@event.IsStatic ? "\t\tstatic event " : "\t\tevent ").Append(@event.Type.Fullname).Append(' ')
				.Append(className).Append('.').Append(@event.Name).AppendLine();
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
		MemberIdTable memberIds, string memberIdPrefix, bool useFastBuffers)
#pragma warning restore S107
	{
		string mockRegistry = property.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		// Interface members are forwarded via their declaring interface: an explicit implementation (hidden
		// via `new`, or a name collision between sibling interfaces) has a different property type than the
		// one reachable through the mocked interface (CS0266), and an implicit member declared by several
		// sibling interfaces is ambiguous on the mocked interface (CS0229/CS0121).
		string wrapsType = isClassInterface ? property.ContainingType : className;
		bool useFastForProperty = useFastBuffers && !property.IsIndexer && IsFastBufferEligibleProperty(property);
		bool useFastForIndexer = useFastBuffers && property.IsIndexer && IsFastBufferEligibleIndexer(property);
		string indexerGetIdRef = property.IsIndexer
			? memberIdPrefix + memberIds.GetIndexerGetIdentifier(property)
			: string.Empty;
		string indexerSetIdRef = property.IsIndexer
			? memberIdPrefix + memberIds.GetIndexerSetIdentifier(property)
			: string.Empty;
		string? indexerGetCachedBufferRef = useFastForIndexer
			? "this." + memberIds.GetIndexerGetterBufferFieldName(property)
			: null;
		string? indexerSetCachedBufferRef = useFastForIndexer
			? "this." + memberIds.GetIndexerSetterBufferFieldName(property)
			: null;
		string propertyGetMemberArg = useFastForProperty
			? memberIdPrefix + memberIds.GetPropertyGetIdentifier(property) + ", "
			: string.Empty;
		string propertySetMemberArg = useFastForProperty
			? memberIdPrefix + memberIds.GetPropertySetIdentifier(property) + ", "
			: string.Empty;
		sb.Append("\t\t/// <inheritdoc cref=\"").Append(property.ContainingType.EscapeForXmlDoc()).Append('.').Append(
				property.IndexerParameters is not null
					? property.Name.Replace("[]",
						$"[{string.Join(", ", property.IndexerParameters.Value.Select(p => $"{p.Type.Fullname.EscapeForXmlDoc()}"))}]")
					: property.Name)
			.AppendLine("\" />");
		sb.Append(property.Attributes, "\t\t");
		if (explicitInterfaceImplementation)
		{
			sb.Append(property.IsStatic ? "\t\tstatic " : "\t\t").Append(property.Type.Fullname)
				.Append(" ").Append(className).Append('.').Append(property.IndexerParameters is not null
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

				sb.Append(property.Type.Fullname).Append(" ");
			}
			else
			{
				sb.Append("\t\t");

				if (property.IsStatic)
				{
					sb.Append("static ");
				}

				sb.Append(property.Type.Fullname).Append(" ").Append(property.ExplicitImplementation)
					.Append('.');
			}

			sb.Append(property.IndexerParameters is not null
				? property.Name.Replace("[]",
					$"[{string.Join(", ", property.IndexerParameters.Value.Select(p => $"{p.Type.Fullname} {p.Name}"))}]")
				: property.Name).AppendLine();
		}

		// Ref-struct-keyed indexer (getter AND setter): the full access pipeline requires the
		// key value to flow through IndexerGetterAccess / IndexerSetterAccess (classes with
		// field slots), which is illegal for a ref-struct key. Emit a runtime
		// NotSupportedException from both accessor bodies; the analyzer flags the declaration
		// at compile time. Full getter wiring to RefStructIndexerGetterSetup is future work.
		bool isRefStructIndexer = property is { IsIndexer: true, IndexerParameters: not null, } &&
		                          property.IndexerParameters.Value.Any(p => p.NeedsRefStructPipeline());

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

			if (isRefStructIndexer)
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
						property.Type, property.IndexerParameters.Value, useFastForIndexer,
						useFastForIndexer ? indexerGetIdRef : null,
						cachedBufferRef: indexerGetCachedBufferRef);
					sb.Append("\t\t\t\tif (").Append(mockRegistry).Append(".Wraps is not ").Append(wrapsType)
						.Append(' ').Append(wrapsVarName).Append(')').AppendLine();
					sb.Append("\t\t\t\t{").AppendLine();
					sb.Append("\t\t\t\t\treturn ").Append(setupVarName).Append(" is null")
						.AppendLine();
					sb.Append("\t\t\t\t\t\t? ").Append(mockRegistry).Append(".GetIndexerFallback<")
						.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
						.Append(signatureIndex).Append(")")
						.AppendLine();
					sb.Append("\t\t\t\t\t\t: ").Append(mockRegistry).Append(".ApplyIndexerSetup<")
						.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
						.Append(setupVarName).Append(", ").Append(signatureIndex).Append(");").AppendLine();
					sb.Append("\t\t\t\t}").AppendLine();
					sb.Append("\t\t\t\t").AppendTypeOrWrapper(property.Type).Append(' ').Append(baseResultVarName)
						.Append(" = ").Append(wrapsVarName).Append('[')
						.Append(FormatIndexerParametersAsNames(property.IndexerParameters.Value)).Append("];")
						.AppendLine();
					sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".ApplyIndexerGetter(")
						.Append(accessVarName).Append(", ").Append(setupVarName).Append(", ")
						.Append(baseResultVarName).Append(", ").Append(signatureIndex).Append(");").AppendLine();
				}
				else
				{
					string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
					if (useFastForProperty)
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetPropertyFast<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
							.Append(", ").Append(accessFieldRef).Append(", static b => ")
							.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue");
						if (!property.IsStatic)
						{
							sb.Append(", ").Append(mockRegistry).Append(".Wraps is not ").Append(wrapsType)
								.Append(" wraps ? null : () => wraps.").Append(property.Name);
						}

						sb.Append(");").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(propertyGetMemberArg).Append(accessFieldRef).Append(", () => ")
							.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue");
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
						property.Type, property.IndexerParameters.Value, useFastForIndexer,
						useFastForIndexer ? indexerGetIdRef : null,
						cachedBufferRef: indexerGetCachedBufferRef);
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

					sb.Append("\t\t\t\t\treturn ").Append(mockRegistry).Append(".ApplyIndexerGetter(")
						.Append(accessVarName).Append(", ").Append(setupVarName).Append(", ")
						.Append(baseResultVarName).Append(", ").Append(signatureIndex).Append(");").AppendLine();
					sb.Append("\t\t\t\t}").AppendLine();
					sb.Append("\t\t\t\treturn ").Append(setupVarName).Append(" is null").AppendLine();
					sb.Append("\t\t\t\t\t? ").Append(mockRegistry).Append(".GetIndexerFallback<")
						.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
						.Append(signatureIndex).Append(")")
						.AppendLine();
					sb.Append("\t\t\t\t\t: ").Append(mockRegistry).Append(".ApplyIndexerSetup<")
						.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
						.Append(setupVarName).Append(", ").Append(signatureIndex).Append(");").AppendLine();
				}
				else
				{
					string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
					if (useFastForProperty)
					{
						sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetPropertyFast<")
							.AppendTypeOrWrapper(property.Type).Append(">(")
							.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
							.Append(", ").Append(accessFieldRef).Append(", static b => ")
							.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue");
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
							.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue");
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
					property.Type, property.IndexerParameters.Value, useFastForIndexer,
					useFastForIndexer ? indexerGetIdRef : null,
					cachedBufferRef: indexerGetCachedBufferRef);
				sb.Append("\t\t\t\treturn ").Append(setupVarName).Append(" is null").AppendLine();
				sb.Append("\t\t\t\t\t? ").Append(mockRegistry).Append(".GetIndexerFallback<")
					.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
					.Append(signatureIndex).Append(")")
					.AppendLine();
				sb.Append("\t\t\t\t\t: ").Append(mockRegistry).Append(".ApplyIndexerSetup<")
					.AppendTypeOrWrapper(property.Type).Append(">(").Append(accessVarName).Append(", ")
					.Append(setupVarName).Append(", ").Append(signatureIndex).Append(");").AppendLine();
			}
			else
			{
				string accessFieldRef = memberIdPrefix + memberIds.GetPropertyGetterAccessFieldName(property);
				if (useFastForProperty)
				{
					sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetPropertyFast<")
						.AppendTypeOrWrapper(property.Type).Append(">(")
						.Append(memberIdPrefix).Append(memberIds.GetPropertyGetIdentifier(property))
						.Append(", ").Append(accessFieldRef)
						.Append(", static b => ")
						.AppendDefaultValueGeneratorFor(property.Type, "b.DefaultValue")
						.Append(");").AppendLine();
				}
				else
				{
					sb.Append("\t\t\t\treturn ").Append(mockRegistry).Append(".GetProperty<")
						.AppendTypeOrWrapper(property.Type).Append(">(")
						.Append(propertyGetMemberArg).Append(accessFieldRef)
						.Append(", () => ")
						.AppendDefaultValueGeneratorFor(property.Type, $"{mockRegistry}.Behavior.DefaultValue")
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

			// Ref-struct-keyed indexer setter: dispatches through
			// RefStructIndexerSetterSetup<TValue, T1..Tn>. Mirrors the getter pipeline.
			if (isRefStructIndexer)
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
						property.Type, property.IndexerParameters.Value, useFastForIndexer,
						useFastForIndexer ? indexerSetIdRef : null,
						useFastForIndexer ? indexerGetIdRef : null,
						indexerSetCachedBufferRef);
					sb.Append("\t\t\t\t").Append(mockRegistry).Append(".ApplyIndexerSetter(")
						.Append(accessVarName).Append(", ").Append(setupVarName).Append(", value, ")
						.Append(signatureIndex).Append(");")
						.AppendLine();

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
					if (useFastForProperty)
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetPropertyFast<")
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
						property.Type, property.IndexerParameters.Value, useFastForIndexer,
						useFastForIndexer ? indexerSetIdRef : null,
						useFastForIndexer ? indexerGetIdRef : null,
						indexerSetCachedBufferRef);
					sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".ApplyIndexerSetter(")
						.Append(accessVarName).Append(", ").Append(setupVarName).Append(", value, ")
						.Append(signatureIndex).Append("))").AppendLine();
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
						property.Type, property.IndexerParameters.Value, useFastForIndexer,
						useFastForIndexer ? indexerSetIdRef : null,
						useFastForIndexer ? indexerGetIdRef : null,
						indexerSetCachedBufferRef);
					sb.Append("\t\t\t\t").Append(mockRegistry).Append(".ApplyIndexerSetter(")
						.Append(accessVarName).Append(", ").Append(setupVarName).Append(", value, ")
						.Append(signatureIndex).Append(");").AppendLine();
				}
			}
			else
			{
				if (!isClassInterface && !property.IsAbstract)
				{
					if (useFastForProperty)
					{
						sb.Append("\t\t\t\tif (!").Append(mockRegistry).Append(".SetPropertyFast<")
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
					if (useFastForProperty)
					{
						sb.Append("\t\t\t\t").Append(mockRegistry).Append(".SetPropertyFast<")
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
		MemberIdTable memberIds, string memberIdPrefix, bool useFastBuffers = true)
#pragma warning restore S107
	{
		string mockRegistry = method.IsStatic ? "MockRegistryProvider.Value" : $"this.{mockRegistryName}";
		string returnRefPrefix = (method.IsRefReturn, method.IsRefReadonlyReturn) switch
		{
			(true, _) => "ref ",
			(false, true) => "ref readonly ",
			(_, _) => "",
		};
		string refReturnStorageField = method.IsRefReturn || method.IsRefReadonlyReturn
			? "_refReturnStorage_" + new string((method.ContainingType + "_" + method.Name)
				.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray())
			: "";
		if (method.IsRefReturn || method.IsRefReadonlyReturn)
		{
			sb.Append("\t\tprivate ").Append(method.ReturnType.Fullname).Append(' ')
				.Append(refReturnStorageField).Append(';').AppendLine();
		}

		sb.Append("\t\t/// <inheritdoc cref=\"").Append(method.ContainingType.EscapeForXmlDoc()).Append('.')
			.Append(method.Name.EscapeForXmlDoc()).Append('(')
			.Append(string.Join(", ", method.Parameters.Select(p => p.RefKind.GetString() + p.Type.Fullname))
				.EscapeForXmlDoc()).AppendLine(")\" />");
		sb.Append(method.Attributes, "\t\t");
		if (explicitInterfaceImplementation)
		{
			sb.Append("\t\t");
			if (method.IsStatic)
			{
				sb.Append("static ");
			}

			sb.Append(returnRefPrefix).Append(method.ReturnType.Fullname).Append(' ').Append(className).Append('.').Append(method.Name)
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
			if (!explicitInterfaceImplementation && parameter.HasExplicitDefaultValue)
			{
				sb.Append(" = ").Append(parameter.ExplicitDefaultValue);
			}
		}

		sb.Append(')');
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			bool isOverride = !isClassInterface && method.UseOverride;
			bool isExplicitImplementation =
				explicitInterfaceImplementation || method.ExplicitImplementation is not null;
			bool inheritsConstraints = isExplicitImplementation || isOverride || method.IsEquals() ||
			                           method.IsGetHashCode() || method.IsToString();
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", inheritsConstraints, isExplicitImplementation);
			}
		}

		sb.AppendLine();
		sb.AppendLine("\t\t{");

		// Methods that use a generic type parameter declaring `allows ref struct` in their
		// signature cannot route through the regular setup pipeline: ReturnMethodSetup<T> /
		// IReturnMethodSetup<T> do not declare the same anti-constraint, so referencing them
		// with such a T fails with CS9244. Mirroring the carve-out for unsupported ref-struct
		// shapes, the body throws NotSupportedException so the rest of the type still compiles.
		if (method.HasUnsupportedAllowsRefStructTypeParameter)
		{
			sb.Append(
					"\t\t\tthrow new global::System.NotSupportedException(\"Mockolate: methods with a generic type parameter declaring 'allows ref struct' are not supported. Method '")
				.Append(method.ContainingType).Append('.').Append(method.Name).Append("'.\");").AppendLine();
			sb.AppendLine("\t\t}");
			return;
		}

		// Methods with at least one ref-struct parameter (outside the Span/ReadOnlySpan wrapper
		// carve-out) route through the ref-struct setup pipeline. The ref-struct value cannot
		// be captured in a closure, so we emit a synchronous, stack-bound match/invoke loop.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			AppendMockSubject_ImplementClass_AddRefStructMethodBody(sb, method, mockRegistry);
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
		EmitFastMethodSetupLookup(sb, "\t\t\t", mockRegistry, methodSetup, methodSetupType, memberIdRef,
			method.GetUniqueNameString(), sb2.ToString(), isGeneric);
		sb.Append("\t\t\tbool ").Append(hasWrappedResult).Append(" = false;").AppendLine();
		if (method.ReturnType != Type.Void)
		{
			sb.Append("\t\t\t").Append(method.ReturnType.Fullname).Append(" ").Append(wrappedResult)
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

		bool useCachedMethodBuffer = useFastBuffers && !method.IsStatic && IsFastBufferEligibleMethod(method);
		sb.Append("\t\t\tif (").Append(mockRegistry).Append(".Behavior.SkipInteractionRecording == false)")
			.AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		if (useCachedMethodBuffer)
		{
			sb.Append("\t\t\t\tthis.").Append(memberIds.GetMethodBufferFieldName(method)).Append(".Append(")
				.Append(method.GetUniqueNameString());
			if (method.Parameters.Count > 0)
			{
				sb.Append(", ").Append(string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper())));
			}

			sb.Append(");").AppendLine();
		}
		else if (useFastBuffers && IsFastBufferEligibleMethod(method))
		{
			int arity = method.Parameters.Count;
			string typeArgs = arity == 0
				? string.Empty
				: "<" + string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper())) + ">";
			string bufferType = $"global::Mockolate.Interactions.FastMethod{arity}Buffer{typeArgs}";

			sb.Append("\t\t\t\t((global::Mockolate.Interactions.FastMockInteractions)")
				.Append(mockRegistry).Append(".Interactions).GetOrCreateBuffer<").Append(bufferType)
				.Append(">(").Append(memberIdRef).Append(", static fast => new ").Append(bufferType)
				.Append("(fast)).Append(")
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

			sb.Append("(").Append(method.GetUniqueNameString());
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
				.Append(".Behavior) ?? ").Append(mockRegistry).Append(".Behavior.SkipBaseClass)");
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
					bool isSpanOrReadOnlySpan = parameter.Type.SpecialGenericType
						is SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan;
					if (isSpanOrReadOnlySpan)
					{
						// C# does not insert user-defined implicit conversions across `out`, so the
						// wrapper-typed slot must be unpacked into a wrapper-typed temp local first,
						// then assigned to the bare-typed parameter (which triggers the implicit op).
						sb.Append("\t\t\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
							.Append(" is not global::Mockolate.Parameters.IOutParameter<")
							.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(outParamBase)
							.Append(parameterIndex)
							.Append(" || !").Append(outParamBase).Append(parameterIndex)
							.Append(".TryGetValue(out ").Append(parameter.Type.ToTypeOrWrapper())
							.Append(' ').Append(outTempBase).Append(parameterIndex).Append("))").AppendLine();
						sb.Append("\t\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ")
							.AppendDefaultValueGeneratorFor(parameter.Type, $"{mockRegistry}.Behavior.DefaultValue")
							.Append(';').AppendLine();
						sb.Append("\t\t\t\t\t\t}").AppendLine();
						sb.Append("\t\t\t\t\t\telse").AppendLine();
						sb.Append("\t\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ").Append(outTempBase)
							.Append(parameterIndex).Append(';').AppendLine();
						sb.Append("\t\t\t\t\t\t}").AppendLine();
					}
					else
					{
						sb.Append("\t\t\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
							.Append(" is not global::Mockolate.Parameters.IOutParameter<")
							.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(outParamBase)
							.Append(parameterIndex)
							.Append(" || !").Append(outParamBase).Append(parameterIndex).Append(".TryGetValue(out ")
							.Append(parameter.Name).Append("))").AppendLine();
						sb.Append("\t\t\t\t\t\t{").AppendLine();
						sb.Append("\t\t\t\t\t\t\t").Append(parameter.Name).Append(" = ")
							.AppendDefaultValueGeneratorFor(parameter.Type, $"{mockRegistry}.Behavior.DefaultValue")
							.Append(';').AppendLine();
						sb.Append("\t\t\t\t\t\t}").AppendLine();
					}
				}
				else if (parameter.RefKind == RefKind.Ref)
				{
					sb.Append("\t\t\t\t\t\tif (").Append(wpc).Append(".Parameter").Append(parameterIndex)
						.Append(" is global::Mockolate.Parameters.IRefParameter<")
						.Append(parameter.Type.ToTypeOrWrapper()).Append("> ").Append(refParamBase)
						.Append(parameterIndex)
						.Append(")").AppendLine();
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

		sb.Append("\t\t\t}").AppendLine();
		sb.Append("\t\t\tfinally").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		AppendTriggerCallbacks(sb, "\t\t\t\t", methodSetup, method.Parameters);
		sb.Append("\t\t\t}").AppendLine();

		string displayMethodName =
			$"{method.ContainingType}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type.DisplayName))})";
		sb.Append("\t\t\tif (").Append(methodSetup).Append(" is null && !").Append(hasWrappedResult).Append(" && ")
			.Append(mockRegistry).Append(".Behavior.ThrowWhenNotSetup)").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
		sb.Append("\t\t\t\tthrow new global::Mockolate.Exceptions.MockNotSetupException(\"The method '")
			.Append(displayMethodName).Append("' was invoked without prior setup.\");").AppendLine();
		sb.Append("\t\t\t}").AppendLine();

		if (method.ReturnType != Type.Void)
		{
			bool isRefReturn = method.IsRefReturn || method.IsRefReadonlyReturn;
			string returnValue = Helpers.GetUniqueLocalVariableName("returnValue", method.Parameters);
			sb.Append("\t\t\tif (").Append(methodSetup).Append("?.HasReturnCallbacks != true && ")
				.Append(hasWrappedResult).Append(")").AppendLine();
			sb.Append("\t\t\t{").AppendLine();
			if (isRefReturn)
			{
				sb.Append("\t\t\t\tthis.").Append(refReturnStorageField).Append(" = ")
					.Append(wrappedResult).Append(";").AppendLine();
				sb.Append("\t\t\t\treturn ref this.").Append(refReturnStorageField).Append(";").AppendLine();
			}
			else
			{
				sb.Append("\t\t\t\treturn ").Append(wrappedResult).Append(";").AppendLine();
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
			if (method.Parameters.Count > 0)
			{
				sb.Append(string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper()))).Append(", ");
				defaultValueGeneratorSuffix = string.Join(", ", method.Parameters.Select(p => p.ToNameOrWrapper()));
			}

			sb.Append("out var ").Append(returnValue).Append(") == true ? ").Append(returnValue).Append(" : ")
				.AppendDefaultValueGeneratorFor(method.ReturnType, $"{mockRegistry}.Behavior.DefaultValue",
					defaultValueGeneratorSuffix)
				.Append(';').AppendLine();
			if (isRefReturn)
			{
				sb.Append("\t\t\treturn ref this.").Append(refReturnStorageField).Append(";").AppendLine();
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
	///         Out of scope (throws <c>NotSupportedException</c> at mock-invocation time, but the
	///         mock class still compiles so the rest of the interface can be mocked):
	///         arity above 4; out/ref ref-struct parameters; ref-struct return types that aren't
	///         <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c>. Proper analyzer-level diagnostics
	///         for these live in <c>MockabilityAnalyzer</c>.
	///     </para>
	/// </remarks>
	private static void AppendMockSubject_ImplementClass_AddRefStructMethodBody(
		StringBuilder sb, Method method, string mockRegistry)
	{
		sb.Append("#if NET9_0_OR_GREATER").AppendLine();

		bool returnsUnsupportedRefStruct = method.ReturnType.IsRefStruct &&
		                                   method.ReturnType.SpecialGenericType is not
			                                   (SpecialGenericType.Span or SpecialGenericType.ReadOnlySpan);

		if (returnsUnsupportedRefStruct)
		{
			sb.Append("\t\t\tthrow new global::System.NotSupportedException(\"Mockolate: ")
				.Append("methods returning a non-span ref struct are not supported")
				.Append(". Method '").Append(method.ContainingType).Append('.')
				.Append(method.Name).Append("'.\");").AppendLine();
			sb.Append("#else").AppendLine();
			sb.Append(
					"#error Mockolate: methods with ref-struct parameters require .NET 9 or later (uses the 'allows ref struct' anti-constraint).")
				.AppendLine();
			sb.Append("\t\t\tthrow new global::System.NotSupportedException();").AppendLine();
			sb.Append("#endif").AppendLine();
			return;
		}

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
				string outVar = Helpers.GetUniqueLocalVariableName($"outParam{refStructSlotIndex}", method.Parameters);
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
				string refVar = Helpers.GetUniqueLocalVariableName($"refParam{refStructSlotIndex}", method.Parameters);
				sb.Append("\t\t\t\tif (").Append(setupVar).Append(".GetMatcher").Append(refStructSlotIndex)
					.Append("() is global::Mockolate.Parameters.IRefRefStructParameter<")
					.Append(parameter.Type.Fullname).Append("> ").Append(refVar).Append(")").AppendLine();
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
				.AppendDefaultValueGeneratorFor(method.ReturnType, $"{mockRegistry}.Behavior.DefaultValue")
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

	// Priority hierarchy among same-arity overloads emitted for one method:
	//   all-values            : int.MaxValue       (so `Method(default, …)` binds here and exposes `.AnyParameters()`)
	//   IParameters           : int.MaxValue - 1
	//   pure IParameter<T>?   : parameterCount     (just below all-values, above any mixed combo)
	//   mixed                 : count of non-value (matcher) params, in [1, parameterCount - 1]
	// parameterCount == 0 returns int.MaxValue so the lone zero-arg overload keeps its historical priority.
	private static string ComputeMethodOverloadPriority(bool useParameters, bool[]? valueFlags, int parameterCount)
	{
		if (parameterCount == 0)
		{
			return "int.MaxValue";
		}

		if (useParameters)
		{
			return "int.MaxValue - 1";
		}

		if (valueFlags is null)
		{
			return parameterCount.ToString();
		}

		return valueFlags.Count(x => !x).ToString();
	}

	// Object-typed parameters defeat the all-values bump: every reference type — including the
	// IParameter<T>? matcher itself — implicitly converts to object, so promoting `Method(object?)` over
	// `Method(IParameter<object?>?)` would silently capture matcher instances as raw values. Detect that case
	// here and skip the priority bump so callers keep using Match.AnyParameters() / It.IsAny<object?>().
	private static bool ParametersBlockAllValuesPromotion(EquatableArray<MethodParameter> parameters,
		bool[] valueFlags)
	{
		ReadOnlySpan<MethodParameter> span = parameters.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			if (valueFlags[i] && span[i].Type.SpecialType == SpecialType.System_Object)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     Which accessors of <paramref name="property" /> the mock actually intercepts, and therefore
	///     which setup/verify facade the generated surface exposes. The setup type, the verify type and the
	///     verify constructor arguments all derive from this one classification so they cannot disagree.
	/// </summary>
	/// <remarks>
	///     The classification follows the accessors the mock emits a body for, which is exactly what
	///     <see cref="Property.Getter" />/<see cref="Property.Setter" /> being non-null means: the property
	///     declares the accessor, and it is not an <c>internal</c>/<c>private protected</c> one hidden from
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
			PropertyAccessors.GetOnly => $"global::Mockolate.Setup.IPropertyGetterOnlySetup<{property.Type.Fullname}>",
			PropertyAccessors.SetOnly => $"global::Mockolate.Setup.IPropertySetterOnlySetup<{property.Type.Fullname}>",
			_ => $"global::Mockolate.Setup.PropertySetup<{property.Type.Fullname}>",
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
				$"global::Mockolate.Verify.VerificationPropertySetterResult<{verifyName}, {property.Type.Fullname}>",
			_ => $"global::Mockolate.Verify.VerificationPropertyResult<{verifyName}, {property.Type.Fullname}>",
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
			   property.MemberType == memberType;
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			sb.AppendXmlSummary(
				$"Setup for the {property.Type.Fullname.EscapeForXmlDoc()} property <see cref=\"{property.DeclaredContainingType.EscapeForXmlDoc()}.{property.Name}\" />.");
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
				$"Setup for the event <see cref=\"{@event.DeclaredContainingType.EscapeForXmlDoc()}.{@event.Name}\" />.");
			sb.Append("\t\tglobal::Mockolate.Setup.EventSetup ").Append(@event.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate =
			indexer => indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, } &&
			           indexer.MemberType == memberType;
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(indexer.IndexerParameters!.Value))
				{
					AppendUnionIndexerSetupDefinition(sb, indexer, slots);
				}

				continue;
			}

			AppendIndexerSetupDefinition(sb, indexer, hasOverloadResolutionPriority: hasOverloadResolutionPriority);
			if (indexer.IndexerParameters!.Value.Count <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(indexer.IndexerParameters.Value))
				{
					AppendIndexerSetupDefinition(sb, indexer, valueFlags, hasOverloadResolutionPriority);
				}
			}
			else
			{
				bool[] allValueFlags = indexer.IndexerParameters.Value.Select(p => p.CanUseNullableParameterOverload())
					.ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendIndexerSetupDefinition(sb, indexer, allValueFlags, hasOverloadResolutionPriority);
				}
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(m => m.Name).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodSetupDefinition(sb, @class, method, true,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodSetupDefinition(sb, @class, method, false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters))
					{
						AppendUnionMethodSetupDefinition(sb, @class, method, slots);
					}
				}
				else
				{
					AppendMethodSetupDefinition(sb, @class, method, false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodSetupDefinition(sb, @class, method, false,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, perElementParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodSetupDefinition(sb, @class, method, false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority);
						}
					}
					else
					{
						bool[] allValueFlags = method.Parameters.Select(p => p.CanUseNullableParameterOverload())
							.ToArray();
						if (allValueFlags.Any(f => f))
						{
							AppendMethodSetupDefinition(sb, @class, method, false, valueFlags: allValueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority);
						}
					}
				}
			}
		}

		#endregion
	}

	private static void AppendOverloadDifferentiatorRemark(StringBuilder sb,
		IReadOnlyList<string> parameterNames, bool useParameters, bool[]? valueFlags,
		bool isVerify = false)
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
					valueParams.Add($"<paramref name=\"{parameterNames[i]}\" />");
				}
				else
				{
					matcherParams.Add($"<paramref name=\"{parameterNames[i]}\" />");
				}
			}

			string directPart = string.Join(", ", valueParams);
			string matcherPart = string.Join(", ", matcherParams);
			text =
				$"This overload accepts a direct value for {directPart} (equivalent to <c>It.Is&lt;T&gt;(value)</c>) and an <see cref=\"global::Mockolate.It\" /> matcher for {matcherPart}.";
		}

		sb.AppendXmlRemarks(text);
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

	/// <summary>
	///     The <c>cref</c> target for <paramref name="method" />, shared between the classic and union summaries.
	/// </summary>
	private static string MethodCref(Method method)
		=> $"{method.DeclaredContainingType.EscapeForXmlDoc()}.{method.Name.EscapeForXmlDoc()}({string.Join(", ", method.Parameters.Select(p => p.RefKind.GetString() + p.Type.Fullname.EscapeForXmlDoc()))})";

	/// <summary>
	///     The <c>cref</c> target for <paramref name="indexer" />, shared between the classic and union summaries.
	/// </summary>
	private static string IndexerCref(Property indexer)
		=> $"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.RefKind.GetString() + p.Type.Fullname.EscapeForXmlDoc()))}]";

	private static void AppendMethodSetupDefinition(StringBuilder sb, Class @class, Method method,
		bool useParameters, string? methodNameOverride = null, bool[]? valueFlags = null,
		bool hasOverloadResolutionPriority = false, bool perElementParams = false)
	{
		// Methods using a generic type parameter that declares `allows ref struct` cannot expose
		// a setup surface: IReturnMethodSetup<T> / IVoidMethodSetup<T> do not carry the same
		// anti-constraint. The override body throws NotSupportedException, so no setup is needed.
		if (method.HasUnsupportedAllowsRefStructTypeParameter)
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
		sb.Append("\t\t/// <summary>").AppendLine();
		if (methodNameOverride is null)
		{
			sb.Append("\t\t///     Setup for the method <see cref=\"").Append(MethodCref(method)).Append("\"/>");
		}
		else
		{
			sb.Append("\t\t///     Setup for the delegate <see cref=\"")
				.Append(@class.ClassFullName.EscapeForXmlDoc())
				.Append("\"/>");
		}

		if (useParameters)
		{
			sb.Append(" with the given <paramref name=\"parameters\" />");
		}
		else if (method.Parameters.Any())
		{
			sb.Append(" with the given ")
				.Append(string.Join(", ", method.Parameters.Select(p => $"<paramref name=\"{p.Name}\"/>")));
		}

		sb.Append(".").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		AppendOverloadDifferentiatorRemark(sb, method.Parameters.Select(p => p.Name).ToArray(), useParameters,
			valueFlags);
		if (method.ReturnType != Type.Void)
		{
			if (valueFlags?.All(x => x) == true)
			{
				if (hasOverloadResolutionPriority &&
				    !ParametersBlockAllValuesPromotion(method.Parameters, valueFlags))
				{
					sb.Append(
							"\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue)]")
						.AppendLine();
				}

				sb.Append("\t\tglobal::Mockolate.Setup.IReturnMethodSetupParameterIgnorer");
			}
			else
			{
				if (hasOverloadResolutionPriority)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(ComputeMethodOverloadPriority(useParameters, valueFlags, method.Parameters.Count))
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
			sb.Append(methodName).Append("(");
		}
		else
		{
			if (valueFlags?.All(x => x) == true)
			{
				if (hasOverloadResolutionPriority &&
				    !ParametersBlockAllValuesPromotion(method.Parameters, valueFlags))
				{
					sb.Append(
							"\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue)]")
						.AppendLine();
				}

				sb.Append("\t\tglobal::Mockolate.Setup.IVoidMethodSetupParameterIgnorer");
			}
			else
			{
				if (hasOverloadResolutionPriority)
				{
					sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
						.Append(ComputeMethodOverloadPriority(useParameters, valueFlags, method.Parameters.Count))
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

			sb.Append(' ').Append(methodName).Append("(");
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters parameters)");
		}
		else
		{
			ReadOnlySpan<MethodParameter> paramSpan = method.Parameters.AsSpan();
			bool[] hasTrailingDefault = ComputeTrailingDefaults(paramSpan, valueFlags);

			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(">[] ").Append(parameter.Name);
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

			sb.Append(")");
		}

		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t");
			}
		}

		sb.Append(";").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementSetupInterface(StringBuilder sb, Class @class, string mockRegistryName,
		string setupName, MemberType memberType, MemberIdTable memberIds, string memberIdPrefix,
		string? scopeExpression = null, bool useUnionOverloads = false)
#pragma warning restore S107
	{
		string scopePrefix = scopeExpression is null ? "" : scopeExpression + ", ";

		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType;
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
				.Append(property.Type.Fullname).Append(">(").Append(mockRegistryName).Append(", ")
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
			           indexer.MemberType == memberType;
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(indexer.IndexerParameters!.Value))
				{
					AppendUnionIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds,
						memberIdPrefix, slots, scopeExpression);
				}

				continue;
			}

			AppendIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds, memberIdPrefix,
				scopeExpression: scopeExpression);
			if (indexer.IndexerParameters!.Value.Count <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(indexer.IndexerParameters.Value))
				{
					AppendIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds,
						memberIdPrefix, valueFlags, scopeExpression);
				}
			}
			else
			{
				bool[] allValueFlags = indexer.IndexerParameters.Value.Select(p => p.CanUseNullableParameterOverload())
					.ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendIndexerSetupImplementation(sb, indexer, mockRegistryName, setupName, memberIds,
						memberIdPrefix, allValueFlags, scopeExpression);
				}
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(m => m.Name).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, true,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression);
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, false,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters))
					{
						AppendUnionMethodSetupImplementation(sb, method, mockRegistryName, setupName, memberIds,
							memberIdPrefix, slots, scopeExpression: scopeExpression);
					}
				}
				else
				{
					AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, false,
						memberIds, memberIdPrefix, scopeExpression: scopeExpression);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, false,
							memberIds, memberIdPrefix, scopeExpression: scopeExpression, perElementParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, false,
								memberIds, memberIdPrefix, valueFlags: valueFlags, scopeExpression: scopeExpression);
						}
					}
					else
					{
						bool[] allValueFlags = method.Parameters.Select(p => p.CanUseNullableParameterOverload())
							.ToArray();
						if (allValueFlags.Any(f => f))
						{
							AppendMethodSetupImplementation(sb, method, mockRegistryName, setupName, false,
								memberIds, memberIdPrefix, valueFlags: allValueFlags, scopeExpression: scopeExpression);
						}
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
		string? scopeExpression = null, bool perElementParams = false)
	{
		// Setup-side carve-out: methods using a generic type parameter that declares
		// `allows ref struct` have no setup interface declaration (see
		// AppendMethodSetupDefinition), so no explicit implementation is emitted either.
		if (method.HasUnsupportedAllowsRefStructTypeParameter)
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
			sb.Append(methodName).Append("(");
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

			sb.Append(" global::Mockolate.Mock.").Append(setupName).Append('.').Append(methodName).Append("(");
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters parameters)");
		}
		else
		{
			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(">[] ").Append(parameter.Name);
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

			sb.Append(")");
		}

		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", true, true);
			}
		}

		sb.AppendLine();
		sb.AppendLine("\t\t{");
		string methodSetupVar = Helpers.GetUniqueLocalVariableName("methodSetup", method.Parameters);
		if (method.ReturnType != Type.Void)
		{
			sb.Append("\t\t\tvar ").Append(methodSetupVar).Append(" = new global::Mockolate.Setup.ReturnMethodSetup<");
			AppendSetupReturnType(sb, method);

			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
			}

			sb.Append(">");
		}
		else
		{
			sb.Append("\t\t\tvar ").Append(methodSetupVar).Append(" = new global::Mockolate.Setup.VoidMethodSetup");

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

		// Look up the method's memberId from the table; all other fluent overloads for the same
		// method share the same id since they register setups for the same target method overload.
		string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
		if (useParameters)
		{
			sb.Append(".WithParameters(").Append(mockRegistryName).Append(", ").Append(method.GetUniqueNameString())
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
			// Fused literal path: when the call site is the bare-value overload (every parameter is a
			// literal value) we can store the values directly on the setup via WithLiteralValues and
			// skip the per-parameter IParameterMatch<T> allocations that WithParameterCollection
			// would otherwise force via It.IsValue<T>(...). Gated to 1..4 parameters because that is
			// the arity range covered by the WithLiteralValues nested types.
			// A params value parameter matches element-wise by value (It.SequenceEquals), which the
			// reference-equality WithLiteralValues fast path cannot express — force the collection path.
			bool useLiteralValues = valueFlags is { Length: > 0 and <= MaxExplicitParameters, } &&
			                        valueFlags.All(x => x) &&
			                        !HasParamsValueParameter(method, valueFlags) &&
			                        !method.Parameters.Any(p => p.RefKind == RefKind.Out ||
			                                                    p.RefKind == RefKind.Ref ||
			                                                    p.RefKind == RefKind.RefReadOnlyParameter);
			if (useLiteralValues)
			{
				sb.Append(".WithLiteralValues(").Append(mockRegistryName).Append(", ")
					.Append(method.GetUniqueNameString());
				foreach (MethodParameter parameter in method.Parameters)
				{
					sb.Append(", ").Append(parameter.Name);
				}

				sb.Append(");").AppendLine();
			}
			else
			{
				sb.Append(".WithParameterCollection(").Append(mockRegistryName).Append(", ")
					.Append(method.GetUniqueNameString());
				int j = 0;
				foreach (MethodParameter parameter in method.Parameters)
				{
					sb.Append(", ");
					if (perElementParams && parameter.IsParams &&
					    parameter.Type.ElementType is not null)
					{
						sb.Append("new global::Mockolate.Parameters.ParamsArrayParameterMatch<")
							.Append(parameter.Type.ElementType.Fullname).Append(">(").Append(parameter.Name)
							.Append(")");
					}
					else if (valueFlags?[j] == true)
					{
						if (parameter.IsParams && parameter.Type.ElementType is not null)
						{
							// params value parameter: match element-wise by value via It.SequenceEquals.
							sb.Append("CovariantParameterAdapter<").Append(parameter.Type.Fullname)
								.Append(">.Wrap(global::Mockolate.It.SequenceEquals<")
								.Append(parameter.Type.ElementType.Fullname).Append(">(").Append(parameter.Name)
								.Append("))");
						}
						else
						{
							AppendNamedValueParameter(sb, parameter);
						}
					}
					else
					{
						AppendNamedParameter(sb, parameter);
					}

					j++;
				}

				sb.Append(");").AppendLine();
			}

			sb.Append("\t\t\tthis.").Append(mockRegistryName).Append(".SetupMethod(")
				.Append(memberIdRef).Append(", ").Append(scopePrefix).Append(methodSetupVar).Append(");").AppendLine();
			sb.Append("\t\t\treturn ").Append(methodSetupVar).Append(';').AppendLine();
		}

		sb.AppendLine("\t\t}");
		sb.AppendLine();
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
			$"Setup for the method <see cref=\"{method.DeclaredContainingType.EscapeForXmlDoc()}.{method.Name.EscapeForXmlDoc()}({string.Join(", ", method.Parameters.Select(p => p.Type.Fullname.EscapeForXmlDoc()))})\"/>" +
			" — ref-struct parameter pipeline (narrow setup surface).");
		sb.Append("\t\t").Append(iface).Append(' ').Append(methodName).Append("(");
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
			.Append(methodName).Append("(");
		int i = 0;
		foreach (MethodParameter parameter in method.Parameters)
		{
			if (i++ > 0)
			{
				sb.Append(", ");
			}

			sb.Append(parameter.ToParameter()).Append("? ").Append(parameter.Name);
		}

		sb.Append(")").AppendLine();
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

	private static void AppendIndexerSetupDefinition(StringBuilder sb, Property indexer, bool[]? valueFlags = null,
		bool hasOverloadResolutionPriority = false)
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
			$"Setup for the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer <see cref=\"{IndexerCref(indexer)}\" />");
		string[] indexerNames = Enumerable.Range(1, indexer.IndexerParameters!.Value.Count)
			.Select(i => $"parameter{i}").ToArray();
		AppendOverloadDifferentiatorRemark(sb, indexerNames, false, valueFlags);
		if (hasOverloadResolutionPriority)
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
				.Append(valueFlags?.Count(x => !x).ToString() ?? "int.MaxValue").Append(")]").AppendLine();
		}

		sb.Append("\t\t").Append(GetIndexerSetupType(indexer)).AppendTypeOrWrapper(indexer.Type);
		foreach (MethodParameter parameter in indexer.IndexerParameters!)
		{
			sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
		}

		sb.Append("> this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			bool isValueParam = valueFlags?[i] == true;
			if (isValueParam)
			{
				sb.Append(parameter.ToNullableType()).Append($" parameter{i + 1}");
			}
			else
			{
				sb.Append(parameter.ToParameter());
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append('?');
				}

				sb.Append($" parameter{i + 1}");
			}

			i++;
		}

		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendIndexerSetupImplementation(StringBuilder sb, Property indexer, string mockRegistryName,
		string setupName, MemberIdTable memberIds, string memberIdPrefix,
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
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			bool isValueParam = valueFlags?[i] == true;
			if (isValueParam)
			{
				sb.Append(parameter.ToNullableType()).Append($" parameter{i + 1}");
			}
			else
			{
				sb.Append(parameter.ToParameter());
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append('?');
				}

				sb.Append($" parameter{i + 1}");
			}

			i++;
		}

		sb.Append("]").AppendLine();
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
			string paramRef = $"parameter{j + 1}";
			if (isValueParam)
			{
				AppendNamedValueParameter(sb, parameter, paramRef);
			}
			else
			{
				sb.Append("CovariantParameterAdapter<")
					.AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(")
					.Append(paramRef);
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append($" ?? global::Mockolate.It.IsNull<{parameter.ToNullableType()}>(\"null\")");
				}

				sb.Append(")");
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
			$"Setup for the ref-struct-keyed getter-only indexer <see cref=\"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname.EscapeForXmlDoc()))}]\" />" +
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
				.Append(">? ").Append($"parameter{i}");
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
			$"Setup for the ref-struct-keyed setter-only indexer <see cref=\"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname.EscapeForXmlDoc()))}]\" />" +
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
			$"Setup for the ref-struct-keyed get+set indexer <see cref=\"{indexer.DeclaredContainingType.EscapeForXmlDoc()}.this[{string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.Type.Fullname.EscapeForXmlDoc()))}]\" />" +
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
		bool[]? valueFlags = null, bool hasOverloadResolutionPriority = false)
	{
		// Ref-struct-keyed indexers: no Verify surface — same rationale as the setup path.
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		sb.AppendXmlSummary(
			$"Verify interactions with the {indexer.Type.Fullname.EscapeForXmlDoc()} indexer <see cref=\"{IndexerCref(indexer)}\" />.");
		AppendOverloadDifferentiatorRemark(sb,
			indexer.IndexerParameters!.Value.Select(p => p.Name).ToArray(),
			false, valueFlags, true);
		if (hasOverloadResolutionPriority)
		{
			sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
				.Append(valueFlags?.Count(x => !x).ToString() ?? "int.MaxValue").Append(")]").AppendLine();
		}

		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(" this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters!.Value)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			bool isValueParam = valueFlags?[i] == true;
			if (isValueParam)
			{
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

		sb.Append("] { get; }").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void AppendIndexerVerifyImplementation(StringBuilder sb, Property indexer, string mockRegistryName,
		string verifyName, MemberIdTable memberIds, string memberIdPrefix, bool useFastBuffers,
		bool[]? valueFlags = null)
#pragma warning restore S107
	{
		if (indexer.IndexerParameters!.Value.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		bool useFastForIndexer = useFastBuffers && IsFastBufferEligibleIndexer(indexer);
		string indexerGetMemberId = useFastForIndexer
			? memberIdPrefix + memberIds.GetIndexerGetIdentifier(indexer)
			: "-1";
		string indexerSetMemberId = useFastForIndexer
			? memberIdPrefix + memberIds.GetIndexerSetIdentifier(indexer)
			: "-1";

		sb.Append("\t\t/// <inheritdoc />").AppendLine();
		sb.Append(
				"\t\t[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]")
			.AppendLine();
		sb.Append("\t\t");
		AppendIndexerVerifyType(sb, indexer, verifyName);
		sb.Append(' ').Append(verifyName).Append(".this[");
		int i = 0;
		foreach (MethodParameter parameter in indexer.IndexerParameters!.Value)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}

			bool isValueParam = valueFlags?[i] == true;
			if (isValueParam)
			{
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

		sb.Append("]").AppendLine();
		sb.Append("\t\t{").AppendLine();
		sb.Append("\t\t\tget").AppendLine();
		sb.Append("\t\t\t{").AppendLine();
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
				.Append(", ").Append(verifyMemberIds).Append(",").AppendLine();

			AppendIndexerVerifyTypedMatches(sb, indexer.IndexerParameters.Value, valueFlags);
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
						.Append(", ").Append(indexerGetMemberId).Append(",").AppendLine();
					break;
				case PropertyAccessors.SetOnly:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerSetterResult<")
						.Append(verifyName);
					foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
					{
						sb.Append(", ").AppendTypeOrWrapper(parameter.Type);
					}

					sb.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerSetMemberId).Append(",").AppendLine();
					break;
				default:
					sb.Append("\t\t\t\treturn new global::Mockolate.Verify.VerificationIndexerResult<").Append(verifyName)
						.Append(", ").AppendTypeOrWrapper(indexer.Type).Append(">(this, this.").Append(mockRegistryName)
						.Append(", ").Append(indexerGetMemberId).Append(", ").Append(indexerSetMemberId).Append(",").AppendLine();
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
				AppendIndexerVerifyParameterMatches(sb, indexer.IndexerParameters.Value, valueFlags, "g");
				sb.Append(",").AppendLine();
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
				AppendIndexerVerifyParameterMatches(sb, indexer.IndexerParameters.Value, valueFlags, "s");
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

			sb.Append("{").Append(k).Append("}");
		}

		sb.Append("]\"");
		int kk = 0;
		foreach (string? parameterName in indexer.IndexerParameters.Value.Select(p => p.Name))
		{
			sb.Append(", ");
			bool isValueParam = valueFlags?[kk] == true;
			if (isValueParam)
			{
				sb.Append("(object?)").Append(parameterName);
			}
			else
			{
				sb.Append("(object?)").Append(parameterName).Append(" ?? \"null\"");
			}

			kk++;
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
		EquatableArray<MethodParameter> parameters, bool[]? valueFlags)
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
				sb.Append("CovariantParameterAdapter<").AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(").Append(parameter.Name);
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append(" ?? global::Mockolate.It.IsNull<").Append(parameter.ToNullableType())
						.Append(">(\"null\")");
				}

				sb.Append(")");
			}

			sb.Append(',').AppendLine();
			j++;
		}
	}

	private static void AppendIndexerVerifyParameterMatches(StringBuilder sb,
		EquatableArray<MethodParameter> parameters, bool[]? valueFlags, string interactionVar)
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
					.Append(", ").Append(interactionVar).Append(".Parameter").Append(j + 1).Append(")");
			}
			else
			{
				sb.Append("CovariantParameterAdapter<").AppendTypeOrWrapper(parameter.Type)
					.Append(">.Wrap(").Append(parameter.Name);
				if (parameter.CanUseNullableParameterOverload())
				{
					sb.Append(" ?? global::Mockolate.It.IsNull<").Append(parameter.ToNullableType())
						.Append(">(\"null\")");
				}

				sb.Append(").Matches(").Append(interactionVar).Append(".Parameter").Append(j + 1).Append(")");
			}

			j++;
		}
	}

	#endregion Setup Helpers

	#region Raise Helpers

	private static void DefineRaiseInterface(StringBuilder sb, Class @class, MemberType memberType)
	{
		Func<Event, bool> predicate = @event => @event.ExplicitImplementation is null &&
		                                        @event.MemberType == memberType;
		foreach (Event @event in @class.AllEvents().Where(predicate))
		{
			sb.AppendXmlSummary(
				$"Raise the <see cref=\"{@event.DeclaredContainingType.EscapeForXmlDoc()}.{@event.Name}\"/> event.");
			sb.Append("\t\tvoid ").Append(@event.Name).Append("(")
				.Append(FormatParametersWithTypeAndName(@event.Delegate.Parameters)).Append(");").AppendLine();
			sb.AppendLine();
		}

		foreach (Event @event in @class.AllEvents()
			         .Where(predicate)
			         .GroupBy(m => m.Name)
			         .Where(g => g.Count() == 1)
			         .Select(g => g.Single())
			         .Where(m => m.Delegate.Parameters.Count > 0))
		{
			sb.AppendXmlSummary(
				$"Raise the <see cref=\"{@event.DeclaredContainingType.EscapeForXmlDoc()}.{@event.Name}\"/> event.");
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
			sb.Append("\t\tvoid ").Append(raiseOnName).Append('.').Append(@event.Name).Append("(")
				.Append(FormatParametersWithTypeAndName(@event.Delegate.Parameters))
				.Append(")").AppendLine();
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
			         .Where(g => g.Count() == 1)
			         .Select(g => g.Single())
			         .Where(m => m.Delegate.Parameters.Count > 0))
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
					string local = $"__arg{idx}";
					sb.Append("\t\t\t").Append(p.Type.Fullname).Append(' ').Append(local).Append(" = ")
						.Append(defaultExpr).Append(";").AppendLine();
					argNames.Add($"{RefKindKeyword(p.RefKind)}{local}");
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
			   property.MemberType == memberType;
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			sb.AppendXmlSummary(
				$"Verify interactions with the {property.Type.Fullname.EscapeForXmlDoc()} property <see cref=\"{property.DeclaredContainingType.EscapeForXmlDoc()}.{property.Name}\" />.");
			sb.Append("\t\t").Append(GetPropertyVerifyType(property, verifyName)).Append(' ')
				.Append(property.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion

		#region Indexers

		Func<Property, bool> indexerPredicate =
			indexer => indexer.ExplicitImplementation is null && indexer is { IsIndexer: true, } &&
			           indexer.MemberType == memberType;
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(indexer.IndexerParameters!.Value))
				{
					AppendUnionIndexerVerifyDefinition(sb, indexer, verifyName, slots);
				}

				continue;
			}

			AppendIndexerVerifyDefinition(sb, indexer, verifyName,
				hasOverloadResolutionPriority: hasOverloadResolutionPriority);
			if (indexer.IndexerParameters!.Value.Count <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(indexer.IndexerParameters.Value))
				{
					AppendIndexerVerifyDefinition(sb, indexer, verifyName, valueFlags, hasOverloadResolutionPriority);
				}
			}
			else
			{
				bool[] allValueFlags = indexer.IndexerParameters.Value.Select(p => p.CanUseNullableParameterOverload())
					.ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendIndexerVerifyDefinition(sb, indexer, verifyName, allValueFlags,
						hasOverloadResolutionPriority);
				}
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(m => m.Name).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodVerifyDefinition(sb, method, verifyName, true,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodVerifyDefinition(sb, method, verifyName, false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters))
					{
						AppendUnionMethodVerifyDefinition(sb, @class, method, verifyName, slots);
					}
				}
				else
				{
					AppendMethodVerifyDefinition(sb, method, verifyName, false,
						hasOverloadResolutionPriority: hasOverloadResolutionPriority);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodVerifyDefinition(sb, method, verifyName, false,
							hasOverloadResolutionPriority: hasOverloadResolutionPriority, perElementParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodVerifyDefinition(sb, method, verifyName, false, valueFlags: valueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority);
						}
					}
					else
					{
						bool[] allValueFlags = method.Parameters.Select(p => p.CanUseNullableParameterOverload())
							.ToArray();
						if (allValueFlags.Any(f => f))
						{
							AppendMethodVerifyDefinition(sb, method, verifyName, false, valueFlags: allValueFlags,
								hasOverloadResolutionPriority: hasOverloadResolutionPriority);
						}
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
				$"Verify subscriptions on the {@event.Name} event of <see cref=\"{@event.DeclaredContainingType.EscapeForXmlDoc()}.{@event.Name}\" />.");
			sb.Append("\t\tglobal::Mockolate.Verify.VerificationEventResult<").Append(verifyName).Append("> ")
				.Append(@event.Name).Append(" { get; }").AppendLine();
			sb.AppendLine();
		}

		#endregion
	}

	private static void AppendMethodVerifyDefinition(StringBuilder sb, Method method, string verifyName,
		bool useParameters, string? methodNameOverride = null, bool[]? valueFlags = null,
		bool hasOverloadResolutionPriority = false, bool perElementParams = false)
	{
		// For methods with ref-struct parameters, skip Verify emission entirely. The
		// VerificationResult pipeline takes IParameter<T>? matchers that then feed into
		// closures over the captured call values — incompatible with a ref-struct T. Callers
		// fall back to reading `mock.Mock.Registry.Interactions.OfType<RefStructMethodInvocation>()`
		// for count-based verification. See brief's "verify count" guidance.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		string methodName = methodNameOverride ?? method.Name;
		sb.Append("\t\t/// <summary>").AppendLine();
		if (methodNameOverride is null)
		{
			sb.Append("\t\t///     Verify invocations for the method <see cref=\"").Append(MethodCref(method))
				.Append("\"/>");
		}
		else
		{
			sb.Append("\t\t///     Verify invocations for the delegate <see cref=\"")
				.Append(method.ContainingType.EscapeForXmlDoc()).Append("\"/>");
		}

		sb.Append(method.Parameters.Count > 0 ? " with the given " : "");
		if (useParameters)
		{
			sb.Append("<paramref name=\"parameters\"/>");
		}
		else
		{
			sb.Append(string.Join(", ", method.Parameters.Select(p => $"<paramref name=\"{p.Name}\"/>")));
		}

		sb.Append(".").AppendLine();
		sb.Append("\t\t/// </summary>").AppendLine();
		AppendOverloadDifferentiatorRemark(sb, method.Parameters.Select(p => p.Name).ToArray(), useParameters,
			valueFlags, true);
		if (valueFlags?.All(x => x) == true || (method.Parameters.Count == 0 && !useParameters))
		{
			if (hasOverloadResolutionPriority && method.Parameters.Count > 0 && valueFlags is not null &&
			    !ParametersBlockAllValuesPromotion(method.Parameters, valueFlags))
			{
				sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(int.MaxValue)]")
					.AppendLine();
			}

			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName)
				.Append(">.IgnoreParameters ").Append(methodName).Append("(");
		}
		else
		{
			if (hasOverloadResolutionPriority)
			{
				sb.Append("\t\t[global::System.Runtime.CompilerServices.OverloadResolutionPriority(")
					.Append(ComputeMethodOverloadPriority(useParameters, valueFlags, method.Parameters.Count))
					.Append(")]").AppendLine();
			}

			sb.Append("\t\tglobal::Mockolate.Verify.VerificationResult<").Append(verifyName).Append("> ")
				.Append(methodName).Append("(");
		}

		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters parameters");
		}
		else
		{
			ReadOnlySpan<MethodParameter> paramSpan = method.Parameters.AsSpan();
			bool[] hasTrailingDefault = ComputeTrailingDefaults(paramSpan, valueFlags);

			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(">[] ").Append(parameter.Name);
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

		sb.Append(")");
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t");
			}
		}

		sb.Append(";").AppendLine();
		sb.AppendLine();
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void ImplementVerifyInterface(StringBuilder sb, Class @class, string mockRegistryName,
		string verifyName, MemberType memberType, MemberIdTable memberIds, string memberIdPrefix,
		bool useFastBuffers = true, bool useUnionOverloads = false)
#pragma warning restore S107
	{
		#region Properties

		Func<Property, bool> propertyPredicate = property
			=> property.ExplicitImplementation is null && property is { IsIndexer: false, } &&
			   property.MemberType == memberType;
		foreach (Property property in @class.AllProperties().Where(propertyPredicate))
		{
			bool useFastForProperty = useFastBuffers && IsFastBufferEligibleProperty(property);
			string propertyGetMemberId = useFastForProperty
				? memberIdPrefix + memberIds.GetPropertyGetIdentifier(property)
				: "-1";
			string propertySetMemberId = useFastForProperty
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
			   indexer.MemberType == memberType;
		foreach (Property indexer in @class.AllProperties().Where(indexerPredicate))
		{
			if (UseUnionIndexer(@class, indexer, useUnionOverloads))
			{
				foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(indexer.IndexerParameters!.Value))
				{
					AppendUnionIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds,
						memberIdPrefix, useFastBuffers, slots);
				}

				continue;
			}

			AppendIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds, memberIdPrefix,
				useFastBuffers);
			if (indexer.IndexerParameters!.Value.Count <= MaxExplicitParameters)
			{
				foreach (bool[] valueFlags in GenerateValueFlagCombinations(indexer.IndexerParameters.Value))
				{
					AppendIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds,
						memberIdPrefix, useFastBuffers, valueFlags);
				}
			}
			else
			{
				bool[] allValueFlags = indexer.IndexerParameters.Value.Select(p => p.CanUseNullableParameterOverload())
					.ToArray();
				if (allValueFlags.Any(f => f))
				{
					AppendIndexerVerifyImplementation(sb, indexer, mockRegistryName, verifyName, memberIds,
						memberIdPrefix, useFastBuffers, allValueFlags);
				}
			}
		}

		#endregion

		#region Methods

		bool MethodPredicate(Method method)
		{
			return method.ExplicitImplementation is null && method.MemberType == memberType;
		}

		List<IGrouping<string, Method>> methodGroups =
			@class.AllMethods().Where((Func<Method, bool>)MethodPredicate).GroupBy(m => m.Name).ToList();
		foreach (IGrouping<string, Method>? methodGroup in methodGroups)
		{
			if (methodGroup.Count() == 1)
			{
				Method? method = methodGroup.Single();
				if (method.Parameters.Count > 0)
				{
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, true,
						memberIds, memberIdPrefix, useFastBuffers);
				}
			}

			foreach (Method? method in methodGroup)
			{
				if (method.Parameters.Count == 0)
				{
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, false,
						memberIds, memberIdPrefix, useFastBuffers);
				}
				else if (UseUnionOverloads(@class, method, useUnionOverloads))
				{
					foreach (UnionSlot[] slots in GenerateUnionSlotCombinations(method.Parameters))
					{
						AppendUnionMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, memberIds,
							memberIdPrefix, useFastBuffers, slots);
					}
				}
				else
				{
					AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, false,
						memberIds, memberIdPrefix, useFastBuffers);
					if (TryGetPerElementParamsParameter(method))
					{
						AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, false,
							memberIds, memberIdPrefix, useFastBuffers, perElementParams: true);
					}

					if (method.Parameters.Count <= MaxExplicitParameters)
					{
						foreach (bool[] valueFlags in GenerateValueFlagCombinations(method.Parameters))
						{
							AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, false,
								memberIds, memberIdPrefix, useFastBuffers, valueFlags: valueFlags);
						}
					}
					else
					{
						bool[] allValueFlags = method.Parameters.Select(p => p.CanUseNullableParameterOverload())
							.ToArray();
						if (allValueFlags.Any(f => f))
						{
							AppendMethodVerifyImplementation(sb, method, mockRegistryName, verifyName, false,
								memberIds, memberIdPrefix, useFastBuffers, valueFlags: allValueFlags);
						}
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
			bool useFastForEvent = useFastBuffers && IsFastBufferEligibleEvent(@event);
			string subMemberId = useFastForEvent
				? memberIdPrefix + memberIds.GetEventSubscribeIdentifier(@event)
				: "-1";
			string unsubMemberId = useFastForEvent
				? memberIdPrefix + memberIds.GetEventUnsubscribeIdentifier(@event)
				: "-1";
			sb.AppendXmlSummary(
				$"Verify subscriptions on the {@event.Name} event <see cref=\"{@event.DeclaredContainingType.EscapeForXmlDoc()}.{@event.Name}\" />.");
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
		bool useParameters, MemberIdTable memberIds, string memberIdPrefix, bool useFastBuffers,
		string? methodNameOverride = null, bool[]? valueFlags = null, bool perElementParams = false)
#pragma warning restore S107
	{
		// Mirror the AppendMethodVerifyDefinition short-circuit for ref-struct signatures.
		if (method.Parameters.Any(p => p.NeedsRefStructPipeline()))
		{
			return;
		}

		bool useFastForMethod = useFastBuffers && IsFastBufferEligibleMethod(method);
		string methodMemberId = useFastForMethod
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

		sb.Append(' ').Append(verifyName).Append('.').Append(methodName).Append("(");
		if (useParameters)
		{
			sb.Append("global::Mockolate.Parameters.IParameters parameters");
		}
		else
		{
			int i = 0;
			foreach (MethodParameter parameter in method.Parameters)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}

				bool isValueParam = valueFlags?[i] == true;
				if (perElementParams && parameter.IsParams &&
				    parameter.Type.ElementType is not null)
				{
					sb.Append("params global::Mockolate.Parameters.IParameter<")
						.Append(parameter.Type.ElementType.Fullname).Append(">[] ").Append(parameter.Name);
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

		sb.Append(")");
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
		{
			foreach (GenericParameter gp in method.GenericParameters.Value)
			{
				gp.AppendWhereConstraint(sb, "\t\t\t", true, true);
			}
		}

		sb.AppendLine();

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
		                         && useFastForMethod
		                         && !useParameters
		                         && method.Parameters.Count <= 4
		                         && (method.GenericParameters is null || method.GenericParameters.Value.Count == 0)
		                         && (valueFlags is null || !valueFlags.Any(x => x))
		                         && noRefStruct;

		if (canUseLiteralVerify)
		{
			// Bare-value verify path: every parameter is a literal value, so we can route through the
			// MockRegistry.VerifyMethod overload that takes the values directly (no IParameterMatch<T>
			// allocation) and uses Method{N}LiteralCountSource for the allocation-free fast path. The
			// memberId may be -1 here when fast buffers are off, which the runtime overload handles by
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

			sb.Append(", () => $\"").Append(method.Name).Append("(")
				.Append(string.Join(", ", method.Parameters.Select(p => $"{{{p.Name}}}"))).Append(")\");").AppendLine();
			return;
		}

		if (canUseTypedVerify)
		{
			sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName);
			foreach (MethodParameter parameter in method.Parameters)
			{
				sb.Append(", ").Append(parameter.ToTypeOrWrapper());
			}

			sb.Append(">(this, ").Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString());
			foreach (MethodParameter parameter in method.Parameters)
			{
				string paramType = parameter.ToTypeOrWrapper();
				sb.Append(", ").Append(parameter.Name).Append(" is null ? ")
					.Append("(global::Mockolate.Parameters.IParameterMatch<").Append(paramType)
					.Append(">)global::Mockolate.It.Is<").Append(paramType).Append(">(default!) : ")
					.Append("CovariantParameterAdapter<").Append(paramType).Append(">.Wrap(").Append(parameter.Name)
					.Append(")");
			}

			sb.Append(", () => $\"").Append(method.Name).Append("(")
				.Append(string.Join(", ", method.Parameters.Select(p => $"{{{p.Name}}}"))).Append(")\");").AppendLine();
			return;
		}

		sb.Append("\t\t\t=> this.").Append(mockRegistryName).Append(".VerifyMethod<").Append(verifyName)
			.Append(", global::Mockolate.Interactions.MethodInvocation");
		if (method.Parameters.Count > 0)
		{
			sb.Append("<").Append(string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper()))).Append(">");
		}

		sb.Append(">(this, ").Append(methodMemberId).Append(", ").Append(method.GetUniqueNameString());
		if (useParameters)
		{
			sb.Append(", __i => parameters switch").AppendLine();
			sb.Append("\t\t\t\t{").AppendLine();
			sb.Append("\t\t\t\t\tglobal::Mockolate.Parameters.IParametersMatch m => m.Matches([")
				.Append(string.Join(", ", Enumerable.Range(1, method.Parameters.Count).Select(i => $"__i.Parameter{i}")))
				.Append("]),").AppendLine();
			sb.Append("\t\t\t\t\tglobal::Mockolate.Parameters.INamedParametersMatch m => m.Matches([")
				.Append(string.Join(", ", method.Parameters.Select((p, i) => $"(\"{p.Name}\", __i.Parameter{i + 1})")))
				.Append("]),").AppendLine();
			sb.Append("\t\t\t\t\t_ => true").AppendLine();
			sb.Append("\t\t\t\t}");
		}
		else if (method.Parameters.Count == 0)
		{
			sb.Append(", __i => true");
		}
		else
		{
			sb.Append(", __i => ");

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
					sb.Append("(new global::Mockolate.Parameters.ParamsArrayParameterMatch<")
						.Append(parameter.Type.ElementType.Fullname).Append(">(").Append(parameter.Name)
						.Append(").Matches(__i.Parameter").Append(i + 1).Append("))");
				}
				else if (isValueParam)
				{
					sb.Append(
						parameter.IsParams && parameter.Type.ElementType is not null
							? $"(CovariantParameterAdapter<{parameter.Type.Fullname}>.Wrap(global::Mockolate.It.SequenceEquals<{parameter.Type.ElementType.Fullname}>({parameter.Name})).Matches(__i.Parameter{i + 1}))"
							: $"(global::System.Collections.Generic.EqualityComparer<{parameter.ToTypeOrWrapper()}>.Default.Equals({parameter.Name}, __i.Parameter{i + 1}))");
				}
				else
				{
					AppendSlowVerifyMatcherCondition(sb, parameter, $"__i.Parameter{i + 1}");
				}

				i++;
			}
		}

		sb.Append(", () => $\"").Append(method.Name).Append("(")
			.Append(useParameters ? "{parameters}" : string.Join(", ", method.Parameters.Select(p => $"{{{p.Name}}}")))
			.Append(")\");").AppendLine();
	}

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
			sb.Append(
				$"({parameter.Name} is global::Mockolate.Parameters.IParameterMatch<{type}> {parameter.Name}Match ? {parameter.Name}Match.Matches({invocationValue}) : global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals({invocationValue}, default({type})))");
		}
		else
		{
			sb.Append(
				$"({parameter.Name} is not null ? CovariantParameterAdapter<{type}>.Wrap({parameter.Name}).Matches({invocationValue}) : global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals({invocationValue}, default({type})))");
		}
	}

	#endregion Verify Helpers

	private static void AppendCreateFastInteractions(StringBuilder sb, string indent)
	{
		sb.Append(indent).Append("/// <summary>").AppendLine();
		sb.Append(indent).Append("///     Creates a <see cref=\"global::Mockolate.Interactions.FastMockInteractions\" /> sized to ").Append("<see cref=\"MemberCount\" /> for use as the mock's interaction store.").AppendLine();
		sb.Append(indent).Append("///     Per-member buffers are not allocated up-front: the recording hot paths call ").Append("<see cref=\"global::Mockolate.Interactions.FastMockInteractions.GetOrCreateBuffer{TBuffer}(int, global::System.Func{global::Mockolate.Interactions.FastMockInteractions, TBuffer})\" />").Append(" so a slot is materialized only when its member is first invoked.").AppendLine();
		sb.Append(indent).Append("/// </summary>").AppendLine();
		sb.Append(indent).Append("internal static global::Mockolate.Interactions.FastMockInteractions CreateFastInteractions(global::Mockolate.MockBehavior behavior)").AppendLine();
		sb.Append(indent).Append("\t=> new global::Mockolate.Interactions.FastMockInteractions(MemberCount, behavior.SkipInteractionRecording);").AppendLine();
	}

	/// <summary>
	///     Emits the declarations of the cached <c>MockolateSkipRecording</c> flag and per-member typed
	///     buffer fields. These mirror values that <c>AppendCreateFastInteractions</c> writes into the
	///     <c>FastMockInteractions.Buffers</c> array so the body emitters can read them as plain field
	///     accesses instead of paying the cast / array-index / property-chain on every invocation. Static
	///     members stay on the legacy path because the cached field would not flow through
	///     <c>AsyncLocal</c>-resolved registries.
	/// </summary>
	/// <returns>
	///     <see langword="true" /> when the class has at least one fast-eligible non-static indexer or
	///     method, in which case the generator emits cached typed-buffer fields and a
	///     <c>MockolateSkipRecording</c> flag that the recording hot paths read instead of paying the
	///     per-call cast / array-index / property-chain.
	/// </returns>
	private static bool HasCachedBufferFields(Class @class)
	{
		foreach (Property property in @class.AllProperties())
		{
			if (property.IsIndexer && IsFastBufferEligibleIndexer(property))
			{
				return true;
			}
		}

		foreach (Method method in @class.AllMethods())
		{
			if (!method.IsStatic && IsFastBufferEligibleMethod(method))
			{
				return true;
			}
		}

		return false;
	}

	private static void AppendCachedFieldDeclarations(StringBuilder sb, string indent, Class @class,
		MemberIdTable memberIds, string memberIdPrefix, string mockRegistryName)
	{
		if (!HasCachedBufferFields(@class))
		{
			return;
		}

		string mockRegistryRef = "this." + mockRegistryName;

		foreach (Property indexer in @class.AllProperties().Where(p => p.IsIndexer))
		{
			if (!IsFastBufferEligibleIndexer(indexer))
			{
				continue;
			}

			string indexerKeyTypeArgs =
				string.Join(", ", indexer.IndexerParameters!.Value.Select(p => p.ToTypeOrWrapper()));
			string indexerValueType = indexer.Type.ToTypeOrWrapper();
			string getMemberIdRef = memberIdPrefix + memberIds.GetIndexerGetIdentifier(indexer);
			string setMemberIdRef = memberIdPrefix + memberIds.GetIndexerSetIdentifier(indexer);
			string getterBufferType = "global::Mockolate.Interactions.FastIndexerGetterBuffer<"
			                          + indexerKeyTypeArgs + ">";
			string setterBufferType = "global::Mockolate.Interactions.FastIndexerSetterBuffer<"
			                          + indexerKeyTypeArgs + ", " + indexerValueType + ">";

			sb.Append(indent).Append("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append(indent).Append("private ").Append(getterBufferType).Append(' ').Append(memberIds.GetIndexerGetterBufferFieldName(indexer)).AppendLine();
			sb.Append(indent).Append("\t=> field ?? (field = ((global::Mockolate.Interactions.FastMockInteractions)")
				.Append(mockRegistryRef).Append(".Interactions).GetOrCreateBuffer<").Append(getterBufferType)
				.Append(">(").Append(getMemberIdRef).Append(", static fast => new ").Append(getterBufferType)
				.Append("(fast)));").AppendLine();

			sb.Append(indent).Append("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append(indent).Append("private ").Append(setterBufferType).Append(' ').Append(memberIds.GetIndexerSetterBufferFieldName(indexer)).AppendLine();
			sb.Append(indent).Append("\t=> field ?? (field = ((global::Mockolate.Interactions.FastMockInteractions)")
				.Append(mockRegistryRef).Append(".Interactions).GetOrCreateBuffer<").Append(setterBufferType)
				.Append(">(").Append(setMemberIdRef).Append(", static fast => new ").Append(setterBufferType)
				.Append("(fast)));").AppendLine();
		}

		foreach (Method method in @class.AllMethods())
		{
			if (method.IsStatic || !IsFastBufferEligibleMethod(method))
			{
				continue;
			}

			int arity = method.Parameters.Count;
			string typeArgs = arity == 0
				? string.Empty
				: "<" + string.Join(", ", method.Parameters.Select(p => p.ToTypeOrWrapper())) + ">";
			string memberIdRef = memberIdPrefix + memberIds.GetMethodIdentifier(method);
			string bufferType = "global::Mockolate.Interactions.FastMethod" + arity + "Buffer" + typeArgs;
			sb.Append(indent).Append("[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]").AppendLine();
			sb.Append(indent).Append("private ").Append(bufferType).Append(' ').Append(memberIds.GetMethodBufferFieldName(method)).AppendLine();
			sb.Append(indent).Append("\t=> field ?? (field = ((global::Mockolate.Interactions.FastMockInteractions)")
				.Append(mockRegistryRef).Append(".Interactions).GetOrCreateBuffer<").Append(bufferType)
				.Append(">(").Append(memberIdRef).Append(", static fast => new ").Append(bufferType)
				.Append("(fast)));").AppendLine();
		}
	}

	/// <summary>
	///     Properties get a typed per-member buffer when they are not static. Static property recordings stay on
	///     the legacy <c>RegisterInteraction</c> path because their member id is shared across <c>AsyncLocal</c>
	///     contexts whereas the buffer instance is stored on a single registry, so per-context isolation breaks.
	/// </summary>
	private static bool IsFastBufferEligibleProperty(Property property)
		=> !property.IsStatic;

	/// <summary>
	///     Indexers with up to four key parameters and a non-ref-struct signature get a typed per-member buffer.
	/// </summary>
	private static bool IsFastBufferEligibleIndexer(Property indexer)
	{
		if (indexer.IsStatic ||
		    indexer.IndexerParameters is null ||
		    indexer.IndexerParameters.Value.Count == 0 ||
		    indexer.IndexerParameters.Value.Count > 4)
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
	///     Events get a typed per-member buffer when they are not static (see <see cref="IsFastBufferEligibleProperty" />
	///     for the rationale).
	/// </summary>
	private static bool IsFastBufferEligibleEvent(Event @event)
		=> !@event.IsStatic;

	/// <summary>
	///     Methods with non-generic, non-ref-struct signatures get a typed per-member buffer; everything
	///     else (open generics, ref-struct params) records via the legacy <c>RegisterInteraction</c> fallback.
	/// </summary>
	private static bool IsFastBufferEligibleMethod(Method method)
	{
		if (method.GenericParameters is not null && method.GenericParameters.Value.Count > 0)
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
	///     <c>class</c>, <c>class?</c>, an interface, or <c>notnull</c>) cannot be expressed in
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
			if (gp.Name == raw)
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

	private static void AppendTypedCreateMockOverloads(StringBuilder sb, Class @class,
		EquatableArray<Method> constructors, string setupType, string escapedClassName, string createMockReturns)
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

			if (constructor.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams))
			{
				continue;
			}

			string mockBehaviorName = CreateUniqueParameterName(constructor.Parameters, "mockBehavior");
			string setupName = CreateUniqueParameterName(constructor.Parameters, "setup");
			string baseSig = string.Join("|",
				constructor.Parameters.Select(p => p.Type.Fullname));

			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, escapedClassName, createMockReturns,
				false, false, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, escapedClassName, createMockReturns,
				true, false, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, escapedClassName, createMockReturns,
				false, true, mockBehaviorName, setupName, baseSig,
				emittedSignatures);
			TryEmitTypedCreateMockOverload(sb, @class, constructor, setupType, escapedClassName, createMockReturns,
				true, true, mockBehaviorName, setupName, baseSig,
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

		StringBuilder cref = new();
		StringBuilder display = new();
		cref.Append(fullName).Append('(');
		display.Append(simpleName).Append('(');
		bool first = true;
		foreach (MethodParameter parameter in constructor.Parameters)
		{
			if (!first)
			{
				cref.Append(", ");
				display.Append(", ");
			}

			first = false;
			cref.Append(parameter.Type.Fullname.EscapeForXmlDoc());
			// Inner text of <see> is XML content, so escape '<'/'>' as entities
			// (unlike cref attributes, which use the '{...}' shorthand).
			display.Append(parameter.Type.DisplayName.Replace("<", "&lt;").Replace(">", "&gt;"));
		}

		cref.Append(')');
		display.Append(')');
		return (cref.ToString(), display.ToString());
	}

#pragma warning disable S107 // Methods should not have too many parameters
	private static void TryEmitTypedCreateMockOverload(StringBuilder sb, Class @class, Method constructor,
		string setupType, string escapedClassName, string createMockReturns,
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
			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given <paramref name=\"{mockBehaviorName}\" />, applying the given <paramref name=\"{setupName}\" /> immediately, using the given constructor parameters to invoke {ctorPhrase}.");
			sb.AppendXmlRemarks($"The provided <paramref name=\"{setupName}\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		}
		else if (includeMockBehavior)
		{
			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given <paramref name=\"{mockBehaviorName}\" /> and the given constructor parameters to invoke {ctorPhrase}.");
		}
		else if (includeSetup)
		{
			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> applying the given <paramref name=\"{setupName}\" /> immediately, using the given constructor parameters to invoke {ctorPhrase}.");
			sb.AppendXmlRemarks($"The provided <paramref name=\"{setupName}\" /> is immediately applied to the mock. Use this overload when you want setups to cover virtual interactions triggered inside the constructor.");
		}
		else
		{
			sb.AppendXmlSummary($"Creates a new mock of <see cref=\"{escapedClassName}\" /> using the given constructor parameters to invoke {ctorPhrase}.");
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
			sb.AppendXmlParam(parameter.Name, "Value forwarded to the base-class constructor.");
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

		sb.Append(")").AppendLine();
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
