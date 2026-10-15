#if NET9_0_OR_GREATER
using Mockolate.Setup;

namespace Mockolate.Tests.RefStruct;

public sealed class RefStructStorageHelperTests
{
	[Test]
	public async Task TryResolveKey_WithNullProjectionAndNullRawKey_ShouldReturnFalse()
	{
		bool result = RefStructStorageHelper.TryResolveKey<int>(null, 7, null, out object key);

		await That(result).IsFalse();
		await That(key).IsNull();
	}

	[Test]
	public async Task TryResolveKey_WithNullProjectionAndRawKey_ShouldReturnRawKey()
	{
		object boxed = 42;

		bool result = RefStructStorageHelper.TryResolveKey<int>(null, 7, boxed, out object key);

		await That(result).IsTrue();
		await That(key).IsSameAs(boxed);
	}
}
#endif
