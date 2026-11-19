using System.Linq;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class AutoMockTests
	{
		private static readonly MockBehavior AutoMocking = MockBehavior.Default.AutoMocking();

		[Test]
		public async Task ClassMock_ShouldPreferTheBaseClassImplementation()
		{
			AutoMockBase sut = AutoMockBase.CreateMock(AutoMocking);

			IAutoMockChild fromBase = sut.GetVirtual();
			IAutoMockChild fromAbstract = sut.GetAbstract();

			await That(fromBase).IsNull().Because("the base class implementation provides the value");
			await That(fromAbstract).IsNotNull();
		}

		[Test]
		public async Task ClassMock_WhenSkippingBaseClass_ShouldReturnMock()
		{
			AutoMockBase sut = AutoMockBase.CreateMock(AutoMocking.SkippingBaseClass());

			IAutoMockChild result = sut.GetVirtual();

			await That(result).IsNotNull();
		}

		[Test]
		public async Task ConcurrentFirstAccess_ShouldReturnTheSameMock()
		{
			const int threads = 8;
			for (int iteration = 0; iteration < 50; iteration++)
			{
				IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);
				using Barrier barrier = new(threads);
				(IAutoMockChild Property, IAutoMockChild Indexer, IAutoMockChild Method)[] results =
					await Task.WhenAll(Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
					{
						barrier.SignalAndWait();
						return (sut.Child, sut[1], sut.GetChild(1));
					})));

				await That(results.All(r => ReferenceEquals(r.Property, results[0].Property) &&
				                            ReferenceEquals(r.Indexer, results[0].Indexer) &&
				                            ReferenceEquals(r.Method, results[0].Method))).IsTrue()
					.Because($"concurrent first reads must not create different auto-mocks (iteration {iteration})");
			}
		}

		[Test]
		public async Task CustomBehaviorWithoutDefault_ShouldReturnMock()
		{
			MockBehavior behavior = new MockBehavior(new NullDefaultValueGenerator()).AutoMocking();
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(behavior);

			IAutoMockChild result = sut.GetChild(1);

			await That(result).IsNotNull()
				.Because("the factory comes with CreateMock and does not depend on MockBehavior.Default");
		}

		[Test]
		public async Task Implementing_ShouldReturnMock()
		{
			IAutoMockChild sut = IAutoMockChild.CreateMock(AutoMocking).Implementing<IAutoMockGeneric<int>>();

			IAutoMockRoot result = sut.GetRoot();

			await That(result).IsNotNull();
		}

		[Test]
		public async Task Initialize_ShouldApplyToAutoMocks()
		{
			MockBehavior behavior = AutoMocking.Initialize<IAutoMockChild>(setup => setup.GetValue().Returns(5));
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(behavior);

			int result = sut.GetChild(1).GetValue();

			await That(result).IsEqualTo(5);
		}

		[Test]
		public async Task Delegate_ShouldReturnSameMock()
		{
			AutoMockChildFactory sut = AutoMockChildFactory.CreateMock(AutoMocking);

			IAutoMockChild first = sut(1);
			IAutoMockChild second = sut(2);

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
		}

		[Test]
		public async Task ExplicitSetupReturningNull_ShouldReturnNull()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);
			sut.Mock.Setup.GetChild(It.IsAny<int>()).Returns((IAutoMockChild)null!);

			IAutoMockChild result = sut.GetChild(1);

			await That(result).IsNull().Because("a setup takes precedence over the auto-mock");
		}

		[Test]
		public async Task FrameworkInterface_ShouldNotBeAutoMocked()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IServiceProvider result = sut.GetServiceProvider();

			await That(result).IsNull();
		}

		[Test]
		public async Task GenericInterface_ShouldReturnMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockGeneric<int> result = sut.GetGeneric();
			result.Mock.Setup.Value.InitializeWith(42);

			await That(sut.GetGeneric().Value).IsEqualTo(42);
		}

		[Test]
		public async Task Indexer_ShouldReturnSameMockPerKey()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = sut[1];
			IAutoMockChild second = sut[1];
			IAutoMockChild otherKey = sut[2];

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
			await That(otherKey).IsNotSameAs(first).Because("an indexer stores its value per key");
		}

		[Test]
		public async Task Indexer_WhenSet_ShouldReturnTheValue()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);
			IAutoMockChild value = IAutoMockChild.CreateMock();

			sut[1] = value;

			await That(sut[1]).IsSameAs(value);
		}

		[Test]
		public async Task IndexerWithFiveKeys_ShouldReturnSameMockPerKey()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = sut[1, 2, 3, 4, 5];
			IAutoMockChild second = sut[1, 2, 3, 4, 5];

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
		}

		[Test]
		public async Task Method_ShouldBeCachedPerMemberAndNotPerArgument()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = sut.GetChild(1);
			IAutoMockChild second = sut.GetChild(2);
			IAutoMockChild viaProperty = sut.Child;

			await That(second).IsSameAs(first);
			await That(viaProperty).IsNotSameAs(first);
		}

		[Test]
		public async Task Method_ShouldReturnSameConfigurableMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			sut.GetChild(1).Mock.Setup.GetValue().Returns(42);

			await That(sut.GetChild(1).GetValue()).IsEqualTo(42);
		}

		[Test]
		public async Task Method_ShouldRecordInteractionsOnTheAutoMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			sut.GetChild(1).GetValue();

			await That(sut.GetChild(1).Mock.Verify.GetValue()).Once();
		}

		[Test]
		public async Task NullableProperty_ShouldReturnMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild? result = sut.NullableChild;

			await That(result).IsNotNull();
		}

		[Test]
		public async Task Property_ShouldReturnSameMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = sut.Child;
			IAutoMockChild second = sut.Child;

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
		}

		[Test]
		public async Task ShouldBeTransitiveAndUseTheSameBehavior()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockRoot root = sut.Child.GetRoot();

			await That(root).IsNotNull().Because("the auto-mock uses the same behavior, so it auto-mocks as well");
			await That(root).IsNotSameAs(sut);
			await That(root.Child).IsNotNull();
		}

		[Test]
		public async Task Task_ShouldReturnSameMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = await sut.GetChildAsync();
			IAutoMockChild second = await sut.GetChildAsync();

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
		}

		[Test]
		public async Task ThrowingWhenNotSetup_ShouldStillThrow()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking.ThrowingWhenNotSetup());

			void Act()
				=> sut.GetChild(1);

			await That(Act).Throws<MockNotSetupException>();
		}

