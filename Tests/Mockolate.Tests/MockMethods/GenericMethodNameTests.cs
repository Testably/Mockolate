using System.Collections.Generic;
using System.Linq;

namespace Mockolate.Tests.MockMethods;

public sealed class GenericMethodNameTests
{
	[Test]
	public async Task Interaction_WithTypeArgument_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();

		sut.Accept<string>("a");

		await That(SingleInteraction(sut)).IsEqualTo("invoke method Accept<string>(\"a\")");
	}

	[Test]
	public async Task Interaction_WithGenericTypeArgument_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();

		sut.Accept<List<string>>([]);

		await That(SingleInteraction(sut)).IsEqualTo(
			"invoke method Accept<List<string>>(System.Collections.Generic.List`1[System.String])");
	}

	[Test]
	public async Task Interaction_WithTwoTypeArguments_ShouldNameTheMethodWithBothTypeArguments()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();

		sut.Two<string, int>("x", 2);

		await That(SingleInteraction(sut)).IsEqualTo("invoke method Two<string, int>(\"x\", 2)");
	}

	[Test]
	public async Task Interaction_WithoutParameters_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();

		sut.NoArg<string>();

		await That(SingleInteraction(sut)).IsEqualTo("invoke method NoArg<string>()");
	}

	[Test]
	public async Task Interaction_WithFiveParameters_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();

		sut.Five<string>("a", 2, 3, 4, 5);

		await That(SingleInteraction(sut)).IsEqualTo("invoke method Five<string>(\"a\", 2, 3, 4, 5)");
	}

	[Test]
	public async Task Interaction_OnGenericInterface_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethodsOf<string> sut = IGenericMethodsOf<string>.CreateMock();

		sut.Accept<int>("a", 1);

		await That(SingleInteraction(sut)).IsEqualTo("invoke method Accept<int>(\"a\", 1)");
	}

	[Test]
	public async Task Setup_WithTypeArgument_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();
		sut.Mock.Setup.Accept<string>(It.IsAny<string>());

		await That(SingleUnusedSetup(sut)).IsEqualTo("void Accept<string>(It.IsAny<string>())");
	}

	[Test]
	public async Task Setup_WithFiveParameters_ShouldNameTheMethodWithTheTypeArgument()
	{
		IGenericMethods sut = IGenericMethods.CreateMock();
		sut.Mock.Setup.Five<string>(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());

		await That(SingleUnusedSetup(sut)).IsEqualTo(
			"int Five<string>(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())");
	}

	private static string? SingleInteraction(object sut)
		=> ((IMock)sut).MockRegistry.Interactions.GetUnverifiedInteractions().Single().ToString();

	private static string? SingleUnusedSetup(object sut)
		=> ((IMock)sut).MockRegistry.GetUnusedSetups(new MockInteractions(0)).Single().ToString();

	public interface IGenericMethods
	{
		void Accept<T>(T value);
		void Two<T1, T2>(T1 a, T2 b);
		void NoArg<T>();
		int Five<T>(T a, int b, int c, int d, int e);
	}

	public interface IGenericMethodsOf<TItem>
	{
		void Accept<T>(TItem item, T value);
	}
}
