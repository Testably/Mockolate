using System.Text;

namespace Mockolate.SourceGenerators.Sources;

internal static partial class Sources
{
	/// <summary>
	///     The union-typed setup/verify argument: a hand-written <c>[Union]</c> struct with the case types
	///     <c>IParameter&lt;T&gt;</c> and <c>T?</c>, so that a setup overload can take a matcher or a literal value
	///     through a single parameter. Stored in typed slots rather than a boxed <c>object</c>, so that a value type
	///     <c>T</c> does not allocate. Only emitted when the compilation supports C# unions.
	/// </summary>
	/// <param name="emitUnionAttributePolyfill">
	///     <see langword="true" /> when neither the referenced framework nor the consuming assembly declares
	///     <c>System.Runtime.CompilerServices.UnionAttribute</c> (it ships with .NET 11), so the file has to
	///     declare it.
	/// </param>
	public static string ParameterArg(bool emitUnionAttributePolyfill)
	{
		StringBuilder sb = InitializeBuilder();

		sb.AppendLine("#nullable enable");
		sb.AppendLine();
		if (emitUnionAttributePolyfill)
		{
			sb.Append("""
			          namespace System.Runtime.CompilerServices
			          {
			          	/// <summary>
			          	///     Polyfill for the attribute that marks a union type; the runtime ships it starting with .NET 11.
			          	/// </summary>
			          	[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false)]
			          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
			          	internal sealed class UnionAttribute : global::System.Attribute
			          	{
			          	}
			          }

			          """);
		}

		sb.Append("""
		          namespace Mockolate
		          {
		          	/// <summary>
		          	///     A setup or verify argument that is either an <see cref="global::Mockolate.It">It</see> matcher
		          	///     (<see cref="global::Mockolate.Parameters.IParameter{T}" />) or a literal value of type <typeparamref name="T" />.
		          	/// </summary>
		          	/// <remarks>
		          	///     Both case types convert implicitly, so <c>Setup.Method(42)</c> and <c>Setup.Method(It.IsAny&lt;int&gt;())</c>
		          	///     bind to the same overload. A <see langword="default" /> instance stands for the literal <c>default(T)</c>.
		          	/// </remarks>
		          	[global::System.Runtime.CompilerServices.Union]
		          	[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		          	internal readonly struct ParameterArg<T>
		          	{
		          		private const byte MatcherTag = 1;
		          		private const byte LiteralTag = 2;

		          		private readonly global::Mockolate.Parameters.IParameter<T>? _matcher;
		          		private readonly T? _literal;
		          		private readonly byte _tag;

		          		/// <summary>
		          		///     Creates the matcher case.
		          		/// </summary>
		          		public ParameterArg(global::Mockolate.Parameters.IParameter<T> matcher)
		          		{
		          			_matcher = matcher;
		          			_literal = default;
		          			_tag = MatcherTag;
		          		}

		          		/// <summary>
		          		///     Creates the literal value case.
		          		/// </summary>
		          		public ParameterArg(T? literal)
		          		{
		          			_matcher = null;
		          			_literal = literal;
		          			_tag = LiteralTag;
		          		}

		          		/// <summary>
		          		///     The contained matcher or literal value, boxed. Part of the union pattern; the generated mocks use the
		          		///     typed accessors instead.
		          		/// </summary>
		          		public object? Value => _tag switch
		          		{
		          			MatcherTag => _matcher,
		          			LiteralTag => _literal,
		          			_ => null,
		          		};

		          		/// <summary>
		          		///     <see langword="true" /> unless this is the <see langword="default" /> instance.
		          		/// </summary>
		          		public bool HasValue => _tag != 0;

		          		/// <summary>
		          		///     <see langword="true" /> when the argument is a literal value (including the <see langword="default" /> instance).
		          		/// </summary>
		          		public bool IsLiteral => _tag != MatcherTag;

		          		/// <summary>
		          		///     The literal value; <c>default(T)</c> for the matcher case and the <see langword="default" /> instance.
		          		/// </summary>
		          		public T? Literal => _literal;

		          		/// <summary>
		          		///     Gets the matcher, when this is the matcher case.
		          		/// </summary>
		          		public bool TryGetValue(out global::Mockolate.Parameters.IParameter<T>? matcher)
		          		{
		          			matcher = _matcher;
		          			return _tag == MatcherTag;
		          		}

		          		/// <summary>
		          		///     Gets the literal value, when this is the literal case.
		          		/// </summary>
		          		public bool TryGetValue(out T? literal)
		          		{
		          			literal = _literal;
		          			return _tag == LiteralTag;
		          		}

		          		/// <summary>
		          		///     The <see cref="global::Mockolate.Parameters.IParameterMatch{T}" /> for this argument: the matcher itself,
		          		///     or an equality match for the literal value.
		          		/// </summary>
		          		public global::Mockolate.Parameters.IParameterMatch<T> ToParameterMatch()
		          		{
		          			if (_tag != MatcherTag)
		          			{
		          				return (global::Mockolate.Parameters.IParameterMatch<T>)global::Mockolate.It.IsValue<T>(_literal!);
		          			}

		          			if (_matcher is null)
		          			{
		          				return (global::Mockolate.Parameters.IParameterMatch<T>)global::Mockolate.It.IsNull<T>("null");
		          			}

		          			return _matcher is global::Mockolate.Parameters.IParameterMatch<T> direct
		          				? direct
		          				: new CovariantAdapter(_matcher);
		          		}

		          		/// <inheritdoc cref="object.ToString()" />
		          		public override string ToString() => _tag switch
		          		{
		          			MatcherTag => _matcher?.ToString() ?? "null",
		          			_ => _literal?.ToString() ?? "null",
		          		};

		          		private sealed class CovariantAdapter(global::Mockolate.Parameters.IParameter inner) : global::Mockolate.Parameters.IParameterMatch<T>
		          		{
		          			public bool Matches(T value) => inner.Matches(value);
		          			public void InvokeCallbacks(T value) => inner.InvokeCallbacks(value);
		          			public override string? ToString() => inner.ToString();
		          		}
		          	}
		          }
		          #nullable disable
		          """);

		return sb.ToString();
	}
}
