using Mockolate.Web;

namespace Mockolate.Tests.Web;

public sealed partial class ItExtensionsTests
{
	public sealed partial class IsUriTests
	{
		public sealed class ToStringTests
		{
			[Test]
			public async Task Default_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri();

				string? result = sut.ToString();

				await That(result).IsEqualTo("any Uri");
			}

			[Test]
			public async Task ForHttp_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().ForHttp();

				string? result = sut.ToString();

				await That(result).IsEqualTo("http Uri");
			}

			[Test]
			public async Task ForHttps_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().ForHttps();

				string? result = sut.ToString();

				await That(result).IsEqualTo("https Uri");
			}

			[Test]
			public async Task MultipleConditions_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri("*example.com*")
					.ForHttps()
					.WithHost("example.com")
					.WithPort(443)
					.WithPath("/api")
					.WithQuery("x=1");

				string? result = sut.ToString();

				await That(result).IsEqualTo(
					"https Uri matching \"*example.com*\" with host matching \"example.com\" with port 443 with path matching \"/api\" with query containing \"x=1\"");
			}

			[Test]
			public async Task MultipleWithQuery_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri()
					.WithQuery("x", "1")
					.WithQuery("y", "2");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with query containing \"x=1\", \"y=2\"");
			}

			[Test]
			public async Task WithHost_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithHost("example.com");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with host matching \"example.com\"");
			}

			[Test]
			public async Task WithPath_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithPath("/api/v1");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with path matching \"/api/v1\"");
			}

			[Test]
			public async Task WithPattern_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri("*example.com*");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri matching \"*example.com*\"");
			}

			[Test]
			public async Task WithPort_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithPort(443);

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with port 443");
			}

			[Test]
			public async Task WithQueryKeyValue_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithQuery("x", "1");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with query containing \"x=1\"");
			}

			[Test]
			public async Task WithQueryString_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithQuery("x=1");

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with query containing \"x=1\"");
			}

			[Test]
			public async Task WithQueryTuple_ShouldReturnExpectedValue()
			{
				ItExtensions.IUriParameter sut = It.IsUri().WithQuery(("x", "1"));

				string? result = sut.ToString();

				await That(result).IsEqualTo("Uri with query containing \"x=1\"");
			}
		}
	}
}
