#if NET8_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Linq;
using Mockolate.Internals;

namespace Mockolate.Setup;

/// <summary>
///     Wraps a <see cref="ReadOnlySpan{T}" /> of <typeparamref name="T" /> to be used as a generic type parameter.
/// </summary>
#if !DEBUG
[DebuggerNonUserCode]
#endif
public class ReadOnlySpanWrapper<T>
{
	/// <inheritdoc cref="ReadOnlySpanWrapper{T}" />
	public ReadOnlySpanWrapper(ReadOnlySpan<T> span)
	{
		ReadOnlySpanValues = span.ToArray();
	}

	/// <summary>
	///     Gets the array of values contained in the read-only span.
	/// </summary>
	public T[] ReadOnlySpanValues { get; }

	/// <summary>
	///     Implicitly converts a <see cref="ReadOnlySpanWrapper{T}" /> to a <see cref="ReadOnlySpan{T}" />.
	///     A <see langword="null" /> wrapper yields <see langword="default" />(<see cref="ReadOnlySpan{T}" />).
	/// </summary>
	public static implicit operator ReadOnlySpan<T>(ReadOnlySpanWrapper<T>? wrapper)
	{
		return wrapper is null ? default : new ReadOnlySpan<T>(wrapper.ReadOnlySpanValues);
	}

	/// <summary>
	///     Implicitly converts a <see cref="ReadOnlySpan{T}" /> to a <see cref="ReadOnlySpanWrapper{T}" />.
	/// </summary>
	public static implicit operator ReadOnlySpanWrapper<T>(ReadOnlySpan<T> span)
	{
		return new ReadOnlySpanWrapper<T>(span);
	}

	/// <inheritdoc cref="object.Equals(object?)" />
	/// <remarks>
	///     Compares <see cref="ReadOnlySpanValues" /> element-wise: a span-typed property or indexer
	///     value is matched through <c>EqualityComparer&lt;ReadOnlySpanWrapper&lt;T&gt;&gt;.Default</c>,
	///     so reference equality would make <c>Set(someSpan)</c> never match the recorded span.
	/// </remarks>
	public override bool Equals(object? obj)
		=> obj is ReadOnlySpanWrapper<T> other && ReadOnlySpanValues.SequenceEqual(other.ReadOnlySpanValues);

	/// <inheritdoc cref="object.GetHashCode()" />
	public override int GetHashCode()
	{
		HashCode hashCode = new();
		foreach (T value in ReadOnlySpanValues)
		{
			hashCode.Add(value);
		}

		return hashCode.ToHashCode();
	}

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"ReadOnlySpan<{typeof(T).FormatType()}>[{string.Join(", ", ReadOnlySpanValues)}]";
}
#endif
