using System;
using System.Diagnostics;
using System.Linq;
using Mockolate.Exceptions;
using Mockolate.Internals;

namespace Mockolate;

/// <summary>
///     The arguments that a constructor received when Mockolate created the class.
/// </summary>
/// <remarks>
///     Use it to set up and verify the mocks that were passed to the constructor.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public sealed class ConstructorArguments
{
	private readonly string _constructor;
	private readonly string[] _names;
	private readonly Type[] _types;
	private readonly object?[] _values;

	internal ConstructorArguments(ConstructorSignature signature, object?[] values)
	{
		_constructor = signature.Constructor;
		_names = signature.Names;
		_types = signature.Types;
		_values = values;
	}

	/// <summary>
	///     The only argument of type <typeparamref name="T" />.
	/// </summary>
	/// <remarks>
	///     A parameter declared with exactly this type wins over an argument that only converts to it.
	/// </remarks>
	/// <exception cref="MockException">The constructor received no or several arguments of type <typeparamref name="T" />.</exception>
	public T Get<T>()
	{
		int[] matches = Enumerable.Range(0, _values.Length).Where(index => _types[index] == typeof(T)).ToArray();
		if (matches.Length == 0)
		{
			matches = Enumerable.Range(0, _values.Length).Where(index => _values[index] is T).ToArray();
		}

		return matches.Length switch
		{
			1 => Get<T>(_names[matches[0]]),
			0 => throw new MockException(
				$"The constructor {_constructor} received no argument of type {typeof(T).FormatType()}."),
			_ => throw new MockException(
				$"The constructor {_constructor} received several arguments of type {typeof(T).FormatType()} ({string.Join(", ", matches.Select(index => $"'{_names[index]}'"))}). Pass the name of the parameter."),
		};
	}

	/// <summary>
	///     The argument of the parameter <paramref name="parameterName" />.
	/// </summary>
	/// <exception cref="MockException">The constructor has no parameter <paramref name="parameterName" />, or its argument is no <typeparamref name="T" />.</exception>
	public T Get<T>(string parameterName)
	{
		int index = Array.IndexOf(_names, parameterName);
		if (index < 0)
		{
			throw new MockException(
				$"The constructor {_constructor} has no parameter '{parameterName}'.");
		}

		object? value = _values[index];
		if (value is T typedValue)
		{
			return typedValue;
		}

		if (value is null && default(T) is null)
		{
			return default!;
		}

		throw new MockException(
			$"The argument of the parameter '{parameterName}' of the constructor {_constructor} is {(value is null ? "null" : $"of type {value.GetType().FormatType()}")}, but must be of type {typeof(T).FormatType()}.");
	}
}
