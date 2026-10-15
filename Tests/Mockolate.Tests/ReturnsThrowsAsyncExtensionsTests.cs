namespace Mockolate.Tests;

public sealed class ReturnsThrowsAsyncExtensionsTests
{
	public sealed class ReturnsTaskTests
	{
		[Test]
		public async Task ReturnsAsync_1Parameter_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.Method1(1);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ReturnsAsync(v1 => v1 + 10);

			int result = await sut.Method1(2);

			await That(result).IsEqualTo(12);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.Method1(1);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.Method1(1);
			int result2 = await sut.Method1(1);
			int result3 = await sut.Method1(1);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.Method2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync((v1, v2) => v1 + v2 + 10);

			int result = await sut.Method2(1, 2);

			await That(result).IsEqualTo(13);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.Method2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42).When(i => i > 0)
				.Only(1);

			int result1 = await sut.Method2(1, 2);
			int result2 = await sut.Method2(1, 2);
			int result3 = await sut.Method2(1, 2);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.Method3(1, 2, 3);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3) => v1 + v2 + v3 + 10);

			int result = await sut.Method3(1, 2, 3);

			await That(result).IsEqualTo(16);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.Method3(1, 2, 3);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42)
				.When(i => i > 0).Only(1);

			int result1 = await sut.Method3(1, 2, 3);
			int result2 = await sut.Method3(1, 2, 3);
			int result3 = await sut.Method3(1, 2, 3);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(() => 42);

			int result = await sut.Method4(1, 2, 3, 4);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3, v4) => v1 + v2 + v3 + v4 + 10);

			int result = await sut.Method4(1, 2, 3, 4);

			await That(result).IsEqualTo(20);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(42);

			int result = await sut.Method4(1, 2, 3, 4);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.Method4(1, 2, 3, 4);
			int result2 = await sut.Method4(1, 2, 3, 4);
			int result3 = await sut.Method4(1, 2, 3, 4);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(() => 42);

			int result = await sut.Method5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3, v4, v5) => v1 + v2 + v3 + v4 + v5 + 10);

			int result = await sut.Method5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(25);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(42);

			int result = await sut.Method5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.Method5(1, 2, 3, 4, 5);
			int result2 = await sut.Method5(1, 2, 3, 4, 5);
			int result3 = await sut.Method5(1, 2, 3, 4, 5);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().ReturnsAsync(() => 42);

			int result = await sut.Method0();

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().ReturnsAsync(42);

			int result = await sut.Method0();

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.Method0();
			int result2 = await sut.Method0();
			int result3 = await sut.Method0();

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_WithParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(Match.AnyParameters()).ReturnsAsync(() => 42);

			int result = await sut.Method2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_WithParameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(Match.AnyParameters()).ReturnsAsync(42);

			int result = await sut.Method2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task Returns_TaskFactory_ShouldReturnPendingTaskAsIs()
		{
			TaskCompletionSource<int> completionSource = new();
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().Returns(() => completionSource.Task);

			Task<int> result = sut.Method0();

			await That(result.IsCompleted).IsFalse()
				.Because("the factory's task must be handed to the caller as-is, so it can be raced against timeouts or cancellation");
			completionSource.SetResult(42);
			await That(await result).IsEqualTo(42);
		}
	}

#if NET8_0_OR_GREATER
	public sealed class ReturnsValueTaskTests
	{
		[Test]
		public async Task ReturnsAsync_1Parameter_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.MethodVT1(1);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ReturnsAsync(v1 => v1 + 10);

			int result = await sut.MethodVT1(2);

			await That(result).IsEqualTo(12);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.MethodVT1(1);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_1Parameter_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.MethodVT1(1);
			int result2 = await sut.MethodVT1(1);
			int result3 = await sut.MethodVT1(1);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.MethodVT2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync((v1, v2) => v1 + v2 + 10);

			int result = await sut.MethodVT2(1, 2);

			await That(result).IsEqualTo(13);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.MethodVT2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_2Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42).When(i => i > 0)
				.Only(1);

			int result1 = await sut.MethodVT2(1, 2);
			int result2 = await sut.MethodVT2(1, 2);
			int result3 = await sut.MethodVT2(1, 2);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42);

			int result = await sut.MethodVT3(1, 2, 3);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3) => v1 + v2 + v3 + 10);

			int result = await sut.MethodVT3(1, 2, 3);

			await That(result).IsEqualTo(16);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(42);

			int result = await sut.MethodVT3(1, 2, 3);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_3Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).ReturnsAsync(() => 42)
				.When(i => i > 0).Only(1);

			int result1 = await sut.MethodVT3(1, 2, 3);
			int result2 = await sut.MethodVT3(1, 2, 3);
			int result3 = await sut.MethodVT3(1, 2, 3);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(() => 42);

			int result = await sut.MethodVT4(1, 2, 3, 4);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3, v4) => v1 + v2 + v3 + v4 + 10);

			int result = await sut.MethodVT4(1, 2, 3, 4);

			await That(result).IsEqualTo(20);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(42);

			int result = await sut.MethodVT4(1, 2, 3, 4);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_4Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.MethodVT4(1, 2, 3, 4);
			int result2 = await sut.MethodVT4(1, 2, 3, 4);
			int result3 = await sut.MethodVT4(1, 2, 3, 4);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(() => 42);

			int result = await sut.MethodVT5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_CallbackWithValues_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync((v1, v2, v3, v4, v5) => v1 + v2 + v3 + v4 + v5 + 10);

			int result = await sut.MethodVT5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(25);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(42);

			int result = await sut.MethodVT5(1, 2, 3, 4, 5);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_5Parameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.MethodVT5(1, 2, 3, 4, 5);
			int result2 = await sut.MethodVT5(1, 2, 3, 4, 5);
			int result3 = await sut.MethodVT5(1, 2, 3, 4, 5);

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT0().ReturnsAsync(() => 42);

			int result = await sut.MethodVT0();

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT0().ReturnsAsync(42);

			int result = await sut.MethodVT0();

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_NoParameters_ShouldSupportWhenAndFor()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT0().ReturnsAsync(42).When(i => i > 0).Only(1);

			int result1 = await sut.MethodVT0();
			int result2 = await sut.MethodVT0();
			int result3 = await sut.MethodVT0();

			await That(result1).IsEqualTo(0);
			await That(result2).IsEqualTo(42);
			await That(result3).IsEqualTo(0);
		}

		[Test]
		public async Task ReturnsAsync_WithParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(Match.AnyParameters()).ReturnsAsync(() => 42);

			int result = await sut.MethodVT2(1, 2);

			await That(result).IsEqualTo(42);
		}

		[Test]
		public async Task ReturnsAsync_WithParameters_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(Match.AnyParameters()).ReturnsAsync(42);

			int result = await sut.MethodVT2(1, 2);

			await That(result).IsEqualTo(42);
		}
	}
