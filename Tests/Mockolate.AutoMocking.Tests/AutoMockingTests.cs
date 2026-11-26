using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Monitor;
using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.AutoMocking.Tests;

public sealed class AutoMockingTests
{
	[Test]
	public async Task Behavior_ShouldStillEqualTheBehaviorPassedToCreateMock()
	{
		MockBehavior expected = new(new NullDefaultValueGenerator());
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(expected);

		MockBehavior behavior = ((IMock)sut).MockRegistry.Behavior;

		await That(behavior).IsEqualTo(expected)
			.Because("the auto-mock factory that CreateMock adds is not part of the equality");
	}

	[Test]
	public async Task Behavior_DerivedFromTheDefault_ShouldBeTheSameInstance()
	{
		MockBehavior expected = MockBehavior.Default.ThrowingWhenNotSetup(false);
		IAutoMockRoot defaultMock = IAutoMockRoot.CreateMock();
		IAutoMockRoot derivedMock = IAutoMockRoot.CreateMock(expected);

		await That(((IMock)defaultMock).MockRegistry.Behavior).IsSameAs(MockBehavior.Default)
			.Because("the default behavior already carries the auto-mock factory");
		await That(((IMock)derivedMock).MockRegistry.Behavior).IsSameAs(expected);
	}

	[Test]
	public async Task ClassMock_ShouldPreferTheBaseClassImplementation()
	{
		AutoMockBase sut = AutoMockBase.CreateMock();

		IAutoMockChild fromBase = sut.GetVirtual();
		IAutoMockChild fromAbstract = sut.GetAbstract();

		await That(fromBase).IsNull().Because("the base class implementation provides the value");
		await That(fromAbstract).IsNotNull();
	}

	[Test]
	public async Task ClassMock_WhenSkippingBaseClass_ShouldReturnMock()
	{
		AutoMockBase sut = AutoMockBase.CreateMock(MockBehavior.Default.SkippingBaseClass());

		IAutoMockChild result = sut.GetVirtual();

		await That(result).IsNotNull();
	}

