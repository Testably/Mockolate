using System.Reflection;
using Mockolate.XUnit.Tests.Domain;
using Xunit;

namespace Mockolate.XUnit.Tests;

public sealed class AutoMockRowTests
{
	[Fact]
	public async Task Create_WithSeveralParametersOfTheTypeAndNoMatchingName_ShouldThrow()
	{
		AutoMockRow row = new(typeof(AutoMockRowTests).Assembly, GetParameters(nameof(Ambiguous)), []);

		void Act()
			=> row.Create();

		await That(Act).Throws<InvalidOperationException>()
			.WithMessage("The constructor parameter 'source' of type 'Mockolate.XUnit.Tests.Domain.IAccount' matches the test parameters 'first', 'second'; name one of them 'source' or mark the others [Transient].");
	}

	[Fact]
	public async Task GetData_WithMoreArgumentsThanParameters_ShouldThrow()
	{
		AutoMockDataAttribute attribute = new(1, 2);

		void Act()
			=> attribute.GetData(typeof(AutoMockRowTests).GetMethod(nameof(Single), BindingFlags.NonPublic | BindingFlags.Static)!,
				new Xunit.Sdk.DisposalTracker());

		await That(Act).Throws<InvalidOperationException>()
			.WithMessage("AutoMockDataAttribute has 2 arguments, but the test method has only 1 parameters.");
	}

	private static ParameterInfo[] GetParameters(string methodName)
		=> typeof(AutoMockRowTests).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters();

	private static void Ambiguous(TransferService sut, IAccount first, IAccount second)
	{
	}

	private static void Single(int value)
	{
	}
}
