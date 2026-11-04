using System;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches any parameter whose value is not <see langword="null" />.
	/// </summary>
	/// <remarks>
	///     The inverse of <see cref="IsNull{T}()" />. Arguments of a different runtime type than
	///     <typeparamref name="T" /> also match - their very presence proves they aren't <see langword="null" />.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <returns>A parameter matcher that accepts every non-<see langword="null" /> argument.</returns>
	public static IParameterWithCallback<T> IsNotNull<T>()
		=> NotNullParameterMatch<T>.Shared;

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private class NotNullParameterMatch<T> : TypedMatch<T>
	{
		internal static readonly NotNullParameterMatch<T> Shared = new SharedNotNullParameterMatch();

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(T value) => value is not null;

		/// <inheritdoc cref="TypedMatch{T}.MatchesOfDifferentType(object?)" />
		protected override bool MatchesOfDifferentType(object? value) => true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsNotNull<{typeof(T).FormatType()}>()";

#if !DEBUG
		[System.Diagnostics.DebuggerNonUserCode]
#endif
		private sealed class SharedNotNullParameterMatch : NotNullParameterMatch<T>
		{
			protected override IParameterWithCallback<T> AddCallback(Action<T> callback)
				=> ((IParameterWithCallback<T>)new NotNullParameterMatch<T>()).Do(callback);
		}
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
