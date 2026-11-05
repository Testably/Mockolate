using System.Text;
using Mockolate.SourceGenerators.Entities;
using Event = Mockolate.SourceGenerators.Entities.Event;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     Emits a member-id-keyed lookup of the latest matching <paramref name="methodSetupType" /> setup,
	///     with a slow-path fallback for cases the fast path cannot cover.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Fast path (default scope only): walks the lock-free <c>GetMethodSetupSnapshot</c> array in
	///         reverse and runs <c>Matches({matchArgs})</c> directly. Eliminates the per-call closure
	///         allocation, the string-name comparison, and the lock taken by the legacy
	///         <c>MethodSetups.GetMatching</c> walk.
	///     </para>
	///     <para>
	///         Slow-path fallback: invoked when an active scenario is set or when the fast path finds no
	///         match. The member-id array is only populated by the <c>SetupMethod(int, ...)</c> overloads
	///         emitted by the generator; setups registered through the hand-written
	///         <c>SetupMethod(MethodSetup)</c> overload (e.g. the <c>HttpClientExtensions.SetupMethod</c>
	///         pipeline) live only in the string-keyed list, so the fallback also covers them.
	///     </para>
	/// </remarks>
#pragma warning disable S107 // Methods should not have too many parameters
	internal static void EmitFastMethodSetupLookup(StringBuilder sb, string indent, string mockRegistry,
		string methodSetup, string methodSetupType, string memberIdRef, string uniqueNameString, string matchArgs,
		bool isGeneric, EquatableArray<MethodParameter> parameters)
