using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Mockolate.Interactions;

namespace Mockolate.Internals;

/// <summary>
///     Creates the auto-mocks with the factory that the source generator
///     emits into each assembly that does not set the <c>MockolateSkipAutoMocking</c> build property.
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
	private static readonly Type[] TupleDefinitions =
	[
		typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
		typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>),
		typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>),
		typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>),
	];

	/// <summary>
	///     Returns the <paramref name="behavior" /> that creates its auto-mocks with the <paramref name="factory" />.
	/// </summary>
	/// <remarks>
	///     The generated <c>CreateMock</c> calls this, so that a mock creates the auto-mocks of its own assembly. An
	///     assembly without the build property passes <see langword="null" />, so that a behavior that carries the
	///     factory of another assembly returns no auto-mocks there. The <paramref name="behavior" /> is returned
	///     unchanged when it already has the <paramref name="factory" /> or skips auto-mocking.
	/// </remarks>
	/// <param name="behavior">The behavior of the mock that is created.</param>
	/// <param name="factory">
	///     Creates a mock of the given interface with the given behavior, or returns <see langword="null" /> when the
	///     assembly has no mock of the interface; <see langword="null" /> disables auto-mocking.
	/// </param>
	/// <returns>The <paramref name="behavior" /> with the <paramref name="factory" />.</returns>
	public static MockBehavior UseFactory(MockBehavior behavior, Func<Type, MockBehavior, object?>? factory)
		=> behavior.AutoMockFactory == factory || (factory is not null && behavior.SkipAutoMocking)
			? behavior
			: behavior with
			{
				AutoMockFactory = factory,
			};

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
	///     the key return the same configurable mock. A tuple <paramref name="value" /> with an interface element is shared
	///     as well, because its elements can be auto-mocks.
	/// </remarks>
	/// <typeparam name="TValue">The value type of the indexer.</typeparam>
	/// <param name="behavior">The behavior of the mock that needs the value; the auto-mock uses it as well.</param>
	/// <param name="value">The default value produced for the indexer key.</param>
	/// <param name="access">The indexer access whose key the value belongs to.</param>
	/// <returns>The <paramref name="value" /> or the shared auto-mock.</returns>
	public static TValue Or<TValue>(MockBehavior behavior, TValue value, IndexerAccess access)
	{
		TValue result = Or(behavior, value);
		return IsShared(behavior, value, result) && access.GetOrCreateLeaf<TValue>() is { } leaf
			? leaf.ShareAutoMock(result)
			: result;
	}

	internal static TValue Or<TValue>(MockBehavior behavior, TValue value, IndexerValueStorage<TValue> leaf)
	{
		TValue result = Or(behavior, value);
		return IsShared(behavior, value, result) ? leaf.ShareAutoMock(result) : result;
	}

	internal static object? Create(Type type, MockBehavior behavior)
		=> behavior.AutoMockFactory?.Invoke(type, behavior);

	private static bool IsShared<TValue>(MockBehavior behavior, TValue value, TValue result)
		=> value is null
			? result is not null
			: TupleShape<TValue>.CanHoldAutoMocks && behavior.AutoMockFactory is not null && !behavior.SkipAutoMocking;

	/// <summary>
	///     Whether the <paramref name="type" /> is a tuple with an interface element, also in a nested tuple or task.
	/// </summary>
	private static bool IsTupleWithInterface(Type type)
		=> type.IsGenericType && Array.IndexOf(TupleDefinitions, type.GetGenericTypeDefinition()) >= 0 &&
		   Array.Exists(type.GetGenericArguments(), HasInterface);

	private static bool HasInterface(Type type)
	{
		if (type.IsInterface)
		{
			return true;
		}

		if (!type.IsGenericType)
		{
			return false;
		}

		Type definition = type.GetGenericTypeDefinition();
		return definition == typeof(Task<>) || string.Equals(definition.FullName, "System.Threading.Tasks.ValueTask`1", StringComparison.Ordinal)
			? HasInterface(type.GetGenericArguments()[0])
			: IsTupleWithInterface(type);
	}

	private static class TupleShape<TValue>
	{
		public static readonly bool CanHoldAutoMocks = IsTupleWithInterface(typeof(TValue));
	}
}
