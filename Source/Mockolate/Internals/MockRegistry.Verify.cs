using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Verify;

namespace Mockolate.Internals;

public partial class MockRegistry
{
	/// <summary>
	///     Counts invocations of the method named <paramref name="methodName" /> on <paramref name="subject" /> whose
	///     recorded interaction satisfies <paramref name="predicate" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type; returned to the caller so chaining can continue.</typeparam>
	/// <typeparam name="TMethod">The concrete <see cref="IMethodInteraction" /> subtype to filter on.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="predicate">Argument predicate applied to each matching interaction.</param>
	/// <param name="expectation">Factory producing the expectation description used in failure messages.</param>
	/// <returns>A verification result exposing <c>AnyParameters()</c> in addition to the usual terminators.</returns>
	internal VerificationResult<T>.IgnoreParameters VerifyMethod<T, TMethod>(T subject, string methodName, Func<TMethod, bool> predicate, Func<string> expectation) where TMethod : IMethodInteraction
	{
		return new VerificationResult<T>.IgnoreParameters(
			subject,
			Interactions,
			methodName,
			Predicate,
			OverloadFilter,
			InvokedMethodExpectation(expectation));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is TMethod method && predicate(method);
		}

		[DebuggerNonUserCode]
		static bool OverloadFilter(IInteraction interaction)
		{
			return interaction is TMethod;
		}
	}

	/// <summary>
	///     Member-id-keyed fast-path overload of
	///     <see cref="VerifyMethod{T, TMethod}(T, string, Func{TMethod, bool}, Func{string})" />. When the mock is
	///     wired to a <see cref="MockInteractions" /> with typed records of the member <paramref name="memberId" />, the
	///     resulting <see cref="VerificationResult{T}.IgnoreParameters" /> walks only those records instead of the
	///     full shared interaction list. Falls back to the legacy full-list scan otherwise.
	/// </summary>
	/// <typeparam name="T">The verification facade type; returned to the caller so chaining can continue.</typeparam>
	/// <typeparam name="TMethod">The concrete <see cref="IMethodInteraction" /> subtype to filter on.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="predicate">Argument predicate applied to each matching interaction.</param>
	/// <param name="expectation">Factory producing the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, TMethod>(T subject, int memberId, string methodName, Func<TMethod, bool> predicate, Func<string> expectation) where TMethod : IMethodInteraction
	{
		MemberRecords? records = TryGetRecords(memberId);
		if (records is null)
		{
			return VerifyMethod(subject, methodName, predicate, expectation);
		}

		return new VerificationResult<T>.IgnoreParameters(
			subject,
			Interactions,
			records,
			methodName,
			Predicate,
			InvokedMethodExpectation(expectation));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is TMethod method && predicate(method);
		}
	}

	/// <summary>
	///     Counts invocations of the generic method <paramref name="genericMethodName" /> with
	///     <paramref name="parameterCount" /> parameters, for any type argument, whose arguments satisfy
	///     <paramref name="predicate" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type; returned to the caller so chaining can continue.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="genericMethodName">
	///     The name of the open generic method with a comma for each further type parameter (e.g.
	///     <c>"Send&lt;,&gt;"</c>).
	/// </param>
	/// <param name="parameterCount">The number of parameters of the method.</param>
	/// <param name="isCallOf">
	///     Recognizes the calls of the method, when other overloads have the same name and as many type parameters and
	///     parameters; otherwise <see langword="null" />.
	/// </param>
	/// <param name="predicate">Argument predicate applied to each call of the method.</param>
	/// <param name="expectation">Factory producing the expectation description used in failure messages.</param>
	public VerificationResult<T> VerifyGenericMethod<T>(T subject, string genericMethodName, int parameterCount,
		Func<IMethodInvocationParameters, bool>? isCallOf, Func<IMethodInvocationParameters, bool> predicate,
		Func<string> expectation)
	{
		return new VerificationResult<T>(subject, Interactions, Predicate, InvokedMethodExpectation(expectation));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return GenericMethodInvocations.TryGetInvocation(interaction, genericMethodName, parameterCount,
				       isCallOf, out IMethodInvocationParameters invocation) &&
			       predicate(invocation);
		}
	}

	/// <summary>
	///     Counts getter accesses of the property named <paramref name="propertyName" /> on <paramref name="subject" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="propertyName">The simple property name.</param>
	/// <returns>A verification result pending a count terminator (e.g. <c>.Once()</c>).</returns>
	internal VerificationResult<T> VerifyProperty<T>(T subject, string propertyName)
	{
		return new VerificationResult<T>(subject,
			Interactions,
			Predicate,
			GotPropertyExpectation(propertyName));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is PropertyGetterAccess property &&
			       property.Name.Equals(propertyName);
		}
	}

	/// <summary>
	///     Counts setter accesses of the property named <paramref name="propertyName" /> on <paramref name="subject" />
	///     where the assigned value matches <paramref name="value" />.
	/// </summary>
	/// <typeparam name="TSubject">The verification facade type.</typeparam>
	/// <typeparam name="TValue">The property's type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="propertyName">The simple property name.</param>
	/// <param name="value">Parameter matcher evaluated against the assigned value.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	internal VerificationResult<TSubject> VerifyProperty<TSubject, TValue>(TSubject subject, string propertyName,
		IParameterMatch<TValue> value)
	{
		return new VerificationResult<TSubject>(subject,
			Interactions,
			Predicate,
			SetPropertyExpectation(propertyName, value));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is PropertySetterAccess<TValue> property &&
			       property.Name.Equals(propertyName) &&
			       value.Matches(property.Value);
		}
	}

	/// <summary>
	///     Counts invocations on <paramref name="subject" /> that match the <paramref name="methodSetup" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="methodSetup">An existing method setup. Must also implement <see cref="IVerifiableMethodSetup" />.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	/// <exception cref="MockException"><paramref name="methodSetup" /> does not implement <see cref="IVerifiableMethodSetup" />.</exception>
	public VerificationResult<T> Method<T>(T subject, IMethodSetup methodSetup)
	{
		if (methodSetup is not IVerifiableMethodSetup verifiableMethodSetup)
		{
			throw new MockException("The setup is not verifiable.");
		}

		if (methodSetup is IGenericMethodSetup)
		{
			return new VerificationResult<T>(subject, Interactions, Predicate, InvokedMethodSetupExpectation(methodSetup));
		}

		return new VerificationResult<T>.IgnoreParameters(
			subject,
			Interactions,
			methodSetup.Name,
			Predicate,
			InvokedMethodSetupExpectation(methodSetup));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is IMethodInteraction methodInteraction &&
			       verifiableMethodSetup.Matches(methodInteraction);
		}
	}

	/// <summary>
	///     Counts indexer getter accesses on <paramref name="subject" /> whose recorded interaction satisfies
	///     <paramref name="gotPredicate" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="gotPredicate">Predicate evaluated against each recorded interaction.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	internal VerificationResult<T> IndexerGot<T>(T subject,
		Func<IInteraction, bool> gotPredicate,
		Func<string> parametersDescription)
		=> new(subject,
			Interactions,
			gotPredicate,
			GotIndexerExpectation(parametersDescription));

	/// <summary>
	///     Member-id-keyed fast-path overload of
	///     <see cref="IndexerGot{T}(T, Func{IInteraction, bool}, Func{string})" />. Walks only the typed indexer
	///     getter records when the mock is wired to a <see cref="MockInteractions" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the indexer getter.</param>
	/// <param name="gotPredicate">Predicate evaluated against each recorded interaction.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationResult<T> IndexerGot<T>(T subject, int memberId,
		Func<IInteraction, bool> gotPredicate,
		Func<string> parametersDescription)
		=> new(subject,
			Interactions,
			TryGetRecords(memberId),
			gotPredicate,
			GotIndexerExpectation(parametersDescription));

	/// <summary>
	///     Counts indexer setter accesses on <paramref name="subject" /> where the recorded interaction matches
	///     <paramref name="setPredicate" /> and the assigned value matches <paramref name="value" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="setPredicate">Predicate evaluated against each recorded interaction and the expected value matcher.</param>
	/// <param name="value">Parameter matcher evaluated against the assigned value.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	internal VerificationResult<T> IndexerSet<T, TValue>(T subject,
		Func<IInteraction, IParameterMatch<TValue>, bool> setPredicate,
		IParameterMatch<TValue> value,
		Func<string> parametersDescription)
	{
		return new VerificationResult<T>(subject,
			Interactions,
			Predicate,
			SetIndexerExpectation(parametersDescription, value));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return setPredicate(interaction, value);
		}
	}

	/// <summary>
	///     Member-id-keyed fast-path overload of
	///     <see cref="IndexerSet{T, TValue}(T, Func{IInteraction, IParameterMatch{TValue}, bool}, IParameterMatch{TValue}, Func{string})" />.
	///     Walks only the typed indexer setter records when the mock is wired to a <see cref="MockInteractions" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <typeparam name="TValue">The indexer's value type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the indexer setter.</param>
	/// <param name="setPredicate">Predicate evaluated against each recorded interaction and the expected value matcher.</param>
	/// <param name="value">Parameter matcher evaluated against the assigned value.</param>
	/// <param name="parametersDescription">Factory producing the indexer-argument description used in failure messages.</param>
	public VerificationResult<T> IndexerSet<T, TValue>(T subject, int memberId,
		Func<IInteraction, IParameterMatch<TValue>, bool> setPredicate,
		IParameterMatch<TValue> value,
		Func<string> parametersDescription)
	{
		return new VerificationResult<T>(subject,
			Interactions,
			TryGetRecords(memberId),
			Predicate,
			SetIndexerExpectation(parametersDescription, value));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return setPredicate(interaction, value);
		}
	}

	/// <summary>
	///     Counts subscriptions (<c>+=</c>) to the event named <paramref name="eventName" /> on <paramref name="subject" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="eventName">The simple event name.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	internal VerificationResult<T> SubscribedTo<T>(T subject, string eventName)
	{
		return new VerificationResult<T>(subject, Interactions,
			Predicate,
			SubscribedToEventExpectation(eventName));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is EventSubscription @event &&
			       @event.Name.Equals(eventName);
		}
	}

	/// <summary>
	///     Counts unsubscriptions (<c>-=</c>) from the event named <paramref name="eventName" /> on <paramref name="subject" />.
	/// </summary>
	/// <typeparam name="T">The verification facade type.</typeparam>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="eventName">The simple event name.</param>
	/// <returns>A verification result pending a count terminator.</returns>
	internal VerificationResult<T> UnsubscribedFrom<T>(T subject, string eventName)
	{
		return new VerificationResult<T>(subject, Interactions,
			Predicate,
			UnsubscribedFromEventExpectation(eventName));

		[DebuggerNonUserCode]
		bool Predicate(IInteraction interaction)
		{
			return interaction is EventUnsubscription @event &&
			       @event.Name.Equals(eventName);
		}
	}

	/// <summary>
	///     Typed fast-path Verify for parameterless methods. Walks the typed
	///     <see cref="MethodRecords" /> via <see cref="IRecordCountSource" /> so count terminators
	///     run allocation-free.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T>(T subject, int memberId, string methodName,
		Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords(f, id)) is { } typed)
		{
			return new Method0CountSource<T>(subject, Interactions, typed, methodName,
				expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, IMethodInteraction>(subject, methodName, _ => true, expectation);
	}

	/// <summary>
	///     Typed fast-path Verify for 1-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1>(f, id)) is { } typed)
		{
			return new Method1CountSource<T, T1>(subject, Interactions, typed, methodName, match1, null,
				expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1>>(subject, methodName,
			m => match1.Matches(m.Parameter1), expectation);
	}

	/// <summary>
	///     Typed fast-path Verify for 2-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2>(f, id)) is { } typed)
		{
			return new Method2CountSource<T, T1, T2>(subject, Interactions, typed, methodName, match1, match2,
				null, null, expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2), expectation);
	}

	/// <summary>
	///     Typed fast-path Verify for 3-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new Method3CountSource<T, T1, T2, T3>(subject, Interactions, typed, methodName,
				match1, match2, match3, null, null, null, expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2, T3>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3), expectation);
	}

	/// <summary>
	///     Typed fast-path Verify for 4-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, T4>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new Method4CountSource<T, T1, T2, T3, T4>(subject, Interactions, typed, methodName,
				match1, match2, match3, match4, null, null, null, null,
				expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2, T3, T4>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3) && match4.Matches(m.Parameter4),
			expectation);
	}

	/// <summary>
	///     Literal-value fast-path Verify for 1-parameter methods. Avoids allocating an
	///     <see cref="IParameterMatch{T1}" /> wrapper around <paramref name="literalValue1" /> by
	///     comparing it inline with <see cref="EqualityComparer{T}.Default" />.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1>(T subject, int memberId, string methodName,
		T1 literalValue1, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1>(f, id)) is { } typed)
		{
			return new Method1LiteralCountSource<T, T1>(subject, Interactions, typed, methodName, literalValue1,
				expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1), expectation);
	}

	/// <summary>
	///     Literal-value fast-path Verify for 2-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2>(f, id)) is { } typed)
		{
			return new Method2LiteralCountSource<T, T1, T2>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2), expectation);
	}

	/// <summary>
	///     Literal-value fast-path Verify for 3-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new Method3LiteralCountSource<T, T1, T2, T3>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, literalValue3, expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2, T3>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2) &&
			     EqualityComparer<T3>.Default.Equals(literalValue3, m.Parameter3), expectation);
	}

	/// <summary>
	///     Literal-value fast-path Verify for 4-parameter methods.
	/// </summary>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, T4>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, T4 literalValue4, Func<string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new Method4LiteralCountSource<T, T1, T2, T3, T4>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, literalValue3, literalValue4,
				expectation, static (_, e) => InvokedMethodText(e));
		}

		return VerifyMethod<T, MethodInvocation<T1, T2, T3, T4>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2) &&
			     EqualityComparer<T3>.Default.Equals(literalValue3, m.Parameter3) &&
			     EqualityComparer<T4>.Default.Equals(literalValue4, m.Parameter4), expectation);
	}

	/// <summary>
	///     Typed fast-path Verify for 1-parameter methods. The failure message is built from
	///     <paramref name="parameter1" /> only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="parameter1">The first parameter as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameter<T1>? parameter1, Func<IParameter<T1>?, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1>(f, id)) is { } typed)
		{
			return new Method1CountSource<T, T1>(subject, Interactions, typed, methodName, match1, parameter1,
				expectation, static (state, format)
					=> $"invoked method {((Method1CountSource<T, T1>)state!).Describe((Func<IParameter<T1>?, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, parameter1, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameter<T1>? parameter1, Func<IParameter<T1>?, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1>>(subject, methodName,
			m => match1.Matches(m.Parameter1),
			() => expectation(parameter1));

	/// <summary>
	///     Typed fast-path Verify for 2-parameter methods. The failure message is built from the parameters as
	///     passed by the caller only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="parameter1">The first parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter2">The second parameter as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
#pragma warning disable S107 // Methods should not have too many parameters
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2,
		Func<IParameter<T1>?, IParameter<T2>?, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2>(f, id)) is { } typed)
		{
			return new Method2CountSource<T, T1, T2>(subject, Interactions, typed, methodName, match1, match2,
				parameter1, parameter2, expectation, static (state, format)
					=> $"invoked method {((Method2CountSource<T, T1, T2>)state!).Describe((Func<IParameter<T1>?, IParameter<T2>?, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, parameter1, parameter2, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2,
		Func<IParameter<T1>?, IParameter<T2>?, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2),
			() => expectation(parameter1, parameter2));

	/// <summary>
	///     Typed fast-path Verify for 3-parameter methods. The failure message is built from the parameters as
	///     passed by the caller only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="match3">The matcher for the third parameter.</param>
	/// <param name="parameter1">The first parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter2">The second parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter3">The third parameter as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2, IParameter<T3>? parameter3,
		Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new Method3CountSource<T, T1, T2, T3>(subject, Interactions, typed, methodName,
				match1, match2, match3, parameter1, parameter2, parameter3, expectation, static (state, format)
					=> $"invoked method {((Method3CountSource<T, T1, T2, T3>)state!).Describe((Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, match3,
			parameter1, parameter2, parameter3, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2, IParameter<T3>? parameter3,
		Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3),
			() => expectation(parameter1, parameter2, parameter3));

	/// <summary>
	///     Typed fast-path Verify for 4-parameter methods. The failure message is built from the parameters as
	///     passed by the caller only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="match3">The matcher for the third parameter.</param>
	/// <param name="match4">The matcher for the fourth parameter.</param>
	/// <param name="parameter1">The first parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter2">The second parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter3">The third parameter as passed by the caller, used for the failure message.</param>
	/// <param name="parameter4">The fourth parameter as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, T4>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2, IParameter<T3>? parameter3, IParameter<T4>? parameter4,
		Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, IParameter<T4>?, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new Method4CountSource<T, T1, T2, T3, T4>(subject, Interactions, typed, methodName,
				match1, match2, match3, match4, parameter1, parameter2, parameter3, parameter4,
				expectation, static (state, format)
					=> $"invoked method {((Method4CountSource<T, T1, T2, T3, T4>)state!).Describe((Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, IParameter<T4>?, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, match3, match4,
			parameter1, parameter2, parameter3, parameter4, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3, T4>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2, IParameter<T3>? parameter3, IParameter<T4>? parameter4,
		Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, IParameter<T4>?, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3, T4>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3) && match4.Matches(m.Parameter4),
			() => expectation(parameter1, parameter2, parameter3, parameter4));
