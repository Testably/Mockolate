using Mockolate.Interactions;
using Mockolate.Internal.Tests.TestHelpers;
using Mockolate.Parameters;
using Mockolate.Setup;

namespace Mockolate.Internal.Tests.Setup;

public sealed class IndexerSetupTests
{
	[Test]
	public async Task ExecuteGetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnGet.Do(() => { callCount++; });
		IndexerGetterAccess<string> access = new("mismatch");

		long result = indexerSetup.DoGetResult(access, 2L);

		await That(result).IsEqualTo(2L);
		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteGetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnGet.Do(() => { callCount++; });
		IndexerGetterAccess<int, int> access = new(1, 1);

		string result = indexerSetup.DoGetResult(access, "foo");

		await That(result).IsEqualTo("foo");
		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteGetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnGet.Do(() => { callCount++; });
		IndexerGetterAccess<string> access = new("expect-int");

		string result = indexerSetup.DoGetResult(access, "foo");

		await That(result).IsEqualTo("foo");
		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteGetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnGet.Do(() => { callCount++; });
		IndexerGetterAccess<int> access = new(1);

		indexerSetup.DoGetResult(access, "foo");

		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task ExecuteSetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnSet.Do(() => { callCount++; });
		IndexerSetterAccess<string, string> access = new("mismatch", "bar");

		indexerSetup.DoSetResult(access, 2L);

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteSetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnSet.Do(() => { callCount++; });
		IndexerSetterAccess<int, int, string> access = new(1, 1, "bar");

		indexerSetup.DoSetResult(access, "foo");

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteSetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnSet.Do(() => { callCount++; });
		IndexerSetterAccess<string, string> access = new("expect-int", "bar");

		indexerSetup.DoSetResult(access, "foo");

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task ExecuteSetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
	{
		int callCount = 0;
		MyIndexerSetup<int> indexerSetup = new();
		indexerSetup.OnSet.Do(() => { callCount++; });
		IndexerSetterAccess<int, string> access = new(1, "bar");

		indexerSetup.DoSetResult(access, "foo");

		await That(callCount).IsEqualTo(1);
	}

	[Test]
	public async Task GetResult_WithBaseValue_StoresComputedValueForLaterLookup()
	{
		IndexerSetup<string, int> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
			(IParameterMatch<int>)It.IsAny<int>());
		IndexerValueStorage<string> storage = new();
		IndexerGetterAccess<int> access1 = new(42)
		{
			Storage = storage,
		};

		string result = setup.GetResult(access1, MockBehavior.Default, "base");

		IndexerGetterAccess<int> access2 = new(42)
		{
			Storage = storage,
		};
		bool found = access2.TryFindStoredValue(out string stored);

