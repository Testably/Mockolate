---
description: Raise events on a Mockolate mock and react to event subscriptions.
---

# Events

## Raise events

Use the `Raise` member to trigger events declared on the mocked interface or class. The generated method has the
same parameters as the event delegate:

```csharp
int dispensedAmount = 0;
dispenser.ChocolateDispensed += (type, amount) => dispensedAmount += amount;

dispenser.Mock.Raise.ChocolateDispensed("Dark", 3);
dispenser.Mock.Raise.ChocolateDispensed("Milk", 2);

await That(dispensedAmount).IsEqualTo(5);
```

Only the handlers that are subscribed at the time the event is raised are called. This lets you test that your code
subscribes to and unsubscribes from events correctly.

## React to subscriptions

Use `OnSubscribed` and `OnUnsubscribed` to run a callback when a handler is added to or removed from an event. The
callback can be parameterless or receive the handler's target and method:

```csharp
dispenser.Mock.Setup.ChocolateDispensed.OnSubscribed
    .Do(() => Console.WriteLine("Someone subscribed to ChocolateDispensed"));

dispenser.Mock.Setup.ChocolateDispensed.OnUnsubscribed
    .Do((target, method) => Console.WriteLine($"{method.Name} unsubscribed"));
```

## Verify subscriptions

To check that your code subscribed to an event, see [Verify interactions](../07-verify-interactions.md#events):

```csharp
dispenser.Mock.Verify.ChocolateDispensed.Subscribed().Once();
```
