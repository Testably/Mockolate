using System.Collections.Generic;
using System.Reflection;
using Mockolate.Exceptions;
using Mockolate.Integrations;
using Mockolate.TUnit.Tests.Domain;

namespace Mockolate.TUnit.Tests;

public sealed class AutoMockRowTests
{
	[Test]
	public async Task Create_WithSeveralParametersOfTheTypeAndNoMatchingName_ShouldThrow()
	{
		AutoMockRow row = CreateRow(nameof(Ambiguous));

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("The constructor parameter 'source' of type 'Mockolate.TUnit.Tests.Domain.IAccount' matches the test parameters 'first', 'second'; name one of them 'source' or mark the others [Transient].");
	}

	[Test]
	public async Task Create_WithNameDifferingInCase_ShouldMatchTheParameter()
	{
		AutoMockRow row = CreateRow(nameof(DifferentCase));

		object?[] values = row.Create();

		await That(values).HasCount(3);
		await That(values[0]).IsNotNull();
	}

	[Test]
	public async Task Create_WithMoreArgumentsThanParameters_ShouldThrow()
	{
		void Act()
			=> CreateRow(nameof(DifferentCase), 1, 2, 3, 4);

		await That(Act).Throws<MockException>()
			.WithMessage("AutoMockArguments has 4 arguments, but the test method has only 3 parameters.");
	}

	[Test]
	public async Task Create_WithParameterThatCannotBeCreated_ShouldThrow()
	{
		AutoMockRow row = CreateRow(nameof(Uncreatable));

		void Act()
			=> row.Create();

		await That(Act).Throws<MockException>()
			.WithMessage("Mockolate cannot create the parameter 'service' of type 'Mockolate.TUnit.Tests.AutoMockRowTests+IUncreatableService'. Pass it as an argument, or register a default value for its type through AutoMockArgumentsAttribute.DefaultBehavior.");
	}

	[Test]
	public async Task Create_ShouldOnlyAskForTheTransientMarkerOfParametersOfAMatchingType()
	{
		List<int> asked = [];
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetTypes(nameof(DifferentCase)),
			GetNames(nameof(DifferentCase)), index =>
			{
				asked.Add(index);
				return false;
			}, [], null);

		row.Create();

		await That(asked).IsEqualTo([1, 2,]);
	}

	private static AutoMockRow CreateRow(string methodName, params object?[] arguments)
		=> new(typeof(AutoMockRowTests).Assembly, GetTypes(methodName), GetNames(methodName), _ => false, arguments, null);

	private static ParameterInfo[] GetParameters(string methodName)
		=> typeof(AutoMockRowTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters();

	private static Type[] GetTypes(string methodName)
		=> Array.ConvertAll(GetParameters(methodName), parameter => parameter.ParameterType);

	private static string[] GetNames(string methodName)
		=> Array.ConvertAll(GetParameters(methodName), parameter => parameter.Name!);

	private static void Ambiguous(TransferService sut, IAccount first, IAccount second)
	{
	}

	private static void DifferentCase(TransferService sut, IAccount Source, IAccount TARGET)
	{
	}

	private static void Uncreatable(IUncreatableService service)
	{
	}

	public interface IUncreatableService;
}
