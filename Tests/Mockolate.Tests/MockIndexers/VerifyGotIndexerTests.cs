using Mockolate.TestHelpers;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifyGotIndexerTests
{
	[Test]
	public async Task WhenKeysAreNull_ShouldCountTheAccess()
	{
		IMyService sut = IMyService.CreateMock();
		_ = sut[null, null, null, null];

		await That(sut.Mock.Verify[It.IsAny<int?>(), null, It.IsNull<int?>(), It.IsAny<int?>()].Got()).Once();
	}

	[Test]
	public async Task WhenKeyCountDoesNotMatch_ShouldReturnNever()
	{
		IMyService sut = IMyService.CreateMock();
		_ = sut[1, 2];

		await That(sut.Mock.Verify[It.IsAny<int>()].Got()).Never();
		await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>()].Got()).Once();
		await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Got()).Never();
	}

	[Test]
	public async Task WhenKeysDoNotMatch_ShouldReturnNever()
	{
		IMyService sut = IMyService.CreateMock();
		_ = sut[1, 2];

		await That(sut.Mock.Verify[It.Is(1), It.Is(2)].Got()).Once();
		await That(sut.Mock.Verify[It.Satisfies<int>(i => i != 1), It.Is(2)].Got()).Never();
		await That(sut.Mock.Verify[It.Is(1), It.Satisfies<int>(i => i != 2)].Got()).Never();
	}

	public class IndexerWith1ParameterTests
	{
		[Test]
		public async Task WithExplicitParameter_ShouldOnlyCountTheExplicitValue()
		{
			IMyService sut = IMyService.CreateMock();
			_ = sut[1];

			await That(sut.Mock.Verify[1].Got()).Once();
			await That(sut.Mock.Verify[2].Got()).Never();
		}
	}

	public class IndexerWith2ParametersTests
	{
		[Test]
		public async Task WithExplicitParameter1_ShouldOnlyCountTheExplicitValue()
		{
			IMyService sut = IMyService.CreateMock();
			_ = sut[1, 10];

			await That(sut.Mock.Verify[1, It.IsAny<int>()].Got()).Once();
			await That(sut.Mock.Verify[2, It.IsAny<int>()].Got()).Never();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldOnlyCountTheExplicitValue()
		{
			IMyService sut = IMyService.CreateMock();
			_ = sut[10, 1];

			await That(sut.Mock.Verify[It.IsAny<int>(), 1].Got()).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), 2].Got()).Never();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldOnlyCountTheExplicitValues()
		{
			IMyService sut = IMyService.CreateMock();
			_ = sut[1, 2];

			await That(sut.Mock.Verify[1, 2].Got()).Once();
			await That(sut.Mock.Verify[1, 10].Got()).Never();
			await That(sut.Mock.Verify[10, 2].Got()).Never();
		}
	}
}
