using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.TestHelpers;

internal sealed class FakeMethodMatch : IMethodMatch
{
	private readonly bool _matches;
	internal FakeMethodMatch(bool matches) { _matches = matches; }
	public bool Matches(MethodInvocation methodInvocation) => _matches;
}

internal sealed class FakeMethodSetup : MethodSetup
{
	internal FakeMethodSetup(string name = "foo") : base(name) { }

	protected override bool MatchesInteraction(IMethodInteraction interaction) => false;
}
