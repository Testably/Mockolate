using System;
using System.Collections.Generic;
using Mockolate.Interactions;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate.Verify;

/// <summary>
///     Allocation-free count source backed by a parameterless method buffer. It is also the verification result,
///     so a fast-path verification allocates a single object.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method0CountSource<TVerify> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod0Buffer _buffer;

	public Method0CountSource(TVerify verify, IMockInteractions interactions, FastMethod0Buffer buffer,
		string methodName, object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
	}

	public int Count() => _buffer.ConsumeAll();
	public int CountAll() => _buffer.ConsumeAll();
	public bool Matches(IInteraction interaction) => true;
}

/// <summary>
///     Allocation-free count source backed by a 1-parameter method buffer and a typed matcher.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method1CountSource<TVerify, T1> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod1Buffer<T1> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameter<T1>? _parameter1;

	public Method1CountSource(TVerify verify, IMockInteractions interactions, FastMethod1Buffer<T1> buffer,
		string methodName, IParameterMatch<T1> match1, IParameter<T1>? parameter1,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_parameter1 = parameter1;
	}

	public int Count() => _buffer.ConsumeMatching(_match1);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1> m && _match1.Matches(m.Parameter1);

	public string Describe(Func<IParameter<T1>?, string> expectation)
		=> expectation(_parameter1);
}

/// <summary>
///     Allocation-free count source backed by a 2-parameter method buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method2CountSource<TVerify, T1, T2> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod2Buffer<T1, T2> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameter<T1>? _parameter1;
	private readonly IParameter<T2>? _parameter2;

	public Method2CountSource(TVerify verify, IMockInteractions interactions, FastMethod2Buffer<T1, T2> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_parameter1 = parameter1;
		_parameter2 = parameter2;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2> m && _match1.Matches(m.Parameter1) && _match2.Matches(m.Parameter2);

	public string Describe(Func<IParameter<T1>?, IParameter<T2>?, string> expectation)
		=> expectation(_parameter1, _parameter2);
}

/// <summary>
///     Allocation-free count source backed by a 3-parameter method buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method3CountSource<TVerify, T1, T2, T3> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod3Buffer<T1, T2, T3> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameter<T1>? _parameter1;
	private readonly IParameter<T2>? _parameter2;
	private readonly IParameter<T3>? _parameter3;

#pragma warning disable S107 // Methods should not have too many parameters
	public Method3CountSource(TVerify verify, IMockInteractions interactions, FastMethod3Buffer<T1, T2, T3> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2, IParameter<T3>? parameter3,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_parameter1 = parameter1;
		_parameter2 = parameter2;
		_parameter3 = parameter3;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3> m && _match1.Matches(m.Parameter1) &&
		   _match2.Matches(m.Parameter2) && _match3.Matches(m.Parameter3);

	public string Describe(Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, string> expectation)
		=> expectation(_parameter1, _parameter2, _parameter3);
}

/// <summary>
///     Allocation-free count source backed by a 4-parameter method buffer.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method4CountSource<TVerify, T1, T2, T3, T4> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
#pragma warning restore S2436
{
	private readonly FastMethod4Buffer<T1, T2, T3, T4> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;
	private readonly IParameter<T1>? _parameter1;
	private readonly IParameter<T2>? _parameter2;
	private readonly IParameter<T3>? _parameter3;
	private readonly IParameter<T4>? _parameter4;

#pragma warning disable S107 // Methods should not have too many parameters
	public Method4CountSource(TVerify verify, IMockInteractions interactions, FastMethod4Buffer<T1, T2, T3, T4> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		IParameter<T1>? parameter1, IParameter<T2>? parameter2,
		IParameter<T3>? parameter3, IParameter<T4>? parameter4,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
		_parameter1 = parameter1;
		_parameter2 = parameter2;
		_parameter3 = parameter3;
		_parameter4 = parameter4;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3, _match4);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3, T4> m && _match1.Matches(m.Parameter1) &&
		   _match2.Matches(m.Parameter2) && _match3.Matches(m.Parameter3) && _match4.Matches(m.Parameter4);

	public string Describe(Func<IParameter<T1>?, IParameter<T2>?, IParameter<T3>?, IParameter<T4>?, string> expectation)
		=> expectation(_parameter1, _parameter2, _parameter3, _parameter4);
}

