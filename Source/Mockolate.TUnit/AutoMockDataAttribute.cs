using System;
using System.Collections.Generic;
using System.Reflection;
using TUnit.Core;

namespace Mockolate.TUnit;

/// <summary>
///     Fills every parameter of the test method with a mock, or for a concrete class with an instance whose
///     constructor dependencies are mocks.
/// </summary>
/// <remarks>
///     A constructor dependency of a class under test gets the argument of the test parameter of the same type, so
///     the test can set up and verify the mock that the class under test uses.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AutoMockDataAttribute : UntypedDataSourceGeneratorAttribute, IMockDataSource
{
	/// <inheritdoc />
	protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
	{
		Assembly assembly = dataGeneratorMetadata.TestInformation?.Type.Assembly
		                    ?? throw new NotSupportedException(
			                    $"{nameof(AutoMockDataAttribute)} needs the test method information.");
		ParameterMetadata[] parameters = Array.ConvertAll(dataGeneratorMetadata.MembersToGenerate,
			member => member as ParameterMetadata ?? throw new NotSupportedException(
				$"{nameof(AutoMockDataAttribute)} only supports method parameters, not '{member.Name}'."));
		yield return () => new AutoMockRow(assembly, parameters).Create();
	}
}
