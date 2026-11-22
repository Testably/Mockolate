using System;
using System.Reflection;
using TUnit.Core;

namespace Mockolate.TUnit;

/// <summary>
///     Creates the arguments of one test invocation on demand, so that a class under test receives the arguments of
///     the other parameters whatever their order.
/// </summary>
internal sealed class AutoMockRow(Assembly assembly, ParameterMetadata[] parameters)
{
	private readonly State[] _states = new State[parameters.Length];
	private readonly object?[] _values = new object?[parameters.Length];

	public object?[] Create()
	{
		for (int index = 0; index < parameters.Length; index++)
		{
			GetOrCreate(index);
		}

		return _values;
	}

	private object? GetOrCreate(int index)
	{
		switch (_states[index])
		{
			case State.Created:
				return _values[index];
			case State.Creating:
				throw new InvalidOperationException(
					$"The parameter '{parameters[index].Name}' depends on itself through a constructor dependency.");
		}

		_states[index] = State.Creating;
		ParameterMetadata parameter = parameters[index];
		if (!MockDataSources.TryCreate(assembly, parameter.Type, null, (type, _) => Resolve(type, index),
			    out object? value))
		{
			throw new InvalidOperationException(
				$"Mockolate cannot create the parameter '{parameter.Name}' of type '{parameter.Type}': it is neither a mockable interface nor a class with exactly one longest accessible constructor.");
		}

		_values[index] = value;
		_states[index] = State.Created;
		return value;
	}

	private object? Resolve(Type type, int requester)
	{
		for (int index = 0; index < parameters.Length; index++)
		{
			if (index != requester && parameters[index].Type == type)
			{
				return GetOrCreate(index);
			}
		}

		return null;
	}

	private enum State
	{
		NotCreated,
		Creating,
		Created,
	}
}
