---
description: Set up, raise and verify protected virtual members of class mocks.
---

# Protected members

Class mocks can also set up and verify protected virtual and abstract members. They work like public members, but are
reached through `Mock.SetupProtected`, `Mock.RaiseProtected` and `Mock.VerifyProtected`:

```csharp
public abstract class ChocolateDispenser
{
    protected virtual bool DispenseInternal(string type, int amount) => true;
    protected virtual int InternalStock { get; set; }
    protected virtual int this[int slot] { get => 0; set { } }
}

ChocolateDispenser dispenser = ChocolateDispenser.CreateMock();
```

## Setup

```csharp
// Set up a protected method
dispenser.Mock.SetupProtected.DispenseInternal("Dark", It.IsAny<int>())
    .Returns(true);

// Set up a protected property
dispenser.Mock.SetupProtected.InternalStock.InitializeWith(100);
```

All setup options of [methods](../05-setup/01-methods.md), [properties](../05-setup/02-properties.md) and
[indexers](../05-setup/03-indexers.md) are available.

## Verification

```csharp
// A protected method was called
dispenser.Mock.VerifyProtected.DispenseInternal("Dark", It.IsAny<int>()).Once();

// A protected property was read and set
dispenser.Mock.VerifyProtected.InternalStock.Got().AtLeastOnce();
dispenser.Mock.VerifyProtected.InternalStock.Set(100).Once();

// A protected indexer was read and set
dispenser.Mock.VerifyProtected[0].Got().Once();
dispenser.Mock.VerifyProtected[0].Set(42).Once();
```

All [verification options](../07-verify-interactions.md) work the same as for public members.

## Generic methods for any type argument

`Mock.SetupProtectedGeneric` and `Mock.VerifyProtectedGeneric` set up and verify protected generic methods for any type argument, like `SetupGeneric` and `VerifyGeneric` do for public ones (see [Generic methods](../06-parameter-matching.md#generic-methods)):

```csharp
public abstract class ChocolateRecipe
{
    protected virtual void AddIngredient<T>(T ingredient) { }
}

ChocolateRecipe recipe = ChocolateRecipe.CreateMock();
recipe.Mock.SetupProtectedGeneric.AddIngredient(It.IsAny<string>()).Throws<InvalidOperationException>();

await That(recipe.Mock.VerifyProtectedGeneric.AddIngredient(Match.AnyParameters())).Never();
```
