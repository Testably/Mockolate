using aweXpect.Chronology;
using Mockolate.Exceptions;

namespace Mockolate.Tests.Exceptions;

public class MockVerificationTimeoutExceptionTests
{
	[Test]
	public async Task WithoutTimeout_ShouldHaveTimedOutMessage()
	{
		Exception exception = new("foo");
		MockVerificationTimeoutException sut = new(null, exception);

		await That(sut.Timeout).IsNull();
		await That(sut.Message).IsEqualTo("it timed out");
		await That(sut.InnerException).IsSameAs(exception);
	}

	[Test]
	public async Task WithTimeout_ShouldIncludeTimeoutInMessage()
	{
		Exception exception = new("foo");
		MockVerificationTimeoutException sut = new(30.Seconds(), exception);

		await That(sut.Timeout).IsEqualTo(30.Seconds());
		await That(sut.Message).IsEqualTo("it timed out after 00:00:30");
		await That(sut.InnerException).IsSameAs(exception);
	}
}
