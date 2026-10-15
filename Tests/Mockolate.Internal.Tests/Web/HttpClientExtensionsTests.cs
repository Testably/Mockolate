using System.IO;
using System.Net;
using System.Net.Http;
using Mockolate.Parameters;
using Mockolate.Web;

namespace Mockolate.Internal.Tests.Web;

public class HttpClientExtensionsTests
{
	[Test]
	public async Task WhenParameterImplementsIHttpRequestMessagePropertyParameter_ShouldUseThisMatch()
	{
		ItExtensions.IHttpContentParameter parameter = ItExtensions.IHttpContentParameter.CreateMock()
			.Implementing<IParameterMatch<HttpContent?>>()
			.Implementing<IHttpRequestMessagePropertyParameter<HttpContent?>>();
		parameter.Mock.As<IParameterMatch<HttpContent?>>().Setup.Matches(It.IsAny<HttpContent?>()).Returns(false);
		parameter.Mock.As<IHttpRequestMessagePropertyParameter<HttpContent?>>().Setup
			.Matches(It.IsAny<HttpContent?>(), It.IsAny<HttpRequestMessage?>()).Returns(true);

		ItExtensions.IStringContentBodyParameter sut = parameter.WithString("foo");

		bool result = ((IHttpRequestMessagePropertyParameter<HttpContent?>)sut).Matches(null, null);

		await That(result).IsTrue();
		await That(parameter.Mock.As<IHttpRequestMessagePropertyParameter<HttpContent?>>().Verify
				.Matches(It.IsNull<HttpContent?>(), It.IsNull<HttpRequestMessage?>()))
			.Once();
		await That(parameter.Mock.As<IParameterMatch<HttpContent?>>().Verify
				.Matches(It.IsAny<HttpContent?>()))
			.Never();
	}

	[Test]
	public async Task WithoutMediaTypeHeader_WhenNoneIsRequired_ShouldReturnTrue()
	{
		ItExtensions.IHttpContentParameter sut = It.IsHttpContent();
		IParameterMatch<HttpContent?> parameter = (IParameterMatch<HttpContent?>)sut;

		bool result = parameter.Matches(new MyHttpContent());

		await That(result).IsTrue();
	}

	[Test]
	public async Task WithoutMediaTypeHeader_WhenOneIsRequired_ShouldReturnFalse()
	{
		ItExtensions.IHttpContentParameter sut = It.IsHttpContent("*");
		IParameterMatch<HttpContent?> parameter = (IParameterMatch<HttpContent?>)sut;

		bool result = parameter.Matches(new MyHttpContent());

		await That(result).IsFalse();
	}

	private sealed class MyHttpContent : HttpContent
	{
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
			=> Task.CompletedTask;

		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}
	}
}