/// <summary>
///     Allocation-free count source backed by a 1-parameter method buffer and a captured literal value.
///     Sibling of <see cref="Method1CountSource{TVerify, T1}" /> that fuses the expected value into the source so
///     no <see cref="IParameterMatch{T1}" /> wrapper has to be allocated for bare-value verifies.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method1LiteralCountSource<TVerify, T1> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod1Buffer<T1> _buffer;
	private readonly T1 _value1;

	public Method1LiteralCountSource(TVerify verify, IMockInteractions interactions, FastMethod1Buffer<T1> buffer,
		string methodName, T1 value1,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_value1 = value1;
	}

	public int Count() => _buffer.ConsumeMatchingLiteral(_value1);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1> m &&
		   EqualityComparer<T1>.Default.Equals(_value1, m.Parameter1);

	public string Describe(Func<T1, string> expectation)
		=> expectation(_value1);
}

/// <summary>
///     Allocation-free count source backed by a 2-parameter method buffer and captured literal values.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method2LiteralCountSource<TVerify, T1, T2> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod2Buffer<T1, T2> _buffer;
	private readonly T1 _value1;
	private readonly T2 _value2;

	public Method2LiteralCountSource(TVerify verify, IMockInteractions interactions, FastMethod2Buffer<T1, T2> buffer,
		string methodName, T1 value1, T2 value2,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_value1 = value1;
		_value2 = value2;
	}

	public int Count() => _buffer.ConsumeMatchingLiteral(_value1, _value2);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2> m &&
		   EqualityComparer<T1>.Default.Equals(_value1, m.Parameter1) &&
		   EqualityComparer<T2>.Default.Equals(_value2, m.Parameter2);

	public string Describe(Func<T1, T2, string> expectation)
		=> expectation(_value1, _value2);
}

/// <summary>
///     Allocation-free count source backed by a 3-parameter method buffer and captured literal values.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method3LiteralCountSource<TVerify, T1, T2, T3> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod3Buffer<T1, T2, T3> _buffer;
	private readonly T1 _value1;
	private readonly T2 _value2;
	private readonly T3 _value3;

	public Method3LiteralCountSource(TVerify verify, IMockInteractions interactions, FastMethod3Buffer<T1, T2, T3> buffer,
		string methodName, T1 value1, T2 value2, T3 value3,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_value1 = value1;
		_value2 = value2;
		_value3 = value3;
	}

	public int Count() => _buffer.ConsumeMatchingLiteral(_value1, _value2, _value3);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3> m &&
		   EqualityComparer<T1>.Default.Equals(_value1, m.Parameter1) &&
		   EqualityComparer<T2>.Default.Equals(_value2, m.Parameter2) &&
		   EqualityComparer<T3>.Default.Equals(_value3, m.Parameter3);

	public string Describe(Func<T1, T2, T3, string> expectation)
		=> expectation(_value1, _value2, _value3);
}

/// <summary>
///     Allocation-free count source backed by a 4-parameter method buffer and captured literal values.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method4LiteralCountSource<TVerify, T1, T2, T3, T4> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
#pragma warning restore S2436
{
	private readonly FastMethod4Buffer<T1, T2, T3, T4> _buffer;
	private readonly T1 _value1;
	private readonly T2 _value2;
	private readonly T3 _value3;
	private readonly T4 _value4;

#pragma warning disable S107 // Methods should not have too many parameters
	public Method4LiteralCountSource(TVerify verify, IMockInteractions interactions, FastMethod4Buffer<T1, T2, T3, T4> buffer,
		string methodName, T1 value1, T2 value2, T3 value3, T4 value4,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_value1 = value1;
		_value2 = value2;
		_value3 = value3;
		_value4 = value4;
	}

	public int Count() => _buffer.ConsumeMatchingLiteral(_value1, _value2, _value3, _value4);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3, T4> m &&
		   EqualityComparer<T1>.Default.Equals(_value1, m.Parameter1) &&
		   EqualityComparer<T2>.Default.Equals(_value2, m.Parameter2) &&
		   EqualityComparer<T3>.Default.Equals(_value3, m.Parameter3) &&
		   EqualityComparer<T4>.Default.Equals(_value4, m.Parameter4);

	public string Describe(Func<T1, T2, T3, T4, string> expectation)
		=> expectation(_value1, _value2, _value3, _value4);
}

