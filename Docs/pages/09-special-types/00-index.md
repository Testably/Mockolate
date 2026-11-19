---
description: How Mockolate handles HttpClient, delegates and loggers.
---

# Special types

Some types need more than the generic setup and verification members. Mockolate supports them out of the box:

| Type                             | What you get                                                                                       |
|----------------------------------|----------------------------------------------------------------------------------------------------|
| [`HttpClient`](01-httpclient.md) | Set up and verify HTTP calls by verb, URI, content and headers, and mock `IHttpClientFactory`.     |
| [Delegates](02-delegates.md)     | Mock `Action`, `Func<T>` and custom delegates; `Mock.Setup(…)` and `Mock.Verify(…)` take the delegate parameters directly. |
| [Logging](03-logging.mdx)        | Set up and verify `ILogger` calls by level and formatted message, mock `ILoggerFactory` and check the records of a `FakeLogger`. |

```csharp
// HttpClient
HttpClient httpClient = HttpClient.CreateMock();
httpClient.Mock.Setup.GetAsync(It.IsUri("*testably.org/api/chocolate*"))
    .ReturnsAsync(HttpStatusCode.OK, "{\"type\":\"Dark\"}", "application/json");

// Delegates
Func<int> nextAmount = Func<int>.CreateMock();
nextAmount.Mock.Setup().Returns(3);

// Logging
ILogger<ChocolateShop> logger = ILogger<ChocolateShop>.CreateMock();
logger.Mock.Verify.LogWarning(It.Matches("*no Dark chocolate*")).Once();
```
