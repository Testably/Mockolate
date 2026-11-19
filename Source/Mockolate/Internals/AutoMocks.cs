using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Mockolate.Internals;

/// <summary>
///     Creates the auto-mocks for <see cref="MockBehavior.AutoMock" /> with the factory that the source generator
///     emits into each assembly that sets the <c>MockolateAutoMocking</c> build property.
/// </summary>
/// <remarks>
///     The factory creates the mocks of its own assembly with a <see langword="typeof" /> comparison, so creating an
///     auto-mock needs no reflection, and the <c>Mock</c> property of an auto-mock works in the assembly that created
///     the parent mock.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AutoMocks
{
	/// <summary>
	///     Returns the <paramref name="behavior" /> that creates its auto-mocks with the <paramref name="factory" />.
	/// </summary>
	/// <remarks>
	///     The generated <c>CreateMock</c> calls this, so that a mock creates the auto-mocks of its own assembly. The
	///     <paramref name="behavior" /> is returned unchanged when it does not enable auto-mocking.
	/// </remarks>
	/// <param name="behavior">The behavior of the mock that is created.</param>
	/// <param name="factory">
	///     Creates a mock of the given interface with the given behavior, or returns <see langword="null" /> when the
	///     assembly has no mock of the interface.
	/// </param>
	/// <returns>The <paramref name="behavior" /> with the <paramref name="factory" />.</returns>
	public static MockBehavior UseFactory(MockBehavior behavior, Func<Type, MockBehavior, object?> factory)
		=> behavior.AutoMock && behavior.AutoMockFactory != factory
			? behavior with
			{
				AutoMockFactory = factory,
			}
			: behavior;

	/// <summary>
	///     Returns the <paramref name="value" />, or a new mock of <typeparamref name="TValue" /> when it is
	///     <see langword="null" /> and <see cref="MockBehavior.AutoMock" /> is enabled on the <paramref name="behavior" />.
	/// </summary>
	/// <remarks>
	///     The caller is responsible for keeping the created mock, so that the member keeps returning it.
	/// </remarks>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="behavior">The behavior of the mock that needs the value; the auto-mock uses it as well.</param>
	/// <param name="value">The default value produced for the member.</param>
	/// <returns>The <paramref name="value" /> or the auto-mock.</returns>
	public static TValue Or<TValue>(MockBehavior behavior, TValue value)
	{
		if (value is not null || !behavior.AutoMock)
		{
			return value;
		}

		return Create(typeof(TValue), behavior) is TValue mock ? mock : value;
	}

	internal static object? Create(Type type, MockBehavior behavior)
		=> behavior.AutoMockFactory?.Invoke(type, behavior);
}
