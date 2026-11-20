---
description: Mock ILogger and ILoggerFactory with Mockolate and set up and verify the log calls by level, message, message template, exception, event id and category.
---

# Logging

Mockolate supports mocking `ILogger`, `ILogger<T>` and `ILoggerFactory` from `Microsoft.Extensions.Logging` without any configuration. The logging setups and verifications match every state type, so they work with the `LogInformation(…)` extension methods, whose state type you cannot name in a test.

```csharp
ILogger<ChocolateShop> logger = ILogger<ChocolateShop>.CreateMock();
ChocolateShop sut = new ChocolateShop(logger);

sut.Order("Dark", 0);

await That(logger.Mock.Verify.LogWarning().WithMessage("There is no Dark chocolate to order")).Once();
```

Unlike other members without a setup, `IsEnabled` returns `true`, so logging code that checks `IsEnabled` first (like the code generated for `[LoggerMessage]`) still reaches `Log`. This also applies to mocks created with `ThrowingWhenNotSetup()`, where a `Log` call without a matching setup still throws.

<details>
<summary>Example types used on this page</summary>

```csharp
public class ChocolateShop(ILogger<ChocolateShop> logger)
{
    public void Order(string type, int amount)
    {
        if (amount <= 0)
        {
            logger.LogWarning("There is no {Type} chocolate to order", type);
            return;
        }

        logger.LogInformation("Ordered {Amount} {Type} chocolates", amount, type);
    }
}
```

</details>

## Setup and verification

Setups and verifications share the same members. `Log()` covers every level, `Log(LogLevel)` and `LogTrace()`, `LogDebug()`, `LogInformation()`, `LogWarning()`, `LogError()` and `LogCritical()` one level. Narrow them down with:

| Member                                          | Matches the calls                                                         |
|-------------------------------------------------|---------------------------------------------------------------------------|
| `OrHigher()`                                    | on the level or a higher one.                                             |
| `WithMessage("…")`                              | whose formatted message is exactly the text.                              |
| `WithMessage(It.Matches("*Dark*"))`             | whose formatted message matches the [matcher](../06-parameter-matching.md). |
| `WithMessage(It.MatchesTemplate("…"))`          | with the message template, and optionally properties.                     |
| `WithException<TException>()`                   | with an exception of the type.                                            |
| `WithException(It.Is<Exception?>(…))`           | whose exception matches the matcher.                                      |
| `WithEventId(3)` or `WithEventId(It.Is<EventId>(…))` | with the event id.                                                   |
| `WithCategory("…")` or `WithCategory<T>()`      | of a logger with the category, see [logger factories](#logger-factories). |

The callbacks of a setup receive the level, the event id, the formatted message and the exception:

```csharp
List<string> warnings = [];
logger.Mock.Setup.LogWarning()
    .Do((level, eventId, message, exception) => warnings.Add(message));

logger.Mock.Setup.LogError().WithMessage(It.Matches("*Dark*"))
    .Throws<InvalidOperationException>();
```

When several setups match, the most recently defined one wins, as for any other member.

A verification counts the matching calls:

```csharp
await That(logger.Mock.Verify.LogInformation().WithMessage("Ordered 3 Dark chocolates")).Once();
await That(logger.Mock.Verify.Log(LogLevel.Warning).OrHigher()).Never();
await That(logger.Mock.Verify.LogError().WithException<TimeoutException>()).AtLeastOnce();
```

### Message templates

`It.MatchesTemplate(…)` matches the message template of the call instead of the formatted message, and `WithProperty(…)` checks a property of the template with a value or a matcher. Properties that you do not check can have any value:

```csharp
await That(logger.Mock.Verify.LogInformation()
    .WithMessage(It.MatchesTemplate("Ordered {Amount} {Type} chocolates")
        .WithProperty("Type", "Dark")
        .WithProperty("Amount", It.Satisfies<int>(amount => amount > 0)))).Once();
```

This works for the `LogInformation(…)` extension methods, `LoggerMessage.Define` and `[LoggerMessage]`, which all pass the template and the properties as state.

## Logger factories

A mocked `ILoggerFactory` returns one logger mock per category from `CreateLogger`, unless `CreateLogger` has a setup. This also applies to mocks created with `ThrowingWhenNotSetup()` or with `WithDefaultValueFor<ILogger>(…)`. The loggers use the behavior of the factory, and the factory verifies the log calls of all of them:

```csharp
ILoggerFactory loggerFactory = ILoggerFactory.CreateMock();
ChocolateShop sut = new ChocolateShop(loggerFactory.CreateLogger<ChocolateShop>());

sut.Order("Dark", 0);

await That(loggerFactory.Mock.Verify.LogWarning().WithCategory<ChocolateShop>()).Once();
```

Each logger of the factory has its category name, which `WithCategory(…)` matches; an `ILogger<T>` mock has the category of `T`. A log call that you verify on the factory also counts as verified on its logger for `VerifyThatAllInteractionsAreVerified()`, and the other way round. The calls of `CreateLogger` count as verified on the factory, and `ClearAllInteractions()` on the factory also clears the calls of its loggers.

To set up or verify a single logger, also before the code under test creates it, use `GetLogger<TCategoryName>()` or `GetLogger(categoryName)`. `GetLogger<TCategoryName>()` returns an `ILogger<TCategoryName>` that shares the setups and calls of the logger of the category, so you can also pass it to code that expects an `ILogger<TCategoryName>`:

```csharp
ILogger<ChocolateShop> logger = loggerFactory.Mock.GetLogger<ChocolateShop>();
logger.Mock.Setup.LogWarning()
    .Throws<InvalidOperationException>();
```

:::warning[Get the logger mock through `Mock`]

Only `loggerFactory.Mock.GetLogger(…)` returns the logger mock itself. `loggerFactory.CreateLogger<TCategoryName>()` is an extension method from `Microsoft.Extensions.Logging` that returns a new `Logger<TCategoryName>`, which only forwards to the logger mock, so its `Mock` property throws a `MockException`. A factory mock that wraps a real factory with `Wrapping(…)` returns the loggers of the real factory instead of logger mocks.

:::

<details>
<summary>Setups of a specific state type</summary>

The generic `Setup.Log<TState>(…)` and `Verify.Log<TState>(…)` remain available and only match calls with the given state type. The logging setups and verifications above match the calls of every state type.

</details>
