---
description: Mock ILogger and ILoggerFactory with Mockolate, match the formatted log messages and check the log records with aweXpect.Logging.
---

import PackageInstall from '@site/src/components/PackageInstall';

# Logging

Mockolate supports mocking `ILogger`, `ILogger<T>` and `ILoggerFactory` from `Microsoft.Extensions.Logging` without any configuration. The logging setups and verifications match the formatted message, so they work with the `LogInformation(…)` extension methods, whose state type you cannot name in a test.

```csharp
ILogger<ChocolateShop> logger = ILogger<ChocolateShop>.CreateMock();
ChocolateShop sut = new ChocolateShop(logger);

sut.Order("Dark", 0);

logger.Mock.Verify.LogWarning(It.Matches("*no Dark chocolate*")).Once();
```

Unlike other members without a setup, `IsEnabled` returns `true`, so logging code that checks `IsEnabled` first (like the code generated for `[LoggerMessage]`) still reaches `Log`.

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

## Setup

`LogTrace`, `LogDebug`, `LogInformation`, `LogWarning`, `LogError` and `LogCritical` set up the calls on one level with a matching formatted message. `Log` takes a matcher for the level and optionally one for the exception. The callbacks receive the level, the event id, the formatted message and the exception:

```csharp
List<string> warnings = [];
logger.Mock.Setup.LogWarning(It.IsAny<string>())
    .Do((level, eventId, message, exception) => warnings.Add(message));

logger.Mock.Setup.Log(It.IsAny<LogLevel>(), It.Matches("*Dark*"), It.IsAny<Exception?>())
    .Throws<InvalidOperationException>();
```

When several setups match, the most recently defined one wins, as for any other member.

## Verification

```csharp
logger.Mock.Verify.LogInformation(It.Matches("Ordered * chocolates")).Once();
logger.Mock.Verify.Log(It.Is(LogLevel.Error), It.IsAny<string>()).Never();
logger.Mock.Verify.Log(It.IsAny<LogLevel>(), It.IsAny<string>(),
    It.Satisfies<Exception?>(exception => exception is TimeoutException)).AtLeastOnce();
```

## Logger factories

A mocked `ILoggerFactory` returns one logger mock per category from `CreateLogger`, unless `CreateLogger` has a setup. The loggers use the behavior of the factory, and the factory verifies the log calls of all of them:

```csharp
ILoggerFactory loggerFactory = ILoggerFactory.CreateMock();
ChocolateShop sut = new ChocolateShop(loggerFactory.CreateLogger<ChocolateShop>());

sut.Order("Dark", 0);

loggerFactory.Mock.Verify.LogWarning(It.IsAny<string>()).Once();
```

To set up or verify a single logger, also before the code under test creates it, use `GetLogger<TCategoryName>()` or `GetLogger(categoryName)`. `GetLogger<TCategoryName>()` returns an `ILogger<TCategoryName>` that shares the setups and calls of the logger of the category, so you can also pass it to code that expects an `ILogger<TCategoryName>`:

```csharp
ILogger<ChocolateShop> logger = loggerFactory.Mock.GetLogger<ChocolateShop>();
logger.Mock.Setup.LogWarning(It.IsAny<string>())
    .Throws<InvalidOperationException>();
```

## Log records with FakeLogger

When the test project references `Microsoft.Extensions.Diagnostics.Testing`, logger mocks forward every call to a `FakeLogger`, and `logger.Mock.LogCollector` returns its `FakeLogCollector`. The `LogCollector` of a factory collects the records of all its loggers. Check the records with [aweXpect.Logging](/aweXpect/extensions/aweXpect.Logging):

<PackageInstall package="Microsoft.Extensions.Diagnostics.Testing" />

```csharp
ILogger<ChocolateShop> logger = ILogger<ChocolateShop>.CreateMock();
ChocolateShop sut = new ChocolateShop(logger);

sut.Order("Dark", 0);

await That(logger.Mock.LogCollector).LoggedWarning().WithMessage("*no Dark chocolate*").AsWildcard();
```

Setups still apply: a call is forwarded to the `FakeLogger` first and then runs the callbacks of the matching setup, so a call whose setup throws is in the collector as well. Mocks created with `ThrowingWhenNotSetup()` do not forward, and their `LogCollector` throws a `MockException`.

<details>
<summary>Setups of a specific state type</summary>

The generic `Setup.Log<TState>(…)` and `Verify.Log<TState>(…)` remain available and only match calls with the given state type. The logging setups and verifications above match the calls of every state type.

</details>
