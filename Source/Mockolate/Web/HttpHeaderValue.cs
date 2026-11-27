namespace Mockolate.Web;

/// <summary>
///     Expected value of an HTTP header, used with <c>It.IsHttpContent().WithHeaders(...)</c> and related matchers.
/// </summary>
/// <remarks>
///     The base class performs ordinal string equality. Subclasses (created via the static helpers on the derived
///     types) can match by wildcard, regex or a predicate. A plain <see langword="string" /> is implicitly convertible
///     to an <see cref="HttpHeaderValue" /> for convenience.
/// </remarks>
public class HttpHeaderValue
{
	private readonly string _value;

	/// <inheritdoc cref="HttpHeaderValue" />
	/// <param name="value">The exact header value to match.</param>
	public HttpHeaderValue(string value)
	{
		_value = value;
	}

	/// <summary>
	///     Returns <see langword="true" /> when the observed <paramref name="headerValue" /> matches this expectation.
	/// </summary>
	/// <param name="headerValue">The header value observed on the request.</param>
	/// <returns><see langword="true" /> when it matches; <see langword="false" /> otherwise.</returns>
	public virtual bool Matches(string headerValue)
		=> _value.Equals(headerValue, System.StringComparison.Ordinal);

	/// <summary>
	///     Implicit conversion from <see langword="string" /> to <see cref="HttpHeaderValue" /> &#8212; lets callers
	///     write a raw string in place of a wrapped value.
	/// </summary>
	/// <param name="value">The exact header value to match.</param>
	public static implicit operator HttpHeaderValue(string value)
	{
		return new HttpHeaderValue(value);
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> _value;
}
