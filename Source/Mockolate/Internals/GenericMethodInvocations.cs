using System;
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
	///     <paramref name="genericMethodName" /> with <paramref name="parameterCount" /> parameters, and of the overload that
	///     <paramref name="isCallOf" /> recognizes, when other overloads have the same name and as many type parameters
	///     and parameters.
	/// </summary>
	public static bool TryGetInvocation(IInteraction interaction, string genericMethodName, int parameterCount,
		Func<IMethodInvocationParameters, bool>? isCallOf, out IMethodInvocationParameters invocation)
	{
		if (interaction is IMethodInteraction method && IsCallOf(method.Name, genericMethodName) &&
		    interaction is IMethodInvocationParameters parameters && parameters.ParameterCount == parameterCount &&
		    (isCallOf is null || isCallOf(parameters)))
		{
			invocation = parameters;
			return true;
		}

		invocation = null!;
		return false;
	}

	private static bool IsCallOf(string name, string genericMethodName)
	{
		int prefixLength = genericMethodName.AsSpan().LastIndexOf('<') + 1;
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
	///     Checks whether the parameter at the zero-based <paramref name="parameterIndex" /> has the type of the type
	///     argument at the zero-based <paramref name="typeArgumentIndex" /> of the called generic method.
	/// </summary>
	/// <param name="invocation">The invocation of the generic method.</param>
	/// <param name="genericMethodName">The name of the generic method without its type arguments, e.g. <c>Ns.IFoo.Send&lt;,&gt;</c>.</param>
	/// <param name="parameterIndex">The zero-based index of the parameter.</param>
	/// <param name="typeArgumentIndex">The zero-based index of the type argument.</param>
	/// <param name="isNullable">Whether the parameter is a <see cref="Nullable{T}" /> of the type argument.</param>
	public static bool IsTypeArgument(IMethodInvocationParameters invocation, string genericMethodName, int parameterIndex,
		int typeArgumentIndex, bool isNullable = false)
	{
		if (invocation is not IMethodInteraction method || invocation.GetParameterTypeAt(parameterIndex) is not { } type)
		{
			return false;
		}

		if (isNullable)
		{
			type = Nullable.GetUnderlyingType(type);
			if (type is null)
			{
				return false;
			}
		}

		string name = method.Name;
		// The name of a type argument can itself contain a '<', e.g. for an anonymous type.
		int start = genericMethodName.AsSpan().LastIndexOf('<') + 1;
		for (int i = 0; i < typeArgumentIndex; i++)
		{
			start = name.IndexOf(", ", start, StringComparison.Ordinal) + 2;
			if (start < 2)
			{
				return false;
			}
		}

		int end = name.IndexOf(", ", start, StringComparison.Ordinal);
		if (end < 0)
		{
			end = name.Length - 1;
		}

		string typeName = type.ToString();
		return end - start == typeName.Length && string.CompareOrdinal(name, start, typeName, 0, typeName.Length) == 0;
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
