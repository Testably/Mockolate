using System.Collections.Generic;
using Mockolate.Exceptions;
using Mockolate.Setup;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	[Test]
	public async Task MultipleValues_ShouldAllStoreValues()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		sut[1] = "a";
		sut[2] = "b";

		string result1A = sut[1];
		string result2A = sut[2];

		sut[1] = "x";
		sut[2] = "y";

		string result1B = sut[1];
		string result2B = sut[2];

		await That(result1A).IsEqualTo("a");
		await That(result2A).IsEqualTo("b");
		await That(result1B).IsEqualTo("x");
		await That(result2B).IsEqualTo("y");
	}

	[Test]
	public async Task OverlappingSetups_ShouldUseLatestMatchingSetup()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.IsAny<int>()].InitializeWith("foo");
		sut.Mock.Setup[It.Is(2)].InitializeWith("bar");

		string result1 = sut[1];
		string result2 = sut[2];
		string result3 = sut[3];

		await That(result1).IsEqualTo("foo");
		await That(result2).IsEqualTo("bar");
		await That(result3).IsEqualTo("foo");
	}

	[Test]
	public async Task OverlappingSetups_WhenGeneralSetupIsLater_ShouldOnlyUseGeneralSetup()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.Is(2)].InitializeWith("bar");
		sut.Mock.Setup[It.IsAny<int>()].InitializeWith("foo");

		string result1 = sut[1];
		string result2 = sut[2];
		string result3 = sut[3];

		await That(result1).IsEqualTo("foo");
		await That(result2).IsEqualTo("foo");
		await That(result3).IsEqualTo("foo");
	}

	[Test]
	public async Task Parameter_Do_ShouldExecuteCallback()
	{
		List<string> capturedValues = [];
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.IsAny<string>().Do(v => capturedValues.Add(v)), It.Is(1), It.Is(2)]
			.InitializeWith(42);

		_ = sut["foo", 1, 2];
		_ = sut["bar", 1, 2];

		await That(capturedValues).IsEqualTo(["foo", "bar",]);
	}

	[Test]
	public async Task Parameter_Do_ShouldOnlyExecuteCallbackWhenAllParametersMatch()
	{
		List<string> capturedValues = [];
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.IsAny<string>().Do(v => capturedValues.Add(v)), It.Is(1), It.Is(2)]
			.InitializeWith(42);

		_ = sut["foo", 1, 2];
		_ = sut["bar", 2, 2];

		await That(capturedValues).IsEqualTo(["foo",]);
	}

	[Test]
	public async Task SetOnDifferentLevel_ShouldNotBeUsed()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		sut[1] = "foo";
		string result1 = sut[1, 2];
		string result2 = sut[2, 1];

		await That(result1).IsEmpty();
		await That(result2).IsEmpty();
	}

	[Test]
	public async Task ShouldSupportNullAsParameter()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		sut[null, 2, 1] = 42;
		int? result1 = sut[null, 2, 1];
		int? result2 = sut["", 0, 2];

		await That(result1).IsEqualTo(42);
		await That(result2).IsNull();
	}

	[Test]
	public async Task ShouldUseInitializedValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.Is(2)].InitializeWith("foo");

		string result1 = sut[2];
		string result2 = sut[3];

		await That(result1).IsEqualTo("foo");
		await That(result2).IsEmpty();
	}

	[Test]
	public async Task StoredNullValue_ShouldNotFallBackToDefault()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		string resultBefore = sut[1];
		sut[1] = null!;
		string resultAfter = sut[1];

		await That(resultBefore).IsNotNull();
		await That(resultAfter).IsNull();
	}

	[Test]
	public async Task ThreeLevels_ShouldUseInitializedValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.Is("foo"), It.Is(1), It.Is(2)].InitializeWith(42);

		int? result1 = sut["foo", 1, 2];
		int? result2 = sut["bar", 1, 2];

		await That(result1).IsEqualTo(42);
		await That(result2).IsNull();
	}

	[Test]
	public async Task ThreeLevels_WithoutSetup_ShouldStoreLastValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		int? result0 = sut["foo", 1, 2];
		sut["foo", 1, 2] = 42;
		int? result1 = sut["foo", 1, 2];
		int? result2 = sut["bar", 1, 2];
		int? result3 = sut["foo", 2, 2];
		int? result4 = sut["foo", 1, 3];

		await That(result0).IsNull();
		await That(result1).IsEqualTo(42);
		await That(result2).IsNull();
		await That(result3).IsNull();
		await That(result4).IsNull();
	}

	[Test]
	public async Task TwoLevels_ShouldUseInitializedValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.Is(2), It.Is(3)].InitializeWith("foo");

		string result1 = sut[2, 3];
		string result2 = sut[1, 4];

		await That(result1).IsEqualTo("foo");
		await That(result2).IsEmpty();
	}

	[Test]
	public async Task TwoLevels_WithoutSetup_ShouldStoreLastValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		string result0 = sut[1, 2];
		sut[1, 2] = "foo";
		string result1 = sut[1, 2];
		string result2 = sut[2, 2];

		await That(result0).IsEmpty();
		await That(result1).IsEqualTo("foo");
		await That(result2).IsEmpty();
	}

	[Test]
	public async Task WhenTypeOfGetIndexerDoesNotMatch_ShouldReturnDefaultValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();
		sut.Mock.Setup[It.IsAny<int>()].Returns("foo");
		MockRegistry registry = ((IMock)sut).MockRegistry;

		IndexerSetup? stringSetup = registry.GetIndexerSetup<IndexerSetup>(s => true);
		IndexerGetterAccess<int> access1 = new(1);
		IndexerGetterAccess<int> access2 = new(1);
		string result1 = registry.ApplyIndexerGetter<string>(access1, stringSetup, () => "", 0);
		// Use a different signature index for the int-typed access: each per-signature storage slot
		// is bound to a single TValue on first access (it is an IndexerValueStorage<TValue>), so
		// reusing index 0 with TValue=int would now throw an InvalidOperationException.
		int result2 = registry.ApplyIndexerGetter(access2, stringSetup, () => 0, 100);

		await That(result1).IsEqualTo("foo");
		await That(result2).IsEqualTo(0);
	}

	[Test]
	public async Task WithoutSetup_ShouldStoreLastValue()
	{
		IIndexerService sut = IIndexerService.CreateMock();

		string result0 = sut[1];
		sut[1] = "foo";
		string result1 = sut[1];
		string result2 = sut[2];

		await That(result0).IsEmpty();
		await That(result1).IsEqualTo("foo");
		await That(result2).IsEmpty();
	}

	[Test]
	public async Task WithoutSetup_ThrowWhenNotSetup_ShouldThrowMockNotSetupException()
	{
		IIndexerService mock = IIndexerService.CreateMock(MockBehavior.Default with
		{
			ThrowWhenNotSetup = true,
		});

		void Act()
		{
			_ = mock[null, 1, 2];
		}

		await That(Act).Throws<MockNotSetupException>()
			.WithMessage("get indexer [null, 1, 2] was accessed without prior setup.");
	}

