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
		public async Task GetLatestOrDefault_OnEmptyStorage_ShouldReturnNullWithoutThrowing()
		{
			MockSetups.MethodSetups setups = new();

			MethodSetup? result = setups.GetLatestOrDefault(_ => true);

			await That(result).IsNull();
		}

		[Test]
		public async Task GetLatestOrDefault_ShouldReturnLatestMatching()
		{
			MockSetups.MethodSetups setups = new();
			FakeMethodSetup setup1 = new();
			FakeMethodSetup setup2 = new();
			setups.Add(setup1);
			setups.Add(setup2);

			MethodSetup? result = setups.GetLatestOrDefault(_ => true);

			await That(result).IsEqualTo(setup2);
		}

		[Test]
		public async Task GetLatestOrDefault_WithSingleMatchingSetup_ShouldReturnIt()
		{
			MockSetups.MethodSetups setups = new();
			FakeMethodSetup setup = new();
			setups.Add(setup);

			MethodSetup? result = setups.GetLatestOrDefault(_ => true);

			await That(result).IsSameAs(setup);
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
		public async Task ThreadSafety_ShouldAllowConcurrentReadsDuringAdds()
		{
			MockSetups.MethodSetups setups = new();
			FakeMethodSetup needle = new();
			Parallel.For(0, 200, i =>
			{
				FakeMethodSetup setup = new();
				setups.Add(setup);
				_ = setups.GetLatestOrDefault(s => ReferenceEquals(s, needle));
			});
			setups.Add(needle);

			MethodSetup? result = setups.GetLatestOrDefault(s => ReferenceEquals(s, needle));

			await That(result).IsEqualTo(needle);
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
