namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifySetIndexerTests
{
	public sealed class DerivedTypeMatcherTests
	{
		[Test]
		public async Task Set_WithMatcherForDerivedType_ShouldMatchOnlyTheDerivedType()
		{
			IBaseValueIndexers sut = IBaseValueIndexers.CreateMock();

			sut[1] = new BaseValue();
			sut[1] = new DerivedValue();
			sut[1, 2] = new BaseValue();
			sut[1, 2] = new DerivedValue();
			sut[1, 2, 3] = new BaseValue();
			sut[1, 2, 3] = new DerivedValue();
			sut[1, 2, 3, 4] = new BaseValue();
			sut[1, 2, 3, 4] = new DerivedValue();
			sut[1, 2, 3, 4, 5] = new BaseValue();
			sut[1, 2, 3, 4, 5] = new DerivedValue();

			await That(sut.Mock.Verify[1].Set(It.IsAny<DerivedValue>())).Once()
				.Because("a matcher for a derived type is a matcher for the value type, although it only implements the typed match for the derived type");
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<DerivedValue>())).Once();
		}

		[Test]
		public async Task SetOnWriteOnlyIndexer_WithMatcherForDerivedType_ShouldMatchOnlyTheDerivedType()
		{
			IBaseValueWriteOnlyIndexers sut = IBaseValueWriteOnlyIndexers.CreateMock();

			sut[1] = new BaseValue();
			sut[1] = new DerivedValue();
			sut[1, 2] = new BaseValue();
			sut[1, 2] = new DerivedValue();
			sut[1, 2, 3] = new BaseValue();
			sut[1, 2, 3] = new DerivedValue();
			sut[1, 2, 3, 4] = new BaseValue();
			sut[1, 2, 3, 4] = new DerivedValue();
			sut[1, 2, 3, 4, 5] = new BaseValue();
			sut[1, 2, 3, 4, 5] = new DerivedValue();

			await That(sut.Mock.Verify[1].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<DerivedValue>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<DerivedValue>())).Once();
		}

		public class BaseValue;

		public class DerivedValue : BaseValue;

		public interface IBaseValueIndexers
		{
			BaseValue this[int key1] { get; set; }
			BaseValue this[int key1, int key2] { get; set; }
			BaseValue this[int key1, int key2, int key3] { get; set; }
			BaseValue this[int key1, int key2, int key3, int key4] { get; set; }
			BaseValue this[int key1, int key2, int key3, int key4, int key5] { get; set; }
		}

		public interface IBaseValueWriteOnlyIndexers
		{
			BaseValue this[int key1] { set; }
			BaseValue this[int key1, int key2] { set; }
			BaseValue this[int key1, int key2, int key3] { set; }
			BaseValue this[int key1, int key2, int key3, int key4] { set; }
			BaseValue this[int key1, int key2, int key3, int key4, int key5] { set; }
		}
	}
}
