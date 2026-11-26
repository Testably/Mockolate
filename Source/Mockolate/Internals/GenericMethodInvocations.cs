using System;
using System.ComponentModel;
using Mockolate.Interactions;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Recognizes the recorded calls of a generic method for any type argument: their names start with the name of the
///     open method up to the last <c>&lt;</c>, as the type arguments of the method follow it and those of a generic
///     containing type precede it.
/// </summary>
/// <remarks>
///     This type is public only so that generated code can use it; it is not intended to be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GenericMethodInvocations
{
	/// <summary>
	///     Gets the start of the names of the calls of the open generic method <paramref name="genericMethodName" />.
	/// </summary>
	public static string GetNamePrefix(string genericMethodName)
		=> genericMethodName.Substring(0, genericMethodName.LastIndexOf('<') + 1);

	/// <summary>
	///     Checks whether <paramref name="interaction" /> is a call with <paramref name="parameterCount" /> parameters
	///     whose name starts with <paramref name="namePrefix" />.
	/// </summary>
	public static bool TryGetInvocation(IInteraction interaction, string namePrefix, int parameterCount,
		out IMethodInvocationParameters invocation)
	{
		if (interaction is IMethodInteraction method && method.Name.StartsWith(namePrefix, StringComparison.Ordinal) &&
		    interaction is IMethodInvocationParameters parameters && parameters.ParameterCount == parameterCount)
		{
			invocation = parameters;
			return true;
		}

		invocation = null!;
		return false;
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