#if NET8_0_OR_GREATER
	[Test]
	public async Task WithReadOnlySpanIndexerParameters_ShouldCompile()
	{
		ReadOnlySpan<int> readOnlySpan123 = ((int[])[1, 2, 3,]).AsSpan();
		ReadOnlySpan<int> readOnlySpan456 = ((int[])[4, 5, 6,]).AsSpan();
		IMyServiceWithSpanIndexerParameters sut = IMyServiceWithSpanIndexerParameters.CreateMock();
		sut.Mock.Setup[It.IsReadOnlySpan<int>(x => x[0] == 1)].InitializeWith(42);

		int result1 = sut[readOnlySpan123];
		int result2 = sut[readOnlySpan456];

		await That(result1).IsEqualTo(42);
		await That(result2).IsEqualTo(0);
		await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(x => x[0] == 1)].Got()).Once();
		await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(x => x[0] == 4)].Got()).Once();
		await That(sut.Mock.Verify[It.IsReadOnlySpan<int>(x => x[0] == 9)].Got()).Never();
	}
#endif

#if NET8_0_OR_GREATER
	[Test]
	public async Task WithSpanIndexerParameters_ShouldCompile()
	{
		Span<char> fooSpan = "foo".ToCharArray().AsSpan();
		Span<char> barSpan = "bar".ToCharArray().AsSpan();
		IMyServiceWithSpanIndexerParameters sut = IMyServiceWithSpanIndexerParameters.CreateMock();
		sut.Mock.Setup[It.IsSpan<char>(x => x[0] == 'f')].InitializeWith(42);

		int result1 = sut[fooSpan];
		int result2 = sut[barSpan];

		await That(result1).IsEqualTo(42);
		await That(result2).IsEqualTo(0);
		await That(sut.Mock.Verify[It.IsSpan<char>(x => x[0] == 'f')].Got()).Once();
		await That(sut.Mock.Verify[It.IsSpan<char>(x => x[0] == 'b')].Got()).Once();
		await That(sut.Mock.Verify[It.IsSpan<char>(x => x[0] == 'x')].Got()).Never();
	}
