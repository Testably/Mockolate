namespace Mockolate.Tests.MockProperties;

public sealed partial class InteractionsTests
{
	public sealed class ThrowingBaseTests
	{
		[Test]
		public async Task VirtualPropertyGetter_WhenBaseThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordAccess()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				_ = sut.Value;
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.Value.Got()).Once();
		}

		[Test]
		public async Task VirtualPropertyGetter_WhenBaseThrows_ShouldStillExecuteOnGetCallback()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			int callCount = 0;
			sut.Mock.Setup.Value.OnGet.Do(() => callCount++);

			void Act()
			{
				_ = sut.Value;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task VirtualPropertyGetter_WhenBaseThrows_ShouldStillRecordAccess()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				_ = sut.Value;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.Value.Got()).Once();
		}

		[Test]
		public async Task VirtualPropertySetter_WhenBaseThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordAccess()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut.Value = 42;
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.Value.Set(It.Is(42))).Once();
		}

		[Test]
		public async Task VirtualPropertySetter_WhenBaseThrows_ShouldStillExecuteOnSetCallback()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			int receivedValue = 0;
			sut.Mock.Setup.Value.OnSet.Do(v => receivedValue = v);

			void Act()
			{
				sut.Value = 42;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(receivedValue).IsEqualTo(42);
		}

		[Test]
		public async Task VirtualPropertySetter_WhenBaseThrows_ShouldStillRecordAccess()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.Value = 42;
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.Value.Set(It.Is(42))).Once();
		}

		public class ThrowingBaseService
		{
			public virtual int Value
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}
		}
	}
}
