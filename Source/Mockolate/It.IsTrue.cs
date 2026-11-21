using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches any <see langword="bool" /> parameter whose value is <see langword="true" />.
	/// </summary>
	/// <remarks>
	///     Shorthand for <c>It.Is(true)</c> with a nicer failure-message rendering. Use
	///     <see cref="IsFalse" /> for the opposite.
	/// </remarks>
	public static IParameterWithCallback<bool> IsTrue()
		=> TrueParameterMatch.Shared;

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	private sealed class TrueParameterMatch() : TypedMatch<bool>(isShared: true)
	{
		internal static readonly TrueParameterMatch Shared = new();

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(bool value) => value;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => "It.IsTrue()";
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
