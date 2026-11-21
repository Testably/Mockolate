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
	///     Matches a collection parameter whose elements equal <paramref name="values" /> in the same order (and
	///     have the same length).
	/// </summary>
	/// <remarks>
	///     Supports method parameters declared as <see cref="IEnumerable{T}" />, <see cref="ICollection{T}" />,
	///     <see cref="IList{T}" />, <see cref="IReadOnlyCollection{T}" />, <see cref="IReadOnlyList{T}" />,
	///     <typeparamref name="T" /> arrays, <see cref="List{T}" />, <see cref="Queue{T}" /> or
	///     <see cref="Stack{T}" />. Unordered shapes such as <see cref="ISet{T}" /> and
	///     <see cref="HashSet{T}" /> are intentionally not supported, as their enumeration order
	///     is not guaranteed. Use <see cref="Contains{T}(T, string)" /> when you only care about containment rather
	///     than order.
	/// </remarks>
	/// <typeparam name="T">The collection element type.</typeparam>
	/// <param name="values">The expected sequence, in order.</param>
	/// <returns>A parameter matcher that accepts any supported ordered collection whose elements equal <paramref name="values" /> in order.</returns>
	public static ISequenceEqualsParameter<T> SequenceEquals<T>(params IEnumerable<T> values)
		=> new ParameterSequenceEqualsMatch<T>(values.ToArray());

	/// <summary>
	///     An <see cref="IParameter{T}" /> that matches any supported ordered collection parameter whose
	///     elements equal an expected sequence.
	/// </summary>
	public interface ISequenceEqualsParameter<T> :
		IParameter<IEnumerable<T>>,
		IParameter<ICollection<T>>,
		IParameter<IList<T>>,
		IParameter<IReadOnlyCollection<T>>,
		IParameter<IReadOnlyList<T>>,
		IParameter<T[]>,
		IParameter<List<T>>,
		IParameter<Queue<T>>,
		IParameter<Stack<T>>
	{
		/// <summary>
		///     Use the specified comparer to determine equality between elements.
		/// </summary>
		ISequenceEqualsParameter<T> Using(IEqualityComparer<T> comparer,
			[CallerArgumentExpression(nameof(comparer))]
			string doNotPopulateThisValue = "");
	}

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class ParameterSequenceEqualsMatch<T>(T[] expected)
		: OrderedCollectionMatch<T>, ISequenceEqualsParameter<T>
	{
		private IEqualityComparer<T>? _comparer;
		private string? _comparerExpression;

		/// <inheritdoc cref="ISequenceEqualsParameter{T}.Using(IEqualityComparer{T}, string)" />
		ISequenceEqualsParameter<T> ISequenceEqualsParameter<T>.Using(IEqualityComparer<T> comparer,
			string doNotPopulateThisValue)
		{
			_comparer = comparer;
			_comparerExpression = doNotPopulateThisValue;
			return this;
		}

		/// <inheritdoc cref="CollectionMatchCore{T}.MatchesCollection(IEnumerable{T})" />
		protected override bool MatchesCollection(IEnumerable<T> value)
		{
			if (value is null)
			{
				return false;
			}

			IEqualityComparer<T> comparer = _comparer ?? EqualityComparer<T>.Default;
			return value.SequenceEqual(expected, comparer);
		}

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
		{
			string result =
				$"It.SequenceEquals({string.Join(", ", expected.Select(v => ValueFormatter.Format(v)))})";
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
