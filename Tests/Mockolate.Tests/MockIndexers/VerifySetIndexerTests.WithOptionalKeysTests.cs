using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifySetIndexerTests
{
	public sealed class WithOptionalKeysTests
	{
		[Test]
		public async Task SetterOnly_WithOmittedKey_ShouldOnlyCountTheDeclaredDefault()
		{
			IOptionalIndexerKeysSetterOnly sut = IOptionalIndexerKeysSetterOnly.CreateMock();

			sut[1, 2, 3] = 1L;
			sut[1, 2, 3, 4] = 1L;
			sut[1, 2, 3, 5] = 1L;

			await That(sut.Mock.Verify[It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()].Set(1L)).Twice();
		}

		[Test]
		public async Task WithOmittedKeys_ShouldOnlyCountTheDeclaredDefaults()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();

			sut[1] = "a";
			sut[2, 5, null] = "a";
			sut[1, 6] = "a";
			sut[1, null] = "a";
			sut[1, 5, "x"] = "a";

			await That(sut.Mock.Verify[It.IsAny<int>()].Set("a")).Twice();
			await That(sut.Mock.Verify[1].Set("a")).Once();
			await That(sut.Mock.Verify[It.IsAny<int>(), 6].Set("a")).Once();
		}

		[Test]
		public async Task WithSixKeysAndOmittedKeys_ShouldOnlyCountTheDeclaredDefaults()
		{
			IOptionalIndexerKeysWithSixKeys sut = IOptionalIndexerKeysWithSixKeys.CreateMock();

			sut[1, 2, 3, 4] = "a";
			sut[1, 2, 3, 4, 5, "f"] = "a";
			sut[1, 2, 3, 4, 6] = "a";
			sut[1, 2, 3, 4, 5, "x"] = "a";

			await That(sut.Mock.Verify[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Set("a"))
				.Twice();
		}
	}
}