#pragma warning restore S107

	/// <summary>
	///     Literal-value fast-path Verify for 1-parameter methods. The failure message is built from
	///     <paramref name="literalValue1" /> only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="literalValue1">The expected value of the first parameter.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1>(T subject, int memberId, string methodName,
		T1 literalValue1, Func<T1, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1>(f, id)) is { } typed)
		{
			return new Method1LiteralCountSource<T, T1>(subject, Interactions, typed, methodName, literalValue1,
				expectation, static (state, format)
					=> $"invoked method {((Method1LiteralCountSource<T, T1>)state!).Describe((Func<T1, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, literalValue1, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1>(T subject, string methodName,
		T1 literalValue1, Func<T1, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1),
			() => expectation(literalValue1));

	/// <summary>
	///     Literal-value fast-path Verify for 2-parameter methods. The failure message is built from the literal
	///     values only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="literalValue1">The expected value of the first parameter.</param>
	/// <param name="literalValue2">The expected value of the second parameter.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, Func<T1, T2, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2>(f, id)) is { } typed)
		{
			return new Method2LiteralCountSource<T, T1, T2>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, expectation, static (state, format)
					=> $"invoked method {((Method2LiteralCountSource<T, T1, T2>)state!).Describe((Func<T1, T2, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, literalValue1, literalValue2, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2>(T subject, string methodName,
		T1 literalValue1, T2 literalValue2, Func<T1, T2, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2),
			() => expectation(literalValue1, literalValue2));

	/// <summary>
	///     Literal-value fast-path Verify for 3-parameter methods. The failure message is built from the literal
	///     values only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="literalValue1">The expected value of the first parameter.</param>
	/// <param name="literalValue2">The expected value of the second parameter.</param>
	/// <param name="literalValue3">The expected value of the third parameter.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, Func<T1, T2, T3, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new Method3LiteralCountSource<T, T1, T2, T3>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, literalValue3, expectation, static (state, format)
					=> $"invoked method {((Method3LiteralCountSource<T, T1, T2, T3>)state!).Describe((Func<T1, T2, T3, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, literalValue1, literalValue2, literalValue3, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3>(T subject, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, Func<T1, T2, T3, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2) &&
			     EqualityComparer<T3>.Default.Equals(literalValue3, m.Parameter3),
			() => expectation(literalValue1, literalValue2, literalValue3));

	/// <summary>
	///     Literal-value fast-path Verify for 4-parameter methods. The failure message is built from the literal
	///     values only when the verification fails.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="literalValue1">The expected value of the first parameter.</param>
	/// <param name="literalValue2">The expected value of the second parameter.</param>
	/// <param name="literalValue3">The expected value of the third parameter.</param>
	/// <param name="literalValue4">The expected value of the fourth parameter.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
