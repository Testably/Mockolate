using System.Collections.Generic;
using System.IO;
using Mockolate.Setup;

namespace Mockolate.Tests.Internals;

public sealed class TypeFormatterTests
{
	[Test]
	[MethodDataSource(nameof(ProvideTestData))]
	public async Task FormattedType_ShouldBeExpectedValue(string formattedType, string expectedValue)
		=> await That(formattedType).IsEqualTo(expectedValue);

	public static IEnumerable<(string, string)> ProvideTestData()
	{
		MockRegistry registry = new(MockBehavior.Default, new FastMockInteractions(0));

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
}
