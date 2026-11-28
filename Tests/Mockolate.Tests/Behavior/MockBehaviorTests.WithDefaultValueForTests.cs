namespace Mockolate.Tests.Behavior;

public sealed partial class MockBehaviorTests
{
	public sealed class WithDefaultValueForTests
	{
		[Test]
		public async Task MultipleTypedFactories_ShouldSupportAll()
		{
			MockBehavior behavior = MockBehavior.Default
				.WithDefaultValueFor(() => new MySpecialValue(5))
				.WithDefaultValueFor(() => new MyInheritedSpecialValue(6, 7));

			IMyServiceForMySpecialValue mockWithBehavior
				= IMyServiceForMySpecialValue.CreateMock(behavior);

			await That(mockWithBehavior.Value.Value).IsEqualTo(5);
			await That(mockWithBehavior.InheritedValue)
				.Whose(x => x.Value, it => it.IsEqualTo(6)).And
				.Whose(x => x.OtherValue, it => it.IsEqualTo(7));
		}

		[Test]
		public async Task WithExplicitDefaultValueFactory_ShouldApplyThisFactory()
		{
			DefaultValueFactory defaultValueFactory = new(
				t => typeof(MySpecialValue).IsAssignableFrom(t),
				(_, _) => new MyInheritedSpecialValue(6, 7));
			MockBehavior behavior = MockBehavior.Default
				.WithDefaultValueFor(defaultValueFactory);

			IMyServiceForMySpecialValue mockWithBehavior
				= IMyServiceForMySpecialValue.CreateMock(behavior);

			await That(mockWithBehavior.Value.Value).IsEqualTo(6);
			await That(mockWithBehavior.InheritedValue)
				.Whose(x => x.Value, it => it.IsEqualTo(6)).And
				.Whose(x => x.OtherValue, it => it.IsEqualTo(7));
		}

		[Test]
		public async Task WithTypedFactory_ShouldRequireExactTypeMatch()
		{
			MockBehavior behavior1 = MockBehavior.Default
				.WithDefaultValueFor(() => new MySpecialValue(5));
			MockBehavior behavior2 = MockBehavior.Default
				.WithDefaultValueFor(() => new MyInheritedSpecialValue(6, 7));

			IMyServiceForMySpecialValue mockWithBehavior1
				= IMyServiceForMySpecialValue.CreateMock(behavior1);
			IMyServiceForMySpecialValue mockWithBehavior2
				= IMyServiceForMySpecialValue.CreateMock(behavior2);

			await That(mockWithBehavior1.InheritedValue).IsNull();
			await That(mockWithBehavior1.Value.Value).IsEqualTo(5);
			await That(mockWithBehavior2.InheritedValue)
				.Whose(x => x.Value, it => it.IsEqualTo(6)).And
				.Whose(x => x.OtherValue, it => it.IsEqualTo(7));
			await That(mockWithBehavior2.Value).IsNull();
		}

		[Test]
		public async Task WithTypedFactory_ShouldUseDefaultValueFactoryFromBehavior()
		{
			MockBehavior behavior = MockBehavior.Default
				.WithDefaultValueFor(() => new MySpecialValue(5));

			IMyServiceForMySpecialValue mockWithDefaultBehavior
				= IMyServiceForMySpecialValue.CreateMock();
			IMyServiceForMySpecialValue mockWithCustomBehavior
				= IMyServiceForMySpecialValue.CreateMock(behavior);

			await That(mockWithDefaultBehavior.Value).IsNull();
			await That(mockWithCustomBehavior.Value.Value).IsEqualTo(5);
		}

		[Test]
		public async Task MethodWithParameters_WithoutFactory_ShouldReturnDefaultValues()
		{
			IMyServiceWithParameters mock = IMyServiceWithParameters.CreateMock();

			await That(mock.GetNumber("foo", 1)).IsEqualTo(0);
			await That(mock.GetText("foo", 1)).IsEqualTo("");
			await That(mock.GetValue("foo", 1)).IsNull();
		}

		[Test]
		public async Task MethodWithParameters_WithTypedFactory_ShouldOnlyApplyToTheExactType()
		{
			MockBehavior behavior = MockBehavior.Default
				.WithDefaultValueFor(() => new MySpecialValue(5))
				.WithDefaultValueFor(() => 42);

			IMyServiceWithParameters mock = IMyServiceWithParameters.CreateMock(behavior);

			await That(mock.GetValue("foo", 1).Value).IsEqualTo(5);
			await That(mock.GetNumber("foo", 1)).IsEqualTo(42);
			await That(mock.GetText("foo", 1)).IsEqualTo("");
			await That(mock.GetFlag("foo", 1)).IsFalse();
		}

		[Test]
		public async Task MethodWithParameters_WithExplicitFactory_ShouldForwardTheParametersAndCheckThePredicateOnce()
		{
			int predicateCalls = 0;
			object?[]? receivedParameters = null;
			DefaultValueFactory defaultValueFactory = new(
				t =>
				{
					predicateCalls++;
					return t == typeof(int);
				},
				(_, parameters) =>
				{
					receivedParameters = parameters;
					return 42;
				});
			MockBehavior behavior = MockBehavior.Default.WithDefaultValueFor(defaultValueFactory);
			IMyServiceWithParameters mock = IMyServiceWithParameters.CreateMock(behavior);

			int result = mock.GetNumber("foo", 1);

			await That(result).IsEqualTo(42);
			await That(receivedParameters).IsEqualTo(["foo", 1,]);
			await That(predicateCalls).IsEqualTo(1)
				.Because("a user-defined predicate must not be evaluated more than once per call");
		}

		[Test]
		public async Task MethodWithParameters_WithCustomGenerator_ShouldForwardTheParameters()
		{
			MyDefaultValueGenerator generator = new();
			MockBehavior behavior = MockBehavior.Default.WithDefaultValueGenerator(generator);
			IMyServiceWithParameters mock = IMyServiceWithParameters.CreateMock(behavior);

			bool result = mock.GetFlag("foo", 1);

			await That(result).IsTrue();
			await That(generator.ReceivedParameters).IsEqualTo(["foo", 1,]);
		}

		[Test]
		public async Task MethodWithParameters_WithTypedFactoryAndCustomGenerator_ShouldFallBackToTheGenerator()
		{
			MyDefaultValueGenerator generator = new();
			MockBehavior behavior = MockBehavior.Default
				.WithDefaultValueFor(() => 42)
				.WithDefaultValueGenerator(generator);
			IMyServiceWithParameters mock = IMyServiceWithParameters.CreateMock(behavior);

			await That(mock.GetNumber("foo", 1)).IsEqualTo(42);
			await That(mock.GetFlag("foo", 1)).IsTrue();
		}

		private sealed class MyDefaultValueGenerator : IDefaultValueGenerator
		{
			public object?[]? ReceivedParameters { get; private set; }

			public object? GenerateValue(Type type, params object?[] parameters)
			{
				ReceivedParameters = parameters;
				return type == typeof(bool) ? true : null;
			}
		}

		internal interface IMyServiceWithParameters
		{
			int GetNumber(string text, int number);
			string GetText(string text, int number);
			bool GetFlag(string text, int number);
			MySpecialValue GetValue(string text, int number);
		}

		internal class MySpecialValue(int value)
		{
			public int Value { get; } = value;
		}

		internal class MyInheritedSpecialValue(int value, int otherValue) : MySpecialValue(value)
		{
			public int OtherValue { get; } = otherValue;
		}

		internal interface IMyServiceForMySpecialValue
		{
			MySpecialValue Value { get; }
			MyInheritedSpecialValue InheritedValue { get; }
		}
	}
}
