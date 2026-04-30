using System.Collections.Generic;

namespace Mockolate.Tests;

public sealed class SetupExtensionsTests
{
	public sealed class PropertySetupReturnWhenBuilderTests
	{
		[Test]
		public async Task Forever_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task OnlyOnce_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut.MyProperty = 0;
				values[i] = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut.MyProperty = 0;
				values[i] = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}
	}

	public sealed class PropertySetupWhenBuilderTests
	{
		[Test]
		public async Task OnlyOnce_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.OnGet.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyProperty;
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.OnGet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyProperty;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyProperty.OnSet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyProperty = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}
	}

	public sealed class IndexerSetupReturnWhenBuilderTests
	{
		[Test]
		public async Task Forever_With1Parameter_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10];
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With1Parameter_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10];
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20];
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].Returns(1).Returns(2).Returns(3).When(i => i >= 2)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20];
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).Returns(2).Returns(3)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).Returns(2).Returns(3)
				.When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1)
				.Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1)
				.Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldKeepLastUsedValue()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10];
				if (i == 4)
				{
					sut[10] = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldUseReturnValueOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10] = 0;
				values[i] = sut[10];
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10] = 0;
				values[i] = sut[10];
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldKeepLastUsedValue()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20];
				if (i == 4)
				{
					sut[10, 20] = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldUseReturnValueOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20] = 0;
				values[i] = sut[10, 20];
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20] = 0;
				values[i] = sut[10, 20];
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldKeepLastUsedValue()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30];
				if (i == 4)
				{
					sut[10, 20, 30] = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldUseReturnValueOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30] = 0;
				values[i] = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30] = 0;
				values[i] = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldKeepLastUsedValue()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40];
				if (i == 4)
				{
					sut[10, 20, 30, 40] = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldUseReturnValueOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40] = 0;
				values[i] = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns(1).For(3)
				.OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40] = 0;
				values[i] = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldKeepLastUsedValue()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut[10, 20, 30, 40, 50];
				if (i == 4)
				{
					sut[10, 20, 30, 40, 50] = 10;
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 1, 1, 10, 10, 10, 10, 10,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldUseReturnValueOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40, 50] = 0;
				values[i] = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40, 50] = 0;
				values[i] = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}
	}

	public sealed class IndexerSetupWhenBuilderTests
	{
		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].OnGet.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10];
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].OnGet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10];
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].OnGet.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20];
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].OnGet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20];
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].OnGet.Do(() => values.Add(1))
				.OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].OnGet.Do(() => values.Add(1))
				.For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30];
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].OnGet
				.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].OnGet
				.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30, 40];
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnGet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut[10, 20, 30, 40, 50];
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_With1Parameter_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].OnSet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut[10] = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_With2Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>()].OnSet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut[10, 20] = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_With3Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].OnSet.Do(() => values.Add(1))
				.For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30] = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_With4Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40] = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnSet_OnlyOnce_With5Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()]
				.OnSet.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut[10, 20, 30, 40, 50] = i;
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}
	}

	public sealed class ReturnMethodSetupReturnWhenBuilderTests
	{
		[Test]
		public async Task Forever_With0Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With0Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With1Parameter_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With1Parameter_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Returns(1).Returns(2).Returns(3).When(i => i >= 2)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Returns(1).Returns(2).Returns(3)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Returns(1).Returns(2).Returns(3)
				.When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(1).Returns(2)
				.Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(1).Returns(2)
				.Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).Returns(2).Returns(3).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).Returns(2).Returns(3).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Returns(1).For(3)
				.OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Returns(1).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}
	}

	public sealed class ReturnMethodSetupWhenBuilderTests
	{
		[Test]
		public async Task OnlyOnce_With0Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod().Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod();
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>()).Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1)).For(3)
				.OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1))
				.OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1))
				.For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyIntMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				_ = sut.MyIntMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}
	}

	public sealed class VoidMethodSetupReturnWhenBuilderTests
	{
		[Test]
		public async Task Forever_With0Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod();
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With0Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod();
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With1Parameter_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With1Parameter_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).When(i => i >= 2)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Throws(new Exception("1"))
				.Throws(new Exception("2")).Throws(new Exception("3"))
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With2Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Throws(new Exception("1"))
				.Throws(new Exception("2")).Throws(new Exception("3"))
				.When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With3Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2"))
				.Throws(new Exception("3")).When(i => i >= 2).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2")).Throws(new Exception("3")).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With4Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2")).Throws(new Exception("3")).When(i => i >= 2)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_ShouldKeepApplyingTheSetup()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2")).Throws(new Exception("3")).Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40, 50);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 3, 3, 3, 3, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task Forever_With5Parameters_When_ShouldKeepApplyingTheSetupWhenThePredicateMatches()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).Throws(new Exception("2")).Throws(new Exception("3")).When(i => i >= 2)
				.Forever();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40, 50);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 2, 1, 2, 1, 2, 3, 3, 3, 3,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod();
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Throws(new Exception("1")).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod();
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Throws(new Exception("1")).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Throws(new Exception("1")).For(3)
				.OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).For(3)
				.OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldApplySetupOnlyOnce()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40, 50);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 0, 0, 0, 0, 0, 0, 0, 0, 0,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldApplySetupOnlyForTimes()
		{
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Throws(new Exception("1")).For(3).OnlyOnce();

			int[] values = new int[10];
			for (int i = 0; i < 10; i++)
			{
				try
				{
					sut.MyVoidMethod(10, 20, 30, 40, 50);
				}
				catch (Exception ex)
				{
					values[i] = int.Parse(ex.Message);
				}
			}

			await That(values).IsEqualTo([1, 1, 1, 0, 0, 0, 0, 0, 0, 0,]);
		}
	}

	public sealed class VoidMethodSetupWhenBuilderTests
	{
		[Test]
		public async Task OnlyOnce_With0Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod();
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With0Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod().Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod();
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With1Parameter_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>()).Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With2Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1)).For(3)
				.OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1))
				.OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With3Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()).Do(() => values.Add(1))
				.For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With4Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30, 40);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_ShouldInvokeTheCallbackOnlyOnce()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1,]);
		}

		[Test]
		public async Task OnlyOnce_With5Parameters_WithFor_ShouldInvokeTheCallbackOnlyForTimes()
		{
			List<int> values = [];
			ISetupExtensionsTestService sut = ISetupExtensionsTestService.CreateMock();
			sut.Mock.Setup.MyVoidMethod(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())
				.Do(() => values.Add(1)).For(3).OnlyOnce();

			for (int i = 0; i < 10; i++)
			{
				sut.MyVoidMethod(10, 20, 30, 40, 50);
			}

			await That(values).IsEqualTo([1, 1, 1,]);
		}
	}

	public interface ISetupExtensionsTestService
	{
		int MyProperty { get; set; }

		int this[int index1] { get; set; }
		int this[int index1, int index2] { get; set; }
		int this[int index1, int index2, int index3] { get; set; }
		int this[int index1, int index2, int index3, int index4] { get; set; }
		int this[int index1, int index2, int index3, int index4, int index5] { get; set; }

		int MyIntMethod();
		int MyIntMethod(int param1);
		int MyIntMethod(int param1, int param2);
		int MyIntMethod(int param1, int param2, int param3);
		int MyIntMethod(int param1, int param2, int param3, int param4);
		int MyIntMethod(int param1, int param2, int param3, int param4, int param5);

		void MyVoidMethod();
		void MyVoidMethod(int param1);
		void MyVoidMethod(int param1, int param2);
		void MyVoidMethod(int param1, int param2, int param3);
		void MyVoidMethod(int param1, int param2, int param3, int param4);
		void MyVoidMethod(int param1, int param2, int param3, int param4, int param5);
	}
}