#endif

	public interface IMyServiceWithSpanIndexerParameters
	{
		int this[Span<char> buffer] { get; set; }
		int this[ReadOnlySpan<int> values] { get; set; }
	}

	public class IndexerWith1Parameter
	{
		[Test]
		public async Task ToString_ShouldIncludeParameterValue()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string this[1]");
		}

		[Test]
		public async Task WithExplicitParameter_ShouldWork()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1].InitializeWith("foo");

			string result1 = sut[1];
			string result2 = sut[2];

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}
	}

	public class IndexerWith2Parameters
	{
		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, 2].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string this[1, 2]");
		}

		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, It.IsAny<int>()].InitializeWith("foo");

			string result1 = sut[1, 10];
			string result2 = sut[2, 10];

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), 1].InitializeWith("foo");

			string result1 = sut[10, 1];
			string result2 = sut[10, 2];

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, 2].InitializeWith("foo");

			string result1 = sut[1, 2];
			string result2 = sut[1, 10];
			string result3 = sut[10, 2];

			await That(result1).IsEqualTo("foo");
			await That(result2).IsEmpty();
			await That(result3).IsEmpty();
		}
	}

	public class IndexerWith3Parameters
	{
		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, 2, 3].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string this[1, 2, 3]");
		}
	}

	public class IndexerWith4Parameters
	{
		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, 2, 3, 4].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string this[1, 2, 3, 4]");
		}
	}

	public class IndexerWith5Parameters
	{
		[Test]
		public async Task ToString_ShouldIncludeParameterValues()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[1, 2, 3, 4, 5].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;

			IReadOnlyCollection<ISetup> result = registry.GetUnusedSetups(new FastMockInteractions(0));

			ISetup setup = await That(result).HasSingle();
			await That(setup.ToString()).IsEqualTo("string this[1, 2, 3, 4, 5]");
		}
	}


	public class NegativeArgumentValidation
	{
		[Test]
		public async Task ApplyIndexerGetter_WithBaseValue_WithNegativeSignatureIndex_ShouldThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			MockRegistry registry = ((IMock)sut).MockRegistry;
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.ApplyIndexerGetter<string>(access, null, "base", -1);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithParamName("signatureIndex");
		}

		[Test]
		public async Task ApplyIndexerGetter_WithGenerator_WithNegativeSignatureIndex_ShouldThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			MockRegistry registry = ((IMock)sut).MockRegistry;
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.ApplyIndexerGetter<string>(access, null, () => "base", -1);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithParamName("signatureIndex");
		}

		[Test]
		public async Task ApplyIndexerSetter_WithNegativeSignatureIndex_ShouldThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			MockRegistry registry = ((IMock)sut).MockRegistry;
			IndexerSetterAccess<int, string> access = new(1, "foo");

			void Act()
			{
				registry.ApplyIndexerSetter<string>(access, null, "foo", -1);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithParamName("signatureIndex");
		}

		[Test]
		public async Task ApplyIndexerSetup_WithNegativeSignatureIndex_ShouldThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns("foo");
			MockRegistry registry = ((IMock)sut).MockRegistry;
			IndexerSetup setup = registry.GetIndexerSetup<IndexerSetup>(s => true)!;
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.ApplyIndexerSetup<string>(access, setup, -1);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithParamName("signatureIndex");
		}

		[Test]
		public async Task GetIndexerFallback_WithNegativeSignatureIndex_ShouldThrow()
		{
			IIndexerService sut = IIndexerService.CreateMock();
			MockRegistry registry = ((IMock)sut).MockRegistry;
			IndexerGetterAccess<int> access = new(1);

			void Act()
			{
				registry.GetIndexerFallback<string>(access, -1);
			}

			await That(Act).Throws<ArgumentOutOfRangeException>()
				.WithParamName("signatureIndex");
		}
	}


	public interface IIndexerService
	{
		string this[int index] { get; set; }
		string this[int index1, int index2] { get; set; }
		string this[int index1, int index2, int index3] { get; set; }
		string this[int index1, int index2, int index3, int index4] { get; set; }
		string this[int index1, int index2, int index3, int index4, int index5] { get; set; }

		string this[char index] { get; set; }
		string this[char index1, char index2] { get; set; }
		string this[char index1, char index2, char index3] { get; set; }
		string this[char index1, char index2, char index3, char index4] { get; set; }
		string this[char index1, char index2, char index3, char index4, char index5] { get; set; }

		int? this[string? index1, int index2, int index3] { get; set; }
	}
}