#endif

	public sealed class ThrowsTaskTests
	{
		[Test]
		public async Task ThrowsAsync_1Parameter_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_1Parameter_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ThrowsAsync(v1 => new MyException($"foo-{v1}"));

			Task<int> task = sut.Method1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo-1");
		}

		[Test]
		public async Task ThrowsAsync_1Parameter_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method1(It.IsAny<int>()).ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2) => new MyException($"foo-{v1}-{v2}"));

			Task<int> task = sut.Method2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method2(It.IsAny<int>(), It.IsAny<int>()).ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3) => new MyException($"foo-{v1}-{v2}-{v3}"));

			Task<int> task = sut.Method3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3, v4) => new MyException($"foo-{v1}-{v2}-{v3}-{v4}"));

			Task<int> task = sut.Method4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3-4");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3, v4, v5) => new MyException($"foo-{v1}-{v2}-{v3}-{v4}-{v5}"));

			Task<int> task = sut.Method5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3-4-5");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_NoParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().ThrowsAsync(() => new MyException("foo"));

			Task<int> task = sut.Method0();

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_NoParameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.Method0().ThrowsAsync(new MyException("foo"));

			Task<int> task = sut.Method0();

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}
	}

#if NET8_0_OR_GREATER
	public sealed class ThrowsValueTaskTests
	{
		[Test]
		public async Task ThrowsAsync_1Parameter_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_1Parameter_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ThrowsAsync(v1 => new MyException($"foo-{v1}"));

			ValueTask<int> task = sut.MethodVT1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo-1");
		}

		[Test]
		public async Task ThrowsAsync_1Parameter_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT1(It.IsAny<int>()).ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT1(1);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2) => new MyException($"foo-{v1}-{v2}"));

			ValueTask<int> task = sut.MethodVT2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2");
		}

		[Test]
		public async Task ThrowsAsync_2Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT2(It.IsAny<int>(), It.IsAny<int>()).ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT2(1, 2);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3) => new MyException($"foo-{v1}-{v2}-{v3}"));

			ValueTask<int> task = sut.MethodVT3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3");
		}

		[Test]
		public async Task ThrowsAsync_3Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT3(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT3(1, 2, 3);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3, v4) => new MyException($"foo-{v1}-{v2}-{v3}-{v4}"));

			ValueTask<int> task = sut.MethodVT4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3-4");
		}

		[Test]
		public async Task ThrowsAsync_4Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT4(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT4(1, 2, 3, 4);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_CallbackWithValue_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync((v1, v2, v3, v4, v5) => new MyException($"foo-{v1}-{v2}-{v3}-{v4}-{v5}"));

			ValueTask<int> task = sut.MethodVT5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo-1-2-3-4-5");
		}

		[Test]
		public async Task ThrowsAsync_5Parameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT5(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
					It.IsAny<int>())
				.ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT5(1, 2, 3, 4, 5);

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_NoParameters_Callback_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT0().ThrowsAsync(() => new MyException("foo"));

			ValueTask<int> task = sut.MethodVT0();

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}

		[Test]
		public async Task ThrowsAsync_NoParameters_Exception_ReturnsConfiguredValue()
		{
			IReturnsAsyncExtensionsSetupTest sut = IReturnsAsyncExtensionsSetupTest.CreateMock();
			sut.Mock.Setup.MethodVT0().ThrowsAsync(new MyException("foo"));

			ValueTask<int> task = sut.MethodVT0();

			await That(() => task).Throws<MyException>().WithMessage("foo");
		}
	}
#endif

	public interface IReturnsAsyncExtensionsSetupTest
	{
		Task<int> Method0();
		Task<int> Method1(int p1);
		Task<int> Method2(int p1, int p2);
		Task<int> Method3(int p1, int p2, int p3);
		Task<int> Method4(int p1, int p2, int p3, int p4);
		Task<int> Method5(int p1, int p2, int p3, int p4, int p5);
#if NET8_0_OR_GREATER
		ValueTask<int> MethodVT0();
		ValueTask<int> MethodVT1(int p1);
		ValueTask<int> MethodVT2(int p1, int p2);
		ValueTask<int> MethodVT3(int p1, int p2, int p3);
		ValueTask<int> MethodVT4(int p1, int p2, int p3, int p4);
		ValueTask<int> MethodVT5(int p1, int p2, int p3, int p4, int p5);
#endif
	}

	private sealed class MyException(string message) : Exception(message);
}
