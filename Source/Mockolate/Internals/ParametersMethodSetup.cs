using System.ComponentModel;
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Internals;

/// <summary>
///     Setup for a method with one parameter matching against <see cref="IParameters" />, returned by the generated
///     overload that takes a value when the call closes its type parameter over <see langword="object" /> and passes a
///     parameters match, which the <c>IParameters</c> overload takes for any other type argument.
/// </summary>
/// <remarks>
///     Public only because generated code in the user's assembly calls it; not intended for direct use from user code.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParametersVoidMethodSetup<T1>(
	MockRegistry mockRegistry,
	string name,
	IParameters parameters,
	string parameterName1)
	: VoidMethodSetup<T1>.WithParameters(mockRegistry, name, parameters, parameterName1),
		IVoidMethodSetupParameterIgnorer<T1>
{
	private bool _matchAnyParameters;

	/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1}.AnyParameters()" />
	IVoidMethodSetup<T1> IVoidMethodSetupParameterIgnorer<T1>.AnyParameters()
	{
		_matchAnyParameters = true;
		return this;
	}

	/// <inheritdoc cref="VoidMethodSetup{T1}.Matches(T1)" />
	public override bool Matches(T1 p1Value)
		=> _matchAnyParameters || base.Matches(p1Value);
}

/// <summary>
///     Setup for a method with a parameter and a trailing <c>params</c> array matching against
///     <see cref="IParameters" />, returned by the generated overload that takes values when the call closes the type
///     parameter of the first parameter over <see langword="object" /> and passes only a parameters match, which the
///     <c>IParameters</c> overload takes for any other type argument.
/// </summary>
/// <remarks>
///     Public only because generated code in the user's assembly calls it; not intended for direct use from user code.
/// </remarks>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParametersVoidMethodSetup<T1, T2>(
	MockRegistry mockRegistry,
	string name,
	IParameters parameters,
	string parameterName1,
	string parameterName2)
	: VoidMethodSetup<T1, T2>.WithParameters(mockRegistry, name, parameters, parameterName1, parameterName2),
		IVoidMethodSetupParameterIgnorer<T1, T2>
{
	private bool _matchAnyParameters;

	/// <inheritdoc cref="IVoidMethodSetupParameterIgnorer{T1, T2}.AnyParameters()" />
	IVoidMethodSetup<T1, T2> IVoidMethodSetupParameterIgnorer<T1, T2>.AnyParameters()
	{
		_matchAnyParameters = true;
		return this;
	}

	/// <inheritdoc cref="VoidMethodSetup{T1, T2}.Matches(T1, T2)" />
	public override bool Matches(T1 p1Value, T2 p2Value)
		=> _matchAnyParameters || base.Matches(p1Value, p2Value);
}

/// <inheritdoc cref="ParametersVoidMethodSetup{T1}" />
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParametersReturnMethodSetup<TReturn, T1>(
	MockRegistry mockRegistry,
	string name,
	IParameters parameters,
	string parameterName1)
	: ReturnMethodSetup<TReturn, T1>.WithParameters(mockRegistry, name, parameters, parameterName1),
		IReturnMethodSetupParameterIgnorer<TReturn, T1>
{
	private bool _matchAnyParameters;

	/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1}.AnyParameters()" />
	IReturnMethodSetup<TReturn, T1> IReturnMethodSetupParameterIgnorer<TReturn, T1>.AnyParameters()
	{
		_matchAnyParameters = true;
		return this;
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1}.Matches(T1)" />
	public override bool Matches(T1 p1Value)
		=> _matchAnyParameters || base.Matches(p1Value);
}

/// <inheritdoc cref="ParametersVoidMethodSetup{T1, T2}" />
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParametersReturnMethodSetup<TReturn, T1, T2>(
	MockRegistry mockRegistry,
	string name,
	IParameters parameters,
	string parameterName1,
	string parameterName2)
	: ReturnMethodSetup<TReturn, T1, T2>.WithParameters(mockRegistry, name, parameters, parameterName1,
			parameterName2),
		IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>
{
	private bool _matchAnyParameters;

	/// <inheritdoc cref="IReturnMethodSetupParameterIgnorer{TReturn, T1, T2}.AnyParameters()" />
	IReturnMethodSetup<TReturn, T1, T2> IReturnMethodSetupParameterIgnorer<TReturn, T1, T2>.AnyParameters()
	{
		_matchAnyParameters = true;
		return this;
	}

	/// <inheritdoc cref="ReturnMethodSetup{TReturn, T1, T2}.Matches(T1, T2)" />
	public override bool Matches(T1 p1Value, T2 p2Value)
		=> _matchAnyParameters || base.Matches(p1Value, p2Value);
}
