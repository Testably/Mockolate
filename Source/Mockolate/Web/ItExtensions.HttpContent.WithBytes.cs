using System.Linq;

namespace Mockolate.Web;

#pragma warning disable MA0038 // Make method static (deprecated, use CA1822 instead)
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
public static partial class ItExtensions
{
	/// <inheritdoc cref="IHttpContentParameter" />
	extension(IHttpContentParameter parameter)
	{
		/// <summary>
		///     Expects the binary content to be byte-for-byte equal to <paramref name="bytes" />.
		/// </summary>
		/// <param name="bytes">The expected byte payload.</param>
		/// <returns>The same <see cref="IHttpContentParameter" /> for chaining additional <c>.With*</c> constraints.</returns>
		/// <remarks>
		///     Shorthand for <c>.WithBytes(actual =&gt; bytes.SequenceEqual(actual))</c>. For a custom predicate &#8212;
		///     e.g. size or header checks &#8212; use the predicate overload.
		/// </remarks>
		public IHttpContentParameter WithBytes(byte[] bytes)
			=> parameter.WithBytes(bytes.SequenceEqual);
	}
}
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#pragma warning restore MA0038 // Make method static (deprecated, use CA1822 instead)
