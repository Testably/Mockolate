---
description: Mock HttpClient and IHttpClientFactory with Mockolate and match URIs, content and headers.
---

# HttpClient

Mockolate supports mocking `HttpClient` out of the box, with no special configuration required. You can set up, use, and
verify HTTP interactions just like with any other class. The HTTP-specific matchers and `ReturnsAsync` overloads live
in the `Mockolate.Web` namespace (.NET 8+).

```csharp
using Mockolate.Web;

HttpClient httpClient = HttpClient.CreateMock();
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

HttpResponseMessage result = await httpClient.PostAsync("https://testably.org/api/chocolate/dispense",
    new StringContent("""
                      { "type": "Dark", "amount": 3 }
                      """, Encoding.UTF8, "application/json"));

await That(result.IsSuccessStatusCode).IsTrue();
httpClient.Mock.Verify.PostAsync(
    It.IsUri("*testably.org/api/chocolate/dispense*").ForHttps(),
    It.IsHttpContent("application/json").WithStringMatching("*\"type\": \"Dark\"*\"amount\": 3*")).Once();
```

:::info

Under the hood, all setups, requests and verifications are forwarded to the `SendAsync` method of a mocked
`HttpMessageHandler`. You can therefore mix `string` and `Uri` parameters between setup and verification, and a
setup for `GetAsync(string)` also applies to calls of `GetAsync(Uri)`.

:::

## All HTTP Methods

Mockolate supports all standard HTTP methods:

```csharp
// GET
httpClient.Mock.Setup
    .GetAsync(It.IsAny<string>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

// POST
httpClient.Mock.Setup
    .PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

// PUT
httpClient.Mock.Setup
    .PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

// DELETE
httpClient.Mock.Setup
    .DeleteAsync(It.IsAny<string>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

// PATCH
httpClient.Mock.Setup
    .PatchAsync(It.IsAny<string>(), It.IsAny<HttpContent>())
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

For all HTTP methods you can add an optional cancellation token parameter.
If no parameter is provided, it matches any `CancellationToken`:

```csharp
CancellationTokenSource cts = new CancellationTokenSource();
httpClient.Mock.Setup
    .GetAsync(It.IsAny<string>(), It.Is(cts.Token))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

await httpClient.GetAsync("https://example.com", cts.Token);
```

## URI matching

Use `It.IsUri(string?)` to match URIs using a wildcard pattern against the string representation of the URI.
The pattern supports `*` to match zero or more characters and `?` to match a single character.

### Scheme

Filter requests by URI scheme using `.ForHttps()` or `.ForHttp()`:

```csharp
// Match only HTTPS requests
httpClient.Mock.Verify
    .GetAsync(It.IsUri("*testably.org*").ForHttps())
    .Once();

// Match only HTTP requests
httpClient.Mock.Verify
    .GetAsync(It.IsUri("*testably.org*").ForHttp())
    .Never();
```

### Host

Filter requests by host using `.WithHost(string)`. You can provide a wildcard pattern to match against the host name:

```csharp
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithHost("*testably.org*"))
    .Once();
```

### Port

Filter requests on a specific port using `.WithPort(int)`:

```csharp
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithPort(443))
    .Once();
```

### Path

Filter requests by path using `.WithPath(string)`. You can provide a wildcard pattern to match against the path:

```csharp
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithPath("/foo/*"))
    .Once();
```

### Query

Filter requests by query parameters using `.WithQuery(...)`. You can provide one or many key-value pairs or a raw query
string to match against the query parameters. The order of the key-value pairs does not matter:

```csharp
// Match query string containing "x=42"
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithQuery("x", "42"))
    .Once();
// Match query string containing "x=42" and "y=foo" (in any order)
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithQuery(("x", "42"), ("y", "foo")))
    .Once();
// Match query string containing "x=42" and "y=foo" (in any order)
httpClient.Mock.Verify
    .GetAsync(It.IsUri().WithQuery("x=42&y=foo"))
    .Once();
