using Mockolate.Parameters;

namespace Mockolate.Tests.MockMethods;

public sealed partial class InteractionsTests
{
	public sealed class ThrowingBaseTests
	{
		[Test]
		public async Task ReturnMethod_WhenBaseThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut.ReturnMethodWith0Parameters();
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.ReturnMethodWith0Parameters()).Once();
		}

		[Test]
		public async Task ReturnMethodWith0Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith0Parameters();
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith0Parameters()).Once();
		}

		[Test]
		public async Task ReturnMethodWith1Parameter_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.ReturnMethodWith1Parameter(It.IsAny<int>().Monitor(out IParameterMonitor<int> v1))
				.Returns(0);

			void Act()
			{
				sut.ReturnMethodWith1Parameter(1);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
		}

		[Test]
		public async Task ReturnMethodWith1Parameter_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith1Parameter(1);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith1Parameter(1)).Once();
		}

		[Test]
		public async Task ReturnMethodWith2Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.ReturnMethodWith2Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2))
				.Returns(0);

			void Act()
			{
				sut.ReturnMethodWith2Parameters(1, 2);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
		}

		[Test]
		public async Task ReturnMethodWith2Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith2Parameters(1, 2);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith2Parameters(1, 2)).Once();
		}

		[Test]
		public async Task ReturnMethodWith3Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.ReturnMethodWith3Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3))
				.Returns(0);

			void Act()
			{
				sut.ReturnMethodWith3Parameters(1, 2, 3);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
		}

		[Test]
		public async Task ReturnMethodWith3Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith3Parameters(1, 2, 3);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith3Parameters(1, 2, 3)).Once();
		}

		[Test]
		public async Task ReturnMethodWith4Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.ReturnMethodWith4Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4))
				.Returns(0);

			void Act()
			{
				sut.ReturnMethodWith4Parameters(1, 2, 3, 4);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
		}

		[Test]
		public async Task ReturnMethodWith4Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith4Parameters(1, 2, 3, 4);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith4Parameters(1, 2, 3, 4)).Once();
		}

		[Test]
		public async Task ReturnMethodWith5Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.ReturnMethodWith5Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v5))
				.Returns(0);

			void Act()
			{
				sut.ReturnMethodWith5Parameters(1, 2, 3, 4, 5);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
			await That(v5.Values).HasSingle().Which.IsEqualTo(5);
		}

		[Test]
		public async Task ReturnMethodWith5Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.ReturnMethodWith5Parameters(1, 2, 3, 4, 5);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.ReturnMethodWith5Parameters(1, 2, 3, 4, 5)).Once();
		}

		[Test]
		public async Task VoidMethod_WhenBaseThrows_AndSkipBaseClass_ShouldNotThrow_AndShouldRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock(MockBehavior.Default.SkippingBaseClass());

			void Act()
			{
				sut.VoidMethodWith0Parameters();
			}

			await That(Act).DoesNotThrow();
			await That(sut.Mock.Verify.VoidMethodWith0Parameters()).Once();
		}

		[Test]
		public async Task VoidMethodWith0Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith0Parameters();
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith0Parameters()).Once();
		}

		[Test]
		public async Task VoidMethodWith1Parameter_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.VoidMethodWith1Parameter(It.IsAny<int>().Monitor(out IParameterMonitor<int> v1))
				.DoesNotThrow();

			void Act()
			{
				sut.VoidMethodWith1Parameter(1);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
		}

		[Test]
		public async Task VoidMethodWith1Parameter_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith1Parameter(1);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith1Parameter(1)).Once();
		}

		[Test]
		public async Task VoidMethodWith2Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.VoidMethodWith2Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2))
				.DoesNotThrow();

			void Act()
			{
				sut.VoidMethodWith2Parameters(1, 2);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
		}

		[Test]
		public async Task VoidMethodWith2Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith2Parameters(1, 2);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith2Parameters(1, 2)).Once();
		}

		[Test]
		public async Task VoidMethodWith3Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.VoidMethodWith3Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3))
				.DoesNotThrow();

			void Act()
			{
				sut.VoidMethodWith3Parameters(1, 2, 3);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
		}

		[Test]
		public async Task VoidMethodWith3Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith3Parameters(1, 2, 3);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith3Parameters(1, 2, 3)).Once();
		}

		[Test]
		public async Task VoidMethodWith4Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.VoidMethodWith4Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4))
				.DoesNotThrow();

			void Act()
			{
				sut.VoidMethodWith4Parameters(1, 2, 3, 4);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
		}

		[Test]
		public async Task VoidMethodWith4Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith4Parameters(1, 2, 3, 4);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith4Parameters(1, 2, 3, 4)).Once();
		}

		[Test]
		public async Task VoidMethodWith5Parameters_WhenBaseThrows_ShouldRecordArgumentsPassedByCaller()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();
			sut.Mock.Setup.VoidMethodWith5Parameters(
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v1),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v2),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v3),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v4),
					It.IsAny<int>().Monitor(out IParameterMonitor<int> v5))
				.DoesNotThrow();

			void Act()
			{
				sut.VoidMethodWith5Parameters(1, 2, 3, 4, 5);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(v1.Values).HasSingle().Which.IsEqualTo(1);
			await That(v2.Values).HasSingle().Which.IsEqualTo(2);
			await That(v3.Values).HasSingle().Which.IsEqualTo(3);
			await That(v4.Values).HasSingle().Which.IsEqualTo(4);
			await That(v5.Values).HasSingle().Which.IsEqualTo(5);
		}

		[Test]
		public async Task VoidMethodWith5Parameters_WhenBaseThrows_ShouldStillRecordInvocation()
		{
			ThrowingBaseService sut = ThrowingBaseService.CreateMock();

			void Act()
			{
				sut.VoidMethodWith5Parameters(1, 2, 3, 4, 5);
			}

			await That(Act).Throws<InvalidOperationException>();
			await That(sut.Mock.Verify.VoidMethodWith5Parameters(1, 2, 3, 4, 5)).Once();
		}

		public class ThrowingBaseService
		{
			public virtual void VoidMethodWith0Parameters()
				=> throw new InvalidOperationException("base throws");

			public virtual void VoidMethodWith1Parameter(int p1)
				=> throw new InvalidOperationException("base throws");

			public virtual void VoidMethodWith2Parameters(int p1, int p2)
				=> throw new InvalidOperationException("base throws");

			public virtual void VoidMethodWith3Parameters(int p1, int p2, int p3)
				=> throw new InvalidOperationException("base throws");

			public virtual void VoidMethodWith4Parameters(int p1, int p2, int p3, int p4)
				=> throw new InvalidOperationException("base throws");

			public virtual void VoidMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5)
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith0Parameters()
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith1Parameter(int p1)
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith2Parameters(int p1, int p2)
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith3Parameters(int p1, int p2, int p3)
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith4Parameters(int p1, int p2, int p3, int p4)
				=> throw new InvalidOperationException("base throws");

			public virtual int ReturnMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5)
				=> throw new InvalidOperationException("base throws");
		}
	}
}
