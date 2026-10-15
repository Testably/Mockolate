namespace Mockolate.Tests.MockMethods;

public sealed partial class SetupMethodTests
{
	public sealed class SkippingBaseClassTests
	{
		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWith1Parameter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWith1Parameter(It.IsAny<int>()).SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWith1Parameter(1);

			await That(sut.MyReturnMethodWith1ParameterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWith2Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWith2Parameters(It.IsAny<int>(), It.IsAny<int>()).SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWith2Parameters(1, 2);

			await That(sut.MyReturnMethodWith2ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWith3Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWith3Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWith3Parameters(1, 2, 3);

			await That(sut.MyReturnMethodWith3ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWith4Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWith4Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWith4Parameters(1, 2, 3, 4);

			await That(sut.MyReturnMethodWith4ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWith5Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWith5Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWith5Parameters(1, 2, 3, 4, 5);

			await That(sut.MyReturnMethodWith5ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyReturnMethodWithoutParameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyReturnMethodWithoutParameters().SkippingBaseClass(skipBaseClass);

			sut.MyReturnMethodWithoutParameters();

			await That(sut.MyReturnMethodWithoutParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWith1Parameter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWith1Parameter(It.IsAny<int>()).SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWith1Parameter(1);

			await That(sut.MyVoidMethodWith1ParameterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWith2Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWith2Parameters(It.IsAny<int>(), It.IsAny<int>()).SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWith2Parameters(1, 2);

			await That(sut.MyVoidMethodWith2ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWith3Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWith3Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWith3Parameters(1, 2, 3);

			await That(sut.MyVoidMethodWith3ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWith4Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWith4Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWith4Parameters(1, 2, 3, 4);

			await That(sut.MyVoidMethodWith4ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWith5Parameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWith5Parameters(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWith5Parameters(1, 2, 3, 4, 5);

			await That(sut.MyVoidMethodWith5ParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task MyVoidMethodWithoutParameters_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyVoidMethodWithoutParameters().SkippingBaseClass(skipBaseClass);

			sut.MyVoidMethodWithoutParameters();

			await That(sut.MyVoidMethodWithoutParametersCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		public async Task SetupSkippingBaseClassWithoutParameter_ShouldReturnDefaultValue()
		{
			MyMethodService sut = MyMethodService.CreateMock();
			sut.Mock.Setup.MyMethodReturning2().SkippingBaseClass();

			int result = sut.MyMethodReturning2();

			await That(result).IsEqualTo(0);
		}

		public class MyMethodService
		{
			public int MyVoidMethodWithoutParametersCallCount { get; private set; }
			public int MyVoidMethodWith1ParameterCallCount { get; private set; }
			public int MyVoidMethodWith2ParametersCallCount { get; private set; }
			public int MyVoidMethodWith3ParametersCallCount { get; private set; }
			public int MyVoidMethodWith4ParametersCallCount { get; private set; }
			public int MyVoidMethodWith5ParametersCallCount { get; private set; }
			public int MyReturnMethodWithoutParametersCallCount { get; private set; }
			public int MyReturnMethodWith1ParameterCallCount { get; private set; }
			public int MyReturnMethodWith2ParametersCallCount { get; private set; }
			public int MyReturnMethodWith3ParametersCallCount { get; private set; }
			public int MyReturnMethodWith4ParametersCallCount { get; private set; }
			public int MyReturnMethodWith5ParametersCallCount { get; private set; }

			public virtual void MyVoidMethodWithoutParameters()
				=> MyVoidMethodWithoutParametersCallCount++;

			public virtual void MyVoidMethodWith1Parameter(int p1)
				=> MyVoidMethodWith1ParameterCallCount++;

			public virtual void MyVoidMethodWith2Parameters(int p1, int p2)
				=> MyVoidMethodWith2ParametersCallCount++;

			public virtual void MyVoidMethodWith3Parameters(int p1, int p2, int p3)
				=> MyVoidMethodWith3ParametersCallCount++;

			public virtual void MyVoidMethodWith4Parameters(int p1, int p2, int p3, int p4)
				=> MyVoidMethodWith4ParametersCallCount++;

			public virtual void MyVoidMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5)
				=> MyVoidMethodWith5ParametersCallCount++;

			public virtual int MyReturnMethodWithoutParameters()
				=> MyReturnMethodWithoutParametersCallCount++;

			public virtual int MyReturnMethodWith1Parameter(int p1)
				=> MyReturnMethodWith1ParameterCallCount++;

			public virtual int MyReturnMethodWith2Parameters(int p1, int p2)
				=> MyReturnMethodWith2ParametersCallCount++;

			public virtual int MyReturnMethodWith3Parameters(int p1, int p2, int p3)
				=> MyReturnMethodWith3ParametersCallCount++;

			public virtual int MyReturnMethodWith4Parameters(int p1, int p2, int p3, int p4)
				=> MyReturnMethodWith4ParametersCallCount++;

			public virtual int MyReturnMethodWith5Parameters(int p1, int p2, int p3, int p4, int p5)
				=> MyReturnMethodWith5ParametersCallCount++;

			public virtual int MyMethodReturning2() => 2;
		}
	}
}