```

## Content Matching

Use `It.IsHttpContent(string?)` to match the HTTP content, optionally providing an expected media type header value:

```csharp
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent("application/json"))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

### String content

To verify against the string content, use the following methods:

- `.WithString(Func<string, bool>)`: to match the string content against the given predicate
- `.WithString(string)`: to match the content exactly as provided
- `.WithStringMatching(string)`: to match the content using wildcard patterns
- `.WithStringMatching(string).AsRegex()`: to match the content using regular expressions

```csharp
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent("application/json").WithStringMatching("*\"type\": \"Dark\"*"))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

Add the `.IgnoringCase()` modifier to make the string matching case-insensitive.

### Binary content

To verify against the binary content, use the following methods:

- `.WithBytes(Func<byte[], bool>)`: to match the binary content against the given predicate
- `.WithBytes(byte[])`: to match the content exactly as provided

```csharp
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent("application/octet-stream").WithBytes([0x01, 0x02, 0x03, ]))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

### Form data content

To verify against the URL-encoded form data content, use the following methods:

- `.WithFormData(string, string)`: checks that the form-data content contains the provided key-value pair
- `.WithFormData(IEnumerable<(string, string)>)`: checks that the form-data content contains the provided key-value
  pairs
- `.WithFormData(string)`: checks that the form-data content contains the provided raw form data string

```csharp
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent("application/x-www-form-urlencoded").WithFormData("my-key", "my-value"))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

As with query parameters, the order of the form-data key-value pairs does not matter. Add the `.Exactly()` modifier to
also check that no other form data is present.

### Header matching

To verify against the HTTP content headers, use the following methods:

- `.WithHeaders(string, HttpHeaderValue)`: checks that the content headers contain the provided key-value pair
- `.WithHeaders(IEnumerable<(string, HttpHeaderValue)>)`: checks that the content headers contain all key-value pairs
- `.WithHeaders(string)`: checks that the content headers contain the provided raw headers

```csharp
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent().WithHeaders(("Content-Type", "application/json"), ("X-My-Header", "my-value")))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
httpClient.Mock.Setup
    .PostAsync(
        It.IsAny<string>(),
        It.IsHttpContent().WithHeaders("""
            Content-Type: application/json
            X-My-Header: my-value
            """))
    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
```

By default, only the content headers are checked, not the headers of the `HttpRequestMessage`. To check both, add the
`.IncludingRequestHeaders()` modifier.

## Return Message

Overloads of `.ReturnsAsync` simplify specifying the return value for HTTP method setups.

```csharp
httpClient.Mock.Setup.GetAsync(It.IsAny<Uri>())
    // Returns a response with status code 200 OK and no content
    .ReturnsAsync(HttpStatusCode.OK);

httpClient.Mock.Setup.GetAsync(It.IsAny<Uri>())
    // Returns a response with status code 200 OK and a string content "some string content"
    .ReturnsAsync(HttpStatusCode.OK, "some string content");

httpClient.Mock.Setup.GetAsync(It.IsAny<Uri>())
    // Returns a response with status code 200 OK and a JSON content {"foo":"bar"}
    .ReturnsAsync(HttpStatusCode.OK, "{\"foo\":\"bar\"}", "application/json");

byte[] bytes = new byte[] { /* ... */ };

httpClient.Mock.Setup.GetAsync(It.IsAny<Uri>())
    // Returns a response with status code 200 OK and a binary content with the provided bytes
    .ReturnsAsync(HttpStatusCode.OK, bytes);

httpClient.Mock.Setup.GetAsync(It.IsAny<Uri>())
    // Returns a response with status code 200 OK and a PNG image content with the provided bytes
    .ReturnsAsync(HttpStatusCode.OK, bytes, "image/png");
