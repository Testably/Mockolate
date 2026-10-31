---
description: Mock static abstract and static virtual interface members with Mockolate.
---

# Static interface members

:::info[.NET 8 or later]

Static interface members can be mocked on .NET 8 or later.

:::

Mockolate supports mocking `static abstract` and `static virtual` members of interfaces. They are set up, raised and verified like instance members, but through `Mock.SetupStatic`, `Mock.RaiseStatic` and `Mock.VerifyStatic`.

C# only allows calls to static abstract members through a generic type parameter that is constrained to the
interface, so code that uses them typically looks like this:

```csharp
public interface IChocolateFactory
{
    static abstract string DefaultRecipe { get; set; }
    static abstract int ProduceBatch(string type, int amount);
    static abstract event Action<int> BatchCompleted;
}

public static class ChocolateProduction
{
    public static string ReadRecipe<TFactory>() where TFactory : IChocolateFactory
        => TFactory.DefaultRecipe;

    public static int Produce<TFactory>(string type, int amount) where TFactory : IChocolateFactory
        => TFactory.ProduceBatch(type, amount);

    public static void Subscribe<TFactory>(Action<int> handler) where TFactory : IChocolateFactory
        => TFactory.BatchCompleted += handler;
}
```

In the test, create the mock as usual and pass the generated mock type `Mock.IChocolateFactory` as the type argument:

```csharp
IChocolateFactory factory = IChocolateFactory.CreateMock();

// Set up static members
factory.Mock.SetupStatic.ProduceBatch("Dark", It.IsAny<int>()).Returns(42);
factory.Mock.SetupStatic.DefaultRecipe.Returns("Dark");

// Call them through the generated mock type
string recipe = ChocolateProduction.ReadRecipe<Mock.IChocolateFactory>();
int produced = ChocolateProduction.Produce<Mock.IChocolateFactory>("Dark", 10);
ChocolateProduction.Subscribe<Mock.IChocolateFactory>(amount => Console.WriteLine($"Batch of {amount} ready"));

await That(recipe).IsEqualTo("Dark");
await That(produced).IsEqualTo(42);

// Raise static events
factory.Mock.RaiseStatic.BatchCompleted(produced);

// Verify static interactions
factory.Mock.VerifyStatic.ProduceBatch("Dark", It.IsAny<int>()).Once();
factory.Mock.VerifyStatic.DefaultRecipe.Got().Once();
factory.Mock.VerifyStatic.BatchCompleted.Subscribed().Once();
```

## Isolation

Static calls are routed to the mock that was **created last in the current async flow** (internally an
`AsyncLocal`). Tests that run in parallel therefore don't interfere with each other.

:::warning

Within a single test, create at most one mock per interface with static members. When you create a second mock of the
same interface, all static calls are routed to the second mock.

:::
