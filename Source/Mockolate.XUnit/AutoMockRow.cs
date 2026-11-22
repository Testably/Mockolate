using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mockolate.XUnit;

/// <summary>
///     Creates the arguments of one test invocation on demand, so that a class under test receives the arguments of
///     the other parameters whatever their order. The given <paramref name="arguments" /> are the arguments of the first
///     parameters.
/// </summary>
internal sealed class AutoMockRow(Assembly assembly, ParameterInfo[] parameters, object?[] arguments)
{
	private readonly State[] _states = new State[parameters.Length];
	private readonly object?[] _values = new object?[parameters.Length];

	private readonly bool[] _isTransient = Array.ConvertAll(parameters, parameter =>
		parameter.IsDefined(typeof(TransientAttribute), false));

	public object?[] Create()
	{
		for (int index = 0; index < arguments.Length; index++)
		{
			_values[index] = arguments[index];
			_states[index] = State.Created;
		}

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
		ParameterInfo parameter = parameters[index];
		if (!MockDataSources.TryCreate(assembly, parameter.ParameterType, null,
			    (type, parameterName) => Resolve(type, parameterName, index), out object? value))
		{
			throw new InvalidOperationException(
				$"Mockolate cannot create the parameter '{parameter.Name}' of type '{parameter.ParameterType}': it is neither a mockable interface nor a class with exactly one longest accessible constructor.");
		}

		_values[index] = value;
		_states[index] = State.Created;
		return value;
	}

	/// <summary>
	///     The shared test parameter of the <paramref name="type" />; the <paramref name="parameterName" /> of the
	///     constructor parameter only decides between several of them.
	/// </summary>
	private object? Resolve(Type type, string parameterName, int requester)
	{
		List<int> candidates = Enumerable.Range(0, parameters.Length)
			.Where(index => index != requester && !_isTransient[index] && parameters[index].ParameterType == type)
			.ToList();
		if (candidates.Count > 1)
		{
			List<int> named = candidates
				.Where(index => string.Equals(parameters[index].Name, parameterName, StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (named.Count != 1)
			{
				throw new InvalidOperationException(
					$"The constructor parameter '{parameterName}' of type '{type}' matches the test parameters {string.Join(", ", candidates.Select(index => $"'{parameters[index].Name}'"))}; name one of them '{parameterName}' or mark the others [Transient].");
			}

			candidates = named;
		}

		return candidates.Count == 1 ? GetOrCreate(candidates[0]) : null;
	}

	private enum State
	{
		NotCreated,
		Creating,
		Created,
	}
}
