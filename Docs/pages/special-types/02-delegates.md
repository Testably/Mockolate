---
description: Mock Action, Func and custom delegates with Mockolate.
---

# Delegates

Mockolate supports mocking delegates, including `Action`, `Func<T>` and custom delegates. Because a delegate has a
single method, `Mock.Setup(…)` and `Mock.Verify(…)` are methods that take the delegate parameters directly.

## Setup

```csharp
// Mock an Action delegate
Action myAction = Action.CreateMock();
myAction.Mock.Setup().Do(() => Console.WriteLine("Action invoked!"));

// Mock a Func<T> delegate
Func<int> myFunc = Func<int>.CreateMock();
myFunc.Mock.Setup().Returns(42);
```

For custom delegates with parameters, pass a value or a [parameter matcher](../06-parameter-matching.md) for each
parameter, including `ref` and `out` parameters:

```csharp
public delegate int Calculate(int x, string operation);
public delegate void ProcessData(int input, ref int value, out int result);

Calculate calculator = Calculate.CreateMock();
calculator.Mock.Setup(It.IsAny<int>(), "add")
    .Returns((x, operation) => x + 10);

ProcessData processor = ProcessData.CreateMock();
processor.Mock.Setup(It.IsAny<int>(), It.IsRef<int>(v => v + 1), It.IsOut(() => 100));
```

The setup supports the same options as a [method setup](../setup/01-methods.md): `.Returns(…)`, `.Throws(…)`,
`.Do(…)`, sequences and the [advanced callback features](../advanced-features/03-advanced-callback-features.md).

## Verification

```csharp
// The Action was invoked at least once
myAction.Invoke();
myAction.Mock.Verify().AtLeastOnce();

// The Func<T> was invoked exactly once
_ = myFunc();
myFunc.Mock.Verify().Once();

// The custom delegate was invoked with "add"
_ = calculator(5, "add");
calculator.Mock.Verify(It.IsAny<int>(), "add").Once();

// Use It.IsRef<T>() and It.IsOut<T>() to match any ref or out parameter
int value = 0;
processor(1, ref value, out int result);
processor.Mock.Verify(It.IsAny<int>(), It.IsRef<int>(), It.IsOut<int>()).Once();
```
