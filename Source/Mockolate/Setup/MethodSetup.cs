using System;
using System.Globalization;
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

	/// <summary>
	///     Returns a formatted string representation of the given <paramref name="type" />.
	/// </summary>
	protected static string FormatType(Type type)
		=> type.FormatType();

	/// <summary>
	///     Renders <paramref name="value" /> for inclusion in a setup's <see cref="object.ToString" />,
	///     mirroring the formatting that the <c>Matchers.IsValue&lt;T&gt;</c> matcher uses for diagnostics:
	///     strings are quoted, <see cref="IFormattable" /> values are rendered with
	///     <see cref="CultureInfo.InvariantCulture" /> (so failure messages don't drift with the host
	///     locale), and everything else falls through to <see cref="object.ToString" />.
	/// </summary>
	protected static string FormatLiteralValue<T>(T value)
	{
		if (value is null)
		{
			return "null";
		}

		if (value is string s)
		{
			return $"\"{s}\"";
		}

		if (value is IFormattable formattable)
		{
			return formattable.ToString(null, CultureInfo.InvariantCulture);
		}

		return value.ToString() ?? "null";
	}

	/// <inheritdoc cref="IVerifiableMethodSetup.Matches(IMethodInteraction)" />
	bool IVerifiableMethodSetup.Matches(IMethodInteraction interaction)
		=> MatchesInteraction(interaction);

	/// <summary>
	///     Checks if the setup matches the method invocations.
	/// </summary>
	protected abstract bool MatchesInteraction(IMethodInteraction interaction);
}
