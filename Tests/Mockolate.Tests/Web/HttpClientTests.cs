using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Setup;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public class HttpClientTests
{
	[Test]
	public async Task WithSharedHandler_ShouldShareSetupsAndInteractionsBetweenClients()
	{
		HttpMessageHandler handler = HttpMessageHandler.CreateMock();
		HttpClient client1 = HttpClient.CreateMock([handler,]);
		HttpClient client2 = HttpClient.CreateMock([handler,]);
		IMethodSetup setup = client1.Mock.Setup
			.GetAsync(It.IsUri("testably.org"))
			.ReturnsAsync(HttpStatusCode.Accepted);

		HttpResponseMessage response = await client2.GetAsync("https://testably.org", CancellationToken.None);

		await That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
		await That(client1.Mock.VerifySetup(setup)).Once();
		await That(client2.Mock.VerifySetup(setup)).Once();
	}

	[Test]
	public async Task SendAsync_ShouldBeCountedSeparatelyForTheClientAndItsHandler()
	{
		HttpMessageHandler handler = HttpMessageHandler.CreateMock();
		HttpClient client = HttpClient.CreateMock([handler,]);
		client.Mock.Setup
			.GetAsync(It.IsUri("testably.org"))
			.ReturnsAsync(HttpStatusCode.Accepted);

		await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://testably.org"), CancellationToken.None);
		await client.GetAsync("https://testably.org", CancellationToken.None);

		await That(client.Mock.Verify.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).Once()
			.Because("only the first request goes through the virtual SendAsync of the client");
		await That(handler.Mock.VerifyProtected.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).Twice()
			.Because("both requests reach the handler");
		await That(client.Mock.Verify.GetAsync(It.IsUri("testably.org"))).Twice();
	}

	[Test]
	public async Task SendAsync_WithoutExplicitHandler_ShouldOnlyCountTheCallsOnTheClient()
	{
		HttpClient client = HttpClient.CreateMock();
		client.Mock.Setup
			.GetAsync(It.IsUri("testably.org"))
			.ReturnsAsync(HttpStatusCode.Accepted);

		await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://testably.org"), CancellationToken.None);
		await client.GetAsync("https://testably.org", CancellationToken.None);

		await That(client.Mock.Verify.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).Once()
			.Because("only the first request goes through the virtual SendAsync of the client");
		await That(client.Mock.Verify.GetAsync(It.IsUri("testably.org"))).Twice();
	}

	[Test]
	public async Task SendAsync_WithSetupOnTheClient_ShouldNotApplyToGetAsync()
	{
		HttpMessageHandler handler = HttpMessageHandler.CreateMock();
		HttpClient client = HttpClient.CreateMock([handler,]);
		client.Mock.Setup
			.GetAsync(It.IsUri("testably.org"))
			.ReturnsAsync(HttpStatusCode.Accepted);
		client.Mock.Setup
			.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

		HttpResponseMessage response = await client.GetAsync("https://testably.org", CancellationToken.None);

		await That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted)
			.Because("GetAsync does not go through the virtual SendAsync of the client, so only the setup for the handler applies");
	}

	[Test]
	public async Task WithDifferentBehavior_ShouldThrow()
	{
		MockBehavior behavior1 = MockBehavior.Default.SkippingBaseClass();
		MockBehavior behavior2 = MockBehavior.Default.ThrowingWhenNotSetup();
		HttpMessageHandler handler = HttpMessageHandler.CreateMock(behavior1);

		void Act()
		{
			_ = HttpClient.CreateMock(behavior2, [handler,]);
		}

		await That(Act).Throws<MockException>().WithMessage(
			"Mock of type 'System.Net.Http.HttpClient' cannot be created with behavior 'ThrowingWhenNotSetup' because it shares its mock registry with a mock of type 'System.Net.Http.HttpMessageHandler' that has behavior 'SkippingBaseClass'.");
	}

	[Test]
	public async Task WithoutBehavior_ShouldSucceed()
	{
		MockBehavior behavior1 = MockBehavior.Default.SkippingBaseClass();
		HttpMessageHandler handler = HttpMessageHandler.CreateMock(behavior1);

		void Act()
		{
			_ = HttpClient.CreateMock([handler,]);
		}

		await That(Act).DoesNotThrow();
	}

	[Test]
	public async Task WithSameBehavior_ShouldSucceed()
	{
		MockBehavior behavior1 = MockBehavior.Default.SkippingBaseClass();
		MockBehavior behavior2 = MockBehavior.Default.SkippingBaseClass();
		HttpMessageHandler handler = HttpMessageHandler.CreateMock(behavior1);

		void Act()
		{
			_ = HttpClient.CreateMock(behavior2, [handler,]);
		}

		await That(Act).DoesNotThrow();
	}
}
