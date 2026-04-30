using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mockolate.Parameters;

namespace Mockolate.Tests;

public sealed partial class ItTests
{
	[Test]
	public async Task InvokeCallbacks_NonGeneric_WithMatchingType_ShouldInvokeCallback()
	{
		int isCalled = 0;
		IParameter<int> sut = It.Satisfies<int>(_ => true)
			.Do(v => isCalled += v);

		sut.InvokeCallbacks(7);

		await That(isCalled).IsEqualTo(7);
	}

	[Test]
	public async Task InvokeCallbacks_NonGeneric_WithMismatchedType_ShouldNotInvokeCallback()
	{
		int isCalled = 0;
		IParameter<int> sut = It.Satisfies<int>(_ => true)
			.Do(_ => isCalled++);

		sut.InvokeCallbacks("not-an-int");

		await That(isCalled).IsEqualTo(0);
	}

	[Test]
	public async Task InvokeCallbacks_NonGeneric_WithNullForNonNullableValueType_ShouldNotInvokeCallback()
	{
		int isCalled = 0;
		IParameter<int> sut = It.Satisfies<int>(_ => true)
			.Do(_ => isCalled++);

		sut.InvokeCallbacks(null);

		await That(isCalled).IsEqualTo(0);
	}

	[Test]
	public async Task InvokeCallbacks_NonGeneric_WithNullForNullableValueType_ShouldInvokeCallbackWithDefault()
	{
		int isCalled = 0;
		int? capturedValue = 42;
		IParameter<int?> sut = It.Satisfies<int?>(_ => true)
			.Do(v =>
			{
				isCalled++;
				capturedValue = v;
			});

		sut.InvokeCallbacks(null);

		await That(isCalled).IsEqualTo(1);
		await That(capturedValue).IsNull();
	}

	[Test]
	public async Task InvokeCallbacks_NonGeneric_WithNullForReferenceType_ShouldInvokeCallbackWithDefault()
	{
		int isCalled = 0;
		string? capturedValue = "not-null";
		IParameter<string?> sut = It.Satisfies<string?>(_ => true)
			.Do(v =>
			{
				isCalled++;
				capturedValue = v;
			});

		sut.InvokeCallbacks(null);

		await That(isCalled).IsEqualTo(1);
		await That(capturedValue).IsNull();
	}

	[Test]
	public async Task InvokeCallbacks_WithCorrectType_ShouldInvokeCallback()
	{
		int isCalled = 0;
		IParameter<int> sut = It.Satisfies<int>(_ => true)
			.Do(v => isCalled += v);

		((IParameterMatch<int>)sut).InvokeCallbacks(5);

		await That(isCalled).IsEqualTo(5);
	}

	[Test]
	public async Task ShouldOnlyHaveOneParameterlessPrivateConstructor()
	{
		ConstructorInfo[] constructors = typeof(Match)
			.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

		await That(constructors).HasSingle().Which
			.Whose(c => c.GetParameters(), p => p.HasCount(0));

		_ = constructors.Single().Invoke([]);
	}

	internal interface IMyServiceWithNullable
	{
		void DoSomething(int? value, bool flag);
		void DoSomethingWithDouble(double value);
		void DoSomethingWithInt(int value);
		void DoSomethingWithLong(int value);
		void DoSomethingWithNullableDouble(double? value);
		void DoSomethingWithString(string value);
	}

	internal class AllEqualComparer : IEqualityComparer<int>
	{
		bool IEqualityComparer<int>.Equals(int x, int y) => true;

		int IEqualityComparer<int>.GetHashCode(int obj) => obj.GetHashCode();
	}
}
