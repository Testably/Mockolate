using System;
using Mockolate.Internals;
using Mockolate.Parameters;

namespace Mockolate;

#pragma warning disable S3453 // This class can't be instantiated; make its constructor 'public'.
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
public partial class It
{
	/// <summary>
	///     Matches any parameter that is <see langword="null" />.
	/// </summary>
	/// <remarks>
	///     Useful to assert explicit-null arguments in setups or verifications; for non-null values use
	///     <see cref="IsNotNull{T}()" />.
	/// </remarks>
	/// <typeparam name="T">The declared type of the parameter. Should be a reference type or a <see cref="System.Nullable{T}" />; for other value types this matcher never matches.</typeparam>
	/// <returns>A parameter matcher that only accepts <see langword="null" />.</returns>
	public static IParameterWithCallback<T> IsNull<T>()
		=> NullParameterMatch<T>.Shared;

#if !DEBUG
	[System.Diagnostics.DebuggerNonUserCode]
#endif
	internal class NullParameterMatch<T>(string? toString) : TypedMatch<T>
	{
		internal static readonly NullParameterMatch<T> Shared = new SharedNullParameterMatch();

		/// <inheritdoc cref="TypedMatch{T}.Matches(T)" />
		protected override bool Matches(T value) => value is null;

		/// <inheritdoc cref="object.ToString()" />
		public override string ToString() => toString ?? $"It.IsNull<{typeof(T).FormatType()}>()";

#if !DEBUG
		[System.Diagnostics.DebuggerNonUserCode]
#endif
		private sealed class SharedNullParameterMatch() : NullParameterMatch<T>(null)
		{
			protected override IParameterWithCallback<T> AddCallback(Action<T> callback)
				=> ((IParameterWithCallback<T>)new NullParameterMatch<T>(null)).Do(callback);
		}
	}
}
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
#pragma warning restore S3453 // This class can't be instantiated; make its constructor 'public'.