	[Test]
	public async Task ConcurrentFirstAccess_ShouldReturnTheSameMock()
	{
		const int threads = 8;
		for (int iteration = 0; iteration < 50; iteration++)
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock();
			(IAutoMockChild Property, IAutoMockChild Indexer, IAutoMockChild Method)[] results =
				await RunConcurrently(threads, () => (sut.Child, sut[1], sut.GetChild(1)));

			await That(results.All(r => ReferenceEquals(r.Property, results[0].Property) &&
			                            ReferenceEquals(r.Indexer, results[0].Indexer) &&
			                            ReferenceEquals(r.Method, results[0].Method))).IsTrue()
				.Because($"concurrent first reads must not create different auto-mocks (iteration {iteration})");
		}
	}

	[Test]
	public async Task ConcurrentFirstAccess_WithUninitializedPropertySetup_ShouldReturnTheSameMock()
	{
		const int threads = 8;
		for (int iteration = 0; iteration < 50; iteration++)
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock();
			sut.Mock.Setup.Child.OnGet.Do(() => { });
			IAutoMockChild[] results = await RunConcurrently(threads, () => sut.Child);

			await That(results.All(r => r is not null && ReferenceEquals(r, results[0]))).IsTrue()
				.Because($"the first default value of a set-up property must win (iteration {iteration})");
		}
	}

	[Test]
	public async Task ConcurrentFirstAccess_WithIndexerSetup_ShouldReturnTheSameMock()
	{
		const int threads = 8;
		for (int iteration = 0; iteration < 50; iteration++)
		{
			IAutoMockRoot sut = IAutoMockRoot.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].OnGet.Do(() => { });
			IAutoMockChild[] results = await RunConcurrently(threads, () => sut[1]);

			await That(results.All(r => r is not null && ReferenceEquals(r, results[0]))).IsTrue()
				.Because($"the first auto-mock of a set-up indexer key must win (iteration {iteration})");
		}
	}

	[Test]
	public async Task CustomBehaviorWithoutDefault_ShouldReturnMock()
	{
		MockBehavior behavior = new MockBehavior(new NullDefaultValueGenerator());
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(behavior);

		IAutoMockChild result = sut.GetChild(1);

		await That(result).IsNotNull()
			.Because("the factory comes with CreateMock and does not depend on MockBehavior.Default");
	}

	[Test]
	public async Task Delegate_ShouldReturnSameMock()
	{
		AutoMockChildFactory sut = AutoMockChildFactory.CreateMock();

		IAutoMockChild first = sut(1);
		IAutoMockChild second = sut(2);

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}

	[Test]
	public async Task ExplicitSetupReturningNull_ShouldReturnNull()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();
		sut.Mock.Setup.GetChild(It.IsAny<int>()).Returns((IAutoMockChild)null!);

		IAutoMockChild result = sut.GetChild(1);

		await That(result).IsNull().Because("a setup takes precedence over the auto-mock");
	}

	[Test]
	public async Task FrameworkInterface_ShouldNotBeAutoMocked()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IServiceProvider result = sut.GetServiceProvider();

		await That(result).IsNull();
	}

	[Test]
	public async Task AsyncEnumerableOfInterface_ShouldReturnEmptySequence()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();
		List<IAutoMockChild> result = [];

		await foreach (IAutoMockChild child in sut.GetChildren())
		{
			result.Add(child);
		}

		await That(result).IsEmpty().Because("no auto-mocks are created for the elements");
	}

	[Test]
	public async Task FileSystemInterface_ShouldBeAutoMocked()
	{
		IFileSystem sut = IFileSystem.CreateMock();

		IFile result = sut.File;
		result.Mock.Setup.Exists(It.IsAny<string?>()).Returns(true);

		await That(sut.File.Exists("foo.txt")).IsTrue()
			.Because("System.IO.Abstractions is included in the auto-mocked namespaces by default");
	}

	[Test]
	public async Task GenericInterface_ShouldReturnMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockGeneric<int> result = sut.GetGeneric();
		result.Mock.Setup.Value.InitializeWith(42);

		await That(sut.GetGeneric().Value).IsEqualTo(42);
	}

	[Test]
	public async Task HttpClient_WithHandlerOfTheSameBehavior_ShouldBeCreated()
	{
		HttpMessageHandler handler = HttpMessageHandler.CreateMock();

		HttpClient sut = HttpClient.CreateMock(MockBehavior.Default, [handler,]);

		await That(sut).IsNotNull();
	}

	[Test]
	public async Task Implementing_ShouldReturnMock()
	{
		IAutoMockChild sut = IAutoMockChild.CreateMock().Implementing<IAutoMockGeneric<int>>();

		IAutoMockRoot result = sut.GetRoot();

		await That(result).IsNotNull();
	}

	[Test]
	public async Task Indexer_ShouldReturnSameMockPerKey()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

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
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();
		IAutoMockChild value = IAutoMockChild.CreateMock();

		sut[1] = value;

		await That(sut[1]).IsSameAs(value);
	}

	[Test]
	public async Task IndexerWithFiveKeys_ShouldReturnSameMockPerKey()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = sut[1, 2, 3, 4, 5];
		IAutoMockChild second = sut[1, 2, 3, 4, 5];

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}

	[Test]
	public async Task IndexerOfTask_ShouldReturnCompletedTaskWithoutAutoMock()
	{
		ITaskIndexer sut = ITaskIndexer.CreateMock();

		Task<IAutoMockChild> result = sut[1];

		await That(result.IsCompleted).IsTrue();
		await That(await result).IsNull().Because("auto-mocking skips indexers that return a task");
	}

	[Test]
	public async Task Initialize_ShouldApplyToAutoMocks()
	{
		MockBehavior behavior = MockBehavior.Default.Initialize<IAutoMockChild>(setup => setup.GetValue().Returns(5));
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(behavior);

		int result = sut.GetChild(1).GetValue();

		await That(result).IsEqualTo(5);
	}

	[Test]
	public async Task Method_ShouldBeCachedPerMemberAndNotPerArgument()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = sut.GetChild(1);
		IAutoMockChild second = sut.GetChild(2);
		IAutoMockChild viaProperty = sut.Child;

		await That(second).IsSameAs(first);
		await That(viaProperty).IsNotSameAs(first);
	}

	[Test]
	public async Task Method_ShouldRecordInteractionsOnTheAutoMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		sut.GetChild(1).GetValue();

		await That(sut.GetChild(1).Mock.Verify.GetValue()).Once();
	}

	[Test]
	public async Task Method_ShouldReturnSameConfigurableMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		sut.GetChild(1).Mock.Setup.GetValue().Returns(42);

		await That(sut.GetChild(1).GetValue()).IsEqualTo(42);
	}

	[Test]
	public async Task Monitor_ShouldReturnTheSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();
		IAutoMockChild outside = sut.GetChild(1);
		MockMonitor<Mock.IMockVerifyForIAutoMockRoot> monitor = sut.Mock.Monitor();

		IAutoMockChild inside;
		using (monitor.Run())
		{
			inside = sut.GetChild(1);
		}

		await That(inside).IsSameAs(outside);
		await That(monitor.Verify.GetChild(It.IsAny<int>())).Once();
	}

	[Test]
	public async Task NullableProperty_ShouldReturnMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild? result = sut.NullableChild;

		await That(result).IsNotNull();
	}

	[Test]
	public async Task Property_ShouldReturnSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = sut.Child;
		IAutoMockChild second = sut.Child;

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}

	[Test]
	public async Task Scenario_ShouldReturnTheSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();
		IAutoMockChild before = sut.GetChild(1);

		sut.Mock.TransitionTo("other");
		IAutoMockChild after = sut.GetChild(1);

		await That(after).IsSameAs(before).Because("the auto-mock belongs to the member, not to a scenario");
	}

	[Test]
	public async Task ShouldBeTransitiveAndUseTheSameBehavior()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockRoot root = sut.Child.GetRoot();

		await That(root).IsNotNull().Because("the auto-mock uses the same behavior, so it auto-mocks as well");
		await That(root).IsNotSameAs(sut);
		await That(root.Child).IsNotNull();
	}

	[Test]
	public async Task Task_ShouldReturnSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = await sut.GetChildAsync();
		IAutoMockChild second = await sut.GetChildAsync();

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}

	[Test]
	public async Task Tuple_ConcurrentFirstAccess_ShouldReturnTheSameMocks()
	{
		const int threads = 8;
		for (int iteration = 0; iteration < 50; iteration++)
		{
			IAutoMockTuples sut = IAutoMockTuples.CreateMock();
			(IAutoMockTupleElement Property, IAutoMockTupleElement Indexer, IAutoMockTupleElement ReferenceIndexer,
				IAutoMockTupleElement Method)[] results = await RunConcurrently(threads,
				() => (sut.Pair.Element, sut[1].Element, sut["a"].Item2, sut.GetReferenceTuple().Item2));

			await That(results.All(r => ReferenceEquals(r.Property, results[0].Property) &&
			                            ReferenceEquals(r.Indexer, results[0].Indexer) &&
			                            ReferenceEquals(r.ReferenceIndexer, results[0].ReferenceIndexer) &&
			                            ReferenceEquals(r.Method, results[0].Method))).IsTrue()
				.Because($"concurrent first reads must not create different auto-mocks (iteration {iteration})");
		}
	}

	[Test]
	public async Task Tuple_Indexer_ShouldReturnSameMockPerKey()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		IAutoMockTupleElement first = sut[1].Element;
		IAutoMockTupleElement second = sut[1].Element;
		IAutoMockTupleElement otherKey = sut[2].Element;

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
		await That(otherKey).IsNotSameAs(first).Because("an indexer stores its value per key");
	}

	[Test]
	public async Task Tuple_LongTuples_ShouldReturnMockForTheEighthElement()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		IAutoMockTupleElement valueTupleElement = sut.GetLongValueTuple().Eighth;
		IAutoMockTupleElement referenceTupleElement = sut.GetLongReferenceTuple().Rest.Item1;

		await That(valueTupleElement).IsNotNull();
		await That(referenceTupleElement).IsNotNull();
	}

	[Test]
	public async Task Tuple_Method_ShouldReturnSameConfigurableMock()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		(await sut.GetPairAsync()).Element.Mock.Setup.GetValue().Returns(3);
		(int stock, IAutoMockTupleElement element) = await sut.GetPairAsync();

		await That(stock).IsEqualTo(0);
		await That(element.GetValue()).IsEqualTo(3);
	}

	[Test]
	public async Task Tuple_Nested_ShouldReturnDifferentMocks()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		((int _, IAutoMockTupleElement inner), IAutoMockTupleElement outer) = sut.GetNested();

		await That(inner).IsNotNull();
		await That(outer).IsNotNull();
		await That(outer).IsNotSameAs(inner);
		await That(sut.GetNested().Inner.Element).IsSameAs(inner);
		await That(sut.GetNested().Outer).IsSameAs(outer);
	}

	[Test]
	public async Task Tuple_Property_ShouldReturnSameMock()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		IAutoMockTupleElement first = sut.Pair.Element;
		IAutoMockTupleElement second = sut.Pair.Element;

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}

	[Test]
	public async Task Tuple_ReferenceTuple_ShouldReturnSameMock()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		Tuple<int, IAutoMockTupleElement> first = sut.GetReferenceTuple();
		Tuple<int, IAutoMockTupleElement> second = sut.GetReferenceTuple();
		IAutoMockTupleElement fromIndexer = sut["a"].Item2;

		await That(first.Item2).IsNotNull();
		await That(second.Item2).IsSameAs(first.Item2);
		await That(fromIndexer).IsNotNull();
		await That(sut["a"].Item2).IsSameAs(fromIndexer);
	}

	[Test]
	public async Task Tuple_TwoElementsOfTheSameType_ShouldReturnDifferentMocks()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();

		(IAutoMockTupleElement first, IAutoMockTupleElement second) = sut.GetTwo();

		await That(first).IsNotNull();
		await That(second).IsNotNull();
		await That(second).IsNotSameAs(first).Because("each element has its own auto-mock");
		await That(sut.GetTwo().Second).IsSameAs(second);
	}

	[Test]
	public async Task Tuple_WhenSetup_ShouldReturnTheSetupValue()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock();
		sut.Mock.Setup.GetReferenceTuple().Returns(Tuple.Create(1, (IAutoMockTupleElement)null!));

		Tuple<int, IAutoMockTupleElement> result = sut.GetReferenceTuple();

		await That(result.Item1).IsEqualTo(1);
		await That(result.Item2).IsNull();
	}

	[Test]
	public async Task Tuple_WhenSkippingAutoMocking_ShouldReturnNullElements()
	{
		IAutoMockTuples sut = IAutoMockTuples.CreateMock(MockBehavior.Default.SkippingAutoMocking());

		await That(sut.GetTwo().First).IsNull();
		await That(sut.Pair.Element).IsNull();
		await That(sut[1].Element).IsNull();
		await That(sut.GetReferenceTuple().Item2).IsNull();
	}

	[Test]
	public async Task Tuple_WithDefaultValueFor_ShouldPreferTheFactory()
	{
		IAutoMockTupleElement expected = IAutoMockTupleElement.CreateMock();
		IAutoMockTuples sut = IAutoMockTuples.CreateMock(MockBehavior.Default.WithDefaultValueFor(() => expected));

		await That(sut.GetTwo().First).IsSameAs(expected);
		await That(sut.Pair.Element).IsSameAs(expected);
		await That(sut[1].Element).IsSameAs(expected);
		await That(sut.GetReferenceTuple().Item2).IsSameAs(expected);
	}

	[Test]
	public async Task Tuple_WithDefaultValueForTheTuple_ShouldPreferTheFactory()
	{
		IAutoMockTupleElement expected = IAutoMockTupleElement.CreateMock();
		IAutoMockTuples sut = IAutoMockTuples.CreateMock(MockBehavior.Default
			.WithDefaultValueFor<(IAutoMockTupleElement, IAutoMockTupleElement)>(() => (expected, expected))
			.WithDefaultValueFor<(int, IAutoMockTupleElement)>(() => (3, expected)));

		await That(sut.GetTwo().Second).IsSameAs(expected);
		await That(sut.Pair.Element).IsSameAs(expected);
		await That(sut[1].Stock).IsEqualTo(3);
		await That((await sut.GetPairAsync()).Element).IsSameAs(expected);
	}

	[Test]
	public async Task ThrowingWhenNotSetup_ShouldStillThrow()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());

		void Act()
			=> sut.GetChild(1);

		await That(Act).Throws<MockNotSetupException>();
	}

	[Test]
	public async Task WithDefaultValueFor_ShouldPreferTheFactory()
	{
		IAutoMockChild expected = IAutoMockChild.CreateMock();
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(MockBehavior.Default.WithDefaultValueFor(() => expected));

		IAutoMockChild result = sut.GetChild(1);

		await That(result).IsSameAs(expected);
	}

	[Test]
	public async Task SkippingAutoMocking_ShouldReturnNull()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(MockBehavior.Default.SkippingAutoMocking());

		await That(sut.GetChild(1)).IsNull();
		await That(sut.Child).IsNull();
		await That(sut[1]).IsNull();
	}

	[Test]
	public async Task Wrapping_WhenSkippingBaseClass_ShouldCreateThePropertyAutoMockOnce()
	{
		int created = 0;
		MockBehavior behavior = MockBehavior.Default.SkippingBaseClass()
			.Initialize<IAutoMockChild>(_ => created++);
		IAutoMockRoot sut = IAutoMockRoot.CreateMock(behavior).Wrapping(new RootWithoutChild());

		IAutoMockChild first = sut.Child;
		_ = sut.Child;
		IAutoMockChild last = sut.Child;

		await That(last).IsSameAs(first);
		await That(created).IsEqualTo(1)
			.Because("every read of a property with a base value must not create a new auto-mock");
	}

	[Test]
	public async Task Wrapping_ShouldPreferTheWrappedInstance()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock().Wrapping(new RootWithoutChild());

		IAutoMockChild result = sut.GetChild(1);

		await That(result).IsNull().Because("the wrapped instance provides the value");
	}