```

## Whole-request matching

Use `It.IsHttpRequestMessage(HttpMethod?)` together with `Setup.SendAsync(...)` to match against the entire
`HttpRequestMessage`, optionally restricted to a specific HTTP method. This is the catch-all entry point that the
verb-specific overloads (`GetAsync`, `PostAsync`, …) route through internally - reach for it when you need the full
request (e.g. to inspect headers, or to handle non-standard verbs like `HEAD` or `OPTIONS`).

Chain builders to add constraints:

- `.WhoseUriIs(string)` / `.WhoseUriIs(Action<IUriParameter>)`: same surface as `It.IsUri(...)`.
- `.WhoseContentIs(Action<IHttpContentParameter>)` / `.WhoseContentIs(string mediaType, …)`: same surface as
  `It.IsHttpContent(...)`.
- `.WithHeaders(...)`: require specific request headers.

```csharp
// Match any POST to /api/chocolate/dispense with a JSON body containing "Dark"
httpClient.Mock.Setup
    .SendAsync(It.IsHttpRequestMessage(HttpMethod.Post)
        .WhoseUriIs(uri => uri.WithPath("/api/chocolate/dispense"))
        .WhoseContentIs("application/json", c => c.WithStringMatching("*\"type\": \"Dark\"*")))
    .ReturnsAsync(HttpStatusCode.OK);
```

## Mocking `IHttpClientFactory`

When the code under test pulls clients from `IHttpClientFactory` and wraps them in `using` blocks
(`using var client = _factory.CreateClient("chocolate-api");`), two naive mocking patterns both break:

- Returning **the same mocked `HttpClient` instance** from every `CreateClient` call throws
  `ObjectDisposedException` on the second call, because the first `using` block already disposed it.
- Returning **a freshly created mocked `HttpClient`** from a `Returns(() => ...)` factory avoids the
  disposal exception but spreads invocation history across many short-lived mocks, so verifications
  can no longer see the full call sequence.

The fix is to mock the underlying `HttpMessageHandler` once and hand out fresh `HttpClient` wrappers
that all share it. Because Mockolate intercepts on the handler, the registry (and therefore the
invocation history) lives on the handler and survives any number of wrapper disposals. Pass
`disposeHandler: false` when constructing each wrapper so that disposing the wrapper does not also
dispose the shared handler.

```csharp
// One mocked handler shared by every HttpClient the factory hands out.
HttpMessageHandler handler = HttpMessageHandler.CreateMock();

// Setup goes on any HttpClient wrapping the handler; the shared registry routes the
// configuration to every wrapper that uses the same handler.
HttpClient setupClient = HttpClient.CreateMock(handler, disposeHandler: false);
setupClient.Mock.Setup
    .GetAsync(It.IsUri("*testably.org/api/chocolate/inventory/*").ForHttps())
    .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK));

// The factory returns a fresh HttpClient each call, all sharing the handler.
// `disposeHandler: false` keeps the shared handler alive when consumers `using` the client.
IHttpClientFactory factory = IHttpClientFactory.CreateMock();
factory.Mock.Setup.CreateClient(It.IsAny<string>())
    .Returns(() => HttpClient.CreateMock(handler, disposeHandler: false));

// The code under test can now safely use `using` blocks. Each call goes through the same
// handler, so the invocation history is preserved across every disposed wrapper.
foreach (string type in new[] { "Dark", "Milk", "White" })
{
    using HttpClient client = factory.CreateClient("chocolate-api");
    _ = await client.GetAsync($"https://testably.org/api/chocolate/inventory/{type}");
}

setupClient.Mock.Verify
    .GetAsync(It.IsUri("*testably.org/api/chocolate/inventory/*").ForHttps())
    .Exactly(3);
```

:::warning

`disposeHandler: false` is the critical bit. `HttpClient.Dispose()` disposes its handler by default, which would tear
down the shared mock after the first `using` block.

:::

Setup and verification can target any `HttpClient` wrapping the same handler. The `setupClient` above is just one
convenient handle; the clients handed out by the factory work equally well.
