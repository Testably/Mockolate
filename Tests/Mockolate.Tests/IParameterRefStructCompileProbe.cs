#if NET9_0_OR_GREATER
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed class IParameterRefStructCompileProbe
{
	[Test]
	public async Task IParameterOfT_Accepts_RefStruct()
	{
		IParameter<MyRefStruct> parameter = new AnyRefStructMatch();
		IParameterMatch<MyRefStruct> match = (IParameterMatch<MyRefStruct>)parameter;

		MyRefStruct value = new(42);
		bool result = match.Matches(value);

		await That(result).IsTrue();
	}

	private ref struct MyRefStruct(int id)
	{
		public int Id => id;
	}

	private sealed class AnyRefStructMatch : IParameter<MyRefStruct>, IParameterMatch<MyRefStruct>
	{
		public bool Matches(object? value) => false;
		public bool Matches(MyRefStruct value) => true;
		public void InvokeCallbacks(object? value) { }
		public void InvokeCallbacks(MyRefStruct value) { }
	}
}
#endif
