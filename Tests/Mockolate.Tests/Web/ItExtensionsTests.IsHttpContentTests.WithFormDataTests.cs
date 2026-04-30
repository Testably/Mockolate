using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsHttpContentTests
	{
		public sealed class WithFormDataTests
		{
			[Test]
			[Arguments(true, "x", "123", "y", "234", "z", "345", "z", "567")]
			[Arguments(true, "z", "567", "x", "123", "z", "345", "y", "234")]
			[Arguments(true, "x", "123", "y", "234", "z", "345", "z", "567", "z", "567")]
			[Arguments(false, "z", "567", "x", "123", "x", "345", "y", "234")]
			[Arguments(false, "x", "123", "y", "234", "z", "345", "z", "567", "z", "789")]
			[Arguments(false, "y", "234", "z", "345", "z", "567")]
			[Arguments(false, "x", "123", "z", "345")]
			[Arguments(false, "x", "123", "y", "234")]
			[Arguments(false, "x", "123", "y", "234", "z", "345")]
			public async Task Exactly_ShouldOnlyMatchWhenAllParametersAreChecked(
				bool expectSuccess, params string[] rawValues)
			{
				List<(string, HttpFormDataValue)> values = new();
				for (int i = 0; i < rawValues.Length; i += 2)
				{
					values.Add((rawValues[i], new HttpFormDataValue(rawValues[i + 1])));
				}

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(values).Exactly())
					.ReturnsAsync(HttpStatusCode.OK);
				MultipartFormDataContent content = new()
				{
					new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
					{
						new("x", "123"),
						new("y", "234"),
						new("z", "345"),
						new("z", "567"),
					}),
				};

				HttpResponseMessage result =
					await httpClient.PostAsync("https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("x<>", "1<2> 3 ")]
			public async Task ShouldEncodeValues(string key, string value)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(key, value))
					.ReturnsAsync(HttpStatusCode.OK);
				MultipartFormDataContent content = new()
				{
					new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{
							key, value
						},
					}),
				};

				HttpResponseMessage result =
					await httpClient.PostAsync("https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("x", "123", "z", "345")]
			[Arguments("z", "345", "x", "123")]
			public async Task WithFormData_ShouldMatchParametersInAnyOrder(params string[] rawValues)
			{
				List<(string, HttpFormDataValue)> values = new();
				for (int i = 0; i < rawValues.Length; i += 2)
				{
					values.Add((rawValues[i], new HttpFormDataValue(rawValues[i + 1])));
				}

				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(values))
					.ReturnsAsync(HttpStatusCode.OK);
				MultipartFormDataContent content = new()
				{
					new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{
							"x", "123"
						},
						{
							"y", "234"
						},
						{
							"z", "345"
						},
					}),
				};

				HttpResponseMessage result =
					await httpClient.PostAsync("https://www.testably.org", content, CancellationToken.None);

				await That(httpClient.Mock.Verify
						.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(values)))
					.Once();
				await That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
			}

			[Test]
			[Arguments("x", "123", true)]
			[Arguments("y", "234", true)]
			[Arguments("x", "", false)]
			[Arguments("y", "", false)]
			[Arguments("x", "234", false)]
			[Arguments("y", "123", false)]
			[Arguments("foo", "", true)]
			public async Task WithFormDataParameter_ShouldVerifyParameters(string key, string value, bool expectMatch)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(key, value))
					.ReturnsAsync(HttpStatusCode.OK);
				MultipartFormDataContent content = new()
				{
					new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{
							"x", "123"
						},
						{
							"foo", ""
						},
						{
							"y", "234"
						},
					}),
				};

				HttpResponseMessage result =
					await httpClient.PostAsync("https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectMatch ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}

			[Test]
			[Arguments("?x=123&y=234", true)]
			[Arguments("?y=234&x=123&", true)]
			[Arguments("x=123", true)]
			[Arguments("y=234", true)]
			[Arguments("x", false)]
			[Arguments("y", false)]
			[Arguments("x=234", false)]
			[Arguments("y=123", false)]
			[Arguments("x=123&y=432", false)]
			[Arguments("y=432&x=123", false)]
			[Arguments("foo", true)]
			public async Task WithFormDataString_ShouldVerifyParameters(string values, bool expectMatch)
			{
				HttpClient httpClient = HttpClient.CreateMock();
				httpClient.Mock.Setup
					.PostAsync(It.IsAny<Uri>(), It.IsHttpContent().WithFormData(values))
					.ReturnsAsync(HttpStatusCode.OK);
				MultipartFormDataContent content = new()
				{
					new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{
							"x", "123"
						},
						{
							"foo", ""
						},
						{
							"y", "234"
						},
					}),
				};

				HttpResponseMessage result =
					await httpClient.PostAsync("https://www.testably.org", content, CancellationToken.None);

				await That(result.StatusCode)
					.IsEqualTo(expectMatch ? HttpStatusCode.OK : HttpStatusCode.NotImplemented);
			}
		}
	}
}
