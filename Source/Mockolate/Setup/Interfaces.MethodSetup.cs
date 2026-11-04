using System;
using Mockolate.Interactions;

namespace Mockolate.Setup;

/// <summary>
///     Marker interface for method setups.
/// </summary>
public interface IMethodSetup : ISetup
{
	/// <summary>
	///     The name of the method.
	/// </summary>
	string Name { get; }
}

/// <summary>
///     Interface for verifiable method setup.
/// </summary>
public interface IVerifiableMethodSetup
{
	/// <summary>
	///     Checks if the setup matches the method invocations.
	/// </summary>
	bool Matches(IMethodInteraction interaction);
}

/// <summary>
///     Setup for a parameterless mocked method that returns <typeparamref name="TReturn" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName()</c>. Chain <see cref="Returns(TReturn)" /> /
///     <see cref="Throws{TException}" /> to define a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive <c>Returns</c>/<c>Throws</c>/<c>Do</c> calls build a sequence that cycles once
///     exhausted - terminate with <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c>
///     / <c>.When(predicate)</c> on the builders to control repetition and gating.
///     <para />
///     For async methods, use the <c>.ReturnsAsync(...)</c> / <c>.ThrowsAsync(...)</c> overloads from
///     <see cref="ReturnsThrowsAsyncExtensions" /> instead of wrapping a <see cref="System.Threading.Tasks.Task" /> in
///     <c>.Returns(...)</c>.
/// </remarks>
public interface IReturnMethodSetup<in TReturn> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs and its return value is used as the default value for un-configured invocations; when
	///     <see langword="true" /> the base-class implementation is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn> Do(Action callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn> Do(Action<int> callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn> TransitionTo(string scenario);

	/// <summary>
	///     Appends a lazy return to the sequence; <paramref name="callback" /> is invoked on each matching invocation
	///     and its result is returned.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn> Returns(Func<TReturn> callback);

	/// <summary>
	///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns this value.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="returnValue">The value returned on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn> Returns(TReturn returnValue);

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn> Throws(Func<Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackBuilder<in TReturn>
	: IReturnMethodSetupParallelCallbackBuilder<TReturn>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupParallelCallbackBuilder<in TReturn>
	: IReturnMethodSetupCallbackWhenBuilder<TReturn>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackWhenBuilder<in TReturn>
	: IReturnMethodSetup<TReturn>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn> Only(int times);
}

/// <summary>
///     Sets up a return/throw builder for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnBuilder<in TReturn>
	: IReturnMethodSetupReturnWhenBuilder<TReturn>
{
	/// <summary>
	///     Limits the return/throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for returns/throws for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnWhenBuilder<in TReturn>
	: IReturnMethodSetup<TReturn>
{
	/// <summary>
	///     Repeats the return/throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn> Only(int times);
}

/// <summary>
///     Setup for a mocked method with one parameter that returns <typeparamref name="TReturn" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Returns(TReturn)" /> /
///     <see cref="Throws{TException}" /> to define a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive <c>Returns</c>/<c>Throws</c>/<c>Do</c> calls build a sequence that cycles once
///     exhausted - terminate with <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c>
///     / <c>.When(predicate)</c> on the builders to control repetition and gating.
///     <para />
///     For async methods, use the <c>.ReturnsAsync(...)</c> / <c>.ThrowsAsync(...)</c> overloads from
///     <see cref="ReturnsThrowsAsyncExtensions" /> instead of wrapping a <see cref="System.Threading.Tasks.Task" /> in
///     <c>.Returns(...)</c>.
/// </remarks>
public interface IReturnMethodSetup<in TReturn, out T1> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs and its return value is used as the default value for un-configured invocations; when
	///     <see langword="true" /> the base-class implementation is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1> TransitionTo(string scenario);

	/// <summary>
	///     Appends a lazy return to the sequence; <paramref name="callback" /> is invoked on each matching invocation
	///     and its result is returned.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Returns(Func<TReturn> callback);

	/// <summary>
	///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns this value.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="returnValue">The value returned on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Returns(TReturn returnValue);

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IReturnMethodSetup{TReturn, T1}" /> - <c>Do</c>, <c>Returns</c>
///     and <c>Throws</c> variants whose callbacks receive the method's argument.
/// </summary>
public interface IReturnMethodSetupWithCallback<in TReturn, out T1> : IReturnMethodSetup<TReturn, T1>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's argument whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1> Do(Action<T1> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's argument.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1> Do(Action<int, T1> callback);

	/// <summary>
	///     Appends a lazy return that receives the method's argument and produces the value to return.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Returns(Func<T1, TReturn> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's argument to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1> Throws(Func<T1, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackBuilder<in TReturn, out T1>
	: IReturnMethodSetupParallelCallbackBuilder<TReturn, T1>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1> InParallel();
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupParallelCallbackBuilder<in TReturn, out T1>
	: IReturnMethodSetupCallbackWhenBuilder<TReturn, T1>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackWhenBuilder<in TReturn, out T1>
	: IReturnMethodSetupWithCallback<TReturn, T1>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1> Only(int times);
}

