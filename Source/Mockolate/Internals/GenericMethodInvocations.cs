using System.ComponentModel;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Recognizes the recorded calls of a generic method for any type argument.
/// </summary>
/// <remarks>
///     The name of the open method marks each type parameter with a comma, e.g. <c>Ns.IFoo.Send&lt;,&gt;</c> for two
///     type parameters. A call is recorded with the type arguments in their place, e.g.
///     <c>Ns.IFoo.Send&lt;System.String, System.Int32&gt;</c>, so its name starts with the name of the open method up to
///     the last <c>&lt;</c>, as the type arguments of a generic containing type precede it, and lists as many type
///     arguments.
///     <para />
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenericMethodInvocations
{
	/// <summary>
	///     Checks whether <paramref name="interaction" /> is a call of the open generic method
	///     <paramref name="genericMethodName" /> with <paramref name="parameterCount" /> parameters.
	/// </summary>
	public static bool TryGetInvocation(IInteraction interaction, string genericMethodName, int parameterCount,
		out IMethodInvocationParameters invocation)
	{
		if (interaction is IMethodInteraction method && IsCallOf(method.Name, genericMethodName) &&
		    interaction is IMethodInvocationParameters parameters && parameters.ParameterCount == parameterCount)
		{
			invocation = parameters;
			return true;
		}

		invocation = null!;
		return false;
	}

	private static bool IsCallOf(string name, string genericMethodName)
	{
		int prefixLength = genericMethodName.LastIndexOf('<') + 1;
		if (name.Length <= prefixLength ||
		    string.CompareOrdinal(name, 0, genericMethodName, 0, prefixLength) != 0)
		{
			return false;
		}

		int typeArgumentCount = 1;
		for (int i = prefixLength; i < name.Length - 1; i++)
		{
			if (name[i] == ',' && name[i + 1] == ' ')
			{
				typeArgumentCount++;
			}
		}

		return typeArgumentCount == genericMethodName.Length - prefixLength;
	}

	/// <summary>
	///     Gets the argument at the zero-based <paramref name="index" /> as <typeparamref name="T" />.
	/// </summary>
	/// <returns>
	///     <see langword="false" /> when the argument is no <typeparamref name="T" />.
	/// </returns>
	public static bool TryGetValue<T>(IMethodInvocationParameters invocation, int index, out T value)
	{
		object? argument = invocation.GetParameterValueAt(index);
		if (argument is T typed)
		{
			value = typed;
			return true;
		}

		value = default!;
		return argument is null && default(T) is null;
	}

	/// <summary>
	///     Checks whether the argument at the zero-based <paramref name="index" /> matches <paramref name="match" />.
	/// </summary>
	public static bool Matches<T>(IParameterMatch<T> match, IMethodInvocationParameters invocation, int index)
		=> TryGetValue(invocation, index, out T value) && match.Matches(value);

	/// <summary>
	///     Checks whether the arguments match <paramref name="parameters" />, e.g. <see cref="Match.AnyParameters()" />.
	/// </summary>
	public static bool Matches(IParameters parameters, IMethodInvocationParameters invocation,
		params string[] parameterNames)
	{
		switch (parameters)
		{
			case IParametersMatch match:
			{
				object?[] values = new object?[parameterNames.Length];
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = invocation.GetParameterValueAt(i);
				}

				return match.Matches(values);
			}
			case INamedParametersMatch match:
			{
				(string, object?)[] values = new (string, object?)[parameterNames.Length];
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = (parameterNames[i], invocation.GetParameterValueAt(i));
				}

				return match.Matches(values);
			}
			default:
				return true;
		}
	}
}
