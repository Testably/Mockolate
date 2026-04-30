using Mockolate.Parameters;

namespace Mockolate.Internal.Tests.Parameters;

public class ParameterMatchAdapterTests
{
	[Test]
	public async Task ParameterMatch_FallsBackToBoxedMatchesCall()
	{
		IParameter<int> match = IParameter<int>.CreateMock();
		match.Mock.Setup.Matches(It.IsAny<object?>())
			.Throws(new Exception("Matches(object) is called"));
		IParameterMatch<int> adapter = match.AsParameterMatch();

		void Act()
		{
			adapter.Matches(1);
		}

		await That(Act).Throws<Exception>().WithMessage("Matches(object) is called");
	}

	[Test]
	public async Task ParameterMatch_PrefersUnboxedMatchesCall()
	{
		IParameter<int> match = IParameter<int>.CreateMock().Implementing<IParameterMatch<int>>();
		match.Mock.Setup.Matches(It.IsAny<object?>())
			.Throws(() => new Exception("Matches(object) should not be called"));
		IParameterMatch<int> adapter = match.AsParameterMatch();

		bool result = adapter.Matches(1);

		await That(result).IsFalse();
	}
}
