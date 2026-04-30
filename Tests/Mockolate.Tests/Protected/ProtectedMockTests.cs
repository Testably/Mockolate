namespace Mockolate.Tests.Protected;

public sealed class ProtectedMockTests
{
	[Test]
	public async Task CanAccessProtectedEvents()
	{
		int callCount = 0;
		MyProtectedClass sut = MyProtectedClass.CreateMock();
		MyProtectedClass.MyEventHandler handler = (_, _) => callCount++;

		sut.RegisterEvent(handler);
		sut.Mock.RaiseProtected.MyEvent(this, EventArgs.Empty);

		await That(sut.Mock.VerifyProtected.MyEvent.Subscribed()).Once();
		await That(sut.Mock.VerifyProtected.MyEvent.Unsubscribed()).Never();
		sut.UnregisterEvent(handler);
		await That(sut.Mock.VerifyProtected.MyEvent.Unsubscribed()).Once();
		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task CanAccessProtectedMethods()
	{
		MyProtectedClass sut = MyProtectedClass.CreateMock();

		sut.Mock.SetupProtected.MyProtectedMethod(It.IsAny<string>())
			.Returns(v => $"Hello, {v}!");

		string result = sut.InvokeMyProtectedMethod("foo");

		await That(sut.Mock.VerifyProtected.MyProtectedMethod(It.Is("foo"))).Once();
		await That(result).IsEqualTo("Hello, foo!");
	}

	[Test]
	public async Task CanAccessProtectedProperties()
	{
		MyProtectedClass sut = MyProtectedClass.CreateMock();

		sut.Mock.SetupProtected.MyProtectedProperty.InitializeWith(42);

		int result = sut.GetMyProtectedProperty();

		await That(sut.Mock.VerifyProtected.MyProtectedProperty.Got()).Once();
		await That(result).IsEqualTo(42);
	}

	[Test]
	public async Task CanReadProtectedIndexers()
	{
		int callCount = 0;
		MyProtectedClass sut = MyProtectedClass.CreateMock();

		sut.Mock.SetupProtected[It.IsAny<int>()].InitializeWith(42).OnGet.Do(() => callCount++);

		int result = sut.GetMyProtectedIndexer(3);

		await That(sut.Mock.VerifyProtected[It.Is(3)].Got()).Once();
		await That(result).IsEqualTo(42);
		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task CanWriteProtectedIndexers()
	{
		int callCount = 0;
		MyProtectedClass sut = MyProtectedClass.CreateMock();

		sut.Mock.SetupProtected[It.IsAny<int>()].OnSet.Do(() => callCount++);

		sut.SetMyProtectedIndexer(3, 4);

		await That(sut.Mock.VerifyProtected[It.Is(3)].Set(It.Is(4))).Once();
		await That(sut.GetMyProtectedIndexer(3)).IsEqualTo(4);
		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task OnlyProtectedVirtualMembers()
	{
		OnlyProtectedVirtualMembersService sut = OnlyProtectedVirtualMembersService.CreateMock();

		bool result1 = sut.DoValidate(-1);

		sut.Mock.SetupProtected.Validate(It.IsAny<int>()).Returns(true);

		bool result2 = sut.DoValidate(-1);

		await That(result1).IsFalse();
		await That(result2).IsTrue();
	}

	[Test]
	public async Task SupportProtectedAccessInConstructorSetups()
	{
		MyProtectedClass sut = MyProtectedClass.CreateMock(setup => setup.Protected.MyProtectedProperty.InitializeWith(42));

		int result = sut.GetMyProtectedProperty();

		await That(sut.Mock.VerifyProtected.MyProtectedProperty.Got()).Once();
		await That(result).IsEqualTo(42);
	}

	public abstract class OnlyProtectedVirtualMembersService
	{
		public bool DoValidate(int value)
			=> Validate(value);

		protected virtual bool Validate(int value)
			=> value > 0;
	}

#pragma warning disable CS0067 // Event is never used
#pragma warning disable CA1070 // Do not declare event fields as virtual
	public abstract class MyProtectedClass
	{
		public delegate void MyEventHandler(object? sender, EventArgs e);

		protected virtual int MyProtectedProperty { get; set; }

		protected abstract int this[int key] { get; set; }

		protected virtual event MyEventHandler? MyEvent;
		protected abstract string MyProtectedMethod(string input);

		public int GetMyProtectedProperty()
			=> MyProtectedProperty;

		public int GetMyProtectedIndexer(int key)
			=> this[key];

		public void SetMyProtectedIndexer(int key, int value)
			=> this[key] = value;

		public string InvokeMyProtectedMethod(string input)
			=> MyProtectedMethod(input);

		public void RegisterEvent(MyEventHandler callback)
			=> MyEvent += callback;

		public void UnregisterEvent(MyEventHandler callback)
			=> MyEvent -= callback;
	}
#pragma warning restore CA1070 // Do not declare event fields as virtual
#pragma warning restore CS0067 // Event is never used
}
