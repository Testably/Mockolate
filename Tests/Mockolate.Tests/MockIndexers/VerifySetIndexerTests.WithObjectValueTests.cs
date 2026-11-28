using Mockolate.Exceptions;
using Mockolate.Verify;

namespace Mockolate.Tests.MockIndexers;

public sealed partial class VerifySetIndexerTests
{
	public sealed class WithObjectValueTests
	{
		[Test]
		public async Task Set_WithAnyMatcher_ShouldUseTheMatcher()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(It.IsAny<object>())).Once()
				.Because("a matcher also converts to object, but must not be compared as a value");
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<object>())).Once();
		}

		[Test]
		public async Task Set_WithValueMatcher_ShouldUseTheMatcher()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(6))).Never();
		}

		[Test]
		public async Task Set_WithValue_ShouldCheckForEquality()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(5)).Once();
			await That(sut.Mock.Verify[1].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(6)).Never();
		}

		[Test]
		public async Task Set_WithMatcherForDerivedType_ShouldUseTheMatcher()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();

			sut[1] = 5;
			sut[1] = "foo";
			sut[1, 2] = 5;
			sut[1, 2] = "foo";
			sut[1, 2, 3] = 5;
			sut[1, 2, 3] = "foo";
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4] = "foo";
			sut[1, 2, 3, 4, 5] = 5;
			sut[1, 2, 3, 4, 5] = "foo";

			await That(sut.Mock.Verify[1].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<string>())).Once();
		}

		[Test]
		public async Task Set_WithMatcherForDerivedTypeOnWriteOnlyIndexer_ShouldUseTheMatcher()
		{
			IObjectValueWriteOnlyIndexers sut = IObjectValueWriteOnlyIndexers.CreateMock();

			sut[1] = 5;
			sut[1] = "foo";
			sut[1, 2] = 5;
			sut[1, 2] = "foo";
			sut[1, 2, 3] = 5;
			sut[1, 2, 3] = "foo";
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4] = "foo";
			sut[1, 2, 3, 4, 5] = 5;
			sut[1, 2, 3, 4, 5] = "foo";

			await That(sut.Mock.Verify[1].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<string>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<string>())).Once();
		}

		[Test]
		public async Task Set_WithWrappedMatcher_ShouldCompareTheMatcherAsValue()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();
			object matcher = It.IsAny<object>();

			sut[1] = matcher;
			sut[1] = 5;

			await That(sut.Mock.Verify[1].Set(It.Is(matcher))).Once();
		}

		[Test]
		public async Task Set_WithMatcher_WhenNotMatching_ShouldDescribeTheMatcher()
		{
			IObjectValueIndexers sut = IObjectValueIndexers.CreateMock();

			void Act()
			{
				sut.Mock.Verify[1].Set(It.Is<object>(6)).Once();
			}

			await That(Act).Throws<MockVerificationException>()
				.WithMessage("Expected that mock set indexer [1] to 6 exactly once, but it never did.")
				.Because("the matcher describes itself, while a value is described by its source expression");
		}

		[Test]
		public async Task Set_WithAnyMatcherOnWriteOnlyIndexer_ShouldUseTheMatcher()
		{
			IObjectValueWriteOnlyIndexers sut = IObjectValueWriteOnlyIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.IsAny<object>())).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.IsAny<object>())).Once();
		}

		[Test]
		public async Task Set_WithValueMatcherOnWriteOnlyIndexer_ShouldUseTheMatcher()
		{
			IObjectValueWriteOnlyIndexers sut = IObjectValueWriteOnlyIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(5))).Once();
			await That(sut.Mock.Verify[1].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(It.Is<object>(6))).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(It.Is<object>(6))).Never();
		}

		[Test]
		public async Task Set_WithValueOnWriteOnlyIndexer_ShouldCheckForEquality()
		{
			IObjectValueWriteOnlyIndexers sut = IObjectValueWriteOnlyIndexers.CreateMock();

			sut[1] = 5;
			sut[1, 2] = 5;
			sut[1, 2, 3] = 5;
			sut[1, 2, 3, 4] = 5;
			sut[1, 2, 3, 4, 5] = 5;

			await That(sut.Mock.Verify[1].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(5)).Once();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(5)).Once();
			await That(sut.Mock.Verify[1].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4].Set(6)).Never();
			await That(sut.Mock.Verify[1, 2, 3, 4, 5].Set(6)).Never();
		}

		public interface IObjectValueIndexers
		{
			object this[int key1] { get; set; }
			object this[int key1, int key2] { get; set; }
			object this[int key1, int key2, int key3] { get; set; }
			object this[int key1, int key2, int key3, int key4] { get; set; }
			object this[int key1, int key2, int key3, int key4, int key5] { get; set; }
		}

		public interface IObjectValueWriteOnlyIndexers
		{
			object this[int key1] { set; }
			object this[int key1, int key2] { set; }
			object this[int key1, int key2, int key3] { set; }
			object this[int key1, int key2, int key3, int key4] { set; }
			object this[int key1, int key2, int key3, int key4, int key5] { set; }
		}
	}
}
