namespace Mockolate.Tests;

public sealed partial class MockBehaviorTests
{
	public sealed class UseConstructorParametersForTests
	{
		[Test]
		public async Task TryGetConstructorParameters_Predicate_IsEvaluatedOnEachCall()
		{
			int invocationCount = 0;
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(() =>
				{
					invocationCount++;
					return [invocationCount,];
				});
			object?[] expectedFirst = [1,];
			object?[] expectedSecond = [2,];

			sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? first);
			sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? second);

			await That(first).IsEqualTo(expectedFirst);
			await That(second).IsEqualTo(expectedSecond);
		}

		[Test]
		public async Task TryGetConstructorParameters_WhenNoneRegistered_ShouldReturnFalse()
		{
			IMockBehaviorAccess sut = MockBehavior.Default;

			bool result = sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? parameters);

			await That(result).IsFalse();
			await That(parameters).IsNull();
		}

		[Test]
		public async Task TryGetConstructorParameters_WhenRegisteredForBaseType_ShouldNotMatchDerivedType()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyBaseService>(1);

			bool result = sut.TryGetConstructorParameters<MyDerivedService>(out object?[]? parameters);

			await That(result).IsFalse();
			await That(parameters).IsNull();
		}

		[Test]
		public async Task TryGetConstructorParameters_WhenRegisteredForDerivedType_ShouldNotMatchBaseType()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyDerivedService>(1);

			bool result = sut.TryGetConstructorParameters<MyBaseService>(out object?[]? parameters);

			await That(result).IsFalse();
			await That(parameters).IsNull();
		}

		[Test]
		public async Task TryGetConstructorParameters_WhenRegisteredForDifferentType_ShouldReturnFalse()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(5);

			bool result = sut.TryGetConstructorParameters<MyOtherService>(out object?[]? parameters);

			await That(result).IsFalse();
			await That(parameters).IsNull();
		}

		[Test]
		public async Task TryGetConstructorParameters_WithMultipleRegistrationsForSameType_Predicate_ShouldReturnLatest()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(() => [1,])
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(() => [2,]);
			object?[] expected = [2,];

			bool result = sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? parameters);

			await That(result).IsTrue();
			await That(parameters).IsEqualTo(expected);
		}

		[Test]
		public async Task TryGetConstructorParameters_WithMultipleRegistrationsForSameType_ShouldReturnLatest()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(1)
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(2);
			object?[] expected = [2,];

			bool result = sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? parameters);

			await That(result).IsTrue();
			await That(parameters).IsEqualTo(expected);
		}

		[Test]
		public async Task TryGetConstructorParameters_WithMultipleRegistrationsForSameType_UsedByMock_ShouldUseLatest()
		{
			MockBehavior behavior = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(1)
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(2);

			MyServiceWithMultipleConstructors mock = MyServiceWithMultipleConstructors.CreateMock(behavior);

			await That(mock.Value).IsEqualTo(2);
		}

		[Test]
		public async Task TryGetConstructorParameters_WithRegistrationsForDifferentTypes_ShouldReturnEachCorrectly()
		{
			IMockBehaviorAccess sut = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(1)
				.UseConstructorParametersFor<MyOtherService>("foo");
			object?[] expectedForService = [1,];
			object?[] expectedForOther = ["foo",];

			bool resultForService = sut.TryGetConstructorParameters<MyServiceWithMultipleConstructors>(out object?[]? serviceParameters);
			bool resultForOther = sut.TryGetConstructorParameters<MyOtherService>(out object?[]? otherParameters);

			await That(resultForService).IsTrue();
			await That(serviceParameters).IsEqualTo(expectedForService);
			await That(resultForOther).IsTrue();
			await That(otherParameters).IsEqualTo(expectedForOther);
		}

		[Test]
		public async Task WithExplicitConstructorParameters_ShouldIgnoreConstructorParametersFromBehavior()
		{
			MockBehavior behavior = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(() => [5,]);

			MyServiceWithMultipleConstructors mock = MyServiceWithMultipleConstructors.CreateMock(behavior, [7,]);

			int value = mock.Value;

			await That(value).IsEqualTo(7);
		}

		[Test]
		public async Task
			WithExplicitParameters_ShouldUseConstructorParametersFromBehavior()
		{
			MockBehavior behavior = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(5);

			MyServiceWithMultipleConstructors mockWithDefaultBehavior
				= MyServiceWithMultipleConstructors.CreateMock();
			MyServiceWithMultipleConstructors mockWithCustomBehavior
				= MyServiceWithMultipleConstructors.CreateMock(behavior);

			int valueWithDefaultBehavior = mockWithDefaultBehavior.Value;
			int valueWithCustomBehavior = mockWithCustomBehavior.Value;

			await That(valueWithDefaultBehavior).IsEqualTo(0);
			await That(valueWithCustomBehavior).IsEqualTo(5);
		}

		[Test]
		public async Task WithPredicate_ShouldUseConstructorParametersFromBehavior()
		{
			MockBehavior behavior = MockBehavior.Default
				.UseConstructorParametersFor<MyServiceWithMultipleConstructors>(() => [5,]);

			MyServiceWithMultipleConstructors mockWithDefaultBehavior
				= MyServiceWithMultipleConstructors.CreateMock();
			MyServiceWithMultipleConstructors mockWithCustomBehavior
				= MyServiceWithMultipleConstructors.CreateMock(behavior);

			int valueWithDefaultBehavior = mockWithDefaultBehavior.Value;
			int valueWithCustomBehavior = mockWithCustomBehavior.Value;

			await That(valueWithDefaultBehavior).IsEqualTo(0);
			await That(valueWithCustomBehavior).IsEqualTo(5);
		}

		internal class MyOtherService;

		internal class MyBaseService;

		internal class MyDerivedService : MyBaseService;

		internal class MyServiceWithMultipleConstructors
		{
			public MyServiceWithMultipleConstructors()
			{
				Value = 0;
			}

			public MyServiceWithMultipleConstructors(int value)
			{
				Value = value;
			}

			public int Value { get; }
		}
	}
}
