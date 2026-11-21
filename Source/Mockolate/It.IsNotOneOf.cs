using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches a parameter whose value is <em>not</em> equal to any of the supplied <paramref name="values" />.
	/// </summary>
	/// <remarks>
	///     The inverse of <see cref="IsOneOf{T}(IEnumerable{T})" />. Values whose runtime type is incompatible with
	///     <typeparamref name="T" /> also match, since they cannot equal any <typeparamref name="T" />-typed
	///     alternative.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="values">The values to reject.</param>
	/// <returns>A parameter matcher that rejects every value equal to one of <paramref name="values" />.</returns>
	public static IIsNotOneOfParameter<T> IsNotOneOf<T>(params IEnumerable<T> values)
		=> new ParameterIsNotOneOfMatch<T>(values.ToArray());

	/// <summary>
	///     An <see cref="IParameter{T}" /> used to reject values matching any of a set of alternatives, with an opt-in
	///     custom comparer.
	/// </summary>
	public interface IIsNotOneOfParameter<out T> : IParameterWithCallback<T>
	{
		/// <summary>
		///     Switches equality comparison to use <paramref name="comparer" /> instead of
		///     <see cref="EqualityComparer{T}.Default" />.
		/// </summary>
		IIsNotOneOfParameter<T> Using(IEqualityComparer<T> comparer,
			[CallerArgumentExpression(nameof(comparer))]
			string doNotPopulateThisValue = "");
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class ParameterIsNotOneOfMatch<T>(T[] values) : TypedMatch<T>, IIsNotOneOfParameter<T>
	{
		private IEqualityComparer<T>? _comparer;
		private string? _comparerExpression;

		/// <inheritdoc cref="IIsParameter{T}.Using(IEqualityComparer{T}, string)" />
		public IIsNotOneOfParameter<T> Using(IEqualityComparer<T> comparer, string doNotPopulateThisValue = "")
		{
			_comparer = comparer;
			_comparerExpression = doNotPopulateThisValue;
			return this;
		}

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(T value)
		{
			IEqualityComparer<T> comparer = _comparer ?? EqualityComparer<T>.Default;
			return values.All(v => !comparer.Equals(value, v));
		}

		/// <inheritdoc cref="TypedMatch{T}.MatchesOfDifferentType(object?)" />
		protected override bool MatchesOfDifferentType(object? value) => true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string result =
				$"It.IsNotOneOf({string.Join(", ", values.Select(v => ValueFormatter.Format(v)))})";
			if (_comparer is not null)
			{
				result += $".Using({_comparerExpression})";
			}

			return result;
		}
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
