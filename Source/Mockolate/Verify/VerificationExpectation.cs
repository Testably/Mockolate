using System;

namespace Mockolate.Verify;

/// <summary>
///     Builds the expectation text of a verification only when a failure message needs it. The factory is a static
///     delegate over existing objects, so a successful verification allocates nothing for its message.
/// </summary>
internal readonly struct VerificationExpectation
{
	private readonly object? _state;
	private readonly object? _argument;
	private readonly Func<object?, object?, string> _factory;

	public VerificationExpectation(object? state, object? argument, Func<object?, object?, string> factory)
	{
		_state = state;
		_argument = argument;
		_factory = factory;
	}

	public string Create() => _factory(_state, _argument);
}
