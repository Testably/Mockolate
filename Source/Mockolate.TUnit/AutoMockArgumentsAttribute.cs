using System;
using System.Collections.Generic;
using System.Reflection;
using TUnit.Core;

namespace Mockolate.TUnit;

/// <summary>
///     Passes the <see cref="Arguments" /> to the first parameters of the test method, like TUnit's <c>Arguments</c>,
///     and fills every remaining parameter with a mock of an interface, or for a concrete class with an instance whose
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
public sealed class AutoMockArgumentsAttribute : UntypedDataSourceGeneratorAttribute, IMockDataSource
{
	/// <summary>
	///     Fills every parameter of the test method, or the parameters after the given <paramref name="arguments" />.
	/// </summary>
	/// <param name="arguments">The arguments of the first parameters of the test method.</param>
	public AutoMockArgumentsAttribute(params object?[]? arguments)
	{
		// A single null argument binds to the array itself.
		Arguments = arguments ?? [null,];
	}

	/// <summary>
	///     The arguments of the first parameters of the test method.
	/// </summary>
	public object?[] Arguments { get; }

	/// <inheritdoc />
	protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
	{
		ParameterMetadata[] parameters = Array.ConvertAll(dataGeneratorMetadata.MembersToGenerate,
			member => member as ParameterMetadata ?? throw new NotSupportedException(
				$"{nameof(AutoMockArgumentsAttribute)} only supports method parameters, not '{member.Name}'."));
		if (Arguments.Length > parameters.Length)
		{
			throw new InvalidOperationException(
				$"{nameof(AutoMockArgumentsAttribute)} has {Arguments.Length} arguments, but the test method has only {parameters.Length} parameters.");
		}

		Assembly assembly = GetDeclaringAssembly(parameters, dataGeneratorMetadata);
		yield return () => new AutoMockRow(assembly, parameters, Arguments).Create();
	}

	/// <summary>
	///     The generated code lives in the assembly that declares the test method, which for an inherited test differs
	///     from the assembly of the test class.
	/// </summary>
	private static Assembly GetDeclaringAssembly(ParameterMetadata[] parameters,
		DataGeneratorMetadata dataGeneratorMetadata)
		=> (parameters.Length > 0 ? parameters[0].ReflectionInfo.Member.DeclaringType?.Assembly : null)
		   ?? dataGeneratorMetadata.TestInformation?.Type.Assembly
		   ?? throw new NotSupportedException($"{nameof(AutoMockArgumentsAttribute)} needs the test method information.");
}
