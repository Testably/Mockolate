using Mockolate.Exceptions;

namespace Mockolate.Tests.MockDelegates;

public partial class DelegateTests
{
	[Fact]
	public async Task Action_ShouldBeTreatedAsVoidDelegate()
	{
		bool isCalled = false;
		Action sut = Action.CreateMock();
		sut.Mock.Setup().Do(() => isCalled = true);

		sut.Invoke();

		await That(sut.Mock.Verify()).Once();
		await That(isCalled).IsTrue();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CreateMockWithBehavior_ShouldApplyBehavior(bool throwWhenNotSetup)
	{
		Func<int> sut = Func<int>.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup(throwWhenNotSetup));

		void Act()
		{
			_ = sut();
		}

		await That(Act).Throws<MockNotSetupException>().OnlyIf(throwWhenNotSetup)
			.WithMessage("The method 'global::System.Func<int>.Invoke()' was invoked without prior setup.");
	}

	[Fact]
	public async Task CreateMockWithSetup_ShouldApplySetups()
	{
		Func<int> sut = Func<int>.CreateMock(mock => mock.Setup().Returns(3));

		int result = sut();

		await That(result).IsEqualTo(3);
	}

	[Fact]
	public async Task Func_ShouldBeTreatedAsReturnDelegate()
	{
		bool isCalled = false;
		Func<int> sut = Func<int>.CreateMock();
		sut.Mock.Setup().Do(() => isCalled = true).Returns(3);

		int result = sut();

		await That(sut.Mock.Verify()).Once();
		await That(isCalled).IsTrue();
		await That(result).IsEqualTo(3);
	}

	[Fact]
	public async Task WithCustomDelegate_SetupShouldWork()
	{
		DoSomething sut = DoSomething.CreateMock();
		sut.Mock.Setup(It.IsAny<int>(), It.IsAny<string>(), It.IsTrue())
			.Returns(1)
			.Throws(new Exception("foobar"))
			.Returns(3);

		int result1 = sut(1, "foo", true);
		await That(() => sut(2, "foo", true)).Throws<Exception>().WithMessage("foobar");
		int result3 = sut(2, "bar", true);

		await That(result1).IsEqualTo(1);
		await That(result3).IsEqualTo(3);
	}

	[Fact]
	public async Task WithCustomDelegate_VerifyShouldWork()
	{
		DoSomething sut = DoSomething.CreateMock();

		_ = sut(1, "foo", true);
		_ = sut(2, "bar", true);

		await That(sut.Mock.Verify(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Twice();
	}

	[Fact]
	public async Task WithCustomDelegateWithInts_SetupWithExplicitParameter_ShouldWork()
	{
		DoSomethingWithInts sut = DoSomethingWithInts.CreateMock();
		sut.Mock.Setup(1, It.IsAny<int>(), It.IsAny<int>()).Returns(42);

		int result1 = sut(1, 10, 20);
		int result2 = sut(2, 10, 20);

		await That(result1).IsEqualTo(42);
		await That(result2).IsEqualTo(0);
	}

	[Fact]
	public async Task WithCustomDelegateWithInts_VerifyWithExplicitParameter_ShouldWork()
	{
		DoSomethingWithInts sut = DoSomethingWithInts.CreateMock();

		_ = sut(1, 10, 20);
		_ = sut(2, 10, 20);

		await That(sut.Mock.Verify(1, It.IsAny<int>(), It.IsAny<int>())).Once();
		await That(sut.Mock.Verify(3, It.IsAny<int>(), It.IsAny<int>())).Never();
	}

	[Fact]
	public async Task WithCustomDelegateWithRefAndOut_SetupShouldWork()
	{
		DoSomethingWithRefAndOut sut = DoSomethingWithRefAndOut.CreateMock();
		int value = 5;
		sut.Mock.Setup(It.IsAny<int>(), It.IsRef<int>(v => v + 1), It.IsOut(() => 10));

		sut(1, ref value, out int value2);

		await That(sut.Mock.Verify(It.IsAny<int>(), It.IsRef<int>(), It.IsOut<int>())).Once();
		await That(value).IsEqualTo(6);
		await That(value2).IsEqualTo(10);
	}

	[Fact]
	public async Task WithCustomDelegateWithRefAndOut_VerifyShouldWork()
	{
		DoSomethingWithRefAndOut sut = DoSomethingWithRefAndOut.CreateMock();
		int value = 5;

		sut(1, ref value, out int _);

		await That(sut.Mock.Verify(It.IsAny<int>(), It.IsRef<int>(), It.IsOut<int>())).Once();
		await That(value).IsEqualTo(5);
	}

	[Fact]
	public async Task WithCustomGenericDelegate_SetupShouldWork()
	{
		DoGeneric<long, string> sut = DoGeneric<long, string>.CreateMock();
		sut.Mock.Setup(It.IsAny<long>(), It.IsAny<string>())
			.Returns(1)
			.Throws(new Exception("foobar"))
			.Returns(3);

		int result1 = sut(1L, "foo");
		await That(() => sut(2L, "foo")).Throws<Exception>().WithMessage("foobar");
		int result3 = sut(2L, "bar");

		await That(result1).IsEqualTo(1);
		await That(result3).IsEqualTo(3);
	}

	[Fact]
	public async Task WithCustomGenericDelegate_VerifyShouldWork()
	{
		DoGeneric<short, string> sut = DoGeneric<short, string>.CreateMock();

		_ = sut(1, "foo");
		_ = sut(2, "bar");

		await That(sut.Mock.Verify(It.IsAny<short>(), It.IsAny<string>())).Twice();
	}

	internal delegate int DoSomething(int x, string y, bool p);

	internal delegate void DoSomethingWithRefAndOut(int x, ref int y, out int z);

	internal delegate int DoGeneric<T1, T2>(T1 x, T2 y)
		where T1 : struct
		where T2 : class;

	internal delegate int DoSomethingWithInts(int a, int b, int c);
}
