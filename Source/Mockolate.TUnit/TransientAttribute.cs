using System;

namespace Mockolate.TUnit;

/// <summary>
///     Excludes the test parameter from the arguments that <see cref="AutoMockArgumentsAttribute" /> shares with the
///     classes under test, like a transient service in dependency injection.
/// </summary>
/// <remarks>
///     A constructor dependency of a class under test does not receive the argument of this parameter. When no other
///     test parameter of its type is shared, the class under test gets its own new mock, which the test cannot reach.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class TransientAttribute : Attribute;
