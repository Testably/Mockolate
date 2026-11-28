using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mockolate.Internals;
using Mockolate.Setup;

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

	[Test]
	[MethodDataSource(nameof(ProvideTestData))]
	public async Task FormattedType_ShouldBeExpectedValue(string formattedType, string expectedValue)
		=> await That(formattedType).IsEqualTo(expectedValue);

	public static IEnumerable<(string, string)> ProvideTestData()
	{
		MockRegistry registry = new(MockBehavior.Default, new MockInteractions(0));

		yield return (new PropertySetup<int>(registry, "Foo").ToString(), "int Foo");
		yield return (new PropertySetup<uint>(registry, "Foo").ToString(), "uint Foo");
		yield return (new PropertySetup<nint>(registry, "Foo").ToString(), "nint Foo");
		yield return (new PropertySetup<nuint>(registry, "Foo").ToString(), "nuint Foo");
		yield return (new PropertySetup<byte>(registry, "Foo").ToString(), "byte Foo");
		yield return (new PropertySetup<sbyte>(registry, "Foo").ToString(), "sbyte Foo");
		yield return (new PropertySetup<short>(registry, "Foo").ToString(), "short Foo");
		yield return (new PropertySetup<ushort>(registry, "Foo").ToString(), "ushort Foo");
		yield return (new PropertySetup<long>(registry, "Foo").ToString(), "long Foo");
		yield return (new PropertySetup<ulong>(registry, "Foo").ToString(), "ulong Foo");
		yield return (new PropertySetup<float>(registry, "Foo").ToString(), "float Foo");
		yield return (new PropertySetup<double>(registry, "Foo").ToString(), "double Foo");
		yield return (new PropertySetup<decimal>(registry, "Foo").ToString(), "decimal Foo");
		yield return (new PropertySetup<object>(registry, "Foo").ToString(), "object Foo");
		yield return (new PropertySetup<bool>(registry, "Foo").ToString(), "bool Foo");
		yield return (new PropertySetup<bool?>(registry, "Foo").ToString(), "bool? Foo");
		yield return (new PropertySetup<char>(registry, "Foo").ToString(), "char Foo");
		yield return (new PropertySetup<string>(registry, "Foo").ToString(), "string Foo");
		yield return (new PropertySetup<int[]>(registry, "Foo").ToString(), "int[] Foo");
		yield return (new PropertySetup<DateTime?>(registry, "Foo").ToString(), "DateTime? Foo");
		yield return (new PropertySetup<StreamReader>(registry, "Foo").ToString(), "StreamReader Foo");
		yield return (new PropertySetup<List<string>>(registry, "Foo").ToString(), "List<string> Foo");
		yield return (new PropertySetup<List<int?[]>>(registry, "Foo").ToString(), "List<int?[]> Foo");
		yield return (new PropertySetup<Dictionary<int, string>>(registry, "Foo").ToString(), "Dictionary<int, string> Foo");
	}

	private class MyType;

	// ReSharper disable once UnusedTypeParameter
	private class MyGeneric<T>;

	// ReSharper disable once UnusedTypeParameter
	private static void MyMethod<T>() { }
}