/// <summary>
///     Sets up a return/throw builder for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnBuilder<in TReturn, out T1>
	: IReturnMethodSetupReturnWhenBuilder<TReturn, T1>
{
	/// <summary>
	///     Limits the return/throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for returns/throws for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnWhenBuilder<in TReturn, out T1>
	: IReturnMethodSetupWithCallback<TReturn, T1>
{
	/// <summary>
	///     Repeats the return/throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1> For(int times);

	/// <summary>
	///     Deactivates the return/throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IReturnMethodSetupParameterIgnorer<in TReturn, out T1>
	: IReturnMethodSetupWithCallback<TReturn, T1>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1> AnyParameters();
}

/// <summary>
///     Setup for a mocked method with two parameters that returns <typeparamref name="TReturn" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Returns(TReturn)" /> /
///     <see cref="Throws{TException}" /> to define a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive <c>Returns</c>/<c>Throws</c>/<c>Do</c> calls build a sequence that cycles once
///     exhausted - terminate with <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c>
///     / <c>.When(predicate)</c> on the builders to control repetition and gating.
///     <para />
///     For async methods, use the <c>.ReturnsAsync(...)</c> / <c>.ThrowsAsync(...)</c> overloads from
///     <see cref="ReturnsThrowsAsyncExtensions" /> instead of wrapping a <see cref="System.Threading.Tasks.Task" /> in
///     <c>.Returns(...)</c>.
/// </remarks>
public interface IReturnMethodSetup<in TReturn, out T1, out T2> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs and its return value is used as the default value for un-configured invocations; when
	///     <see langword="true" /> the base-class implementation is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2> TransitionTo(string scenario);

	/// <summary>
	///     Appends a lazy return to the sequence; <paramref name="callback" /> is invoked on each matching invocation
	///     and its result is returned.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Returns(Func<TReturn> callback);

	/// <summary>
	///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns this value.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="returnValue">The value returned on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Returns(TReturn returnValue);

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IReturnMethodSetup{TReturn, T1, T2}" /> - <c>Do</c>, <c>Returns</c>
///     and <c>Throws</c> variants whose callbacks receive the method's arguments.
/// </summary>
public interface IReturnMethodSetupWithCallback<in TReturn, out T1, out T2> : IReturnMethodSetup<TReturn, T1, T2>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> Do(Action<T1, T2> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2> Do(Action<int, T1, T2> callback);

	/// <summary>
	///     Appends a lazy return that receives the method's arguments and produces the value to return.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Returns(Func<T1, T2, TReturn> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2> Throws(Func<T1, T2, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackBuilder<in TReturn, out T1, out T2>
	: IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupParallelCallbackBuilder<in TReturn, out T1, out T2>
	: IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackWhenBuilder<in TReturn, out T1, out T2>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2> Only(int times);
}

/// <summary>
///     Sets up a return/throw builder for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnBuilder<in TReturn, out T1, out T2>
	: IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2>
{
	/// <summary>
	///     Limits the return/throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for returns/throws for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnWhenBuilder<in TReturn, out T1, out T2>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2>
{
	/// <summary>
	///     Repeats the return/throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2> For(int times);

	/// <summary>
	///     Deactivates the return/throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IReturnMethodSetupParameterIgnorer<in TReturn, out T1, out T2>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2> AnyParameters();
}

/// <summary>
///     Setup for a mocked method with three parameters that returns
///     <typeparamref name="TReturn" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Returns(TReturn)" /> /
///     <see cref="Throws{TException}" /> to define a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive <c>Returns</c>/<c>Throws</c>/<c>Do</c> calls build a sequence that cycles once
///     exhausted - terminate with <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c>
///     / <c>.When(predicate)</c> on the builders to control repetition and gating.
///     <para />
///     For async methods, use the <c>.ReturnsAsync(...)</c> / <c>.ThrowsAsync(...)</c> overloads from
///     <see cref="ReturnsThrowsAsyncExtensions" /> instead of wrapping a <see cref="System.Threading.Tasks.Task" /> in
///     <c>.Returns(...)</c>.
/// </remarks>
public interface IReturnMethodSetup<in TReturn, out T1, out T2, out T3> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs and its return value is used as the default value for un-configured invocations; when
	///     <see langword="true" /> the base-class implementation is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3> TransitionTo(string scenario);

	/// <summary>
	///     Appends a lazy return to the sequence; <paramref name="callback" /> is invoked on each matching invocation
	///     and its result is returned.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Returns(Func<TReturn> callback);

	/// <summary>
	///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns this value.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="returnValue">The value returned on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Returns(TReturn returnValue);

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IReturnMethodSetup{TReturn, T1, T2, T3}" /> - <c>Do</c>,
///     <c>Returns</c> and <c>Throws</c> variants whose callbacks receive the method's arguments.
/// </summary>
public interface IReturnMethodSetupWithCallback<in TReturn, out T1, out T2, out T3> : IReturnMethodSetup<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> Do(Action<T1, T2, T3> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3> Do(Action<int, T1, T2, T3> callback);

	/// <summary>
	///     Appends a lazy return that receives the method's arguments and produces the value to return.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Returns(Func<T1, T2, T3, TReturn> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3> Throws(Func<T1, T2, T3, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackBuilder<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupParallelCallbackBuilder<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackWhenBuilder<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3> Only(int times);
}

/// <summary>
///     Sets up a return/throw builder for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnBuilder<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Limits the return/throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for returns/throws for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnWhenBuilder<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Repeats the return/throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3> For(int times);

	/// <summary>
	///     Deactivates the return/throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IReturnMethodSetupParameterIgnorer<in TReturn, out T1, out T2, out T3>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3> AnyParameters();
}

#pragma warning disable S2436 // Types and methods should not have too many generic parameters
/// <summary>
///     Setup for a mocked method with four parameters that returns
///     <typeparamref name="TReturn" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Returns(TReturn)" /> /
///     <see cref="Throws{TException}" /> to define a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive <c>Returns</c>/<c>Throws</c>/<c>Do</c> calls build a sequence that cycles once
///     exhausted - terminate with <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c>
///     / <c>.When(predicate)</c> on the builders to control repetition and gating.
///     <para />
///     For async methods, use the <c>.ReturnsAsync(...)</c> / <c>.ThrowsAsync(...)</c> overloads from
///     <see cref="ReturnsThrowsAsyncExtensions" /> instead of wrapping a <see cref="System.Threading.Tasks.Task" /> in
///     <c>.Returns(...)</c>.
/// </remarks>
public interface IReturnMethodSetup<in TReturn, out T1, out T2, out T3, out T4> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs and its return value is used as the default value for un-configured invocations; when
	///     <see langword="true" /> the base-class implementation is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4> TransitionTo(string scenario);

	/// <summary>
	///     Appends a lazy return to the sequence; <paramref name="callback" /> is invoked on each matching invocation
	///     and its result is returned.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation to produce the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Returns(Func<TReturn> callback);

	/// <summary>
	///     Appends <paramref name="returnValue" /> to the sequence - the next matching invocation returns this value.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="returnValue">The value returned on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Returns(TReturn returnValue);

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IReturnMethodSetup{TReturn, T1, T2, T3, T4}" /> - <c>Do</c>,
///     <c>Returns</c> and <c>Throws</c> variants whose callbacks receive the method's arguments.
/// </summary>
public interface IReturnMethodSetupWithCallback<in TReturn, out T1, out T2, out T3, out T4> : IReturnMethodSetup<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> Do(Action<T1, T2, T3, T4> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IReturnMethodSetupCallbackBuilder<TReturn, T1, T2, T3, T4> Do(Action<int, T1, T2, T3, T4> callback);

	/// <summary>
	///     Appends a lazy return that receives the method's arguments and produces the value to return.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory invoked on every matching invocation; receives the method arguments and produces the return value.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Returns(Func<T1, T2, T3, T4, TReturn> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Returns</c>/<c>Throws</c> multiple times to build a sequence; once exhausted it cycles back to the
	///     first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IReturnMethodSetupReturnBuilder<TReturn, T1, T2, T3, T4> Throws(Func<T1, T2, T3, T4, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackBuilder<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupParallelCallbackBuilder<TReturn, T1, T2, T3, T4> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupParallelCallbackBuilder<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupCallbackWhenBuilder<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupCallbackWhenBuilder<TReturn, T1, T2, T3, T4> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupParallelCallbackBuilder{TReturn}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> Only(int times);
}

/// <summary>
///     Sets up a return/throw builder for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnBuilder<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Limits the return/throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for returns/throws for a method returning <typeparamref name="TReturn" />.
/// </summary>
public interface IReturnMethodSetupReturnWhenBuilder<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Repeats the return/throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3, T4}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IReturnMethodSetupReturnWhenBuilder<TReturn, T1, T2, T3, T4> For(int times);

	/// <summary>
	///     Deactivates the return/throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IReturnMethodSetupReturnBuilder{TReturn, T1, T2, T3, T4}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />
	///     ).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IReturnMethodSetupParameterIgnorer<in TReturn, out T1, out T2, out T3, out T4>
	: IReturnMethodSetupWithCallback<TReturn, T1, T2, T3, T4>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IReturnMethodSetup<TReturn, T1, T2, T3, T4> AnyParameters();
}
#pragma warning restore S2436 // Types and methods should not have too many generic parameters

