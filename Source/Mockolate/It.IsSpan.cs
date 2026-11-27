#if NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mockolate.Internals;
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches any parameter of type <see cref="Span{T}" /> of <typeparamref name="T" />.
	/// </summary>
	/// <remarks>
	///     Because <see cref="Span{T}" /> is a ref struct and cannot be stored, Mockolate copies its contents into a
	///     <typeparamref name="T" />-array before dispatch. The predicate-based overload
	///     <see cref="IsSpan{T}(Func{T[], bool}, string)" /> receives this array.
	/// </remarks>
	public static IVerifySpanParameter<T> IsAnySpan<T>()
		=> new SpanParameterMatch<T>(predicate: null);

	/// <summary>
	///     Matches a <see cref="Span{T}" /> of <typeparamref name="T" /> parameter whose contents (copied to a
	///     <typeparamref name="T" />-array) satisfy <paramref name="predicate" />.
	/// </summary>
	/// <remarks>
	///     The span is copied to an array before <paramref name="predicate" /> is invoked, because ref structs cannot
	///     be stored. If you only need to match any span, use <see cref="IsAnySpan{T}" />.
	/// </remarks>
	public static IVerifySpanParameter<T> IsSpan<T>(Func<T[], bool> predicate,
		[CallerArgumentExpression("predicate")]
		string doNotPopulateThisValue = "")
		=> new SpanParameterMatch<T>(predicate, doNotPopulateThisValue);

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class SpanParameterMatch<T>(Func<T[], bool>? predicate, string? predicateExpression = null)
		: TypedMatch<SpanWrapper<T>>, IVerifySpanParameter<T>
	{
		/// <inheritdoc cref="object.ToString()" />
		public override string ToString()
			=> predicate is null
				? $"It.IsAnySpan<{typeof(T).FormatType()}>()"
				: $"It.IsSpan<{typeof(T).FormatType()}>({predicateExpression})";

		protected override bool Matches(SpanWrapper<T> value)
			=> predicate?.Invoke(value.SpanValues) ?? true;
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
#endif
