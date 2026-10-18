namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class CombinedMockTests
	{
		[Test]
		public async Task DistinctReturnTypes_SameParameterTypes_ShouldNotCollide()
		{
			IFooIndexer sut = IFooIndexer.CreateMock().Implementing<IBarIndexer>().Implementing<IBazIndexer>();
			IBarIndexer barView = (IBarIndexer)sut;
			IBazIndexer bazView = (IBazIndexer)sut;

			sut[1] = "foo";
			barView[1] = 42;
			bazView[1] = 43;

			await That(sut[1]).IsEqualTo("foo");
			await That(barView[1]).IsEqualTo(42);
			await That(bazView[1]).IsEqualTo(43);
		}

		[Test]
		public async Task VirtualClassIndexer_AndExplicitInterfaceImplWithSameSignature_ShouldNotCollide()
		{
			BaseWithVirtualIndexer sut = BaseWithVirtualIndexer.CreateMock().Implementing<IExplicitIndexer>();
			IExplicitIndexer explicitView = (IExplicitIndexer)sut;

			sut[1] = 100;
			explicitView[1] = 200;

			await That(sut[1]).IsEqualTo(100);
			await That(explicitView[1]).IsEqualTo(200);
		}

		public class BaseWithVirtualIndexer
		{
			public virtual int this[int x]
			{
				get => 0;
				set { }
			}
		}

		public interface IExplicitIndexer
		{
			int this[int x] { get; set; }
		}

		public interface IFooIndexer
		{
			string this[int x] { get; set; }
		}

		public interface IBarIndexer
		{
			int this[int x] { get; set; }
		}

		public interface IBazIndexer
		{
			int this[int x] { get; set; }
		}
	}
}
