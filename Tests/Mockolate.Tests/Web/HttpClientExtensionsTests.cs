using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Exceptions;
using Mockolate.Parameters;
using Mockolate.Setup;
using Mockolate.Verify;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class HttpClientExtensionsTests
{
	[Test]
	public async Task CustomParameter_WithoutIParameterMatch_ShouldBeInvokedViaAdapter()
	{
		HttpClient httpClient = HttpClient.CreateMock();
		IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> setup = httpClient
			.Mock.Setup
			.GetAsync(new InvalidParameter())
			.ReturnsAsync(HttpStatusCode.OK);
		ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> methodSetup =
			(ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>)setup;

		void Act()
		{
			methodSetup.Matches(
				new HttpRequestMessage(HttpMethod.Get, "http://localhost/"),
				CancellationToken.None);
		}

		await That(Act).Throws<NotSupportedException>();
	}

	[Test]
	public async Task InvokedSetup_ShouldWorkForHttpClient()
	{
		HttpClient httpClient = HttpClient.CreateMock();
		IMethodSetup setup = httpClient
			.Mock.Setup.GetAsync(It.IsUri("https://www.testably.org"))
			.ReturnsAsync(HttpStatusCode.Accepted);
		HttpResponseMessage result =
			await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

		await That(result.StatusCode)
			.IsEqualTo(HttpStatusCode.Accepted);
		await That(httpClient.Mock.VerifySetup(setup)).Once();
	}

	[Test]
	public async Task InvokedSetup_WhenMethodSetupIsNotVerifiable_ShouldThrowMockException()
	{
		HttpClient sut = HttpClient.CreateMock();
		IMethodSetup setup = new MyMethodSetup();

		void Act()
		{
			sut.Mock.VerifySetup(setup).Never();
		}

		await That(Act).Throws<MockException>()
			.WithMessage("The setup is not verifiable.");
	}

	[Test]
	public async Task NullRequestUri_ShouldInvokeCallbackWithNull()
	{
		// ReSharper disable once VariableCanBeNotNullable
		string? callbackUri = "initialized";
		HttpClient httpClient = HttpClient.CreateMock();
		IMethodSetup setup = httpClient
			.Mock.Setup
			.GetAsync(It.Matches("*").Do(uri => callbackUri = uri))
			.ReturnsAsync(HttpStatusCode.OK);
		ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> interactiveSetup = (ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>)setup;

		interactiveSetup.TriggerCallbacks(new HttpRequestMessage(), CancellationToken.None);
		await That(callbackUri).IsNull();
	}

	[Test]
	public async Task NullRequestUri_ShouldReturnFalse()
	{
		HttpClient httpClient = HttpClient.CreateMock();
		IMethodSetup setup = httpClient
			.Mock.Setup
			.GetAsync(It.Matches("*"))
			.ReturnsAsync(HttpStatusCode.OK);
		ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> methodSetup =
			(ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>)setup;

		bool result = methodSetup.Matches(new HttpRequestMessage(), CancellationToken.None);

		await That(result).IsFalse();
	}

	[Test]
	public async Task NullUri_ShouldReturnFalse()
	{
		HttpClient httpClient = HttpClient.CreateMock();
		IReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> setup = httpClient
			.Mock.Setup
			.GetAsync(It.IsAny<string?>())
			.ReturnsAsync(HttpStatusCode.OK);
		ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken> methodSetup =
			(ReturnMethodSetup<Task<HttpResponseMessage>, HttpRequestMessage, CancellationToken>)setup;

		bool result = methodSetup.Matches(new HttpRequestMessage(), CancellationToken.None);

		await That(result).IsFalse();
	}

	[Test]
	public async Task SendAsync_WithoutMockedHttpMessageHandler_ShouldThrowMockException()
	{
		HttpClient httpClient = HttpClient.CreateMock([]);

		void Act()
		{
			httpClient.Mock.Setup
				.SendAsync(It.IsHttpRequestMessage())
				.ReturnsAsync(HttpStatusCode.OK);
		}

		await That(Act).Throws<MockException>()
			.WithMessage(
				"Cannot setup HttpClient when it is not mocked with a mockable HttpMessageHandler.");
	}

	[Test]
	public async Task ShouldSupportMonitoring()
	{
		int callbackCount = 0;
		HttpClient httpClient = HttpClient.CreateMock();
		httpClient.Mock.Setup
			.GetAsync(It.Matches("*")
				.Do(_ => callbackCount++)
				.Monitor(out IParameterMonitor<string> monitor))
			.ReturnsAsync(HttpStatusCode.OK);

		await httpClient.GetAsync("https://www.Testably.org/foo", CancellationToken.None);
		await httpClient.PostAsync("https://www.Testably.org/bar", null, CancellationToken.None);
		await httpClient.GetAsync("https://www.Testably.org/baz", CancellationToken.None);

		await That(monitor.Values).IsEqualTo([
			"https://www.testably.org/foo",
			"https://www.testably.org/baz",
		]);
		await That(callbackCount).IsEqualTo(2);
	}

	[Test]
	[Arguments("*testably.org")]
	[Arguments("*testably.org/")]
	public async Task TrailingSlash_ShouldBeIgnored(string matchPattern)
	{
		HttpClient httpClient = HttpClient.CreateMock();
		httpClient.Mock.Setup
			.GetAsync(It.Matches(matchPattern))
			.ReturnsAsync(HttpStatusCode.OK);

		HttpResponseMessage result =
			await httpClient.GetAsync("https://www.testably.org", CancellationToken.None);

		await That(result.StatusCode)
			.IsEqualTo(HttpStatusCode.OK);
	}

	[Test]
	public async Task TrailingSlash_WhenNotPresent_ShouldNotBeAdded()
	{
		HttpClient httpClient = HttpClient.CreateMock();
		httpClient.Mock.Setup
			.GetAsync(It.Matches("*www.testably.org/foo/"))
			.ReturnsAsync(HttpStatusCode.OK);

		HttpResponseMessage result =
			await httpClient.GetAsync("https://www.testably.org/foo", CancellationToken.None);

		await That(result.StatusCode)
			.IsEqualTo(HttpStatusCode.NotImplemented);
	}

	private sealed class InvalidParameter : IParameter<string?>
	{
		public bool Matches(object? value)
			=> throw new NotSupportedException();

		public void InvokeCallbacks(object? value)
			=> throw new NotSupportedException();

		public IParameter<string?> Do(Action<string?> callback)
			=> throw new NotSupportedException();
	}

	private sealed class MyMethodSetup : IMethodSetup
	{
		public string Name { get; } = "Foo";
	}
}
