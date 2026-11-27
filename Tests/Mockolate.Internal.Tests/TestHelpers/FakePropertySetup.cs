using Mockolate.Interactions;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.TestHelpers;

internal sealed class FakePropertySetup : PropertySetup<int>
{
	internal FakePropertySetup(string name) : base(new MockRegistry(MockBehavior.Default, new MockInteractions(0)),
		name)
	{
	}
}