#pragma warning disable S107 // Methods should not have too many parameters
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, T4>(T subject, int memberId, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, T4 literalValue4, Func<T1, T2, T3, T4, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new Method4LiteralCountSource<T, T1, T2, T3, T4>(subject, Interactions, typed, methodName,
				literalValue1, literalValue2, literalValue3, literalValue4, expectation, static (state, format)
					=> $"invoked method {((Method4LiteralCountSource<T, T1, T2, T3, T4>)state!).Describe((Func<T1, T2, T3, T4, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName,
			literalValue1, literalValue2, literalValue3, literalValue4, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3, T4>(T subject, string methodName,
		T1 literalValue1, T2 literalValue2, T3 literalValue3, T4 literalValue4, Func<T1, T2, T3, T4, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3, T4>>(subject, methodName,
			m => EqualityComparer<T1>.Default.Equals(literalValue1, m.Parameter1) &&
			     EqualityComparer<T2>.Default.Equals(literalValue2, m.Parameter2) &&
			     EqualityComparer<T3>.Default.Equals(literalValue3, m.Parameter3) &&
			     EqualityComparer<T4>.Default.Equals(literalValue4, m.Parameter4),
			() => expectation(literalValue1, literalValue2, literalValue3, literalValue4));

	/// <summary>
	///     Typed fast-path Verify for 1-parameter methods whose failure message is formatted from
	///     <paramref name="state" /> only when the verification fails, so the caller needs no closure.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="state">The arguments as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, TState>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, TState state, Func<TState, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1>(f, id)) is { } typed)
		{
			return new Method1StateCountSource<T, T1, TState>(subject, Interactions, typed, methodName, match1, state,
				expectation, static (source, format)
					=> $"invoked method {((Method1StateCountSource<T, T1, TState>)source!).Describe((Func<TState, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, state, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, TState>(T subject, string methodName,
		IParameterMatch<T1> match1, TState state, Func<TState, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1>>(subject, methodName,
			m => match1.Matches(m.Parameter1),
			() => expectation(state));

	/// <summary>
	///     Typed fast-path Verify for 2-parameter methods whose failure message is formatted from
	///     <paramref name="state" /> only when the verification fails, so the caller needs no closure.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="state">The arguments as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, TState>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, TState state, Func<TState, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2>(f, id)) is { } typed)
		{
			return new Method2StateCountSource<T, T1, T2, TState>(subject, Interactions, typed, methodName,
				match1, match2, state, expectation, static (source, format)
					=> $"invoked method {((Method2StateCountSource<T, T1, T2, TState>)source!).Describe((Func<TState, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, state, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, TState>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, TState state, Func<TState, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2),
			() => expectation(state));

	/// <summary>
	///     Typed fast-path Verify for 3-parameter methods whose failure message is formatted from
	///     <paramref name="state" /> only when the verification fails, so the caller needs no closure.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="match3">The matcher for the third parameter.</param>
	/// <param name="state">The arguments as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, TState>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		TState state, Func<TState, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new Method3StateCountSource<T, T1, T2, T3, TState>(subject, Interactions, typed, methodName,
				match1, match2, match3, state, expectation, static (source, format)
					=> $"invoked method {((Method3StateCountSource<T, T1, T2, T3, TState>)source!).Describe((Func<TState, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, match3, state, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3, TState>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		TState state, Func<TState, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3),
			() => expectation(state));

	/// <summary>
	///     Typed fast-path Verify for 4-parameter methods whose failure message is formatted from
	///     <paramref name="state" /> only when the verification fails, so the caller needs no closure.
	/// </summary>
	/// <param name="subject">The verification facade the result is bound to.</param>
	/// <param name="memberId">The generator-emitted member id for the method.</param>
	/// <param name="methodName">The simple method name (e.g. <c>"Greet"</c>).</param>
	/// <param name="match1">The matcher for the first parameter.</param>
	/// <param name="match2">The matcher for the second parameter.</param>
	/// <param name="match3">The matcher for the third parameter.</param>
	/// <param name="match4">The matcher for the fourth parameter.</param>
	/// <param name="state">The arguments as passed by the caller, used for the failure message.</param>
	/// <param name="expectation">A static formatter for the expectation description used in failure messages.</param>
	public VerificationResult<T>.IgnoreParameters VerifyMethod<T, T1, T2, T3, T4, TState>(T subject, int memberId, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		TState state, Func<TState, string> expectation)
	{
		if (TryGetRecords(memberId, static (f, id) => new MethodRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new Method4StateCountSource<T, T1, T2, T3, T4, TState>(subject, Interactions, typed, methodName,
				match1, match2, match3, match4, state, expectation, static (source, format)
					=> $"invoked method {((Method4StateCountSource<T, T1, T2, T3, T4, TState>)source!).Describe((Func<TState, string>)format!)}");
		}

		return VerifyMethodWithoutRecords(subject, methodName, match1, match2, match3, match4, state, expectation);
	}

	private VerificationResult<T>.IgnoreParameters VerifyMethodWithoutRecords<T, T1, T2, T3, T4, TState>(T subject, string methodName,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		TState state, Func<TState, string> expectation)
		=> VerifyMethod<T, MethodInvocation<T1, T2, T3, T4>>(subject, methodName,
			m => match1.Matches(m.Parameter1) && match2.Matches(m.Parameter2) && match3.Matches(m.Parameter3) && match4.Matches(m.Parameter4),
			() => expectation(state));
#pragma warning restore S107

	/// <summary>
	///     Typed fast-path Verify for property getter accesses.
	/// </summary>
	internal VerificationResult<T> VerifyPropertyTyped<T>(T subject, int memberId, string propertyName)
	{
		if (TryGetRecords(memberId, static (f, id) => new PropertyGetterRecords(f, id)) is { } typed)
		{
			return new PropertyGetterCountSource<T>(subject, Interactions, typed,
				propertyName, static (_, name) => GotPropertyText(name));
		}

		return VerifyProperty(subject, propertyName);
	}

	/// <summary>
	///     Typed fast-path Verify for property setter accesses.
	/// </summary>
	internal VerificationResult<TSubject> VerifyPropertyTyped<TSubject, TValue>(TSubject subject, int memberId,
		string propertyName, IParameterMatch<TValue> value)
	{
		if (TryGetRecords(memberId, static (f, id) => new PropertySetterRecords<TValue>(f, id)) is { } typed)
		{
			return new PropertySetterCountSource<TSubject, TValue>(subject, Interactions, typed, value,
				propertyName, static (state, name)
					=> SetPropertyText(name, ((PropertySetterCountSource<TSubject, TValue>)state!).Value));
		}

		return VerifyProperty(subject, propertyName, value);
	}

	/// <summary>
	///     Typed fast-path Verify for 1-key indexer getter.
	/// </summary>
	internal VerificationResult<T> IndexerGotTyped<T, T1>(T subject, int memberId,
		IParameterMatch<T1> match1, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerGetterRecords<T1>(f, id)) is { } typed)
		{
			return new IndexerGetter1CountSource<T, T1>(subject, Interactions, typed, match1,
				parametersDescription, static (_, description) => GotIndexerText(description));
		}

		return IndexerGot(subject,
			interaction => interaction is IndexerGetterAccess<T1> g && match1.Matches(g.Parameter1),
			parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 2-key indexer getter.
	/// </summary>
	internal VerificationResult<T> IndexerGotTyped<T, T1, T2>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerGetterRecords<T1, T2>(f, id)) is { } typed)
		{
			return new IndexerGetter2CountSource<T, T1, T2>(subject, Interactions, typed, match1, match2,
				parametersDescription, static (_, description) => GotIndexerText(description));
		}

		return IndexerGot(subject,
			interaction => interaction is IndexerGetterAccess<T1, T2> g && match1.Matches(g.Parameter1) && match2.Matches(g.Parameter2),
			parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 3-key indexer getter.
	/// </summary>
	internal VerificationResult<T> IndexerGotTyped<T, T1, T2, T3>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerGetterRecords<T1, T2, T3>(f, id)) is { } typed)
		{
			return new IndexerGetter3CountSource<T, T1, T2, T3>(subject, Interactions, typed, match1, match2, match3,
				parametersDescription, static (_, description) => GotIndexerText(description));
		}

		return IndexerGot(subject,
			interaction => interaction is IndexerGetterAccess<T1, T2, T3> g && match1.Matches(g.Parameter1) && match2.Matches(g.Parameter2) && match3.Matches(g.Parameter3),
			parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 4-key indexer getter.
	/// </summary>
	internal VerificationResult<T> IndexerGotTyped<T, T1, T2, T3, T4>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerGetterRecords<T1, T2, T3, T4>(f, id)) is { } typed)
		{
			return new IndexerGetter4CountSource<T, T1, T2, T3, T4>(subject, Interactions, typed,
				match1, match2, match3, match4,
				parametersDescription, static (_, description) => GotIndexerText(description));
		}

		return IndexerGot(subject,
			interaction => interaction is IndexerGetterAccess<T1, T2, T3, T4> g && match1.Matches(g.Parameter1) && match2.Matches(g.Parameter2) && match3.Matches(g.Parameter3) && match4.Matches(g.Parameter4),
			parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 1-key indexer setter.
	/// </summary>
	internal VerificationResult<T> IndexerSetTyped<T, T1, TValue>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<TValue> value, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerSetterRecords<T1, TValue>(f, id)) is { } typed)
		{
			return new IndexerSetter1CountSource<T, T1, TValue>(subject, Interactions, typed, match1, value,
				parametersDescription, static (state, description)
					=> SetIndexerText(description, ((IndexerSetter1CountSource<T, T1, TValue>)state!).Value));
		}

		return IndexerSet(subject,
			(interaction, v) => interaction is IndexerSetterAccess<T1, TValue> s && match1.Matches(s.Parameter1) && v.Matches(s.TypedValue),
			value, parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 2-key indexer setter.
	/// </summary>
	internal VerificationResult<T> IndexerSetTyped<T, T1, T2, TValue>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<TValue> value, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerSetterRecords<T1, T2, TValue>(f, id)) is { } typed)
		{
			return new IndexerSetter2CountSource<T, T1, T2, TValue>(subject, Interactions, typed, match1, match2, value,
				parametersDescription, static (state, description)
					=> SetIndexerText(description, ((IndexerSetter2CountSource<T, T1, T2, TValue>)state!).Value));
		}

		return IndexerSet(subject,
			(interaction, v) => interaction is IndexerSetterAccess<T1, T2, TValue> s && match1.Matches(s.Parameter1) && match2.Matches(s.Parameter2) && v.Matches(s.TypedValue),
			value, parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 3-key indexer setter.
	/// </summary>
	internal VerificationResult<T> IndexerSetTyped<T, T1, T2, T3, TValue>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		IParameterMatch<TValue> value, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerSetterRecords<T1, T2, T3, TValue>(f, id)) is { } typed)
		{
			return new IndexerSetter3CountSource<T, T1, T2, T3, TValue>(subject, Interactions, typed,
				match1, match2, match3, value,
				parametersDescription, static (state, description)
					=> SetIndexerText(description, ((IndexerSetter3CountSource<T, T1, T2, T3, TValue>)state!).Value));
		}

		return IndexerSet(subject,
			(interaction, v) => interaction is IndexerSetterAccess<T1, T2, T3, TValue> s && match1.Matches(s.Parameter1) && match2.Matches(s.Parameter2) && match3.Matches(s.Parameter3) && v.Matches(s.TypedValue),
			value, parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for 4-key indexer setter.
	/// </summary>
	internal VerificationResult<T> IndexerSetTyped<T, T1, T2, T3, T4, TValue>(T subject, int memberId,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		IParameterMatch<TValue> value, Func<string> parametersDescription)
	{
		if (TryGetRecords(memberId, static (f, id) => new IndexerSetterRecords<T1, T2, T3, T4, TValue>(f, id)) is { } typed)
		{
			return new IndexerSetter4CountSource<T, T1, T2, T3, T4, TValue>(subject, Interactions, typed,
				match1, match2, match3, match4, value,
				parametersDescription, static (state, description)
					=> SetIndexerText(description, ((IndexerSetter4CountSource<T, T1, T2, T3, T4, TValue>)state!).Value));
		}

		return IndexerSet(subject,
			(interaction, v) => interaction is IndexerSetterAccess<T1, T2, T3, T4, TValue> s && match1.Matches(s.Parameter1) && match2.Matches(s.Parameter2) && match3.Matches(s.Parameter3) && match4.Matches(s.Parameter4) && v.Matches(s.TypedValue),
			value, parametersDescription);
	}

	/// <summary>
	///     Typed fast-path Verify for event subscribe.
	/// </summary>
	internal VerificationResult<T> SubscribedToTyped<T>(T subject, int memberId, string eventName)
	{
		if (TryGetRecords(memberId, static (f, id) => new EventRecords(f, id)) is { } typed)
		{
			return new EventCountSource<T>(subject, Interactions, typed,
				eventName, static (_, name) => SubscribedToEventText(name));
		}

		return SubscribedTo(subject, eventName);
	}

	/// <summary>
	///     Typed fast-path Verify for event unsubscribe.
	/// </summary>
	internal VerificationResult<T> UnsubscribedFromTyped<T>(T subject, int memberId, string eventName)
	{
		if (TryGetRecords(memberId, static (f, id) => new EventRecords(f, id)) is { } typed)
		{
			return new EventCountSource<T>(subject, Interactions, typed,
				eventName, static (_, name) => UnsubscribedFromEventText(name));
		}

		return UnsubscribedFrom(subject, eventName);
	}

	private static VerificationExpectation InvokedMethodExpectation(Func<string> expectation)
		=> new(expectation, null, static (state, _) => InvokedMethodText(state));

	private static VerificationExpectation InvokedMethodSetupExpectation(IMethodSetup methodSetup)
		=> new(methodSetup, null, static (state, _) => $"invoked method {state}");

	private static VerificationExpectation GotPropertyExpectation(string propertyName)
		=> new(propertyName, null, static (state, _) => GotPropertyText(state));

	private static VerificationExpectation SetPropertyExpectation(string propertyName, object value)
		=> new(propertyName, value, static (state, value) => SetPropertyText(state, value));

	private static VerificationExpectation GotIndexerExpectation(Func<string> parametersDescription)
		=> new(parametersDescription, null, static (state, _) => GotIndexerText(state));

	private static VerificationExpectation SetIndexerExpectation(Func<string> parametersDescription, object value)
		=> new(parametersDescription, value, static (state, value) => SetIndexerText(state, value));

	private static VerificationExpectation SubscribedToEventExpectation(string eventName)
		=> new(eventName, null, static (state, _) => SubscribedToEventText(state));

	private static VerificationExpectation UnsubscribedFromEventExpectation(string eventName)
		=> new(eventName, null, static (state, _) => UnsubscribedFromEventText(state));

	private static string InvokedMethodText(object? expectation)
		=> $"invoked method {((Func<string>)expectation!)()}";

	private static string GotPropertyText(object? propertyName)
		=> $"got property {((string)propertyName!).SubstringAfterLast('.')}";

	private static string SetPropertyText(object? propertyName, object? value)
		=> $"set property {((string)propertyName!).SubstringAfterLast('.')} to {value}";

	private static string GotIndexerText(object? parametersDescription)
		=> $"got indexer {((Func<string>)parametersDescription!)()}";

	private static string SetIndexerText(object? parametersDescription, object? value)
		=> $"set indexer {((Func<string>)parametersDescription!)()} to {value}";

	private static string SubscribedToEventText(object? eventName)
		=> $"subscribed to event {((string)eventName!).SubstringAfterLast('.')}";

	private static string UnsubscribedFromEventText(object? eventName)
		=> $"unsubscribed from event {((string)eventName!).SubstringAfterLast('.')}";

	/// <summary>
	///     The view on the typed records of the member, or <see langword="null" /> when it has none, so that the
	///     caller verifies against all interactions of the mock instead.
	/// </summary>
	private MemberRecords? TryGetRecords(int memberId)
		=> Interactions is MockInteractions mockInteractions ? mockInteractions.FindRecords(memberId) : null;

	/// <inheritdoc cref="TryGetRecords(int)" />
	private TRecords? TryGetRecords<TRecords>(int memberId, Func<MockInteractions, int, TRecords> factory)
		where TRecords : MemberRecords
		=> Interactions is MockInteractions mockInteractions ? mockInteractions.FindRecords(memberId, factory) : null;

	/// <summary>
	///     Returns every registered setup (indexer, property, method) that was not hit by any of the given
	///     <paramref name="interactions" />.
	/// </summary>
	/// <remarks>
	///     The default-scope method and indexer snapshot tables populated by the
	///     <see cref="SetupMethod(int, MethodSetup)" /> / <see cref="SetupIndexer(int, IndexerSetup)" /> overloads
	///     are walked alongside the string-keyed lists, so generator-emitted setups are visible to the
	///     diagnostic enumeration even though they bypass the dictionary.
	/// </remarks>
	/// <param name="interactions">The interactions to check against.</param>
	/// <returns>The unused setups; empty when every setup was exercised.</returns>
	public IReadOnlyCollection<ISetup> GetUnusedSetups(IMockInteractions interactions)
	{
		List<ISetup> unusedSetups =
		[
			..Setup.Indexers.EnumerateUnusedSetupsBy(interactions),
			..Setup.Properties.EnumerateUnusedSetupsBy(interactions),
			..Setup.Methods.EnumerateUnusedSetupsBy(interactions),
			..EnumerateUnusedMethodSnapshotSetups(interactions),
			..EnumerateUnusedIndexerSnapshotSetups(interactions),
		];
		return unusedSetups;
	}

	private IEnumerable<MethodSetup> EnumerateUnusedMethodSnapshotSetups(IMockInteractions interactions)
	{
		MethodSetup[]?[]? table = Volatile.Read(ref _setupsByMemberId);
		if (table is null)
		{
			yield break;
		}

		List<IMethodInteraction> methodInteractions = [];
		foreach (IInteraction interaction in interactions)
		{
			if (interaction is IMethodInteraction method)
			{
				methodInteractions.Add(method);
			}
		}

		HashSet<MethodSetup> visited = [];
		foreach (MethodSetup[]? bucket in table)
		{
			if (bucket is null)
			{
				continue;
			}

			foreach (MethodSetup setup in bucket)
			{
				if (!visited.Add(setup))
				{
					continue;
				}

				bool matched = false;
				foreach (IMethodInteraction interaction in methodInteractions)
				{
					if (((IVerifiableMethodSetup)setup).Matches(interaction))
					{
						matched = true;
						break;
					}
				}

				if (!matched)
				{
					yield return setup;
				}
			}
		}
	}

	private IEnumerable<IndexerSetup> EnumerateUnusedIndexerSnapshotSetups(IMockInteractions interactions)
	{
		IndexerSetup[]?[]? table = Volatile.Read(ref _indexerSetupsByMemberId);
		if (table is null)
		{
			yield break;
		}

		List<IndexerAccess> indexerAccesses = [];
		foreach (IInteraction interaction in interactions)
		{
			if (interaction is IndexerAccess access)
			{
				indexerAccesses.Add(access);
			}
		}

		foreach (IndexerSetup[]? bucket in table)
		{
			if (bucket is null)
			{
				continue;
			}

			foreach (IndexerSetup setup in bucket)
			{
				bool matched = false;
				foreach (IndexerAccess access in indexerAccesses)
				{
					if (((IInteractiveIndexerSetup)setup).Matches(access))
					{
						matched = true;
						break;
					}
				}

				if (!matched)
				{
					yield return setup;
				}
			}
		}
	}
}
