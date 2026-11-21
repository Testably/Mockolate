using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.TestHelpers;

internal sealed class FakeMethodSetup : MethodSetup
{
	internal FakeMethodSetup(string name = "foo") : base(name) { }

	protected override bool MatchesInteraction(IMethodInteraction interaction) => false;
}
