using Mockolate.Tests.GeneratorCoverage;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class SetupIndexerTests
{
	public sealed class WithOptionalKeysTests
	{
		[Test]
		public async Task Getter_WithExplicitNullKey_ShouldMatchNullInsteadOfTheDeclaredDefault()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), null].Returns("a");

			string forNull = sut[1, null];
			string forDeclaredDefault = sut[1];

			await That(forNull).IsEqualTo("a");
			await That(forDeclaredDefault).IsEqualTo("").Because("only a left-out key stands for its declared default");
		}

		[Test]
		public async Task Getter_WithOmittedKeyAfterMatchers_ShouldOnlyMatchTheDeclaredDefault()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsNull<int?>()].Returns("a");

			string forOmitted = sut[1, null];
			string forOtherName = sut[1, null, "x"];
			string forDeclaredDefaults = sut[1];

			await That(forOmitted).IsEqualTo("a");
			await That(forOtherName).IsEqualTo("");
			await That(forDeclaredDefaults).IsEqualTo("");
		}

		[Test]
		public async Task Getter_WithOmittedKeyAfterValues_ShouldOnlyMatchTheDeclaredDefault()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();
			sut.Mock.Setup[1, 6].Returns("a");

			string forOmitted = sut[1, 6];
			string forOtherName = sut[1, 6, "x"];
			string forOtherKey = sut[2, 6];

			await That(forOmitted).IsEqualTo("a");
			await That(forOtherName).IsEqualTo("");
			await That(forOtherKey).IsEqualTo("");
		}

		[Test]
		public async Task Getter_WithOmittedKeys_ShouldOnlyMatchTheDeclaredDefaults()
		{
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns("a");

			string forOmitted = sut[1];
			string forDeclaredDefaults = sut[2, 5, null];
			string forOtherCount = sut[1, 6];
			string forNullCount = sut[1, null];
			string forOtherName = sut[1, 5, "x"];

			await That(forOmitted).IsEqualTo("a");
			await That(forDeclaredDefaults).IsEqualTo("a");
			await That(forOtherCount).IsEqualTo("");
			await That(forNullCount).IsEqualTo("").Because("null is not the declared default 5");
			await That(forOtherName).IsEqualTo("").Because("the declared default of name is null");
		}

		[Test]
		public async Task GetterOnly_WithOmittedKey_ShouldOnlyMatchTheDeclaredDefault()
		{
			IOptionalIndexerKeysGetterOnly sut = IOptionalIndexerKeysGetterOnly.CreateMock();
			sut.Mock.Setup[It.IsAny<byte>()].Returns(3L);

			long forOmitted = sut[1];
			long forOther = sut[1, 3];

			await That(forOmitted).IsEqualTo(3L);
			await That(forOther).IsEqualTo(0L);
		}

		[Test]
		public async Task Setter_WithOmittedKeys_ShouldOnlyMatchTheDeclaredDefaults()
		{
			int callCount = 0;
			IOptionalIndexerKeys sut = IOptionalIndexerKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].OnSet.Do(() => callCount++);

			sut[1] = "a";
			sut[1, 5, null] = "b";
			sut[1, 6] = "c";
			sut[1, 5, "x"] = "d";

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task SetterOnly_WithOmittedKey_ShouldOnlyMatchTheDeclaredDefault()
		{
			int callCount = 0;
			IOptionalIndexerKeysSetterOnly sut = IOptionalIndexerKeysSetterOnly.CreateMock();
			sut.Mock.Setup[It.IsAny<short>(), It.IsAny<short>(), It.IsAny<short>()].OnSet.Do(() => callCount++);

			sut[1, 2, 3] = 1L;
			sut[1, 2, 3, 4] = 2L;
			sut[1, 2, 3, 5] = 3L;

			await That(callCount).IsEqualTo(2);
		}

		[Test]
		public async Task WithSibling_ShouldBindToTheIndexerWithFewerKeys()
		{
			IOptionalIndexerKeysWithSibling sut = IOptionalIndexerKeysWithSibling.CreateMock();
			sut.Mock.Setup[It.IsAny<int>()].Returns("one");
			sut.Mock.Setup[2].Returns("two");

			string forAnyKey = sut[1];
			string forKey = sut[2];
			string forDeclaredDefaults = sut[1, 2, 3];

			await That(forAnyKey).IsEqualTo("one");
			await That(forKey).IsEqualTo("two");
			await That(forDeclaredDefaults).IsEqualTo("");
		}

		[Test]
		public async Task WithSixKeysAndOmittedKeys_ShouldOnlyMatchTheDeclaredDefaults()
		{
			IOptionalIndexerKeysWithSixKeys sut = IOptionalIndexerKeysWithSixKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()].Returns("a");

			string forOmitted = sut[1, 2, 3, 4];
			string forDeclaredDefaults = sut[1, 2, 3, 4, 5, "f"];
			string forOtherE = sut[1, 2, 3, 4, 6];
			string forOtherF = sut[1, 2, 3, 4, 5, "x"];

			await That(forOmitted).IsEqualTo("a");
			await That(forDeclaredDefaults).IsEqualTo("a");
			await That(forOtherE).IsEqualTo("");
			await That(forOtherF).IsEqualTo("");
		}

		[Test]
		public async Task WithSixKeysAndOmittedKeyAfterMatchers_ShouldOnlyMatchTheDeclaredDefault()
		{
			IOptionalIndexerKeysWithSixKeys sut = IOptionalIndexerKeysWithSixKeys.CreateMock();
			sut.Mock.Setup[It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.Is(6)].Returns("a");

			string forOmitted = sut[1, 2, 3, 4, 6];
			string forOtherF = sut[1, 2, 3, 4, 6, "x"];

			await That(forOmitted).IsEqualTo("a");
			await That(forOtherF).IsEqualTo("");
		}
	}
}
