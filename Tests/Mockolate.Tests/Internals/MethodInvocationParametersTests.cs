using System.Collections.Generic;
using Mockolate.Interactions;

namespace Mockolate.Tests.Internals;

public sealed class MethodInvocationParametersTests
{
	[Test]
	[MethodDataSource(nameof(ProvideInvocations))]
	public async Task GetParameterValueAt_ShouldReturnTheArgumentsAndNullOutsideOfThem(
		IMethodInvocationParameters invocation, object?[] expectedValues)
	{
		object?[] values = new object?[invocation.ParameterCount + 1];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = invocation.GetParameterValueAt(i);
		}

		await That(invocation.ParameterCount).IsEqualTo(expectedValues.Length);
		await That(values).IsEqualTo([..expectedValues, null,])
			.Because("an index after the last parameter has no argument");
	}

	public static IEnumerable<Func<(IMethodInvocationParameters, object?[])>> ProvideInvocations()
	{
		yield return () => (new MethodInvocation("M"), []);
		yield return () => (new MethodInvocation<int>("M", 1), [1,]);
		yield return () => (new MethodInvocation<int, string>("M", 1, "a"), [1, "a",]);
		yield return () => (new MethodInvocation<int, string, bool>("M", 1, "a", true), [1, "a", true,]);
		yield return () => (new MethodInvocation<int, string, bool, char>("M", 1, "a", true, 'c'), [1, "a", true, 'c',]);
	}
}
