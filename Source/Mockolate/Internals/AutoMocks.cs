using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Mockolate.Internals;

/// <summary>
///     The auto-mock factories that the source generator registers for the interfaces it generates a mock for.
/// </summary>
/// <remarks>
///     Every assembly with generated mocks registers one factory that creates the mocks of its own compilation with
///     a <see langword="typeof" /> comparison, so creating an auto-mock needs no reflection.
/// </remarks>
#if !DEBUG
[DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AutoMocks
{
	private static Func<Type, MockBehavior, object?>[] _factories = [];

	/// <summary>
	///     Registers a <paramref name="factory" /> that creates a mock of the given interface with the given
	///     behavior, or returns <see langword="null" /> when it has no mock for the interface.
	/// </summary>
	/// <param name="factory">The generator-emitted factory of one assembly.</param>
	public static void Register(Func<Type, MockBehavior, object?> factory)
	{
		Func<Type, MockBehavior, object?>[] current;
		Func<Type, MockBehavior, object?>[] updated;
		do
		{
			current = Volatile.Read(ref _factories);
			updated = new Func<Type, MockBehavior, object?>[current.Length + 1];
			Array.Copy(current, updated, current.Length);
			updated[current.Length] = factory;
		}
		while (Interlocked.CompareExchange(ref _factories, updated, current) != current);
	}

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
	{
		if (!type.IsInterface)
		{
			return null;
		}

		foreach (Func<Type, MockBehavior, object?> factory in Volatile.Read(ref _factories))
		{
			if (factory(type, behavior) is { } mock)
			{
				return mock;
			}
		}

		return null;
	}
}
