using Mockolate.Tests.MockIndexers;
using Mockolate.Tests.MockMethods;
using Mockolate.Tests.MockProperties;

namespace Mockolate.Tests;

public sealed class OverlappingSetupsTests
{
	[Test]
	public async Task Indexer_NarrowMatcherDefinedLater_ShouldWinForMatchingArguments()
	{
		SetupIndexerTests.IIndexerService sut = SetupIndexerTests.IIndexerService.CreateMock();

		sut.Mock.Setup[It.IsAny<int>()].InitializeWith("foo");
		sut.Mock.Setup[It.Is(2)].InitializeWith("bar");

		string matching = sut[2];
		string nonMatching = sut[1];

		await That(matching).IsEqualTo("bar");
		await That(nonMatching).IsEqualTo("foo");
	}

	[Test]
	public async Task Method_BroadMatcherDefinedLater_ShouldWinEvenForArgumentsTheNarrowMatcherCovered()
	{
		SetupMethodTests.IMethodService sut = SetupMethodTests.IMethodService.CreateMock();

		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(2);
		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(1);

		int previouslyNarrowMatch = sut.MyIntMethodWithParameters(0, "foo");

		await That(previouslyNarrowMatch).IsEqualTo(1);
	}

	[Test]
	public async Task Method_MostRecentlyDefinedSetup_ShouldWin_WhenMatchersOverlap()
	{
		SetupMethodTests.IMethodService sut = SetupMethodTests.IMethodService.CreateMock();

		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(1);
		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(2);
		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(3);

		int result = sut.MyIntMethodWithParameters(0, "foo");

		await That(result).IsEqualTo(3);
	}

	[Test]
	public async Task Method_NarrowMatcherDefinedLater_ShouldWinForMatchingArguments()
	{
		SetupMethodTests.IMethodService sut = SetupMethodTests.IMethodService.CreateMock();

		sut.Mock.Setup.MyIntMethodWithParameters(It.IsAny<int>(), It.IsAny<string>()).Returns(1);
		sut.Mock.Setup.MyIntMethodWithParameters(It.Is(0), It.Is("foo")).Returns(2);

		int matching = sut.MyIntMethodWithParameters(0, "foo");
		int nonMatching = sut.MyIntMethodWithParameters(1, "bar");

		await That(matching).IsEqualTo(2);
		await That(nonMatching).IsEqualTo(1);
	}

	[Test]
	public async Task Property_LaterReturnsCall_ShouldReplacePreviousSetup()
	{
		SetupPropertyTests.IPropertyService sut = SetupPropertyTests.IPropertyService.CreateMock();

		sut.Mock.Setup.MyProperty.Returns(1);
		sut.Mock.Setup.MyProperty.Returns(2);
		sut.Mock.Setup.MyProperty.Returns(3);

		int result = sut.MyProperty;

		await That(result).IsEqualTo(3);
	}
}