/// <summary>
///     Allocation-free count source backed by a 1-parameter method buffer and a typed matcher. It keeps the
///     caller's arguments as <typeparamref name="TState" />, so the failure message needs no closure.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method1StateCountSource<TVerify, T1, TState> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod1Buffer<T1> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly TState _state;

	public Method1StateCountSource(TVerify verify, IMockInteractions interactions, FastMethod1Buffer<T1> buffer,
		string methodName, IParameterMatch<T1> match1, TState state,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_state = state;
	}

	public int Count() => _buffer.ConsumeMatching(_match1);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1> m && _match1.Matches(m.Parameter1);

	public string Describe(Func<TState, string> expectation)
		=> expectation(_state);
}

/// <summary>
///     Allocation-free count source backed by a 2-parameter method buffer that keeps the caller's arguments as
///     <typeparamref name="TState" />.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method2StateCountSource<TVerify, T1, T2, TState> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
{
	private readonly FastMethod2Buffer<T1, T2> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly TState _state;

	public Method2StateCountSource(TVerify verify, IMockInteractions interactions, FastMethod2Buffer<T1, T2> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2, TState state,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_state = state;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2> m && _match1.Matches(m.Parameter1) && _match2.Matches(m.Parameter2);

	public string Describe(Func<TState, string> expectation)
		=> expectation(_state);
}

/// <summary>
///     Allocation-free count source backed by a 3-parameter method buffer that keeps the caller's arguments as
///     <typeparamref name="TState" />.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method3StateCountSource<TVerify, T1, T2, T3, TState> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
#pragma warning restore S2436
{
	private readonly FastMethod3Buffer<T1, T2, T3> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly TState _state;

#pragma warning disable S107 // Methods should not have too many parameters
	public Method3StateCountSource(TVerify verify, IMockInteractions interactions, FastMethod3Buffer<T1, T2, T3> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		TState state, object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_state = state;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3> m && _match1.Matches(m.Parameter1) &&
		   _match2.Matches(m.Parameter2) && _match3.Matches(m.Parameter3);

	public string Describe(Func<TState, string> expectation)
		=> expectation(_state);
}

/// <summary>
///     Allocation-free count source backed by a 4-parameter method buffer that keeps the caller's arguments as
///     <typeparamref name="TState" />.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class Method4StateCountSource<TVerify, T1, T2, T3, T4, TState> : VerificationResult<TVerify>.IgnoreParameters, IFastMethodCountSource
#pragma warning restore S2436
{
	private readonly FastMethod4Buffer<T1, T2, T3, T4> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;
	private readonly TState _state;

#pragma warning disable S107 // Methods should not have too many parameters
	public Method4StateCountSource(TVerify verify, IMockInteractions interactions, FastMethod4Buffer<T1, T2, T3, T4> buffer,
		string methodName, IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<T4> match4, TState state,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, methodName, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
		_state = state;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3, _match4);
	public int CountAll() => _buffer.ConsumeAll();

	public bool Matches(IInteraction interaction)
		=> interaction is MethodInvocation<T1, T2, T3, T4> m && _match1.Matches(m.Parameter1) &&
		   _match2.Matches(m.Parameter2) && _match3.Matches(m.Parameter3) && _match4.Matches(m.Parameter4);

	public string Describe(Func<TState, string> expectation)
		=> expectation(_state);
}

/// <summary>
///     Allocation-free count source backed by a property getter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class PropertyGetterCountSource<TVerify> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastPropertyGetterBuffer _buffer;

	public PropertyGetterCountSource(TVerify verify, IMockInteractions interactions, FastPropertyGetterBuffer buffer,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
	}

	public int Count() => _buffer.ConsumeAll();
	public bool Matches(IInteraction interaction) => true;
}

/// <summary>
///     Allocation-free count source backed by a property setter buffer and a typed value matcher.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class PropertySetterCountSource<TVerify, T> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastPropertySetterBuffer<T> _buffer;

	public PropertySetterCountSource(TVerify verify, IMockInteractions interactions, FastPropertySetterBuffer<T> buffer,
		IParameterMatch<T> match, object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		Value = match;
	}

	public IParameterMatch<T> Value { get; }

	public int Count() => _buffer.ConsumeMatching(Value);

	public bool Matches(IInteraction interaction)
		=> interaction is PropertySetterAccess<T> p && Value.Matches(p.Value);
}

