using System.Diagnostics;
using Mockolate.Internals;
using Mockolate.Setup;

namespace Mockolate.Interactions;

/// <summary>
///     An access of an indexer getter with a single typed parameter.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerGetterAccess<T1>(T1 parameter1) : IndexerAccess
{
	/// <summary>
	///     The single parameter value of the indexer.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <inheritdoc cref="IndexerAccess.ParameterCount" />
	public override int ParameterCount => 1;

	/// <inheritdoc cref="IndexerAccess.GetParameterValueAt(int)" />
	public override object? GetParameterValueAt(int index)
		=> index switch
		{
			0 => Parameter1,
			_ => null,
		};

	/// <inheritdoc cref="IndexerAccess.TraverseStorage(Mockolate.Internals.IndexerValueStorage?, bool)" />
	protected override IndexerValueStorage? TraverseStorage(IndexerValueStorage? storage, bool createMissing)
	{
		IndexerValueStorage? s = storage;
		if (s is null)
		{
			return null;
		}

		return createMissing ? s.GetOrAddChildDispatch(Parameter1) : s.GetChildDispatch(Parameter1);
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"get indexer [{Parameter1?.ToString() ?? "null"}]";
}

/// <summary>
///     An access of an indexer getter with two typed parameters.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerGetterAccess<T1, T2>(T1 parameter1, T2 parameter2) : IndexerAccess
{
	/// <summary>
	///     The first parameter value of the indexer.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the indexer.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <inheritdoc cref="IndexerAccess.ParameterCount" />
	public override int ParameterCount => 2;

	/// <inheritdoc cref="IndexerAccess.GetParameterValueAt(int)" />
	public override object? GetParameterValueAt(int index)
		=> index switch
		{
			0 => Parameter1,
			1 => Parameter2,
			_ => null,
		};

	/// <inheritdoc cref="IndexerAccess.TraverseStorage(Mockolate.Internals.IndexerValueStorage?, bool)" />
	protected override IndexerValueStorage? TraverseStorage(IndexerValueStorage? storage, bool createMissing)
	{
		IndexerValueStorage? s = storage;
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter1) : s.GetChildDispatch(Parameter1);
		if (s is null)
		{
			return null;
		}

		return createMissing ? s.GetOrAddChildDispatch(Parameter2) : s.GetChildDispatch(Parameter2);
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"get indexer [{Parameter1?.ToString() ?? "null"}, {Parameter2?.ToString() ?? "null"}]";
}

/// <summary>
///     An access of an indexer getter with three typed parameters.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerGetterAccess<T1, T2, T3>(T1 parameter1, T2 parameter2, T3 parameter3) : IndexerAccess
{
	/// <summary>
	///     The first parameter value of the indexer.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the indexer.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <summary>
	///     The third parameter value of the indexer.
	/// </summary>
	public T3 Parameter3 { get; } = parameter3;

	/// <inheritdoc cref="IndexerAccess.ParameterCount" />
	public override int ParameterCount => 3;

	/// <inheritdoc cref="IndexerAccess.GetParameterValueAt(int)" />
	public override object? GetParameterValueAt(int index)
		=> index switch
		{
			0 => Parameter1,
			1 => Parameter2,
			2 => Parameter3,
			_ => null,
		};

	/// <inheritdoc cref="IndexerAccess.TraverseStorage(Mockolate.Internals.IndexerValueStorage?, bool)" />
	protected override IndexerValueStorage? TraverseStorage(IndexerValueStorage? storage, bool createMissing)
	{
		IndexerValueStorage? s = storage;
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter1) : s.GetChildDispatch(Parameter1);
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter2) : s.GetChildDispatch(Parameter2);
		if (s is null)
		{
			return null;
		}

		return createMissing ? s.GetOrAddChildDispatch(Parameter3) : s.GetChildDispatch(Parameter3);
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"get indexer [{Parameter1?.ToString() ?? "null"}, {Parameter2?.ToString() ?? "null"}, {Parameter3?.ToString() ?? "null"}]";
}

/// <summary>
///     An access of an indexer getter with four typed parameters.
/// </summary>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class IndexerGetterAccess<T1, T2, T3, T4>(T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4) : IndexerAccess
{
	/// <summary>
	///     The first parameter value of the indexer.
	/// </summary>
	public T1 Parameter1 { get; } = parameter1;

	/// <summary>
	///     The second parameter value of the indexer.
	/// </summary>
	public T2 Parameter2 { get; } = parameter2;

	/// <summary>
	///     The third parameter value of the indexer.
	/// </summary>
	public T3 Parameter3 { get; } = parameter3;

	/// <summary>
	///     The fourth parameter value of the indexer.
	/// </summary>
	public T4 Parameter4 { get; } = parameter4;

	/// <inheritdoc cref="IndexerAccess.ParameterCount" />
	public override int ParameterCount => 4;

	/// <inheritdoc cref="IndexerAccess.GetParameterValueAt(int)" />
	public override object? GetParameterValueAt(int index)
		=> index switch
		{
			0 => Parameter1,
			1 => Parameter2,
			2 => Parameter3,
			3 => Parameter4,
			_ => null,
		};

	/// <inheritdoc cref="IndexerAccess.TraverseStorage(Mockolate.Internals.IndexerValueStorage?, bool)" />
	protected override IndexerValueStorage? TraverseStorage(IndexerValueStorage? storage, bool createMissing)
	{
		IndexerValueStorage? s = storage;
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter1) : s.GetChildDispatch(Parameter1);
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter2) : s.GetChildDispatch(Parameter2);
		if (s is null)
		{
			return null;
		}

		s = createMissing ? s.GetOrAddChildDispatch(Parameter3) : s.GetChildDispatch(Parameter3);
		if (s is null)
		{
			return null;
		}

		return createMissing ? s.GetOrAddChildDispatch(Parameter4) : s.GetChildDispatch(Parameter4);
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"get indexer [{Parameter1?.ToString() ?? "null"}, {Parameter2?.ToString() ?? "null"}, {Parameter3?.ToString() ?? "null"}, {Parameter4?.ToString() ?? "null"}]";
}
