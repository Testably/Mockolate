using System.Linq;
using System.Reflection;
using Mockolate.Internals;

namespace Mockolate.Internal.Tests.Internals;

public sealed class TypeFormatterTests
{
	[Test]
	public async Task GenericArguments_ShouldFormatToName()
	{
		MethodInfo methodInfo = GetType().GetMethod(nameof(MyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
		Type type = methodInfo.GetGenericArguments().First();

		string result = type.FormatType();

		await That(result).IsEqualTo("T");
	}

	[Test]
	public async Task NestedType_ShouldIncludeDeclaringType()
	{
		Type type = typeof(MyType);

		string result = type.FormatType();

		await That(result).IsEqualTo("TypeFormatterTests.MyType");
	}

	[Test]
	public async Task ShouldFormatVoid()
	{
		MethodInfo methodInfo = GetType().GetMethod(nameof(MyMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
		Type type = methodInfo.ReturnType;

		string result = type.FormatType();

		await That(result).IsEqualTo("void");
	}

	private class MyType;

	// ReSharper disable once UnusedTypeParameter
	private static void MyMethod<T>() { }
}
