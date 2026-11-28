using Mockolate.Tests.GeneratorCoverage;
using Mockolate.Verify;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifyGotIndexerTests
{
	public sealed class WithOptionalKeysTests
	{
		[Test]
		public async Task GetterOnly_WithOmittedKey_ShouldOnlyCountTheDeclaredDefault()
		{
			IOptionalIndexerKeysGetterOnly sut = IOptionalIndexerKeysGetterOnly.CreateMock();

			_ = sut[1];
			_ = sut[1, 3];

			await That(sut.Mock.Verify[It.IsAny<byte>()].Got()).Once();
		}

		[Test]
		public async Task WithOmittedKeys_ShouldOnlyCountTheDeclaredDefaults()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();

			_ = sut[1];
			_ = sut[2, 5, null];
			_ = sut[1, 6];
			_ = sut[1, null];
			_ = sut[1, 5, "x"];

			await That(sut.Mock.Verify[It.IsAny<int>()].Got()).Twice();
			await That(sut.Mock.Verify[1].Got()).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), 6].Got()).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), It.IsNull<int?>()].Got()).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), null].Got()).Once()
				.Because("only a left-out key stands for its declared default");
		}

		[Test]
		public async Task WithOmittedKeys_ShouldShowTheDeclaredDefaultsInTheExpectation()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();

			VerificationResult<Mock.IMockVerifyForIOptionalIndexerKeys> result = sut.Mock.Verify[It.IsAny<int>()].Got();

			await That(((IVerificationResult)result).Expectation).IsEqualTo("got indexer [It.IsAny<int>(), 5, null]");
		}

		[Test]
		public async Task WithSibling_ShouldBindToTheIndexerWithFewerKeys()
		{
			IOptionalIndexerKeysWithSibling sut = IOptionalIndexerKeysWithSibling.CreateMock();

			_ = sut[1];
			_ = sut[1, 2, 3];

			await That(sut.Mock.Verify[It.IsAny<int>()].Got()).Once();
			await That(sut.Mock.Verify[1].Got()).Once();
		}

		[Test]
		public async Task WithSixKeysAndOmittedKeys_ShouldOnlyCountTheDeclaredDefaults()
		{
			IOptionalIndexerKeysWithSixKeys sut = IOptionalIndexerKeysWithSixKeys.CreateMock();

			_ = sut[1, 2, 3, 4];
			_ = sut[1, 2, 3, 4, 5, "f"];
			_ = sut[1, 2, 3, 4, 6];
			_ = sut[1, 2, 3, 4, 5, "x"];

			await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Got())
				.Twice();
		}

		[Test]
		public async Task WithSixKeysAndOmittedKeys_ShouldShowTheDeclaredDefaultsInTheExpectation()
		{
			IOptionalIndexerKeysWithSixKeys sut = IOptionalIndexerKeysWithSixKeys.CreateMock();

			VerificationResult<Mock.IMockVerifyForIOptionalIndexerKeysWithSixKeys> result
				= sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Got();

			await That(((IVerificationResult)result).Expectation)
				.IsEqualTo("got indexer [It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 5, \"f\"]");
		}
	}
}
