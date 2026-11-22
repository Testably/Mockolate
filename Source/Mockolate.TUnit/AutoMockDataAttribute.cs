using System;
using System.Collections.Generic;
using System.Reflection;
using TUnit.Core;

namespace Mockolate.TUnit;

/// <summary>
///     Fills every parameter of the test method with a mock of an interface, or for a concrete class with an
///     instance whose constructor dependencies are mocks.
/// </summary>
/// <remarks>
///     A constructor dependency of a class under test gets the argument of the test parameter of the same type, so
///     the test can set up and verify the mock that the class under test uses; several dependencies of that type share
///     it. Classes are always constructed, never mocked.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AutoMockDataAttribute : UntypedDataSourceGeneratorAttribute, IMockDataSource
{
	/// <inheritdoc />
	protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
	{
		ParameterMetadata[] parameters = Array.ConvertAll(dataGeneratorMetadata.MembersToGenerate,
			member => member as ParameterMetadata ?? throw new NotSupportedException(
				$"{nameof(AutoMockDataAttribute)} only supports method parameters, not '{member.Name}'."));
		Assembly assembly = GetDeclaringAssembly(parameters, dataGeneratorMetadata);
		yield return () => new AutoMockRow(assembly, parameters).Create();
	}

	/// <summary>
	///     The generated code lives in the assembly that declares the test method, which for an inherited test differs
	///     from the assembly of the test class.
	/// </summary>
	private static Assembly GetDeclaringAssembly(ParameterMetadata[] parameters,
		DataGeneratorMetadata dataGeneratorMetadata)
		=> (parameters.Length > 0 ? parameters[0].ReflectionInfo.Member.DeclaringType?.Assembly : null)
		   ?? dataGeneratorMetadata.TestInformation?.Type.Assembly
		   ?? throw new NotSupportedException($"{nameof(AutoMockDataAttribute)} needs the test method information.");
}