#if NET8_0_OR_GREATER
		[Test]
		public async Task ValueTask_ShouldReturnSameMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = await sut.GetChildValueTaskAsync();
			IAutoMockChild second = await sut.GetChildValueTaskAsync();

			await That(first).IsNotNull()
				.Because("the default ValueTask<T> only wraps the default value of T on .NET 8 or later");
			await That(second).IsSameAs(first);
		}
#endif

		[Test]
		public async Task WithDefaultValueFor_ShouldPreferTheFactory()
		{
			IAutoMockChild expected = IAutoMockChild.CreateMock();
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking.WithDefaultValueFor(() => expected));

			IAutoMockChild result = sut.GetChild(1);

			await That(result).IsSameAs(expected);
		}

		[Test]
		public async Task WithoutAutoMocking_ShouldReturnNull()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock();

			await That(sut.GetChild(1)).IsNull();
			await That(sut.Child).IsNull();
			await That(sut[1]).IsNull();
		}
		public abstract class AutoMockBase
		{
			public abstract IAutoMockChild GetAbstract();
			public virtual IAutoMockChild GetVirtual() => null!;
		}

		private sealed class NullDefaultValueGenerator : IDefaultValueGenerator
		{
			public object? GenerateValue(Type type, params object?[] parameters) => null;
		}
#if NET10_0_OR_GREATER

		[Test]
		public async Task RefStructParameter_ShouldReturnSameMock()
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock(AutoMocking);

			IAutoMockChild first = sut.Consume(new Packet(1));
			IAutoMockChild second = sut.Consume(new Packet(2));

			await That(first).IsNotNull();
			await That(second).IsSameAs(first);
		}
#endif
	}
}