		await That(result).IsEqualTo("base");
		await That(found).IsTrue();
		await That(stored).IsEqualTo("base");
	}

	[Test]
	public async Task GetResult_WithDefaultValueGenerator_StoresComputedValueForLaterLookup()
	{
		IndexerSetup<string, int> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
			(IParameterMatch<int>)It.IsAny<int>());
		IndexerValueStorage<string> storage = new();
		IndexerGetterAccess<int> access1 = new(42)
		{
			Storage = storage,
		};

		string result = setup.GetResult<string>(access1, MockBehavior.Default, () => "generated");

		IndexerGetterAccess<int> access2 = new(42)
		{
			Storage = storage,
		};
		bool found = access2.TryFindStoredValue(out string stored);

		await That(result).IsEqualTo("generated");
		await That(found).IsTrue();
		await That(stored).IsEqualTo("generated");
	}

	[Test]
	public async Task MatchesAccess_WithUnknownAccessType_ShouldReturnFalse()
	{
		IndexerSetup<int, string> setup = new(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
			(IParameterMatch<string>)It.IsAny<string>());
		FakeIndexerAccess access = new();

		bool matches = ((IInteractiveIndexerSetup)setup).Matches(access);

		await That(matches).IsFalse();
	}

	[Test]
	public async Task SetResult_WhenValueDoesNotCastToTValue_ShouldNotInvokeSetterCallbacks()
	{
		MyIndexerSetup<int> setup = new();
		int callCount = 0;
		setup.OnSet.Do(() => callCount++);
		IndexerSetterAccess<int, string> access = new(1, "stored");

		setup.DoSetResult(access, 2L);

		await That(callCount).IsEqualTo(0);
	}

	[Test]
	public async Task TryCast_WhenValueIsNotOfTargetTypeAndNotNull_ShouldReturnFalse()
	{
		bool success = FakeIndexerSetup.InvokeTryCast(42, out string _, MockBehavior.Default);

		await That(success).IsFalse();
	}

	[Test]
	public async Task TryCast_WhenValueIsNull_ShouldReturnTrue()
	{
		bool success = FakeIndexerSetup.InvokeTryCast(null, out string result, MockBehavior.Default);

		await That(success).IsTrue();
		await That(result).IsNull();
	}

	public sealed class With2Levels
	{
		[Test]
		public async Task ExecuteGetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<string, string> access = new("a", "b");

			long result = indexerSetup.DoGetResult(access, 2L);

			await That(result).IsEqualTo(2L);
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int> access = new(1, 2, 3);

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, string> access = new(1, "expect-int");

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int> access = new(1, 2);

			indexerSetup.DoGetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenAssignedValueDoesNotCastToTValue_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, string> access = new(1, 2, "bar");

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int> access = new(1, 2, 99);

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, string, string> access = new(1, "expect-int", "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, string> access = new(1, 2, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task GetResult_WithBaseValue_StoresComputedValueForLaterLookup()
		{
			IndexerSetup<string, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int> access1 = new(1, 2)
			{
				Storage = storage,
			};

			string result = setup.GetResult(access1, MockBehavior.Default, "base");

			IndexerGetterAccess<int, int> access2 = new(1, 2)
			{
				Storage = storage,
			};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("base");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("base");
		}

		[Test]
		public async Task GetResult_WithFuncGenerator_AndInitialization_ShouldUseInitializationValue()
		{
			IndexerSetup<string, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			setup.InitializeWith((p1, p2) => $"{p1}-{p2}");
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int> access1 = new(7, 9)
			{
				Storage = storage,
			};

			string result = setup.GetResult<string>(access1, MockBehavior.Default, () => "fallback");

			IndexerGetterAccess<int, int> access2 = new(7, 9)
			{
				Storage = storage,
			};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("7-9");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("7-9");
		}

		[Test]
		public async Task MatchesAccess_WithUnknownAccessType_ShouldReturnFalse()
		{
			IndexerSetup<string, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			FakeIndexerAccess access = new();

			bool matches = ((IInteractiveIndexerSetup)setup).Matches(access);

			await That(matches).IsFalse();
		}

		private sealed class MyIndexerSetup<T1, T2>()
			: IndexerSetup<string, T1, T2>(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<T1>)It.IsAny<T1>(),
				(IParameterMatch<T2>)It.IsAny<T2>())
		{
			private readonly IndexerValueStorage<string> _storage = new();

			public T DoGetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				return GetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}

			public void DoSetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				SetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}
		}
	}

	public sealed class With3Levels
	{
		[Test]
		public async Task ExecuteGetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<string, string, string> access = new("a", "b", "c");

			long result = indexerSetup.DoGetResult(access, 2L);

			await That(result).IsEqualTo(2L);
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, int> access = new(1, 2, 3, 4);

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, string> access = new(1, 2, "expect-int");

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int> access = new(1, 2, 3);

			indexerSetup.DoGetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenAssignedValueDoesNotCastToTValue_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "bar");

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int> access = new(1, 2, 3, 99);

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, string> access = new(1, 2, 3, 4, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, string, string> access = new(1, 2, "expect-int", "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, string> access = new(1, 2, 3, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task GetResult_WithBaseValue_StoresComputedValueForLaterLookup()
		{
			IndexerSetup<string, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int, int> access1 =
				new(1, 2, 3)
				{
					Storage = storage,
				};

			string result = setup.GetResult(access1, MockBehavior.Default, "base");

			IndexerGetterAccess<int, int, int> access2 =
				new(1, 2, 3)
				{
					Storage = storage,
				};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("base");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("base");
		}

		[Test]
		public async Task GetResult_WithFuncGenerator_AndInitialization_ShouldUseInitializationValue()
		{
			IndexerSetup<string, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			setup.InitializeWith((p1, p2, p3) => $"{p1}-{p2}-{p3}");
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int, int> access1 = new(7, 8, 9)
			{
				Storage = storage,
			};

			string result = setup.GetResult<string>(access1, MockBehavior.Default, () => "fallback");

			IndexerGetterAccess<int, int, int> access2 = new(7, 8, 9)
			{
				Storage = storage,
			};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("7-8-9");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("7-8-9");
		}

		[Test]
		public async Task MatchesAccess_WithUnknownAccessType_ShouldReturnFalse()
		{
			IndexerSetup<string, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			FakeIndexerAccess access = new();

			bool matches = ((IInteractiveIndexerSetup)setup).Matches(access);

			await That(matches).IsFalse();
		}

		private sealed class MyIndexerSetup<T1, T2, T3>()
			: IndexerSetup<string, T1, T2, T3>(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<T1>)It.IsAny<T1>(),
				(IParameterMatch<T2>)It.IsAny<T2>(),
				(IParameterMatch<T3>)It.IsAny<T3>())
		{
			private readonly IndexerValueStorage<string> _storage = new();

			public T DoGetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				return GetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}

			public void DoSetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				SetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}
		}
	}

	public sealed class With4Levels
	{
		[Test]
		public async Task ExecuteGetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<string, string, string, string> access =
				new("a", "b", "c", "d");

			long result = indexerSetup.DoGetResult(access, 2L);

			await That(result).IsEqualTo(2L);
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int> access = new(1, 2, 3);

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, string> access =
				new(1, 2, 3, "expect-int");

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, int> access =
				new(1, 2, 3, 4);

			indexerSetup.DoGetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenAssignedValueDoesNotCastToTValue_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, string> access =
				new(1, 2, 3, 4, "bar");

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, int> access =
				new(1, 2, 3, 4, 99);

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, string> access =
				new(1, 2, 3, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, string, string> access =
				new(1, 2, 3, "expect-int", "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, string> access =
				new(1, 2, 3, 4, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task GetResult_WithBaseValue_StoresComputedValueForLaterLookup()
		{
			IndexerSetup<string, int, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int, int, int> access1 =
				new(1, 2, 3, 4)
				{
					Storage = storage,
				};

			string result = setup.GetResult(access1, MockBehavior.Default, "base");

			IndexerGetterAccess<int, int, int, int> access2 =
				new(1, 2, 3, 4)
				{
					Storage = storage,
				};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("base");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("base");
		}

		[Test]
		public async Task GetResult_WithFuncGenerator_AndInitialization_ShouldUseInitializationValue()
		{
			IndexerSetup<string, int, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			setup.InitializeWith((p1, p2, p3, p4) => $"{p1}-{p2}-{p3}-{p4}");
			IndexerValueStorage<string> storage = new();
			IndexerGetterAccess<int, int, int, int> access1 = new(6, 7, 8, 9)
			{
				Storage = storage,
			};

			string result = setup.GetResult<string>(access1, MockBehavior.Default, () => "fallback");

			IndexerGetterAccess<int, int, int, int> access2 = new(6, 7, 8, 9)
			{
				Storage = storage,
			};
			bool found = access2.TryFindStoredValue(out string stored);

			await That(result).IsEqualTo("6-7-8-9");
			await That(found).IsTrue();
			await That(stored).IsEqualTo("6-7-8-9");
		}

		[Test]
		public async Task MatchesAccess_WithUnknownAccessType_ShouldReturnFalse()
		{
			IndexerSetup<string, int, int, int, int> setup = new(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>(),
				(IParameterMatch<int>)It.IsAny<int>());
			FakeIndexerAccess access = new();

			bool matches = ((IInteractiveIndexerSetup)setup).Matches(access);

			await That(matches).IsFalse();
		}

		private sealed class MyIndexerSetup<T1, T2, T3, T4>()
			: IndexerSetup<string, T1, T2, T3, T4>(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<T1>)It.IsAny<T1>(),
				(IParameterMatch<T2>)It.IsAny<T2>(),
				(IParameterMatch<T3>)It.IsAny<T3>(),
				(IParameterMatch<T4>)It.IsAny<T4>())
		{
			private readonly IndexerValueStorage<string> _storage = new();

			public T DoGetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				return GetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}

			public void DoSetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				SetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}
		}
	}

	public sealed class With5Levels
	{
		[Test]
		public async Task ExecuteGetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<string, string, string, string, string> access =
				new("a", "b", "c", "d", "e");

			long result = indexerSetup.DoGetResult(access, 2L);

			await That(result).IsEqualTo(2L);
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, int> access =
				new(1, 2, 3, 4);

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, int, string> access =
				new(1, 2, 3, 4, "expect-int");

			string result = indexerSetup.DoGetResult(access, "foo");

			await That(result).IsEqualTo("foo");
			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteGetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnGet.Do(() => { callCount++; });
			IndexerGetterAccess<int, int, int, int, int> access =
				new(1, 2, 3, 4, 5);

			indexerSetup.DoGetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenGenericTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, int, int> access =
				new(1, 2, 3, 4, 5, 99);

			indexerSetup.DoSetResult(access, 2L);

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenNumberOfParametersDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, string> access =
				new(1, 2, 3, 4, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenParameterTypeDoesNotMatch_ShouldNotExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, string, string> access =
				new(1, 2, 3, 4, "expect-int", "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(0);
		}

		[Test]
		public async Task ExecuteSetterCallback_WhenTypesAndNumberMatch_ShouldExecute()
		{
			int callCount = 0;
			MyIndexerSetup<int, int, int, int, int> indexerSetup = new();
			indexerSetup.OnSet.Do(() => { callCount++; });
			IndexerSetterAccess<int, int, int, int, int, string> access =
				new(1, 2, 3, 4, 5, "bar");

			indexerSetup.DoSetResult(access, "foo");

			await That(callCount).IsEqualTo(1);
		}

		// Triggers the source generator to emit IndexerSetup<TValue, T1, T2, T3, T4, T5> in this project.
		private static readonly IFiveIndexer _ = IFiveIndexer.CreateMock();

		internal interface IFiveIndexer
		{
			string this[int p1, int p2, int p3, int p4, int p5] { get; set; }
		}

		private sealed class MyIndexerSetup<T1, T2, T3, T4, T5>()
			: IndexerSetup<string, T1, T2, T3, T4, T5>(
				new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
				(IParameterMatch<T1>)It.IsAny<T1>(),
				(IParameterMatch<T2>)It.IsAny<T2>(),
				(IParameterMatch<T3>)It.IsAny<T3>(),
				(IParameterMatch<T4>)It.IsAny<T4>(),
				(IParameterMatch<T5>)It.IsAny<T5>())
		{
			private readonly IndexerValueStorage<string> _storage = new();

			public T DoGetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				return GetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}

			public void DoSetResult<T>(
				IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
			{
				indexerAccess.Storage = _storage;
				SetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
			}
		}
	}

	private sealed class MyIndexerSetup<T1>()
		: IndexerSetup<string, T1>(
			new MockRegistry(MockBehavior.Default, new FastMockInteractions(0)),
			(IParameterMatch<T1>)It.IsAny<T1>())
	{
		private readonly IndexerValueStorage<string> _storage = new();

		public T DoGetResult<T>(
			IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
		{
			indexerAccess.Storage = _storage;
			return GetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
		}

		public void DoSetResult<T>(
			IndexerAccess indexerAccess, T value, MockBehavior? behavior = null)
		{
			indexerAccess.Storage = _storage;
			SetResult(indexerAccess, behavior ?? MockBehavior.Default, value);
		}
	}
}
