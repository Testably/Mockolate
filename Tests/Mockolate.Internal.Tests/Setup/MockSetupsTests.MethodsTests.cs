using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public partial class MockSetupsTests
{
	public class MethodsTests
	{
		[Test]
		public async Task AddAndRetrieve_ShouldReturnCorrectCount()
		{
			MockSetups.MethodSetups setups = new();
			FakeMethodSetup setup1 = new();
			FakeMethodSetup setup2 = new();

			setups.Add(setup1);
			setups.Add(setup2);

			await That(setups.Count).IsEqualTo(2);
		}

		[Test]
		public async Task Stress_ShouldMaintainCountAfterManyAdds()
		{
			MockSetups.MethodSetups setups = new();

			for (int r = 0; r < 10; r++)
			{
				Parallel.For(0, 100, _ => setups.Add(new FakeMethodSetup()));
			}

			await That(setups.Count).IsEqualTo(1000);
		}

		[Test]
		public async Task ThreadSafety_ShouldHandleParallelAdds()
		{
			MockSetups.MethodSetups setups = new();

			Parallel.For(0, 200, _ => setups.Add(new FakeMethodSetup()));

			await That(setups.Count).IsEqualTo(200);
		}
	}
}
