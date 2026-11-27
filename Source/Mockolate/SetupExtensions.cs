using Mockolate.Setup;

namespace Mockolate;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
/// <summary>
///     Extensions for indexer, property and method setups.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public static class SetupExtensions
{
	/// <summary>
	///     Extensions for property setups.
	/// </summary>
	extension<T>(IPropertySetupReturnWhenBuilder<T> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for property getter callback setups.
	/// </summary>
	extension<T>(IPropertyGetterSetupCallbackWhenBuilder<T> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for property setter callback setups.
	/// </summary>
	extension<T>(IPropertySetterSetupCallbackWhenBuilder<T> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setups of get-only properties.
	/// </summary>
	extension<T>(IPropertyGetterOnlySetupReturnWhenBuilder<T> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertyGetterOnlySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for getter callback setups of get-only properties.
	/// </summary>
	extension<T>(IPropertyGetterOnlySetupCallbackWhenBuilder<T> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertyGetterOnlySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setter callback setups of set-only properties.
	/// </summary>
	extension<T>(IPropertySetterOnlySetupCallbackWhenBuilder<T> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IPropertySetterOnlySetup<T> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for event subscription callback setups.
	/// </summary>
	extension(IEventSubscriptionSetupCallbackWhenBuilder setup)
	{
		/// <summary>
		///     Terminates the callback sequence by repeating the preceding <c>Do(...)</c> entry forever instead of
		///     cycling back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Do(...)</c> entry.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IEventSetup OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for event unsubscription callback setups.
	/// </summary>
	extension(IEventUnsubscriptionSetupCallbackWhenBuilder setup)
	{
		/// <summary>
		///     Terminates the callback sequence by repeating the preceding <c>Do(...)</c> entry forever instead of
		///     cycling back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Do(...)</c> entry.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IEventSetup OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setups with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerSetupReturnWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer getter callback setups with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerGetterSetupCallbackWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setter callback setups with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerSetterSetupCallbackWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setups with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerSetupReturnWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer getter callback setups with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setter callback setups with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setups with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer getter callback setups with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setter callback setups with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setups with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerSetupReturnWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer getter callback setups with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerGetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for indexer setter callback setups with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerSetterSetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setups of get-only indexers with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for getter callback setups of get-only indexers with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setter callback setups of set-only indexers with one parameter.
	/// </summary>
	extension<TValue, T1>(IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetterOnlySetup<TValue, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setups of get-only indexers with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for getter callback setups of get-only indexers with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setter callback setups of set-only indexers with two parameters.
	/// </summary>
	extension<TValue, T1, T2>(IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetterOnlySetup<TValue, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setups of get-only indexers with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for getter callback setups of get-only indexers with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setter callback setups of set-only indexers with three parameters.
	/// </summary>
	extension<TValue, T1, T2, T3>(IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetterOnlySetup<TValue, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setups of get-only indexers with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerGetterOnlySetupReturnWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for getter callback setups of get-only indexers with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerGetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerGetterOnlySetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for setter callback setups of set-only indexers with four parameters.
	/// </summary>
	extension<TValue, T1, T2, T3, T4>(IIndexerSetterOnlySetupCallbackWhenBuilder<TValue, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IIndexerSetterOnlySetup<TValue, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning <typeparamref name="TReturn" /> without parameters.
	/// </summary>
	extension<TReturn>(IReturnMethodSetupReturnWhenBuilder<TReturn> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning <typeparamref name="TReturn" /> without parameters.
	/// </summary>
	extension<TReturn>(IReturnMethodSetupCallbackWhenBuilder<TReturn> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning <typeparamref name="TReturn" /> with one parameter.
	/// </summary>
	extension<TReturn, T1>(IReturnMethodSetupReturnWhenBuilder<TReturn, T1> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning <typeparamref name="TReturn" /> with one parameter.
	/// </summary>
	extension<TReturn, T1>(IReturnMethodSetupCallbackWhenBuilder<TReturn, T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning <typeparamref name="TReturn" /> with two parameters.
	/// </summary>
	extension<TReturn, T1, T2>(IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning <typeparamref name="TReturn" /> with two parameters.
	/// </summary>
	extension<TReturn, T1, T2>(IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning <typeparamref name="TReturn" /> with three parameters.
	/// </summary>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning <typeparamref name="TReturn" /> with three parameters.
	/// </summary>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning <typeparamref name="TReturn" /> with four parameters.
	/// </summary>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning <typeparamref name="TReturn" /> with four parameters.
	/// </summary>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IReturnMethodSetup<TReturn, T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning void without parameters.
	/// </summary>
	extension(IVoidMethodSetupReturnWhenBuilder setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning void without parameters.
	/// </summary>
	extension(IVoidMethodSetupCallbackWhenBuilder setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning void with one parameter.
	/// </summary>
	extension<T1>(IVoidMethodSetupReturnWhenBuilder<T1> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning void with one parameter.
	/// </summary>
	extension<T1>(IVoidMethodSetupCallbackWhenBuilder<T1> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning void with two parameters.
	/// </summary>
	extension<T1, T2>(IVoidMethodSetupReturnWhenBuilder<T1, T2> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning void with two parameters.
	/// </summary>
	extension<T1, T2>(IVoidMethodSetupCallbackWhenBuilder<T1, T2> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning void with three parameters.
	/// </summary>
	extension<T1, T2, T3>(IVoidMethodSetupReturnWhenBuilder<T1, T2, T3> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning void with three parameters.
	/// </summary>
	extension<T1, T2, T3>(IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2, T3> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method setup returning void with four parameters.
	/// </summary>
	extension<T1, T2, T3, T4>(IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Terminates the return/throw sequence by repeating the preceding entry forever instead of cycling
		///     back to the first entry once the end is reached.
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.For(int.MaxValue)</c>. Applies only to the preceding <c>Returns(...)</c>/<c>Throws(...)</c>
		///     entry; earlier entries in the sequence still run once each in order.
		/// </remarks>
		public void Forever()
			=> setup.For(int.MaxValue);

		/// <summary>
		///     Deactivates the preceding <c>Returns(...)</c>/<c>Throws(...)</c> entry after a single invocation,
		///     so subsequent invocations fall through to the next sequence entry (or to the mock's default behaviour).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}

	/// <summary>
	///     Extensions for method callback setup returning void with four parameters.
	/// </summary>
	extension<T1, T2, T3, T4>(IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Deactivates the preceding <c>Do(...)</c> callback after a single invocation, so subsequent invocations
		///     fall through to the next callback in the sequence (or are skipped).
		/// </summary>
		/// <remarks>
		///     Equivalent to <c>.Only(1)</c>.
		/// </remarks>
		public IVoidMethodSetup<T1, T2, T3, T4> OnlyOnce()
			=> setup.Only(1);
	}
}
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