#if NET8_0_OR_GREATER
	[Test]
	public async Task ValueTask_ShouldReturnSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = await sut.GetChildValueTaskAsync();
		IAutoMockChild second = await sut.GetChildValueTaskAsync();

		await That(first).IsNotNull()
			.Because("the default ValueTask<T> only wraps the default value of T on .NET 8 or later");
		await That(second).IsSameAs(first);
	}
#endif

#if NET10_0_OR_GREATER
	[Test]
	public async Task RefStructParameter_ShouldReturnSameMock()
	{
		IAutoMockRoot sut = IAutoMockRoot.CreateMock();

		IAutoMockChild first = sut.Consume(new Packet(1));
		IAutoMockChild second = sut.Consume(new Packet(2));

		await That(first).IsNotNull();
		await That(second).IsSameAs(first);
	}
#endif

	private static async Task<T[]> RunConcurrently<T>(int threads, Func<T> read)
	{
		using Barrier barrier = new(threads);
		Task<T>[] tasks = new Task<T>[threads];
		for (int i = 0; i < threads; i++)
		{
			tasks[i] = Task.Run(() =>
			{
				barrier.SignalAndWait();
				return read();
			});
		}

		return await Task.WhenAll(tasks);
	}

	public abstract class AutoMockBase
	{
		public abstract IAutoMockChild GetAbstract();
		public virtual IAutoMockChild GetVirtual() => null!;
	}

	public interface ITaskIndexer
	{
		Task<IAutoMockChild> this[int key] { get; }
	}

	private sealed class NullDefaultValueGenerator : IDefaultValueGenerator
	{
		public object? GenerateValue(Type type, params object?[] parameters) => null;
	}

	private sealed class RootWithoutChild : IAutoMockRoot
	{
		public IAutoMockChild Child => null!;
		public IAutoMockChild? NullableChild => null;

		public IAutoMockChild this[int key]
		{
			get => null!;
			set { }
		}

		public IAutoMockChild this[int a, int b, int c, int d, int e] => null!;
		public IAutoMockChild GetChild(int id) => null!;
		public Task<IAutoMockChild> GetChildAsync() => Task.FromResult<IAutoMockChild>(null!);
		public ValueTask<IAutoMockChild> GetChildValueTaskAsync() => new((IAutoMockChild)null!);
		public IAutoMockGeneric<int> GetGeneric() => null!;
		public IServiceProvider GetServiceProvider() => null!;
		public IAsyncEnumerable<IAutoMockChild> GetChildren() => null!;
#if NET10_0_OR_GREATER
		public IAutoMockChild Consume(Packet packet) => null!;
#endif
	}
}
