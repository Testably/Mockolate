using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mockolate.ExampleTests.TestData;
using Mockolate.Verify;
#if NET8_0_OR_GREATER
using System.Net;
using System.Net.Http;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Mockolate.Web;
#endif

namespace Mockolate.ExampleTests;

public class ExampleTests
{
	[Test]
	public async Task Any_ShouldAlwaysMatch()
	{
		Guid id = Guid.NewGuid();
		MyClass sut = MyClass.CreateMock([3,])
			.Implementing<IExampleRepository>(setup => setup.AddUser(
					It.IsAny<string>())
				.Returns(new User(id, "Alice")))
			.Implementing<IOrderRepository>(setup => setup.AddOrder(
					It.IsAny<string>())
				.Returns(new Order(id, "Foo")));

		User result1 = ((IExampleRepository)sut).AddUser("Bob");
		Order result2 = ((IOrderRepository)sut).AddOrder("Bar");

		await That(result1).IsEqualTo(new User(id, "Alice"));
		await That(result2).IsEqualTo(new Order(id, "Foo"));
		sut.Mock.As<IExampleRepository>().Verify.AddUser(It.Is("Bob")).Once();
		sut.Mock.As<IOrderRepository>().Verify.AddOrder(It.Is("Bar")).Once();
	}

#if NET8_0_OR_GREATER
	[Test]
	public async Task Azure_ShouldBeMockable()
	{
		Response response = Response.CreateMock();
		Page<TableEntity> settingsPage = Page<TableEntity>.FromValues(
		[
			new TableEntity("demo", "foo"),
		], null, response);
		Pageable<TableEntity> pageable = Pageable<TableEntity>.FromPages([settingsPage,]);

		TableClient tableClient = TableClient.CreateMock(MockBehavior.Default.SkippingBaseClass());
		_ = BlobClient.CreateMock(MockBehavior.Default.ThrowingWhenNotSetup());
		tableClient.Mock.Setup
			.Query(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())
			.Returns(pageable);
		Pageable<TableEntity>? result = tableClient.Query<TableEntity>(x => x.RowKey == "foo", 10, [], CancellationToken.None);

		await That(tableClient.GetType()).IsEqualTo(typeof(Mock.TableClient));
		await That(result.AsPages().First().GetRawResponse()).IsSameAs(response);
	}
#endif

	[Test]
	public async Task BaseClassWithConstructorParameters()
	{
		MyClass sut = MyClass.CreateMock(3);

		sut.Mock.Setup.MyMethod(It.IsAny<int>()).Returns(5);

		int result = sut.MyMethod(3);

		VerificationResult<Mock.IMockVerifyForMyClass> check = sut.Mock.Verify.MyMethod(It.IsAny<int>());
		await That(result).IsEqualTo(5);
		check.Once();
	}

#if NET8_0_OR_GREATER
	[Test]
	[Arguments(HttpStatusCode.OK)]
	[Arguments(HttpStatusCode.NotFound)]
	[Arguments(HttpStatusCode.ServiceUnavailable)]
	public async Task HttpClientTest(HttpStatusCode statusCode)
	{
		HttpClient httpClient = HttpClient.CreateMock();
		httpClient.Mock.Setup.PostAsync(It.Matches("*example.com*"), It.IsHttpContent())
			.ReturnsAsync(new HttpResponseMessage(statusCode));

		HttpResponseMessage result = await httpClient.PostAsync("https://www.example.com", new StringContent(""));

		await That(result.StatusCode).IsEqualTo(statusCode);
	}

	[Test]
	public async Task IHttpClientFactory_SharedHandler_PreservesInvocationsAcrossDisposes()
	{
		HttpMessageHandler handler = HttpMessageHandler.CreateMock();
		HttpClient setupClient = HttpClient.CreateMock(handler, disposeHandler: false);
		setupClient.Mock.Setup.GetAsync(It.Matches("*example.com*"))
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));

		IHttpClientFactory factory = IHttpClientFactory.CreateMock();
		factory.Mock.Setup.CreateClient(It.IsAny<string>())
			.Returns(() => HttpClient.CreateMock(handler, disposeHandler: false));

		HttpStatusCode[] statuses = new HttpStatusCode[3];
		for (int i = 0; i < statuses.Length; i++)
		{
			using HttpClient client = factory.CreateClient("api");
			HttpResponseMessage response = await client.GetAsync($"https://example.com/{i}");
			statuses[i] = response.StatusCode;
		}

		await That(statuses).IsEqualTo([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK,]);
		setupClient.Mock.Verify.GetAsync(It.Matches("*example.com*")).Exactly(3);
	}
