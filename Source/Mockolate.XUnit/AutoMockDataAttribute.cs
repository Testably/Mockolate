using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Mockolate.XUnit;

/// <summary>
///     Passes the <see cref="Arguments" /> to the first parameters of the theory, like xUnit's <c>InlineData</c>, and
///     fills every remaining parameter with a mock of an interface, or for a concrete class with an instance whose
///     constructor dependencies are mocks.
/// </summary>
/// <remarks>
///     A constructor dependency of a class under test gets the argument of the test parameter of the same type, given
///     or created, so the test can set up and verify the mock that the class under test uses; several dependencies of
///     that type share it. When several test parameters have the type, the one with the name of the constructor
///     parameter (ignoring case) is used, and without one the creation fails. A <see cref="TransientAttribute" />
///     parameter is not shared. Classes are always constructed, never mocked. Each attribute on a method is one test
///     case.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class AutoMockDataAttribute : DataAttribute, IMockDataSource
{
	/// <summary>
	///     Fills every parameter of the theory, or the parameters after the given <paramref name="arguments" />.
	/// </summary>
	/// <param name="arguments">The arguments of the first parameters of the theory.</param>
	public AutoMockDataAttribute(params object?[]? arguments)
	{
		// A single null argument binds to the array itself.
		Arguments = arguments ?? [null,];
	}

	/// <summary>
	///     The arguments of the first parameters of the theory.
	/// </summary>
	public object?[] Arguments { get; }

	/// <inheritdoc />
	public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod,
		DisposalTracker disposalTracker)
	{
		ParameterInfo[] parameters = testMethod.GetParameters();
		if (Arguments.Length > parameters.Length)
		{
			throw new InvalidOperationException(
				$"{nameof(AutoMockDataAttribute)} has {Arguments.Length} arguments, but the test method has only {parameters.Length} parameters.");
		}

		// The generated code lives in the assembly that declares the test method, which for an inherited test differs
		// from the assembly of the test class.
		Assembly assembly = testMethod.DeclaringType!.Assembly;
		object?[] row = new AutoMockRow(assembly, parameters, Arguments).Create();
		return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>([ConvertDataRow(row),]);
	}

	/// <summary>
	///     Mocks cannot be serialized, so the rows are created when the theory runs instead of during discovery.
	/// </summary>
	public override bool SupportsDiscoveryEnumeration()
		=> false;
}
