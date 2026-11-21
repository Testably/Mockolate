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
	///     Matches a parameter that equals any one of the supplied <paramref name="values" />.
	/// </summary>
	/// <remarks>
	///     Equality uses <see cref="EqualityComparer{T}.Default" /> unless <see cref="IIsOneOfParameter{T}.Using" /> is
	///     called. Pass the allowed values either as a comma-separated list or as any <see cref="IEnumerable{T}" />.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <param name="values">The accepted values.</param>
	/// <returns>A parameter matcher that accepts any value equal to one of <paramref name="values" />.</returns>
	public static IIsOneOfParameter<T> IsOneOf<T>(params IEnumerable<T> values)
		=> new ParameterIsOneOfMatch<T>(values.ToArray());

	/// <summary>
	///     An <see cref="IParameter{T}" /> used for equality comparison against a set of alternatives, with an opt-in
	///     custom comparer.
	/// </summary>
	public interface IIsOneOfParameter<out T> : IParameterWithCallback<T>
	{
		/// <summary>
		///     Switches equality comparison to use <paramref name="comparer" /> instead of
		///     <see cref="EqualityComparer{T}.Default" />.
		/// </summary>
		IIsOneOfParameter<T> Using(IEqualityComparer<T> comparer,
			[CallerArgumentExpression(nameof(comparer))]
			string doNotPopulateThisValue = "");
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class ParameterIsOneOfMatch<T>(T[] values) : TypedMatch<T>, IIsOneOfParameter<T>
	{
		private IEqualityComparer<T>? _comparer;
		private string? _comparerExpression;

		/// <inheritdoc cref="IIsParameter{T}.Using(IEqualityComparer{T}, string)" />
		public IIsOneOfParameter<T> Using(IEqualityComparer<T> comparer, string doNotPopulateThisValue = "")
		{
			_comparer = comparer;
			_comparerExpression = doNotPopulateThisValue;
			return this;
		}

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(T value)
		{
			IEqualityComparer<T> comparer = _comparer ?? EqualityComparer<T>.Default;
			return values.Any(v => comparer.Equals(value, v));
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string result =
				$"It.IsOneOf({string.Join(", ", values.Select(v => ValueFormatter.Format(v)))})";
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
