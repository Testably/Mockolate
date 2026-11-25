using System;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Setup;

#pragma warning disable S2436 // Types and methods should not have too many generic parameters

namespace Mockolate.Internals;

public partial class MockRegistry
{
	/// <summary>
	///     Whether an indexer setup can exist that is not registered under the <paramref name="memberId" />, so
	///     that the generated lookup by member id is not sufficient.
	/// </summary>
	private bool HasIndexerSetupsBeyond(int memberId)
	{
		if (!string.IsNullOrEmpty(Scenario) || Setup.Indexers.Count > 0)
		{
			return true;
		}

		IndexerSetup[]?[]? table = Volatile.Read(ref _indexerSetupsByMemberId);
		if (table is not null)
		{
			for (int i = 0; i < table.Length; i++)
			{
				if (i != memberId && table[i] is not null)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	///     Returns the setup that matches the indexer getter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	public IndexerSetup<TValue, T1>? FindIndexerGetterSetup<TValue, T1>(int memberId, T1 parameter1)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1>>(new IndexerGetterAccess<T1>(parameter1))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer getter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	public IndexerSetup<TValue, T1, T2>? FindIndexerGetterSetup<TValue, T1, T2>(int memberId, T1 parameter1, T2 parameter2)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2>>(new IndexerGetterAccess<T1, T2>(parameter1, parameter2))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer getter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	public IndexerSetup<TValue, T1, T2, T3>? FindIndexerGetterSetup<TValue, T1, T2, T3>(int memberId, T1 parameter1, T2 parameter2, T3 parameter3)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2, T3>>(new IndexerGetterAccess<T1, T2, T3>(parameter1, parameter2, parameter3))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer getter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <typeparam name="T4">The type of indexer parameter 4.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="parameter4">The value of indexer parameter 4.</param>
	public IndexerSetup<TValue, T1, T2, T3, T4>? FindIndexerGetterSetup<TValue, T1, T2, T3, T4>(int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2, T3, T4>>(new IndexerGetterAccess<T1, T2, T3, T4>(parameter1, parameter2, parameter3, parameter4))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer setter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="value">The value being assigned.</param>
	public IndexerSetup<TValue, T1>? FindIndexerSetterSetup<TValue, T1>(int memberId, T1 parameter1, TValue value)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1>>(new IndexerSetterAccess<T1, TValue>(parameter1, value))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer setter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="value">The value being assigned.</param>
	public IndexerSetup<TValue, T1, T2>? FindIndexerSetterSetup<TValue, T1, T2>(int memberId, T1 parameter1, T2 parameter2, TValue value)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2>>(new IndexerSetterAccess<T1, T2, TValue>(parameter1, parameter2, value))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer setter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="value">The value being assigned.</param>
	public IndexerSetup<TValue, T1, T2, T3>? FindIndexerSetterSetup<TValue, T1, T2, T3>(int memberId, T1 parameter1, T2 parameter2, T3 parameter3, TValue value)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2, T3>>(new IndexerSetterAccess<T1, T2, T3, TValue>(parameter1, parameter2, parameter3, value))
			: null;

	/// <summary>
	///     Returns the setup that matches the indexer setter access and is not registered under the
	///     <paramref name="memberId" />, or <see langword="null" /> when there is none.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <typeparam name="T4">The type of indexer parameter 4.</typeparam>
	/// <param name="memberId">The generator-emitted member id whose setups were already checked.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="parameter4">The value of indexer parameter 4.</param>
	/// <param name="value">The value being assigned.</param>
	public IndexerSetup<TValue, T1, T2, T3, T4>? FindIndexerSetterSetup<TValue, T1, T2, T3, T4>(int memberId, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value)
		=> HasIndexerSetupsBeyond(memberId)
			? GetIndexerSetup<IndexerSetup<TValue, T1, T2, T3, T4>>(new IndexerSetterAccess<T1, T2, T3, T4, TValue>(parameter1, parameter2, parameter3, parameter4, value))
			: null;

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" />, or the stored or default value when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="defaultValueGenerator">Cached factory invoked with the active <see cref="MockBehavior" /> when a default value is needed.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	/// <exception cref="MockNotSetupException"><paramref name="setup" /> is <see langword="null" />, <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" /> and no value was stored.</exception>
	public TValue GetIndexer<TValue, T1>(IndexerSetup<TValue, T1>? setup, T1 parameter1,
		Func<MockBehavior, TValue> defaultValueGenerator, int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1>))
		{
			return ApplyIndexerSetup<TValue>(new IndexerGetterAccess<T1>(parameter1), setup, defaultValueGenerator, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1);
		if (setup is not null)
		{
			return setup.GetResult(leaf, parameter1, Behavior, defaultValueGenerator);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		if (Behavior.ThrowWhenNotSetup)
		{
			throw new MockNotSetupException(
				$"{new IndexerGetterAccess<T1>(parameter1)} was accessed without prior setup.");
		}

		TValue value = AutoMocks.Or(Behavior, defaultValueGenerator(Behavior), leaf);
		leaf.Store(value);
		return value;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" /> starting with the
	///     <paramref name="baseValue" />, or the stored value or <paramref name="baseValue" /> when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="baseValue">The value returned by the base class or the wrapped instance.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	public TValue GetIndexer<TValue, T1>(IndexerSetup<TValue, T1>? setup, T1 parameter1, TValue baseValue,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1>))
		{
			return ApplyIndexerGetter(new IndexerGetterAccess<T1>(parameter1), setup, baseValue, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1);
		if (setup is not null)
		{
			return setup.GetResultWithBase(leaf, parameter1, baseValue);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		leaf.Store(baseValue);
		return baseValue;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" />, or the stored or default value when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="defaultValueGenerator">Cached factory invoked with the active <see cref="MockBehavior" /> when a default value is needed.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	/// <exception cref="MockNotSetupException"><paramref name="setup" /> is <see langword="null" />, <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" /> and no value was stored.</exception>
	public TValue GetIndexer<TValue, T1, T2>(IndexerSetup<TValue, T1, T2>? setup, T1 parameter1, T2 parameter2,
		Func<MockBehavior, TValue> defaultValueGenerator, int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2>))
		{
			return ApplyIndexerSetup<TValue>(new IndexerGetterAccess<T1, T2>(parameter1, parameter2), setup, defaultValueGenerator, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2);
		if (setup is not null)
		{
			return setup.GetResult(leaf, parameter1, parameter2, Behavior, defaultValueGenerator);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		if (Behavior.ThrowWhenNotSetup)
		{
			throw new MockNotSetupException(
				$"{new IndexerGetterAccess<T1, T2>(parameter1, parameter2)} was accessed without prior setup.");
		}

		TValue value = AutoMocks.Or(Behavior, defaultValueGenerator(Behavior), leaf);
		leaf.Store(value);
		return value;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" /> starting with the
	///     <paramref name="baseValue" />, or the stored value or <paramref name="baseValue" /> when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="baseValue">The value returned by the base class or the wrapped instance.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	public TValue GetIndexer<TValue, T1, T2>(IndexerSetup<TValue, T1, T2>? setup, T1 parameter1, T2 parameter2, TValue baseValue,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2>))
		{
			return ApplyIndexerGetter(new IndexerGetterAccess<T1, T2>(parameter1, parameter2), setup, baseValue, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2);
		if (setup is not null)
		{
			return setup.GetResultWithBase(leaf, parameter1, parameter2, baseValue);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		leaf.Store(baseValue);
		return baseValue;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" />, or the stored or default value when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="defaultValueGenerator">Cached factory invoked with the active <see cref="MockBehavior" /> when a default value is needed.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	/// <exception cref="MockNotSetupException"><paramref name="setup" /> is <see langword="null" />, <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" /> and no value was stored.</exception>
	public TValue GetIndexer<TValue, T1, T2, T3>(IndexerSetup<TValue, T1, T2, T3>? setup, T1 parameter1, T2 parameter2, T3 parameter3,
		Func<MockBehavior, TValue> defaultValueGenerator, int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3>))
		{
			return ApplyIndexerSetup<TValue>(new IndexerGetterAccess<T1, T2, T3>(parameter1, parameter2, parameter3), setup, defaultValueGenerator, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3);
		if (setup is not null)
		{
			return setup.GetResult(leaf, parameter1, parameter2, parameter3, Behavior, defaultValueGenerator);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		if (Behavior.ThrowWhenNotSetup)
		{
			throw new MockNotSetupException(
				$"{new IndexerGetterAccess<T1, T2, T3>(parameter1, parameter2, parameter3)} was accessed without prior setup.");
		}

		TValue value = AutoMocks.Or(Behavior, defaultValueGenerator(Behavior), leaf);
		leaf.Store(value);
		return value;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" /> starting with the
	///     <paramref name="baseValue" />, or the stored value or <paramref name="baseValue" /> when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="baseValue">The value returned by the base class or the wrapped instance.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	public TValue GetIndexer<TValue, T1, T2, T3>(IndexerSetup<TValue, T1, T2, T3>? setup, T1 parameter1, T2 parameter2, T3 parameter3, TValue baseValue,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3>))
		{
			return ApplyIndexerGetter(new IndexerGetterAccess<T1, T2, T3>(parameter1, parameter2, parameter3), setup, baseValue, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3);
		if (setup is not null)
		{
			return setup.GetResultWithBase(leaf, parameter1, parameter2, parameter3, baseValue);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		leaf.Store(baseValue);
		return baseValue;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" />, or the stored or default value when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <typeparam name="T4">The type of indexer parameter 4.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="parameter4">The value of indexer parameter 4.</param>
	/// <param name="defaultValueGenerator">Cached factory invoked with the active <see cref="MockBehavior" /> when a default value is needed.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	/// <exception cref="MockNotSetupException"><paramref name="setup" /> is <see langword="null" />, <see cref="MockBehavior.ThrowWhenNotSetup" /> is <see langword="true" /> and no value was stored.</exception>
	public TValue GetIndexer<TValue, T1, T2, T3, T4>(IndexerSetup<TValue, T1, T2, T3, T4>? setup, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4,
		Func<MockBehavior, TValue> defaultValueGenerator, int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3, T4>))
		{
			return ApplyIndexerSetup<TValue>(new IndexerGetterAccess<T1, T2, T3, T4>(parameter1, parameter2, parameter3, parameter4), setup, defaultValueGenerator, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3).GetOrAddChild(parameter4);
		if (setup is not null)
		{
			return setup.GetResult(leaf, parameter1, parameter2, parameter3, parameter4, Behavior, defaultValueGenerator);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		if (Behavior.ThrowWhenNotSetup)
		{
			throw new MockNotSetupException(
				$"{new IndexerGetterAccess<T1, T2, T3, T4>(parameter1, parameter2, parameter3, parameter4)} was accessed without prior setup.");
		}

		TValue value = AutoMocks.Or(Behavior, defaultValueGenerator(Behavior), leaf);
		leaf.Store(value);
		return value;
	}

	/// <summary>
	///     Gets the value of an indexer from the <paramref name="setup" /> starting with the
	///     <paramref name="baseValue" />, or the stored value or <paramref name="baseValue" /> when
	///     <paramref name="setup" /> is <see langword="null" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <typeparam name="T4">The type of indexer parameter 4.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="parameter4">The value of indexer parameter 4.</param>
	/// <param name="baseValue">The value returned by the base class or the wrapped instance.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns>The value of the indexer.</returns>
	public TValue GetIndexer<TValue, T1, T2, T3, T4>(IndexerSetup<TValue, T1, T2, T3, T4>? setup, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue baseValue,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3, T4>))
		{
			return ApplyIndexerGetter(new IndexerGetterAccess<T1, T2, T3, T4>(parameter1, parameter2, parameter3, parameter4), setup, baseValue, signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3).GetOrAddChild(parameter4);
		if (setup is not null)
		{
			return setup.GetResultWithBase(leaf, parameter1, parameter2, parameter3, parameter4, baseValue);
		}

		if (leaf.HasValue)
		{
			return leaf.Value;
		}

		leaf.Store(baseValue);
		return baseValue;
	}

	/// <summary>
	///     Stores the <paramref name="value" /> of an indexer and applies the <paramref name="setup" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="value">The value being assigned.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetIndexer<TValue, T1>(IndexerSetup<TValue, T1>? setup, T1 parameter1, TValue value,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1>))
		{
			return ApplyIndexerSetter(new IndexerSetterAccess<T1, TValue>(parameter1, value), setup, value,
				signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1);
		if (setup is null)
		{
			leaf.Store(value);
			return Behavior.SkipBaseClass;
		}

		setup.SetResult(leaf, parameter1, value);
		return setup.SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Stores the <paramref name="value" /> of an indexer and applies the <paramref name="setup" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="value">The value being assigned.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetIndexer<TValue, T1, T2>(IndexerSetup<TValue, T1, T2>? setup, T1 parameter1, T2 parameter2, TValue value,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2>))
		{
			return ApplyIndexerSetter(new IndexerSetterAccess<T1, T2, TValue>(parameter1, parameter2, value), setup, value,
				signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2);
		if (setup is null)
		{
			leaf.Store(value);
			return Behavior.SkipBaseClass;
		}

		setup.SetResult(leaf, parameter1, parameter2, value);
		return setup.SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Stores the <paramref name="value" /> of an indexer and applies the <paramref name="setup" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="value">The value being assigned.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetIndexer<TValue, T1, T2, T3>(IndexerSetup<TValue, T1, T2, T3>? setup, T1 parameter1, T2 parameter2, T3 parameter3, TValue value,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3>))
		{
			return ApplyIndexerSetter(new IndexerSetterAccess<T1, T2, T3, TValue>(parameter1, parameter2, parameter3, value), setup, value,
				signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3);
		if (setup is null)
		{
			leaf.Store(value);
			return Behavior.SkipBaseClass;
		}

		setup.SetResult(leaf, parameter1, parameter2, parameter3, value);
		return setup.SkipBaseClass() ?? Behavior.SkipBaseClass;
	}

	/// <summary>
	///     Stores the <paramref name="value" /> of an indexer and applies the <paramref name="setup" />.
	/// </summary>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <typeparam name="T1">The type of indexer parameter 1.</typeparam>
	/// <typeparam name="T2">The type of indexer parameter 2.</typeparam>
	/// <typeparam name="T3">The type of indexer parameter 3.</typeparam>
	/// <typeparam name="T4">The type of indexer parameter 4.</typeparam>
	/// <param name="setup">The matching indexer setup, or <see langword="null" />.</param>
	/// <param name="parameter1">The value of indexer parameter 1.</param>
	/// <param name="parameter2">The value of indexer parameter 2.</param>
	/// <param name="parameter3">The value of indexer parameter 3.</param>
	/// <param name="parameter4">The value of indexer parameter 4.</param>
	/// <param name="value">The value being assigned.</param>
	/// <param name="signatureIndex">Index into the mock's indexer-signature table (emitted by the source generator).</param>
	/// <returns><see langword="true" /> when the base-class setter should be skipped.</returns>
	public bool SetIndexer<TValue, T1, T2, T3, T4>(IndexerSetup<TValue, T1, T2, T3, T4>? setup, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, TValue value,
		int signatureIndex)
	{
		if (setup is not null && setup.GetType() != typeof(IndexerSetup<TValue, T1, T2, T3, T4>))
		{
			return ApplyIndexerSetter(new IndexerSetterAccess<T1, T2, T3, T4, TValue>(parameter1, parameter2, parameter3, parameter4, value), setup, value,
				signatureIndex);
		}

		IndexerValueStorage<TValue> leaf = Setup.Indexers.GetOrCreateStorage<TValue>(signatureIndex)
			.GetOrAddChild(parameter1).GetOrAddChild(parameter2).GetOrAddChild(parameter3).GetOrAddChild(parameter4);
		if (setup is null)
		{
			leaf.Store(value);
			return Behavior.SkipBaseClass;
		}

		setup.SetResult(leaf, parameter1, parameter2, parameter3, parameter4, value);
		return setup.SkipBaseClass() ?? Behavior.SkipBaseClass;
	}
}
