using Mockolate.Parameters;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class IndexerInteractionsTests
{
	public sealed class WhenBaseThrowsTests
	{
		[Test]
		public async Task Getter_WithSkippingBaseClass_ShouldRecordTheAccessWithoutThrowing()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				_ = sut[1];
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify[1].Got()).Once();
		}

		[Test]
		public async Task Getter_With1Key_ShouldRecordTheKeyPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>().Monitor(out IParameterMonitor<int> v1)].Returns("foo");

			void Act()
			{
				_ = sut[1];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
		}

		[Test]
		public async Task Getter_With1Key_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				_ = sut[1];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1].Got()).Once();
		}

		[Test]
		public async Task Getter_With2Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2)]
				.Returns("foo");

			void Act()
			{
				_ = sut[1, 2];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
		}

		[Test]
		public async Task Getter_With2Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				_ = sut[1, 2];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2].Got()).Once();
		}

		[Test]
		public async Task Getter_With3Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3)]
				.Returns("foo");

			void Act()
			{
				_ = sut[1, 2, 3];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
		}

		[Test]
		public async Task Getter_With3Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				_ = sut[1, 2, 3];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3].Got()).Once();
		}

		[Test]
		public async Task Getter_With4Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4)]
				.Returns("foo");

			void Act()
			{
				_ = sut[1, 2, 3, 4];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
		}

		[Test]
		public async Task Getter_With4Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				_ = sut[1, 2, 3, 4];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3, 4].Got()).Once();
		}

		[Test]
		public async Task Getter_With5Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v5)]
				.Returns("foo");

			void Act()
			{
				_ = sut[1, 2, 3, 4, 5];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
			await That(v5.Values).HasSingle().Which.IsEqualTo(5);
		}

		[Test]
		public async Task Getter_With5Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				_ = sut[1, 2, 3, 4, 5];
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Got()).Once();
		}

		[Test]
		public async Task Setter_WithSkippingBaseClass_ShouldRecordTheAccessWithoutThrowing()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut[1] = "value";
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify[1].Set("value")).Once();
		}

		[Test]
		public async Task Setter_With1Key_ShouldRecordTheKeyPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>().Monitor(out IParameterMonitor<int> v1)].Returns("foo");

			void Act()
			{
				sut[1] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
		}

		[Test]
		public async Task Setter_With1Key_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				sut[1] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1].Set("value")).Once();
		}

		[Test]
		public async Task Setter_With2Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2)]
				.Returns("foo");

			void Act()
			{
				sut[1, 2] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
		}

		[Test]
		public async Task Setter_With2Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				sut[1, 2] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2].Set("value")).Once();
		}

		[Test]
		public async Task Setter_With3Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3)]
				.Returns("foo");

			void Act()
			{
				sut[1, 2, 3] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
		}

		[Test]
		public async Task Setter_With3Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				sut[1, 2, 3] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3].Set("value")).Once();
		}

		[Test]
		public async Task Setter_With4Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4)]
				.Returns("foo");

			void Act()
			{
				sut[1, 2, 3, 4] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
		}

		[Test]
		public async Task Setter_With4Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				sut[1, 2, 3, 4] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set("value")).Once();
		}

		[Test]
		public async Task Setter_With5Keys_ShouldRecordTheKeysPassedByTheCaller()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();
			sut.Mock.Setup[
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v5)]
				.Returns("foo");

			void Act()
			{
				sut[1, 2, 3, 4, 5] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
			await That(v5.Values).HasSingle().Which.IsEqualTo(5);
		}

		[Test]
		public async Task Setter_With5Keys_ShouldStillRecordTheAccess()
		{
			ThrowingBaseIndexerService sut = ThrowingBaseIndexerService.CreateMock();

			void Act()
			{
				sut[1, 2, 3, 4, 5] = "value";
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set("value")).Once();
		}

		public class ThrowingBaseIndexerService
		{
			public virtual string this[int p1]
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}

			public virtual string this[int p1, int p2]
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}

			public virtual string this[int p1, int p2, int p3]
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}

			public virtual string this[int p1, int p2, int p3, int p4]
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}

			public virtual string this[int p1, int p2, int p3, int p4, int p5]
			{
				get => throw new InvalidOperationException("base getter throws");
				set => throw new InvalidOperationException("base setter throws");
			}
		}
	}
}
