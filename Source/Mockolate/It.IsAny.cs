using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches any value passed for a parameter of type <typeparamref name="T" /> (including <see langword="null" />).
	/// </summary>
	/// <remarks>
	///     The workhorse matcher when a setup or verification shouldn't care about the exact argument value. For
	///     reference-typed and nullable parameters <see langword="null" /> also matches. For <see langword="ref" />,
	///     <see langword="out" />, span or ref-struct parameters, use the dedicated <c>It.IsAnyRef</c>,
	///     <c>It.IsAnyOut</c>, <c>It.IsAnySpan</c>, <c>It.IsAnyRefStruct</c> counterparts instead.
	///     <para />
	///     Chain <c>.Monitor(out var monitor)</c> (see <see cref="ParameterExtensions" />) to observe the actual values that
	///     flow through the matcher at runtime.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter.</typeparam>
	/// <returns>A parameter matcher that accepts every value of type <typeparamref name="T" />.</returns>
	public static IParameterWithCallback<T> IsAny<T>()
		=> AnyParameterMatch<T>.Shared;

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class AnyParameterMatch<T>() : TypedMatch<T>(isShared: true)
	{
		internal static readonly AnyParameterMatch<T> Shared = new();

		protected override bool Matches(T value) => true;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => $"It.IsAny<{typeof(T).FormatType()}>()";
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
