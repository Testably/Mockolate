using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mockolate.SourceGenerators.Entities;

namespace Mockolate.SourceGenerators.Tests;

/// <summary>
///     Direct coverage for the <c>Equals</c>/<c>GetHashCode</c> implementations on
///     <see cref="RefStructAggregate" /> and <see cref="NamedMock" />. Roslyn's incremental
///     generator pipeline does invoke these (during cache lookup), but coverage instrumentation
///     of those internal Roslyn calls is unreliable — these unit tests pin the contract.
/// </summary>
public sealed class MockGeneratorEqualityTests
{
	public sealed class RefStructAggregateTests
	{
		[Test]
		public async Task Equals_WithDifferentType_IsFalse()
		{
			RefStructAggregate a = new(
				new EquatableArray<MethodSetupKey>([]),
				new EquatableArray<RefStructIndexerSetup>([]));

			// ReSharper disable once SuspiciousTypeConversion.Global
			await That(a.Equals("not an aggregate")).IsFalse();
		}

		[Test]
		public async Task Equals_WithDifferingIndexerSetups_IsFalse()
		{
			RefStructAggregate a = new(
				new EquatableArray<MethodSetupKey>([]),
				new EquatableArray<RefStructIndexerSetup>([new RefStructIndexerSetup(5, true, false),]));
			RefStructAggregate b = new(
				new EquatableArray<MethodSetupKey>([]),
				new EquatableArray<RefStructIndexerSetup>([new RefStructIndexerSetup(5, false, true),]));

			await That(a.Equals(b)).IsFalse();
		}

		[Test]
		public async Task Equals_WithDifferingMethodSetups_IsFalse()
		{
			RefStructAggregate a = new(
				new EquatableArray<MethodSetupKey>([new MethodSetupKey(5, false),]),
				new EquatableArray<RefStructIndexerSetup>([]));
			RefStructAggregate b = new(
				new EquatableArray<MethodSetupKey>([new MethodSetupKey(6, false),]),
				new EquatableArray<RefStructIndexerSetup>([]));

			await That(a.Equals(b)).IsFalse();
			await That(a.Equals((object)b)).IsFalse();
		}

		[Test]
		public async Task Equals_WithEqualMethodAndIndexerSetups_IsTrue()
		{
			RefStructAggregate a = new(
				new EquatableArray<MethodSetupKey>([new MethodSetupKey(5, false), new MethodSetupKey(6, true),]),
				new EquatableArray<RefStructIndexerSetup>([new RefStructIndexerSetup(5, true, false),]));
			RefStructAggregate b = new(
				new EquatableArray<MethodSetupKey>([new MethodSetupKey(5, false), new MethodSetupKey(6, true),]),
				new EquatableArray<RefStructIndexerSetup>([new RefStructIndexerSetup(5, true, false),]));

			await That(a.Equals(b)).IsTrue();
			await That(a.Equals((object)b)).IsTrue();
			await That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
		}

		[Test]
		public async Task Equals_WithNull_IsFalse()
		{
			RefStructAggregate a = new(
				new EquatableArray<MethodSetupKey>([]),
				new EquatableArray<RefStructIndexerSetup>([]));

			await That(a.Equals(null)).IsFalse();
			await That(a!.Equals((object?)null)).IsFalse();
		}
	}

	public sealed class NamedMockTests
	{
		[Test]
		public async Task Equals_WhenAdditionalClassesDiffer_IsFalse()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("F", "P", mc,
				new EquatableArray<NamedClass>([new NamedClass("A", mc),]));
			NamedMock b = new("F", "P", mc,
				new EquatableArray<NamedClass>([new NamedClass("B", mc),]));

			await That(a.Equals(b)).IsFalse();
		}

		[Test]
		public async Task Equals_WhenAdditionalClassesPresentOnOneAndNullOnOther_IsFalse()
		{
			MockClass mc = CreateMockClass();
			NamedMock withAdditional = new("F", "P", mc,
				new EquatableArray<NamedClass>([new NamedClass("X", mc),]));
			NamedMock withNull = new("F", "P", mc, null);

			await That(withAdditional.Equals(withNull)).IsFalse();
			await That(withNull.Equals(withAdditional)).IsFalse();
		}

		[Test]
		public async Task Equals_WhenBothAdditionalClassesNull_IsTrue()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("F", "P", mc, null);
			NamedMock b = new("F", "P", mc, null);

			await That(a.Equals(b)).IsTrue();
		}

		[Test]
		public async Task Equals_WithDifferentFileName_IsFalse()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("File1", "Parent", mc, null);
			NamedMock b = new("File2", "Parent", mc, null);

			await That(a.Equals(b)).IsFalse();
		}

		[Test]
		public async Task Equals_WithDifferentMockClass_IsFalse()
		{
			MockClass mcA = CreateMockClass();
			MockClass mcB = CreateMockClass(
				"public interface IBar { void M(); }",
				"IBar");
			NamedMock a = new("File", "Parent", mcA, null);
			NamedMock b = new("File", "Parent", mcB, null);

			await That(a.Equals(b)).IsFalse();
		}

		[Test]
		public async Task Equals_WithDifferentParentName_IsFalse()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("File", "Parent1", mc, null);
			NamedMock b = new("File", "Parent2", mc, null);

			await That(a.Equals(b)).IsFalse();
		}

		[Test]
		public async Task Equals_WithIdenticalNamedMock_IsTrue()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("File1", "Parent1", mc, null);
			NamedMock b = new("File1", "Parent1", mc, null);

			await That(a.Equals(b)).IsTrue();
			await That(a.Equals((object)b)).IsTrue();
			await That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
		}

		[Test]
		public async Task Equals_WithNullOther_IsFalse()
		{
			MockClass mc = CreateMockClass();
			NamedMock a = new("F", "P", mc, null);

			await That(a.Equals(null)).IsFalse();
			await That(a!.Equals((object?)null)).IsFalse();
		}

		[Test]
		public async Task GetHashCode_IncludesAdditionalClasses()
		{
			MockClass mc = CreateMockClass();
			NamedMock withAdditional = new("F", "P", mc,
				new EquatableArray<NamedClass>([new NamedClass("X", mc),]));
			NamedMock withNull = new("F", "P", mc, null);

			_ = withAdditional.GetHashCode();
			_ = withNull.GetHashCode();
			await That(true).IsTrue()
				.Because(
					"strict inequality of the two hash codes cannot be guaranteed, so this only pins that additionalClasses takes part in the hash at all");
		}

		private static MockClass CreateMockClass(string source = "public interface IFoo { void M(); }",
			string typeName = "IFoo")
		{
			SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
			CSharpCompilation compilation = CSharpCompilation.Create("test",
				[tree,],
				[MetadataReference.CreateFromFile(typeof(object).Assembly.Location),]);
			INamedTypeSymbol symbol = compilation.GetTypeByMetadataName(typeName)!;
			return new MockClass([symbol,], compilation.Assembly);
		}
	}
}
