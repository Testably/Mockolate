using System;

namespace Mockolate.Integrations
{
	/// <summary>
	///     The marker that the <c>[Deferred]</c> attribute of the generated test framework integrations derives from,
	///     which this project does not get.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public abstract class DeferredParameterAttribute : Attribute;
}

namespace Mockolate.Tests.GeneratorCoverage
{
	/// <summary>
	///     A <c>[Deferred]</c> attribute like the one of the test framework integrations.
	/// </summary>
	public sealed class MockDataDeferredAttribute : Mockolate.Integrations.DeferredParameterAttribute;
}
