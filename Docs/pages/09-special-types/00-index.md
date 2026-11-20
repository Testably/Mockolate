---
description: How Mockolate handles delegates, HttpClient and loggers.
---

# Special types

Some types need more than the generic setup and verification members. Mockolate supports them out of the box:

| Type                             | What you get                                                                                       |
|----------------------------------|----------------------------------------------------------------------------------------------------|
| [Delegates](01-delegates.md)     | Mock `Action`, `Func<T>` and custom delegates; `Mock.Setup(…)` and `Mock.Verify(…)` take the delegate parameters directly. |
| [`HttpClient`](02-httpclient.md) | Set up and verify HTTP calls by verb, URI, content and headers, and mock `IHttpClientFactory`.     |
| [Logging](03-logging.md)         | Set up and verify `ILogger` calls by level and formatted message, and mock `ILoggerFactory`.      |

```csharp
// Delegates
Func<int> nextAmount = Func<int>.CreateMock();
nextAmount.Mock.Setup().Returns(3);

// HttpClient
HttpClient httpClient = HttpClient.CreateMock();
httpClient.Mock.Setup.GetAsync(It.IsUri("*testably.org/api/chocolate*"))
    .ReturnsAsync(HttpStatusCode.OK, "{\"type\":\"Dark\"}", "application/json");

// Logging
ILogger<ChocolateShop> logger = ILogger<ChocolateShop>.CreateMock();
await That(logger.Mock.Verify.LogWarning().WithMessage(It.Matches("*no Dark chocolate*"))).Once();
```
