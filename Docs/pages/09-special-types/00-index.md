---
description: How Mockolate handles HttpClient and delegates.
---

# Special types

Some types need more than the generic setup and verification members. Mockolate supports them out of the box:

| Type                             | What you get                                                                                       |
|----------------------------------|----------------------------------------------------------------------------------------------------|
| [`HttpClient`](01-httpclient.md) | Set up and verify HTTP calls by verb, URI, content and headers, and mock `IHttpClientFactory`.     |
| [Delegates](02-delegates.md)     | Mock `Action`, `Func<T>` and custom delegates; `Mock.Setup(…)` and `Mock.Verify(…)` take the delegate parameters directly. |

```csharp
// HttpClient
HttpClient httpClient = HttpClient.CreateMock();
httpClient.Mock.Setup.GetAsync(It.IsUri("*testably.org/api/chocolate*"))
    .ReturnsAsync(HttpStatusCode.OK, "{\"type\":\"Dark\"}", "application/json");

// Delegates
Func<int> nextAmount = Func<int>.CreateMock();
nextAmount.Mock.Setup().Returns(3);
```
