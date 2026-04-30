namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class SkippingBaseClassTests
	{
		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith1Key_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			_ = sut[1];

			await That(sut.MyIndexerWith1KeyGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith1Key_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			sut[1] = 1;

			await That(sut.MyIndexerWith1KeySetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith2Keys_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			_ = sut[1, 2];

			await That(sut.MyIndexerWith2KeysGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith2Keys_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			sut[1, 2] = 1;

			await That(sut.MyIndexerWith2KeysSetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith3Keys_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			_ = sut[1, 2, 3];

			await That(sut.MyIndexerWith3KeysGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith3Keys_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			sut[1, 2, 3] = 1;

			await That(sut.MyIndexerWith3KeysSetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith4Keys_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			_ = sut[1, 2, 3, 4];

			await That(sut.MyIndexerWith4KeysGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith4Keys_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].SkippingBaseClass(skipBaseClass);

			sut[1, 2, 3, 4] = 1;

			await That(sut.MyIndexerWith4KeysSetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith5Keys_Getter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.SkippingBaseClass(skipBaseClass);

			_ = sut[1, 2, 3, 4, 5];

			await That(sut.MyIndexerWith5KeysGetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		[Arguments(false, 1)]
		[Arguments(true, 0)]
		public async Task IndexerWith5Keys_Setter_ShouldSkipCallingBaseWhenRequested(bool skipBaseClass,
			int expectedCallCount)
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.SkippingBaseClass(skipBaseClass);

			sut[1, 2, 3, 4, 5] = 1;

			await That(sut.MyIndexerWith5KeysSetterCallCount).IsEqualTo(expectedCallCount);
		}

		[Test]
		public async Task SetupSkippingBaseClassWithoutParameter_ShouldReturnDefaultValue()
		{
			MyIndexerService sut = MyIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<string>()]
				.SkippingBaseClass();

			int result = sut["returning 2"];

			await That(result).IsEqualTo(0);
		}

		public class MyIndexerService
		{
			public int MyIndexerWith1KeyGetterCallCount { get; private set; }
			public int MyIndexerWith1KeySetterCallCount { get; private set; }
			public int MyIndexerWith2KeysGetterCallCount { get; private set; }
			public int MyIndexerWith2KeysSetterCallCount { get; private set; }
			public int MyIndexerWith3KeysGetterCallCount { get; private set; }
			public int MyIndexerWith3KeysSetterCallCount { get; private set; }
			public int MyIndexerWith4KeysGetterCallCount { get; private set; }
			public int MyIndexerWith4KeysSetterCallCount { get; private set; }
			public int MyIndexerWith5KeysGetterCallCount { get; private set; }
			public int MyIndexerWith5KeysSetterCallCount { get; private set; }

			public virtual int this[int key1]
			{
				get => MyIndexerWith1KeyGetterCallCount++;
				set => MyIndexerWith1KeySetterCallCount += value;
			}

			public virtual int this[int key1, int key2]
			{
				get => MyIndexerWith2KeysGetterCallCount++;
				set => MyIndexerWith2KeysSetterCallCount += value;
			}

			public virtual int this[int key1, int key2, int key3]
			{
				get => MyIndexerWith3KeysGetterCallCount++;
				set => MyIndexerWith3KeysSetterCallCount += value;
			}

			public virtual int this[int key1, int key2, int key3, int key4]
			{
				get => MyIndexerWith4KeysGetterCallCount++;
				set => MyIndexerWith4KeysSetterCallCount += value;
			}

			public virtual int this[int key1, int key2, int key3, int key4, int key5]
			{
				get => MyIndexerWith5KeysGetterCallCount++;
				set => MyIndexerWith5KeysSetterCallCount += value;
			}

			public virtual int this[string keyReturning2]
				=> 2;
		}
	}
}
