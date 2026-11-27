using System;

namespace Mockolate.Integrations
{
	/// <summary>
	///     The marker that the <c>[ValueFrom]</c> attribute of the generated test framework integrations derives from,
	///     which this project does not get.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public abstract class ValueFromParameterAttribute(string memberName) : Attribute
	{
		public string MemberName { get; } = memberName;
	}
}

namespace Mockolate.Tests.GeneratorCoverage
{
	/// <summary>
	///     A <c>[ValueFrom]</c> attribute like the one of the test framework integrations.
	/// </summary>
	public sealed class MockDataValueFromAttribute(string memberName)
		: Mockolate.Integrations.ValueFromParameterAttribute(memberName);
}