/// <summary>
///     Allocation-free count source backed by a 1-key indexer getter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerGetter1CountSource<TVerify, T1> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastIndexerGetterBuffer<T1> _buffer;
	private readonly IParameterMatch<T1> _match1;

	public IndexerGetter1CountSource(TVerify verify, IMockInteractions interactions, FastIndexerGetterBuffer<T1> buffer,
		IParameterMatch<T1> match1, object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
	}

	public int Count() => _buffer.ConsumeMatching(_match1);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerGetterAccess<T1> g && _match1.Matches(g.Parameter1);
}

/// <summary>
///     Allocation-free count source backed by a 2-key indexer getter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerGetter2CountSource<TVerify, T1, T2> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastIndexerGetterBuffer<T1, T2> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;

	public IndexerGetter2CountSource(TVerify verify, IMockInteractions interactions, FastIndexerGetterBuffer<T1, T2> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerGetterAccess<T1, T2> g && _match1.Matches(g.Parameter1) && _match2.Matches(g.Parameter2);
}

/// <summary>
///     Allocation-free count source backed by a 3-key indexer getter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerGetter3CountSource<TVerify, T1, T2, T3> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastIndexerGetterBuffer<T1, T2, T3> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;

	public IndexerGetter3CountSource(TVerify verify, IMockInteractions interactions, FastIndexerGetterBuffer<T1, T2, T3> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<T3> match3,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerGetterAccess<T1, T2, T3> g && _match1.Matches(g.Parameter1) &&
		   _match2.Matches(g.Parameter2) && _match3.Matches(g.Parameter3);
}

/// <summary>
///     Allocation-free count source backed by a 4-key indexer getter buffer.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerGetter4CountSource<TVerify, T1, T2, T3, T4> : VerificationResult<TVerify>, IFastCountSource
#pragma warning restore S2436
{
	private readonly FastIndexerGetterBuffer<T1, T2, T3, T4> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;

#pragma warning disable S107 // Methods should not have too many parameters
	public IndexerGetter4CountSource(TVerify verify, IMockInteractions interactions, FastIndexerGetterBuffer<T1, T2, T3, T4> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<T4> match4,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
	}

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3, _match4);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerGetterAccess<T1, T2, T3, T4> g && _match1.Matches(g.Parameter1) &&
		   _match2.Matches(g.Parameter2) && _match3.Matches(g.Parameter3) && _match4.Matches(g.Parameter4);
}

/// <summary>
///     Allocation-free count source backed by a 1-key indexer setter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerSetter1CountSource<TVerify, T1, TValue> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastIndexerSetterBuffer<T1, TValue> _buffer;
	private readonly IParameterMatch<T1> _match1;

	public IndexerSetter1CountSource(TVerify verify, IMockInteractions interactions, FastIndexerSetterBuffer<T1, TValue> buffer,
		IParameterMatch<T1> match1, IParameterMatch<TValue> matchValue,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		Value = matchValue;
	}

	public IParameterMatch<TValue> Value { get; }

	public int Count() => _buffer.ConsumeMatching(_match1, Value);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerSetterAccess<T1, TValue> s && _match1.Matches(s.Parameter1) && Value.Matches(s.TypedValue);
}

