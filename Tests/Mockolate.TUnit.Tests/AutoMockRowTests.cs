using System.Reflection;
using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockRowTests
{
	[Test]
	public async Task Create_WithSeveralParametersOfTheTypeAndNoMatchingName_ShouldThrow()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetParameters(nameof(Ambiguous)), []);

		void Act()
			=> row.Create();

		await That(Act).Throws<InvalidOperationException>()
			.WithMessage("The constructor parameter 'source' of type 'Mockolate.TUnit.Tests.Domain.IAccount' matches the test parameters 'first', 'second'; name one of them 'source' or mark the others [Transient].");
	}

	[Test]
	public async Task Create_WithNameDifferingInCase_ShouldMatchTheParameter()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetParameters(nameof(DifferentCase)), []);

		object?[] values = row.Create();

		await That(values).HasCount(3);
		await That(values[0]).IsNotNull();
	}

	private static ParameterMetadata[] GetParameters(string methodName)
		=> Array.ConvertAll(
			typeof(AutoMockRowTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters(),
			parameter => new ParameterMetadata(parameter.ParameterType)
			{
				Name = parameter.Name!,
				TypeInfo = new ConcreteType(parameter.ParameterType),
				ReflectionInfo = parameter,
			});

	private static void Ambiguous(TransferService sut, IAccount first, IAccount second)
	{
	}

	private static void DifferentCase(TransferService sut, IAccount Source, IAccount TARGET)
	{
	}
}
