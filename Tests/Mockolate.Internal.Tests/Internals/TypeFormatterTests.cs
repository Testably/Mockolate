using System.Collections.Generic;
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

	[Test]
	[Arguments(typeof(string))]
	[Arguments(typeof(int?))]
	[Arguments(typeof(int[]))]
	[Arguments(typeof(List<string>[]))]
	[Arguments(typeof(Dictionary<string, List<int?>>))]
	[Arguments(typeof((int, string)))]
	[Arguments(typeof(MyType))]
	[Arguments(typeof(MyGeneric<MyType>))]
	public async Task FormatTypeNames_ShouldMatchFormatType(Type type)
	{
		string result = TypeFormatter.FormatTypeNames(type.ToString());

		await That(result).IsEqualTo(type.FormatType());
	}

	private class MyType;

	// ReSharper disable once UnusedTypeParameter
	private class MyGeneric<T>;

	// ReSharper disable once UnusedTypeParameter
	private static void MyMethod<T>() { }
}
