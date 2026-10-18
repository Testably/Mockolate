using System;
using System.Threading.Tasks;
using Mockolate.Setup;

namespace Mockolate;

#pragma warning disable S2436 // Types and methods should not have too many generic parameters
/// <summary>
///     Extensions for setting up return values and throwing exceptions for <see langword="async" /> methods.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
public static class ReturnsThrowsAsyncExtensions
{
	/// <summary>
	///     Extensions for method setups returning a <see cref="Task{TResult}" /> without parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn>(IReturnMethodSetup<Task<TReturn>> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="Task{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(Task.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => Task.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="Task{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>> ThrowsAsync(Exception exception)
			=> setup.Returns(Task.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => Task.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="Task{TResult}" /> with one parameter.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1>(IReturnMethodSetup<Task<TReturn>, T1> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="Task{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(Task.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => Task.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="Task{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ThrowsAsync(Exception exception)
			=> setup.Returns(Task.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => Task.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="Task{TResult}" /> with one parameter.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1>(IReturnMethodSetupWithCallback<Task<TReturn>, T1> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's argument and produces the value wrapped in a
		///     completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ReturnsAsync(Func<T1, TReturn> callback)
			=> setup.Returns(v1 => Task.FromResult(callback(v1)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's argument to build the
		///     exception the returned <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1> ThrowsAsync(Func<T1, Exception> callback)
			=> setup.Returns(p1 => Task.FromException<TReturn>(callback(p1)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="Task{TResult}" /> with two parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2>(IReturnMethodSetup<Task<TReturn>, T1, T2> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="Task{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(Task.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => Task.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="Task{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ThrowsAsync(Exception exception)
			=> setup.Returns(Task.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => Task.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="Task{TResult}" /> with two parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2>(IReturnMethodSetupWithCallback<Task<TReturn>, T1, T2> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ReturnsAsync(Func<T1, T2, TReturn> callback)
			=> setup.Returns((v1, v2) => Task.FromResult(callback(v1, v2)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2> ThrowsAsync(Func<T1, T2, Exception> callback)
			=> setup.Returns((p1, p2) => Task.FromException<TReturn>(callback(p1, p2)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="Task{TResult}" /> with three parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetup<Task<TReturn>, T1, T2, T3> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="Task{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(Task.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => Task.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="Task{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ThrowsAsync(Exception exception)
			=> setup.Returns(Task.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => Task.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="Task{TResult}" /> with three parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetupWithCallback<Task<TReturn>, T1, T2, T3> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ReturnsAsync(Func<T1, T2, T3, TReturn> callback)
			=> setup.Returns((v1, v2, v3) => Task.FromResult(callback(v1, v2, v3)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3> ThrowsAsync(Func<T1, T2, T3, Exception> callback)
			=> setup.Returns((p1, p2, p3) => Task.FromException<TReturn>(callback(p1, p2, p3)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="Task{TResult}" /> with four parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetup<Task<TReturn>, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="Task{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(Task.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => Task.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="Task{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ThrowsAsync(Exception exception)
			=> setup.Returns(Task.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => Task.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="Task{TResult}" /> with four parameters.
	/// </summary>
	/// <typeparam name="TReturn">The task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetupWithCallback<Task<TReturn>, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="Task{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ReturnsAsync(Func<T1, T2, T3, T4, TReturn> callback)
			=> setup.Returns((v1, v2, v3, v4) => Task.FromResult(callback(v1, v2, v3, v4)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="Task{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<Task<TReturn>, T1, T2, T3, T4> ThrowsAsync(Func<T1, T2, T3, T4, Exception> callback)
			=> setup.Returns((p1, p2, p3, p4) => Task.FromException<TReturn>(callback(p1, p2, p3, p4)));
	}

#if NET8_0_OR_GREATER
#pragma warning disable CA2012 // Use ValueTasks correctly
	/// <summary>
	///     Extensions for method setups returning a <see cref="ValueTask{TResult}" /> without parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn>(IReturnMethodSetup<ValueTask<TReturn>> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="ValueTask{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(ValueTask.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => ValueTask.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="ValueTask{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>> ThrowsAsync(Exception exception)
			=> setup.Returns(ValueTask.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => ValueTask.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="ValueTask{TResult}" /> with one parameter.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1>(IReturnMethodSetup<ValueTask<TReturn>, T1> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="ValueTask{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(ValueTask.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => ValueTask.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="ValueTask{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ThrowsAsync(Exception exception)
			=> setup.Returns(ValueTask.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => ValueTask.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="ValueTask{TResult}" /> with one parameter.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1>(IReturnMethodSetupWithCallback<ValueTask<TReturn>, T1> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's argument and produces the value wrapped in a
		///     completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ReturnsAsync(Func<T1, TReturn> callback)
			=> setup.Returns(v1 => ValueTask.FromResult(callback(v1)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's argument to build the
		///     exception the returned <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1> ThrowsAsync(Func<T1, Exception> callback)
			=> setup.Returns(p1 => ValueTask.FromException<TReturn>(callback(p1)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="ValueTask{TResult}" /> with two parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2>(IReturnMethodSetup<ValueTask<TReturn>, T1, T2> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="ValueTask{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(ValueTask.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => ValueTask.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="ValueTask{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ThrowsAsync(Exception exception)
			=> setup.Returns(ValueTask.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => ValueTask.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="ValueTask{TResult}" /> with two parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2>(IReturnMethodSetupWithCallback<ValueTask<TReturn>, T1, T2> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ReturnsAsync(Func<T1, T2, TReturn> callback)
			=> setup.Returns((v1, v2) => ValueTask.FromResult(callback(v1, v2)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2> ThrowsAsync(Func<T1, T2, Exception> callback)
			=> setup.Returns((p1, p2) => ValueTask.FromException<TReturn>(callback(p1, p2)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="ValueTask{TResult}" /> with three parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetup<ValueTask<TReturn>, T1, T2, T3> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="ValueTask{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(ValueTask.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => ValueTask.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="ValueTask{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ThrowsAsync(Exception exception)
			=> setup.Returns(ValueTask.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => ValueTask.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="ValueTask{TResult}" /> with three parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3>(IReturnMethodSetupWithCallback<ValueTask<TReturn>, T1, T2, T3> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ReturnsAsync(Func<T1, T2, T3, TReturn> callback)
			=> setup.Returns((v1, v2, v3) => ValueTask.FromResult(callback(v1, v2, v3)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3> ThrowsAsync(Func<T1, T2, T3, Exception> callback)
			=> setup.Returns((p1, p2, p3) => ValueTask.FromException<TReturn>(callback(p1, p2, p3)));
	}

	/// <summary>
	///     Extensions for method setups returning a <see cref="ValueTask{TResult}" /> with four parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetup<ValueTask<TReturn>, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns a completed
		///     <see cref="ValueTask{TReturn}" /> carrying this value.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="returnValue">The value returned on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ReturnsAsync(TReturn returnValue)
			=> setup.Returns(ValueTask.FromResult(returnValue));

		/// <summary>
		///     Appends a lazy async return to the sequence; <paramref name="callback" /> is invoked on each matching
		///     invocation and its result is wrapped in a completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ReturnsAsync(Func<TReturn> callback)
			=> setup.Returns(() => ValueTask.FromResult(callback()));

		/// <summary>
		///     Appends an entry that faults the returned <see cref="ValueTask{TReturn}" /> with <paramref name="exception" />
		///     so awaiting it throws.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="exception">The exception to throw on the next matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ThrowsAsync(Exception exception)
			=> setup.Returns(ValueTask.FromException<TReturn>(exception));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> to build the exception the returned
		///     <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ThrowsAsync(Func<Exception> callback)
			=> setup.Returns(() => ValueTask.FromException<TReturn>(callback()));
	}

	/// <summary>
	///     Extensions for method callback setups returning a <see cref="ValueTask{TResult}" /> with four parameters.
	/// </summary>
	/// <typeparam name="TReturn">The value-task result type produced by the asynchronous method.</typeparam>
	/// <typeparam name="T1">The type of the first method argument.</typeparam>
	/// <typeparam name="T2">The type of the second method argument.</typeparam>
	/// <typeparam name="T3">The type of the third method argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth method argument.</typeparam>
	/// <param name="setup">The method setup to extend.</param>
	extension<TReturn, T1, T2, T3, T4>(IReturnMethodSetupWithCallback<ValueTask<TReturn>, T1, T2, T3, T4> setup)
	{
		/// <summary>
		///     Appends a lazy async return that receives the method's arguments and produces the value wrapped in a
		///     completed <see cref="ValueTask{TReturn}" />.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ReturnsAsync(Func<T1, T2, T3, T4, TReturn> callback)
			=> setup.Returns((v1, v2, v3, v4) => ValueTask.FromResult(callback(v1, v2, v3, v4)));

		/// <summary>
		///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
		///     exception the returned <see cref="ValueTask{TReturn}" /> is faulted with.
		/// </summary>
		/// <remarks>
		///     Call <c>ReturnsAsync</c>/<c>ThrowsAsync</c> multiple times to build a sequence; once exhausted it cycles
		///     back to the first entry unless the last one is followed by <c>.Forever()</c>.
		/// </remarks>
		/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
		/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
		public IReturnMethodSetupReturnBuilder<ValueTask<TReturn>, T1, T2, T3, T4> ThrowsAsync(Func<T1, T2, T3, T4, Exception> callback)
			=> setup.Returns((p1, p2, p3, p4) => ValueTask.FromException<TReturn>(callback(p1, p2, p3, p4)));
	}
#pragma warning restore CA2012 // Use ValueTasks correctly
#endif
}
#pragma warning restore S2436 // Types and methods should not have too many generic parameters
