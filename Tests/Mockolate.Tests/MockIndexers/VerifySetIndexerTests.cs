using Mockolate.Tests.TestHelpers;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifySetIndexerTests
{
	[Test]
	public async Task Set_WithExplicitParameter_ShouldCheckForEquality()
	{
		IMyService sut = IMyService.CreateMock();

		sut[1] = "foo";

		await That(sut.Mock.Verify[1].Set("bar")).Never();
		await That(sut.Mock.Verify[1].Set("foo")).Once();
		await That(sut.Mock.Verify[1].Set("FOO")).Never();
	}

	[Test]
	public async Task WhenNullParametersAndValue_AndMatches_ShouldReturn()
	{
		IMyService sut = IMyService.CreateMock();
		sut[null, null, null, null] = null;

		await That(sut.Mock.Verify[It.IsAny<int?>(), null, It.IsNull<int?>(), It.IsAny<int?>()].Set(null)).Once();
	}

	[Test]
	public async Task WhenParameterLengthDoesNotMatch_ShouldReturnNever()
	{
		IMyService sut = IMyService.CreateMock();
		sut[1, 2] = "foo";

		await That(sut.Mock.Verify[It.IsAny<int>()].Set(It.Is("foo"))).Never();
		await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>()].Set(It.Is("foo"))).Once();
		await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Set(It.Is("foo"))).Never();
	}

	[Test]
	public async Task WhenParametersDoNotMatch_ShouldReturnNever()
	{
		IMyService sut = IMyService.CreateMock();
		sut[1, 2] = "foo";

		await That(sut.Mock.Verify[It.Is(1), It.Is(2)].Set(It.Is("foo"))).Once();
		await That(sut.Mock.Verify[It.Satisfies<int>(i => i != 1), It.Is(2)].Set(It.Is("foo"))).Never();
		await That(sut.Mock.Verify[It.Is(1), It.Satisfies<int>(i => i != 2)].Set(It.Is("foo"))).Never();
	}

	public class IndexerWith1Parameter
	{
		[Test]
		public async Task WithExplicitParameter_ShouldWork()
		{
			IMyService sut = IMyService.CreateMock();
			sut[1] = "foo";

			await That(sut.Mock.Verify[1].Set("foo")).Once();
			await That(sut.Mock.Verify[2].Set("foo")).Never();
			await That(sut.Mock.Verify[1].Set("bar")).Never();
		}
	}

	public class IndexerWith2Parameters
	{
		[Test]
		public async Task WithExplicitParameter1_ShouldWork()
		{
			IMyService sut = IMyService.CreateMock();
			sut[1, 10] = "foo";

			await That(sut.Mock.Verify[1, It.IsAny<int>()].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[2, It.IsAny<int>()].Set(It.IsAny<string>())).Never();
		}

		[Test]
		public async Task WithExplicitParameter2_ShouldWork()
		{
			IMyService sut = IMyService.CreateMock();
			sut[10, 1] = "foo";

			await That(sut.Mock.Verify[It.IsAny<int>(), 1].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), 2].Set(It.IsAny<string>())).Never();
		}

		[Test]
		public async Task WithExplicitParameters_ShouldWork()
		{
			IMyService sut = IMyService.CreateMock();
			sut[1, 2] = "foo";

			await That(sut.Mock.Verify[1, 2].Set("foo")).Once();
			await That(sut.Mock.Verify[1, 10].Set("foo")).Never();
			await That(sut.Mock.Verify[10, 2].Set("foo")).Never();
			await That(sut.Mock.Verify[1, 2].Set("bar")).Never();
		}
	}
}