/// <summary>
///     Allocation-free count source backed by a 2-key indexer setter buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerSetter2CountSource<TVerify, T1, T2, TValue> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastIndexerSetterBuffer<T1, T2, TValue> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;

	public IndexerSetter2CountSource(TVerify verify, IMockInteractions interactions, FastIndexerSetterBuffer<T1, T2, TValue> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2, IParameterMatch<TValue> matchValue,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		Value = matchValue;
	}

	public IParameterMatch<TValue> Value { get; }

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, Value);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerSetterAccess<T1, T2, TValue> s && _match1.Matches(s.Parameter1) &&
		   _match2.Matches(s.Parameter2) && Value.Matches(s.TypedValue);
}

/// <summary>
///     Allocation-free count source backed by a 3-key indexer setter buffer.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerSetter3CountSource<TVerify, T1, T2, T3, TValue> : VerificationResult<TVerify>, IFastCountSource
#pragma warning restore S2436
{
	private readonly FastIndexerSetterBuffer<T1, T2, T3, TValue> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;

#pragma warning disable S107 // Methods should not have too many parameters
	public IndexerSetter3CountSource(TVerify verify, IMockInteractions interactions, FastIndexerSetterBuffer<T1, T2, T3, TValue> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<TValue> matchValue,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		Value = matchValue;
	}

	public IParameterMatch<TValue> Value { get; }

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3, Value);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerSetterAccess<T1, T2, T3, TValue> s && _match1.Matches(s.Parameter1) &&
		   _match2.Matches(s.Parameter2) && _match3.Matches(s.Parameter3) && Value.Matches(s.TypedValue);
}

/// <summary>
///     Allocation-free count source backed by a 4-key indexer setter buffer.
/// </summary>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class IndexerSetter4CountSource<TVerify, T1, T2, T3, T4, TValue> : VerificationResult<TVerify>, IFastCountSource
#pragma warning restore S2436 // Types and methods should not have too many generic parameters
{
	private readonly FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> _buffer;
	private readonly IParameterMatch<T1> _match1;
	private readonly IParameterMatch<T2> _match2;
	private readonly IParameterMatch<T3> _match3;
	private readonly IParameterMatch<T4> _match4;

#pragma warning disable S107 // Methods should not have too many parameters
	public IndexerSetter4CountSource(TVerify verify, IMockInteractions interactions, FastIndexerSetterBuffer<T1, T2, T3, T4, TValue> buffer,
		IParameterMatch<T1> match1, IParameterMatch<T2> match2,
		IParameterMatch<T3> match3, IParameterMatch<T4> match4, IParameterMatch<TValue> matchValue,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
#pragma warning restore S107
	{
		_buffer = buffer;
		_match1 = match1;
		_match2 = match2;
		_match3 = match3;
		_match4 = match4;
		Value = matchValue;
	}

	public IParameterMatch<TValue> Value { get; }

	public int Count() => _buffer.ConsumeMatching(_match1, _match2, _match3, _match4, Value);

	public bool Matches(IInteraction interaction)
		=> interaction is IndexerSetterAccess<T1, T2, T3, T4, TValue> s && _match1.Matches(s.Parameter1) &&
		   _match2.Matches(s.Parameter2) && _match3.Matches(s.Parameter3) && _match4.Matches(s.Parameter4) &&
		   Value.Matches(s.TypedValue);
}

/// <summary>
///     Allocation-free count source backed by an event subscribe/unsubscribe buffer.
/// </summary>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
internal sealed class EventCountSource<TVerify> : VerificationResult<TVerify>, IFastCountSource
{
	private readonly FastEventBuffer _buffer;

	public EventCountSource(TVerify verify, IMockInteractions interactions, FastEventBuffer buffer,
		object? expectationArgument, Func<object?, object?, string> expectationFactory)
		: base(verify, interactions, buffer, expectationArgument, expectationFactory)
	{
		_buffer = buffer;
	}

	public int Count() => _buffer.ConsumeAll();
	public bool Matches(IInteraction interaction) => true;
}