/// <summary>
///     Setup for a parameterless mocked method that returns <see langword="void" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName()</c>. Chain <see cref="Throws{TException}" /> /
///     <see cref="DoesNotThrow" /> to build a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive entries form a sequence that cycles once exhausted - terminate with
///     <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c> /
///     <c>.When(predicate)</c> on the builders to control repetition and gating.
/// </remarks>
public interface IVoidMethodSetup : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs as part of the invocation; when <see langword="true" /> it is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder Do(Action callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder Do(Action<int> callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder TransitionTo(string scenario);

	/// <summary>
	///     Appends a &quot;does-nothing&quot; entry to the sequence - useful between <see cref="Throws{TException}" />
	///     entries to model &quot;throw, succeed, throw&quot; patterns.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup DoesNotThrow();

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder Throws(Func<Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackBuilder : IVoidMethodSetupParallelCallbackBuilder
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupParallelCallbackBuilder : IVoidMethodSetupCallbackWhenBuilder
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackWhenBuilder : IVoidMethodSetup
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup Only(int times);
}

/// <summary>
///     Sets up a throw builder for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnBuilder : IVoidMethodSetupReturnWhenBuilder
{
	/// <summary>
	///     Limits the throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for throws for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnWhenBuilder : IVoidMethodSetup
{
	/// <summary>
	///     Repeats the throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder For(int times);

	/// <summary>
	///     Deactivates the throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup Only(int times);
}

/// <summary>
///     Setup for a mocked method with one parameter that returns <see langword="void" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Throws{TException}" /> /
///     <see cref="DoesNotThrow" /> to build a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive entries form a sequence that cycles once exhausted - terminate with
///     <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c> /
///     <c>.When(predicate)</c> on the builders to control repetition and gating.
/// </remarks>
public interface IVoidMethodSetup<out T1> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs as part of the invocation; when <see langword="true" /> it is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1> TransitionTo(string scenario);

	/// <summary>
	///     Appends a &quot;does-nothing&quot; entry to the sequence - useful between <see cref="Throws{TException}" />
	///     entries to model &quot;throw, succeed, throw&quot; patterns.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1> DoesNotThrow();

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IVoidMethodSetup{T1}" /> - <c>Do</c> and <c>Throws</c> variants
///     whose callbacks receive the method's argument.
/// </summary>
public interface IVoidMethodSetupWithCallback<out T1> : IVoidMethodSetup<T1>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's argument whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1> Do(Action<T1> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's argument.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1> Do(Action<int, T1> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's argument to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1> Throws(Func<T1, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackBuilder<out T1>
	: IVoidMethodSetupParallelCallbackBuilder<T1>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupParallelCallbackBuilder<out T1>
	: IVoidMethodSetupCallbackWhenBuilder<T1>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackWhenBuilder<out T1>
	: IVoidMethodSetupWithCallback<T1>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1> Only(int times);
}

/// <summary>
///     Sets up a throw builder for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnBuilder<out T1>
	: IVoidMethodSetupReturnWhenBuilder<T1>
{
	/// <summary>
	///     Limits the throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for throws for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnWhenBuilder<out T1>
	: IVoidMethodSetupWithCallback<T1>
{
	/// <summary>
	///     Repeats the throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1> For(int times);

	/// <summary>
	///     Deactivates the throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IVoidMethodSetupParameterIgnorer<out T1>
	: IVoidMethodSetupWithCallback<T1>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1> AnyParameters();
}

/// <summary>
///     Setup for a mocked method with two parameters that returns <see langword="void" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Throws{TException}" /> /
///     <see cref="DoesNotThrow" /> to build a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive entries form a sequence that cycles once exhausted - terminate with
///     <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c> /
///     <c>.When(predicate)</c> on the builders to control repetition and gating.
/// </remarks>
public interface IVoidMethodSetup<out T1, out T2> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs as part of the invocation; when <see langword="true" /> it is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2> TransitionTo(string scenario);

	/// <summary>
	///     Appends a &quot;does-nothing&quot; entry to the sequence - useful between <see cref="Throws{TException}" />
	///     entries to model &quot;throw, succeed, throw&quot; patterns.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2> DoesNotThrow();

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IVoidMethodSetup{T1, T2}" /> - <c>Do</c> and <c>Throws</c> variants
///     whose callbacks receive the method's arguments.
/// </summary>
public interface IVoidMethodSetupWithCallback<out T1, out T2> : IVoidMethodSetup<T1, T2>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2> Do(Action<T1, T2> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2> Do(Action<int, T1, T2> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2> Throws(Func<T1, T2, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackBuilder<out T1, out T2>
	: IVoidMethodSetupParallelCallbackBuilder<T1, T2>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupParallelCallbackBuilder<out T1, out T2>
	: IVoidMethodSetupCallbackWhenBuilder<T1, T2>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackWhenBuilder<out T1, out T2>
	: IVoidMethodSetupWithCallback<T1, T2>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2> Only(int times);
}

/// <summary>
///     Sets up a throw builder for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnBuilder<out T1, out T2>
	: IVoidMethodSetupReturnWhenBuilder<T1, T2>
{
	/// <summary>
	///     Limits the throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for throws for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnWhenBuilder<out T1, out T2>
	: IVoidMethodSetupWithCallback<T1, T2>
{
	/// <summary>
	///     Repeats the throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2> For(int times);

	/// <summary>
	///     Deactivates the throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IVoidMethodSetupParameterIgnorer<out T1, out T2>
	: IVoidMethodSetupWithCallback<T1, T2>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2> AnyParameters();
}

/// <summary>
///     Setup for a mocked method with three parameters that returns <see langword="void" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Throws{TException}" /> /
///     <see cref="DoesNotThrow" /> to build a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive entries form a sequence that cycles once exhausted - terminate with
///     <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c> /
///     <c>.When(predicate)</c> on the builders to control repetition and gating.
/// </remarks>
public interface IVoidMethodSetup<out T1, out T2, out T3> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs as part of the invocation; when <see langword="true" /> it is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3> TransitionTo(string scenario);

	/// <summary>
	///     Appends a &quot;does-nothing&quot; entry to the sequence - useful between <see cref="Throws{TException}" />
	///     entries to model &quot;throw, succeed, throw&quot; patterns.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3> DoesNotThrow();

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IVoidMethodSetup{T1, T2, T3}" /> - <c>Do</c> and <c>Throws</c>
///     variants whose callbacks receive the method's arguments.
/// </summary>
public interface IVoidMethodSetupWithCallback<out T1, out T2, out T3> : IVoidMethodSetup<T1, T2, T3>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> Do(Action<T1, T2, T3> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3> Do(Action<int, T1, T2, T3> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3> Throws(Func<T1, T2, T3, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackBuilder<out T1, out T2, out T3>
	: IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupParallelCallbackBuilder<out T1, out T2, out T3>
	: IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackWhenBuilder<out T1, out T2, out T3>
	: IVoidMethodSetupWithCallback<T1, T2, T3>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2, T3> Only(int times);
}

/// <summary>
///     Sets up a throw builder for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnBuilder<out T1, out T2, out T3>
	: IVoidMethodSetupReturnWhenBuilder<T1, T2, T3>
{
	/// <summary>
	///     Limits the throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for throws for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnWhenBuilder<out T1, out T2, out T3>
	: IVoidMethodSetupWithCallback<T1, T2, T3>
{
	/// <summary>
	///     Repeats the throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2, T3}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3> For(int times);

	/// <summary>
	///     Deactivates the throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2, T3}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2, T3> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IVoidMethodSetupParameterIgnorer<out T1, out T2, out T3>
	: IVoidMethodSetupWithCallback<T1, T2, T3>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3> AnyParameters();
}

/// <summary>
///     Setup for a mocked method with four parameters that returns <see langword="void" />.
/// </summary>
/// <remarks>
///     Reached via <c>sut.Mock.Setup.MethodName(...)</c>. Chain <see cref="Throws{TException}" /> /
///     <see cref="DoesNotThrow" /> to build a sequence of responses, and <see cref="Do(Action)" /> to attach
///     side-effects. Consecutive entries form a sequence that cycles once exhausted - terminate with
///     <c>.Forever()</c> to freeze on the last entry, or use <c>.For(n)</c> / <c>.Only(n)</c> /
///     <c>.When(predicate)</c> on the builders to control repetition and gating.
/// </remarks>
public interface IVoidMethodSetup<out T1, out T2, out T3, out T4> : IMethodSetup
{
	/// <summary>
	///     Overrides <see cref="MockBehavior.SkipBaseClass" /> for this method only.
	/// </summary>
	/// <remarks>
	///     Only meaningful for class mocks. When <see langword="false" /> (the default on mocks) the base-class
	///     implementation runs as part of the invocation; when <see langword="true" /> it is skipped entirely.
	/// </remarks>
	/// <param name="skipBaseClass">Whether to skip the base-class implementation. Defaults to <see langword="true" />.</param>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3, T4> SkippingBaseClass(bool skipBaseClass = true);

	/// <summary>
	///     Appends a side-effect callback to the sequence; <paramref name="callback" /> runs whenever the method is
	///     invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> Do(Action callback);

	/// <summary>
	///     Switches the mock's current scenario to <paramref name="scenario" /> whenever the method is invoked - useful
	///     to model state transitions.
	/// </summary>
	/// <param name="scenario">The name of the scenario to transition to.</param>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4> TransitionTo(string scenario);

	/// <summary>
	///     Appends a &quot;does-nothing&quot; entry to the sequence - useful between <see cref="Throws{TException}" />
	///     entries to model &quot;throw, succeed, throw&quot; patterns.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3, T4> DoesNotThrow();

	/// <summary>
	///     Appends an entry that throws a freshly-constructed <typeparamref name="TException" /> on the next matching
	///     invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <typeparam name="TException">The exception type; a new instance is created per invocation.</typeparam>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> Throws<TException>()
		where TException : Exception, new();

	/// <summary>
	///     Appends an entry that throws <paramref name="exception" /> on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="exception">The exception to throw on the next matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> Throws(Exception exception);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> to build the exception thrown on the next
	///     matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> Throws(Func<Exception> callback);
}

/// <summary>
///     Parameter-aware overloads for <see cref="IVoidMethodSetup{T1, T2, T3, T4}" /> - <c>Do</c> and <c>Throws</c>
///     variants whose callbacks receive the method's arguments.
/// </summary>
public interface IVoidMethodSetupWithCallback<out T1, out T2, out T3, out T4> : IVoidMethodSetup<T1, T2, T3, T4>
{
	/// <summary>
	///     Appends a side-effect callback that receives the method's arguments whenever the method is invoked.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> Do(Action<T1, T2, T3, T4> callback);

	/// <summary>
	///     Appends a side-effect callback that receives a zero-based invocation counter and the method's arguments.
	/// </summary>
	/// <param name="callback">The action to invoke on every matching invocation; receives a zero-based invocation counter and the method arguments.</param>
	/// <returns>A builder for chaining repetition/gating operators.</returns>
	IVoidMethodSetupCallbackBuilder<T1, T2, T3, T4> Do(Action<int, T1, T2, T3, T4> callback);

	/// <summary>
	///     Appends an entry that invokes <paramref name="callback" /> with the method's arguments to build the
	///     exception thrown on the next matching invocation.
	/// </summary>
	/// <remarks>
	///     Call <c>Throws</c>/<c>DoesNotThrow</c> multiple times to build a sequence; once exhausted it cycles back
	///     to the first entry unless the last one is followed by <c>.Forever()</c>.
	/// </remarks>
	/// <param name="callback">The factory that produces the exception thrown on every matching invocation.</param>
	/// <returns>A builder for chaining additional returns/throws or gating operators.</returns>
	IVoidMethodSetupReturnBuilder<T1, T2, T3, T4> Throws(Func<T1, T2, T3, T4, Exception> callback);
}

/// <summary>
///     Sets up a callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackBuilder<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4>
{
	/// <summary>
	///     Runs the callback in parallel to the other callbacks.
	/// </summary>
	/// <returns>A builder for chaining <c>.When(...)</c>, <c>.For(n)</c>, or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupParallelCallbackBuilder<T1, T2, T3, T4> InParallel();
}

/// <summary>
///     Sets up a parallel callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupParallelCallbackBuilder<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4>
{
	/// <summary>
	///     Limits the callback to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when callback for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupCallbackWhenBuilder<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupWithCallback<T1, T2, T3, T4>
{
	/// <summary>
	///     Repeats the callback for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupCallbackWhenBuilder<T1, T2, T3, T4> For(int times);

	/// <summary>
	///     Deactivates the callback after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupParallelCallbackBuilder.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2, T3, T4> Only(int times);
}

/// <summary>
///     Sets up a throw builder for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnBuilder<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4>
{
	/// <summary>
	///     Limits the throw to only execute for method invocations where the predicate returns true.
	/// </summary>
	/// <remarks>
	///     Provides a zero-based counter indicating how many times the method has been invoked so far.
	/// </remarks>
	/// <param name="predicate">A predicate receiving a zero-based counter of how many times the method has been invoked so far.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4> When(Func<int, bool> predicate);
}

/// <summary>
///     Sets up a when builder for throws for a method returning <see langword="void" />.
/// </summary>
public interface IVoidMethodSetupReturnWhenBuilder<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupWithCallback<T1, T2, T3, T4>
{
	/// <summary>
	///     Repeats the throw for the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2, T3, T4}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions before the callback cycles back to the first entry in the sequence.</param>
	/// <returns>A builder for chaining <c>.For(n)</c> or <c>.Only(n)</c>.</returns>
	IVoidMethodSetupReturnWhenBuilder<T1, T2, T3, T4> For(int times);

	/// <summary>
	///     Deactivates the throw after the given number of <paramref name="times" />.
	/// </summary>
	/// <remarks>
	///     The number of times is only counted for actual executions (
	///     <see cref="IVoidMethodSetupReturnBuilder{T1, T2, T3, T4}.When(Func{int, bool})" /> evaluates to
	///     <see langword="true" />).
	/// </remarks>
	/// <param name="times">The number of executions after which the callback stops firing.</param>
	/// <returns>The outer setup for chaining additional returns/throws.</returns>
	IVoidMethodSetup<T1, T2, T3, T4> Only(int times);
}

/// <summary>
///     Allows ignoring the provided parameters.
/// </summary>
public interface IVoidMethodSetupParameterIgnorer<out T1, out T2, out T3, out T4>
	: IVoidMethodSetupWithCallback<T1, T2, T3, T4>
{
	/// <summary>
	///     Replaces the explicit parameter matcher with <see cref="Match.AnyParameters()" />.
	/// </summary>
	/// <returns>The same setup instance, to allow chaining.</returns>
	IVoidMethodSetup<T1, T2, T3, T4> AnyParameters();
}
