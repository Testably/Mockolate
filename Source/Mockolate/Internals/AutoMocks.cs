using System;
using System.ComponentModel;
using System.Diagnostics;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Creates the auto-mocks with the factory that the source generator
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
	///     <paramref name="behavior" /> is returned unchanged when it skips auto-mocking.
	/// </remarks>
	/// <param name="behavior">The behavior of the mock that is created.</param>
	/// <param name="factory">
	///     Creates a mock of the given interface with the given behavior, or returns <see langword="null" /> when the
	///     assembly has no mock of the interface.
	/// </param>
	/// <returns>The <paramref name="behavior" /> with the <paramref name="factory" />.</returns>
	public static MockBehavior UseFactory(MockBehavior behavior, Func<Type, MockBehavior, object?> factory)
		=> !behavior.SkipAutoMocking && behavior.AutoMockFactory != factory
			? behavior with
			{
				AutoMockFactory = factory,
			}
			: behavior;

	/// <summary>
	///     Returns the <paramref name="value" />, or a new mock of <typeparamref name="TValue" /> when it is
	///     <see langword="null" /> and auto-mocking is not skipped on the <paramref name="behavior" />.
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
		if (value is not null || behavior.SkipAutoMocking)
		{
			return value;
		}

		return Create(typeof(TValue), behavior) is TValue mock ? mock : value;
	}

	/// <summary>
	///     Returns the <paramref name="value" />, or the auto-mock of <typeparamref name="TValue" /> that is shared by all
	///     reads of the indexer key of the <paramref name="access" />.
	/// </summary>
	/// <remarks>
	///     The caller stores the returned value for the key; sharing the first auto-mock makes concurrent first reads of
	///     the key return the same configurable mock.
	/// </remarks>
	/// <typeparam name="TValue">The value type of the indexer.</typeparam>
	/// <param name="behavior">The behavior of the mock that needs the value; the auto-mock uses it as well.</param>
	/// <param name="value">The default value produced for the indexer key.</param>
	/// <param name="access">The indexer access whose key the value belongs to.</param>
	/// <returns>The <paramref name="value" /> or the shared auto-mock.</returns>
	public static TValue Or<TValue>(MockBehavior behavior, TValue value, IndexerAccess access)
	{
		TValue result = Or(behavior, value);
		return value is null && result is not null && access.GetOrCreateLeaf<TValue>() is { } leaf
			? leaf.ShareAutoMock(result)
			: result;
	}

	internal static TValue Or<TValue>(MockBehavior behavior, TValue value, IndexerValueStorage<TValue> leaf)
	{
		TValue result = Or(behavior, value);
		return value is null && result is not null ? leaf.ShareAutoMock(result) : result;
	}

	internal static object? Create(Type type, MockBehavior behavior)
		=> behavior.AutoMockFactory?.Invoke(type, behavior);
}
