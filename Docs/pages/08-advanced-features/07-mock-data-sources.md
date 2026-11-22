---
description: Write a test framework attribute that fills test parameters with Mockolate mocks and classes under test, without reflection.
---

# Mock data sources

A mock data source is an attribute of a test framework that fills test parameters with mocks and with classes whose constructor dependencies are mocks. Mockolate provides the building blocks for such attributes. Use this page when you write the integration for a test framework; to use an existing integration, see its documentation.

Mark the attribute with the `IMockDataSource` interface. For every method, constructor or type with such an attribute, the source generator emits the code that creates the parameter types. At runtime, the attribute calls `MockDataSource.TryCreate` for each parameter:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class MockArgumentsAttribute : UntypedDataSourceGeneratorAttribute, IMockDataSource
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        Type[] types = metadata.MembersToGenerate.OfType<ParameterMetadata>()
            .Select(parameter => parameter.Type)
            .ToArray();
        // The generated code lives in the assembly that declares the test method, which differs from the
        // assembly of the test class for an inherited test.
        Assembly assembly = metadata.MembersToGenerate.OfType<ParameterMetadata>().FirstOrDefault()
            ?.ReflectionInfo.Member.DeclaringType?.Assembly ?? metadata.TestInformation!.Type.Assembly;
        yield return () => Array.ConvertAll(types, type =>
            MockDataSource.TryCreate(assembly, type, null, NoArgument, out object? value)
                ? value
                : throw new InvalidOperationException($"Mockolate cannot create a {type}."));
    }

    private static bool NoArgument(Type type, string parameterName, out object? value)
    {
        value = null;
        return false;
    }
}
```

The attribute then fills the parameters of a [TUnit](https://tunit.dev/) test:

```csharp
public class VendingMachine(IChocolateDispenser dispenser)
{
    public string Buy(string type, int amount)
        => dispenser.Dispense(type, amount) ? "Enjoy!" : "Sold out";
}

public class VendingMachineTests
{
    [Test]
    [MockArguments]
    public async Task Buy_WhenChocolateIsSoldOut_ShouldSaySo(VendingMachine sut)
    {
        string result = sut.Buy("Dark", 4);

        await That(result).IsEqualTo("Sold out");
    }
}
```

`TryCreate` returns `false` for a type that the generated code of the assembly cannot create. Pass the assembly that declares the member with the attribute, because the generated code lives there. The `behavior` applies to every created mock and to the default values; `null` stands for `MockBehavior.Default`. Every call creates new mocks and new instances.

## What is created

- An **interface, delegate or abstract class** gets a new mock, as if the test called `CreateMock` on it. An abstract class needs an accessible constructor without parameters, because the mock calls it.
- A **concrete class** gets an instance, created like by [`CreateWithMocks`](../04-create-mocks.md#create-the-class-under-test-with-mocks), see [constructor dependencies](#constructor-dependencies).
- **Types of the `System`, `Microsoft` and `Windows` namespaces** are often the arguments that the test framework passes. They get a value from the default values of the behavior; an interface that Mockolate can mock without further checks, such as `IServiceProvider`, gets a new mock when the default values provide none. No framework class is created.
- **Value types, strings and other types** get a value from the default values of the behavior, the same value that an un-configured mock member returns: `""` for a `string`, an empty collection, the default of a value type. `TryCreate` returns `false` when the default values provide no value of a reference type.

The [Mockolate0010](../10-analyzers.md#mockolate0010) analyzer reports a parameter whose type asks for a mock or an instance, but that Mockolate cannot create. A [default value generator](../04-create-mocks.md#default-values) of the behavior, for example one backed by a library like AutoFixture, can still provide a value for it.

## Constructor dependencies

A created class gets an argument for every constructor parameter, in this order:

1. The value of the `resolve` callback, see [supply constructor arguments](#supply-constructor-arguments).
2. A new mock for an interface outside the framework namespaces, by the rules of `CreateWithMocks`, and for `ILogger`, `ILogger<T>` and `ILoggerFactory`.
3. An empty collection for a `params` parameter, and the declared default value for an optional parameter.
4. A new instance, created the same way, for a concrete class outside the framework namespaces.
5. `null` for a nullable parameter.
6. A value from the default values of the behavior, and for a framework interface such as `IServiceProvider` a new mock when there is none.

A dependency is mocked by narrower rules than a parameter: a delegate or an abstract class as a constructor parameter gets no mock, because no `CreateMock` call and no analyzer check its mock. When none of the steps provides an argument, the creation fails with a `MockException` that names the parameter.

## Supply constructor arguments

The `resolve` callback is asked for every constructor parameter of a created class, also of the classes created as constructor dependencies. Return `true` to pass your `value`, which may be `null`, instead of a new mock, instance or default value. This lets the class under test share the mocks that the test receives as other parameters:

```csharp
IChocolateDispenser dispenser = IChocolateDispenser.CreateMock();

MockDataSource.TryCreate(typeof(VendingMachine).Assembly, typeof(VendingMachine), null, Resolve, out object? sut);

bool Resolve(Type type, string parameterName, out object? value)
{
    value = dispenser;
    return type == typeof(IChocolateDispenser);
}
```

A value that is not of the parameter type fails with a `MockException` that names the parameter.

<details>
<summary>Details and limitations</summary>

- A constructor dependency that depends on a class that is being created is not created again. It gets `null` when it is nullable, and otherwise a value from the default values of the behavior or a `MockException`.
- A nullable-oblivious parameter (in code without nullable annotations) does not receive `null`, but a value from the default values of the behavior.
- Parameters of a type that contains a type parameter, such as `IRepository<T>` in a generic test method or class, cannot be created.
- The attribute must be declared on the method, the constructor or the type itself; an attribute on a base class is not inherited. A `[method: …]` attribute on a type with a primary constructor applies to that constructor.
- The generator creates a mock for every interface parameter of a member with the attribute, also for a framework interface that the test framework passes as an argument.
- The generated code registers itself from a module initializer. On .NET Framework and .NET Standard, Mockolate declares the `ModuleInitializerAttribute` when the compilation lacks it. If another generator such as PolySharp declares it too, set `<MockolateUnionAttributePolyfills>false</MockolateUnionAttributePolyfills>`, or add `ModuleInitializerAttribute` to the list of attributes in this property.
- The generated code uses no reflection, so mock data sources work with trimming and Native AOT.

</details>
