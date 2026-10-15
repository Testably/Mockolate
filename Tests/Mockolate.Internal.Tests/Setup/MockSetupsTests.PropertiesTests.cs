using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public partial class MockSetupsTests
{
	public class PropertiesTests
	{
		[Test]
		public async Task Add_DefaultPlaceholderAfterUserSetup_ShouldNotOverwriteUserSetup()
		{
			MockSetups.PropertySetups setups = new();
			FakePropertySetup userSetup = new("p");
			PropertySetup defaultSetup = new PropertySetup.Default<int>("p", 0);

			setups.Add(userSetup);
			setups.Add(defaultSetup);

			setups.TryGetValue("p", out PropertySetup? found);
			await That(found).IsSameAs(userSetup);
			await That(setups.Count).IsEqualTo(1);
		}

		[Test]
		public async Task Add_DefaultPlaceholderOverDefault_ShouldKeepDefaultEntry()
		{
			MockSetups.PropertySetups setups = new();
			PropertySetup firstDefault = new PropertySetup.Default<int>("p", 0);
			PropertySetup secondDefault = new PropertySetup.Default<int>("p", 0);

			setups.Add(firstDefault);
			setups.Add(secondDefault);

			setups.TryGetValue("p", out PropertySetup? found);
			await That(found).IsSameAs(secondDefault);
			await That(setups.Count).IsEqualTo(0);
		}

		[Test]
		public async Task Add_ReplacingDefaultWithUserSetup_ShouldIncrementCountByOne()
		{
			MockSetups.PropertySetups setups = new();
			PropertySetup defaultSetup = new PropertySetup.Default<int>("p", 0);
			FakePropertySetup userSetup = new("p");

			setups.Add(defaultSetup);
			await That(setups.Count).IsEqualTo(0);

			setups.Add(userSetup);

			await That(setups.Count).IsEqualTo(1);
			setups.TryGetValue("p", out PropertySetup? found);
			await That(found).IsSameAs(userSetup);
		}

		[Test]
		public async Task AddDuplicate_ShouldReplaceAndAdjustCount()
		{
			MockSetups.PropertySetups setups = new();
			FakePropertySetup setup1 = new("foo");
			FakePropertySetup setup2 = new("foo");

			setups.Add(setup1);
			setups.Add(setup2);

			await That(setups.Count).IsEqualTo(1);

			setups.TryGetValue("foo", out PropertySetup? found);

			await That(found).IsEqualTo(setup2);
		}

		[Test]
		public async Task Stress_ShouldMaintainCountAfterManyAddsAndReplacements()
		{
			MockSetups.PropertySetups setups = new();

			for (int r = 0; r < 10; r++)
			{
				Parallel.For(0, 100, i => setups.Add(new FakePropertySetup($"p{i}")));
			}

			await That(setups.Count).IsEqualTo(100);
		}

		[Test]
		public async Task ThreadSafety_DuplicateKeyReplacements_ShouldPreserveSingleEntry()
		{
			MockSetups.PropertySetups setups = new();
			Parallel.For(0, 200, _ => setups.Add(new FakePropertySetup("same")));

			await That(setups.Count).IsEqualTo(1);

			bool found = setups.TryGetValue("same", out PropertySetup? setup);

			await That(found).IsTrue();
			await That(setup).IsNotNull();
		}

		[Test]
		public async Task ThreadSafety_ShouldHandleParallelAdds()
		{
			MockSetups.PropertySetups setups = new();

			Parallel.For(0, 200, i => setups.Add(new FakePropertySetup($"p{i}")));

			await That(setups.Count).IsEqualTo(200);
		}

		[Test]
		public async Task TryGetValue_Nonexistent_ShouldReturnFalse()
		{
			MockSetups.PropertySetups setups = new();

			bool result = setups.TryGetValue("bar", out PropertySetup? found);

			await That(result).IsFalse();
			await That(found).IsNull();
		}
	}
}
