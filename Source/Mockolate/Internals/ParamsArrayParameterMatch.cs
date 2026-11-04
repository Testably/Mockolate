using System.ComponentModel;
using System.Linq;
using Mockolate.Parameters;

namespace Mockolate.Internals;

/// <summary>
///     Matches a <c>params</c> array argument element-by-element against a set of per-element matchers.
///     The recorded array matches when its length equals the number of matchers and each element
///     satisfies the matcher at the same position.
/// </summary>
/// <remarks>
///     Generated <c>Setup</c>/<c>Verify</c> overloads for <c>params T[]</c> methods wrap the per-element
///     matchers into a single instance of this type, which then flows through the regular whole-array
///     <see cref="IParameterMatch{T}" /> pipeline (where <c>T</c> is <typeparamref name="TElement" /> array).
/// </remarks>
/// <typeparam name="TElement">The element type of the <c>params</c> array.</typeparam>
#if !DEBUG
[System.Diagnostics.DebuggerNonUserCode]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ParamsArrayParameterMatch<TElement> : IParameterMatch<TElement[]>
{
	private readonly IParameter<TElement>[] _matchers;

	/// <summary>
	///     Initializes a new <see cref="ParamsArrayParameterMatch{TElement}" /> from the per-element
	///     <paramref name="matchers" />.
	/// </summary>
	public ParamsArrayParameterMatch(params IParameter<TElement>[] matchers)
		=> _matchers = matchers;

	/// <inheritdoc cref="IParameterMatch{T}.Matches(T)" />
	public bool Matches(TElement[] value)
	{
		if (value is null || value.Length != _matchers.Length)
		{
			return false;
		}

		for (int i = 0; i < _matchers.Length; i++)
		{
			if (_matchers[i] is null || !_matchers[i].Matches(value[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc cref="IParameterMatch{T}.InvokeCallbacks(T)" />
	public void InvokeCallbacks(TElement[] value)
	{
		if (value is null || value.Length != _matchers.Length)
		{
			return;
		}

		for (int i = 0; i < _matchers.Length; i++)
		{
			_matchers[i]?.InvokeCallbacks(value[i]);
		}
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"[{string.Join(", ", _matchers.Select(m => m?.ToString() ?? "null"))}]";
}
