using Mockolate.Exceptions;
using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public sealed class PropertySetupTests
{
	[Test]
	public async Task AutoInitializeWith_WhenAlreadyInitialized_ShouldNotOverwriteValue()
	{
		FakePropertySetup setup = new("p");
		IInteractivePropertySetup interactive = setup;

		interactive.InitializeWith(5);
		interactive.InitializeWith(10);

		int value = interactive.InvokeGetter(null, MockBehavior.Default, () => -1);
		await That(value).IsEqualTo(5);
	}

	[Test]
	public async Task DefaultInvokeGetter_WhenRequestedTypeDiffersFromBackingType_ShouldFallBackToGenerator()
	{
		PropertySetup.Default<int> setup = new("p", 0x40400000);
		IInteractivePropertySetup interactive = setup;

		float value = interactive.InvokeGetter(null, MockBehavior.Default, () => 99f);

		await That(value).IsEqualTo(99f)
			.Because(
				"the typeof-equality branch must fall through to the defaultValueGenerator; reinterpreting 0x40400000 via Unsafe.As<int, float> would yield 3.0f instead");
	}

	[Test]
	public async Task DefaultInvokeGetter_WhenRequestedTypeIsBoxedSuperType_ShouldReturnStoredValueViaPatternMatch()
	{
		PropertySetup.Default<int> setup = new("p", 42);
		IInteractivePropertySetup interactive = setup;

		object value = interactive.InvokeGetter<object>(null, MockBehavior.Default, () => 99);

		await That(value).IsEqualTo(42);
	}

	[Test]
	public async Task DefaultInvokeGetterFast_WhenRequestedTypeIsBoxedSuperType_ShouldReturnStoredValueViaPatternMatch()
	{
		PropertySetup.Default<int> setup = new("p", 42);

		object value = setup.InvokeGetterFast<object>(MockBehavior.Default, _ => 99);

		await That(value).IsEqualTo(42);
	}

	[Test]
	public async Task DefaultInvokeGetterFast_WhenStoredValueIsNullReference_ShouldReturnNullViaTypeofFastPath()
	{
		PropertySetup.Default<string> setup = new("p", null!);

		string value = setup.InvokeGetterFast<string>(MockBehavior.Default, _ => "fallback");

		await That(value).IsNull();
	}

	[Test]
	public async Task DefaultInvokeSetter_WhenValueIsNullAndUnderlyingTypeIsNullable_ShouldStoreDefault()
	{
		PropertySetup.Default<int?> setup = new("p", 5);
		IInteractivePropertySetup interactive = setup;

		interactive.InvokeSetter<object?>(null, null, MockBehavior.Default);

		int? value = interactive.InvokeGetter<int?>(null, MockBehavior.Default, () => 42);
		await That(value).IsNull();
	}

	[Test]
	public async Task DefaultInvokeSetter_WhenValueIsNullButUnderlyingTypeIsNonNullable_ShouldThrow()
	{
		PropertySetup.Default<int> setup = new("p", 5);
		IInteractivePropertySetup interactive = setup;

		void Act()
		{
			interactive.InvokeSetter<object?>(null, null, MockBehavior.Default);
		}

		await That(Act).Throws<MockException>()
			.WithMessage("*int*").AsWildcard();
	}

	[Test]
	public async Task DefaultInvokeSetter_WhenValueTypeMismatch_ShouldThrowWithFormattedMessage()
	{
		PropertySetup.Default<int> setup = new("p", 5);
		IInteractivePropertySetup interactive = setup;

		void Act()
		{
			interactive.InvokeSetter<string>(null, "string-value", MockBehavior.Default);
		}

		await That(Act).Throws<MockException>()
			.WithMessage("*'int'*'string'*").AsWildcard();
	}

	[Test]
	public async Task PropertySetupInvokeGetter_WhenRequestedTypeIsBoxedSuperType_ShouldReturnStoredValueViaPatternMatch()
	{
		PropertySetup<int> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "p");
		setup.InitializeWith(42);
		IInteractivePropertySetup interactive = setup;

		object value = interactive.InvokeGetter<object>(null, MockBehavior.Default, () => 99);

		await That(value).IsEqualTo(42);
	}

	[Test]
	public async Task PropertySetupInvokeGetter_WhenStoredValueIsNullReference_ShouldReturnNullViaTypeofFastPath()
	{
		PropertySetup<string> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "p");
		IInteractivePropertySetup interactive = setup;

		string value = interactive.InvokeGetter<string>(null, MockBehavior.Default, () => "fallback");

		await That(value).IsNull();
	}

	[Test]
	public async Task PropertySetupInvokeGetterFast_WhenRequestedTypeIsBoxedSuperType_ShouldReturnStoredValueViaPatternMatch()
	{
		PropertySetup<int> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)), "p");
		setup.InitializeWith(42);

		object value = setup.InvokeGetterFast<object>(MockBehavior.Default, _ => 99);

		await That(value).IsEqualTo(42);
	}

	[Test]
	public async Task UserInitializeWith_SecondCall_ShouldNotOverwriteValue()
	{
		FakePropertySetup setup = new("p");
		IPropertySetup<int> userFacing = setup;

		userFacing.InitializeWith(5);
		userFacing.InitializeWith(10);

		int value = ((IInteractivePropertySetup)setup).InvokeGetter(null, MockBehavior.Default, () => -1);
		await That(value).IsEqualTo(5);
	}
}
