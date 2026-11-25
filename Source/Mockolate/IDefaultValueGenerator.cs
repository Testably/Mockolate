using System;

namespace Mockolate;

/// <summary>
///     Defines a mechanism for generating default values of a specified type. Populated on
///     <see cref="MockBehavior.DefaultValue" /> and consulted whenever a mock needs a return value without a
///     matching setup.
/// </summary>
public interface IDefaultValueGenerator
{
	/// <summary>
	///     Generates a default value for <paramref name="type" />, optionally using the invocation
	///     <paramref name="parameters" /> for context.
	/// </summary>
	/// <param name="type">The runtime type to produce a default value for.</param>
	/// <param name="parameters">
	///     The runtime arguments of the mocked invocation, in declaration order. Implementations can inspect this
	///     array to return a more appropriate default - for example, the built-in cancellable-task factory scans it
	///     for a cancelled <see cref="System.Threading.CancellationToken" /> and returns
	///     <see cref="System.Threading.Tasks.Task.FromCanceled(System.Threading.CancellationToken)" /> instead of
	///     <see cref="System.Threading.Tasks.Task.CompletedTask" />.
	/// </param>
	/// <returns>
	///     A default value assignable to <paramref name="type" />, or <see langword="null" /> if no value can be
	///     produced. The caller will fall back to the language default if the returned value is not
	///     assignment-compatible.
	/// </returns>
	object? GenerateValue(Type type, params object?[] parameters);
}
