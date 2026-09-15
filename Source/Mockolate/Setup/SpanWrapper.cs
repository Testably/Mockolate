#if NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Linq;
using Mockolate.Internals;

namespace Mockolate.Setup;

/// <summary>
///     Wraps a <see cref="Span{T}" /> of <typeparamref name="T" /> to be used as a generic type parameter.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class SpanWrapper<T>
{
	/// <inheritdoc cref="SpanWrapper{T}" />
	public SpanWrapper(Span<T> span)
	{
		SpanValues = span.ToArray();
	}

	/// <summary>
	///     Gets the array of values contained in the span.
	/// </summary>
	public T[] SpanValues { get; }

	/// <summary>
	///     Implicitly converts a <see cref="SpanWrapper{T}" /> to a <see cref="Span{T}" />. A
	///     <see langword="null" /> wrapper yields <see langword="default" />(<see cref="Span{T}" />).
	/// </summary>
	public static implicit operator Span<T>(SpanWrapper<T>? wrapper)
	{
		return wrapper is null ? default : new Span<T>(wrapper.SpanValues);
	}

	/// <summary>
	///     Implicitly converts a <see cref="Span{T}" /> to a <see cref="SpanWrapper{T}" />.
	/// </summary>
	public static implicit operator SpanWrapper<T>(Span<T> span)
	{
		return new SpanWrapper<T>(span);
	}

	/// <inheritdoc cref="object.Equals(object?)" />
	/// <remarks>
	///     Compares <see cref="SpanValues" /> element-wise: a span-typed property or indexer value is
	///     matched through <c>EqualityComparer&lt;SpanWrapper&lt;T&gt;&gt;.Default</c>, so reference
	///     equality would make <c>Set(someSpan)</c> never match the recorded span.
	/// </remarks>
	public override bool Equals(object? obj)
		=> obj is SpanWrapper<T> other && SpanValues.SequenceEqual(other.SpanValues);

	/// <inheritdoc cref="object.GetHashCode()" />
	public override int GetHashCode()
	{
		HashCode hashCode = new();
		foreach (T value in SpanValues)
		{
			hashCode.Add(value);
		}

		return hashCode.ToHashCode();
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"Span<{typeof(T).FormatType()}>[{string.Join(", ", SpanValues)}]";
}
#endif