#pragma warning restore S107
	{
		// All loop-locals carry the `methodSetup` suffix so they stay distinct across nested scopes.
		string snapshotVar = Helpers.GetUniqueLocalVariableName($"snapshot_{methodSetup}", parameters);
		string nameVar = Helpers.GetUniqueLocalVariableName($"name_{methodSetup}", parameters);
		string indexVar = Helpers.GetUniqueLocalVariableName($"i_{methodSetup}", parameters);
		string itemVar = Helpers.GetUniqueLocalVariableName($"s_{methodSetup}", parameters);

		sb.Append(indent).Append(methodSetupType).Append("? ").Append(methodSetup).Append(" = null;").AppendLine();
		sb.Append(indent).Append("if (string.IsNullOrEmpty(").Append(mockRegistry).Append(".Scenario))").AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		sb.Append(indent).Append("\tglobal::Mockolate.Setup.MethodSetup[]? ").Append(snapshotVar)
			.Append(" = ").Append(mockRegistry).Append(".GetMethodSetupSnapshot(").Append(memberIdRef).Append(");")
			.AppendLine();
		if (isGeneric)
		{
			// A single member id covers every instantiation of a generic method, so the snapshot
			// can hold setups whose closed-generic Name differs from this call site's instantiation.
			// Pre-compute the runtime name and filter the bucket by setup.Name to keep lookups
			// instantiation-scoped.
			sb.Append(indent).Append("\tstring ").Append(nameVar).Append(" = ")
				.Append(uniqueNameString).Append(';').AppendLine();
		}

		sb.Append(indent).Append("\tif (").Append(snapshotVar).Append(" is not null)").AppendLine();
		sb.Append(indent).Append("\t{").AppendLine();
		sb.Append(indent).Append("\t\tfor (int ").Append(indexVar).Append(" = ").Append(snapshotVar)
			.Append(".Length - 1; ").Append(indexVar).Append(" >= 0; ").Append(indexVar).Append("--)").AppendLine();
		sb.Append(indent).Append("\t\t{").AppendLine();
		sb.Append(indent).Append("\t\t\tif (").Append(snapshotVar).Append('[').Append(indexVar)
			.Append("] is ").Append(methodSetupType).Append(' ').Append(itemVar);
		if (isGeneric)
		{
			sb.Append(" && ").Append(itemVar).Append(".Name == ").Append(nameVar);
		}

		sb.Append(" && ").Append(itemVar).Append(".Matches(").Append(matchArgs).Append("))")
			.AppendLine();
		sb.Append(indent).Append("\t\t\t{").AppendLine();
		sb.Append(indent).Append("\t\t\t\t").Append(methodSetup).Append(" = ").Append(itemVar)
			.Append(';').AppendLine();
		sb.Append(indent).Append("\t\t\t\tbreak;").AppendLine();
		sb.Append(indent).Append("\t\t\t}").AppendLine();
		sb.Append(indent).Append("\t\t}").AppendLine();
		sb.Append(indent).Append("\t}").AppendLine();
		sb.Append(indent).Append('}').AppendLine();
		sb.Append(indent).Append("if (").Append(methodSetup).Append(" is null)").AppendLine();
		sb.Append(indent).Append('{').AppendLine();
		sb.Append(indent).Append("\tforeach (").Append(methodSetupType).Append(' ').Append(itemVar).Append(" in ").Append(mockRegistry)
			.Append(".GetMethodSetups<").Append(methodSetupType).Append(">(").Append(uniqueNameString).Append("))")
			.AppendLine();
		sb.Append(indent).Append("\t{").AppendLine();
		sb.Append(indent).Append("\t\tif (").Append(itemVar).Append(".Matches(").Append(matchArgs).Append("))").AppendLine();
		sb.Append(indent).Append("\t\t{").AppendLine();
		sb.Append(indent).Append("\t\t\t").Append(methodSetup).Append(" = ").Append(itemVar).Append(';').AppendLine();
		sb.Append(indent).Append("\t\t\tbreak;").AppendLine();
		sb.Append(indent).Append("\t\t}").AppendLine();
		sb.Append(indent).Append("\t}").AppendLine();
		sb.Append(indent).Append('}').AppendLine();
	}

	internal static MemberIdTable ComputeMemberIds(params Class[] classes)
		=> ComputeMemberIds(new MemberAliases(), classes);

	/// <remarks>
	///     <paramref name="classes" /> must start with the mocked class, so an aliased member's target
	///     already has an id by the time the additional interface is processed.
	/// </remarks>
	internal static MemberIdTable ComputeMemberIds(MemberAliases aliases, params Class[] classes)
	{
		MemberIdTable table = new(aliases);
		foreach (Class @class in classes)
		{
			AddMissing(@class.AllProperties().Where(p => !p.IsIndexer), table.PropertyGetIds, table.AddProperty,
				table.IsAlias);
			AddMissing(@class.AllEvents(), table.EventSubscribeIds, table.AddEvent, table.IsAlias);
			AddMissing(@class.AllProperties().Where(p => p.IsIndexer), table.IndexerGetIds, table.AddIndexer,
				table.IsAlias);
			AddMissing(@class.AllMethods(), table.MethodIds, table.AddMethod, table.IsAlias);
		}

		return table;
	}

	private static void AddMissing<T>(IEnumerable<T> items, Dictionary<T, int> existing, Action<T> add,
		Func<T, bool> isAlias)
		where T : notnull
	{
		foreach (T item in items)
		{
			if (!existing.ContainsKey(item) && !isAlias(item))
			{
				add(item);
			}
		}
	}

	/// <summary>
	///     Per-member id assignments for a generated mock class. The ids are compile-time constants
	///     emitted into the proxy class body; setup registrations and proxy method bodies look up
	///     setups by id instead of by string name for fast dispatch.
	/// </summary>
	/// <remarks>
	///     Ids are assigned in a deterministic order: properties (get + set), events (subscribe +
	///     unsubscribe), indexers (get + set per signature), methods (overloaded methods receive
	///     distinct ids). The same member always receives the same id across regenerations of a
	///     given class shape.
	/// </remarks>
	internal sealed class MemberIdTable
	{
		private readonly MemberAliases _aliases;
		private readonly List<string> _declarations = new();
		private readonly List<(Property Property, string FieldName)> _propertyGetterAccessFields = new();
		private readonly Dictionary<string, int> _usedIdentifiers = new();

		internal MemberIdTable(MemberAliases aliases)
		{
			_aliases = aliases;
		}

		/// <summary>
		///     True when the member is an additional interface member that the mocked class already
		///     implements, and whose slot is therefore served by the mock's own override.
		/// </summary>
		internal bool IsAlias(Method method) => _aliases.IsAlias(method);

		internal bool IsAlias(Property property) => _aliases.IsAlias(property);

		internal bool IsAlias(Event @event) => _aliases.IsAlias(@event);

		internal Dictionary<Method, int> MethodIds { get; } = new();
		internal Dictionary<Property, int> PropertyGetIds { get; } = new();
		internal Dictionary<Property, int> PropertySetIds { get; } = new();
		internal Dictionary<Property, string> PropertyGetterAccessFieldNames { get; } = new();
		internal Dictionary<Property, int> IndexerGetIds { get; } = new();
		internal Dictionary<Property, int> IndexerSetIds { get; } = new();
		internal Dictionary<Event, int> EventSubscribeIds { get; } = new();
		internal Dictionary<Event, int> EventUnsubscribeIds { get; } = new();

		internal int Count => _declarations.Count;

		internal string GetMethodIdentifier(Method method)
			=> _declarations[MethodIds[_aliases.Resolve(method)]];

		internal string GetPropertyGetIdentifier(Property property)
			=> _declarations[PropertyGetIds[_aliases.Resolve(property)]];

		internal string GetPropertySetIdentifier(Property property)
			=> _declarations[PropertySetIds[_aliases.Resolve(property)]];

		internal string GetIndexerGetIdentifier(Property indexer)
			=> _declarations[IndexerGetIds[_aliases.Resolve(indexer)]];

		internal string GetIndexerSetIdentifier(Property indexer)
			=> _declarations[IndexerSetIds[_aliases.Resolve(indexer)]];

		internal string GetEventSubscribeIdentifier(Event @event)
			=> _declarations[EventSubscribeIds[_aliases.Resolve(@event)]];

		internal string GetEventUnsubscribeIdentifier(Event @event)
			=> _declarations[EventUnsubscribeIds[_aliases.Resolve(@event)]];

		/// <summary>
		///     Returns the cached typed-buffer field name for the indexer's getter slot.
		/// </summary>
		internal string GetIndexerGetterBufferFieldName(Property indexer)
			=> ToBufferFieldName(_declarations[IndexerGetIds[_aliases.Resolve(indexer)]]);

		/// <summary>
		///     Returns the cached typed-buffer field name for the indexer's setter slot.
		/// </summary>
		internal string GetIndexerSetterBufferFieldName(Property indexer)
			=> ToBufferFieldName(_declarations[IndexerSetIds[_aliases.Resolve(indexer)]]);

		/// <summary>
		///     Returns the cached typed-buffer field name for the method's slot.
		/// </summary>
		internal string GetMethodBufferFieldName(Method method)
			=> ToBufferFieldName(_declarations[MethodIds[_aliases.Resolve(method)]]);

		private static string ToBufferFieldName(string identifier)
		{
			const string Prefix = "MemberId_";
			string suffix = identifier.StartsWith(Prefix, StringComparison.Ordinal)
				? identifier.Substring(Prefix.Length)
				: identifier;
			return "MockolateBuffer_" + suffix;
		}

		private int AllocateId(string identifier)
		{
			int id = _declarations.Count;
			_declarations.Add(identifier);
			return id;
		}

		private string UniqueIdentifier(string baseIdentifier)
		{
			string sanitized = Sanitize(baseIdentifier);
			if (!_usedIdentifiers.TryGetValue(sanitized, out int count))
			{
				_usedIdentifiers[sanitized] = 1;
				return sanitized;
			}

			_usedIdentifiers[sanitized] = count + 1;
			return sanitized + "_" + (count + 1);
		}

		internal void AddProperty(Property property)
		{
			string identifierGet = UniqueIdentifier(property.Name + "_Get");
			int getId = AllocateId("MemberId_" + identifierGet);
			PropertyGetIds[property] = getId;

			// PropertyGetterAccess is identified solely by Name, so the generator emits one
			// static readonly per-property singleton next to MemberId_<X>_Get and reuses it for
			// every recorded access. Sharing one reference is safe because the only two
			// reference-keyed verification paths in the codebase tolerate it: the
			// FastMockInteractions._verified filter is all-or-nothing per matched property
			// (every recording of a getter shares the same predicate result), and
			// VerificationResultExtensions.Then walks the snapshot positionally so repeated
			// occurrences of the same reference still resolve to distinct positions.
			string accessFieldName = "PropertyAccess_" + identifierGet;
			PropertyGetterAccessFieldNames[property] = accessFieldName;
			_propertyGetterAccessFields.Add((property, accessFieldName));

			string identifierSet = UniqueIdentifier(property.Name + "_Set");
			int setId = AllocateId("MemberId_" + identifierSet);
			PropertySetIds[property] = setId;
		}

		internal string GetPropertyGetterAccessFieldName(Property property)
			=> PropertyGetterAccessFieldNames[_aliases.Resolve(property)];

		internal void AddIndexer(Property indexer)
		{
			string keySignature = BuildIndexerSignatureSuffix(indexer);
			string identifierGet = UniqueIdentifier("Indexer" + keySignature + "_Get");
			int getId = AllocateId("MemberId_" + identifierGet);
			IndexerGetIds[indexer] = getId;

			string identifierSet = UniqueIdentifier("Indexer" + keySignature + "_Set");
			int setId = AllocateId("MemberId_" + identifierSet);
			IndexerSetIds[indexer] = setId;
		}

		internal void AddEvent(Event @event)
		{
			string identifierSub = UniqueIdentifier(@event.Name + "_Subscribe");
			int subId = AllocateId("MemberId_" + identifierSub);
			EventSubscribeIds[@event] = subId;

			string identifierUnsub = UniqueIdentifier(@event.Name + "_Unsubscribe");
			int unsubId = AllocateId("MemberId_" + identifierUnsub);
			EventUnsubscribeIds[@event] = unsubId;
		}

		internal void AddMethod(Method method)
		{
			string identifier = UniqueIdentifier(method.Name);
			int id = AllocateId("MemberId_" + identifier);
			MethodIds[method] = id;
		}

		internal void Emit(StringBuilder sb, string indent)
		{
			if (_declarations.Count == 0)
			{
				sb.Append(indent).Append("internal const int MemberCount = 0;").AppendLine();
				return;
			}

			for (int i = 0; i < _declarations.Count; i++)
			{
				sb.Append(indent).Append("internal const int ").Append(_declarations[i])
					.Append(" = ").Append(i).Append(';').AppendLine();
			}

			sb.Append(indent).Append("internal const int MemberCount = ").Append(_declarations.Count)
				.Append(';').AppendLine();

			foreach ((Property property, string fieldName) in _propertyGetterAccessFields)
			{
				sb.Append(indent)
					.Append(
						"internal static readonly global::Mockolate.Interactions.PropertyGetterAccess ")
					.Append(fieldName)
					.Append(" = new global::Mockolate.Interactions.PropertyGetterAccess(")
					.Append(property.GetUniqueNameString()).Append(");").AppendLine();
			}
		}

		private static string BuildIndexerSignatureSuffix(Property indexer)
		{
			if (indexer.IndexerParameters is null || indexer.IndexerParameters.Value.Count == 0)
			{
				return string.Empty;
			}

			StringBuilder sb = new();
			foreach (MethodParameter parameter in indexer.IndexerParameters.Value)
			{
				sb.Append('_').Append(Sanitize(parameter.Type.Fullname));
			}

			return sb.ToString();
		}

		private static string Sanitize(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "_";
			}

			StringBuilder sb = new(value.Length);
			foreach (char c in value)
			{
				if (char.IsLetterOrDigit(c))
				{
					sb.Append(c);
				}
				else if (c == '_')
				{
					sb.Append('_');
				}
				else
				{
					sb.Append('_');
				}
			}

			if (sb.Length == 0 || char.IsDigit(sb[0]))
			{
				sb.Insert(0, '_');
			}

			return sb.ToString();
		}
	}
}
