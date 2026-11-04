using Mockolate.Internals;

namespace Mockolate;

/// <summary>
///     Non-generic contract exposed by every Mockolate-generated mock instance. Provides access to the
///     <see cref="MockRegistry" /> that backs the mock.
/// </summary>
/// <remarks>
///     Most users reach for the generator-emitted typed <c>Mock</c> property (for example <c>sut.Mock</c>) instead of
///     this interface. The typed view surfaces the fluent entry points <c>Setup</c>, <c>Verify</c>, <c>Raise</c>,
///     <c>InScenario</c>, <c>TransitionTo</c>, <c>Monitor</c>, and <c>ClearAllInteractions</c>. Use
///     <see cref="IMock" /> directly only for framework code that needs to work against an untyped mock handle.
/// </remarks>
public interface IMock
{
	/// <summary>
	///     The <see cref="Mockolate.Internals.MockRegistry" /> that backs this mock: it owns the <see cref="MockBehavior" />,
	///     the registered setups, and the recorded interactions.
	/// </summary>
	/// <remarks>
	///     The typed <c>Mock</c> property on a generated mock exposes the fluent surface (<c>Setup</c>, <c>Verify</c>,
	///     <c>Raise</c>, <c>InScenario</c>, <c>TransitionTo</c>, <c>Monitor</c>, <c>ClearAllInteractions</c>) and is
	///     the preferred entry point; reach for <see cref="MockRegistry" /> directly only when you need to interact
	///     with the underlying state (for example in custom extensions or diagnostic tooling).
	/// </remarks>
	MockRegistry MockRegistry { get; }

	/// <summary>
	///     A string representation of the mock, which includes the type of the mocked object and any additional interfaces it implements.
	/// </summary>
	string ToString();
}