#endif

	[Test]
	public async Task MockIFileSystem_ShouldWork()
	{
		IFileSystem sut = IFileSystem.CreateMock(MockBehavior.Default.SkippingBaseClass());
		sut.Mock.Setup.Path.Returns(IPath.CreateMock());

		sut.Path.Mock.Setup.DirectorySeparatorChar.Returns('a');

		char result = sut.Path.DirectorySeparatorChar;

		await That(result).IsEqualTo('a');
	}

	[Test]
	public async Task SimpleInterfaceMock()
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock().Implementing<IOrderRepository>();
		sut.Mock.Setup.AddUser(It.IsAny<string>())
			.Returns(new User(id, "Alice"));
		User result = sut.AddUser("Bob");
		await That(result).IsEqualTo(new User(id, "Alice"));
		sut.Mock.Verify.AddUser(It.IsAny<string>()).Once();
	}

	[Test]
	public async Task WithAdditionalInterface_ShouldWork()
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock().Implementing<IOrderRepository>();
		sut.Mock.As<IOrderRepository>().Setup.AddOrder(It.IsAny<string>())
			.Returns(new Order(id, "Order1"));

		Order result = ((IOrderRepository)sut).AddOrder("foo");

		await That(result.Name).IsEqualTo("Order1");
		sut.Mock.As<IOrderRepository>().Verify.AddOrder(It.Is("foo")).Once();
		await That(sut).Is<IExampleRepository>();
		await That(sut).Is<IOrderRepository>();
	}

	[Test]
	public async Task WithAdditionalInterfaceInOtherOrder_ShouldWork()
	{
		Guid id = Guid.NewGuid();
		IOrderRepository sut = IOrderRepository.CreateMock().Implementing<IExampleRepository>();
		sut.Mock.As<IExampleRepository>().Setup.AddUser(It.IsAny<string>())
			.Returns(new User(id, "Alice"));
		User result = ((IExampleRepository)sut).AddUser("Bob");
		await That(result).IsEqualTo(new User(id, "Alice"));
		sut.Mock.As<IExampleRepository>().Verify.AddUser(It.IsAny<string>()).Once();
	}

	[Test]
	public async Task WithEvent_ShouldSupportRaisingEvent()
	{
		EventArgs eventArgs = EventArgs.Empty;
		int raiseCount = 0;
		IExampleRepository sut = IExampleRepository.CreateMock();

		sut.Mock.Raise.UsersChanged(this, eventArgs);
		sut.UsersChanged += Register;
		sut.Mock.Raise.UsersChanged(this, eventArgs);
		sut.Mock.Raise.UsersChanged(this, eventArgs);
		sut.UsersChanged -= Register;
		sut.Mock.Raise.UsersChanged(this, eventArgs);

		await That(raiseCount).IsEqualTo(2);

		void Register(object? sender, EventArgs e)
		{
			if (sender == this && e == eventArgs)
			{
				raiseCount++;
			}
		}
	}

	[Test]
	public async Task WithExplicitCastToAdditionalInterfaceSetup_ShouldWork()
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock().Implementing<IOrderRepository>();
		((IOrderRepository)sut).Mock.Setup.AddOrder(It.IsAny<string>())
			.Returns(new Order(id, "Order1"));

		Order result = ((IOrderRepository)sut).AddOrder("foo");

		await That(result.Name).IsEqualTo("Order1");
		sut.Mock.As<IOrderRepository>().Verify.AddOrder(It.Is("foo")).Once();
		await That(sut).Is<IExampleRepository>();
		await That(sut).Is<IOrderRepository>();
	}

	[Test]
	public async Task WithExplicitCastToAdditionalInterfaceVerify_ShouldWork()
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock().Implementing<IOrderRepository>();
		sut.Mock.As<IOrderRepository>().Setup
			.AddOrder(It.IsAny<string>())
			.Returns(new Order(id, "Order1"));

		Order result = ((IOrderRepository)sut).AddOrder("foo");

		await That(result.Name).IsEqualTo("Order1");
		((IOrderRepository)sut).Mock.Verify.AddOrder(It.Is("foo")).Once();
		await That(sut).Is<IExampleRepository>();
		await That(sut).Is<IOrderRepository>();
	}

	[Test]
	[Arguments("Alice", true)]
	[Arguments("Bob", false)]
	public async Task WithMatching_ShouldAlwaysMatch(string name, bool expectResult)
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock();

		sut.Mock.Setup.AddUser(
				It.Satisfies<string>(x => x == "Alice"))
			.Returns(new User(id, "Alice"));

		User result = sut.AddUser(name);

		await That(result).IsEqualTo(expectResult ? new User(id, "Alice") : null);
		sut.Mock.Verify.AddUser(It.Is(name)).Once();
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task WithOut_ShouldSupportOutParameter(bool returnValue)
	{
		Guid id = Guid.NewGuid();
		IExampleRepository sut = IExampleRepository.CreateMock();

		sut.Mock.Setup.TryDelete(
				It.IsAny<Guid>(),
				It.IsOut<User?>(() => new User(id, "Alice")))
			.Returns(returnValue);

		bool result = sut.TryDelete(id, out User? deletedUser);

		await That(deletedUser).IsEqualTo(new User(id, "Alice"));
		await That(result).IsEqualTo(returnValue);
		sut.Mock.Verify.TryDelete(It.Is(id), It.IsOut<User?>()).Once();
	}

	[Test]
	public async Task Wrapping_HiddenMember_ShouldForwardToDeclaringInterface()
	{
		User alice = new(Guid.NewGuid(), "Alice");
		User bob = new(Guid.NewGuid(), "Bob");
		MyUserCache realCache = new();
		IUserCache sut = IUserCache.CreateMock().Wrapping(realCache);

		sut.Users = [alice,];
		((IUserCacheBase)sut).Users = [bob,];

		// Each interface sees its own member on the wrapped instance.
		await That(sut.Users).IsEqualTo([alice,]);
		await That(((IUserCacheBase)sut).Users).IsEqualTo([bob,]);
		await That(realCache.CacheUsers).IsEqualTo([alice,]);
		await That(realCache.BaseUsers).IsEqualTo([bob,]);
	}

	private sealed class MyUserCache : IUserCache
	{
		public IList<User> CacheUsers { get; private set; } = [];

		public IEnumerable<User> BaseUsers { get; private set; } = [];

		public IList<User> Users
		{
			get => CacheUsers;
			set => CacheUsers = value;
		}

		IEnumerable<User> IUserCacheBase.Users
		{
			get => BaseUsers;
			set => BaseUsers = value;
		}
	}
}
