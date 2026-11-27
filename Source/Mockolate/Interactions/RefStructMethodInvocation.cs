#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mockolate.Internals;

namespace Mockolate.Interactions;

/// <summary>
///     An invocation of a method that has at least one ref struct parameter.
/// </summary>
/// <remarks>
///     <para>
///         Ref struct values cannot be boxed or stored as fields on a heap object, so only the
///         method name and parameter names are retained. This is a structurally separate
///         interaction type from <see cref="MethodInvocation{T}" /> because a class field of ref
///         struct type is forbidden by the type system.
///     </para>
///     <para>
///         As a consequence, post-hoc verification of any parameter's value is not supported when
///         the method has at least one ref struct parameter. For methods with mixed ref-struct and
///         non-ref-struct parameters, the non-ref-struct values are likewise not recorded — the
///         entire invocation collapses to a name-only placeholder. Matchers supplied at setup time
///         still apply at call time; only post-hoc matcher-on-value verification is lost.
///     </para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
#if !DEBUG
[DebuggerNonUserCode]
#endif
public sealed class RefStructMethodInvocation : IMethodInteraction
{
	/// <inheritdoc cref="RefStructMethodInvocation" />
	public RefStructMethodInvocation(string name, params string[] parameterNames)
	{
		Name = name;
		ParameterNames = parameterNames;
	}

	/// <summary>
	///     The name of the method.
	/// </summary>
	public string Name { get; }

	/// <summary>
	///     The names of the method's parameters, in declaration order.
	/// </summary>
	public IReadOnlyList<string> ParameterNames { get; }

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
	{
		string shortName = ValueFormatter.FormatMethodName(Name);
		if (ParameterNames.Count == 0)
		{
			return $"invoke method {shortName}()";
		}

		string body = string.Join(", ", ParameterNames.Select(p => $"{p}: <ref struct>"));
		return $"invoke method {shortName}({body})";
	}
}
#endif
