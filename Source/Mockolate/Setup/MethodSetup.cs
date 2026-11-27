using System;
using Mockolate.Interactions;
using Mockolate.Internals;

namespace Mockolate.Setup;

/// <summary>
///     Base class for method setups.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public abstract class MethodSetup : IMethodSetup, IVerifiableMethodSetup
{
	/// <summary>
	///     Base class for method setups.
	/// </summary>
	protected MethodSetup(string name)
	{
		Name = name;
	}

	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; }

	internal bool HasName(string methodName, string? alternativeMethodName)
		=> Name.Equals(methodName, StringComparison.Ordinal) || (alternativeMethodName is not null && Name.Equals(alternativeMethodName, StringComparison.Ordinal));

	/// <summary>
	///     Returns a formatted string representation of the given <paramref name="type" />.
	/// </summary>
	protected static string FormatType(Type type)
		=> type.FormatType();

	/// <summary>
	///     Renders <paramref name="value" /> for inclusion in a setup's <see cref="object.ToString" />, like every
	///     argument value in failure messages: <see cref="ValueFormatter.Format{T}(T)" /> quotes strings and formats
	///     independent of the current culture.
	/// </summary>
	protected static string FormatLiteralValue<T>(T value)
		=> ValueFormatter.Format(value);

	/// <inheritdoc cref="IVerifiableMethodSetup.Matches(IMethodInteraction)" />
	bool IVerifiableMethodSetup.Matches(IMethodInteraction interaction)
		=> MatchesInteraction(interaction);

	/// <summary>
	///     Checks if the setup matches the method invocations.
	/// </summary>
	protected abstract bool MatchesInteraction(IMethodInteraction interaction);
}
